import Darwin
import Foundation
import CryptoKit

struct ClaudeBridgePreview: Sendable {
    let settingsURL: URL
    let directory: URL
    let originalCommand: String
    let installedCommand: String
    let settingsExisted: Bool
    let originalSettings: Data
    let installedSettings: Data
    let wrapperBinary: Data
    let helperBinary: Data
}

enum ClaudeBridgeInstaller {
    private struct RestoredSettings: Codable {
        let settingsDigest: String?
        let backupDigest: String
    }

    private struct Ownership: Codable {
        let settingsExisted: Bool?
        let schemaVersion: Int
        let settingsPath: String
        let originalCommand: String
        let installedCommand: String
        let wrapperDigest: String
        let helperDigest: String
        var restoredSettings: RestoredSettings? = nil
    }
    static let artifactNames = ["claude-wrapper", "claude-helper", "claude-settings-backup.json", "claude-install.json"]
    // Keep ownership evidence until every other artifact is gone. The backup
    // remains available until binaries and the snapshot have been removed.
    static let restoreCleanupNames = ["claude-wrapper", "claude-helper", "claude-latest.json",
                                      "claude-settings-backup.json", "claude-install.json"]

    static func shellQuote(_ text: String) -> String { "'" + text.replacingOccurrences(of: "'", with: "'\\''") + "'" }

    static func preview(settingsURL: URL, directory: URL, wrapperBinary: Data,
                        helperBinary: Data) throws -> ClaudeBridgePreview {
        let existing = try readSettingsIfPresent(settingsURL)
        let settings = existing ?? Data("{}".utf8)
        let token = try ClaudeSettingsCommand(data: settings)
        guard !token.original.contains("AIUsageBar/claude-wrapper"),
              !FileManager.default.fileExists(atPath: directory.appendingPathComponent("claude-install.json").path) else {
            throw ClaudeBridgeError.alreadyInstalled
        }
        let wrapper = directory.appendingPathComponent("claude-wrapper").path
        let helper = directory.appendingPathComponent("claude-helper").path
        let installed: String
        if !token.hasStatusLine {
            installed = "if [ -x " + shellQuote(wrapper) + " ]; then exec " + shellQuote(wrapper)
                + " collect " + shellQuote(directory.path) + "; else :; fi"
        } else {
            installed = "if [ -x " + shellQuote(wrapper) + " ]; then exec " + shellQuote(wrapper)
                + " wrap " + shellQuote(token.original) + " " + shellQuote(helper) + " " + shellQuote(directory.path)
                + "; else exec /bin/sh -c " + shellQuote(token.original) + "; fi"
        }
        let installedSettings = try token.replacing(in: settings, with: installed)
        guard installedSettings.count <= ClaudePrivateFiles.maximumSettingsBytes else { throw ClaudeBridgeError.oversized }
        return ClaudeBridgePreview(settingsURL: settingsURL, directory: directory,
                                   originalCommand: token.original, installedCommand: installed,
                                   settingsExisted: existing != nil, originalSettings: settings, installedSettings: installedSettings,
                                   wrapperBinary: wrapperBinary, helperBinary: helperBinary)
    }

    /// Preview contains the exact reviewed bytes. All artifacts are written
    /// before touching settings; an interrupted install is recoverable from backup.
    static func apply(_ preview: ClaudeBridgePreview) throws {
        let fd = try ClaudePrivateFiles.directory(preview.directory, create: true)
        defer { close(fd) }
        try ClaudePrivateFiles.withLock(directoryFD: fd) {
            guard try readSettingsIfPresent(preview.settingsURL) == (preview.settingsExisted ? preview.originalSettings : nil) else {
                throw ClaudeBridgeError.conflict
            }
            for name in artifactNames + ["claude-latest.json"] {
                var info = stat()
                guard fstatat(fd, name, &info, AT_SYMLINK_NOFOLLOW) != 0, errno == ENOENT else {
                    throw ClaudeBridgeError.conflict
                }
            }
            let ownership = Ownership(settingsExisted: preview.settingsExisted, schemaVersion: 1, settingsPath: preview.settingsURL.path,
                                      originalCommand: preview.originalCommand, installedCommand: preview.installedCommand,
                                      wrapperDigest: digest(preview.wrapperBinary), helperDigest: digest(preview.helperBinary))
            let metadata = try JSONEncoder().encode(ownership)
            let artifacts: [(String, Data, mode_t)] = [
                ("claude-settings-backup.json", preview.originalSettings, 0o600),
                ("claude-wrapper", preview.wrapperBinary, 0o700),
                ("claude-helper", preview.helperBinary, 0o700),
                ("claude-install.json", metadata, 0o600)
            ]
            do {
                for (name, bytes, mode) in artifacts {
                    try ClaudePrivateFiles.atomicWrite(bytes, name: name, directoryFD: fd, mode: mode)
                }
                try replaceSettings(preview.settingsURL, expected: preview.settingsExisted ? preview.originalSettings : nil, replacement: preview.installedSettings)
            } catch {
                // If settings already point to our wrapper, retain the backup
                // and metadata so explicit disconnect can recover. Otherwise
                // remove only byte-identical artifacts from this failed attempt.
                let current = try? ClaudePrivateFiles.read(preview.settingsURL, privateMode: false)
                let installed = current.flatMap { try? ClaudeSettingsCommand(data: $0).original } == preview.installedCommand
                if !installed {
                    for (name, bytes, _) in artifacts {
                        if (try? ClaudePrivateFiles.read(name: name, directoryFD: fd, limit: 64 * 1_024 * 1_024)) == bytes {
                            unlinkat(fd, name, 0)
                        }
                    }
                }
                throw error
            }
        }
    }

    /// Restores only our command token, preserving later edits to unrelated keys.
    /// A command edit by another tool is a conflict and nothing is overwritten.
    static func restore(directory: URL, settingsURL: URL) throws {
        let fd = try ClaudePrivateFiles.directory(directory)
        defer { close(fd) }
        try ClaudePrivateFiles.withLock(directoryFD: fd) {
            var ownership = try JSONDecoder().decode(Ownership.self, from: ClaudePrivateFiles.read(name: "claude-install.json", directoryFD: fd))
            guard ownership.schemaVersion == 1, ownership.settingsPath == settingsURL.path else { throw ClaudeBridgeError.conflict }
            let current = try readSettingsIfPresent(settingsURL)
            let backup: Data?
            do { backup = try ClaudePrivateFiles.read(name: "claude-settings-backup.json", directoryFD: fd) }
            catch ClaudeBridgeError.missingFile { backup = nil }
            var replacement: Data?
            if let restored = ownership.restoredSettings {
                // Cleanup may already have removed the backup. This checkpoint
                // binds retry to the exact restored settings (including absence),
                // so subsequent edits remain conflicts rather than being lost.
                guard current.map(digest) == restored.settingsDigest,
                      backup.map(digest).map({ $0 == restored.backupDigest }) ?? true else {
                    throw ClaudeBridgeError.conflict
                }
                replacement = current
            } else {
                guard let backup else { throw ClaudeBridgeError.missingFile }
                let backupToken = try ClaudeSettingsCommand(data: backup)
                guard backupToken.original == ownership.originalCommand else { throw ClaudeBridgeError.conflict }
                if let current {
                    let token = try ClaudeSettingsCommand(data: current)
                    guard token.original == ownership.installedCommand || token.original == ownership.originalCommand else {
                        throw ClaudeBridgeError.conflict
                    }
                    replacement = current
                    if token.original == ownership.installedCommand {
                        if backupToken.hasStatusLine {
                            replacement?.replaceSubrange(token.range, with: backup.subdata(in: backupToken.range))
                        } else {
                            replacement = try token.removingStatusLine(in: current, installedCommand: ownership.installedCommand)
                        }
                    } else if token.hasStatusLine != backupToken.hasStatusLine { throw ClaudeBridgeError.conflict }
                    if ownership.settingsExisted == false, replacement == backup { replacement = nil }
                } else {
                    // A first-install restore may have removed settings just
                    // before interruption, while ownership and backup survive.
                    guard ownership.settingsExisted == false, !backupToken.hasStatusLine,
                          backup == Data("{}".utf8) else { throw ClaudeBridgeError.conflict }
                    replacement = nil
                }
            }
            // Validate every remaining file before restoring or deleting any.
            for name in restoreCleanupNames {
                let bytes: Data
                do { bytes = try ClaudePrivateFiles.read(name: name, directoryFD: fd, limit: 64 * 1_024 * 1_024) }
                catch ClaudeBridgeError.missingFile { continue }
                if name == "claude-wrapper", digest(bytes) != ownership.wrapperDigest { throw ClaudeBridgeError.conflict }
                if name == "claude-helper", digest(bytes) != ownership.helperDigest { throw ClaudeBridgeError.conflict }
            }
            if ownership.restoredSettings == nil {
                if let current {
                    if let replacement {
                        try replaceSettings(settingsURL, expected: current, replacement: replacement)
                    } else {
                        let parent = try ClaudePrivateFiles.directory(settingsURL.deletingLastPathComponent())
                        defer { close(parent) }
                        guard try readSettingsIfPresent(settingsURL) == current,
                              unlinkat(parent, settingsURL.lastPathComponent, 0) == 0 else { throw ClaudeBridgeError.conflict }
                        guard fsync(parent) == 0 else { throw ClaudeBridgeError.ioFailure }
                    }
                }
                guard try readSettingsIfPresent(settingsURL) == replacement, let backup else { throw ClaudeBridgeError.conflict }
                ownership.restoredSettings = RestoredSettings(settingsDigest: replacement.map(digest), backupDigest: digest(backup))
                // Commit recovery evidence before any destructive cleanup.
                try ClaudePrivateFiles.atomicWrite(JSONEncoder().encode(ownership), name: "claude-install.json", directoryFD: fd)
            }
            for name in restoreCleanupNames {
                if unlinkat(fd, name, 0) != 0, errno != ENOENT { throw ClaudeBridgeError.ioFailure }
                // Persist each boundary before deleting the final ownership
                // file, preventing metadata-free leftovers after interruption.
                guard fsync(fd) == 0 else { throw ClaudeBridgeError.ioFailure }
            }
        }
    }

    private static func readSettingsIfPresent(_ url: URL) throws -> Data? {
        do { return try ClaudePrivateFiles.read(url, privateMode: false) }
        catch ClaudeBridgeError.missingFile { return nil }
    }

    private static func replaceSettings(_ url: URL, expected: Data?, replacement: Data) throws {
        let fd = try ClaudePrivateFiles.directory(url.deletingLastPathComponent(), create: expected == nil)
        defer { close(fd) }
        guard try readSettingsIfPresent(url) == expected else { throw ClaudeBridgeError.conflict }
        // Replacing uses 0600, retaining all existing content outside command.
        try ClaudePrivateFiles.atomicWrite(replacement, name: url.lastPathComponent, directoryFD: fd, replaceExisting: expected != nil)
    }

    private static func digest(_ data: Data) -> String {
        SHA256.hash(data: data).map { String(format: "%02x", $0) }.joined()
    }
}

import Darwin
import Foundation
import CryptoKit

struct ClaudeBridgePreview: Sendable {
    let settingsURL: URL
    let directory: URL
    let originalCommand: String
    let installedCommand: String
    let originalSettings: Data
    let installedSettings: Data
    let wrapperBinary: Data
    let helperBinary: Data
}

enum ClaudeBridgeInstaller {
    private struct Ownership: Codable {
        let schemaVersion: Int
        let settingsPath: String
        let originalCommand: String
        let installedCommand: String
        let wrapperDigest: String
        let helperDigest: String
    }
    static let artifactNames = ["claude-wrapper", "claude-helper", "claude-settings-backup.json", "claude-install.json"]

    static func shellQuote(_ text: String) -> String { "'" + text.replacingOccurrences(of: "'", with: "'\\''") + "'" }

    static func preview(settingsURL: URL, directory: URL, wrapperBinary: Data,
                        helperBinary: Data) throws -> ClaudeBridgePreview {
        let settings = try ClaudePrivateFiles.read(settingsURL, privateMode: false)
        let token = try ClaudeSettingsCommand(data: settings)
        guard !token.original.contains("AIUsageBar/claude-wrapper"),
              !FileManager.default.fileExists(atPath: directory.appendingPathComponent("claude-install.json").path) else {
            throw ClaudeBridgeError.alreadyInstalled
        }
        let wrapper = directory.appendingPathComponent("claude-wrapper").path
        let helper = directory.appendingPathComponent("claude-helper").path
        let installed = "if [ -x " + shellQuote(wrapper) + " ]; then exec " + shellQuote(wrapper)
            + " wrap " + shellQuote(token.original) + " " + shellQuote(helper) + " " + shellQuote(directory.path)
            + "; else exec /bin/sh -c " + shellQuote(token.original) + "; fi"
        return ClaudeBridgePreview(settingsURL: settingsURL, directory: directory,
                                   originalCommand: token.original, installedCommand: installed,
                                   originalSettings: settings, installedSettings: try token.replacing(in: settings, with: installed),
                                   wrapperBinary: wrapperBinary, helperBinary: helperBinary)
    }

    /// Preview contains the exact reviewed bytes. All artifacts are written
    /// before touching settings; an interrupted install is recoverable from backup.
    static func apply(_ preview: ClaudeBridgePreview) throws {
        let fd = try ClaudePrivateFiles.directory(preview.directory, create: true)
        defer { close(fd) }
        try ClaudePrivateFiles.withLock(directoryFD: fd) {
            guard try ClaudePrivateFiles.read(preview.settingsURL, privateMode: false) == preview.originalSettings else {
                throw ClaudeBridgeError.conflict
            }
            for name in artifactNames + ["claude-latest.json"] {
                var info = stat()
                guard fstatat(fd, name, &info, AT_SYMLINK_NOFOLLOW) != 0, errno == ENOENT else {
                    throw ClaudeBridgeError.conflict
                }
            }
            let ownership = Ownership(schemaVersion: 1, settingsPath: preview.settingsURL.path,
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
                try replaceSettings(preview.settingsURL, expected: preview.originalSettings, replacement: preview.installedSettings)
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
            let ownership = try JSONDecoder().decode(Ownership.self, from: ClaudePrivateFiles.read(name: "claude-install.json", directoryFD: fd))
            guard ownership.schemaVersion == 1, ownership.settingsPath == settingsURL.path else { throw ClaudeBridgeError.conflict }
            let current = try ClaudePrivateFiles.read(settingsURL, privateMode: false)
            let token = try ClaudeSettingsCommand(data: current)
            guard token.original == ownership.installedCommand || token.original == ownership.originalCommand else {
                throw ClaudeBridgeError.conflict
            }
            // Confirm backup belongs to the same install before any mutation.
            let backup = try ClaudePrivateFiles.read(name: "claude-settings-backup.json", directoryFD: fd)
            let backupToken = try ClaudeSettingsCommand(data: backup)
            guard backupToken.original == ownership.originalCommand else { throw ClaudeBridgeError.conflict }
            var replacement = current
            if token.original == ownership.installedCommand {
                replacement.replaceSubrange(token.range, with: backup.subdata(in: backupToken.range))
            }
            // Validate every file before restoring or deleting any of them.
            for name in artifactNames {
                let bytes: Data
                do { bytes = try ClaudePrivateFiles.read(name: name, directoryFD: fd, limit: 64 * 1_024 * 1_024) }
                catch ClaudeBridgeError.missingFile { continue }
                if name == "claude-wrapper", digest(bytes) != ownership.wrapperDigest { throw ClaudeBridgeError.conflict }
                if name == "claude-helper", digest(bytes) != ownership.helperDigest { throw ClaudeBridgeError.conflict }
            }
            do { _ = try ClaudePrivateFiles.read(name: "claude-latest.json", directoryFD: fd, limit: 64 * 1_024 * 1_024) }
            catch ClaudeBridgeError.missingFile { }
            try replaceSettings(settingsURL, expected: current, replacement: replacement)
            for name in artifactNames + ["claude-latest.json"] {
                if unlinkat(fd, name, 0) != 0, errno != ENOENT { throw ClaudeBridgeError.ioFailure }
            }
        }
    }

    private static func replaceSettings(_ url: URL, expected: Data, replacement: Data) throws {
        let fd = try ClaudePrivateFiles.directory(url.deletingLastPathComponent())
        defer { close(fd) }
        guard try ClaudePrivateFiles.read(url, privateMode: false) == expected else { throw ClaudeBridgeError.conflict }
        // Replacing uses 0600, retaining all existing content outside command.
        try ClaudePrivateFiles.atomicWrite(replacement, name: url.lastPathComponent, directoryFD: fd)
    }

    private static func digest(_ data: Data) -> String {
        SHA256.hash(data: data).map { String(format: "%02x", $0) }.joined()
    }
}

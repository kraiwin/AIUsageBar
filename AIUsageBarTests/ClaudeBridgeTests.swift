import Darwin
import Foundation
import XCTest
@testable import AIUsageBar

final class ClaudeBridgeTests: XCTestCase {
    private let now = Date(timeIntervalSince1970: 1_700_000_000)
    private let quota = Data(#"{"rate_limits":{"seven_day":{"used_percentage":0,"resets_at":1900000000},"five_hour":{"used_percentage":25,"resets_at":1900000100}},"session_id":"synthetic-not-retained","workspace":{"cwd":"synthetic-not-retained"},"secret":"synthetic-not-retained"}"#.utf8)

    private func temporary() throws -> URL {
        let url = FileManager.default.temporaryDirectory.appendingPathComponent("AIUsageBar-ClaudeTests-" + UUID().uuidString)
        try FileManager.default.createDirectory(at: url, withIntermediateDirectories: false, attributes: [.posixPermissions: 0o700])
        addTeardownBlock { try? FileManager.default.removeItem(at: url) }
        return url
    }

    private func write(_ bytes: Data, to url: URL, mode: mode_t = 0o600) throws {
        try bytes.write(to: url)
        XCTAssertEqual(chmod(url.path, mode), 0)
    }

    private func preview(_ root: URL, original: Data? = nil) throws -> ClaudeBridgePreview {
        let settings = root.appendingPathComponent("settings.json")
        try write(original ?? Data("{\n  \"statusLine\" : { \"type\":\"command\", \"command\" : \"/bin/cat\", \"padding\": 2 },\n \"other\": {\"keep\":true}\n}\n".utf8), to: settings)
        return try ClaudeBridgeInstaller.preview(settingsURL: settings, directory: root.appendingPathComponent("bridge"),
                                                  wrapperBinary: Data("synthetic-wrapper".utf8), helperBinary: Data("synthetic-helper".utf8))
    }

    func testQuotaOnlyAndRepeatedReadsDoNotAdvanceTimestamp() throws {
        let root = try temporary()
        try ClaudeSnapshot.write(quota, directory: root, receivedAt: now)
        let url = root.appendingPathComponent("claude-latest.json")
        let bytes = try ClaudePrivateFiles.read(url)
        XCTAssertFalse(String(decoding: bytes, as: UTF8.self).contains("synthetic-not-retained"))
        let provider = ClaudeSnapshotProvider(snapshotURL: url)
        let first = try XCTUnwrap(provider.read(now: now.addingTimeInterval(10)))
        let next = try XCTUnwrap(provider.read(now: now.addingTimeInterval(100)))
        XCTAssertEqual(first, next)
        XCTAssertEqual(first.weekly.usedPercent, 0)
        XCTAssertEqual(first.receivedAt, now)
        XCTAssertNil(first.providerObservedAt)
    }

    func testLatestNoDataClearsOldQuotaAndRejectsOlderQuota() throws {
        let root = try temporary(), url = root.appendingPathComponent("claude-latest.json")
        try ClaudeSnapshot.write(quota, directory: root, receivedAt: now)
        try ClaudeSnapshot.write(Data("{}".utf8), directory: root, receivedAt: now.addingTimeInterval(1))
        XCTAssertNil(try ClaudeSnapshotProvider(snapshotURL: url).read(now: now.addingTimeInterval(2)))
        try ClaudeSnapshot.write(quota, directory: root, receivedAt: now)
        XCTAssertNil(try ClaudeSnapshotProvider(snapshotURL: url).read(now: now.addingTimeInterval(2)))
    }

    func testMalformedInputDoesNotRefreshSnapshot() throws {
        let root = try temporary(), url = root.appendingPathComponent("claude-latest.json")
        try ClaudeSnapshot.write(quota, directory: root, receivedAt: now)
        let old = try ClaudePrivateFiles.read(url)
        for invalid in ["not-json", "[]", #"{"rate_limits":{"seven_day":{"used_percentage":true,"resets_at":1900000000}}}"#] {
            XCTAssertThrowsError(try ClaudeSnapshot.write(Data(invalid.utf8), directory: root, receivedAt: now.addingTimeInterval(100)))
            XCTAssertEqual(try ClaudePrivateFiles.read(url), old)
        }
    }

    func testCorruptAndFutureSnapshotAreRepairedByNewValidInput() throws {
        let root = try temporary(), url = root.appendingPathComponent("claude-latest.json")
        for invalid in [Data("broken".utf8), try JSONEncoder().encode(ClaudeSnapshot(schemaVersion: 1,
            receivedAt: Date().addingTimeInterval(100_000).timeIntervalSince1970, state: "quota",
            weekly: .init(usedPercent: 2, resetsAt: 1_900_000_000), session: nil))] {
            try write(invalid, to: url)
            try ClaudeSnapshot.write(quota, directory: root, receivedAt: now)
            XCTAssertEqual(try ClaudeSnapshotProvider(snapshotURL: url).read(now: now)?.receivedAt, now)
        }
    }

    func testMissingInstallationAndMissingSnapshotAreNoData() throws {
        let root = try temporary()
        XCTAssertNil(try ClaudeSnapshotProvider(snapshotURL: root.appendingPathComponent("missing/claude-latest.json")).read())
        XCTAssertNil(try ClaudeSnapshotProvider(snapshotURL: root.appendingPathComponent("claude-latest.json")).read())
    }

    func testSymlinkHardlinkWorldReadableAndOversizedSnapshotRejected() throws {
        let root = try temporary(), target = root.appendingPathComponent("target"), url = root.appendingPathComponent("claude-latest.json")
        try write(Data("{}".utf8), to: target)
        try FileManager.default.createSymbolicLink(at: url, withDestinationURL: target)
        XCTAssertThrowsError(try ClaudeSnapshotProvider(snapshotURL: url).read())
        try FileManager.default.removeItem(at: url)
        XCTAssertEqual(link(target.path, url.path), 0)
        XCTAssertThrowsError(try ClaudeSnapshotProvider(snapshotURL: url).read())
        try FileManager.default.removeItem(at: url)
        try write(Data("{}".utf8), to: url, mode: 0o644)
        XCTAssertThrowsError(try ClaudeSnapshotProvider(snapshotURL: url).read())
        try write(Data(repeating: 32, count: 4_097), to: url)
        XCTAssertThrowsError(try ClaudeSnapshotProvider(snapshotURL: url).read())
        XCTAssertEqual(chmod(root.path, 0o755), 0)
        XCTAssertThrowsError(try ClaudeSnapshotProvider(snapshotURL: url).read())
    }

    func testCommandOnlyPatchAndExactEscapedTokenRestore() throws {
        let root = try temporary()
        let original = Data(#"{ "statusLine" : { "type":"command", "command":"echo \/tmp\u0020hello", "padding": 8 }, "unrelated":[1,{"nested":true}] }"#.utf8)
        let item = try preview(root, original: original)
        let token = try ClaudeSettingsCommand(data: original)
        XCTAssertEqual(item.installedSettings.prefix(token.range.lowerBound), original.prefix(token.range.lowerBound))
        XCTAssertTrue(item.installedSettings.suffix(original.count - token.range.upperBound) == original.suffix(original.count - token.range.upperBound))
        try ClaudeBridgeInstaller.apply(item)
        try ClaudeBridgeInstaller.restore(directory: item.directory, settingsURL: item.settingsURL)
        XCTAssertEqual(try ClaudePrivateFiles.read(item.settingsURL), original)
        XCTAssertFalse(FileManager.default.fileExists(atPath: item.directory.appendingPathComponent("claude-wrapper").path))
    }

    func testApplyDetectsSettingsRevisionAndDoesNotInstall() throws {
        let root = try temporary(), item = try preview(root)
        try write(Data("{\"changed\":true}".utf8), to: item.settingsURL)
        XCTAssertThrowsError(try ClaudeBridgeInstaller.apply(item))
        XCTAssertEqual(try ClaudePrivateFiles.read(item.settingsURL), Data("{\"changed\":true}".utf8))
        XCTAssertFalse(FileManager.default.fileExists(atPath: item.directory.appendingPathComponent("claude-wrapper").path))
    }

    func testInstallRejectsPreexistingSnapshotAndPreservesIt() throws {
        let root = try temporary(), item = try preview(root)
        try FileManager.default.createDirectory(at: item.directory, withIntermediateDirectories: false,
                                                attributes: [.posixPermissions: 0o700])
        let snapshot = item.directory.appendingPathComponent("claude-latest.json"), prior = Data("preexisting-user-file".utf8)
        try write(prior, to: snapshot)
        XCTAssertThrowsError(try ClaudeBridgeInstaller.apply(item))
        XCTAssertEqual(try ClaudePrivateFiles.read(snapshot), prior)
        XCTAssertEqual(try ClaudePrivateFiles.read(item.settingsURL), item.originalSettings)
        XCTAssertFalse(FileManager.default.fileExists(atPath: item.directory.appendingPathComponent("claude-install.json").path))
    }

    func testRestorePreservesUnrelatedEditsAndUserFiles() throws {
        let root = try temporary(), item = try preview(root)
        try ClaudeBridgeInstaller.apply(item)
        let changed = String(decoding: item.installedSettings, as: UTF8.self).replacingOccurrences(of: "\"keep\":true", with: "\"keep\":false")
        try write(Data(changed.utf8), to: item.settingsURL)
        let userFile = item.directory.appendingPathComponent("user-file")
        try write(Data("keep".utf8), to: userFile)
        try write(Data("corrupt".utf8), to: item.directory.appendingPathComponent("claude-latest.json"))
        try ClaudeBridgeInstaller.restore(directory: item.directory, settingsURL: item.settingsURL)
        XCTAssertTrue(String(decoding: try ClaudePrivateFiles.read(item.settingsURL), as: UTF8.self).contains("\"keep\":false"))
        XCTAssertEqual(try ClaudePrivateFiles.read(userFile), Data("keep".utf8))
    }

    func testRestoreCommandAndBinaryConflictsDoNotOverwriteOrDelete() throws {
        let root = try temporary(), item = try preview(root)
        try ClaudeBridgeInstaller.apply(item)
        let current = try ClaudeSettingsCommand(data: item.installedSettings)
        let modified = try current.replacing(in: item.installedSettings, with: "echo user-edit")
        try write(modified, to: item.settingsURL)
        XCTAssertThrowsError(try ClaudeBridgeInstaller.restore(directory: item.directory, settingsURL: item.settingsURL))
        XCTAssertEqual(try ClaudePrivateFiles.read(item.settingsURL), modified)
        try write(item.installedSettings, to: item.settingsURL)
        try write(Data("modified-wrapper".utf8), to: item.directory.appendingPathComponent("claude-wrapper"), mode: 0o700)
        XCTAssertThrowsError(try ClaudeBridgeInstaller.restore(directory: item.directory, settingsURL: item.settingsURL))
        XCTAssertEqual(try ClaudePrivateFiles.read(item.settingsURL), item.installedSettings)
    }

    func testInterruptedInstallCanRestoreWhenCommandIsStillOriginal() throws {
        let root = try temporary(), item = try preview(root)
        try ClaudeBridgeInstaller.apply(item)
        try write(item.originalSettings, to: item.settingsURL)
        try ClaudeBridgeInstaller.restore(directory: item.directory, settingsURL: item.settingsURL)
        XCTAssertEqual(try ClaudePrivateFiles.read(item.settingsURL), item.originalSettings)
    }

    func testFailedSettingsWriteRemovesOnlyArtifactsFromAttempt() throws {
        let root = try temporary(), settingsDirectory = root.appendingPathComponent("settings-dir")
        try FileManager.default.createDirectory(at: settingsDirectory, withIntermediateDirectories: false,
                                                attributes: [.posixPermissions: 0o755])
        let item = try preview(settingsDirectory)
        // Reading the safe settings file is allowed, but the non-private parent
        // is rejected at replacement time after the private artifacts exist.
        XCTAssertThrowsError(try ClaudeBridgeInstaller.apply(item))
        XCTAssertEqual(try ClaudePrivateFiles.read(item.settingsURL), item.originalSettings)
        for name in ClaudeBridgeInstaller.artifactNames {
            XCTAssertFalse(FileManager.default.fileExists(atPath: item.directory.appendingPathComponent(name).path))
        }
    }

    func testDuplicateSettingsKeysAndNonCommandRejected() throws {
        for invalid in [#"{"statusLine":{"type":"command","command":"one","command":"two"}}"#,
                        #"{"statusLine":{"type":"other","command":"one"}}"#,
                        #"{"statusLine":{"type":"command","command":10}}"#] {
            XCTAssertThrowsError(try ClaudeSettingsCommand(data: Data(invalid.utf8)))
        }
    }

    func testConcurrentWritesKeepNewestObservation() throws {
        let root = try temporary(), payload = quota, timestamp = now
        final class Failures: @unchecked Sendable {
            private let lock = NSLock()
            private var failures = 0
            var count: Int { lock.withLock { failures } }
            func record() { lock.withLock { failures += 1 } }
        }
        let failures = Failures()
        DispatchQueue.concurrentPerform(iterations: 12) { number in
            do { try ClaudeSnapshot.write(payload, directory: root, receivedAt: timestamp.addingTimeInterval(Double(number))) }
            catch { failures.record() }
        }
        XCTAssertEqual(failures.count, 0)
        let value = try ClaudeSnapshotProvider(snapshotURL: root.appendingPathComponent("claude-latest.json")).read(now: now.addingTimeInterval(12))
        XCTAssertEqual(value?.receivedAt, now.addingTimeInterval(11))
    }

    private func binary() throws -> URL {
        // Hosted tests already have the app binary built from this source. Its
        // bridge entry point avoids a separate tool compilation prerequisite.
        return try XCTUnwrap(Bundle.main.executableURL)
    }

    private func childEnvironment() -> [String: String] {
        ProcessInfo.processInfo.environment.filter { key, _ in
            !key.hasPrefix("DYLD_") && !key.hasPrefix("XCTest") && !key.hasPrefix("XCInject")
                && key != "LLVM_PROFILE_FILE"
        }
    }

    private func launch(_ arguments: [String], input: Data) throws -> (Data, Data, Int32) {
        let child = Process(), stdin = Pipe(), stdout = Pipe(), stderr = Pipe()
        child.executableURL = try binary(); child.arguments = arguments
        child.environment = childEnvironment()
        child.standardInput = stdin; child.standardOutput = stdout; child.standardError = stderr
        try child.run()
        try stdin.fileHandleForWriting.write(contentsOf: input)
        try stdin.fileHandleForWriting.close()
        let output = try stdout.fileHandleForReading.readToEnd() ?? Data()
        let errors = try stderr.fileHandleForReading.readToEnd() ?? Data()
        child.waitUntilExit()
        return (output, errors, child.terminationStatus)
    }

    func testWrapperPreservesExactInputOutputErrorAndExitAndHelperMissing() throws {
        let root = try temporary()
        let input = Data([0, 1, 13, 10, 255, 32, 34])
        let value = try launch(["wrap", "/bin/cat; printf 'original-error' >&2; exit 7", root.appendingPathComponent("missing-helper").path, root.path], input: input)
        XCTAssertEqual(value.0, input)
        XCTAssertEqual(value.1, Data("original-error".utf8))
        XCTAssertEqual(value.2, 7)
    }

    func testWrapperIngestFailureCannotChangeOriginalExit() throws {
        let root = try temporary(), helper = try binary()
        let value = try launch(["wrap", "/bin/cat; exit 9", helper.path, root.path], input: Data("invalid-json".utf8))
        XCTAssertEqual(value.0, Data("invalid-json".utf8))
        XCTAssertEqual(value.1, Data())
        XCTAssertEqual(value.2, 9)
        XCTAssertFalse(FileManager.default.fileExists(atPath: root.appendingPathComponent("claude-latest.json").path))
    }

    func testBrokenHelperTimesOutAndPreservesOriginalOutputAndExit() throws {
        let root = try temporary(), broken = root.appendingPathComponent("broken-helper")
        try write(Data("#!/bin/sh\ntrap '' TERM\nsleep 30\n".utf8), to: broken, mode: 0o700)
        let start = ProcessInfo.processInfo.systemUptime
        let value = try launch(["wrap", "/bin/cat; exit 11", broken.path, root.path], input: quota)
        XCTAssertEqual(value.0, quota)
        XCTAssertEqual(value.1, Data())
        XCTAssertEqual(value.2, 11)
        XCTAssertLessThan(ProcessInfo.processInfo.systemUptime - start, 3)
    }

    func testWrapperWritesQuotaAndShellFallbackWorksWithoutWrapper() throws {
        let root = try temporary(), helper = try binary()
        let item = try preview(root)
        try ClaudeBridgeInstaller.apply(item)
        let value = try launch(["wrap", "/bin/cat", helper.path, item.directory.path], input: quota)
        XCTAssertEqual(value.0, quota)
        XCTAssertEqual(value.2, 0)
        XCTAssertEqual(try ClaudeSnapshotProvider(snapshotURL: item.directory.appendingPathComponent("claude-latest.json")).read()?.weekly.usedPercent, 0)
        // Installer's outer shell fallback runs the original without its wrapper.
        try FileManager.default.removeItem(at: item.directory.appendingPathComponent("claude-wrapper"))
        let shell = Process(), stdin = Pipe(), stdout = Pipe()
        shell.executableURL = URL(fileURLWithPath: "/bin/sh"); shell.arguments = ["-c", item.installedCommand]
        shell.environment = childEnvironment()
        shell.standardInput = stdin; shell.standardOutput = stdout
        try shell.run(); try stdin.fileHandleForWriting.write(contentsOf: quota); try stdin.fileHandleForWriting.close()
        XCTAssertEqual(try stdout.fileHandleForReading.readToEnd(), quota)
        shell.waitUntilExit(); XCTAssertEqual(shell.terminationStatus, 0)
    }

    func testDelayedIngestAfterDisconnectCannotRecreateSnapshot() throws {
        let root = try temporary(), item = try preview(root)
        try ClaudeBridgeInstaller.apply(item)
        try ClaudeBridgeInstaller.restore(directory: item.directory, settingsURL: item.settingsURL)
        XCTAssertThrowsError(try ClaudeSnapshot.write(quota, directory: item.directory, receivedAt: now, requireInstallation: true))
        XCTAssertNil(try ClaudeSnapshotProvider(snapshotURL: item.directory.appendingPathComponent("claude-latest.json")).read())
    }

    func testOriginalCommandThatDoesNotReadStdinStillIngestsQuota() throws {
        let root = try temporary(), item = try preview(root), helper = try binary()
        try ClaudeBridgeInstaller.apply(item)
        let value = try launch(["wrap", "printf 'original-output'; exit 6", helper.path, item.directory.path], input: quota)
        XCTAssertEqual(value.0, Data("original-output".utf8))
        XCTAssertEqual(value.1, Data())
        XCTAssertEqual(value.2, 6)
        XCTAssertEqual(try ClaudeSnapshotProvider(snapshotURL: item.directory.appendingPathComponent("claude-latest.json")).read()?.weekly.usedPercent, 0)
    }

    func testOriginalEarlyExitWithHeldOpenStdinReturnsBoundedAndDoesNotIngestIncompleteInput() throws {
        let root = try temporary(), item = try preview(root)
        try ClaudeBridgeInstaller.apply(item)
        let child = Process(), stdin = Pipe(), stdout = Pipe(), stderr = Pipe()
        child.executableURL = try binary(); child.environment = childEnvironment()
        child.arguments = ["wrap", "printf 'original-output'; exit 8", try binary().path, item.directory.path]
        child.standardInput = stdin; child.standardOutput = stdout; child.standardError = stderr
        try child.run()
        defer { if child.isRunning { kill(child.processIdentifier, SIGKILL) }; try? stdin.fileHandleForWriting.close() }
        try stdin.fileHandleForWriting.write(contentsOf: quota)
        let deadline = Date().addingTimeInterval(2)
        while child.isRunning, Date() < deadline { usleep(10_000) }
        XCTAssertFalse(child.isRunning)
        if child.isRunning { kill(child.processIdentifier, SIGKILL) }
        child.waitUntilExit()
        XCTAssertEqual(child.terminationStatus, 8)
        XCTAssertEqual(try stdout.fileHandleForReading.readToEnd(), Data("original-output".utf8))
        XCTAssertNil(try ClaudeSnapshotProvider(snapshotURL: item.directory.appendingPathComponent("claude-latest.json")).read())
    }

    func testCancellationWithHeldOpenStdinKillsOriginalDescendant() throws {
        let root = try temporary(), marker = root.appendingPathComponent("child-pid")
        let child = Process(), stdin = Pipe(), stdout = Pipe(), stderr = Pipe()
        child.executableURL = try binary()
        child.environment = childEnvironment()
        let command = "sleep 30 & echo $! > " + ClaudeBridgeInstaller.shellQuote(marker.path) + "; wait"
        child.arguments = ["wrap", command, root.appendingPathComponent("missing").path, root.path]
        child.standardInput = stdin; child.standardOutput = stdout; child.standardError = stderr
        try child.run()
        defer { if child.isRunning { child.terminate() }; try? stdin.fileHandleForWriting.close() }
        let markerDeadline = Date().addingTimeInterval(3)
        while !FileManager.default.fileExists(atPath: marker.path), Date() < markerDeadline { usleep(10_000) }
        let pidText = String(decoding: try Data(contentsOf: marker), as: UTF8.self).trimmingCharacters(in: .whitespacesAndNewlines)
        let descendant = try XCTUnwrap(Int32(pidText))
        child.terminate()
        let deadline = Date().addingTimeInterval(3)
        while child.isRunning, Date() < deadline { usleep(10_000) }
        XCTAssertFalse(child.isRunning)
        if child.isRunning { kill(child.processIdentifier, SIGKILL) }
        child.waitUntilExit()
        XCTAssertEqual(child.terminationReason, .uncaughtSignal)
        XCTAssertEqual(child.terminationStatus, SIGTERM)
        let cleanupDeadline = Date().addingTimeInterval(2)
        while kill(descendant, 0) == 0, Date() < cleanupDeadline { usleep(10_000) }
        XCTAssertEqual(kill(descendant, 0), -1)
    }

    func testCancellationWhileOriginalInputPipeIsFullAndCommandIgnoresTERM() throws {
        let root = try temporary(), inputFile = root.appendingPathComponent("large-input")
        try write(Data(repeating: 65, count: 3 * 1_024 * 1_024), to: inputFile)
        let child = Process(), stdout = Pipe(), stderr = Pipe()
        let input = try FileHandle(forReadingFrom: inputFile)
        defer { try? input.close() }
        child.executableURL = try binary()
        child.environment = childEnvironment()
        child.arguments = ["wrap", "trap '' TERM; sleep 30", root.appendingPathComponent("missing").path, root.path]
        child.standardInput = input; child.standardOutput = stdout; child.standardError = stderr
        try child.run()
        usleep(100_000)
        child.terminate()
        let deadline = Date().addingTimeInterval(3)
        while child.isRunning, Date() < deadline { usleep(10_000) }
        XCTAssertFalse(child.isRunning)
        if child.isRunning { kill(child.processIdentifier, SIGKILL) }
        child.waitUntilExit()
        XCTAssertEqual(child.terminationReason, .uncaughtSignal)
        XCTAssertEqual(child.terminationStatus, SIGTERM)
    }
}

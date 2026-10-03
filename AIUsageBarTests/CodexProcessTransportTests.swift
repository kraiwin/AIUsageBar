import Darwin
import Foundation
import XCTest
@testable import AIUsageBar

/// Synthetic shell fixtures are test-only, never a production discovery fallback.
final class CodexProcessTransportTests: XCTestCase {
    func testFragmentedResponsesUnknownNotificationsAndRegistryPagination() throws {
        let script = #"""
        IFS= read -r request
        printf '{"id":1,"result":'
        printf '{"userAgent":"synthetic"}}\n'
        IFS= read -r notification
        IFS= read -r request
        printf '{"method":"future/notification","params":{}}\n'
        printf '{"id":2,"result":{"data":[{"name":"hooks","enabled":false,"stage":"stable"}],"nextCursor":"next"}}\n'
        IFS= read -r request
        printf '{"id":3,"result":{"data":[{"name":"plugins","enabled":false,"stage":"stable"},{"name":"code_mode_host","enabled":false,"stage":"stable"}],"nextCursor":null}}\n'
        IFS= read -r request
        printf '{"id":4,"result":{"config":{"notify":[],"analytics":{"enabled":false},"otel":{"exporter":"none","trace_exporter":"none"},"mcp_servers":{}}}}\n'
        IFS= read -r request
        printf '{"id":5,"result":{"rateLimits":{"limitId":"codex","primary":{"usedPercent":0,"windowDurationMins":10080,"resetsAt":2000000000}}}}\n'
        while IFS= read -r request; do :; done
        """#
        let channel = try fixture(script)
        defer { channel.closeAndReap() }
        try channel.initialize()
        try channel.registry()
        XCTAssertEqual(try channel.inventory(cwd: URL(fileURLWithPath: "/tmp"), requireDisabled: true), [])
        let quota = try channel.quota()
        XCTAssertEqual(quota.reading?.weekly.usedPercent, 0)
        XCTAssertNotNil(quota.reading?.providerObservedAt)
        XCTAssertTrue(channel.closeAndReap())
    }

    func testRegistryRejectsRepeatedCursorBeforeConfigOrQuota() throws {
        let script = #"""
        IFS= read -r request
        printf '{"id":1,"result":{"userAgent":"synthetic"}}\n'
        IFS= read -r notification
        IFS= read -r request
        printf '{"id":2,"result":{"data":[],"nextCursor":"same"}}\n'
        IFS= read -r request
        printf '{"id":3,"result":{"data":[],"nextCursor":"same"}}\n'
        while IFS= read -r request; do :; done
        """#
        let channel = try fixture(script)
        defer { channel.closeAndReap() }
        try channel.initialize()
        XCTAssertThrowsError(try channel.registry()) { XCTAssertEqual($0 as? CodexProviderError, .unsupportedConfiguration) }
    }

    func testUnsolicitedFutureResponseCannotSatisfyNextRequest() throws {
        for suffix in [
            #"{"id":2,"result":{"data":[{"name":"hooks","enabled":false,"stage":"stable"},{"name":"plugins","enabled":false,"stage":"stable"},{"name":"code_mode_host","enabled":false,"stage":"stable"}],"nextCursor":null}}"# + "\n",
            #"{"id":2,"result":{}"#
        ] {
            // printf sends the handshake and unsolicited future response in one
            // write. A partial future frame is completed after registry is sent.
            let output = #"{"id":1,"result":{"userAgent":"synthetic"}}"# + "\n" + suffix
            let quoted = output.replacingOccurrences(of: "'", with: "'\\''")
            let script = "IFS= read -r request\nprintf '%s' '\(quoted)'\nIFS= read -r notification\nIFS= read -r request\nprintf '}\\n'\nwhile IFS= read -r request; do :; done"
            let channel = try fixture(script)
            defer { channel.closeAndReap() }
            try channel.initialize()
            XCTAssertThrowsError(try channel.registry()) { XCTAssertEqual($0 as? CodexProviderError, .unexpectedResponse) }
        }
    }

    func testEOFAndTimeoutTerminateAndReapChild() throws {
        for (script, expected) in [("exit 0", CodexProviderError.childExited), ("sleep 30", .timeout)] {
            let channel = try fixture(script, timeout: 0.1)
            XCTAssertThrowsError(try channel.initialize()) { XCTAssertEqual($0 as? CodexProviderError, expected) }
            XCTAssertTrue(channel.closeAndReap())
            XCTAssertTrue(channel.closeAndReap())
        }
    }

    func testCancellationTerminatesBlockedChild() throws {
        let control = CodexOperationControl()
        let channel = try fixture("sleep 30", control: control)
        DispatchQueue.global().asyncAfter(deadline: .now() + 0.1) { control.cancel() }
        XCTAssertThrowsError(try channel.initialize()) { XCTAssertEqual($0 as? CodexProviderError, .cancelled) }
        XCTAssertTrue(channel.closeAndReap())
    }

    func testDescendantHoldingPipesIsTerminatedWithoutTouchingUnrelatedProcess() throws {
        let marker = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: marker) }
        let unrelated = Process()
        unrelated.executableURL = URL(fileURLWithPath: "/bin/sleep")
        unrelated.arguments = ["30"]
        try unrelated.run()
        defer { if unrelated.isRunning { unrelated.terminate() }; unrelated.waitUntilExit() }
        let channel = try fixture("sleep 30 &\necho $! > '\(marker.path)'\nexit 0", timeout: 0.1)
        XCTAssertThrowsError(try channel.initialize())
        let descendant = try XCTUnwrap(Int32(String(contentsOf: marker, encoding: .utf8).trimmingCharacters(in: .whitespacesAndNewlines)))
        XCTAssertTrue(channel.closeAndReap())
        let deadline = Date().addingTimeInterval(2)
        while kill(descendant, 0) == 0 && Date() < deadline { Thread.sleep(forTimeInterval: 0.01) }
        XCTAssertEqual(kill(descendant, 0), -1)
        XCTAssertEqual(errno, ESRCH)
        XCTAssertTrue(unrelated.isRunning)
    }

    func testStderrIsDrainedWithoutBeingParsedAsJSON() throws {
        let script = #"""
        IFS= read -r request
        i=0
        while [ "$i" -lt 2000 ]; do printf 'synthetic diagnostic bytes that are not JSON\n' >&2; i=$((i+1)); done
        printf '{"id":1,"result":{"userAgent":"synthetic"}}\n'
        while IFS= read -r request; do :; done
        """#
        let channel = try fixture(script)
        defer { channel.closeAndReap() }
        try channel.initialize()
        XCTAssertTrue(channel.closeAndReap())
    }

    func testOversizedUnterminatedOutputFailsWithinBound() throws {
        let script = #"""
        IFS= read -r request
        while :; do printf 'xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx'; done
        """#
        let channel = try fixture(script)
        XCTAssertThrowsError(try channel.initialize()) { XCTAssertEqual($0 as? CodexProviderError, .outputLimit) }
        XCTAssertTrue(channel.closeAndReap())
    }

    func testStrictEnvelopeRejectsRequestsAndTypeConfusedIDs() {
        for (json, expected) in [
            (#"{"id":true,"result":{}}"#, CodexProviderError.unexpectedResponse),
            (#"{"id":2,"result":{}}"#, .unexpectedResponse),
            (#"{"id":9,"method":"future/request","params":{}}"#, .serverRequest),
            (#"{"method":"account/chatgptAuthTokens/refresh","params":{}}"#, .serverRequest),
            (#"{"method":"future/notification","params":false}"#, .invalidEnvelope),
            (#"{"id":1,"result":{},"error":{}}"#, .invalidEnvelope)
        ] {
            XCTAssertThrowsError(try CodexRPCEnvelope.result(Data(json.utf8), expectedID: 1)) {
                XCTAssertEqual($0 as? CodexProviderError, expected)
            }
        }
    }

    private func fixture(_ script: String, timeout: TimeInterval = 5,
                         control: CodexOperationControl = CodexOperationControl()) throws -> CodexProcessTransport {
        try CodexProcessTransport(executable: URL(fileURLWithPath: "/bin/sh"), arguments: ["-c", script],
            cwd: FileManager.default.temporaryDirectory,
            deadline: DispatchTime.now().uptimeNanoseconds + UInt64(timeout * 1_000_000_000), control: control)
    }
}

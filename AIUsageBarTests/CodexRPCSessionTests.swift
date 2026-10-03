import Foundation
import XCTest
@testable import AIUsageBar

/// Only synthetic RPC data; these tests never launch a CLI or read an account.
final class CodexRPCSessionTests: XCTestCase {
    private let start = Date(timeIntervalSince1970: 1_800_000_000)
    private var initializedAt: Date { start.addingTimeInterval(1) }
    private var receipt: Date { start.addingTimeInterval(2) }
    private let handshake = #"{"id":1,"result":{"userAgent":"synthetic-test"}}"#
    private let quota = #"{"id":2,"result":{"rateLimits":{"limitId":"codex","primary":{"usedPercent":42,"windowDurationMins":10080,"resetsAt":1800001000}}}}"#

    func testOutboundContractSendsOnlyHandshakeAndQuotaRead() throws {
        var session = CodexRPCSession()
        let initial = try object(session.start(at: start))
        XCTAssertEqual(initial["method"] as? String, "initialize")
        XCTAssertEqual(initial["id"] as? Int, 1)
        let params = try XCTUnwrap(initial["params"] as? [String: Any])
        XCTAssertNil(params["capabilities"])
        let update = try session.receive(line(handshake), receivedAt: initializedAt)
        XCTAssertNil(update.completion)
        XCTAssertEqual(update.outbound.count, 2)
        let notification = try object(update.outbound[0])
        XCTAssertEqual(notification["method"] as? String, "initialized")
        XCTAssertNil(notification["id"])
        let read = try object(update.outbound[1])
        XCTAssertEqual(read["method"] as? String, "account/rateLimits/read")
        XCTAssertEqual(read["id"] as? Int, 2)
        XCTAssertNil(read["params"])
        XCTAssertTrue(update.outbound.allSatisfy { $0.last == 10 })
    }

    func testByteByByteFragmentsProduceOneCompletion() throws {
        var session = CodexRPCSession()
        _ = try session.start(at: start)
        var handshakeWrites: [Data] = []
        for byte in line(handshake) {
            let update = try session.receive(Data([byte]), receivedAt: initializedAt)
            handshakeWrites += update.outbound
            XCTAssertNil(update.completion)
        }
        XCTAssertEqual(handshakeWrites.count, 2)
        var completions: [CodexRPCSession.Completion] = []
        for byte in line(quota) {
            let update = try session.receive(Data([byte]), receivedAt: receipt,
                                             quotaFreshness: .verifiedFreshFetch)
            XCTAssertTrue(update.outbound.isEmpty)
            if let completion = update.completion { completions.append(completion) }
        }
        XCTAssertEqual(completions.count, 1)
        let reading = try XCTUnwrap(completions.first?.reading)
        XCTAssertEqual(reading.weekly.usedPercent, 42)
        XCTAssertEqual(reading.receivedAt, receipt)
        XCTAssertEqual(reading.providerObservedAt, initializedAt)
        XCTAssertEqual(UsageFreshness.evaluate(reading, now: receipt,
                                              policy: try UsageFreshnessPolicy(maxAge: 300)), .fresh)
    }

    func testUnverifiedOrCachedResponseNeverBecomesFresh() throws {
        var session = try ready()
        let reading = try XCTUnwrap(session.receive(line(quota), receivedAt: receipt).completion?.reading)
        XCTAssertNil(reading.providerObservedAt)
        XCTAssertEqual(UsageFreshness.evaluate(reading, now: receipt,
                                              policy: try UsageFreshnessPolicy(maxAge: 300)), .snapshot)
    }

    func testNotificationsAreIgnoredWithoutUsingTheirQuotaOrAccountFields() throws {
        var session = try ready()
        let notifications = #"{"method":"account/rateLimits/updated","params":{"rateLimits":{"primary":{"usedPercent":99}}}}"# + "\n" +
            #"{"method":"account/updated","params":{"authMode":null}}"# + "\n" +
            #"{"method":"mcpServer/startupStatus/updated","params":{"status":"ready"}}"# + "\n" +
            #"{"method":"unrecognized/notification","params":{}}"# + "\n"
        let ignored = try session.receive(Data(notifications.utf8), receivedAt: receipt)
        XCTAssertTrue(ignored.outbound.isEmpty)
        XCTAssertNil(ignored.completion)
        let result = try session.receive(line(quota), receivedAt: receipt)
        XCTAssertEqual(result.completion?.reading?.weekly.usedPercent, 42)
    }

    func testQuotaAndTrailingNotificationsInOneChunkMatchSeparateReceipt() throws {
        let notification = line(#"{"method":"account/rateLimits/updated","params":{"rateLimits":null}}"#)
        var joined = try ready()
        let joinedResult = try joined.receive(line(quota) + notification, receivedAt: receipt,
                                               quotaFreshness: .verifiedFreshFetch)
        var separate = try ready()
        let separateResult = try separate.receive(line(quota), receivedAt: receipt,
                                                   quotaFreshness: .verifiedFreshFetch)
        XCTAssertEqual(joinedResult, separateResult)
        try joined.finish()
        // Transport closes on completion; every bounded remainder is dropped.
        for remainder in [Data(notification.prefix(8)), line("malformed"), line(quota),
                          line(#"{"id":9,"method":"account/chatgptAuthTokens/refresh","params":{}}"#)] {
            var trailing = try ready()
            XCTAssertEqual(try trailing.receive(line(quota) + remainder, receivedAt: receipt,
                                                quotaFreshness: .verifiedFreshFetch), separateResult)
        }
    }

    func testIDsAreCorrelatedWithExactExpectedRequest() throws {
        for id in ["true", "false", "null", "\"1\"", "1.5", "2", "[]", "{}", "2147483648"] {
            var session = CodexRPCSession()
            _ = try session.start(at: start)
            assertFailure(&session, json: "{\"id\":\(id),\"result\":{\"userAgent\":\"test\"}}",
                          error: .unexpectedResponse, at: initializedAt)
        }
        for id in ["1", "true", "\"2\"", "3"] {
            var session = try ready()
            assertFailure(&session, json: "{\"id\":\(id),\"result\":{}}",
                          error: .unexpectedResponse, at: receipt)
        }
        var unsolicited = CodexRPCSession()
        _ = try unsolicited.start(at: start)
        assertFailure(&unsolicited, json: handshake + "\n" + quota,
                      error: .unexpectedResponse, at: initializedAt)
    }

    func testServerRequestsAndDisguisedRefreshTerminateWithoutReply() throws {
        for json in [
            #"{"id":9,"method":"account/chatgptAuthTokens/refresh","params":{"reason":"unauthorized"}}"#,
            #"{"method":"account/chatgptAuthTokens/refresh","params":{}}"#,
            #"{"id":9,"method":"item/commandExecution/requestApproval","params":{}}"#,
            #"{"id":null,"method":"account/updated","params":{}}"#
        ] {
            var session = try ready()
            assertFailure(&session, json: json, error: .serverRequest, at: receipt)
        }
    }

    func testMalformedJSONAndEnvelopeAreTerminal() throws {
        for json in ["{", ""] {
            var session = try ready()
            assertFailure(&session, json: json, error: .malformedJSON, at: receipt)
        }
        // Foundation accepts a trailing comma, but the missing result/error still
        // makes this an invalid RPC envelope and terminates safely.
        for json in ["[]", "{\"id\":2,}", #"{"id":2,"result":null}"#,
                     #"{"id":2,"result":{},"error":{"code":1,"message":"synthetic"}}"#,
                     #"{"id":2,"result":{},"params":{}}"#,
                     #"{"method":false,"params":{}}"#,
                     #"{"method":"unrecognized/notification","params":false}"#] {
            var session = try ready()
            assertFailure(&session, json: json, error: .invalidEnvelope, at: receipt)
        }
        var session = try ready()
        XCTAssertThrowsError(try session.receive(Data([255, 10]), receivedAt: receipt)) {
            XCTAssertEqual($0 as? CodexRPCError, .malformedJSON)
        }
    }

    func testHandshakeSchemaMustBeValid() throws {
        for result in ["{}", "{\"userAgent\":null}", "{\"userAgent\":false}", "{\"userAgent\":\"\"}"] {
            var session = CodexRPCSession()
            _ = try session.start(at: start)
            assertFailure(&session, json: "{\"id\":1,\"result\":\(result)}",
                          error: .invalidHandshake, at: initializedAt)
        }
    }

    func testRPCErrorIsRedactedAndErrorCodeCannotBeBoolean() throws {
        var session = try ready()
        assertFailure(&session, json: #"{"id":2,"error":{"code":-32001,"message":"synthetic-private-message","data":{"syntheticPrivateField":"fixture"}}}"#,
                      error: .serverError(code: -32001), at: receipt)
        XCTAssertFalse(String(describing: CodexRPCError.serverError(code: -32001)).contains("synthetic-private"))
        for code in ["true", "null", "\"-32001\"", "1.5"] {
            var session = try ready()
            assertFailure(&session, json: "{\"id\":2,\"error\":{\"code\":\(code),\"message\":\"test\"}}",
                          error: .invalidEnvelope, at: receipt)
        }
    }

    func testMalformedQuotaIsRedactedAndMissingWeeklyIsExplicitNoData() throws {
        var invalid = try ready()
        assertFailure(&invalid, json: #"{"id":2,"result":{"rateLimits":{"primary":{"usedPercent":true,"windowDurationMins":10080,"resetsAt":1800001000}}}}"#,
                      error: .invalidQuota, at: receipt)
        var missingKeys = try ready()
        assertFailure(&missingKeys, json: #"{"id":2,"result":{}}"#,
                      error: .invalidQuota, at: receipt)
        for quotaFields in [#"{"rateLimits":null}"#, #"{"rateLimitsByLimitId":{}}"#,
                            #"{"rateLimitsByLimitId":null}"#] {
            var missingWeekly = try ready()
            let update = try missingWeekly.receive(line("{\"id\":2,\"result\":\(quotaFields)}"), receivedAt: receipt)
            XCTAssertNotNil(update.completion)
            XCTAssertNil(update.completion?.reading)
        }
    }

    func testLineByteLimitAcrossFragmentsAndTotalChunkLimit() throws {
        var fragmented = CodexRPCSession(limits: try .init(lineBytes: 8, totalBytes: 64, lineCount: 4))
        _ = try fragmented.start(at: start)
        _ = try fragmented.receive(Data("12345678".utf8), receivedAt: initializedAt)
        XCTAssertThrowsError(try fragmented.receive(Data([32]), receivedAt: initializedAt)) {
            XCTAssertEqual($0 as? CodexRPCError, .byteLimit)
        }
        var oversized = CodexRPCSession(limits: try .init(lineBytes: 8, totalBytes: 8, lineCount: 4))
        _ = try oversized.start(at: start)
        XCTAssertThrowsError(try oversized.receive(Data(repeating: 32, count: 9), receivedAt: initializedAt)) {
            XCTAssertEqual($0 as? CodexRPCError, .byteLimit)
        }
        var cumulative = CodexRPCSession(limits: try .init(lineBytes: 64, totalBytes: 80, lineCount: 4))
        _ = try cumulative.start(at: start)
        _ = try cumulative.receive(line(handshake), receivedAt: initializedAt)
        XCTAssertThrowsError(try cumulative.receive(Data(repeating: 32, count: 40), receivedAt: receipt)) {
            XCTAssertEqual($0 as? CodexRPCError, .byteLimit)
        }
    }

    func testLineCountLimitsNotificationFlood() throws {
        var session = CodexRPCSession(limits: try .init(lineBytes: 128, totalBytes: 512, lineCount: 2))
        _ = try session.start(at: start)
        _ = try session.receive(line(handshake), receivedAt: initializedAt)
        let notification = line(#"{"method":"account/updated","params":{}}"#)
        _ = try session.receive(notification, receivedAt: receipt)
        XCTAssertThrowsError(try session.receive(notification, receivedAt: receipt)) {
            XCTAssertEqual($0 as? CodexRPCError, .lineLimit)
        }
    }

    func testEOFDoesNotParseUnterminatedQuotaAndCannotRestartFailure() throws {
        for partial in [Data(), Data(quota.utf8)] {
            var session = try ready()
            let update = try session.receive(partial, receivedAt: receipt)
            XCTAssertNil(update.completion)
            XCTAssertThrowsError(try session.finish()) {
                XCTAssertEqual($0 as? CodexRPCError, .unexpectedEOF)
            }
            XCTAssertThrowsError(try session.start(at: start))
            XCTAssertThrowsError(try session.receive(line(quota), receivedAt: receipt)) {
                XCTAssertEqual($0 as? CodexRPCError, .invalidState)
            }
        }
    }

    func testInvalidOrBackwardTimestampsCannotEstablishFreshness() throws {
        for date in [start.addingTimeInterval(-1), Date(timeIntervalSince1970: .nan),
                     Date(timeIntervalSince1970: .infinity)] {
            var session = CodexRPCSession()
            _ = try session.start(at: start)
            XCTAssertThrowsError(try session.receive(line(handshake), receivedAt: date)) {
                XCTAssertEqual($0 as? CodexRPCError, .invalidTimestamp)
            }
        }
        var session = try ready()
        XCTAssertThrowsError(try session.receive(line(quota), receivedAt: start,
                                                 quotaFreshness: .verifiedFreshFetch)) {
            XCTAssertEqual($0 as? CodexRPCError, .invalidTimestamp)
        }
    }

    func testRepeatedCompletionCannotReturnOrMutatePriorSuccess() throws {
        var session = try ready()
        let completed = try session.receive(line(quota), receivedAt: receipt)
        XCTAssertThrowsError(try session.receive(line(quota), receivedAt: receipt)) {
            XCTAssertEqual($0 as? CodexRPCError, .alreadyFinished)
        }
        try session.finish()
        XCTAssertEqual(completed.completion?.reading?.weekly.usedPercent, 42)
    }

    func testInvalidLimitsAndReceiveBeforeStartAreRejected() throws {
        XCTAssertThrowsError(try CodexRPCSession.Limits(lineBytes: 0))
        XCTAssertThrowsError(try CodexRPCSession.Limits(totalBytes: 262_145))
        XCTAssertThrowsError(try CodexRPCSession.Limits(lineCount: 129))
        var session = CodexRPCSession()
        XCTAssertThrowsError(try session.receive(line(handshake), receivedAt: start)) {
            XCTAssertEqual($0 as? CodexRPCError, .invalidState)
        }
    }

    private func ready() throws -> CodexRPCSession {
        var session = CodexRPCSession()
        _ = try session.start(at: start)
        _ = try session.receive(line(handshake), receivedAt: initializedAt)
        return session
    }

    private func line(_ json: String) -> Data { Data((json + "\n").utf8) }

    private func object(_ data: Data) throws -> [String: Any] {
        try XCTUnwrap(JSONSerialization.jsonObject(with: data) as? [String: Any])
    }

    private func assertFailure(_ session: inout CodexRPCSession, json: String,
                               error: CodexRPCError, at date: Date,
                               file: StaticString = #filePath, line testLine: UInt = #line) {
        XCTAssertThrowsError(try session.receive(line(json), receivedAt: date), file: file, line: testLine) {
            XCTAssertEqual($0 as? CodexRPCError, error, file: file, line: testLine)
        }
        XCTAssertThrowsError(try session.receive(line(quota), receivedAt: receipt), file: file, line: testLine) {
            XCTAssertEqual($0 as? CodexRPCError, .invalidState, file: file, line: testLine)
        }
    }
}

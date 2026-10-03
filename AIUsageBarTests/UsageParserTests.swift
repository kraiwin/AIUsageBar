import Foundation
import XCTest
@testable import AIUsageBar

/// Entirely synthetic quota payloads, without account identifiers or credentials.
final class UsageParserTests: XCTestCase {
    private let receipt = Date(timeIntervalSince1970: 1_800_000_000)

    func testCodexRecognizesWeeklyInEitherSlotAndSession() throws {
        for weeklyKey in ["primary", "secondary"] {
            let otherKey = weeklyKey == "primary" ? "secondary" : "primary"
            let result = try codex("{\"rateLimits\":{\"\(weeklyKey)\":\(window()),\"\(otherKey)\":\(window(duration: "300", percent: "25"))}}")
            XCTAssertEqual(result?.weekly.usedPercent, 42.75)
            XCTAssertEqual(result?.session?.usedPercent, 25)
            XCTAssertEqual(result?.provider, .codex)
            XCTAssertEqual(result?.source, .codexAppServer)
            XCTAssertEqual(result?.receivedAt, receipt)
            XCTAssertNil(result?.providerObservedAt)
        }
    }

    func testCodexSelectsCodexBucketOverUnrelatedAndLegacyQuota() throws {
        let result = try codex("{\"rateLimits\":{\"primary\":\(window(percent: "99"))},\"rateLimitsByLimitId\":{\"other\":{\"primary\":\(window(percent: "88"))},\"codex\":{\"primary\":\(window(percent: "7"))}}}")
        XCTAssertEqual(result?.weekly.usedPercent, 7)
        XCTAssertNil(try codex("{\"rateLimits\":{\"primary\":\(window())},\"rateLimitsByLimitId\":{\"other\":{\"primary\":\(window())}}}"))
    }

    func testCodexLegacyExplicitOtherProductIsNotCodexUsage() throws {
        XCTAssertNil(try codex("{\"rateLimits\":{\"limitId\":\"codex_other\",\"primary\":\(window())}}"))
        for identity in ["null", "\"codex\""] {
            XCTAssertEqual(try codex("{\"rateLimits\":{\"limitId\":\(identity),\"primary\":\(window())}}")?.weekly.usedPercent, 42.75)
        }
    }

    func testCodexExplicitLimitIdentityMustBeString() {
        for identity in ["true", "42", "[]", "{}"] {
            assertCodexError("{\"rateLimits\":{\"limitId\":\(identity),\"primary\":\(window())}}", field: "limitId")
            assertCodexError("{\"rateLimitsByLimitId\":{\"codex\":{\"limitId\":\(identity),\"primary\":\(window())}}}", field: "limitId")
        }
    }

    func testCodexMappedBucketRejectsConflictingExplicitIdentity() throws {
        assertCodexError("{\"rateLimitsByLimitId\":{\"codex\":{\"limitId\":\"codex_other\",\"primary\":\(window())}}}", field: "limitId")
        for identity in ["null", "\"codex\""] {
            XCTAssertEqual(try codex("{\"rateLimitsByLimitId\":{\"codex\":{\"limitId\":\(identity),\"primary\":\(window())}}}")?.weekly.usedPercent, 42.75)
        }
    }

    func testCodexMissingOrUnsupportedWeeklyNeverBecomesZero() throws {
        for json in ["{}", "{\"rateLimits\":null}", "{\"rateLimits\":{}}",
                     "{\"rateLimitsByLimitId\":{}}",
                     "{\"rateLimits\":{\"secondary\":\(window(duration: "300"))}}",
                     "{\"rateLimits\":{\"secondary\":\(window(duration: "1440"))}}",
                     "{\"rateLimits\":{\"secondary\":{\"usedPercent\":20}}}"] {
            XCTAssertNil(try codex(json))
        }
    }

    func testCodexMalformedWeeklyPercentagesThrow() {
        for percent in ["null", "true", "false", "\"12\"", "-0.01", "100.01", "[]", "{}"] {
            assertCodexError("{\"rateLimits\":{\"primary\":\(window(percent: percent))}}", field: "usedPercent")
        }
        assertCodexError("{\"rateLimits\":{\"primary\":{\"windowDurationMins\":10080,\"resetsAt\":1800001000}}}", field: "usedPercent")
    }

    func testCodexMalformedResetsThrow() {
        for reset in ["null", "true", "\"1800001000\"", "-1", "0", "253402300800", "[]"] {
            assertCodexError("{\"rateLimits\":{\"primary\":\(window(reset: reset))}}", field: "resetsAt")
        }
        assertCodexError("{\"rateLimits\":{\"primary\":{\"windowDurationMins\":10080,\"usedPercent\":20}}}", field: "resetsAt")
    }

    func testCodexMalformedContainerAndDurationThrow() {
        for field in ["rateLimits", "rateLimitsByLimitId"] {
            assertCodexError("{\"\(field)\":[]}", field: field)
        }
        assertCodexError("{\"rateLimitsByLimitId\":{\"codex\":false}}", field: "codex")
        assertCodexError("{\"rateLimits\":{\"primary\":false}}", field: "primary")
        for duration in ["true", "\"10080\"", "-1", "0", "10080.5"] {
            assertCodexError("{\"rateLimits\":{\"primary\":\(window(duration: duration))}}", field: "windowDurationMins")
        }
    }

    func testDuplicateWeeklyIsRejectedRatherThanChoosingArbitrarily() {
        assertCodexError("{\"rateLimits\":{\"primary\":\(window()),\"secondary\":\(window())}}", field: "weekly")
        assertCodexError("{\"rateLimits\":{\"primary\":\(window(duration: "300")),\"secondary\":\(window(duration: "300"))}}", field: "session")
    }

    func testClaudeWeeklyAndOptionalSession() throws {
        let result = try claude("{\"rate_limits\":{\"seven_day\":\(claudeWindow()),\"five_hour\":\(claudeWindow(percent: "100"))}}")
        XCTAssertEqual(result?.weekly.usedPercent, 42.75)
        XCTAssertEqual(result?.session?.remainingPercent, 0)
        XCTAssertEqual(result?.provider, .claude)
        XCTAssertEqual(result?.source, .claudeStatusLine)
        XCTAssertNil(result?.providerObservedAt)
    }

    func testClaudeMissingOptionalWindowsReturnNoData() throws {
        for json in ["{}", "{\"rate_limits\":null}", "{\"rate_limits\":{}}",
                     "{\"rate_limits\":{\"seven_day\":null}}",
                     "{\"rate_limits\":{\"five_hour\":\(claudeWindow())}}"] {
            XCTAssertNil(try claude(json))
        }
        XCTAssertNil(try claude("{\"rate_limits\":{\"seven_day\":\(claudeWindow()),\"five_hour\":null}}")?.session)
    }

    func testClaudeMalformedWeeklyAndOptionalSessionThrow() {
        for percent in ["null", "true", "false", "\"12\"", "-1", "101", "[]"] {
            assertClaudeError("{\"rate_limits\":{\"seven_day\":\(claudeWindow(percent: percent))}}", field: "used_percentage")
        }
        for reset in ["null", "true", "\"date\"", "0", "-1", "253402300800"] {
            assertClaudeError("{\"rate_limits\":{\"seven_day\":\(claudeWindow(reset: reset))}}", field: "resets_at")
        }
        assertClaudeError("{\"rate_limits\":{\"seven_day\":{},\"five_hour\":null}}", field: "used_percentage")
        assertClaudeError("{\"rate_limits\":{\"seven_day\":\(claudeWindow()),\"five_hour\":{}}}", field: "used_percentage")
        assertClaudeError("{\"rate_limits\":false}", field: "rate_limits")
        assertClaudeError("{\"rate_limits\":{\"seven_day\":[]}}", field: "seven_day")
    }

    func testParserAcceptsPastResetsAndBoundaryPercentages() throws {
        for percent in ["0", "100", "0.125"] {
            let a = try codex("{\"rateLimits\":{\"primary\":\(window(percent: percent, reset: "1"))}}")
            let b = try claude("{\"rate_limits\":{\"seven_day\":\(claudeWindow(percent: percent, reset: "1"))}}")
            XCTAssertEqual(a?.weekly.usedPercent, Double(percent))
            XCTAssertEqual(b?.weekly.resetsAt, Date(timeIntervalSince1970: 1))
        }
    }

    func testInvalidJSONHasSanitizedTypedError() {
        for input in ["not JSON", "[]", "null", "{\"secretSynthetic\":NaN}"] {
            XCTAssertThrowsError(try codex(input)) {
                XCTAssertEqual($0 as? UsagePayloadError, .invalidJSON)
            }
            XCTAssertThrowsError(try claude(input)) {
                XCTAssertEqual($0 as? UsagePayloadError, .invalidJSON)
            }
        }
    }

    private func codex(_ json: String) throws -> UsageReading? {
        try UsagePayloadParser.parseCodex(Data(json.utf8), receivedAt: receipt)
    }

    private func claude(_ json: String) throws -> UsageReading? {
        try UsagePayloadParser.parseClaude(Data(json.utf8), receivedAt: receipt)
    }

    private func window(duration: String = "10080", percent: String = "42.75",
                        reset: String = "1800001000") -> String {
        "{\"windowDurationMins\":\(duration),\"usedPercent\":\(percent),\"resetsAt\":\(reset)}"
    }

    private func claudeWindow(percent: String = "42.75", reset: String = "1800001000") -> String {
        "{\"used_percentage\":\(percent),\"resets_at\":\(reset)}"
    }

    private func assertCodexError(_ json: String, field: String,
                                 file: StaticString = #filePath, line: UInt = #line) {
        XCTAssertThrowsError(try codex(json), file: file, line: line) {
            XCTAssertEqual($0 as? UsagePayloadError, .invalidField(field), file: file, line: line)
        }
    }

    private func assertClaudeError(_ json: String, field: String,
                                  file: StaticString = #filePath, line: UInt = #line) {
        XCTAssertThrowsError(try claude(json), file: file, line: line) {
            XCTAssertEqual($0 as? UsagePayloadError, .invalidField(field), file: file, line: line)
        }
    }
}

import Foundation
import XCTest
@testable import AIUsageBar

final class UsageModelTests: XCTestCase {
    private let now = Date(timeIntervalSince1970: 1_800_000_000)

    func testPercentageBoundariesAndDecimals() throws {
        for percent in [0.0, 100.0, 42.75] {
            let quota = try QuotaWindow(usedPercent: percent, resetsAt: now)
            XCTAssertEqual(quota.usedPercent, percent)
            XCTAssertEqual(quota.remainingPercent, 100 - percent)
        }
    }

    func testRejectsNonfiniteAndOutOfRangePercentage() {
        for percent in [Double.nan, .infinity, -.infinity, -0.01, 100.01] {
            XCTAssertThrowsError(try QuotaWindow(usedPercent: percent, resetsAt: now)) {
                XCTAssertEqual($0 as? UsageValidationError, .invalidPercentage)
            }
        }
    }

    func testRejectsInvalidResetDate() {
        for epoch in [Double.nan, .infinity, -.infinity, -1, 0, 253_402_300_800] {
            XCTAssertThrowsError(try QuotaWindow(usedPercent: 0,
                resetsAt: Date(timeIntervalSince1970: epoch))) {
                XCTAssertEqual($0 as? UsageValidationError, .invalidResetTime)
            }
        }
    }

    func testPastResetIsAcceptedAndClassifiedExpired() throws {
        let reading = try reading(reset: now.addingTimeInterval(-1))
        XCTAssertEqual(UsageFreshness.evaluate(reading, now: now,
            policy: try UsageFreshnessPolicy(maxAge: 300)), .expired)
    }

    func testResetAtNowIsExpired() throws {
        XCTAssertEqual(UsageFreshness.evaluate(try reading(reset: now), now: now,
            policy: try UsageFreshnessPolicy(maxAge: 300)), .expired)
    }

    func testUnknownObservationIsSnapshotEvenWhenJustReceived() throws {
        XCTAssertEqual(UsageFreshness.evaluate(try reading(), now: now,
            policy: try UsageFreshnessPolicy(maxAge: 300)), .snapshot)
    }

    func testFreshnessUsesObservationTimeRatherThanReceivedTime() throws {
        let policy = try UsageFreshnessPolicy(maxAge: 300)
        XCTAssertEqual(UsageFreshness.evaluate(try reading(observed: now.addingTimeInterval(-300)),
            now: now, policy: policy), .fresh)
        XCTAssertEqual(UsageFreshness.evaluate(try reading(observed: now.addingTimeInterval(-301)),
            now: now, policy: policy), .stale)
    }

    func testFutureAndNonfiniteTimestampsAreInvalid() throws {
        let policy = try UsageFreshnessPolicy(maxAge: 300)
        for invalid in [now.addingTimeInterval(1), Date(timeIntervalSince1970: .nan),
                        Date(timeIntervalSince1970: 0), Date(timeIntervalSince1970: -1)] {
            XCTAssertEqual(UsageFreshness.evaluate(try reading(observed: invalid),
                now: now, policy: policy), .invalidTimestamp)
            XCTAssertEqual(UsageFreshness.evaluate(try reading(received: invalid),
                now: now, policy: policy), .invalidTimestamp)
        }
        XCTAssertEqual(UsageFreshness.evaluate(try reading(),
            now: Date(timeIntervalSince1970: .infinity), policy: policy), .invalidTimestamp)
    }

    func testObservationCannotBeLaterThanReceipt() throws {
        XCTAssertEqual(UsageFreshness.evaluate(try reading(observed: now,
            received: now.addingTimeInterval(-1)), now: now,
            policy: try UsageFreshnessPolicy(maxAge: 300)), .invalidTimestamp)
    }

    func testExpiredSessionDoesNotExpireWeeklyQuota() throws {
        let reading = UsageReading(provider: .codex,
            weekly: try QuotaWindow(usedPercent: 12, resetsAt: now.addingTimeInterval(1000)),
            session: try QuotaWindow(usedPercent: 50, resetsAt: now.addingTimeInterval(-1)),
            source: .codexAppServer, receivedAt: now, providerObservedAt: now)
        XCTAssertEqual(UsageFreshness.evaluate(reading, now: now,
            policy: try UsageFreshnessPolicy(maxAge: 300)), .fresh)
    }

    func testFreshnessPolicyRejectsInvalidIntervals() throws {
        for interval in [Double.nan, .infinity, -.infinity, -1] {
            XCTAssertThrowsError(try UsageFreshnessPolicy(maxAge: interval))
        }
        XCTAssertEqual(try UsageFreshnessPolicy(maxAge: 0).maxAge, 0)
    }

    private func reading(reset: Date? = nil, observed: Date? = nil,
                         received: Date? = nil) throws -> UsageReading {
        UsageReading(provider: .claude,
            weekly: try QuotaWindow(usedPercent: 12.5,
                resetsAt: reset ?? now.addingTimeInterval(1000)),
            source: .claudeStatusLine, receivedAt: received ?? now, providerObservedAt: observed)
    }
}

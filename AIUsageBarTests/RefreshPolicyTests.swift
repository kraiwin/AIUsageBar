import Foundation
import XCTest
@testable import AIUsageBar

/// Synthetic clocks only. No CLI, account or shared UserDefaults access.
final class RefreshPolicyTests: XCTestCase {
    private let start = Date(timeIntervalSince1970: 1_800_000_000)

    func testFirstAttemptStartsImmediatelyAndConsumesCooldown() {
        var policy = RefreshPolicy()
        XCTAssertEqual(policy.remaining(at: start), 0)
        XCTAssertNil(policy.lastAttempt)
        XCTAssertTrue(policy.begin(at: start))
        XCTAssertEqual(policy.lastAttempt, start)
        XCTAssertEqual(policy.remaining(at: start), 60)
        XCTAssertFalse(policy.begin(at: start))
    }

    func testManualWakeAndRetryAttemptsCannotBypassOneMinuteBoundary() {
        var policy = RefreshPolicy()
        XCTAssertTrue(policy.begin(at: start))
        for elapsed in [0.0, 1, 20, 59, 59.999] {
            let now = start.addingTimeInterval(elapsed)
            XCTAssertFalse(policy.begin(at: now))
            XCTAssertEqual(policy.lastAttempt, start)
            XCTAssertEqual(policy.remaining(at: now), 60 - elapsed, accuracy: 0.00001)
        }
        XCTAssertTrue(policy.begin(at: start.addingTimeInterval(60)))
        XCTAssertEqual(policy.lastAttempt, start.addingTimeInterval(60))
    }

    func testRejectedAttemptDoesNotExtendCooldown() {
        var policy = RefreshPolicy(lastAttempt: start)
        XCTAssertFalse(policy.begin(at: start.addingTimeInterval(59)))
        XCTAssertEqual(policy.remaining(at: start.addingTimeInterval(60)), 0)
        XCTAssertTrue(policy.begin(at: start.addingTimeInterval(60)))
    }

    func testRelaunchRetainsCooldownFromPersistedAttempt() {
        var original = RefreshPolicy()
        XCTAssertTrue(original.begin(at: start))
        var relaunched = RefreshPolicy(lastAttempt: original.lastAttempt)
        XCTAssertEqual(relaunched.remaining(at: start.addingTimeInterval(25)), 35)
        XCTAssertFalse(relaunched.begin(at: start.addingTimeInterval(59)))
        XCTAssertTrue(relaunched.begin(at: start.addingTimeInterval(60)))
    }

    func testOldPersistedAttemptDoesNotDelayFirstEligibleRefresh() {
        var policy = RefreshPolicy(lastAttempt: start.addingTimeInterval(-300))
        XCTAssertEqual(policy.remaining(at: start), 0)
        XCTAssertTrue(policy.begin(at: start))
        XCTAssertEqual(policy.lastAttempt, start)
    }

    func testBackwardClockRebasesToFiniteCooldownInsteadOfBurstOrLongLockout() {
        var policy = RefreshPolicy(lastAttempt: start)
        let corrected = start.addingTimeInterval(-86_400)
        XCTAssertEqual(policy.remaining(at: corrected), 60)
        XCTAssertEqual(policy.lastAttempt, corrected)
        XCTAssertFalse(policy.begin(at: corrected))
        XCTAssertEqual(policy.remaining(at: corrected.addingTimeInterval(59)), 1)
        XCTAssertTrue(policy.begin(at: corrected.addingTimeInterval(60)))
    }

    func testBackwardClockAtBeginAlsoRejectsAndPersistsNewCooldownAnchor() {
        var policy = RefreshPolicy(lastAttempt: start)
        let corrected = start.addingTimeInterval(-120)
        XCTAssertFalse(policy.begin(at: corrected))
        var relaunched = RefreshPolicy(lastAttempt: policy.lastAttempt)
        XCTAssertEqual(relaunched.remaining(at: corrected), 60)
        XCTAssertFalse(relaunched.begin(at: corrected.addingTimeInterval(59)))
        XCTAssertTrue(relaunched.begin(at: corrected.addingTimeInterval(60)))
    }

    func testForwardClockAllowsOneAttemptAndThenThrottlesRepeatedAttempts() {
        var policy = RefreshPolicy(lastAttempt: start)
        let corrected = start.addingTimeInterval(86_400)
        XCTAssertEqual(policy.remaining(at: corrected), 0)
        XCTAssertTrue(policy.begin(at: corrected))
        XCTAssertFalse(policy.begin(at: corrected))
        XCTAssertFalse(policy.begin(at: corrected.addingTimeInterval(59)))
        XCTAssertTrue(policy.begin(at: corrected.addingTimeInterval(60)))
    }

    func testRepeatedCountdownReadsDoNotTurnIntoAttempts() {
        var policy = RefreshPolicy(lastAttempt: start)
        for elapsed in [0.0, 30, 60, 300, 600] {
            XCTAssertGreaterThanOrEqual(policy.remaining(at: start.addingTimeInterval(elapsed)), 0)
            XCTAssertEqual(policy.lastAttempt, start)
        }
    }

    func testAutomaticCadenceIsFiveMinutesAndStillUsesGlobalMinimum() {
        XCTAssertEqual(RefreshPolicy.minimumInterval, 60)
        XCTAssertEqual(RefreshPolicy.pollingInterval, 300)
        var policy = RefreshPolicy(lastAttempt: start)
        XCTAssertTrue(policy.begin(at: start.addingTimeInterval(RefreshPolicy.pollingInterval)))
        XCTAssertEqual(policy.remaining(at: start.addingTimeInterval(300)), 60)
    }
}

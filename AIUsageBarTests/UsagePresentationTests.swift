import XCTest
@testable import AIUsageBar

final class UsagePresentationTests: XCTestCase {
    private let now = Date(timeIntervalSince1970: 1_800_000_000)

    func testUnknownObservationRemainsSnapshotInsteadOfFresh() throws {
        let reading = try sample()
        let display = UsagePresentation(state: .available(reading), now: now,
                                        policy: try UsageFreshnessPolicy(maxAge: 300))
        XCTAssertEqual(display.title, "ข้อมูลที่ได้รับล่าสุด")
        XCTAssertEqual(display.detail, "จาก Claude Code ล่าสุด เวลา \(UsagePresentation.format(reading.receivedAt))")
        XCTAssertNil(reading.providerObservedAt)
    }

    func testFailedRefreshKeepsFailureLabelWithRetainedQuota() throws {
        let reading = try sample()
        let display = UsagePresentation(state: .lastFailure(previous: reading, lastSuccessfulAt: nil),
                                        now: now, policy: try UsageFreshnessPolicy(maxAge: 300))
        XCTAssertEqual(display.title, "โหลดไม่สำเร็จ")
        XCTAssertEqual(display.quota?.usedPercent, 42)
        XCTAssertNotNil(display.detail)
    }

    func testExpiredQuotaIsNotPresentedAsCurrentNumbers() throws {
        let reading = try sample(reset: now)
        let display = UsagePresentation(state: .available(reading), now: now,
                                        policy: try UsageFreshnessPolicy(maxAge: 300))
        XCTAssertEqual(display.title, "ถึงเวลารีเซ็ตแล้ว")
        XCTAssertNil(display.quota)
    }

    func testNotConnectedContainsNoQuota() throws {
        let display = UsagePresentation(state: .notConnected, now: now,
                                        policy: try UsageFreshnessPolicy(maxAge: 300))
        XCTAssertNil(display.quota)
        XCTAssertEqual(display.title, "ยังไม่เชื่อมบริการ")
    }

    func testInvalidTimestampStaysHiddenAfterLoadingAndFailure() throws {
        let quota = try QuotaWindow(usedPercent: 42, resetsAt: now.addingTimeInterval(3600))
        let invalid = [
            UsageReading(provider: .claude, weekly: quota, source: .claudeStatusLine,
                         receivedAt: Date(timeIntervalSince1970: 0)),
            UsageReading(provider: .claude, weekly: quota, source: .claudeStatusLine,
                         receivedAt: now, providerObservedAt: now.addingTimeInterval(1))
        ]
        let policy = try UsageFreshnessPolicy(maxAge: 300)
        for reading in invalid {
            var store = UsageStatusStore()
            store.receive(reading)
            XCTAssertNil(UsagePresentation(state: store[.claude], now: now, policy: policy).quota)
            store.startLoading(provider: .claude)
            XCTAssertNil(UsagePresentation(state: store[.claude], now: now, policy: policy).quota)
            store.fail(provider: .claude)
            XCTAssertNil(UsagePresentation(state: store[.claude], now: now, policy: policy).quota)
        }
    }

    private func sample(reset: Date? = nil) throws -> UsageReading {
        UsageReading(provider: .claude,
                     weekly: try QuotaWindow(usedPercent: 42, resetsAt: reset ?? now.addingTimeInterval(3600)),
                     source: .claudeStatusLine, receivedAt: now)
    }
}

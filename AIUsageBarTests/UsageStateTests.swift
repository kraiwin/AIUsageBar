import Foundation
import XCTest
@testable import AIUsageBar

final class UsageStateTests: XCTestCase {
    private let now = Date(timeIntervalSince1970: 1_800_000_000)

    func testInitialStatesAreNotConnectedWithoutData() {
        let store = UsageStatusStore()
        for provider in AIProvider.allCases {
            XCTAssertEqual(store[provider], .notConnected)
            XCTAssertNil(store.lastSuccessfulAt(for: provider))
        }
    }

    func testFailurePreservesReadingAndVerifiedSuccessfulTime() throws {
        var store = UsageStatusStore()
        let reading = try reading(provider: .codex, observed: now)
        store.receive(reading)
        store.startLoading(provider: .codex)
        XCTAssertEqual(store[.codex], .loading(previous: reading))
        store.fail(provider: .codex)
        XCTAssertEqual(store[.codex], .lastFailure(previous: reading, lastSuccessfulAt: now))
        store.fail(provider: .codex)
        XCTAssertEqual(store[.codex], .lastFailure(previous: reading, lastSuccessfulAt: now))
        store.startLoading(provider: .codex)
        XCTAssertEqual(store[.codex], .loading(previous: reading))
    }

    func testFirstFailureDoesNotInventLastSuccess() {
        var store = UsageStatusStore()
        store.startLoading(provider: .claude)
        XCTAssertEqual(store[.claude], .loading(previous: nil))
        store.fail(provider: .claude)
        XCTAssertEqual(store[.claude], .lastFailure(previous: nil, lastSuccessfulAt: nil))
    }

    func testRepeatedUnknownSnapshotDoesNotBecomeVerifiedFresh() throws {
        var store = UsageStatusStore()
        store.receive(try reading(provider: .claude))
        let reread = try reading(provider: .claude, received: now.addingTimeInterval(600))
        store.receive(reread)
        XCTAssertNil(store.lastSuccessfulAt(for: .claude))
        XCTAssertNil(store[.claude].previousReading?.providerObservedAt)
        XCTAssertEqual(UsageFreshness.evaluate(reread, now: now.addingTimeInterval(600),
            policy: try UsageFreshnessPolicy(maxAge: 300)), .snapshot)
    }

    func testRepeatedKnownSnapshotDoesNotAdvanceObservationOrSuccess() throws {
        var store = UsageStatusStore()
        store.receive(try reading(provider: .claude, observed: now))
        let reread = try reading(provider: .claude, observed: now,
                                received: now.addingTimeInterval(600))
        store.receive(reread)
        XCTAssertEqual(store.lastSuccessfulAt(for: .claude), now)
        XCTAssertEqual(UsageFreshness.evaluate(reread, now: now.addingTimeInterval(600),
            policy: try UsageFreshnessPolicy(maxAge: 300)), .stale)
    }

    func testProvidersStayIndependentAcrossAllFailureStates() throws {
        var store = UsageStatusStore()
        let codex = try reading(provider: .codex, observed: now)
        store.receive(codex)
        store.receive(try reading(provider: .claude))
        store.startLoading(provider: .claude)
        store.fail(provider: .claude)
        XCTAssertEqual(store[.codex], .available(codex))
        store.missingData(provider: .claude)
        XCTAssertEqual(store[.claude], .unavailable)
        store.requireLogin(provider: .claude)
        XCTAssertEqual(store[.claude], .requiresLogin)
        store.disconnect(provider: .claude)
        XCTAssertEqual(store[.codex], .available(codex))
        XCTAssertEqual(store.lastSuccessfulAt(for: .codex), now)
    }

    func testDisconnectClearsVerifiedSuccessAndPreviousData() throws {
        var store = UsageStatusStore()
        store.receive(try reading(provider: .codex, observed: now))
        store.disconnect(provider: .codex)
        XCTAssertEqual(store[.codex], .notConnected)
        XCTAssertNil(store.lastSuccessfulAt(for: .codex))
        XCTAssertNil(store[.codex].previousReading)
    }

    func testInvalidObservationDoesNotRecordSuccess() throws {
        var store = UsageStatusStore()
        for observed in [now.addingTimeInterval(1), Date(timeIntervalSince1970: .nan),
                         Date(timeIntervalSince1970: 0), Date(timeIntervalSince1970: -1)] {
            store.receive(try reading(provider: .codex, observed: observed))
            XCTAssertNil(store.lastSuccessfulAt(for: .codex))
        }
    }

    private func reading(provider: AIProvider, observed: Date? = nil,
                         received: Date? = nil) throws -> UsageReading {
        UsageReading(provider: provider,
            weekly: try QuotaWindow(usedPercent: 25, resetsAt: now.addingTimeInterval(10_000)),
            source: provider == .claude ? .claudeStatusLine : .codexAppServer,
            receivedAt: received ?? now, providerObservedAt: observed)
    }
}

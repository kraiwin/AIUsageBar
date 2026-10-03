import Foundation
import XCTest
@testable import AIUsageBar

private actor DelayedQuotaFetch {
    private(set) var started = false
    private var continuation: CheckedContinuation<UsageReading?, Never>?
    func run() async -> UsageReading? {
        await withCheckedContinuation { continuation in
            self.continuation = continuation
            started = true
        }
    }
    func finish() {
        continuation?.resume(returning: nil)
        continuation = nil
    }
}

@MainActor
final class RefreshCoordinatorTests: XCTestCase {
    func testDisconnectThenQuitWaitsForPreviouslyCancelledCleanup() async throws {
        try await verifyShutdown(reconnect: false)
    }

    func testInvalidReconnectThenQuitWaitsForPreviouslyCancelledCleanup() async throws {
        try await verifyShutdown(reconnect: true)
    }

    private func verifyShutdown(reconnect: Bool) async throws {
        let name = "AIUsageBar-CoordinatorTests-" + UUID().uuidString
        let defaults = try XCTUnwrap(UserDefaults(suiteName: name))
        defer { defaults.removePersistentDomain(forName: name) }
        let delayed = DelayedQuotaFetch()
        let coordinator = RefreshCoordinator(defaults: defaults, fetch: { await delayed.run() })
        coordinator.refresh()
        while !(await delayed.started) { await Task.yield() }
        if reconnect { coordinator.connectCodex(URL(fileURLWithPath: "/no-such-aiusagebar-codex")) }
        else { coordinator.disconnectCodex() }
        var shutdownFinished = false
        let shutdown = Task { await coordinator.shutdown(); shutdownFinished = true }
        for _ in 0..<20 { await Task.yield() }
        XCTAssertFalse(shutdownFinished, "Quit must wait for retired provider cleanup")
        await delayed.finish()
        await shutdown.value
        XCTAssertTrue(shutdownFinished)
        XCTAssertEqual(coordinator.store[.codex], .notConnected)
        XCTAssertNil(defaults.string(forKey: "codexExecutablePath"))
    }
}

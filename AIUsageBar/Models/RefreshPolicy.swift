import Foundation

/// A backwards wall-clock jump starts a new cooldown, rather than permitting a burst.
struct RefreshPolicy: Sendable {
    static let minimumInterval: TimeInterval = 60
    static let pollingInterval: TimeInterval = 300
    private(set) var lastAttempt: Date?

    init(lastAttempt: Date? = nil) { self.lastAttempt = lastAttempt }

    mutating func remaining(at now: Date) -> TimeInterval {
        guard let lastAttempt else { return 0 }
        let elapsed = now.timeIntervalSince(lastAttempt)
        if elapsed < 0 {
            self.lastAttempt = now
            return Self.minimumInterval
        }
        return max(0, Self.minimumInterval - elapsed)
    }

    mutating func begin(at now: Date) -> Bool {
        guard remaining(at: now) == 0 else { return false }
        lastAttempt = now
        return true
    }
}

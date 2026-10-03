import Foundation

struct UsageFreshnessPolicy: Sendable {
    let maxAge: TimeInterval

    init(maxAge: TimeInterval) throws {
        guard maxAge.isFinite, maxAge >= 0 else {
            throw UsageValidationError.invalidFreshnessInterval
        }
        self.maxAge = maxAge
    }
}

enum UsageFreshness: Equatable, Sendable {
    case fresh, stale, snapshot, expired, invalidTimestamp

    static func evaluate(_ reading: UsageReading, now: Date,
                         policy: UsageFreshnessPolicy) -> Self {
        guard now.timeIntervalSince1970.isFinite,
              isValidTimestamp(reading.receivedAt),
              reading.receivedAt <= now else { return .invalidTimestamp }
        if let observed = reading.providerObservedAt {
            guard isValidTimestamp(observed),
                  observed <= reading.receivedAt, observed <= now else {
                return .invalidTimestamp
            }
        }
        guard reading.weekly.resetsAt > now else { return .expired }
        guard let observed = reading.providerObservedAt else { return .snapshot }
        return now.timeIntervalSince(observed) <= policy.maxAge ? .fresh : .stale
    }

    private static func isValidTimestamp(_ date: Date) -> Bool {
        let seconds = date.timeIntervalSince1970
        return seconds.isFinite && seconds > 0 && seconds <= 253_402_300_799
    }
}

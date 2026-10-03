import Foundation

enum UsageState: Equatable, Sendable {
    case notConnected
    case loading(previous: UsageReading?)
    case available(UsageReading)
    case unavailable
    case requiresLogin
    case lastFailure(previous: UsageReading?, lastSuccessfulAt: Date?)

    var previousReading: UsageReading? {
        switch self {
        case .available(let reading): reading
        case .loading(let previous), .lastFailure(let previous, _): previous
        case .notConnected, .unavailable, .requiresLogin: nil
        }
    }
}

/// Pure per-provider state; callers own I/O, request ordering and clock policy.
struct UsageStatusStore: Sendable {
    private(set) var states: [AIProvider: UsageState] = [:]
    private(set) var lastSuccessfulAt: [AIProvider: Date] = [:]

    func state(for provider: AIProvider) -> UsageState {
        states[provider] ?? .notConnected
    }

    subscript(provider: AIProvider) -> UsageState { state(for: provider) }

    func lastSuccessfulAt(for provider: AIProvider) -> Date? { lastSuccessfulAt[provider] }

    mutating func startLoading(provider: AIProvider) {
        states[provider] = .loading(previous: state(for: provider).previousReading)
    }

    mutating func receive(_ reading: UsageReading) {
        states[reading.provider] = .available(reading)
        // Reading a file successfully does not establish a successful server fetch.
        if let observed = reading.providerObservedAt,
           observed.timeIntervalSince1970.isFinite,
           observed.timeIntervalSince1970 > 0,
           observed.timeIntervalSince1970 <= 253_402_300_799,
           observed <= reading.receivedAt,
           lastSuccessfulAt[reading.provider].map({ observed > $0 }) ?? true {
            lastSuccessfulAt[reading.provider] = observed
        }
    }

    mutating func fail(provider: AIProvider) {
        states[provider] = .lastFailure(previous: state(for: provider).previousReading,
                                        lastSuccessfulAt: lastSuccessfulAt[provider])
    }

    mutating func disconnect(provider: AIProvider) {
        states[provider] = .notConnected
        lastSuccessfulAt[provider] = nil
    }

    mutating func missingData(provider: AIProvider) { states[provider] = .unavailable }
    mutating func requireLogin(provider: AIProvider) { states[provider] = .requiresLogin }
}

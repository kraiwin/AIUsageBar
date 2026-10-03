import Foundation

enum AIProvider: String, CaseIterable, Identifiable, Sendable {
    case claude, codex

    var id: String { rawValue }
    var displayName: String { self == .claude ? "Claude" : "Codex" }
}

enum UsageSource: Equatable, Sendable {
    case codexAppServer, claudeStatusLine
}

/// Errors describe the contract violation without retaining source payload values.
enum UsageValidationError: Error, Equatable, Sendable {
    case invalidPercentage
    case invalidResetTime
    case invalidFreshnessInterval
}

struct QuotaWindow: Equatable, Sendable {
    let usedPercent: Double
    let resetsAt: Date

    var remainingPercent: Double { 100 - usedPercent }

    init(usedPercent: Double, resetsAt: Date) throws {
        guard usedPercent.isFinite, (0...100).contains(usedPercent) else {
            throw UsageValidationError.invalidPercentage
        }
        // Positive Unix seconds through year 9999; old but valid reset times remain valid.
        guard resetsAt.timeIntervalSince1970.isFinite,
              (0...253_402_300_799).contains(resetsAt.timeIntervalSince1970),
              resetsAt.timeIntervalSince1970 > 0 else {
            throw UsageValidationError.invalidResetTime
        }
        self.usedPercent = usedPercent
        self.resetsAt = resetsAt
    }
}

struct UsageReading: Equatable, Sendable {
    let provider: AIProvider
    let weekly: QuotaWindow
    let session: QuotaWindow?
    let source: UsageSource
    let receivedAt: Date
    /// Only a verified provider observation time, never the snapshot-file read time.
    let providerObservedAt: Date?

    init(provider: AIProvider, weekly: QuotaWindow, session: QuotaWindow? = nil,
         source: UsageSource, receivedAt: Date, providerObservedAt: Date? = nil) {
        self.provider = provider
        self.weekly = weekly
        self.session = session
        self.source = source
        self.receivedAt = receivedAt
        self.providerObservedAt = providerObservedAt
    }
}

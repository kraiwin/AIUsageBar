import CoreFoundation
import Foundation

enum UsagePayloadError: Error, Equatable, Sendable {
    case invalidJSON
    case invalidField(String)
}

/// Parses provider result objects only, without transport envelopes or account data.
enum UsagePayloadParser {
    static func parseCodex(_ data: Data, receivedAt: Date) throws -> UsageReading? {
        let root = try object(data)
        let limits: [String: Any]?
        let selectedCodexBucket: Bool
        if let buckets = try optionalObject(root, key: "rateLimitsByLimitId") {
            // A map can contain other products: never display their quota as Codex.
            limits = try optionalObject(buckets, key: "codex")
            selectedCodexBucket = true
        } else {
            limits = try optionalObject(root, key: "rateLimits")
            selectedCodexBucket = false
        }
        guard let limits else { return nil }
        if let value = limits["limitId"], !(value is NSNull) {
            guard let limitID = value as? String else {
                throw UsagePayloadError.invalidField("limitId")
            }
            if limitID != "codex" {
                // A conflicting explicit identity is malformed in the selected Codex bucket.
                guard !selectedCodexBucket else { throw UsagePayloadError.invalidField("limitId") }
                return nil
            }
        }
        var weekly: QuotaWindow?
        var session: QuotaWindow?
        for key in ["primary", "secondary"] {
            guard let window = try optionalObject(limits, key: key) else { continue }
            guard let duration = try optionalNumber(window, key: "windowDurationMins") else {
                continue
            }
            guard duration > 0, duration.rounded() == duration else {
                throw UsagePayloadError.invalidField("windowDurationMins")
            }
            if duration == 10_080 {
                guard weekly == nil else { throw UsagePayloadError.invalidField("weekly") }
                weekly = try quota(window, percentageKey: "usedPercent", resetKey: "resetsAt")
            } else if duration == 300 {
                guard session == nil else { throw UsagePayloadError.invalidField("session") }
                session = try quota(window, percentageKey: "usedPercent", resetKey: "resetsAt")
            }
        }
        guard let weekly else { return nil }
        return UsageReading(provider: .codex, weekly: weekly, session: session,
                            source: .codexAppServer, receivedAt: receivedAt)
    }

    static func parseClaude(_ data: Data, receivedAt: Date) throws -> UsageReading? {
        let root = try object(data)
        guard let limits = try optionalObject(root, key: "rate_limits"),
              let week = try optionalObject(limits, key: "seven_day") else { return nil }
        let weekly = try quota(week, percentageKey: "used_percentage", resetKey: "resets_at")
        let session = try optionalObject(limits, key: "five_hour").map {
            try quota($0, percentageKey: "used_percentage", resetKey: "resets_at")
        }
        return UsageReading(provider: .claude, weekly: weekly, session: session,
                            source: .claudeStatusLine, receivedAt: receivedAt)
    }

    private static func object(_ data: Data) throws -> [String: Any] {
        let value: Any
        do { value = try JSONSerialization.jsonObject(with: data) }
        catch { throw UsagePayloadError.invalidJSON }
        guard let result = value as? [String: Any] else { throw UsagePayloadError.invalidJSON }
        return result
    }

    private static func optionalObject(_ object: [String: Any], key: String) throws -> [String: Any]? {
        guard let value = object[key], !(value is NSNull) else { return nil }
        guard let result = value as? [String: Any] else {
            throw UsagePayloadError.invalidField(key)
        }
        return result
    }

    private static func optionalNumber(_ object: [String: Any], key: String) throws -> Double? {
        guard let value = object[key], !(value is NSNull) else { return nil }
        guard let number = value as? NSNumber,
              CFGetTypeID(number) != CFBooleanGetTypeID(), number.doubleValue.isFinite else {
            throw UsagePayloadError.invalidField(key)
        }
        return number.doubleValue
    }

    private static func quota(_ object: [String: Any], percentageKey: String,
                              resetKey: String) throws -> QuotaWindow {
        guard let percentage = try optionalNumber(object, key: percentageKey),
              (0...100).contains(percentage) else {
            throw UsagePayloadError.invalidField(percentageKey)
        }
        guard let reset = try optionalNumber(object, key: resetKey),
              reset > 0, reset <= 253_402_300_799 else {
            throw UsagePayloadError.invalidField(resetKey)
        }
        return try QuotaWindow(usedPercent: percentage, resetsAt: Date(timeIntervalSince1970: reset))
    }
}

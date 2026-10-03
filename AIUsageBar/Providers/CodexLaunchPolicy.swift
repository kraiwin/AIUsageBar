import CoreFoundation
import Foundation

/// Values deliberately contain no raw payload, server message or account data.
enum CodexProviderError: Error, Equatable, Sendable {
    case executableUnavailable, busy, cancelled, timeout, launchFailed, ioFailure
    case childExited, cleanupFailed, outputLimit, invalidEnvelope, unexpectedResponse
    case serverRequest, serverError(code: Int), unsupportedConfiguration, invalidQuota
}

enum CodexLaunchPolicy {
    enum TransportKind: Equatable, Sendable { case stdio, http }
    struct Server: Equatable, Sendable {
        let name: String
        let kind: TransportKind
        let disabled: Bool
    }
    struct Feature: Equatable, Sendable {
        let name: String
        let enabled: Bool
        let stage: String
    }
    static let requiredFeatures: Set<String> = ["hooks", "plugins", "code_mode_host"]
    static let staticOverrides = ["features.hooks=false", "features.plugins=false",
        "features.code_mode_host=false", "features.remote_control=false", "notify=[]",
        "analytics.enabled=false", "otel.exporter=\"none\"", "otel.trace_exporter=\"none\""]

    static func arguments(disabling servers: [Server]? = nil) throws -> [String] {
        var overrides = staticOverrides
        if let servers {
            guard servers.count <= 256, Set(servers.map(\.name)).count == servers.count else {
                throw CodexProviderError.unsupportedConfiguration
            }
            let entries = try servers.sorted { $0.name < $1.name }.map { item in
                let name = try tomlString(item.name)
                let placeholder = item.kind == .stdio ? "command=\"/usr/bin/false\"" : "url=\"https://example.invalid/\""
                return "\(name)={enabled=false,\(placeholder)}"
            }
            overrides.append("mcp_servers={\(entries.joined(separator: ","))}")
        }
        return overrides.flatMap { ["-c", $0] } + ["app-server", "--listen", "stdio://", "--strict-config"]
    }

    static func registryPage(_ result: [String: Any]) throws -> (features: [Feature], cursor: String?) {
        guard let data = result["data"] as? [[String: Any]], data.count <= 100 else {
            throw CodexProviderError.unsupportedConfiguration
        }
        let features = try data.map { entry in
            guard let name = entry["name"] as? String, !name.isEmpty, name.utf8.count <= 256,
                  let enabled = boolean(entry["enabled"]), let stage = entry["stage"] as? String else {
                throw CodexProviderError.unsupportedConfiguration
            }
            return Feature(name: name, enabled: enabled, stage: stage)
        }
        let cursor: String?
        if result["nextCursor"] == nil || result["nextCursor"] is NSNull { cursor = nil }
        else if let next = result["nextCursor"] as? String, !next.isEmpty, next.utf8.count <= 4096 { cursor = next }
        else { throw CodexProviderError.unsupportedConfiguration }
        return (features, cursor)
    }

    static func assertRegistry(_ features: [Feature]) throws {
        guard features.count <= 2048, Set(features.map(\.name)).count == features.count else {
            throw CodexProviderError.unsupportedConfiguration
        }
        let activeStages: Set<String> = ["stable", "beta", "underDevelopment"]
        for name in requiredFeatures {
            guard let feature = features.first(where: { $0.name == name }),
                  !feature.enabled, activeStages.contains(feature.stage) else {
                throw CodexProviderError.unsupportedConfiguration
            }
        }
        // remote_control is Removed in 0.160. Its echoed false is not a guard.
    }

    static func inventory(_ result: [String: Any], requireDisabled: Bool) throws -> [Server] {
        guard let config = result["config"] as? [String: Any],
              let notify = config["notify"] as? [Any], notify.isEmpty,
              let analytics = config["analytics"] as? [String: Any], boolean(analytics["enabled"]) == false,
              let otel = config["otel"] as? [String: Any], otel["exporter"] as? String == "none",
              otel["trace_exporter"] as? String == "none",
              let servers = config["mcp_servers"] as? [String: Any], servers.count <= 256 else {
            throw CodexProviderError.unsupportedConfiguration
        }
        return try servers.map { name, value in
            guard !name.isEmpty, name.utf8.count <= 256, let details = value as? [String: Any] else {
                throw CodexProviderError.unsupportedConfiguration
            }
            _ = try tomlString(name)
            // Typed protocol config may serialize absent fields as null.
            let command = details["command"].flatMap { $0 is NSNull ? nil : $0 }
            let url = details["url"].flatMap { $0 is NSNull ? nil : $0 }
            let kind: TransportKind
            if let command = command as? String, !command.isEmpty, url == nil { kind = .stdio }
            else if let url = url as? String, !url.isEmpty, command == nil { kind = .http }
            else { throw CodexProviderError.unsupportedConfiguration }
            let enabled = boolean(details["enabled"])
            guard details["enabled"] == nil || details["enabled"] is NSNull || enabled != nil else {
                throw CodexProviderError.unsupportedConfiguration
            }
            if requireDisabled && enabled != false { throw CodexProviderError.unsupportedConfiguration }
            return Server(name: name, kind: kind, disabled: enabled == false)
        }.sorted { $0.name < $1.name }
    }

    static func assertSameInventory(_ expected: [Server], _ actual: [Server]) throws {
        guard expected.map(\.name) == actual.map(\.name), actual.allSatisfy(\.disabled),
              zip(expected, actual).allSatisfy({ $0.kind == $1.kind }) else {
            throw CodexProviderError.unsupportedConfiguration
        }
    }

    static func boolean(_ value: Any?) -> Bool? {
        guard let number = value as? NSNumber, CFGetTypeID(number) == CFBooleanGetTypeID() else { return nil }
        return number.boolValue
    }

    private static func tomlString(_ value: String) throws -> String {
        guard !value.unicodeScalars.contains(where: { $0.value < 32 || $0.value == 127 }) else {
            throw CodexProviderError.unsupportedConfiguration
        }
        return "\"" + value.replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"") + "\""
    }
}

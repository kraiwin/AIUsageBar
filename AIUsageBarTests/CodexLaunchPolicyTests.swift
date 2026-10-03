import Foundation
import XCTest
@testable import AIUsageBar

final class CodexLaunchPolicyTests: XCTestCase {
    private var validFeatures: [CodexLaunchPolicy.Feature] {
        CodexLaunchPolicy.requiredFeatures.sorted().map { .init(name: $0, enabled: false, stage: "stable") }
    }

    func testRegistryRequiresCanonicalPresentActiveDisabledGuards() throws {
        try CodexLaunchPolicy.assertRegistry(validFeatures)
        for name in CodexLaunchPolicy.requiredFeatures {
            let others = validFeatures.filter { $0.name != name }
            for replacement in [[], [.init(name: name + "_renamed", enabled: false, stage: "stable")],
                                [.init(name: name, enabled: true, stage: "stable")],
                                [.init(name: name, enabled: false, stage: "removed")],
                                [.init(name: name, enabled: false, stage: "deprecated")],
                                [.init(name: name, enabled: false, stage: "unknown")]] as [[CodexLaunchPolicy.Feature]] {
                XCTAssertThrowsError(try CodexLaunchPolicy.assertRegistry(others + replacement))
            }
        }
        XCTAssertThrowsError(try CodexLaunchPolicy.assertRegistry(validFeatures + [validFeatures[0]]))
        // Removed remote_control is explicitly excluded from functional guards.
        try CodexLaunchPolicy.assertRegistry(validFeatures + [.init(name: "remote_control", enabled: false, stage: "removed")])
    }

    func testRegistryPageRejectsTypeConfusionAndInvalidCursor() throws {
        let result: [String: Any] = ["data": [["name": "hooks", "enabled": false, "stage": "stable"]], "nextCursor": "synthetic-next"]
        let page = try CodexLaunchPolicy.registryPage(result)
        XCTAssertEqual(page.cursor, "synthetic-next")
        XCTAssertEqual(page.features.first?.name, "hooks")
        for bad: [String: Any] in [[:], ["data": false], ["data": [["name": "hooks", "enabled": 0, "stage": "stable"]]],
                                 ["data": [], "nextCursor": false], ["data": [], "nextCursor": ""]] {
            XCTAssertThrowsError(try CodexLaunchPolicy.registryPage(bad))
        }
    }

    func testDisabledPlaceholdersKeepNoOriginalCommandArgumentsOrSecrets() throws {
        let payload = config(servers: [
            "quoted.\"name\\": ["command": "synthetic-private-command", "enabled": true, "args": ["synthetic-private-arg"], "env": ["PRIVATE": "synthetic-private-value"]],
            "web": ["url": "https://synthetic-private.invalid", "enabled": true, "headers": ["Authorization": "synthetic-private-header"]]
        ])
        let inventory = try CodexLaunchPolicy.inventory(payload, requireDisabled: false)
        let arguments = try CodexLaunchPolicy.arguments(disabling: inventory)
        let text = arguments.joined(separator: " ")
        XCTAssertTrue(text.contains("/usr/bin/false"))
        XCTAssertTrue(text.contains("https://example.invalid/"))
        XCTAssertTrue(text.contains(#"quoted.\"name\\"#))
        XCTAssertFalse(text.contains("synthetic-private"))
        XCTAssertFalse(text.contains("Authorization"))
        XCTAssertEqual(arguments.suffix(4), ["app-server", "--listen", "stdio://", "--strict-config"])
    }

    func testUnknownConflictingTransportAndUnsafeNamesFailClosed() {
        for servers: [String: Any] in [
            ["both": ["command": "/usr/bin/false", "url": "https://example.invalid/"]],
            ["unknown": ["transport": "future"]], ["wrong": ["command": false]],
            ["line\nbreak": ["command": "/usr/bin/false"]]
        ] {
            XCTAssertThrowsError(try CodexLaunchPolicy.inventory(config(servers: servers), requireDisabled: false))
        }
    }

    func testSecondInventoryRejectsEnabledAddedMissingOrChangedKind() throws {
        let original = try CodexLaunchPolicy.inventory(config(servers: ["one": ["command": "/usr/bin/false", "enabled": true]]), requireDisabled: false)
        let disabled = try CodexLaunchPolicy.inventory(config(servers: ["one": ["command": "/usr/bin/false", "enabled": false]]), requireDisabled: true)
        try CodexLaunchPolicy.assertSameInventory(original, disabled)
        for servers: [String: Any] in [
            ["one": ["command": "/usr/bin/false", "enabled": true]],
            ["one": ["command": "/usr/bin/false", "enabled": 0]],
            ["one": ["command": "/usr/bin/false"]]
        ] {
            XCTAssertThrowsError(try CodexLaunchPolicy.inventory(config(servers: servers), requireDisabled: true))
        }
        for actual: [CodexLaunchPolicy.Server] in [[], disabled + [.init(name: "added", kind: .stdio, disabled: true)],
                                                  [.init(name: "one", kind: .http, disabled: true)]] {
            XCTAssertThrowsError(try CodexLaunchPolicy.assertSameInventory(original, actual))
        }
    }

    func testNotifyAndTelemetryAssertionsRejectPolicyOverride() {
        for mutation: [String: Any] in [["notify": ["synthetic"]], ["notify": NSNull()],
                                      ["analytics": ["enabled": true]], ["analytics": ["enabled": 0]],
                                      ["otel": ["exporter": "none", "trace_exporter": "future"]]] {
            var result = config(servers: [:])
            var values = result["config"] as? [String: Any] ?? [:]
            values.merge(mutation) { _, new in new }
            result["config"] = values
            XCTAssertThrowsError(try CodexLaunchPolicy.inventory(result, requireDisabled: false))
        }
    }

    func testNativeDiscoveryNeverAcceptsExecutableScript() throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: directory) }
        let shim = directory.appendingPathComponent("codex")
        try Data("#!/bin/sh\nexit 0\n".utf8).write(to: shim)
        try FileManager.default.setAttributes([.posixPermissions: 0o700], ofItemAtPath: shim.path)
        XCTAssertThrowsError(try CodexExecutableDiscovery.validate(shim))
        XCTAssertTrue(CodexExecutableDiscovery.candidates(home: directory, path: directory.path).allSatisfy { $0 != shim })
        try CodexExecutableDiscovery.validate(URL(fileURLWithPath: "/usr/bin/false"))
    }

    private func config(servers: [String: Any]) -> [String: Any] {
        ["config": ["notify": [], "analytics": ["enabled": false],
                    "otel": ["exporter": "none", "trace_exporter": "none"], "mcp_servers": servers]]
    }
}

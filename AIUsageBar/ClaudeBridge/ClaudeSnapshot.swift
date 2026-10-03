import Foundation

/// No session, workspace, account identity, transcript or raw status-line fields.
struct ClaudeSnapshot: Codable, Equatable, Sendable {
    struct Window: Codable, Equatable, Sendable {
        let usedPercent: Double
        let resetsAt: Double
    }
    let schemaVersion: Int
    let receivedAt: Double
    let state: String
    let weekly: Window?
    let session: Window?

    static func ingest(_ data: Data, receivedAt: Date) throws -> Self {
        guard data.count <= ClaudePrivateFiles.maximumSettingsBytes else { throw ClaudeBridgeError.oversized }
        let reading = try UsagePayloadParser.parseClaude(data, receivedAt: receivedAt)
        func window(_ value: QuotaWindow?) -> Window? {
            value.map { Window(usedPercent: $0.usedPercent, resetsAt: $0.resetsAt.timeIntervalSince1970) }
        }
        return Self(schemaVersion: 1, receivedAt: receivedAt.timeIntervalSince1970,
                    state: reading == nil ? "no-data" : "quota", weekly: window(reading?.weekly),
                    session: window(reading?.session))
    }

    func reading(now: Date) throws -> UsageReading? {
        guard schemaVersion == 1, receivedAt.isFinite, receivedAt > 0,
              receivedAt <= now.timeIntervalSince1970, receivedAt <= 253_402_300_799 else {
            throw ClaudeBridgeError.invalidSnapshot
        }
        if state == "no-data" {
            guard weekly == nil, session == nil else { throw ClaudeBridgeError.invalidSnapshot }
            return nil
        }
        guard state == "quota", let weekly else { throw ClaudeBridgeError.invalidSnapshot }
        func quota(_ value: Window) throws -> QuotaWindow {
            try QuotaWindow(usedPercent: value.usedPercent, resetsAt: Date(timeIntervalSince1970: value.resetsAt))
        }
        return try UsageReading(provider: .claude, weekly: quota(weekly), session: session.map(quota),
                                source: .claudeStatusLine, receivedAt: Date(timeIntervalSince1970: receivedAt))
    }

    static func write(_ data: Data, directory: URL, receivedAt: Date = Date(),
                      requireInstallation: Bool = false) throws {
        let fd = try ClaudePrivateFiles.directory(directory)
        defer { close(fd) }
        // Serialize the timestamp and replace so a slower concurrent writer cannot
        // publish an older observation after a newer one.
        try ClaudePrivateFiles.withLock(directoryFD: fd) {
            if requireInstallation {
                struct Marker: Decodable { let schemaVersion: Int }
                let bytes = try ClaudePrivateFiles.read(name: "claude-install.json", directoryFD: fd)
                guard try JSONDecoder().decode(Marker.self, from: bytes).schemaVersion == 1 else {
                    throw ClaudeBridgeError.conflict
                }
            }
            let snapshot = try ingest(data, receivedAt: receivedAt)
            _ = try snapshot.reading(now: Date())
            let url = directory.appendingPathComponent("claude-latest.json")
            do {
                let bytes = try ClaudePrivateFiles.read(name: url.lastPathComponent, directoryFD: fd, limit: 4_096)
                if let current = try? JSONDecoder().decode(Self.self, from: bytes) {
                    let valid: Bool
                    do { _ = try current.reading(now: Date()); valid = true } catch { valid = false }
                    if valid, current.receivedAt > snapshot.receivedAt { return }
                }
            } catch ClaudeBridgeError.missingFile {
                // First observation is safe to create.
            } catch ClaudeBridgeError.oversized {
                // A safe regular file with corrupt content may be repaired.
            }
            try ClaudePrivateFiles.atomicWrite(JSONEncoder().encode(snapshot), name: url.lastPathComponent, directoryFD: fd)
        }
    }
}

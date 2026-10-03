import Foundation

struct ClaudeSnapshotProvider: Sendable {
    let snapshotURL: URL

    init(snapshotURL: URL = FileManager.default.homeDirectoryForCurrentUser
        .appendingPathComponent("Library/Application Support/AIUsageBar/claude-latest.json")) {
        self.snapshotURL = snapshotURL
    }

    /// Repeated reads preserve ingest time. Missing or an explicit no-data
    /// tombstone returns nil; malformed/insecure files fail without returning quota.
    func read(now: Date = Date()) throws -> UsageReading? {
        let directoryFD: Int32
        do { directoryFD = try ClaudePrivateFiles.directory(snapshotURL.deletingLastPathComponent()) }
        catch ClaudeBridgeError.missingFile { return nil }
        defer { close(directoryFD) }
        let data: Data
        do { data = try ClaudePrivateFiles.read(name: snapshotURL.lastPathComponent, directoryFD: directoryFD, limit: 4_096) }
        catch ClaudeBridgeError.missingFile { return nil }
        let root = try JSONSerialization.jsonObject(with: data)
        guard let object = root as? [String: Any],
              Set(object.keys).isSubset(of: ["schemaVersion", "receivedAt", "state", "weekly", "session"]) else {
            throw ClaudeBridgeError.invalidSnapshot
        }
        return try JSONDecoder().decode(ClaudeSnapshot.self, from: data).reading(now: now)
    }
}

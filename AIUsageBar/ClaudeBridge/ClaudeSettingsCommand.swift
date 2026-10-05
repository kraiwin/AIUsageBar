import Foundation

/// JSON token walker retains byte offsets; encoding a whole settings object
/// would reorder unrelated keys or destroy the user's whitespace.
struct ClaudeSettingsCommand {
    private struct Node {
        let range: Range<Int>
        var members: [String: Node]? = nil
    }
    let original: String
    let range: Range<Int>
    let hasStatusLine: Bool
    private let statusRemovalRange: Range<Int>?
    private let insertionNeedsComma: Bool

    init(data: Data) throws {
        guard data.count <= ClaudePrivateFiles.maximumSettingsBytes,
              (try? JSONSerialization.jsonObject(with: data)) is [String: Any] else {
            throw ClaudeBridgeError.invalidSettings
        }
        var scanner = Scanner(bytes: Array(data))
        let root = try scanner.value(depth: 0)
        guard root.members != nil else { throw ClaudeBridgeError.invalidSettings }
        insertionNeedsComma = !(root.members?.isEmpty ?? true)
        guard let status = root.members?["statusLine"] else {
            original = ""
            range = (root.range.upperBound - 1)..<(root.range.upperBound - 1)
            hasStatusLine = false
            statusRemovalRange = nil
            return
        }
        hasStatusLine = true
        let entries = scanner.rootEntries
        guard let entry = entries.firstIndex(where: { $0.key == "statusLine" }) else {
            throw ClaudeBridgeError.invalidSettings
        }
        let member = entries[entry].range
        if entry > 0 {
            var start = member.lowerBound - 1
            while [9, 10, 13, 32].contains(scanner.bytes[start]) { start -= 1 }
            statusRemovalRange = start..<member.upperBound
        } else if entries.count > 1 {
            var end = member.upperBound
            while [9, 10, 13, 32].contains(scanner.bytes[end]) { end += 1 }
            statusRemovalRange = member.lowerBound..<(end + 1)
        } else { statusRemovalRange = member }
        guard let members = status.members,
              let kind = members["type"], let command = members["command"],
              try Self.string(data.subdata(in: kind.range)) == "command" else {
            throw ClaudeBridgeError.invalidSettings
        }
        self.original = try Self.string(data.subdata(in: command.range))
        self.range = command.range
    }

    func replacing(in data: Data, with command: String) throws -> Data {
        let encoded = try JSONEncoder().encode(command)
        var updated = data
        if hasStatusLine { updated.replaceSubrange(range, with: encoded) }
        else {
            let prefix = insertionNeedsComma ? "," : ""
            let field = Data((prefix + "\"statusLine\":{\"type\":\"command\",\"command\":").utf8) + encoded + Data("}".utf8)
            updated.replaceSubrange(range, with: field)
        }
        return updated
    }

    func removingStatusLine(in data: Data, installedCommand: String) throws -> Data {
        // A quota-only installation owns the entire newly added object. Refuse
        // to delete padding or other fields added later by the user.
        let object = try JSONSerialization.jsonObject(with: data) as? [String: Any]
        guard let status = object?["statusLine"] as? [String: Any], status.count == 2,
              status["type"] as? String == "command", status["command"] as? String == installedCommand,
              let statusRemovalRange else { throw ClaudeBridgeError.conflict }
        var result = data
        result.removeSubrange(statusRemovalRange)
        return result
    }

    private static func string(_ data: Data) throws -> String {
        guard let string = try? JSONDecoder().decode(String.self, from: data) else {
            throw ClaudeBridgeError.invalidSettings
        }
        return string
    }

    private struct Scanner {
        let bytes: [UInt8]
        var index = 0
        var rootEntries: [(key: String, range: Range<Int>)] = []

        mutating func whitespace() {
            while index < bytes.count, [9, 10, 13, 32].contains(bytes[index]) { index += 1 }
        }

        mutating func consume(_ byte: UInt8) throws {
            whitespace()
            guard index < bytes.count, bytes[index] == byte else { throw ClaudeBridgeError.invalidSettings }
            index += 1
        }

        mutating func quoted() throws -> Range<Int> {
            whitespace()
            let start = index
            try consume(34)
            while index < bytes.count {
                let byte = bytes[index]; index += 1
                if byte == 34 { return start..<index }
                if byte == 92 { index += 1 }
            }
            throw ClaudeBridgeError.invalidSettings
        }

        mutating func value(depth: Int) throws -> Node {
            guard depth < 64 else { throw ClaudeBridgeError.invalidSettings }
            whitespace()
            let start = index
            guard index < bytes.count else { throw ClaudeBridgeError.invalidSettings }
            if bytes[index] == 123 {
                index += 1; whitespace()
                var members: [String: Node] = [:]
                if index < bytes.count, bytes[index] == 125 { index += 1; return Node(range: start..<index, members: members) }
                while true {
                    let keyRange = try quoted()
                    let key = try ClaudeSettingsCommand.string(Data(bytes[keyRange]))
                    guard members[key] == nil else { throw ClaudeBridgeError.invalidSettings }
                    try consume(58)
                    members[key] = try value(depth: depth + 1)
                    if depth == 0 { rootEntries.append((key, keyRange.lowerBound..<index)) }
                    whitespace()
                    if index < bytes.count, bytes[index] == 125 { index += 1; break }
                    try consume(44)
                }
                return Node(range: start..<index, members: members)
            }
            if bytes[index] == 91 {
                index += 1; whitespace()
                if index < bytes.count, bytes[index] == 93 { index += 1; return Node(range: start..<index) }
                while true {
                    _ = try value(depth: depth + 1); whitespace()
                    if index < bytes.count, bytes[index] == 93 { index += 1; break }
                    try consume(44)
                }
            } else if bytes[index] == 34 {
                _ = try quoted()
            } else {
                while index < bytes.count, ![9, 10, 13, 32, 44, 93, 125].contains(bytes[index]) { index += 1 }
            }
            return Node(range: start..<index)
        }
    }
}

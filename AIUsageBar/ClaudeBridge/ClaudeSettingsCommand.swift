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

    init(data: Data) throws {
        guard data.count <= ClaudePrivateFiles.maximumSettingsBytes,
              (try? JSONSerialization.jsonObject(with: data)) is [String: Any] else {
            throw ClaudeBridgeError.invalidSettings
        }
        var scanner = Scanner(bytes: Array(data))
        let root = try scanner.value(depth: 0)
        guard let status = root.members?["statusLine"], let members = status.members,
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
        updated.replaceSubrange(range, with: encoded)
        return updated
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

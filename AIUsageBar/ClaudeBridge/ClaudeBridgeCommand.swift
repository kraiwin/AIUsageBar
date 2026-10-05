import Darwin
import Foundation

enum ClaudeBridgeCommand {
    /// Call before NSApplication starts. nil means ordinary app startup. The
    /// copied app executable is also a standalone helper, without opening UI.
    static func run(arguments: [String]) -> Int32? {
        guard let mode = arguments.first, ["wrap", "collect", "ingest", "preview", "install", "disconnect"].contains(mode) else { return nil }
        do {
            switch mode {
            case "wrap":
                guard arguments.count == 4 else { throw ClaudeBridgeError.invalidSettings }
                return ClaudeOriginalCommand.run(command: arguments[1], helperPath: arguments[2], directory: URL(fileURLWithPath: arguments[3]))
            case "ingest", "collect":
                guard arguments.count == 2 else { throw ClaudeBridgeError.invalidSettings }
                var data = Data()
                while let chunk = try FileHandle.standardInput.read(upToCount: 8_192), !chunk.isEmpty {
                    data.append(chunk)
                    guard data.count <= ClaudePrivateFiles.maximumSettingsBytes else { throw ClaudeBridgeError.oversized }
                }
                try ClaudeSnapshot.write(data, directory: URL(fileURLWithPath: arguments[1]), requireInstallation: true)
            case "preview", "install":
                guard arguments.count == 4 else { throw ClaudeBridgeError.invalidSettings }
                let settings = URL(fileURLWithPath: arguments[1]), directory = URL(fileURLWithPath: arguments[2])
                let binary = try ClaudePrivateFiles.read(URL(fileURLWithPath: arguments[3]), limit: 64 * 1_024 * 1_024, privateMode: false)
                let preview = try ClaudeBridgeInstaller.preview(settingsURL: settings, directory: directory, wrapperBinary: binary, helperBinary: binary)
                if mode == "preview" {
                    print("Settings: \(settings.path)\nDirectory: \(directory.path)\nOriginal command: \(preview.originalCommand)\nInstalled command: \(preview.installedCommand)")
                } else { try ClaudeBridgeInstaller.apply(preview); print("Claude bridge installed") }
            case "disconnect":
                guard arguments.count == 3 else { throw ClaudeBridgeError.invalidSettings }
                try ClaudeBridgeInstaller.restore(directory: URL(fileURLWithPath: arguments[2]), settingsURL: URL(fileURLWithPath: arguments[1]))
                print("Claude bridge disconnected")
            default: return nil
            }
            return 0
        } catch {
            if mode != "ingest" && mode != "collect" && mode != "wrap" {
                FileHandle.standardError.write(Data("Claude bridge operation failed; settings were not printed.\n".utf8))
            }
            return mode == "collect" ? 0 : 1
        }
    }
}

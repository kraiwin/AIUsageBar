import Foundation

/// Resolves installed native binaries without invoking Node, a login shell or a shim.
enum CodexExecutableDiscovery {
    static func candidates(home: URL = FileManager.default.homeDirectoryForCurrentUser,
                           path: String = ProcessInfo.processInfo.environment["PATH"] ?? "") -> [URL] {
        var roots = [URL(fileURLWithPath: "/opt/homebrew"), URL(fileURLWithPath: "/usr/local")]
        let nvm = home.appendingPathComponent(".nvm/versions/node", isDirectory: true)
        if let versions = try? FileManager.default.contentsOfDirectory(at: nvm,
            includingPropertiesForKeys: nil, options: [.skipsHiddenFiles]) {
            roots += versions.sorted { $0.lastPathComponent.localizedStandardCompare($1.lastPathComponent) == .orderedDescending }
        }
        roots += [home.appendingPathComponent(".npm-global"), home.appendingPathComponent(".local")]
        var paths = path.split(separator: ":").filter { $0.hasPrefix("/") }.map {
            URL(fileURLWithPath: String($0)).appendingPathComponent("codex")
        }
        for root in roots {
            paths.append(root.appendingPathComponent("bin/codex"))
            paths += packageCandidates(root.appendingPathComponent("lib/node_modules/@openai/codex"))
        }
        // PATH may point directly at an npm wrapper. Resolve its package layout by
        // following filesystem links, without evaluating that wrapper's JavaScript.
        for item in paths {
            let resolved = item.resolvingSymlinksInPath()
            if resolved.lastPathComponent == "codex.js" {
                paths += packageCandidates(resolved.deletingLastPathComponent().deletingLastPathComponent())
            }
        }
        var seen = Set<String>()
        return paths.compactMap {
            let resolved = $0.resolvingSymlinksInPath().standardizedFileURL
            guard seen.insert(resolved.path).inserted, (try? validate(resolved)) != nil else { return nil }
            return resolved
        }
    }

    /// A user-selected executable must exist and be a native Mach-O, not a script.
    /// This is format validation, not a signature/provenance claim.
    static func validate(_ url: URL) throws {
        guard url.isFileURL, url.path.hasPrefix("/"),
              FileManager.default.isExecutableFile(atPath: url.path),
              let values = try? url.resourceValues(forKeys: [.isRegularFileKey]),
              values.isRegularFile == true else { throw CodexProviderError.executableUnavailable }
        let handle: FileHandle
        do { handle = try FileHandle(forReadingFrom: url) }
        catch { throw CodexProviderError.executableUnavailable }
        defer { try? handle.close() }
        let bytes = try? handle.read(upToCount: 4)
        let magic = bytes.map { Array($0) } ?? []
        guard [[0xcf, 0xfa, 0xed, 0xfe], [0xfe, 0xed, 0xfa, 0xcf],
               [0xca, 0xfe, 0xba, 0xbe], [0xbe, 0xba, 0xfe, 0xca],
               [0xca, 0xfe, 0xba, 0xbf], [0xbf, 0xba, 0xfe, 0xca]].contains(magic) else {
            throw CodexProviderError.executableUnavailable
        }
    }

    private static func packageCandidates(_ package: URL) -> [URL] {
        let platforms = [("darwin-arm64", "aarch64-apple-darwin"), ("darwin-x64", "x86_64-apple-darwin")]
        return platforms.flatMap { platform, triple in
            [package.appendingPathComponent("vendor/\(triple)/bin/codex"),
             package.appendingPathComponent("node_modules/@openai/codex-\(platform)/vendor/\(triple)/bin/codex"),
             package.deletingLastPathComponent().appendingPathComponent("codex-\(platform)/vendor/\(triple)/bin/codex")]
        }
    }
}

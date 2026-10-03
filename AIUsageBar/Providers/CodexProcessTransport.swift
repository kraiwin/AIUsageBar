import CoreFoundation
import Darwin
import Foundation

/// All mutable process/pipe state stays on one utility queue. The only shared
/// value is this locked cancellation bit; cancellation never races a PID lookup.
final class CodexOperationControl: @unchecked Sendable {
    private let lock = NSLock()
    private var cancelled = false
    func cancel() { lock.withLock { cancelled = true } }
    func check() throws {
        if lock.withLock({ cancelled }) { throw CodexProviderError.cancelled }
    }
}

enum CodexRPCEnvelope {
    static func discardNotification(_ line: Data) throws {
        guard let value = try? JSONSerialization.jsonObject(with: line),
              let envelope = value as? [String: Any], envelope["id"] == nil,
              envelope["method"] != nil else { throw CodexProviderError.unexpectedResponse }
        _ = try result(line, expectedID: 0)
    }
    /// nil is a valid notification which is discarded, including future methods.
    static func result(_ line: Data, expectedID: Int) throws -> [String: Any]? {
        guard let value = try? JSONSerialization.jsonObject(with: line),
              let envelope = value as? [String: Any] else { throw CodexProviderError.invalidEnvelope }
        if let methodValue = envelope["method"] {
            guard let method = methodValue as? String, !method.isEmpty,
                  envelope["result"] == nil, envelope["error"] == nil else {
                throw CodexProviderError.invalidEnvelope
            }
            guard envelope["id"] == nil, method != "account/chatgptAuthTokens/refresh" else {
                throw CodexProviderError.serverRequest
            }
            guard envelope["params"] == nil || envelope["params"] as? [String: Any] != nil else {
                throw CodexProviderError.invalidEnvelope
            }
            return nil
        }
        guard let number = envelope["id"] as? NSNumber,
              CFGetTypeID(number) != CFBooleanGetTypeID(), number.doubleValue == Double(expectedID) else {
            throw CodexProviderError.unexpectedResponse
        }
        guard envelope["params"] == nil, (envelope["result"] != nil) != (envelope["error"] != nil) else {
            throw CodexProviderError.invalidEnvelope
        }
        if let error = envelope["error"] as? [String: Any] {
            guard let code = error["code"] as? NSNumber,
                  CFGetTypeID(code) != CFBooleanGetTypeID(),
                  code.doubleValue.rounded() == code.doubleValue,
                  (-2_147_483_648...2_147_483_647).contains(code.doubleValue), error["message"] is String else {
                throw CodexProviderError.invalidEnvelope
            }
            throw CodexProviderError.serverError(code: code.intValue)
        }
        guard let result = envelope["result"] as? [String: Any] else { throw CodexProviderError.invalidEnvelope }
        return result
    }
}

/// Internal stdio channel, never exposed to UI. Uses bounded nonblocking reads
/// and monotonic deadlines, draining stderr without retaining diagnostic text.
final class CodexProcessTransport {
    private var childPID: pid_t = 0
    private var reaped = false
    private let input = Pipe()
    private let output = Pipe()
    private let diagnostics = Pipe()
    private let control: CodexOperationControl
    private let deadline: UInt64
    private var buffer = Data()
    private var stdoutBytes = 0
    private var stderrBytes = 0
    private var lines = 0
    private var stdoutEnded = false
    private var stderrEnded = false
    private var closed = false
    private var nextID = 1
    private static let totalLimit = 16 * 1024 * 1024
    private static let configLimit = 2 * 1024 * 1024

    init(executable: URL, arguments: [String], cwd: URL,
         deadline: UInt64, control: CodexOperationControl) throws {
        self.control = control
        self.deadline = deadline
        try control.check()
        try spawn(executable: executable, arguments: arguments, cwd: cwd)
        // Only parent endpoints remain open. Children closing stdout then produce EOF.
        try? input.fileHandleForReading.close()
        try? output.fileHandleForWriting.close()
        try? diagnostics.fileHandleForWriting.close()
        for handle in [input.fileHandleForWriting, output.fileHandleForReading, diagnostics.fileHandleForReading] {
            let fd = handle.fileDescriptor
            let flags = fcntl(fd, F_GETFL)
            guard flags >= 0, fcntl(fd, F_SETFL, flags | O_NONBLOCK) >= 0 else {
                _ = closeAndReap()
                throw CodexProviderError.ioFailure
            }
        }
        guard fcntl(input.fileHandleForWriting.fileDescriptor, F_SETNOSIGPIPE, 1) >= 0 else {
            _ = closeAndReap()
            throw CodexProviderError.ioFailure
        }
    }

    deinit { _ = closeAndReap() }

    func initialize() throws {
        let response = try request("initialize", params: ["clientInfo": ["name": "aiusagebar", "title": "AIUsageBar", "version": "0.1.0"]])
        guard let agent = response["userAgent"] as? String, !agent.isEmpty else {
            throw CodexProviderError.invalidEnvelope
        }
        try write(["method": "initialized", "params": [:]])
    }

    func registry() throws {
        var features: [CodexLaunchPolicy.Feature] = []
        var cursor: String?
        var seen = Set<String>()
        for _ in 0..<32 {
            var params: [String: Any] = ["limit": 100]
            if let cursor { params["cursor"] = cursor }
            let page = try CodexLaunchPolicy.registryPage(request("experimentalFeature/list", params: params))
            features += page.features
            guard features.count <= 2048 else { throw CodexProviderError.unsupportedConfiguration }
            guard let next = page.cursor else {
                try CodexLaunchPolicy.assertRegistry(features)
                return
            }
            guard seen.insert(next).inserted else { throw CodexProviderError.unsupportedConfiguration }
            cursor = next
        }
        throw CodexProviderError.unsupportedConfiguration
    }

    func inventory(cwd: URL, requireDisabled: Bool) throws -> [CodexLaunchPolicy.Server] {
        try CodexLaunchPolicy.inventory(request("config/read", params: ["includeLayers": false, "cwd": cwd.path]),
                                        requireDisabled: requireDisabled)
    }

    func quota() throws -> CodexRPCSession.Completion {
        let observedAt = Date()
        let result = try request("account/rateLimits/read", lineLimit: 65_536)
        let receivedAt = Date()
        guard receivedAt >= observedAt,
              result["rateLimits"] != nil || result["rateLimitsByLimitId"] != nil else {
            throw CodexProviderError.invalidQuota
        }
        let fields = result.filter { ["rateLimits", "rateLimitsByLimitId"].contains($0.key) }
        let reading: UsageReading?
        do {
            reading = try UsagePayloadParser.parseCodex(JSONSerialization.data(withJSONObject: fields), receivedAt: receivedAt)
        } catch { throw CodexProviderError.invalidQuota }
        // Official 0.160 account processor performs a BackendClient usage fetch on
        // every read; request time is a conservative upstream observation bound.
        let verified = reading.map {
            UsageReading(provider: $0.provider, weekly: $0.weekly, session: $0.session,
                         source: $0.source, receivedAt: receivedAt, providerObservedAt: observedAt)
        }
        return .init(reading: verified, receivedAt: receivedAt)
    }

    private func request(_ method: String, params: [String: Any]? = nil,
                         lineLimit: Int = configLimit) throws -> [String: Any] {
        try check()
        // Responses already received while no matching request was outstanding
        // cannot become valid merely because the next request uses that ID.
        try discardPendingNotifications(lineLimit: lineLimit)
        if stdoutEnded { throw CodexProviderError.childExited }
        var inheritedPartialLine = !buffer.isEmpty
        let identifier = nextID
        nextID += 1
        var message: [String: Any] = ["id": identifier, "method": method]
        if let params { message["params"] = params }
        try write(message)
        while true {
            try check()
            if let newline = buffer.firstIndex(of: 10) {
                let lineLength = buffer.distance(from: buffer.startIndex, to: newline)
                guard lineLength <= lineLimit else { throw CodexProviderError.outputLimit }
                let line = Data(buffer[..<newline])
                buffer.removeSubrange(buffer.startIndex...newline)
                lines += 1
                guard lines <= 4096 else { throw CodexProviderError.outputLimit }
                if inheritedPartialLine {
                    try CodexRPCEnvelope.discardNotification(line)
                    inheritedPartialLine = false
                } else if let result = try CodexRPCEnvelope.result(line, expectedID: identifier) { return result }
            } else {
                guard buffer.count <= lineLimit else { throw CodexProviderError.outputLimit }
                if stdoutEnded { throw CodexProviderError.childExited }
                try pump()
            }
        }
    }

    private func discardPendingNotifications(lineLimit: Int) throws {
        for _ in 0..<4096 {
            try check()
            while let newline = buffer.firstIndex(of: 10) {
                let length = buffer.distance(from: buffer.startIndex, to: newline)
                guard length <= lineLimit else { throw CodexProviderError.outputLimit }
                let line = Data(buffer[..<newline])
                buffer.removeSubrange(buffer.startIndex...newline)
                lines += 1
                guard lines <= 4096 else { throw CodexProviderError.outputLimit }
                try CodexRPCEnvelope.discardNotification(line)
            }
            guard buffer.count <= lineLimit else { throw CodexProviderError.outputLimit }
            if !(try pump(timeout: 0)) { return }
        }
        throw CodexProviderError.outputLimit
    }

    private func write(_ value: [String: Any]) throws {
        let bytes: Data
        do { bytes = try JSONSerialization.data(withJSONObject: value) + Data([10]) }
        catch { throw CodexProviderError.invalidEnvelope }
        guard bytes.count <= 8192 else { throw CodexProviderError.outputLimit }
        var offset = 0
        while offset < bytes.count {
            try check()
            let count = bytes.withUnsafeBytes { pointer in
                Darwin.write(input.fileHandleForWriting.fileDescriptor, pointer.baseAddress?.advanced(by: offset), bytes.count - offset)
            }
            if count > 0 { offset += count }
            else if count < 0 && (errno == EAGAIN || errno == EINTR) { try pump() }
            else { throw CodexProviderError.ioFailure }
        }
    }

    private func check() throws {
        try control.check()
        if DispatchTime.now().uptimeNanoseconds >= deadline { throw CodexProviderError.timeout }
    }

    @discardableResult
    private func pump(checkLimits: Bool = true, timeout: Int32 = 25) throws -> Bool {
        var fds = [pollfd(fd: stdoutEnded ? -1 : output.fileHandleForReading.fileDescriptor, events: Int16(POLLIN), revents: 0),
                   pollfd(fd: stderrEnded ? -1 : diagnostics.fileHandleForReading.fileDescriptor, events: Int16(POLLIN), revents: 0)]
        let status = poll(&fds, nfds_t(fds.count), timeout)
        if status < 0 {
            if errno == EINTR { return false }
            throw CodexProviderError.ioFailure
        }
        for index in fds.indices where fds[index].revents != 0 {
            guard fds[index].revents & Int16(POLLNVAL) == 0 else { throw CodexProviderError.ioFailure }
            var chunk = [UInt8](repeating: 0, count: 8192)
            let count = Darwin.read(fds[index].fd, &chunk, chunk.count)
            if count > 0 {
                if index == 0 {
                    stdoutBytes += count
                    if checkLimits {
                        guard stdoutBytes <= Self.totalLimit, buffer.count + count <= Self.configLimit + 8192 else {
                            throw CodexProviderError.outputLimit
                        }
                        buffer.append(contentsOf: chunk.prefix(count))
                    }
                } else {
                    stderrBytes += count
                    if checkLimits && stderrBytes > Self.totalLimit { throw CodexProviderError.outputLimit }
                    // Never retain or log diagnostics: they may contain raw config.
                }
            } else if count == 0 {
                if index == 0 { stdoutEnded = true } else { stderrEnded = true }
            } else if errno != EAGAIN && errno != EINTR { throw CodexProviderError.ioFailure }
        }
        return status > 0
    }

    private func spawn(executable: URL, arguments: [String], cwd: URL) throws {
        var actions: posix_spawn_file_actions_t?
        var attributes: posix_spawnattr_t?
        guard posix_spawn_file_actions_init(&actions) == 0,
              posix_spawnattr_init(&attributes) == 0 else { throw CodexProviderError.launchFailed }
        defer {
            posix_spawn_file_actions_destroy(&actions)
            posix_spawnattr_destroy(&attributes)
        }
        let endpoints = [input.fileHandleForReading.fileDescriptor, input.fileHandleForWriting.fileDescriptor,
                         output.fileHandleForReading.fileDescriptor, output.fileHandleForWriting.fileDescriptor,
                         diagnostics.fileHandleForReading.fileDescriptor, diagnostics.fileHandleForWriting.fileDescriptor]
        for (source, target) in [(endpoints[0], STDIN_FILENO), (endpoints[3], STDOUT_FILENO), (endpoints[5], STDERR_FILENO)] {
            guard posix_spawn_file_actions_adddup2(&actions, source, target) == 0 else { throw CodexProviderError.launchFailed }
        }
        for fd in endpoints where fd > STDERR_FILENO {
            guard posix_spawn_file_actions_addclose(&actions, fd) == 0 else { throw CodexProviderError.launchFailed }
        }
        // A fresh group belongs only to this invocation; a stalled descendant
        // cannot keep output pipes or survive cancellation. No user CLI joins it.
        let directoryStatus: Int32
        if #available(macOS 26.0, *) {
            directoryStatus = posix_spawn_file_actions_addchdir(&actions, cwd.path)
        } else {
            directoryStatus = posix_spawn_file_actions_addchdir_np(&actions, cwd.path)
        }
        guard directoryStatus == 0,
              posix_spawnattr_setpgroup(&attributes, 0) == 0,
              posix_spawnattr_setflags(&attributes, Int16(POSIX_SPAWN_SETPGROUP | POSIX_SPAWN_CLOEXEC_DEFAULT)) == 0 else {
            throw CodexProviderError.launchFailed
        }
        let argv = ([executable.path] + arguments).map { strdup($0) } + [nil]
        let environment = ProcessInfo.processInfo.environment.map { strdup("\($0.key)=\($0.value)") } + [nil]
        defer {
            for pointer in argv { free(pointer) }
            for pointer in environment { free(pointer) }
        }
        guard !argv.dropLast().contains(where: { $0 == nil }),
              !environment.dropLast().contains(where: { $0 == nil }) else { throw CodexProviderError.launchFailed }
        let status = argv.withUnsafeBufferPointer { args in
            environment.withUnsafeBufferPointer { env in
                posix_spawn(&childPID, executable.path, &actions, &attributes,
                            UnsafeMutablePointer(mutating: args.baseAddress), UnsafeMutablePointer(mutating: env.baseAddress))
            }
        }
        guard status == 0 else { childPID = 0; throw CodexProviderError.launchFailed }
    }

    /// Signal the invocation's owned process group before reaping its leader.
    /// Keeping the leader unreaped until the last signal prevents PID reuse from
    /// targeting another invocation. stdout/stderr are drained during the grace.
    @discardableResult
    func closeAndReap() -> Bool {
        if closed { return reaped }
        closed = true
        guard childPID > 0 else { return true }
        try? input.fileHandleForWriting.close()
        buffer.removeAll(keepingCapacity: false)
        let grace = DispatchTime.now().uptimeNanoseconds + 300_000_000
        while !(stdoutEnded && stderrEnded) && DispatchTime.now().uptimeNanoseconds < grace { _ = try? pump(checkLimits: false) }
        _ = Darwin.kill(-childPID, SIGTERM)
        let terminateLimit = DispatchTime.now().uptimeNanoseconds + 200_000_000
        while !(stdoutEnded && stderrEnded) && DispatchTime.now().uptimeNanoseconds < terminateLimit { _ = try? pump(checkLimits: false) }
        _ = Darwin.kill(-childPID, SIGKILL)
        let reapLimit = DispatchTime.now().uptimeNanoseconds + 1_000_000_000
        var status: Int32 = 0
        repeat {
            let result = waitpid(childPID, &status, WNOHANG)
            if result == childPID { reaped = true; break }
            if result < 0 && errno != EINTR { break }
            _ = try? pump(checkLimits: false)
        } while DispatchTime.now().uptimeNanoseconds < reapLimit
        try? output.fileHandleForReading.close()
        try? diagnostics.fileHandleForReading.close()
        return reaped
    }
}

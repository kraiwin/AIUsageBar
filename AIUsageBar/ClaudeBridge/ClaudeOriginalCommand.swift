import Darwin
import Foundation

/// Executes the original shell command with inherited stdout/stderr. Stdin is
/// forwarded as bytes; only a bounded in-memory copy is eligible for ingestion.
/// Children run in their own process group so cancellation reaches descendants.
enum ClaudeOriginalCommand {
    private final class Cancellation: @unchecked Sendable {
        private let lock = NSLock()
        private var processGroup: pid_t = 0
        private var receivedSignal: Int32 = 0
        var signalNumber: Int32 { lock.withLock { receivedSignal } }
        func activate(_ pid: pid_t) {
            lock.withLock {
                processGroup = pid
                if receivedSignal != 0, pid > 0 { kill(-pid, receivedSignal) }
            }
        }
        func cancel(_ number: Int32) {
            lock.withLock {
                receivedSignal = number
                if processGroup > 0 { kill(-processGroup, number) }
            }
        }
    }

    static func run(command: String, helperPath: String, directory: URL) -> Int32 {
        signal(SIGPIPE, SIG_IGN)
        var descriptors: [Int32] = [0, 0]
        guard pipe(&descriptors) == 0 else { return 127 }
        var child: pid_t = 0
        do {
            child = try spawn(path: "/bin/sh", arguments: ["-c", command], input: descriptors[0], closing: descriptors[1])
        } catch { close(descriptors[0]); close(descriptors[1]); return 127 }
        close(descriptors[0])
        _ = fcntl(descriptors[1], F_SETFL, O_NONBLOCK)
        let state = Cancellation()
        state.activate(child)
        let cancellation = [SIGINT, SIGTERM, SIGHUP].map { number -> DispatchSourceSignal in
            signal(number, SIG_IGN)
            let source = DispatchSource.makeSignalSource(signal: number, queue: .global())
            source.setEventHandler { state.cancel(number) }
            source.resume()
            return source
        }
        defer { cancellation.forEach { $0.cancel() } }
        var captured: Data? = Data()
        var buffer = [UInt8](repeating: 0, count: 8_192)
        var pipeOpen = true
        var status: Int32 = 0
        var reaped = false
        var postExitDeadline: TimeInterval?
        while true {
            if state.signalNumber != 0 { captured = nil; break }
            if !reaped, waitpid(child, &status, WNOHANG) == child {
                reaped = true
                state.activate(0)
                postExitDeadline = ProcessInfo.processInfo.systemUptime + 0.5
                if pipeOpen { close(descriptors[1]); pipeOpen = false }
            }
            if let deadline = postExitDeadline, ProcessInfo.processInfo.systemUptime >= deadline {
                captured = nil; break
            }
            var input = pollfd(fd: STDIN_FILENO, events: Int16(POLLIN), revents: 0)
            let ready = poll(&input, 1, 50)
            if ready == 0 { continue }
            if ready < 0 { if errno == EINTR { continue }; captured = nil; break }
            let count = Darwin.read(STDIN_FILENO, &buffer, buffer.count)
            if count < 0 { if errno == EINTR { continue }; captured = nil; break }
            if count == 0 { break }
            if let data = captured {
                if data.count + count <= ClaudePrivateFiles.maximumSettingsBytes {
                    captured?.append(contentsOf: buffer.prefix(count))
                } else { captured = nil }
            }
            if pipeOpen {
                var offset = 0
                while offset < count {
                    if state.signalNumber != 0 {
                        captured = nil; close(descriptors[1]); pipeOpen = false; break
                    }
                    let written = buffer.withUnsafeBytes { bytes in
                        Darwin.write(descriptors[1], bytes.baseAddress?.advanced(by: offset), count - offset)
                    }
                    if written < 0 {
                        if errno == EINTR { continue }
                        if errno == EAGAIN {
                            if waitpid(child, &status, WNOHANG) == child {
                                reaped = true; state.activate(0)
                                postExitDeadline = ProcessInfo.processInfo.systemUptime + 0.5
                                close(descriptors[1]); pipeOpen = false; break
                            }
                            usleep(1_000); continue
                        }
                        close(descriptors[1]); pipeOpen = false; break
                    }
                    offset += written
                }
            }
        }
        if pipeOpen { close(descriptors[1]) }
        let cancellationDeadline = ProcessInfo.processInfo.systemUptime + 0.5
        while !reaped {
            let result = waitpid(child, &status, WNOHANG)
            if result == child { reaped = true; break }
            if result < 0, errno != EINTR { return 127 }
            if state.signalNumber != 0, ProcessInfo.processInfo.systemUptime >= cancellationDeadline {
                kill(-child, SIGKILL)
            }
            usleep(1_000)
        }
        state.activate(0)
        if state.signalNumber != 0 { return terminateLikeSignal(state.signalNumber) }
        let terminationSignal = status & 0x7f
        if terminationSignal != 0 {
            return terminateLikeSignal(terminationSignal)
        }
        let result = (status >> 8) & 0xff
        if let captured { ingest(captured, helperPath: helperPath, directory: directory, cancellation: state) }
        if state.signalNumber != 0 { return terminateLikeSignal(state.signalNumber) }
        return result
    }

    private static func terminateLikeSignal(_ number: Int32) -> Int32 {
        signal(number, SIG_DFL)
        kill(getpid(), number)
        return 128 + number
    }

    private static func ingest(_ data: Data, helperPath: String, directory: URL, cancellation: Cancellation) {
        guard access(helperPath, X_OK) == 0 else { return }
        var pipes: [Int32] = [0, 0]
        guard pipe(&pipes) == 0 else { return }
        var child: pid_t = 0
        do { child = try spawn(path: helperPath, arguments: ["ingest", directory.path], input: pipes[0], closing: pipes[1], silent: true) }
        catch { close(pipes[0]); close(pipes[1]); return }
        cancellation.activate(child)
        defer { cancellation.activate(0) }
        close(pipes[0])
        // Poll writes and process completion against one monotonic deadline so a
        // broken helper can never keep Claude's status line waiting indefinitely.
        let deadline = ProcessInfo.processInfo.systemUptime + 2
        _ = fcntl(pipes[1], F_SETFL, O_NONBLOCK)
        data.withUnsafeBytes { bytes in
            var offset = 0
            while offset < bytes.count, ProcessInfo.processInfo.systemUptime < deadline, cancellation.signalNumber == 0 {
                let count = Darwin.write(pipes[1], bytes.baseAddress?.advanced(by: offset), bytes.count - offset)
                if count > 0 { offset += count }
                else if errno == EAGAIN || errno == EINTR { usleep(1_000) }
                else { break }
            }
        }
        close(pipes[1])
        var status: Int32 = 0
        while ProcessInfo.processInfo.systemUptime < deadline {
            if waitpid(child, &status, WNOHANG) == child { return }
            if cancellation.signalNumber != 0 { break }
            usleep(1_000)
        }
        kill(-child, SIGKILL)
        while waitpid(child, &status, 0) < 0, errno == EINTR { }
    }

    private static func spawn(path: String, arguments: [String], input: Int32,
                              closing: Int32, silent: Bool = false) throws -> pid_t {
        var actions: posix_spawn_file_actions_t?
        var attributes: posix_spawnattr_t?
        guard posix_spawn_file_actions_init(&actions) == 0, posix_spawnattr_init(&attributes) == 0 else {
            throw ClaudeBridgeError.ioFailure
        }
        defer { posix_spawn_file_actions_destroy(&actions); posix_spawnattr_destroy(&attributes) }
        posix_spawn_file_actions_adddup2(&actions, input, STDIN_FILENO)
        posix_spawn_file_actions_addclose(&actions, input)
        posix_spawn_file_actions_addclose(&actions, closing)
        if silent {
            posix_spawn_file_actions_addopen(&actions, STDOUT_FILENO, "/dev/null", O_WRONLY, 0)
            posix_spawn_file_actions_addopen(&actions, STDERR_FILENO, "/dev/null", O_WRONLY, 0)
        }
        var defaults = sigset_t(), mask = sigset_t()
        sigemptyset(&defaults); sigemptyset(&mask)
        for number in [SIGINT, SIGTERM, SIGHUP, SIGPIPE] { sigaddset(&defaults, number) }
        posix_spawnattr_setsigdefault(&attributes, &defaults)
        posix_spawnattr_setsigmask(&attributes, &mask)
        posix_spawnattr_setpgroup(&attributes, 0)
        posix_spawnattr_setflags(&attributes, Int16(POSIX_SPAWN_SETPGROUP | POSIX_SPAWN_SETSIGDEF | POSIX_SPAWN_SETSIGMASK))
        let strings = ([path] + arguments).map { strdup($0) }
        defer { strings.forEach { free($0) } }
        var argv = strings + [nil]
        var pid: pid_t = 0
        guard posix_spawn(&pid, path, &actions, &attributes, &argv, environ) == 0 else { throw ClaudeBridgeError.ioFailure }
        return pid
    }
}

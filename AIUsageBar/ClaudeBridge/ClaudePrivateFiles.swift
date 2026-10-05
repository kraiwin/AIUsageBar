import Darwin
import Foundation

enum ClaudeBridgeError: Error, Equatable, Sendable {
    case missingFile, unsafeFile, oversized, invalidSnapshot, invalidSettings, conflict, ioFailure, alreadyInstalled
}

/// Uses directory-relative, no-follow opens. Files are user-owned regular files,
/// never hard links; private artifacts cannot be readable by other users.
enum ClaudePrivateFiles {
    static let maximumSettingsBytes = 2 * 1_024 * 1_024

    static func directory(_ url: URL, create: Bool = false) throws -> Int32 {
        if create {
            do { try FileManager.default.createDirectory(at: url, withIntermediateDirectories: true,
                                                        attributes: [.posixPermissions: 0o700]) }
            catch { throw ClaudeBridgeError.ioFailure }
        }
        let fd = open(url.path, O_RDONLY | O_DIRECTORY | O_NOFOLLOW | O_CLOEXEC)
        guard fd >= 0 else { throw errno == ENOENT ? ClaudeBridgeError.missingFile : ClaudeBridgeError.unsafeFile }
        var info = stat()
        guard fstat(fd, &info) == 0, info.st_uid == getuid(), info.st_mode & 0o077 == 0 else {
            close(fd); throw ClaudeBridgeError.unsafeFile
        }
        return fd
    }

    static func read(_ url: URL, limit: Int = maximumSettingsBytes,
                     privateMode: Bool = true) throws -> Data {
        let parent = open(url.deletingLastPathComponent().path, O_RDONLY | O_DIRECTORY | O_NOFOLLOW | O_CLOEXEC)
        guard parent >= 0 else { throw errno == ENOENT ? ClaudeBridgeError.missingFile : ClaudeBridgeError.unsafeFile }
        defer { close(parent) }
        return try read(name: url.lastPathComponent, directoryFD: parent, limit: limit, privateMode: privateMode)
    }

    static func read(name: String, directoryFD: Int32, limit: Int = maximumSettingsBytes,
                     privateMode: Bool = true) throws -> Data {
        guard !name.contains("/"), name != ".", name != ".." else { throw ClaudeBridgeError.unsafeFile }
        let fd = openat(directoryFD, name, O_RDONLY | O_NOFOLLOW | O_CLOEXEC | O_NONBLOCK)
        guard fd >= 0 else { throw errno == ENOENT ? ClaudeBridgeError.missingFile : ClaudeBridgeError.unsafeFile }
        defer { close(fd) }
        var info = stat()
        guard fstat(fd, &info) == 0, info.st_uid == getuid(),
              info.st_mode & S_IFMT == S_IFREG, info.st_nlink == 1,
              info.st_mode & (privateMode ? 0o077 : 0o022) == 0 else {
            throw ClaudeBridgeError.unsafeFile
        }
        guard info.st_size <= limit else { throw ClaudeBridgeError.oversized }
        var result = Data(), buffer = [UInt8](repeating: 0, count: 8_192)
        while true {
            let count = Darwin.read(fd, &buffer, min(buffer.count, limit + 1 - result.count))
            if count < 0 { if errno == EINTR { continue }; throw ClaudeBridgeError.ioFailure }
            if count == 0 { break }
            result.append(contentsOf: buffer.prefix(count))
            guard result.count <= limit else { throw ClaudeBridgeError.oversized }
        }
        return result
    }

    /// Caller holds the directory lock. Replaces only a safe existing artifact.
    static func atomicWrite(_ data: Data, name: String, directoryFD: Int32,
                            mode: mode_t = 0o600, replaceExisting: Bool = true) throws {
        guard !name.contains("/"), name != ".", name != ".." else { throw ClaudeBridgeError.unsafeFile }
        var old = stat()
        if fstatat(directoryFD, name, &old, AT_SYMLINK_NOFOLLOW) == 0 {
            guard old.st_uid == getuid(), old.st_mode & S_IFMT == S_IFREG,
                  old.st_nlink == 1, old.st_mode & 0o022 == 0 else { throw ClaudeBridgeError.unsafeFile }
        } else if errno != ENOENT { throw ClaudeBridgeError.ioFailure }
        let temporary = ".tmp-" + UUID().uuidString
        let fd = openat(directoryFD, temporary, O_WRONLY | O_CREAT | O_EXCL | O_NOFOLLOW | O_CLOEXEC, mode)
        guard fd >= 0 else { throw ClaudeBridgeError.ioFailure }
        defer { close(fd); unlinkat(directoryFD, temporary, 0) }
        try data.withUnsafeBytes { bytes in
            var offset = 0
            while offset < bytes.count {
                let count = Darwin.write(fd, bytes.baseAddress?.advanced(by: offset), bytes.count - offset)
                if count < 0 { if errno == EINTR { continue }; throw ClaudeBridgeError.ioFailure }
                offset += count
            }
        }
        guard fsync(fd) == 0 else { throw ClaudeBridgeError.ioFailure }
        let renamed = replaceExisting
            ? renameat(directoryFD, temporary, directoryFD, name)
            : renameatx_np(directoryFD, temporary, directoryFD, name, UInt32(RENAME_EXCL))
        guard renamed == 0 else {
            throw !replaceExisting && errno == EEXIST ? ClaudeBridgeError.conflict : ClaudeBridgeError.ioFailure
        }
        guard fsync(directoryFD) == 0 else { throw ClaudeBridgeError.ioFailure }
    }

    static func withLock<T>(directoryFD: Int32, _ body: () throws -> T) throws -> T {
        // APFS can return ENOENT while concurrent O_CREAT opens race to create
        // this lock. Retry only that transient case (or EINTR), still using the
        // pinned directory and no-follow flags. All other errors fail closed.
        var fd: Int32 = -1
        for _ in 0..<8 {
            fd = openat(directoryFD, ".bridge-lock", O_RDWR | O_CREAT | O_NOFOLLOW | O_CLOEXEC | O_NONBLOCK, 0o600)
            if fd >= 0 { break }
            if errno != ENOENT && errno != EINTR { break }
            usleep(1_000)
        }
        guard fd >= 0 else { throw ClaudeBridgeError.unsafeFile }
        defer { close(fd) }
        var info = stat()
        guard fstat(fd, &info) == 0, info.st_uid == getuid(), info.st_mode & S_IFMT == S_IFREG,
              info.st_nlink == 1, info.st_mode & 0o077 == 0, flock(fd, LOCK_EX) == 0 else {
            throw ClaudeBridgeError.unsafeFile
        }
        defer { flock(fd, LOCK_UN) }
        return try body()
    }
}

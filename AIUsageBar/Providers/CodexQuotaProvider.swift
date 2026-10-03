import Foundation

/// Default direct app-server context only. The official CLI owns auth/state and
/// may refresh its own credentials. This module never reads or stores tokens.
/// Each fetch inventories one child, reaps it, then validates a guarded second
/// child before requesting quota. No generic command/RPC API escapes this type.
final class CodexQuotaProvider: @unchecked Sendable {
    private let executableURL: URL
    private let workingDirectory: URL
    private let timeout: TimeInterval
    private let lock = NSLock()
    private var active: CodexOperationControl?
    private let queue = DispatchQueue(label: "AIUsageBar.CodexQuota", qos: .utility)

    init(executableURL: URL, workingDirectory: URL = FileManager.default.homeDirectoryForCurrentUser,
         timeout: TimeInterval = 30) {
        self.executableURL = executableURL
        self.workingDirectory = workingDirectory
        self.timeout = timeout
    }

    func fetchQuota() async throws -> CodexRPCSession.Completion {
        let operation = CodexOperationControl()
        let acquired = lock.withLock { () -> Bool in
            guard active == nil else { return false }
            active = operation
            return true
        }
        guard acquired else { throw CodexProviderError.busy }
        defer { lock.withLock { active = nil } }
        return try await withTaskCancellationHandler {
            if Task.isCancelled { operation.cancel() }
            return try await withCheckedThrowingContinuation { continuation in
                queue.async { [self] in
                    do { continuation.resume(returning: try execute(operation)) }
                    catch let error as CodexProviderError { continuation.resume(throwing: error) }
                    catch { continuation.resume(throwing: CodexProviderError.ioFailure) }
                }
            }
        } onCancel: { operation.cancel() }
    }

    func cancel() { lock.withLock { active }?.cancel() }

    private func execute(_ operation: CodexOperationControl) throws -> CodexRPCSession.Completion {
        try operation.check()
        try CodexExecutableDiscovery.validate(executableURL)
        guard timeout.isFinite, (0.05...120).contains(timeout), workingDirectory.isFileURL else {
            throw CodexProviderError.unsupportedConfiguration
        }
        let deadline = DispatchTime.now().uptimeNanoseconds + UInt64(timeout * 1_000_000_000)
        let first = try CodexProcessTransport(executable: executableURL,
            arguments: CodexLaunchPolicy.arguments(), cwd: workingDirectory, deadline: deadline, control: operation)
        let inventory: [CodexLaunchPolicy.Server]
        do {
            try first.initialize()
            try first.registry()
            inventory = try first.inventory(cwd: workingDirectory, requireDisabled: false)
        } catch {
            guard first.closeAndReap() else { throw CodexProviderError.cleanupFailed }
            throw error
        }
        guard first.closeAndReap() else { throw CodexProviderError.cleanupFailed }
        try operation.check()
        let second = try CodexProcessTransport(executable: executableURL,
            arguments: CodexLaunchPolicy.arguments(disabling: inventory), cwd: workingDirectory,
            deadline: deadline, control: operation)
        do {
            try second.initialize()
            try second.registry()
            let disabled = try second.inventory(cwd: workingDirectory, requireDisabled: true)
            try CodexLaunchPolicy.assertSameInventory(inventory, disabled)
            let quota = try second.quota()
            guard second.closeAndReap() else { throw CodexProviderError.cleanupFailed }
            try operation.check()
            return quota
        } catch {
            guard second.closeAndReap() else { throw CodexProviderError.cleanupFailed }
            throw error
        }
    }
}

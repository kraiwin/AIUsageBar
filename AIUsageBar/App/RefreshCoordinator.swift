import AppKit
import Combine
import Foundation

@MainActor
final class RefreshCoordinator: ObservableObject {
    typealias Fetch = @Sendable () async throws -> UsageReading?
    typealias ReadSnapshot = @Sendable () throws -> UsageReading?

    @Published private(set) var store = UsageStatusStore()
    @Published private(set) var isRefreshing = false
    @Published private(set) var cooldownSeconds = 0
    @Published private(set) var message: String?
    @Published private(set) var now = Date()
    @Published private(set) var codexPath: String?

    private let defaults: UserDefaults
    private var policy: RefreshPolicy
    private var fetch: Fetch?
    private var cancelProvider: (() -> Void)?
    private var readSnapshot: ReadSnapshot?
    private var task: Task<Void, Never>?
    private var taskID: UUID?
    private var retiringTasks: [UUID: Task<Void, Never>] = [:]
    private var generation: UInt64 = 0
    private var timer: Timer?
    private var nextPoll = Date()
    private var wakeObserver: NSObjectProtocol?

    init(defaults: UserDefaults = .standard, fetch: Fetch? = nil, cancel: (() -> Void)? = nil) {
        self.defaults = defaults
        self.fetch = fetch
        self.cancelProvider = cancel
        let persisted = defaults.object(forKey: "codexLastAttempt") as? Date
        policy = RefreshPolicy(lastAttempt: persisted)
        codexPath = defaults.string(forKey: "codexExecutablePath")
        let arguments = ProcessInfo.processInfo.arguments
        if let index = arguments.firstIndex(of: "--codex-path"), arguments.indices.contains(index + 1) {
            // Explicit CLI selection is confirmation of this path; never auto-select a discovery result.
            codexPath = arguments[index + 1]
        }
    }

    func start(snapshot: @escaping ReadSnapshot) {
        readSnapshot = snapshot
        if let codexPath { connectCodex(URL(fileURLWithPath: codexPath)) }
        refresh()
        timer = Timer.scheduledTimer(withTimeInterval: 5, repeats: true) { [weak self] _ in
            Task { @MainActor [weak self] in self?.tick() }
        }
        wakeObserver = NSWorkspace.shared.notificationCenter.addObserver(
            forName: NSWorkspace.didWakeNotification, object: nil, queue: .main
        ) { [weak self] _ in Task { @MainActor [weak self] in self?.refresh() } }
    }

    func connectCodex(_ url: URL) {
        cancelCurrent()
        do {
            try CodexExecutableDiscovery.validate(url)
            let provider = CodexQuotaProvider(executableURL: url)
            fetch = {
                try await provider.fetchQuota().reading
            }
            cancelProvider = { provider.cancel() }
            codexPath = url.path
            defaults.set(url.path, forKey: "codexExecutablePath")
            message = nil
        } catch {
            message = "ไม่พบ Codex native CLI ที่ใช้ได้ กรุณาเลือกไฟล์ใหม่"
            fetch = nil
            codexPath = nil
            defaults.removeObject(forKey: "codexExecutablePath")
            store.disconnect(provider: .codex)
        }
    }

    func disconnectCodex() {
        cancelCurrent()
        fetch = nil
        codexPath = nil
        defaults.removeObject(forKey: "codexExecutablePath")
        store.disconnect(provider: .codex)
    }

    func setMessage(_ value: String?) { message = value }

    func refresh() {
        updateClock()
        readClaude()
        guard !isRefreshing, let fetch else { return }
        guard policy.begin(at: now) else {
            updateCooldown()
            return
        }
        defaults.set(policy.lastAttempt, forKey: "codexLastAttempt")
        updateCooldown()
        isRefreshing = true
        generation &+= 1
        let requestGeneration = generation
        let requestID = UUID()
        taskID = requestID
        store.startLoading(provider: .codex)
        nextPoll = now.addingTimeInterval(RefreshPolicy.pollingInterval)
        task = Task { [weak self] in
            defer { self?.retiringTasks.removeValue(forKey: requestID) }
            do {
                let reading = try await fetch()
                guard let self, !Task.isCancelled, self.generation == requestGeneration else { return }
                self.message = nil
                if let reading { self.store.receive(reading) }
                else { self.store.missingData(provider: .codex) }
            } catch {
                guard let self, !Task.isCancelled, self.generation == requestGeneration else { return }
                self.store.fail(provider: .codex)
                switch error as? CodexProviderError {
                case .childExited, .unsupportedConfiguration:
                    self.message = "Codex โหลดไม่สำเร็จ อาจเป็นเพราะไฟล์ตั้งค่ามีค่าที่ CLI รุ่นนี้ไม่รู้จัก หรือการตั้งค่าไม่ตรงกับที่แอปรองรับ ตรวจ CLI และไฟล์ตั้งค่า"
                default:
                    self.message = "Codex โหลดไม่สำเร็จ ตรวจ CLI/login และการตั้งค่าที่รองรับ"
                }
            }
            guard let self, self.generation == requestGeneration else { return }
            self.isRefreshing = false
            self.task = nil
            self.taskID = nil
        }
    }

    private func readClaude() {
        guard let readSnapshot else { return }
        do {
            if let reading = try readSnapshot() {
                if store[.claude] != .available(reading) { store.receive(reading) }
            } else if store[.claude] != .unavailable { store.missingData(provider: .claude) }
        } catch { store.fail(provider: .claude) }
    }

    private func updateClock() {
        let current = Date()
        if current < now { nextPoll = current }
        now = current
    }

    private func tick() {
        updateClock()
        updateCooldown()
        // Snapshot-file reads never become a new provider observation.
        readClaude()
        if now >= nextPoll { refresh() }
    }

    private func updateCooldown() {
        cooldownSeconds = Int(ceil(policy.remaining(at: now)))
        if (defaults.object(forKey: "codexLastAttempt") as? Date) != policy.lastAttempt {
            defaults.set(policy.lastAttempt, forKey: "codexLastAttempt")
        }
    }

    private func cancelCurrent() {
        generation &+= 1
        if let task, let taskID { retiringTasks[taskID] = task }
        task?.cancel()
        cancelProvider?()
        task = nil
        taskID = nil
        isRefreshing = false
    }

    func shutdown() async {
        stop()
        // Disconnect/reconnect may already have cancelled an older fetch. It
        // still owns children until its utility queue finishes bounded cleanup.
        let pending = Array(retiringTasks.values)
        for pendingTask in pending { await pendingTask.value }
    }

    func stop() {
        timer?.invalidate()
        timer = nil
        if let wakeObserver { NSWorkspace.shared.notificationCenter.removeObserver(wakeObserver) }
        wakeObserver = nil
        cancelCurrent()
    }
}

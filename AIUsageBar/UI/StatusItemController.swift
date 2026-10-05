import AppKit
import Combine
import SwiftUI

/// Public AppKit status-item APIs provide reliable tooltip and variable-width text.
@MainActor
final class StatusItemController: NSObject {
    private let coordinator = RefreshCoordinator()
    private var subscriptions = Set<AnyCancellable>()
    private let statusItem: NSStatusItem
    private let isExample: Bool
    private let popover = NSPopover()
    private var exampleWindow: NSWindow?
    private var usageWindow: NSWindow?
    private var selectedProviders = Set(AIProvider.allCases)
    private var exampleMenuController: NSHostingController<UsageDemoView>?
    private var examplePreviewController: NSHostingController<UsageDemoView>?

    private var exampleProviders: [AIProvider] {
        AIProvider.allCases.filter { selectedProviders.contains($0) }
    }

    init(isExample: Bool) {
        self.isExample = isExample
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        super.init()

        if let button = statusItem.button {
            button.image = nil
            button.font = .monospacedDigitSystemFont(ofSize: 12, weight: .regular)
            button.title = isExample ? MenuBarExample.title(for: exampleProviders) : "AIUsageBar"
            button.toolTip = isExample ? MenuBarExample.toolTip(for: exampleProviders) : "AIUsageBar\nยังไม่เชื่อมบริการ"
            button.setAccessibilityLabel(button.title)
            button.setAccessibilityHelp(button.toolTip)
            button.target = self
            button.action = #selector(togglePopover)
        }

        popover.behavior = .transient
        popover.animates = false
        popover.contentSize = NSSize(width: 350, height: isExample ? 480 : 640)
        if isExample {
            let hosting = NSHostingController(rootView: makeExampleView())
            exampleMenuController = hosting
            popover.contentViewController = hosting
        } else {
            popover.contentViewController = NSHostingController(rootView: makeUsageView())
            coordinator.objectWillChange.sink { [weak self] _ in
                Task { @MainActor [weak self] in self?.updateTitle() }
            }.store(in: &subscriptions)
            if ProcessInfo.processInfo.environment["XCTestConfigurationFilePath"] == nil {
                let snapshotURL = FileManager.default.homeDirectoryForCurrentUser
                    .appendingPathComponent("Library/Application Support/AIUsageBar/claude-latest.json")
                coordinator.start(snapshot: { try ClaudeSnapshotProvider(snapshotURL: snapshotURL).read() })
            }
            updateTitle()
        }
        installApplicationMenu()
    }

    private func makeUsageView() -> UsageMenuView {
        UsageMenuView(coordinator: coordinator, connect: { [weak self] in self?.chooseCodex() },
            installClaude: { [weak self] in self?.installClaude() },
            disconnectClaude: { [weak self] in self?.disconnectClaude() })
    }

    /// Optional CLI window mirrors the menu for keyboard/accessibility inspection.
    func showUsageWindow() {
        guard !isExample else { return }
        if usageWindow == nil {
            let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 350, height: 640),
                styleMask: [.titled, .closable], backing: .buffered, defer: false)
            window.title = "AIUsageBar"
            window.isReleasedWhenClosed = false
            window.contentViewController = NSHostingController(rootView: makeUsageView())
            window.center()
            usageWindow = window
        }
        NSApplication.shared.activate()
        usageWindow?.makeKeyAndOrderFront(nil)
    }

    func showPopover() {
        guard let button = statusItem.button else { return }
        NSApplication.shared.activate()
        popover.show(relativeTo: button.bounds, of: button, preferredEdge: .minY)
        popover.contentViewController?.view.window?.title = "AIUsageBar"
    }

    /// An example-only window lets the user inspect the layout while hovering the bar.
    func showExampleWindow() {
        guard isExample else { return }
        if exampleWindow == nil {
            let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 350, height: 420),
                                  styleMask: [.titled, .closable], backing: .buffered, defer: false)
            window.title = "AIUsageBar — ตัวอย่าง"
            window.isReleasedWhenClosed = false
            let hosting = NSHostingController(rootView: makeExampleView())
            examplePreviewController = hosting
            window.contentViewController = hosting
            window.center()
            exampleWindow = window
        }
        NSApplication.shared.activate()
        exampleWindow?.makeKeyAndOrderFront(nil)
    }

    @objc private func togglePopover(_ sender: Any?) {
        if popover.isShown {
            popover.performClose(sender)
        } else {
            showPopover()
        }
    }

    private func makeExampleView() -> UsageDemoView {
        UsageDemoView(providers: exampleProviders) { [weak self] provider, enabled in
            self?.setExampleProvider(provider, enabled: enabled)
        }
    }

    private func setExampleProvider(_ provider: AIProvider, enabled: Bool) {
        guard isExample else { return }
        if enabled { selectedProviders.insert(provider) }
        else { selectedProviders.remove(provider) }
        let view = makeExampleView()
        exampleMenuController?.rootView = view
        examplePreviewController?.rootView = view
        statusItem.button?.title = MenuBarExample.title(for: exampleProviders)
        statusItem.button?.toolTip = MenuBarExample.toolTip(for: exampleProviders)
        statusItem.button?.setAccessibilityLabel(statusItem.button?.title)
        statusItem.button?.setAccessibilityHelp(statusItem.button?.toolTip)
    }

    func shutdown() async { await coordinator.shutdown() }

    func invalidate() {
        coordinator.stop()
        subscriptions.removeAll()
        popover.close()
        exampleWindow?.close()
        usageWindow?.close()
        statusItem.button?.target = nil
        statusItem.button?.action = nil
        NSStatusBar.system.removeStatusItem(statusItem)
    }

    private func updateTitle() {
        guard !isExample else { return }
        let now = coordinator.now
        var remaining: [AIProvider: Double] = [:]
        var marks: [AIProvider: String] = [:]
        var details: [String] = []
        guard let policy = try? UsageFreshnessPolicy(maxAge: 300) else { return }
        for provider in AIProvider.allCases {
            let state = coordinator.store[provider]
            let display = UsagePresentation(state: state, now: now, policy: policy)
            details.append("\(provider.displayName): \(display.title)" + (display.detail.map { " · " + $0 } ?? ""))
            if let quota = display.quota {
                remaining[provider] = quota.remainingPercent
                if case .available(let reading) = state {
                    switch UsageFreshness.evaluate(reading, now: now, policy: policy) {
                    case .fresh: break
                    case .snapshot: marks[provider] = "*"
                    default: marks[provider] = "~"
                    }
                } else { marks[provider] = "~" }
            }
        }
        let title = AIProvider.allCases.map {
            MenuBarText.title(for: [$0], remaining: remaining) + (marks[$0] ?? "")
        }.joined(separator: " · ")
        let toolTip = "เหลือรายสัปดาห์ · * snapshot · ~ ข้อมูลครั้งก่อน\n" + details.joined(separator: "\n")
        if statusItem.button?.title != title { statusItem.button?.title = title }
        if statusItem.button?.toolTip != toolTip { statusItem.button?.toolTip = toolTip }
        statusItem.button?.setAccessibilityLabel(statusItem.button?.title)
        statusItem.button?.setAccessibilityHelp(statusItem.button?.toolTip)
    }

    private func chooseCodex() {
        popover.close()
        let candidates = CodexExecutableDiscovery.candidates()
        if let candidate = candidates.first {
            let alert = NSAlert()
            alert.messageText = "ใช้ Codex CLI นี้หรือไม่?"
            alert.informativeText = candidate.path + "\nแอปจะอ่าน weekly quota ผ่าน official CLI ซึ่งจัดการ login ของตัวเอง"
            alert.addButton(withTitle: "ใช้ไฟล์นี้")
            alert.addButton(withTitle: "เลือกไฟล์อื่น…")
            alert.addButton(withTitle: "ยกเลิก")
            let response = alert.runModal()
            if response == .alertFirstButtonReturn {
                coordinator.connectCodex(candidate)
                coordinator.refresh()
                return
            }
            if response != .alertSecondButtonReturn { return }
        }
        let panel = NSOpenPanel()
        panel.canChooseDirectories = false
        panel.allowsMultipleSelection = false
        panel.message = "เลือก native executable ของ Codex (ไม่ใช้ JavaScript/Node shim)"
        if panel.runModal() == .OK, let url = panel.url {
            coordinator.connectCodex(url)
            coordinator.refresh()
        }
    }

    private var bridgeDirectory: URL {
        FileManager.default.homeDirectoryForCurrentUser
            .appendingPathComponent("Library/Application Support/AIUsageBar")
    }

    private var claudeSettings: URL {
        FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent(".claude/settings.json")
    }

    private func installClaude() {
        popover.close()
        do {
            guard let executable = Bundle.main.executableURL else { return }
            let binary = try Data(contentsOf: executable)
            let preview = try ClaudeBridgeInstaller.preview(settingsURL: claudeSettings,
                directory: bridgeDirectory, wrapperBinary: binary, helperBinary: binary)
            let alert = NSAlert()
            alert.messageText = "ติดตั้ง Claude Code bridge"
            alert.informativeText = "ตั้งตัวเชื่อมใน \(preview.settingsURL.path)\n"
                + "คำสั่งเดิม: \(preview.originalCommand.isEmpty ? "ยังไม่มี — เก็บ quota อย่างเดียว" : preview.originalCommand)\nคำสั่งใหม่: \(preview.installedCommand)\n"
                + "เก็บ native wrapper/helper, backup และ quota-only snapshot ใน \(preview.directory.path)"
            alert.addButton(withTitle: "ติดตั้งพร้อม backup")
            alert.addButton(withTitle: "ยกเลิก")
            if alert.runModal() == .alertFirstButtonReturn {
                try ClaudeBridgeInstaller.apply(preview)
                coordinator.setMessage("ติดตั้งแล้ว เปิด Claude Code session ใหม่เพื่อส่ง snapshot; project override อาจไม่ส่งข้อมูล")
                coordinator.refresh()
            }
        } catch { coordinator.setMessage("ติดตั้งไม่ได้: settings ไม่รองรับ หรือพบไฟล์/การแก้ไขที่ขัดกัน") }
    }

    private func disconnectClaude() {
        do {
            try ClaudeBridgeInstaller.restore(directory: bridgeDirectory, settingsURL: claudeSettings)
            coordinator.setMessage("คืน statusline เดิมแล้ว")
            coordinator.refresh()
        } catch { coordinator.setMessage("คืน statusline ไม่ได้: พบ conflict หรือไม่มี installation ที่เป็นเจ้าของ ดู README") }
    }

    private func installApplicationMenu() {
        let menu = NSMenu()
        let applicationItem = NSMenuItem()
        let applicationMenu = NSMenu(title: "AIUsageBar")
        let quit = NSMenuItem(title: "ออกจาก AIUsageBar", action: #selector(NSApplication.terminate(_:)),
                             keyEquivalent: "q")
        quit.target = NSApplication.shared
        applicationMenu.addItem(quit)
        applicationItem.submenu = applicationMenu
        menu.addItem(applicationItem)
        NSApplication.shared.mainMenu = menu
    }
}

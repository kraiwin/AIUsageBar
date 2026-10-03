import AppKit
import Darwin

/// Menu-bar process with native bridge CLI entry points. Example data requires an explicit launch argument.
@main
@MainActor
enum AIUsageBarApp {
    static func main() {
        if let code = ClaudeBridgeCommand.run(arguments: Array(CommandLine.arguments.dropFirst())) {
            exit(code)
        }
        let application = NSApplication.shared
        let delegate = AIUsageBarDelegate()
        application.delegate = delegate
        application.setActivationPolicy(.accessory)
        withExtendedLifetime(delegate) {
            application.run()
        }
    }
}

@MainActor
private final class AIUsageBarDelegate: NSObject, NSApplicationDelegate {
    private var controller: StatusItemController?

    func applicationDidFinishLaunching(_ notification: Notification) {
        let example = ProcessInfo.processInfo.arguments.contains("--demo")
        controller = StatusItemController(isExample: example)
        if example {
            DispatchQueue.main.async { [weak self] in self?.controller?.showExampleWindow() }
        } else if ProcessInfo.processInfo.arguments.contains("--show-window") {
            DispatchQueue.main.async { [weak self] in self?.controller?.showUsageWindow() }
        } else if ProcessInfo.processInfo.arguments.contains("--show-menu") {
            DispatchQueue.main.async { [weak self] in self?.controller?.showPopover() }
        }
    }

    func applicationShouldTerminate(_ sender: NSApplication) -> NSApplication.TerminateReply {
        Task { [weak self] in
            await self?.controller?.shutdown()
            sender.reply(toApplicationShouldTerminate: true)
        }
        return .terminateLater
    }

    func applicationWillTerminate(_ notification: Notification) {
        controller?.invalidate()
    }
}

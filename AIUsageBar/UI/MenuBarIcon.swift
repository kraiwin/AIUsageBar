import AppKit

/// Original three-bar mark; template rendering follows the system menu-bar color.
@MainActor
enum MenuBarIcon {
    static func image() -> NSImage {
        let image = NSImage(size: NSSize(width: 20, height: 18), flipped: false) { _ in
            NSColor.black.setFill()
            for (x, height) in [(3.0, 6.0), (8.5, 10.0), (14.0, 14.0)] {
                NSBezierPath(roundedRect: NSRect(x: x, y: 2, width: 3, height: height),
                             xRadius: 1.5, yRadius: 1.5).fill()
            }
            return true
        }
        image.isTemplate = true
        image.accessibilityDescription = "AIUsageBar"
        return image
    }
}

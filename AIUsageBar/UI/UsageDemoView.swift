import AppKit
import SwiftUI

/// Example-only display values; never enter UsageStatusStore or a provider/cache.
enum MenuBarExample {
    static let claudeRemaining = 58
    static let codexRemaining = 72

    static func remaining(for provider: AIProvider) -> Int {
        provider == .claude ? claudeRemaining : codexRemaining
    }

    static func title(for providers: [AIProvider]) -> String {
        MenuBarText.title(for: providers, remaining: [.claude: Double(claudeRemaining), .codex: Double(codexRemaining)])
    }

    static func toolTip(for providers: [AIProvider]) -> String {
        let values = providers.map { "\($0.displayName): \(remaining(for: $0))%" }.joined(separator: "\n")
        return "ข้อมูลตัวอย่าง — ไม่ได้เชื่อมบัญชี\nโควตารายสัปดาห์ที่เหลือ\n"
            + (values.isEmpty ? "ยังไม่ได้เลือกบริการ" : values)
            + "\nตัวเลขสมมติ ไม่ใช่ยอดใช้งานจริง"
    }
}

@MainActor
struct UsageDemoView: View {
    let providers: [AIProvider]
    let onSelectionChange: (AIProvider, Bool) -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack {
                Text("AIUsageBar").font(.title3.weight(.semibold))
                Spacer()
                Text("ตัวอย่าง").font(.caption.weight(.semibold))
                    .padding(.horizontal, 8).padding(.vertical, 4)
                    .background(.orange.opacity(0.15), in: Capsule())
            }
            Text("โควตารายสัปดาห์ที่เหลือ")
                .font(.subheadline).foregroundStyle(.secondary)

            HStack {
                ForEach(AIProvider.allCases) { provider in
                    Toggle(provider.displayName, isOn: Binding(
                        get: { providers.contains(provider) },
                        set: { onSelectionChange(provider, $0) }
                    ))
                    .toggleStyle(.checkbox)
                    .accessibilityIdentifier("example-selection-\(provider.rawValue)")
                }
            }
            .font(.caption)

            ForEach(providers) { provider in
                exampleCard(provider.displayName, remaining: MenuBarExample.remaining(for: provider),
                            tint: provider == .claude ? .orange : .mint)
            }
            if providers.isEmpty {
                Text("ยังไม่ได้เลือกบริการตัวอย่าง").font(.subheadline).foregroundStyle(.secondary)
            }

            Label("ตัวเลขสมมติ · ไม่ใช่ข้อมูลบัญชีจริง", systemImage: "info.circle")
                .font(.caption).foregroundStyle(.secondary)
                .fixedSize(horizontal: false, vertical: true)
            Text("วางเมาส์ค้างบนข้อความที่ Menu Bar เพื่อดู tooltip")
                .font(.caption).foregroundStyle(.secondary)
                .fixedSize(horizontal: false, vertical: true)

            Divider()
            HStack {
                Text("ไม่ได้เชื่อม server").font(.caption).foregroundStyle(.secondary)
                Spacer()
                Button("ออกจากแอป") { NSApplication.shared.terminate(nil) }
                    .keyboardShortcut("q")
                    .accessibilityIdentifier("quitApplication")
            }
        }
        .padding(20)
        .frame(width: 350)
        .accessibilityIdentifier("exampleMenu")
    }

    private func exampleCard(_ name: String, remaining: Int, tint: Color) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack {
                Text(name).font(.headline)
                Spacer()
                Text("เหลือ \(remaining)%").font(.headline.monospacedDigit())
            }
            ProgressView(value: Double(remaining), total: 100).tint(tint)
            Text("รายสัปดาห์ · ข้อมูลตัวอย่าง").font(.caption).foregroundStyle(.secondary)
        }
        .padding(14)
        .background(.quaternary.opacity(0.45), in: RoundedRectangle(cornerRadius: 12))
        .accessibilityElement(children: .combine)
    }
}

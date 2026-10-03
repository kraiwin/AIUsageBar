import AppKit
import SwiftUI

@MainActor
struct UsageMenuView: View {
    @ObservedObject var coordinator: RefreshCoordinator
    let connect: () -> Void
    let installClaude: () -> Void
    let disconnectClaude: () -> Void

    private var store: UsageStatusStore { coordinator.store }

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            VStack(alignment: .leading, spacing: 4) {
                Text("AIUsageBar").font(.title3.weight(.semibold))
                Text("โควตาการใช้งานรายสัปดาห์")
                    .font(.subheadline).foregroundStyle(.secondary)
            }

            ForEach(AIProvider.allCases) { provider in
                ProviderStatusView(provider: provider, state: store.state(for: provider), now: coordinator.now)
            }

            VStack(alignment: .leading, spacing: 6) {
                Button(coordinator.codexPath == nil ? "เชื่อม Codex CLI…" : "เลือก Codex CLI ใหม่…", action: connect)
                if coordinator.codexPath != nil {
                    Button("ตัดการเชื่อม Codex", action: coordinator.disconnectCodex)
                }
                Button("ติดตั้ง Claude Code bridge…", action: installClaude)
                Button("คืน statusline เดิม / ตัด Claude", action: disconnectClaude)
                Text("Claude เป็น snapshot ล่าสุดจาก CLI ไม่ได้ยืนยันบัญชีปัจจุบัน")
                    .font(.caption).foregroundStyle(.secondary)
                if let message = coordinator.message {
                    Text(message).font(.caption).foregroundStyle(.orange)
                }
            }
            .fixedSize(horizontal: false, vertical: true)

            Divider()

            HStack {
                Button(action: coordinator.refresh) { Label("รีเฟรช", systemImage: "arrow.clockwise") }
                    .disabled(coordinator.isRefreshing)
                    .help("Codex จำกัดอย่างน้อย 1 นาที; Claude อ่าน snapshot ล่าสุด")
                Spacer()
                Button("ออกจากแอป") { NSApplication.shared.terminate(nil) }
                    .keyboardShortcut("q")
                    .accessibilityIdentifier("quitApplication")
            }

            if coordinator.cooldownSeconds > 0 {
                Text("Codex รีเฟรชได้อีกใน \(coordinator.cooldownSeconds) วินาที")
                    .font(.caption).foregroundStyle(.secondary)
            }

            if let version = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String {
                Text("เวอร์ชัน \(version)").font(.caption2).foregroundStyle(.tertiary)
            }
        }
        .padding(20)
        .frame(width: 350)
        .accessibilityIdentifier("usageMenu")
    }
}

@MainActor
private struct ProviderStatusView: View {
    let provider: AIProvider
    let state: UsageState
    let now: Date

    var body: some View {
        // Five minutes is the agreed refresh interval, not proof of source freshness.
        let presentation = makePresentation()
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(provider.displayName).font(.headline)
                Spacer()
                Text("รายสัปดาห์").font(.caption).foregroundStyle(.secondary)
            }
            Text(presentation.title).font(.subheadline)
                .foregroundStyle(statusColor(presentation.tone))
            if let detail = presentation.detail {
                Text(detail).font(.caption).foregroundStyle(.secondary)
            }
            if let quota = presentation.quota {
                Text("ใช้ไปแล้ว \(quota.usedPercent, specifier: "%.1f")% · เหลืออีก \(quota.remainingPercent, specifier: "%.1f")%")
                    .font(.subheadline.monospacedDigit())
                Text("รีเซ็ตเมื่อ \(UsagePresentation.format(quota.resetsAt))")
                    .font(.caption).foregroundStyle(.secondary)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(14)
        .background(.quaternary.opacity(0.45), in: RoundedRectangle(cornerRadius: 12))
        .accessibilityElement(children: .combine)
        .accessibilityIdentifier("provider-\(provider.rawValue)")
    }

    private func makePresentation() -> UsagePresentation {
        do {
            let policy = try UsageFreshnessPolicy(maxAge: 5 * 60)
            return UsagePresentation(state: state, now: now, policy: policy)
        } catch {
            // A configuration error must not turn into an apparently fresh reading.
            return .configurationUnavailable
        }
    }

    private func statusColor(_ tone: UsagePresentation.Tone) -> Color {
        switch tone {
        case .neutral: .secondary
        case .success: .green
        case .warning: .orange
        case .failure: .red
        }
    }
}

import Foundation

/// A text-only label for the selected services; missing values never become zero.
enum MenuBarText {
    static func title(for providers: [AIProvider], remaining: [AIProvider: Double]) -> String {
        let selected = AIProvider.allCases.filter { providers.contains($0) }
        guard !selected.isEmpty else { return "AIUsageBar" }
        return selected.map { provider in
            "\(provider.displayName) \(percentage(remaining[provider]))"
        }.joined(separator: " · ")
    }

    private static func percentage(_ value: Double?) -> String {
        guard let value, value.isFinite, (0...100).contains(value) else { return "—" }
        let format = value.rounded() == value ? "%.0f%%" : "%.1f%%"
        return String(format: format, locale: Locale(identifier: "en_US_POSIX"), value == 0 ? 0 : value)
    }
}

import XCTest
@testable import AIUsageBar

final class MenuBarTextTests: XCTestCase {
    func testOnlySelectedProviderAppears() {
        XCTAssertEqual(MenuBarText.title(for: [.claude], remaining: [.claude: 58, .codex: 72]), "Claude 58%")
        XCTAssertEqual(MenuBarText.title(for: [.codex], remaining: [.claude: 58, .codex: 72]), "Codex 72%")
    }

    func testTwoProvidersHaveStableOrderAndNoDuplicates() {
        XCTAssertEqual(MenuBarText.title(for: [.codex, .claude, .claude], remaining: [.claude: 58, .codex: 72]),
                       "Claude 58% · Codex 72%")
    }

    func testZeroAndMissingDataStayDistinct() {
        XCTAssertEqual(MenuBarText.title(for: [.claude, .codex], remaining: [.claude: 0]), "Claude 0% · Codex —")
    }

    func testInvalidDataDoesNotAppearAsAQuota() {
        for value in [Double.nan, .infinity, -1, 101] {
            XCTAssertEqual(MenuBarText.title(for: [.claude], remaining: [.claude: value]), "Claude —")
        }
    }

    func testEmptySelectionRetainsAnAccessibleAppLabel() {
        XCTAssertEqual(MenuBarText.title(for: [], remaining: [.claude: 58]), "AIUsageBar")
    }
}

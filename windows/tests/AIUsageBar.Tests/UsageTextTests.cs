using AIUsageBar.Core;

namespace AIUsageBar.Tests;

internal static class UsageTextTests
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private static UsageReading Reading(double used) => new(UsageProvider.Codex, new(used, DateTimeOffset.Parse("2026-10-12T02:00:00Z")), null, UsageSource.CodexAppServer, Fixture.Now);
    public static IEnumerable<TestCase> Cases()
    {
        yield return TestCase.Sync("usage-text/S3-T1-menu-timezone", () => Check.Equal("Codex: เหลือรายสัปดาห์ 72% · รีเซ็ต 12/10 09:00", UsageText.MenuLine("Codex", Reading(28), Zone)));
        yield return TestCase.Sync("usage-text/S3-T2-no-reading", () => Check.Equal("Codex: เหลือรายสัปดาห์ —", UsageText.MenuLine("Codex", null, Zone)));
        yield return TestCase.Sync("usage-text/S3-T3-round-and-boundaries", () =>
        {
            Check.Equal("Codex: เหลือรายสัปดาห์ 72% · รีเซ็ต 12/10 09:00", UsageText.MenuLine("Codex", Reading(28.5), Zone));
            Check.Equal("Codex: เหลือรายสัปดาห์ 100% · รีเซ็ต 12/10 09:00", UsageText.MenuLine("Codex", Reading(0), Zone));
            Check.Equal("Codex: เหลือรายสัปดาห์ 0% · รีเซ็ต 12/10 09:00", UsageText.MenuLine("Codex", Reading(100), Zone));
        });
        yield return TestCase.Sync("usage-text/S3-T4-tooltip-partial", () => Check.Equal("Codex 72% · Claude —", UsageText.Tooltip(Reading(28), null)));
        yield return TestCase.Sync("usage-text/S3-T5-tooltip-empty", () =>
        { var text = UsageText.Tooltip(null, null); Check.Equal("Codex — · Claude —", text); Check.True(text.Length <= 127); });
        yield return TestCase.Sync("usage-text/S3-T6-claude-age", () =>
        {
            Check.Equal("Claude: อัปเดตจาก Claude Code ล่าสุด 10:15", UsageText.ClaudeAge(DateTimeOffset.Parse("2026-10-06T03:15:00Z"), Fixture.Now, Zone));
            Check.Equal("Claude: ยังไม่มีข้อมูลจาก Claude Code", UsageText.ClaudeAge(null, Fixture.Now, Zone));
        });
    }
}

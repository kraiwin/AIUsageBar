using System.Globalization;

namespace AIUsageBar.Core;

public static class UsageText
{
    private static string Remaining(UsageReading? reading) => reading is null ? "—" :
        Math.Round(100 - reading.Weekly.UsedPercent, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture) + "%";
    public static string MenuLine(string name, UsageReading? reading, TimeZoneInfo tz) =>
        $"{name}: เหลือรายสัปดาห์ {Remaining(reading)}" + (reading is null ? "" :
            " · รีเซ็ต " + TimeZoneInfo.ConvertTime(reading.Weekly.ResetsAt, tz).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture));
    public static string Tooltip(UsageReading? codex, UsageReading? claude) => $"Codex {Remaining(codex)} · Claude {Remaining(claude)}";
    public static string ClaudeAge(DateTimeOffset? receivedAt, DateTimeOffset now, TimeZoneInfo tz) => receivedAt is null ?
        "Claude: ยังไม่มีข้อมูลจาก Claude Code" : "Claude: อัปเดตจาก Claude Code ล่าสุด " + TimeZoneInfo.ConvertTime(receivedAt.Value, tz).ToString("HH:mm", CultureInfo.InvariantCulture);
}

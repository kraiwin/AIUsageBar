using System.Text.Json;

namespace AIUsageBar.Core;

public sealed class CoreException(string category) : Exception(category)
{
    public string Category { get; } = category;
}
public enum UsageProvider { Codex, Claude }
public enum UsageSource { CodexAppServer, ClaudeStatusLine }
public sealed record QuotaWindow
{
    public double UsedPercent { get; }
    public DateTimeOffset ResetsAt { get; }
    public double RemainingPercent => 100 - UsedPercent;
    public QuotaWindow(double usedPercent, DateTimeOffset resetsAt)
    {
        if (!double.IsFinite(usedPercent) || usedPercent < 0 || usedPercent > 100 || resetsAt.ToUnixTimeSeconds() <= 0)
            throw new CoreException("invalid-quota");
        UsedPercent = usedPercent; ResetsAt = resetsAt;
    }
}
public sealed record UsageReading(UsageProvider Provider, QuotaWindow Weekly, QuotaWindow? Session,
    UsageSource Source, DateTimeOffset ReceivedAt, DateTimeOffset? ProviderObservedAt = null);
public enum UsageFreshness { Fresh, Stale, Snapshot, Expired, InvalidTimestamp }
public static class Freshness
{
    public static UsageFreshness Evaluate(UsageReading reading, DateTimeOffset now, TimeSpan maxAge)
    {
        if (maxAge < TimeSpan.Zero) throw new CoreException("invalid-freshness");
        if (reading.ReceivedAt.ToUnixTimeSeconds() <= 0 || reading.ReceivedAt > now ||
            reading.ProviderObservedAt is { } observed && (observed.ToUnixTimeSeconds() <= 0 || observed > reading.ReceivedAt || observed > now))
            return UsageFreshness.InvalidTimestamp;
        if (reading.Weekly.ResetsAt <= now) return UsageFreshness.Expired;
        return reading.ProviderObservedAt is not { } time ? UsageFreshness.Snapshot :
            now - time <= maxAge ? UsageFreshness.Fresh : UsageFreshness.Stale;
    }
}
public static class UsagePayloadParser
{
    public static UsageReading? ParseCodex(ReadOnlySpan<byte> bytes, DateTimeOffset receivedAt)
    {
        using var doc = Parse(bytes); var root = doc.RootElement;
        var buckets = Object(root, "rateLimitsByLimitId");
        var limits = buckets is { } map ? Object(map, "codex") : Object(root, "rateLimits");
        if (limits is not { } value) return null;
        if (value.TryGetProperty("limitId", out var id) && id.ValueKind != JsonValueKind.Null)
        {
            if (id.ValueKind != JsonValueKind.String) throw new CoreException("invalid-field");
            if (id.GetString() != "codex") { if (buckets is not null) throw new CoreException("invalid-field"); return null; }
        }
        QuotaWindow? week = null, session = null;
        foreach (var key in new[] { "primary", "secondary" })
        {
            if (Object(value, key) is not { } window) continue;
            var duration = Number(window, "windowDurationMins", false);
            if (duration is null) continue;
            if (duration <= 0 || Math.Truncate(duration.Value) != duration) throw new CoreException("invalid-field");
            if (duration == 10080) { if (week is not null) throw new CoreException("duplicate-weekly"); week = Quota(window, "usedPercent", "resetsAt"); }
            if (duration == 300) { if (session is not null) throw new CoreException("duplicate-session"); session = Quota(window, "usedPercent", "resetsAt"); }
        }
        return week is null ? null : new(UsageProvider.Codex, week, session, UsageSource.CodexAppServer, receivedAt);
    }
    public static UsageReading? ParseClaude(ReadOnlySpan<byte> bytes, DateTimeOffset receivedAt)
    {
        using var doc = Parse(bytes);
        if (Object(doc.RootElement, "rate_limits") is not { } limits || Object(limits, "seven_day") is not { } week) return null;
        var session = Object(limits, "five_hour");
        return new(UsageProvider.Claude, Quota(week, "used_percentage", "resets_at"),
            session is { } window ? Quota(window, "used_percentage", "resets_at") : null, UsageSource.ClaudeStatusLine, receivedAt);
    }
    internal static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        try
        {
            var doc = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 64 });
            try { ValidateObject(doc.RootElement); return doc; }
            catch { doc.Dispose(); throw; }
        }
        catch (JsonException) { throw new CoreException("invalid-json"); }
    }
    private static void ValidateObject(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new CoreException("invalid-json");
        ValidateDuplicates(value);
    }
    private static void ValidateDuplicates(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            { if (!keys.Add(property.Name)) throw new CoreException("duplicate-field"); ValidateDuplicates(property.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (var child in value.EnumerateArray()) ValidateDuplicates(child);
    }
    internal static JsonElement? Object(JsonElement value, string name)
    {
        if (!value.TryGetProperty(name, out var field) || field.ValueKind == JsonValueKind.Null) return null;
        if (field.ValueKind != JsonValueKind.Object) throw new CoreException("invalid-field"); return field;
    }
    internal static double? Number(JsonElement value, string name, bool required)
    {
        if (!value.TryGetProperty(name, out var field) || field.ValueKind == JsonValueKind.Null)
        { if (required) throw new CoreException("missing-field"); return null; }
        if (field.ValueKind != JsonValueKind.Number || !field.TryGetDouble(out var number) || !double.IsFinite(number)) throw new CoreException("invalid-field");
        return number;
    }
    internal static QuotaWindow Quota(JsonElement value, string percent, string reset)
    {
        var seconds = Number(value, reset, true)!.Value;
        if (seconds <= 0 || seconds > 253402300799) throw new CoreException("invalid-reset");
        return new(Number(value, percent, true)!.Value, DateTimeOffset.UnixEpoch.AddSeconds(seconds));
    }
}

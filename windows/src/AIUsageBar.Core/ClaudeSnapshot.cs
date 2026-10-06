using System.Text.Json;

namespace AIUsageBar.Core;

public sealed record SnapshotResult(UsageReading? Reading, bool CaptureError, DateTimeOffset? ReceivedAt);
public sealed class ClaudeSnapshotStore : IDisposable
{
    public const string SnapshotName = "claude-latest.json", ErrorName = "claude-error.json";
    private readonly PrivateFiles files;
    public ClaudeSnapshotStore(string scratchRoot) => files = new(scratchRoot);
    public void Capture(ReadOnlySpan<byte> input, DateTimeOffset receipt)
    {
        using var held = files.AcquireLock();
        UsageReading? reading;
        try
        {
            if (input.Length > 2 * 1024 * 1024) throw new CoreException("capture-limit");
            if (receipt.ToUnixTimeSeconds() <= 0 || receipt > DateTimeOffset.UtcNow) throw new CoreException("invalid-timestamp");
            reading = UsagePayloadParser.ParseClaude(input, receipt);
        }
        catch (CoreException)
        { files.WriteLocked(ErrorName, "{\"schemaVersion\":1,\"state\":\"capture-error\"}"u8); return; }
        var current = ReadSnapshot(DateTimeOffset.UtcNow);
        if (current.ReceivedAt is { } previous && previous > receipt) return;
        object? Window(QuotaWindow? value) => value is null ? null : new { usedPercent = value.UsedPercent, resetsAt = (value.ResetsAt - DateTimeOffset.UnixEpoch).TotalSeconds };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { schemaVersion = 1, receivedAt = (receipt - DateTimeOffset.UnixEpoch).TotalSeconds,
            state = reading is null ? "no-data" : "quota", weekly = Window(reading?.Weekly), session = Window(reading?.Session) });
        files.WriteLocked(SnapshotName, bytes); files.DeleteLocked(ErrorName);
    }
    private SnapshotResult ReadSnapshot(DateTimeOffset now)
    {
        byte[] bytes; try { bytes = files.Read(SnapshotName); } catch (FileNotFoundException) { return new(null, false, null); }
        using var doc = UsagePayloadParser.Parse(bytes); var root = doc.RootElement;
        if (root.EnumerateObject().Any(x => x.Name is not ("schemaVersion" or "receivedAt" or "state" or "weekly" or "session"))) throw new CoreException("invalid-snapshot");
        if (UsagePayloadParser.Number(root, "schemaVersion", true) != 1) throw new CoreException("invalid-snapshot");
        var seconds = UsagePayloadParser.Number(root, "receivedAt", true)!.Value;
        if (seconds <= 0 || seconds > 253402300799) throw new CoreException("invalid-snapshot");
        var receipt = DateTimeOffset.UnixEpoch.AddSeconds(seconds); if (receipt > now) throw new CoreException("invalid-snapshot");
        var state = root.GetProperty("state"); if (state.ValueKind != JsonValueKind.String) throw new CoreException("invalid-snapshot");
        var weekly = UsagePayloadParser.Object(root, "weekly"); var session = UsagePayloadParser.Object(root, "session");
        if (state.GetString() == "no-data" && weekly is null && session is null) return new(null, false, receipt);
        if (state.GetString() != "quota" || weekly is null) throw new CoreException("invalid-snapshot");
        return new(new(UsageProvider.Claude, UsagePayloadParser.Quota(weekly.Value, "usedPercent", "resetsAt"),
            session is { } window ? UsagePayloadParser.Quota(window, "usedPercent", "resetsAt") : null, UsageSource.ClaudeStatusLine, receipt), false, receipt);
    }
    public SnapshotResult Read(DateTimeOffset now)
    {
        var snapshot = ReadSnapshot(now); byte[] bytes;
        try { bytes = files.Read(ErrorName); } catch (FileNotFoundException) { return snapshot; }
        using var doc = UsagePayloadParser.Parse(bytes); var root = doc.RootElement;
        if (root.EnumerateObject().Count() != 2 || UsagePayloadParser.Number(root, "schemaVersion", true) != 1 ||
            !root.TryGetProperty("state", out var state) || state.ValueKind != JsonValueKind.String || state.GetString() != "capture-error") throw new CoreException("invalid-marker");
        return snapshot with { CaptureError = true };
    }
    public void Dispose() => files.Dispose();
}

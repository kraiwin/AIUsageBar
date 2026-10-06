using System.Text.Json;

namespace AIUsageBar.Core;

/// <summary>Nonsecret selected tuple and attempts only. No proof import or connection activation exists in W1.</summary>
public sealed record LocalStateData(ExecutableTuple? Selected, DateTimeOffset? LastAttempt);
public sealed class LocalState : IDisposable
{
    private readonly PrivateFiles files;
    public LocalState(string scratchRoot) => files = new(scratchRoot);
    public LocalStateData Read()
    {
        byte[] bytes; try { bytes = files.Read("local-state.json", 16384); } catch (FileNotFoundException) { return new(null, null); }
        using var doc = UsagePayloadParser.Parse(bytes); var root = doc.RootElement;
        if (root.EnumerateObject().Any(x => x.Name is not ("schemaVersion" or "selected" or "lastAttempt")) || UsagePayloadParser.Number(root, "schemaVersion", true) != 1) throw new CoreException("invalid-state");
        var selected = UsagePayloadParser.Object(root, "selected"); ExecutableTuple? tuple = null;
        if (selected is { } value)
        {
            try { tuple = value.Deserialize<ExecutableTuple>(); } catch (JsonException) { throw new CoreException("invalid-state"); }
            ValidateTuple(tuple);
        }
        var time = UsagePayloadParser.Number(root, "lastAttempt", false);
        if (time is <= 0 or > 253402300799) throw new CoreException("invalid-state");
        return new(tuple, time is { } seconds ? DateTimeOffset.UnixEpoch.AddSeconds(seconds) : null);
    }
    public void Write(LocalStateData state)
    {
        if (state.Selected is not null) ValidateTuple(state.Selected);
        if (state.LastAttempt is { } time && time.ToUnixTimeSeconds() <= 0) throw new CoreException("invalid-state");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { schemaVersion = 1, selected = state.Selected,
            lastAttempt = state.LastAttempt is { } last ? (double?)(last - DateTimeOffset.UnixEpoch).TotalSeconds : null });
        if (bytes.Length > 16384) throw new CoreException("state-limit");
        files.AtomicWrite("local-state.json", bytes);
    }
    private static void ValidateTuple(ExecutableTuple? tuple)
    {
        if (tuple is null || tuple.Path is null || !Path.IsPathFullyQualified(tuple.Path) || tuple.MetadataVersion is null || tuple.MetadataVersion.Length is 0 or > 128 ||
            tuple.FileId is null || tuple.FileId.Length is 0 or > 128 || tuple.Size <= 0 || tuple.Sha256 is null || tuple.Sha256.Length != 64 || tuple.Sha256.Any(c => !Uri.IsHexDigit(c))) throw new CoreException("invalid-state");
    }
    public void Dispose() => files.Dispose();
}

using System.Text;
using System.Text.Json;

namespace AIUsageBar.Core;

public enum McpTransportKind { Stdio, Http }
public sealed record McpServer(string Name, McpTransportKind Kind, bool Disabled);
public sealed record CodexFeature(string Name, bool Enabled, string Stage);
public static class CodexPolicy
{
    public static IReadOnlyList<string> Arguments(string inertPath, IReadOnlyList<McpServer>? servers = null)
    {
        if (!Path.IsPathFullyQualified(inertPath)) throw new CoreException("invalid-inert-path");
        var overrides = new List<string> { "features.hooks=false", "features.plugins=false", "features.code_mode_host=false", "features.remote_control=false", "notify=[]", "analytics.enabled=false", "otel.exporter=\"none\"", "otel.trace_exporter=\"none\"" };
        if (servers is not null)
        {
            if (servers.Count > 256 || servers.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != servers.Count) throw new CoreException("unsupported-configuration");
            var entries = servers.OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => TomlString(x.Name) + "={enabled=false," +
                (x.Kind == McpTransportKind.Stdio ? "command=" + TomlString(inertPath) : "url=\"https://example.invalid/\"") + "}");
            overrides.Add("mcp_servers={" + string.Join(',', entries) + "}");
        }
        return overrides.SelectMany(x => new[] { "-c", x }).Concat(["app-server", "--listen", "stdio://", "--strict-config"]).ToArray();
    }
    public static string TomlString(string text)
    {
        if (text.Length == 0 || text.Any(c => c < 32 || c == 127) || !ValidUnicode(text)) throw new CoreException("unsupported-configuration");
        return "\"" + text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
    private static bool ValidUnicode(string text)
    { try { _ = new UTF8Encoding(false, true).GetByteCount(text); return true; } catch (EncoderFallbackException) { return false; } }
    public static (IReadOnlyList<CodexFeature> Features, string? Cursor) RegistryPage(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array || data.GetArrayLength() > 100) throw new CoreException("unsupported-configuration");
        var features = new List<CodexFeature>();
        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String ||
                string.IsNullOrEmpty(name.GetString()) || Encoding.UTF8.GetByteCount(name.GetString()!) > 256 ||
                !item.TryGetProperty("stage", out var stage) || stage.ValueKind != JsonValueKind.String || !item.TryGetProperty("enabled", out var enabled) || enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new CoreException("unsupported-configuration");
            features.Add(new(name.GetString()!, enabled.GetBoolean(), stage.GetString()!));
        }
        string? cursor = null;
        if (root.TryGetProperty("nextCursor", out var next) && next.ValueKind != JsonValueKind.Null)
        { if (next.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(next.GetString()) || Encoding.UTF8.GetByteCount(next.GetString()!) > 4096) throw new CoreException("unsupported-configuration"); cursor = next.GetString(); }
        return (features, cursor);
    }
    public static void AssertRegistry(IReadOnlyList<CodexFeature> features)
    {
        if (features.Count > 2048 || features.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != features.Count) throw new CoreException("unsupported-configuration");
        foreach (var name in new[] { "hooks", "plugins", "code_mode_host" })
            if (!features.Any(x => x.Name == name && !x.Enabled && x.Stage is "stable" or "beta" or "underDevelopment")) throw new CoreException("unsupported-configuration");
    }
    public static IReadOnlyList<McpServer> Inventory(JsonElement result, bool requireDisabled)
    {
        try
        {
            var config = result.GetProperty("config");
            if (config.GetProperty("notify").ValueKind != JsonValueKind.Array || config.GetProperty("notify").GetArrayLength() != 0 || config.GetProperty("analytics").GetProperty("enabled").ValueKind != JsonValueKind.False ||
                config.GetProperty("otel").GetProperty("exporter").GetString() != "none" || config.GetProperty("otel").GetProperty("trace_exporter").GetString() != "none") throw new CoreException("unsupported-configuration");
            var servers = config.GetProperty("mcp_servers");
            if (servers.ValueKind != JsonValueKind.Object || servers.EnumerateObject().Count() > 256) throw new CoreException("unsupported-configuration");
            var resultServers = new List<McpServer>();
            foreach (var property in servers.EnumerateObject())
            {
                if (Encoding.UTF8.GetByteCount(property.Name) > 256) throw new CoreException("unsupported-configuration"); _ = TomlString(property.Name);
                var details = property.Value;
                string? Field(string key)
                { if (!details.TryGetProperty(key, out var field) || field.ValueKind == JsonValueKind.Null) return null; if (field.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(field.GetString())) throw new CoreException("unsupported-configuration"); return field.GetString(); }
                var command = Field("command"); var url = Field("url");
                if ((command is null) == (url is null)) throw new CoreException("unsupported-configuration");
                bool disabled = false;
                if (details.TryGetProperty("enabled", out var enabled) && enabled.ValueKind != JsonValueKind.Null)
                { if (enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new CoreException("unsupported-configuration"); disabled = !enabled.GetBoolean(); }
                if (requireDisabled && !disabled) throw new CoreException("unsupported-configuration");
                resultServers.Add(new(property.Name, command is null ? McpTransportKind.Http : McpTransportKind.Stdio, disabled));
            }
            return resultServers.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException) { throw new CoreException("unsupported-configuration"); }
    }
    public static void AssertSameInventory(IReadOnlyList<McpServer> expected, IReadOnlyList<McpServer> actual)
    {
        if (expected.Count != actual.Count || !expected.Zip(actual).All(x => x.First.Name == x.Second.Name && x.First.Kind == x.Second.Kind && x.Second.Disabled)) throw new CoreException("unsupported-configuration");
    }
}

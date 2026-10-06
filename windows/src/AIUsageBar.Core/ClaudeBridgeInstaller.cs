using System.Text;
using System.Text.Json.Nodes;

namespace AIUsageBar.Core;

public enum BridgeState { NotInstalled, Installed, Partial }
public enum RestoreResult { NothingToDo, Restored, AlreadyDetached }

public sealed class ClaudeBridgeInstaller
{
    private const string BackupName = "claude-bridge.json";
    private readonly Action? beforeSettingsReplace;
    public ClaudeBridgeInstaller() { }
    internal ClaudeBridgeInstaller(Action beforeSettingsReplace) => this.beforeSettingsReplace = beforeSettingsReplace;
    public static string SettingsPath() => Path.Combine(Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR") is { Length: > 0 } config
        ? config : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude"), "settings.json");
    private static JsonObject ReadSettings(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new CoreException("invalid-path");
        if (!File.Exists(path)) return new();
        try
        {
            using var stream = File.OpenRead(path);
            if (stream.Length > 1024 * 1024) throw new CoreException("settings-limit");
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
            return JsonNode.Parse(reader.ReadToEnd()) as JsonObject ?? throw new CoreException("invalid-settings");
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or DecoderFallbackException) { throw new CoreException("invalid-settings"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { throw new CoreException("settings-read-failure"); }
    }
    private static string? Command(JsonNode? node) => node is JsonObject obj && obj["command"] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
    private static string InstalledCommand(string root)
    {
        if (!Path.IsPathFullyQualified(root)) throw new CoreException("invalid-path");
        return "\"" + Path.Combine(root, "bridge", "AIUsageBar.Bridge.exe").Replace('\\', '/') + "\" statusline";
    }
    public (string? OriginalCommand, string InstalledCommand) Preview(string settingsPath, string root, string bridgeSourceDir)
        => (Command(ReadSettings(settingsPath)["statusLine"]), InstalledCommand(root));
    private static JsonObject? Backup(string root)
    {
        AppPaths.EnsureRoot(root);
        using var files = new PrivateFiles(root);
        byte[] bytes; try { bytes = files.Read(BackupName, 2 * 1024 * 1024); } catch (FileNotFoundException) { return null; }
        try
        {
            var backup = JsonNode.Parse(bytes) as JsonObject;
            if (backup is null || backup["schemaVersion"]?.GetValue<int>() != 1 || backup["hadStatusLine"] is not JsonValue had || !had.TryGetValue<bool>(out _) ||
                backup["installedCommand"] is not JsonValue command || !command.TryGetValue<string>(out var text) || string.IsNullOrEmpty(text)) throw new CoreException("invalid-bridge-backup");
            return backup;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException or FormatException) { throw new CoreException("invalid-bridge-backup"); }
    }
    public BridgeState State(string settingsPath, string root)
    {
        var backup = Backup(root);
        return backup is null ? BridgeState.NotInstalled : Command(ReadSettings(settingsPath)["statusLine"]) == backup["installedCommand"]!.GetValue<string>() ? BridgeState.Installed : BridgeState.Partial;
    }
    public static string? OriginalCommand(string root) => Command(Backup(root)?["original"]);
    public void Install(string settingsPath, string root, string bridgeSourceDir)
    {
        var settings = ReadSettings(settingsPath); var command = InstalledCommand(root);
        if (Command(settings["statusLine"]) == command) return;
        AppPaths.EnsureRoot(root);
        try
        {
            using var files = new PrivateFiles(root);
            if (Backup(root) is not null) { using var held = files.AcquireLock(); files.DeleteLocked(BackupName); }
            using (var bridgeFiles = new PrivateFiles(Path.Combine(root, "bridge")))
            {
                foreach (var source in Directory.EnumerateFiles(bridgeSourceDir, "AIUsageBar.*"))
                    File.Copy(source, Path.Combine(bridgeFiles.Root, Path.GetFileName(source)), overwrite: true);
            }
            var had = settings.ContainsKey("statusLine"); var original = settings["statusLine"]?.DeepClone();
            var backup = new JsonObject { ["schemaVersion"] = 1, ["hadStatusLine"] = had, ["original"] = original, ["installedCommand"] = command };
            using (var held = files.AcquireLock()) files.WriteLocked(BackupName, Encoding.UTF8.GetBytes(backup.ToJsonString()));
            var status = settings["statusLine"] is JsonObject old ? (JsonObject)old.DeepClone() : new JsonObject();
            status["type"] = "command"; status["command"] = command; settings["statusLine"] = status;
            WriteSettings(settingsPath, settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { throw new CoreException("bridge-install-failure"); }
    }
    public RestoreResult Restore(string settingsPath, string root)
    {
        var backup = Backup(root); if (backup is null) return RestoreResult.NothingToDo;
        try
        {
            var settings = ReadSettings(settingsPath); var attached = Command(settings["statusLine"]) == backup["installedCommand"]!.GetValue<string>();
            if (attached)
            {
                if (backup["hadStatusLine"]!.GetValue<bool>()) settings["statusLine"] = backup["original"]?.DeepClone();
                else settings.Remove("statusLine");
                WriteSettings(settingsPath, settings);
            }
            AppPaths.EnsureRoot(root); using var files = new PrivateFiles(root); using var held = files.AcquireLock(); files.DeleteLocked(BackupName);
            return attached ? RestoreResult.Restored : RestoreResult.AlreadyDetached;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { throw new CoreException("bridge-restore-failure"); }
    }
    private void WriteSettings(string path, JsonObject settings)
    {
        var directory = Path.GetDirectoryName(path)!; Directory.CreateDirectory(directory);
        var temp = Path.Combine(directory, ".aiusagebar-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temp, settings.ToJsonString(), new UTF8Encoding(false)); beforeSettingsReplace?.Invoke();
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

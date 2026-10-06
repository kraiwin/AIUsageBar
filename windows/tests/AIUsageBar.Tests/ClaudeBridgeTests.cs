using System.Diagnostics;
using System.Text.Json.Nodes;
using AIUsageBar.Core;

namespace AIUsageBar.Tests;

internal static class ClaudeBridgeTests
{
    private const string Original = "{\"model\":\"x\",\"hooks\":{\"a\":1},\"statusLine\":{\"type\":\"command\",\"command\":\"bash ~/s.sh\",\"padding\":2}}";
    private static string Source => Path.GetDirectoryName(Fixture.Bridge)!;
    private static (string Settings, string Root) Paths(Scratch scratch) => (Path.Combine(scratch.Root, "settings.json"), Path.Combine(scratch.Root, "app"));
    private static JsonObject Json(string path) => (JsonObject)JsonNode.Parse(File.ReadAllBytes(path))!;
    private static void EqualJson(string expected, string path) => Check.True(JsonNode.DeepEquals(JsonNode.Parse(expected), Json(path)));
    public static IEnumerable<TestCase> Cases()
    {
        yield return TestCase.Sync("claude-bridge/S2-T1-install-preserves-keys", () =>
        {
            using var scratch = new Scratch(); var (settings, root) = Paths(scratch); File.WriteAllText(settings, Original);
            var installer = new ClaudeBridgeInstaller(); var preview = installer.Preview(settings, root, Source);
            Check.Equal("bash ~/s.sh", preview.OriginalCommand); Check.True(!Directory.Exists(root));
            installer.Install(settings, root, Source); var current = Json(settings); var old = JsonNode.Parse(Original)!;
            Check.True(JsonNode.DeepEquals(old["model"], current["model"])); Check.True(JsonNode.DeepEquals(old["hooks"], current["hooks"]));
            Check.True(JsonNode.DeepEquals(new JsonObject { ["type"] = "command", ["command"] = preview.InstalledCommand, ["padding"] = 2 }, current["statusLine"]));
            var backup = Json(Path.Combine(root, "claude-bridge.json")); Check.True(backup["hadStatusLine"]!.GetValue<bool>());
            Check.True(JsonNode.DeepEquals(old["statusLine"], backup["original"])); Check.Equal(BridgeState.Installed, installer.State(settings, root));
        });
        yield return TestCase.Sync("claude-bridge/S2-T2-exact-semantic-restore", () =>
        {
            using var scratch = new Scratch(); var (settings, root) = Paths(scratch); File.WriteAllText(settings, Original); var installer = new ClaudeBridgeInstaller();
            installer.Install(settings, root, Source); Check.Equal(RestoreResult.Restored, installer.Restore(settings, root)); EqualJson(Original, settings); Check.True(!File.Exists(Path.Combine(root, "claude-bridge.json")));
        });
        yield return TestCase.Sync("claude-bridge/S2-T3-absent-statusline", () =>
        {
            using var scratch = new Scratch(); var (settings, root) = Paths(scratch); const string original = "{\"model\":\"x\",\"hooks\":{\"a\":1}}"; File.WriteAllText(settings, original);
            var installer = new ClaudeBridgeInstaller(); installer.Install(settings, root, Source); installer.Restore(settings, root); EqualJson(original, settings); Check.True(!Json(settings).ContainsKey("statusLine"));
        });
        yield return TestCase.Sync("claude-bridge/S2-T4-idempotent-install-bytes", () =>
        {
            using var scratch = new Scratch(); var (settings, root) = Paths(scratch); File.WriteAllText(settings, Original); var installer = new ClaudeBridgeInstaller(); installer.Install(settings, root, Source);
            var settingsBytes = File.ReadAllBytes(settings); var backupBytes = File.ReadAllBytes(Path.Combine(root, "claude-bridge.json")); installer.Install(settings, root, Source);
            Check.Bytes(settingsBytes, File.ReadAllBytes(settings)); Check.Bytes(backupBytes, File.ReadAllBytes(Path.Combine(root, "claude-bridge.json")));
        });
        yield return TestCase.Sync("claude-bridge/S2-T5-user-edit-detaches", () =>
        {
            using var scratch = new Scratch(); var (settings, root) = Paths(scratch); File.WriteAllText(settings, Original); var installer = new ClaudeBridgeInstaller(); installer.Install(settings, root, Source);
            var edited = Json(settings); edited["statusLine"]!["command"] = "other"; File.WriteAllText(settings, edited.ToJsonString()); var bytes = File.ReadAllBytes(settings);
            Check.Equal(BridgeState.Partial, installer.State(settings, root)); Check.Equal(RestoreResult.AlreadyDetached, installer.Restore(settings, root)); Check.Bytes(bytes, File.ReadAllBytes(settings)); Check.True(!File.Exists(Path.Combine(root, "claude-bridge.json")));
        });
        yield return TestCase.Sync("claude-bridge/S2-T6-invalid-settings-unchanged", () =>
        {
            using var scratch = new Scratch(); var (settings, root) = Paths(scratch); File.WriteAllText(settings, "{bad"); var bytes = File.ReadAllBytes(settings);
            Check.Throws<CoreException>(() => new ClaudeBridgeInstaller().Install(settings, root, Source)); Check.Bytes(bytes, File.ReadAllBytes(settings)); Check.True(!File.Exists(Path.Combine(root, "claude-bridge.json")));
        });
        yield return TestCase.Sync("claude-bridge/S2-T7-missing-settings", () =>
        {
            using var scratch = new Scratch(); var (settings, root) = Paths(scratch); var installer = new ClaudeBridgeInstaller(); installer.Install(settings, root, Source);
            Check.Equal(1, Json(settings).Count); Check.True(!Json(Path.Combine(root, "claude-bridge.json"))["hadStatusLine"]!.GetValue<bool>());
            installer.Restore(settings, root); EqualJson("{}", settings);
        });
        yield return new("claude-bridge/S2-T8-installed-executable-tee", async () =>
        {
            using var scratch = new Scratch(); var (settings, root) = Paths(scratch); File.WriteAllText(settings, "{\"statusLine\":{\"command\":\"cat\"}}");
            var installer = new ClaudeBridgeInstaller(); installer.Install(settings, root, Source);
            var command = installer.Preview(settings, root, Source).InstalledCommand; var exe = command.Split('"')[1].Replace('/', '\\');
            var bytes = Fixture.Claude("42"); var result = await Run(exe, root, bytes); Check.Equal(0, result.Exit); Check.Bytes(bytes, result.Output);
            using var store = new ClaudeSnapshotStore(Path.Combine(root, "claude")); Check.Equal(42d, store.Read(DateTimeOffset.UtcNow).Reading!.Weekly.UsedPercent);
        });
        yield return new("claude-bridge/S2-T9-no-backup-capture", async () =>
        {
            using var scratch = new Scratch(); var (_, root) = Paths(scratch); var result = await Run(Fixture.Bridge, root, Fixture.Claude("42"));
            Check.Equal(0, result.Exit); Check.Equal(0, result.Output.Length); using var store = new ClaudeSnapshotStore(Path.Combine(root, "claude")); Check.Equal(42d, store.Read(DateTimeOffset.UtcNow).Reading!.Weekly.UsedPercent);
        });
        yield return new("claude-bridge/S2-T10-timeout-kills-sleep", async () =>
        {
            using var scratch = new Scratch(); var root = Prepare(scratch, "sleep 30"); var before = SleepPids(); var watch = Stopwatch.StartNew();
            var result = await Run(Fixture.Bridge, root, Fixture.Claude()); Check.Equal(1, result.Exit); Check.True(watch.Elapsed < TimeSpan.FromSeconds(7)); Check.True(!SleepPids().Except(before).Any());
        });
        yield return new("claude-bridge/S2-T11-invalid-capture-original-continues", async () =>
        {
            using var scratch = new Scratch(); var root = Prepare(scratch, "echo ok"); var result = await Run(Fixture.Bridge, root, "{bad"u8.ToArray());
            Check.Equal(0, result.Exit); Check.Bytes("ok\n"u8, result.Output);
        });
        yield return TestCase.Sync("claude-bridge/S2-T12-failed-write-retry", () =>
        {
            using var scratch = new Scratch(); var (settings, root) = Paths(scratch); File.WriteAllText(settings, Original); var bytes = File.ReadAllBytes(settings);
            var broken = new ClaudeBridgeInstaller(() => throw new CoreException("test-settings-failure")); Check.Throws<CoreException>(() => broken.Install(settings, root, Source));
            Check.Bytes(bytes, File.ReadAllBytes(settings)); Check.Equal(BridgeState.Partial, broken.State(settings, root));
            var installer = new ClaudeBridgeInstaller(); installer.Install(settings, root, Source); Check.Equal(BridgeState.Installed, installer.State(settings, root)); installer.Restore(settings, root); EqualJson(Original, settings);
        });
        yield return TestCase.Sync("claude-bridge/S2-T13-partial-restore-preserves-bytes", () =>
        {
            using var scratch = new Scratch(); var (settings, root) = Paths(scratch); File.WriteAllText(settings, Original);
            var broken = new ClaudeBridgeInstaller(() => throw new CoreException("test-settings-failure")); Check.Throws<CoreException>(() => broken.Install(settings, root, Source)); var bytes = File.ReadAllBytes(settings);
            Check.Equal(RestoreResult.AlreadyDetached, broken.Restore(settings, root)); Check.Bytes(bytes, File.ReadAllBytes(settings)); Check.True(!File.Exists(Path.Combine(root, "claude-bridge.json"))); Check.Equal(BridgeState.NotInstalled, broken.State(settings, root));
        });
        yield return new("claude-bridge/S2-T14-overflow-skips-original", async () =>
        {
            using var scratch = new Scratch(); var root = Prepare(scratch, "echo ok"); var result = await Run(Fixture.Bridge, root, new byte[3 * 1024 * 1024]);
            Check.Equal(0, result.Exit); Check.Equal(0, result.Output.Length); using var store = new ClaudeSnapshotStore(Path.Combine(root, "claude")); Check.True(store.Read(DateTimeOffset.UtcNow).CaptureError);
        });
        yield return new("claude-bridge/S2-T15-open-stdin-deadline", async () =>
        {
            using var scratch = new Scratch(); var root = Prepare(scratch, "echo ok"); var watch = Stopwatch.StartNew(); var result = await Run(Fixture.Bridge, root, Fixture.Claude(), holdInput: true);
            Check.Equal(0, result.Exit); Check.Equal(0, result.Output.Length); Check.True(watch.Elapsed < TimeSpan.FromSeconds(4));
        });
        // S2-T16: all existing ClaudeTests, including --capture-test cases, remain in --offline.
        yield return new("claude-bridge/S2-T17-original-inherits-working-directory", async () =>
        {
            using var scratch = new Scratch(); var root = Prepare(scratch, "pwd -W");
            var workingDirectory = Directory.CreateDirectory(Path.Combine(scratch.Root, "X")).FullName;
            var result = await Run(Fixture.Bridge, root, Fixture.Claude(), workingDirectory: workingDirectory);
            Check.Equal(0, result.Exit);
            var actual = System.Text.Encoding.UTF8.GetString(result.Output).TrimEnd('\r', '\n');
            Check.True(string.Equals(Path.GetFullPath(workingDirectory).Replace('/', '\\'),
                Path.GetFullPath(actual.Replace('/', '\\')), StringComparison.OrdinalIgnoreCase));
        });
    }
    private static int[] SleepPids() => Process.GetProcessesByName("sleep").Select(p => { using (p) return p.Id; }).ToArray();
    private static string Prepare(Scratch scratch, string command)
    {
        var (settings, root) = Paths(scratch); File.WriteAllText(settings, new JsonObject { ["statusLine"] = new JsonObject { ["type"] = "command", ["command"] = command } }.ToJsonString());
        new ClaudeBridgeInstaller().Install(settings, root, Source); return root;
    }
    private static async Task<(int Exit, byte[] Output)> Run(string exe, string root, byte[] bytes, bool holdInput = false, string? workingDirectory = null)
    {
        var info = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        if (workingDirectory is not null) info.WorkingDirectory = workingDirectory;
        foreach (var argument in new[] { "statusline", "--root", root }) info.ArgumentList.Add(argument);
        using var child = Process.Start(info)!; using var output = new MemoryStream();
        var drain = child.StandardOutput.BaseStream.CopyToAsync(output); var errors = child.StandardError.BaseStream.CopyToAsync(Stream.Null);
        var write = Task.Run(async () =>
        {
            try { await child.StandardInput.BaseStream.WriteAsync(bytes); await child.StandardInput.BaseStream.FlushAsync(); }
            catch (IOException) { /* Overflow terminates without consuming the remainder. */ }
            finally { if (!holdInput) child.StandardInput.Close(); }
        });
        try { await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(8)); await Task.WhenAll(drain, errors, write); return (child.ExitCode, output.ToArray()); }
        finally { if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); } }
    }
}

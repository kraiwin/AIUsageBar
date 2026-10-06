using System.Diagnostics;
using AIUsageBar.Core;

namespace AIUsageBar.Tests;

internal static class NativeCodexTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return TestCase.Sync("native-codex/S1-T1-launch-fields", () =>
        {
            var launch = NativeCodex.Launch(@"C:\x\codex.exe", ["a", "b"], @"C:\w");
            Check.Equal(@"C:\x\codex.exe", launch.Executable); Check.True(launch.Arguments.SequenceEqual(new[] { "a", "b" })); Check.Equal(@"C:\w", launch.WorkingDirectory);
        });
        yield return TestCase.Sync("native-codex/S1-T2-environment", () =>
        {
            var old = Environment.GetEnvironmentVariable("AIUSAGEBAR_TEST_MARKER");
            try
            {
                Environment.SetEnvironmentVariable("AIUSAGEBAR_TEST_MARKER", "1");
                var launch = NativeCodex.Launch(@"C:\x\codex.exe", [], @"C:\w");
                Check.Equal("1", launch.Environment["AIUSAGEBAR_TEST_MARKER"]); Check.Equal(Environment.GetEnvironmentVariable("USERPROFILE"), launch.Environment["USERPROFILE"]);
            }
            finally { Environment.SetEnvironmentVariable("AIUSAGEBAR_TEST_MARKER", old); }
        });
        yield return TestCase.Sync("native-codex/S1-T3-relative-path", () => Check.Throws<CoreException>(() => NativeCodex.Launch("codex.exe", [], @"C:\w")));
        yield return TestCase.Sync("native-codex/S1-T4-empty-discovery", () =>
        { using var scratch = new Scratch(); Check.Equal(0, CodexDiscovery.Discover(scratch.Root).Count); Check.Equal<string?>(null, NativeCodex.FindExecutable(scratch.Root)); });
        // S1-T5: all existing CodexTests remain registered, unchanged apart from the provider rename.
        yield return new("native-codex/S1-T6-descendants-allowed", async () =>
        {
            using var scratch = new Scratch(); await using var child = Start(scratch, "ping -n 1 127.0.0.1 >nul", true);
            child.CloseInput(); using var timeout = new CancellationTokenSource(5000);
            var exit = await child.WaitForExitAsync(timeout.Token);
            // The primary process signal can precede the job accounting update.
            while (child.ActiveProcesses != 0 && !timeout.IsCancellationRequested) await Task.Delay(10, timeout.Token);
            Console.WriteLine($"EVIDENCE S1-T6 exit={exit} total={child.TotalProcesses} active={child.ActiveProcesses}");
            Check.Equal(0, exit); Check.True(child.TotalProcesses >= 2); Check.Equal(0u, child.ActiveProcesses);
        });
        yield return new("native-codex/S1-T7-descendants-blocked", async () =>
        {
            using var scratch = new Scratch(); await using var child = Start(scratch, "ping -n 1 127.0.0.1 >nul", false);
            child.CloseInput(); using var timeout = new CancellationTokenSource(5000);
            Check.True(await child.WaitForExitAsync(timeout.Token) != 0); Check.Equal(1u, child.TotalProcesses);
        });
        yield return new("native-codex/S1-T8-dispose-descendants", async () =>
        {
            using var scratch = new Scratch(); await using var child = Start(scratch, "start /b ping -n 30 127.0.0.1 >nul", true);
            child.CloseInput(); using var timeout = new CancellationTokenSource(5000); await child.WaitForExitAsync(timeout.Token);
            Check.True(child.ActiveProcesses >= 1); var watch = Stopwatch.StartNew(); await child.DisposeAsync(); Check.True(watch.Elapsed < TimeSpan.FromSeconds(3));
        });
        yield return TestCase.Sync("native-codex/S1-T9-native-allows-descendants", () => Check.True(NativeCodex.Launch(@"C:\x\codex.exe", [], @"C:\w").AllowDescendants));
        yield return new("native-codex/S1-T10-transport-inherited-pipes", async () =>
        {
            using var scratch = new Scratch(); await using var transport = new JsonRpcTransport(Start(scratch, "start /b ping -n 30 127.0.0.1 >nul & exit 0", true));
            var watch = Stopwatch.StartNew(); await transport.FinishAsync(default); Check.True(watch.Elapsed < TimeSpan.FromSeconds(4));
            watch.Restart(); await transport.DisposeAsync(); Check.True(watch.Elapsed < TimeSpan.FromSeconds(3));
        });
        yield return new("native-codex/S1-T11-transport-nonzero", async () =>
        {
            using var scratch = new Scratch(); await using var transport = new JsonRpcTransport(Start(scratch, "exit 3", true));
            Check.Equal("cleanup-failure", (await Check.ThrowsAsync<CoreException>(() => transport.FinishAsync(default))).Category);
        });
    }
    private static WindowsProcess Start(Scratch scratch, string command, bool descendants) => WindowsProcess.Start(
        NativeCodex.Launch(Path.Combine(Environment.SystemDirectory, "cmd.exe"), ["/c " + command], scratch.Root) with { AllowDescendants = descendants });
    public static async Task<int> Live()
    {
        var watch = Stopwatch.StartNew();
        try
        {
            var exe = NativeCodex.FindExecutable() ?? throw new CoreException("codex-not-found");
            var reading = await NativeCodex.Create(exe, NativeCodex.WorkingDirectory()).FetchAsync(DateTimeOffset.UtcNow) ?? throw new CoreException("no-weekly-data");
            Console.WriteLine(FormattableString.Invariant($"CODEX weekly_remaining={Math.Round(100 - reading.Weekly.UsedPercent, MidpointRounding.AwayFromZero):0} resets_at={reading.Weekly.ResetsAt.UtcDateTime:O} elapsed_ms={watch.ElapsedMilliseconds}")); return 0;
        }
        catch (Exception ex) { Console.WriteLine($"CODEX error={(ex is CoreException core ? core.Category : ex.GetType().Name)}"); return 1; }
    }
}

using System.Diagnostics;
using System.Text;
using AIUsageBar.Core;

namespace AIUsageBar.Tests;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--live-codex") return await NativeCodexTests.Live();
        if (args.Length > 0 && args[0] == "--fake-rpc") return await CodexTests.FakeChild(args[1..]);
        if (args.Length > 0 && args[0] == "--fake-h2") return await H2InventoryTests.FakeChild(args[1..]);
        if (args.Length > 0 && args[0] == "--crash-parent") return await CodexTests.CrashParent(args[1..]);
        if (args.Length > 0 && args[0] == "--sleep") { await Task.Delay(60_000); return 0; }
        if (args.Length > 0 && args[0] == "--echo-args")
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(args[1..])); return 0;
        }
        if ((args.Length != 1 && !(args.Length == 3 && args[1] == "--case")) || (args[0] != "--offline" && args[0] != "--tee-experiment" && args[0] != "--file-experiment"))
        {
            Console.Error.WriteLine("Use --offline, --live-codex, --tee-experiment or --file-experiment."); return 2;
        }
        var cases = args[0] switch
        {
            "--offline" => UsageTests.Cases().Concat(RefreshTests.Cases()).Concat(CodexTests.Cases()).Concat(ClaudeTests.Cases()).Concat(H2InventoryTests.Cases()).Concat(NativeCodexTests.Cases()).Concat(ClaudeBridgeTests.Cases()).Concat(UsageTextTests.Cases()),
            "--file-experiment" => ClaudeTests.FileCases(),
            _ => ClaudeTests.TeeCases()
        };
        if (args.Length == 3) cases = cases.Where(c => c.Name.StartsWith(args[2], StringComparison.Ordinal));
        var selected = cases.ToArray();
        Console.WriteLine($"SCOPE mode={args[0]} selected={selected.Length} native=false account=false experimental={args[0] != "--offline"}");
        if (selected.Length == 0) { Console.Error.WriteLine("No matching synthetic test cases."); return 2; }
        int passed = 0, failed = 0;
        var total = Stopwatch.StartNew();
        foreach (var test in selected)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                await test.Run(); passed++;
                Console.WriteLine($"PASS {test.Name} elapsedMs={timer.ElapsedMilliseconds}");
            }
            catch (Exception exception)
            {
                failed++;
                // Never log JSON, child diagnostics, paths, account/config or exception messages.
                var frame = new StackTrace(exception, true).GetFrames().FirstOrDefault(f => f.GetFileName() is { } name && Path.GetFileName(name) != "Program.cs");
                var category = exception is CoreException core ? core.Category : "none";
                Console.WriteLine($"FAIL {test.Name} type={exception.GetType().Name} category={category} source={Path.GetFileName(frame?.GetFileName())}:{frame?.GetFileLineNumber()} elapsedMs={timer.ElapsedMilliseconds}");
                foreach (var source in new StackTrace(exception, true).GetFrames().Where(f => f.GetFileName() is not null))
                    Console.WriteLine($"TRACE source={Path.GetFileName(source.GetFileName())}:{source.GetFileLineNumber()} hresult={exception.HResult:X8}");
            }
        }
        Console.WriteLine($"SUMMARY passed={passed} failed={failed} skipped=0 elapsedMs={total.ElapsedMilliseconds}");
        return failed == 0 ? 0 : 1;
    }
}

internal sealed record TestCase(string Name, Func<Task> Run)
{
    public static TestCase Sync(string name, Action test) => new(name, () => { test(); return Task.CompletedTask; });
}

internal static class Check
{
    public static void True(bool condition) { if (!condition) throw new TestFailure(); }
    public static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new TestFailure(); }
    public static void Bytes(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual) { True(expected.SequenceEqual(actual)); }
    public static T Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T exception) { return exception; }
        throw new TestFailure();
    }
    public static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T exception) { return exception; }
        throw new TestFailure();
    }
}

internal sealed class TestFailure : Exception;

internal sealed class Scratch : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "AIUsageBar-OfflineTests", Guid.NewGuid().ToString("N"));
    public Scratch() { Directory.CreateDirectory(Root); }
    public void Dispose()
    {
        var full = Path.GetFullPath(Root);
        var allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "AIUsageBar-OfflineTests")) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new TestFailure();
        var deadline = Stopwatch.StartNew();
        while (Directory.Exists(Root))
        {
            try { Directory.Delete(Root, recursive: true); }
            catch (IOException) when (deadline.Elapsed < TimeSpan.FromSeconds(2)) { Thread.Sleep(10); }
            catch (UnauthorizedAccessException) when (deadline.Elapsed < TimeSpan.FromSeconds(2)) { Thread.Sleep(10); }
        }
    }
}

internal static class Fixture
{
    public static DateTimeOffset Now => DateTimeOffset.FromUnixTimeSeconds(1_790_812_800);
    public static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
    public static byte[] Claude(string percent = "25", string reset = "1791273600") => Utf8(
        "{\"rate_limits\":{\"seven_day\":{\"used_percentage\":" + percent + ",\"resets_at\":" + reset + "}}}");
    public static byte[] Codex(string percent = "25", string reset = "1791273600", string duration = "10080") => Utf8(
        "{\"rateLimits\":{\"limitId\":\"codex\",\"secondary\":{\"usedPercent\":" + percent + ",\"resetsAt\":" + reset + ",\"windowDurationMins\":" + duration + "}}}");
    public static string Self => Path.Combine(AppContext.BaseDirectory, "AIUsageBar.Tests.exe");
    public static string Bridge => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "../../../../../src/AIUsageBar.Bridge/bin/Release/net10.0-windows/AIUsageBar.Bridge.exe"));
}

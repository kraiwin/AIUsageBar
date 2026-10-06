using AIUsageBar.Core;
using System.Diagnostics;

if (args.Length > 0 && args[0] == "statusline") return await LiveStatusLine.Run(args[1..]);

// No-argument inert placeholder: no console output, account access or writes.
if (args.Length != 3 || args[0] != "--capture-test" || args[1] != "--scratch" || !Path.IsPathFullyQualified(args[2])) return 1;
var timer = Stopwatch.StartNew();
// Lifetime covers reading AND filesystem work. Background watchdog cannot be delayed by an open pipe or blocked filesystem.
using var deadline = new Timer(_ => Environment.Exit(2), null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
try
{
    var input = Console.OpenStandardInput();
    using var capture = new MemoryStream(); bool overflow = false;
    var buffer = new byte[16384];
    while (true)
    {
        var read = await input.ReadAsync(buffer);
        if (read == 0) break;
        if (!overflow && capture.Length + read <= 2 * 1024 * 1024) capture.Write(buffer, 0, read);
        else { overflow = true; capture.SetLength(0); }
    }
    // Never ingest incomplete input. No store/directory/marker exists before EOF.
    if (timer.Elapsed >= TimeSpan.FromSeconds(2)) return 2;
    using var store = new ClaudeSnapshotStore(args[2]);
    store.Capture(overflow ? "{"u8.ToArray() : capture.ToArray(), DateTimeOffset.UtcNow);
    return 0;
}
catch { return 1; }


internal static class LiveStatusLine
{
    public static async Task<int> Run(string[] args)
    {
        var root = AppPaths.Root;
        if (args.Length != 0)
        {
            if (args.Length != 2 || args[0] != "--root" || !Path.IsPathFullyQualified(args[1])) return 1;
            root = args[1];
        }
        byte[] bytes;
        try
        {
            var read = Task.Run(async () =>
            {
                using var capture = new MemoryStream(); var buffer = new byte[16384]; var input = Console.OpenStandardInput();
                while (true)
                {
                    var count = await input.ReadAsync(buffer); if (count == 0) return capture.ToArray();
                    if (capture.Length + count > 2 * 1024 * 1024) throw new CoreException("capture-limit");
                    capture.Write(buffer, 0, count);
                }
            });
            bytes = await read.WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch { Capture(root, "{"u8.ToArray()); return 0; }
        Capture(root, bytes);
        string? command;
        try { command = ClaudeBridgeInstaller.OriginalCommand(root); } catch { return 0; }
        if (command is null) return 0;
        try
        {
            var bash = Environment.GetEnvironmentVariable("CLAUDE_CODE_GIT_BASH_PATH") ?? @"C:\Program Files\Git\bin\bash.exe";
            await using var child = WindowsProcess.Start(NativeCodex.Launch(bash, ["-c", command], Environment.CurrentDirectory));
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            // Drain output concurrently with writing stdin to avoid pipe backpressure deadlocks.
            var output = Task.Run(() => child.Output.CopyToAsync(Console.OpenStandardOutput(), deadline.Token));
            var error = Task.Run(() => child.Error.CopyToAsync(Stream.Null, deadline.Token));
            var write = Task.Run(async () =>
            {
                try { await child.Input.WriteAsync(bytes, deadline.Token); await child.Input.FlushAsync(deadline.Token); }
                catch (IOException) { /* Commands such as echo can exit without consuming stdin. */ }
                finally { child.CloseInput(); }
            });
            var exit = await child.WaitForExitAsync(deadline.Token);
            await Task.WhenAll(output, error, write).WaitAsync(deadline.Token); return exit;
        }
        catch { return 1; }
    }
    private static void Capture(string root, byte[] bytes)
    {
        try
        {
            Directory.CreateDirectory(root); using var store = new ClaudeSnapshotStore(Path.Combine(root, "claude"));
            store.Capture(bytes, DateTimeOffset.UtcNow);
        }
        catch { /* Snapshot failure must not interrupt the original statusline. */ }
    }
}

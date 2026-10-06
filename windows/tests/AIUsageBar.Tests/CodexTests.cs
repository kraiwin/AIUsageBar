using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using AIUsageBar.Core;
using Microsoft.Win32.SafeHandles;

namespace AIUsageBar.Tests;

internal static class CodexTests
{
    public static ProcessLaunch Launch(Scratch scratch, params string[] args) => new(Fixture.Self, args, scratch.Root,
        new Dictionary<string, string> { ["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            ["PATH"] = Path.GetDirectoryName(Fixture.Self)!, ["TEMP"] = scratch.Root, ["TMP"] = scratch.Root });
    public static IEnumerable<TestCase> Cases()
    {
        yield return TestCase.Sync("discovery/synthetic-package-native-tuple-no-execution", () =>
        {
            using var scratch = new Scratch();
            var package = Directory.CreateDirectory(Path.Combine(scratch.Root, "node_modules", "@openai", "codex")).FullName;
            File.WriteAllText(Path.Combine(package, "package.json"), "{\"name\":\"@openai/codex\",\"version\":\"SYNTHETIC-ONLY\"}");
            var native = Directory.CreateDirectory(Path.Combine(package, "vendor", "x86_64-pc-windows-msvc", "bin")).FullName;
            var path = Path.Combine(native, "codex.exe"); File.Copy(Fixture.Self, path);
            var tuple = CodexDiscovery.Discover(scratch.Root).Single();
            Check.Equal("SYNTHETIC-ONLY", tuple.MetadataVersion); Check.Equal(new FileInfo(path).Length, tuple.Size);
            Check.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))), tuple.Sha256);
            var original = tuple.FileId;
            File.Move(path, path + ".old"); File.Copy(Fixture.Self, path);
            Check.True(CodexDiscovery.Inspect(path, "SYNTHETIC-ONLY").FileId != original);
        });
        yield return TestCase.Sync("discovery/reject-script-and-wrong-architecture", () =>
        {
            using var scratch = new Scratch(); var path = Path.Combine(scratch.Root, "fixture.exe");
            File.WriteAllText(path, new string('x', 70)); Check.Throws<CoreException>(() => CodexDiscovery.Inspect(path, "synthetic"));
            var bytes = new byte[128]; bytes[0] = (byte)'M'; bytes[1] = (byte)'Z'; bytes[60] = 64;
            "PE\0\0"u8.CopyTo(bytes.AsSpan(64)); bytes[68] = 0x4c; bytes[69] = 0x01;
            File.WriteAllBytes(path, bytes); Check.Throws<CoreException>(() => CodexDiscovery.Inspect(path, "synthetic"));
        });
        yield return TestCase.Sync("policy/strict-registry-types-stages-and-bounds", () =>
        {
            var good = new[] { new CodexFeature("hooks", false, "stable"), new CodexFeature("plugins", false, "beta"), new CodexFeature("code_mode_host", false, "underDevelopment") };
            CodexPolicy.AssertRegistry(good);
            Check.Throws<CoreException>(() => CodexPolicy.AssertRegistry([.. good, good[0]]));
            Check.Throws<CoreException>(() => CodexPolicy.AssertRegistry([good[0] with { Stage = "removed" }, good[1], good[2]]));
            Check.Throws<CoreException>(() => CodexPolicy.AssertRegistry([good[0] with { Enabled = true }, good[1], good[2]]));
            Check.Throws<CoreException>(() => CodexPolicy.RegistryPage(JsonSerializer.SerializeToElement(new { data = new[] { new { name = "hooks", enabled = 0, stage = "stable" } } })));
            Check.Throws<CoreException>(() => CodexPolicy.RegistryPage(JsonSerializer.SerializeToElement(new { data = Enumerable.Range(0, 101).Select(i => new { name = i.ToString(), enabled = false, stage = "stable" }) })));
        });
        yield return TestCase.Sync("policy/inventory-bound-and-TOML-escaping", () =>
        {
            Check.Equal("\"quote\\\"slash\\\\ไทย\"", CodexPolicy.TomlString("quote\"slash\\ไทย"));
            Check.Throws<CoreException>(() => CodexPolicy.TomlString("newline\n"));
            Check.Throws<CoreException>(() => CodexPolicy.TomlString("\uD800"));
            var servers = Enumerable.Range(0, 257).Select(i => new McpServer("name" + i, McpTransportKind.Stdio, false)).ToArray();
            Check.Throws<CoreException>(() => CodexPolicy.Arguments(Fixture.Bridge, servers));
            Check.Throws<CoreException>(() => CodexPolicy.Arguments(Fixture.Bridge, [servers[0], servers[0]]));
        });
        yield return TestCase.Sync("process/explicit-failed-association-accounting-control", () =>
        {
            using var scratch = new Scratch();
            using var child = TestProcess.Start(Fixture.Self, ["--sleep"], scratch.Root);
            var result = child.FailedAssociation(Fixture.Self, scratch.Root);
            Console.WriteLine($"EVIDENCE explicitFailedAssociationError={result.Error} total={result.Total} active={result.Active} terminated={result.Terminated} secondaryNeverResumed=true");
            Check.Equal(1816, result.Error); Check.Equal(2u, result.Total);
        });
        yield return new("process/quoted-unicode-args-detached-console-pipes", async () =>
        {
            using var scratch = new Scratch();
            var args = new[] { "space ไทย", "quote\"slash\\", "", "trailing\\", "emoji😀" };
            await using var child = WindowsProcess.Start(Launch(scratch, ["--echo-args", .. args]));
            child.CloseInput();
            var data = await new StreamReader(child.Output).ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Check.True(args.SequenceEqual(JsonSerializer.Deserialize<string[]>(data)!));
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            Check.Equal(0, await child.WaitForExitAsync(deadline.Token));
            Check.Equal(0u, child.ActiveProcesses);
            Check.Equal(1u, child.TotalProcesses);
            Check.Equal(0x0008040Cu, WindowsProcess.CreationFlags);
        });
        yield return TestCase.Sync("process/commandline-UTF16-boundary", () =>
        {
            var shortLine = WindowsProcess.CommandLine("C:\\t.exe", [new string('a', 100)]);
            var overhead = shortLine.Length - 100;
            Check.Equal(32766, WindowsProcess.CommandLine("C:\\t.exe", [new string('a', 32766 - overhead)]).Length);
            Check.Throws<CoreException>(() => WindowsProcess.CommandLine("C:\\t.exe", [new string('a', 32767 - overhead)]));
            Check.Throws<CoreException>(() => WindowsProcess.CommandLine("C:\\t.exe", [string.Concat(Enumerable.Repeat("😀", 16384))]));
        });
        yield return new("process/job-active-limit-no-console-window", async () =>
        {
            using var scratch = new Scratch();
            await using var child = WindowsProcess.Start(Launch(scratch, "--fake-rpc", "spawn"));
            var line = await new StreamReader(child.Output).ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));
            using var report = JsonDocument.Parse(line!);
            Check.True(!report.RootElement.GetProperty("spawnSucceeded").GetBoolean());
            Check.True(!report.RootElement.GetProperty("consoleWindow").GetBoolean());
            child.CloseInput();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            Check.Equal(0, await child.WaitForExitAsync(deadline.Token));
            Check.Equal(0u, child.ActiveProcesses);
        });
        yield return new("process/dispose-kills-owned-child-within-two-seconds", async () =>
        {
            using var scratch = new Scratch();
            await using var child = WindowsProcess.Start(Launch(scratch, "--sleep"));
            using var observed = Process.GetProcessById(child.ProcessId);
            var timer = Stopwatch.StartNew();
            await child.DisposeAsync();
            Check.True(observed.HasExited);
            Check.True(timer.Elapsed < TimeSpan.FromSeconds(2.2));
        });
        yield return new("process/parent-crash-closes-job-child", async () =>
        {
            using var scratch = new Scratch();
            using var parent = TestProcess.Start(Fixture.Self, ["--crash-parent", scratch.Root], scratch.Root);
            var line = await parent.OutputLine().WaitAsync(TimeSpan.FromSeconds(5));
            using var child = Process.GetProcessById(int.Parse(line, System.Globalization.CultureInfo.InvariantCulture));
            parent.KillTop();
            var timer = Stopwatch.StartNew();
            while (!child.HasExited && timer.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(20);
            Check.True(child.HasExited);
        });
        foreach (var scenario in new[] { "valid", "crlf", "partial", "quota-EOF-noLF" })
        {
            var mode = scenario;
            yield return new($"rpc/{mode}-two-child-quota", async () =>
            {
                using var scratch = new Scratch();
                int launched = 0;
                var children = new List<Process>();
                var provider = new CodexProvider((arguments, _) =>
                {
                    Check.True(children.All(c => c.HasExited));
                    if (launched == 0) Check.True(!arguments.Any(a => a.StartsWith("mcp_servers=", StringComparison.Ordinal)));
                    else
                    {
                        var disable = arguments.Single(a => a.StartsWith("mcp_servers=", StringComparison.Ordinal));
                        Check.True(disable.Contains("stdio-test", StringComparison.Ordinal) && disable.Contains("http-test", StringComparison.Ordinal));
                        Check.True(disable.Contains("enabled=false", StringComparison.Ordinal) && disable.Contains("https://example.invalid/", StringComparison.Ordinal));
                        Check.True(!disable.Contains("should-not-run", StringComparison.Ordinal));
                    }
                    var process = WindowsProcess.Start(Launch(scratch, "--fake-rpc", mode, launched == 0 ? "first" : "second"));
                    children.Add(Process.GetProcessById(process.ProcessId)); launched++;
                    return Task.FromResult<ICodexTransport>(new JsonRpcTransport(process));
                }, Fixture.Bridge);
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                try
                {
                    var reading = await provider.FetchAsync(Fixture.Now, deadline.Token);
                    Check.Equal(75d, reading!.Weekly.RemainingPercent);
                    Check.Equal(2, launched);
                    foreach (var child in children) Check.True(child.HasExited);
                }
                finally { foreach (var child in children) child.Dispose(); }
            });
        }
        foreach (var scenario in new[] { "malformed", "wrong-id", "server-request", "oversize", "quota-oversize", "stdout-total-overflow", "stderr-overflow", "line-overflow", "registry-cycle", "registry-page-bound", "registry-entry-bound", "crash", "stall", "feature-enabled", "inventory-drift", "beforeB-abnormal-exit", "after-quota-abnormal-exit", "after-quota-server-request", "after-quota-wrong-id", "after-quota-stderr-overflow", "after-quota-stderr-line-overflow" })
        {
            var mode = scenario;
            yield return new($"rpc/reject-{mode}", async () =>
            {
                using var scratch = new Scratch();
                int launched = 0;
                var children = new List<Process>();
                var provider = new CodexProvider((_, _) =>
                {
                    launched++;
                    var effectiveMode = (mode == "inventory-drift" || mode.StartsWith("after-quota", StringComparison.Ordinal)) && launched == 1 ? "valid" : mode;
                    var process = WindowsProcess.Start(Launch(scratch, "--fake-rpc", effectiveMode, launched == 1 ? "first" : "second"));
                    children.Add(Process.GetProcessById(process.ProcessId));
                    return Task.FromResult<ICodexTransport>(new JsonRpcTransport(process));
                }, Fixture.Bridge);
                using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(mode == "stall" ? 300 : 8000));
                try
                {
                    if (mode == "stall") await Check.ThrowsAsync<OperationCanceledException>(async () => { await provider.FetchAsync(Fixture.Now, cancellation.Token); });
                    else
                    {
                        var rejected = await Check.ThrowsAsync<CoreException>(async () => { await provider.FetchAsync(Fixture.Now, cancellation.Token); });
                        Check.True(rejected.Category is "invalid-json" or "unexpected-response" or "server-request" or "output-limit" or "io-failure" or "unexpected-eof" or "unsupported-configuration" or "cleanup-failure");
                    }
                    Check.True(launched <= 2);
                    if (mode == "beforeB-abnormal-exit") Check.Equal(1, launched);
                    foreach (var child in children) Check.True(child.HasExited);
                }
                finally
                {
                    foreach (var child in children) child.Dispose();
                }
            });
        }
        yield return new("process/blocked-createprocess-enforced-accounting-limitation", async () =>
        {
            using var scratch = new Scratch();
            var process = WindowsProcess.Start(Launch(scratch, "--fake-rpc", "after-quota-blocked-spawn", "second"));
            await using var transport = new JsonRpcTransport(process);
            var beforeTotal = process.TotalProcesses;
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await transport.RequestAsync("account/rateLimits/read", JsonSerializer.SerializeToElement(new { }), deadline.Token);
            await transport.FinishAsync(deadline.Token);
            using var proof = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(scratch.Root, "spawn-proof.json")));
            var r = proof.RootElement;
            Check.Equal(1816, r.GetProperty("error").GetInt32());
            Check.Equal(1u, beforeTotal); Check.Equal(1u, process.TotalProcesses); Check.Equal(0u, process.ActiveProcesses); Check.Equal(0u, process.TerminatedProcesses);
            Check.Equal(1u, r.GetProperty("afterTotal").GetUInt32()); Check.Equal(0u, r.GetProperty("afterTerminated").GetUInt32());
            Console.WriteLine("LIMITATION blockedCreateProcessError=1816 ownJobTotal=1 ownJobActive=0 ownJobTerminated=0 FinishAccepted=true attemptDetection=false");
        });
    }

    public static async Task<int> FakeChild(string[] args)
    {
        var scenario = args[0]; var second = args.Length > 1 && args[1] == "second";
        if (scenario == "spawn")
        {
            bool succeeded = false;
            try
            {
                using var nested = Process.Start(new ProcessStartInfo(Fixture.Self) { UseShellExecute = false, ArgumentList = { "--sleep" } });
                succeeded = nested is not null;
                if (nested is not null) { nested.Kill(); await nested.WaitForExitAsync(); }
            }
            catch (Win32Exception) { }
            Console.WriteLine(JsonSerializer.Serialize(new { spawnSucceeded = succeeded, consoleWindow = GetConsoleWindow() != 0 }));
            return 0;
        }
        if (scenario == "crash") return 17;
        if (scenario == "stall") { await Task.Delay(60_000); return 0; }
        int registryPage = 0;
        var input = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        var output = Console.OpenStandardOutput();
        while (await input.ReadLineAsync() is { } line)
        {
            using var request = JsonDocument.Parse(line);
            var root = request.RootElement;
            if (!root.TryGetProperty("id", out var identifier)) continue;
            var method = root.GetProperty("method").GetString();
            string result = method switch
            {
                "initialize" => "{\"userAgent\":\"synthetic-test\"}",
                "experimentalFeature/list" => $$"""{"data":[{"name":"hooks","enabled":{{(scenario == "feature-enabled" ? "true" : "false")}},"stage":"stable"},{"name":"plugins","enabled":false,"stage":"stable"},{"name":"code_mode_host","enabled":false,"stage":"underDevelopment"}],"nextCursor":null}""",
                "config/read" => scenario == "inventory-drift"
                    ? "{\"config\":{\"notify\":[],\"analytics\":{\"enabled\":false},\"otel\":{\"exporter\":\"none\",\"trace_exporter\":\"none\"},\"mcp_servers\":{\"added\":{\"enabled\":false,\"command\":\"fake.exe\"}}}}"
                    : "{\"config\":{\"notify\":[],\"analytics\":{\"enabled\":false},\"otel\":{\"exporter\":\"none\",\"trace_exporter\":\"none\"},\"mcp_servers\":{\"stdio-test\":{\"enabled\":" + (second ? "false" : "true") + ",\"command\":\"should-not-run.exe\"},\"http-test\":{\"enabled\":" + (second ? "false" : "true") + ",\"url\":\"https://example.invalid/\"}}}}",
                "account/rateLimits/read" => Encoding.UTF8.GetString(Fixture.Codex()),
                _ => throw new TestFailure()
            };
            string envelope = $"{{\"id\":{identifier.GetRawText()},\"result\":{result}}}";
            if (method == "experimentalFeature/list" && scenario.StartsWith("registry-", StringComparison.Ordinal))
            {
                registryPage++;
                var cursor = scenario == "registry-cycle" ? "cycle" : "page" + registryPage;
                var data = scenario == "registry-entry-bound" ? Enumerable.Range(0, 100).Select(i => new { name = "feature" + (registryPage * 100 + i), enabled = false, stage = "stable" }).ToArray() : [];
                envelope = JsonSerializer.Serialize(new { id = identifier.GetInt32(), result = new { data, nextCursor = cursor } });
            }
            if (scenario == "malformed") envelope = "{";
            if (scenario == "wrong-id") envelope = "{\"id\":999,\"result\":{}}";
            if (scenario == "server-request") envelope = "{\"id\":42,\"method\":\"account/chatgptAuthTokens/refresh\",\"params\":{}}";
            if (scenario == "oversize") envelope = new string('x', 2 * 1024 * 1024 + 1);
            if (scenario == "quota-oversize" && method == "account/rateLimits/read") envelope = "{\"id\":" + identifier.GetInt32() + ",\"result\":{\"padding\":\"" + new string('x', 65536) + "\"}}";
            if (scenario == "stdout-total-overflow" && method == "initialize")
            {
                var notification = Encoding.UTF8.GetBytes("{\"method\":\"synthetic\",\"params\":{\"pad\":\"" + new string('x', 32760) + "\"}}\n");
                for (int i = 0; i < 513; i++) await output.WriteAsync(notification);
            }
            if (scenario == "stderr-overflow")
            {
                var block = new byte[64 * 1024];
                for (int i = 0; i < 257; i++) await Console.OpenStandardError().WriteAsync(block);
            }
            if (scenario == "line-overflow")
                for (int i = 0; i < 4097; i++) await output.WriteAsync(Encoding.UTF8.GetBytes("{\"method\":\"synthetic\"}\n"));
            var bytes = Encoding.UTF8.GetBytes(envelope + (scenario == "quota-EOF-noLF" && method == "account/rateLimits/read" ? "" : scenario == "crlf" ? "\r\n" : "\n"));
            if (scenario == "partial")
            {
                await output.WriteAsync(bytes.AsMemory(0, bytes.Length / 2)); await output.FlushAsync();
                await Task.Delay(5); await output.WriteAsync(bytes.AsMemory(bytes.Length / 2));
            }
            else await output.WriteAsync(bytes);
            await output.FlushAsync();
            if (method == "account/rateLimits/read")
            {
                if (scenario == "quota-EOF-noLF") return 0;
                if (scenario == "after-quota-abnormal-exit") return 17;
                if (scenario == "after-quota-server-request") await output.WriteAsync("{\"id\":42,\"method\":\"account/chatgptAuthTokens/refresh\",\"params\":{}}\n"u8.ToArray());
                if (scenario == "after-quota-wrong-id") await output.WriteAsync("{\"id\":999,\"result\":{}}\n"u8.ToArray());
                if (scenario == "after-quota-stderr-overflow")
                    for (int i = 0; i < 257; i++) await Console.OpenStandardError().WriteAsync(new byte[64 * 1024]);
                if (scenario == "after-quota-stderr-line-overflow") await Console.OpenStandardError().WriteAsync(Encoding.UTF8.GetBytes(new string('\n', 4097)));
                if (scenario == "after-quota-blocked-spawn")
                {
                    Check.True(QueryInformationJobObject(0, 1, out var before, (uint)Marshal.SizeOf<Accounting>(), 0));
                    int error = 0;
                    try { using var nested = Process.Start(new ProcessStartInfo(Fixture.Self) { UseShellExecute = false, ArgumentList = { "--sleep" } }); }
                    catch (Win32Exception exception) { error = exception.NativeErrorCode; }
                    Check.True(QueryInformationJobObject(0, 1, out var after, (uint)Marshal.SizeOf<Accounting>(), 0));
                    File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, "spawn-proof.json"), JsonSerializer.SerializeToUtf8Bytes(new
                    { error, beforeTotal = before.Total, afterTotal = after.Total, afterActive = after.Active, afterTerminated = after.Terminated }));
                }
                await output.FlushAsync();
            }
        }
        return scenario == "beforeB-abnormal-exit" ? 17 : 0;
    }

    public static async Task<int> CrashParent(string[] args)
    {
        await using var child = WindowsProcess.Start(new ProcessLaunch(Fixture.Self, ["--sleep"], args[0],
            new Dictionary<string, string> { ["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows) }));
        Console.WriteLine(child.ProcessId); await Console.Out.FlushAsync();
        await Task.Delay(60_000); return 0;
    }

    [DllImport("kernel32.dll")] private static extern nint GetConsoleWindow();
    [StructLayout(LayoutKind.Sequential)] private struct Accounting
    { public long User, Kernel, PeriodUser, PeriodKernel; public uint Faults, Total, Active, Terminated; }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool QueryInformationJobObject(nint job, int kind, out Accounting info, uint size, nint returned);
}

// Test-only multi-process job, never used by the production limit-1 transport.
// Suspended assignment eliminates unowned-launch races. Candidate cancellation
// terminates only the returned top PID; job disposal happens AFTER observation.
internal sealed class TestProcess : IDisposable
{
    private readonly nint job, process, thread;
    public int Id { get; }
    public FileStream Input { get; }
    public FileStream Output { get; }
    public FileStream Error { get; }
    public Process Observed { get; }
    private TestProcess(nint job, ProcessInformation info, nint input, nint output, nint error)
    {
        this.job = job; process = info.Process; thread = info.Thread; Id = (int)info.ProcessId;
        Input = new(new SafeFileHandle(input, ownsHandle: true), FileAccess.Write);
        Output = new(new SafeFileHandle(output, ownsHandle: true), FileAccess.Read);
        Error = new(new SafeFileHandle(error, ownsHandle: true), FileAccess.Read);
        Observed = Process.GetProcessById(Id);
    }
    public static TestProcess Start(string executable, IReadOnlyList<string> arguments, string scratch, IReadOnlyDictionary<string, string>? environment = null, string? commandLine = null)
    {
        var security = new SecurityAttributes { Size = Marshal.SizeOf<SecurityAttributes>(), Inherit = 1 };
        nint inputRead = 0, inputWrite = 0, outputRead = 0, outputWrite = 0, errorRead = 0, errorWrite = 0;
        nint job = 0; ProcessInformation info = default;
        try
        {
            Require(CreatePipe(out inputRead, out inputWrite, ref security, 0));
            Require(CreatePipe(out outputRead, out outputWrite, ref security, 0));
            Require(CreatePipe(out errorRead, out errorWrite, ref security, 0));
            Require(SetHandleInformation(inputWrite, 1, 0)); Require(SetHandleInformation(outputRead, 1, 0)); Require(SetHandleInformation(errorRead, 1, 0));
            job = CreateJobObjectW(0, null); Require(job != 0);
            var limits = new ExtendedLimit { Basic = new BasicLimit { Flags = 0x2000 } };
            Require(SetInformationJobObject(job, 9, ref limits, (uint)Marshal.SizeOf<ExtendedLimit>()));
            var startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfo>(), Flags = 0x100, Input = inputRead, Output = outputWrite, Error = errorWrite };
            nint environmentBlock = 0;
            try
            {
                if (environment is not null)
                    environmentBlock = Marshal.StringToHGlobalUni(string.Join('\0', environment.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase).Select(e => e.Key + "=" + e.Value)) + "\0\0");
                var command = commandLine ?? WindowsProcess.CommandLine(executable, arguments);
                Check.True(command.Length + 1 <= 32767);
                Require(CreateProcessW(executable, new StringBuilder(command), 0, 0, true,
                    0x08000004 | (environmentBlock == 0 ? 0u : 0x400u), environmentBlock, scratch, ref startup, out info));
            }
            finally { if (environmentBlock != 0) Marshal.FreeHGlobal(environmentBlock); }
            Require(AssignProcessToJobObject(job, info.Process));
            Require(ResumeThread(info.Thread) != uint.MaxValue);
            var result = new TestProcess(job, info, inputWrite, outputRead, errorRead);
            inputWrite = outputRead = errorRead = 0;
            job = info.Process = info.Thread = 0;
            return result;
        }
        catch
        {
            if (info.Process != 0) { TerminateProcess(info.Process, 99); WaitForSingleObject(info.Process, 2000); }
            if (job != 0) CloseHandle(job);
            if (info.Process != 0) CloseHandle(info.Process); if (info.Thread != 0) CloseHandle(info.Thread);
            throw;
        }
        finally
        {
            foreach (var handle in new[] { inputRead, inputWrite, outputRead, outputWrite, errorRead, errorWrite })
                if (handle != 0) CloseHandle(handle);
        }
    }
    public Task<string> OutputLine() => Task.Run(async () => (await new StreamReader(Output, leaveOpen: true).ReadLineAsync()) ?? throw new TestFailure());
    public void KillTop() { Require(TerminateProcess(process, 99)); Require(WaitForSingleObject(process, 2000) == 0); }
    public int[] JobPids()
    {
        var buffer = Marshal.AllocHGlobal(8 + 8 * 512);
        try
        {
            Require(QueryInformationJobObject(job, 3, buffer, 8 + 8 * 512, out _));
            int count = Marshal.ReadInt32(buffer, 4);
            Check.True(count <= 512);
            return Enumerable.Range(0, count).Select(i => (int)Marshal.ReadInt64(buffer, 8 + i * 8)).ToArray();
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    public (int Error, uint Total, uint Active, uint Terminated) FailedAssociation(string executable, string scratch)
    {
        var limits = new ExtendedLimit { Basic = new BasicLimit { Flags = 0x2000 | 8, ActiveLimit = 1 } };
        Require(SetInformationJobObject(job, 9, ref limits, (uint)Marshal.SizeOf<ExtendedLimit>()));
        var startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfo>() };
        Require(CreateProcessW(executable, new StringBuilder(WindowsProcess.CommandLine(executable, ["--sleep"])), 0, 0, false,
            0x08000004, 0, scratch, ref startup, out var owned));
        try
        {
            Check.True(!AssignProcessToJobObject(job, owned.Process)); var error = Marshal.GetLastWin32Error();
            var buffer = Marshal.AllocHGlobal(48);
            try
            {
                Require(QueryInformationJobObject(job, 1, buffer, 48, out _));
                return (error, (uint)Marshal.ReadInt32(buffer, 36), (uint)Marshal.ReadInt32(buffer, 40), (uint)Marshal.ReadInt32(buffer, 44));
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        finally
        {
            Require(TerminateProcess(owned.Process, 99)); Require(WaitForSingleObject(owned.Process, 2000) == 0);
            CloseHandle(owned.Thread); CloseHandle(owned.Process);
        }
    }
    public void Dispose()
    {
        // Disposable job contains only this harness's descendants.
        TerminateJobObject(job, 99); WaitForSingleObject(process, 2000);
        var deadline = Stopwatch.StartNew();
        while (JobPids().Length != 0 && deadline.Elapsed < TimeSpan.FromSeconds(2)) Thread.Sleep(10);
        var clean = JobPids().Length == 0;
        Input.Dispose(); Output.Dispose(); Error.Dispose(); Observed.Dispose();
        CloseHandle(thread); CloseHandle(process); CloseHandle(job);
        Check.True(clean);
    }
    private static void Require(bool condition) { if (!condition) throw new Win32Exception(Marshal.GetLastWin32Error()); }
    public static Dictionary<int, int> ParentPids()
    {
        var snapshot = CreateToolhelp32Snapshot(2, 0); Require(snapshot != -1);
        try
        {
            var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
            var result = new Dictionary<int, int>();
            if (Process32FirstW(snapshot, ref entry))
                do { result[(int)entry.ProcessId] = (int)entry.ParentId; } while (Process32NextW(snapshot, ref entry));
            return result;
        }
        finally { CloseHandle(snapshot); }
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct ProcessEntry
    {
        public uint Size, Usage, ProcessId; public nuint Heap; public uint Module, Threads, ParentId; public int Priority; public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string Exe;
    }
    [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public int Size; public nint Descriptor; public int Inherit; }
    [StructLayout(LayoutKind.Sequential)] private struct ProcessInformation { public nint Process, Thread; public uint ProcessId, ThreadId; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct StartupInfo
    {
        public int Size; public string? Reserved, Desktop, Title; public uint X, Y, XSize, YSize, XCount, YCount, Fill, Flags;
        public ushort Show, ReservedSize; public nint ReservedBytes, Input, Output, Error;
    }
    [StructLayout(LayoutKind.Sequential)] private struct BasicLimit
    {
        public long ProcessTime, JobTime; public uint Flags; public nuint Minimum, Maximum; public uint ActiveLimit; public nuint Affinity; public uint Priority, Scheduling;
    }
    [StructLayout(LayoutKind.Sequential)] private struct IoCounters { public ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes; }
    [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimit { public BasicLimit Basic; public IoCounters Io; public nuint ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory; }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CreatePipe(out nint read, out nint write, ref SecurityAttributes security, uint size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetHandleInformation(nint handle, uint mask, uint flags);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern nint CreateJobObjectW(nint security, string? name);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(nint job, int kind, ref ExtendedLimit limits, uint size);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool CreateProcessW(string app, StringBuilder command, nint security, nint threadSecurity, bool inherit, uint flags, nint environment, string cwd, ref StartupInfo startup, out ProcessInformation info);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(nint job, nint process);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(nint thread);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(nint process, uint exit);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateJobObject(nint job, uint exit);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool QueryInformationJobObject(nint job, int kind, nint info, int size, out int returned);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(nint handle, uint milliseconds);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern nint CreateToolhelp32Snapshot(uint flags, uint processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool Process32FirstW(nint snapshot, ref ProcessEntry entry);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool Process32NextW(nint snapshot, ref ProcessEntry entry);
}

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace AIUsageBar.Core;

public sealed record ProcessLaunch(string Executable, IReadOnlyList<string> Arguments, string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment, bool AllowDescendants = false);

/// <summary>Explicit fake/test transport. No executable discovery, account access or implicit environment inheritance.</summary>
public sealed class WindowsProcess : IAsyncDisposable
{
    public const uint CreationFlags = 0x0008040C;
    private readonly SafeFileHandle process, job;
    private bool disposed;
    public Stream Input { get; }
    public Stream Output { get; }
    public Stream Error { get; }
    public int ProcessId { get; }
    public bool AllowDescendants { get; }
    public uint ActiveProcesses => Accounting().ActiveProcesses;
    public uint TotalProcesses => Accounting().TotalProcesses;
    public uint TerminatedProcesses => Accounting().TerminatedProcesses;
    private WindowsProcess(SafeFileHandle process, SafeFileHandle job, int pid, Stream input, Stream output, Stream error, bool allowDescendants)
    { this.process = process; this.job = job; ProcessId = pid; Input = input; Output = output; Error = error; AllowDescendants = allowDescendants; }
    public static string QuoteArgument(string value)
    {
        if (value.Contains('\0')) throw new CoreException("invalid-argument");
        try { _ = new UTF8Encoding(false, true).GetByteCount(value); } catch (EncoderFallbackException) { throw new CoreException("invalid-argument"); }
        var result = new StringBuilder("\""); int slashes = 0;
        foreach (var c in value)
        {
            if (c == '\\') { slashes++; continue; }
            if (c == '"') result.Append('\\', slashes * 2 + 1); else result.Append('\\', slashes);
            result.Append(c); slashes = 0;
        }
        return result.Append('\\', slashes * 2).Append('"').ToString();
    }
    public static string CommandLine(string executable, IReadOnlyList<string> arguments)
    {
        var result = string.Join(" ", new[] { QuoteArgument(executable) }.Concat(arguments.Select(QuoteArgument)));
        if (result.Length + 1 > 32767) throw new CoreException("command-line-limit"); return result;
    }
    public static WindowsProcess Start(ProcessLaunch launch)
    {
        if (!Path.IsPathFullyQualified(launch.Executable) || !Path.IsPathFullyQualified(launch.WorkingDirectory)) throw new CoreException("invalid-path");
        var command = new StringBuilder(CommandLine(launch.Executable, launch.Arguments));
        var envText = string.Join('\0', launch.Environment.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x =>
        {
            if (x.Key.Length == 0 || x.Key.Contains('=') || x.Key.Contains('\0') || x.Value.Contains('\0')) throw new CoreException("invalid-environment");
            return x.Key + "=" + x.Value;
        })) + "\0\0";
        using var executablePin = File.OpenHandle(launch.Executable, FileMode.Open, FileAccess.Read, FileShare.Read);
        var handles = new List<SafeFileHandle>();
        IntPtr list = IntPtr.Zero, allowed = IntPtr.Zero, env = IntPtr.Zero; bool initialized = false;
        SafeFileHandle? ownedProcess = null, ownedJob = null, thread = null;
        try
        {
            var sa = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), Inherit = 1 };
            Pipe(out var stdinRead, out var stdinWrite, ref sa); handles.AddRange([stdinRead, stdinWrite]);
            Pipe(out var stdoutRead, out var stdoutWrite, ref sa); handles.AddRange([stdoutRead, stdoutWrite]);
            Pipe(out var stderrRead, out var stderrWrite, ref sa); handles.AddRange([stderrRead, stderrWrite]);
            foreach (var handle in new[] { stdinWrite, stdoutRead, stderrRead }) if (!SetHandleInformation(handle, 1, 0)) throw new CoreException("pipe-failure");
            ownedJob = CreateJobObjectW(IntPtr.Zero, null);
            if (ownedJob.IsInvalid) throw new CoreException("job-failure");
            var limits = new ExtendedLimits { Basic = new BasicLimits { Flags = launch.AllowDescendants ? 0x2000u : 0x2008u, ActiveProcessLimit = launch.AllowDescendants ? 0u : 1u } };
            if (!SetInformationJobObject(ownedJob, 9, ref limits, (uint)Marshal.SizeOf<ExtendedLimits>())) throw new CoreException("job-failure");
            nuint length = 0; _ = InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref length);
            list = Marshal.AllocHGlobal(checked((int)length));
            if (!InitializeProcThreadAttributeList(list, 1, 0, ref length)) throw new CoreException("launch-failure");
            initialized = true;
            allowed = Marshal.AllocHGlobal(3 * IntPtr.Size);
            Marshal.WriteIntPtr(allowed, 0, stdinRead.DangerousGetHandle()); Marshal.WriteIntPtr(allowed, IntPtr.Size, stdoutWrite.DangerousGetHandle()); Marshal.WriteIntPtr(allowed, 2 * IntPtr.Size, stderrWrite.DangerousGetHandle());
            if (!UpdateProcThreadAttribute(list, 0, (nuint)0x20002, allowed, (nuint)(3 * IntPtr.Size), IntPtr.Zero, IntPtr.Zero)) throw new CoreException("launch-failure");
            var startup = new StartupEx { Startup = new Startup { Size = Marshal.SizeOf<StartupEx>(), Flags = 0x100, Input = stdinRead.DangerousGetHandle(), Output = stdoutWrite.DangerousGetHandle(), Error = stderrWrite.DangerousGetHandle() }, Attributes = list };
            env = Marshal.StringToHGlobalUni(envText);
            if (!CreateProcessW(launch.Executable, command, IntPtr.Zero, IntPtr.Zero, true, CreationFlags, env, launch.WorkingDirectory, ref startup, out var info)) throw new CoreException("launch-failure");
            ownedProcess = new(info.Process, true); thread = new(info.Thread, true);
            if (!AssignProcessToJobObject(ownedJob, ownedProcess)) throw new CoreException("job-assignment-failure");
            if (ResumeThread(thread) == uint.MaxValue) throw new CoreException("launch-failure");
            stdinRead.Dispose(); stdoutWrite.Dispose(); stderrWrite.Dispose();
            var result = new WindowsProcess(ownedProcess, ownedJob, checked((int)info.ProcessId),
                new FileStream(stdinWrite, FileAccess.Write, 4096, false), new FileStream(stdoutRead, FileAccess.Read, 4096, false), new FileStream(stderrRead, FileAccess.Read, 4096, false), launch.AllowDescendants);
            handles.Clear(); ownedProcess = null; ownedJob = null; return result;
        }
        finally
        {
            // Failure cleanup uses only the handle of this owned suspended child.
            bool cleanupFailed = false;
            if (ownedProcess is not null)
            { cleanupFailed = !TerminateProcess(ownedProcess, 1) || WaitForSingleObject(ownedProcess, 2000) != 0; ownedProcess.Dispose(); }
            ownedJob?.Dispose(); thread?.Dispose(); foreach (var handle in handles) handle.Dispose();
            if (list != IntPtr.Zero) { if (initialized) DeleteProcThreadAttributeList(list); Marshal.FreeHGlobal(list); }
            if (allowed != IntPtr.Zero) Marshal.FreeHGlobal(allowed); if (env != IntPtr.Zero) Marshal.FreeHGlobal(env);
            if (cleanupFailed) throw new CoreException("cleanup-failure");
        }
    }
    private static void Pipe(out SafeFileHandle read, out SafeFileHandle write, ref SecurityAttributes attributes)
    { if (!CreatePipe(out read, out write, ref attributes, 0)) throw new CoreException("pipe-failure"); }
    public void CloseInput()
    { try { Input.Dispose(); } catch (IOException) { throw new CoreException("io-failure"); } }
    public async Task<int> WaitForExitAsync(CancellationToken cancellationToken)
    {
        uint wait;
        while ((wait = WaitForSingleObject(process, 0)) == 258) { cancellationToken.ThrowIfCancellationRequested(); await Task.Delay(10, cancellationToken).ConfigureAwait(false); }
        if (wait != 0) throw new CoreException("wait-failure");
        if (!GetExitCodeProcess(process, out var code)) throw new CoreException("wait-failure"); return unchecked((int)code);
    }
    private AccountingInfo Accounting()
    { if (disposed || !QueryInformationJobObject(job, 1, out var info, (uint)Marshal.SizeOf<AccountingInfo>(), IntPtr.Zero)) throw new CoreException("accounting-failure"); return info; }
    public async ValueTask DisposeAsync()
    {
        if (disposed) return; bool failed = false;
        try
        {
            try { CloseInput(); } catch (CoreException) { failed = true; }
            var watch = Stopwatch.StartNew();
            if (ActiveProcesses != 0 && !TerminateJobObject(job, 1)) throw new CoreException("cleanup-failure");
            while (ActiveProcesses != 0 && watch.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(10).ConfigureAwait(false);
            if (ActiveProcesses != 0) throw new CoreException("cleanup-failure");
        }
        catch (CoreException) { failed = true; }
        finally
        {
            disposed = true; job.Dispose(); process.Dispose();
            foreach (var stream in new[] { Input, Output, Error })
                try { stream.Dispose(); } catch (IOException) { failed = true; }
        }
        if (failed) throw new CoreException("cleanup-failure");
    }
    [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public int Length; public IntPtr Descriptor; public int Inherit; }
    [StructLayout(LayoutKind.Sequential)] private struct Startup { public int Size; public IntPtr Reserved, Desktop, Title; public uint X,Y,XSize,YSize,XChars,YChars,Fill,Flags; public ushort Show, ReservedSize; public IntPtr ReservedBytes,Input,Output,Error; }
    [StructLayout(LayoutKind.Sequential)] private struct StartupEx { public Startup Startup; public IntPtr Attributes; }
    [StructLayout(LayoutKind.Sequential)] private struct ProcessInfo { public IntPtr Process,Thread; public uint ProcessId,ThreadId; }
    [StructLayout(LayoutKind.Sequential)] private struct BasicLimits { public long ProcessTime,JobTime; public uint Flags; public nuint MinimumWorkingSet,MaximumWorkingSet; public uint ActiveProcessLimit; public nuint Affinity; public uint Priority,Scheduling; }
    [StructLayout(LayoutKind.Sequential)] private struct IoCounters { public ulong ReadOperations,WriteOperations,OtherOperations,ReadBytes,WriteBytes,OtherBytes; }
    [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits { public BasicLimits Basic; public IoCounters Io; public nuint ProcessMemory,JobMemory,PeakProcessMemory,PeakJobMemory; }
    [StructLayout(LayoutKind.Sequential)] private struct AccountingInfo { public long UserTime,KernelTime,PeriodUserTime,PeriodKernelTime; public uint Faults,TotalProcesses,ActiveProcesses,TerminatedProcesses; }
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool CreatePipe(out SafeFileHandle read,out SafeFileHandle write,ref SecurityAttributes attributes,uint size);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool SetHandleInformation(SafeFileHandle handle,uint mask,uint flags);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern SafeFileHandle CreateJobObjectW(IntPtr attributes,string? name);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool SetInformationJobObject(SafeFileHandle job,int kind,ref ExtendedLimits info,uint length);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool QueryInformationJobObject(SafeFileHandle job,int kind,out AccountingInfo info,uint length,IntPtr returned);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool AssignProcessToJobObject(SafeFileHandle job,SafeFileHandle process);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool InitializeProcThreadAttributeList(IntPtr list,int count,uint flags,ref nuint size);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool UpdateProcThreadAttribute(IntPtr list,uint flags,nuint attribute,IntPtr value,nuint size,IntPtr previous,IntPtr returned);
    [DllImport("kernel32.dll")] private static extern void DeleteProcThreadAttributeList(IntPtr list);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern bool CreateProcessW(string application,StringBuilder command,IntPtr processAttributes,IntPtr threadAttributes,bool inherit,uint flags,IntPtr environment,string directory,ref StartupEx startup,out ProcessInfo info);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern uint ResumeThread(SafeFileHandle thread);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern uint WaitForSingleObject(SafeFileHandle handle,uint milliseconds);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool TerminateProcess(SafeFileHandle process,uint code);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool TerminateJobObject(SafeFileHandle job,uint code);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool GetExitCodeProcess(SafeFileHandle process,out uint code);
}

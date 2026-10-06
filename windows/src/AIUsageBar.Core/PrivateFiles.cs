using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace AIUsageBar.Core;

/// <summary>Explicit private scratch store, immutable publishing; never a settings editor.</summary>
public sealed class PrivateFiles : IDisposable
{
    private readonly string root;
    private readonly Action? beforeReplace;
    private readonly List<SafeFileHandle> ancestors = [];
    private static readonly SecurityIdentifier User = WindowsIdentity.GetCurrent().User ?? throw new CoreException("missing-user");
    private static readonly SecurityIdentifier SystemUser = new(WellKnownSidType.LocalSystemSid, null);
    public string Root => root;
    public PrivateFiles(string scratchRoot) : this(scratchRoot, null) { }
    internal PrivateFiles(string scratchRoot, Action? beforeReplace)
    {
        this.beforeReplace = beforeReplace;
        if (!Path.IsPathFullyQualified(scratchRoot) || scratchRoot.StartsWith("\\\\", StringComparison.Ordinal)) throw new CoreException("invalid-scratch");
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(scratchRoot));
        var parent = Path.GetDirectoryName(root);
        if (parent is null || !Directory.Exists(parent)) throw new CoreException("invalid-scratch");
        try
        {
            foreach (var path in AncestorPaths(parent)) Pin(path);
            var security = new DirectorySecurity(); security.SetOwner(User); security.SetAccessRuleProtection(true, false);
            foreach (var sid in new[] { User, SystemUser }) security.AddAccessRule(new(sid, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(root).Create(security); Pin(root);
            ValidateSecurity(new DirectoryInfo(root).GetAccessControl());
        }
        catch { Dispose(); throw; }
    }
    private static IEnumerable<string> AncestorPaths(string parent)
    {
        var stack = new Stack<string>(); string? cursor = parent;
        while (cursor is not null) { stack.Push(cursor); cursor = Path.GetDirectoryName(cursor); }
        return stack;
    }
    private void Pin(string path)
    {
        var handle = CreateFileW(path, 0x80, 3, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
        if (handle.IsInvalid) { handle.Dispose(); throw new CoreException("unsafe-directory"); }
        try { var info = Information(handle); if ((info.Attributes & 0x400) != 0 || (info.Attributes & 0x10) == 0) throw new CoreException("unsafe-directory"); ancestors.Add(handle); }
        catch { handle.Dispose(); throw; }
    }
    private string FilePath(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > 128 || name is "." or ".." || name.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '.' and not '-')) throw new CoreException("invalid-name");
        return Path.Combine(root, name);
    }
    private static FileSecurity Security()
    {
        var result = new FileSecurity(); result.SetOwner(User); result.SetAccessRuleProtection(true, false);
        foreach (var sid in new[] { User, SystemUser }) result.AddAccessRule(new(sid, FileSystemRights.FullControl, AccessControlType.Allow)); return result;
    }
    private static void ValidateSecurity(FileSystemSecurity security)
    {
        if (!security.AreAccessRulesProtected || !User.Equals(security.GetOwner(typeof(SecurityIdentifier)))) throw new CoreException("unsafe-owner");
        var allowed = new HashSet<string>();
        foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
        {
            var sid = (SecurityIdentifier)rule.IdentityReference;
            if (rule.AccessControlType != AccessControlType.Allow || !sid.Equals(User) && !sid.Equals(SystemUser)) throw new CoreException("unsafe-acl");
            if ((rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl) allowed.Add(sid.Value);
        }
        if (!allowed.Contains(User.Value) || !allowed.Contains(SystemUser.Value)) throw new CoreException("unsafe-acl");
    }
    public FileStream OpenReader(string name)
    {
        var handle = CreateFileW(FilePath(name), 0x80000000, 7, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
        if (handle.IsInvalid) { var error = Marshal.GetLastWin32Error(); handle.Dispose(); if (error is 2 or 3) throw new FileNotFoundException(); throw new CoreException("read-failure"); }
        var stream = new FileStream(handle, FileAccess.Read);
        try { ValidateFile(stream); return stream; } catch { stream.Dispose(); throw; }
    }
    private static FileInfoByHandle ValidateFile(FileStream stream)
    {
        var info = Information(stream.SafeFileHandle);
        if ((info.Attributes & (0x400 | 0x10)) != 0 || info.Links != 1) throw new CoreException("unsafe-file");
        ValidateSecurity(stream.GetAccessControl()); return info;
    }
    public byte[] Read(string name, int limit = 4096)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            FileStream stream;
            try { stream = OpenReader(name); }
            catch (FileNotFoundException) when (attempt == 0) { Thread.Sleep(5); continue; }
            using var held = stream; var before = ValidateFile(stream);
            if (stream.Length > limit || limit < 0) throw new CoreException("file-limit");
            var bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes);
            var after = Information(stream.SafeFileHandle);
            if (Stable(before, after) && stream.Length == bytes.Length) return bytes;
        }
        throw new CoreException("read-conflict");
    }
    public FileStream AcquireLock()
    {
        var path = FilePath("writer.lock");
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            FileStream stream;
            if (File.Exists(path))
            {
                var handle = CreateFileW(path, 0xC0000000, 0, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
                if (handle.IsInvalid)
                {
                    var error = Marshal.GetLastWin32Error(); handle.Dispose();
                    if (error is 32 or 33 && deadline.Elapsed < TimeSpan.FromSeconds(2)) { Thread.Sleep(10); continue; }
                    throw new CoreException("lock-failure");
                }
                stream = new FileStream(handle, FileAccess.ReadWrite);
            }
            else
            {
                try { stream = new FileInfo(path).Create(FileMode.CreateNew, FileSystemRights.FullControl, FileShare.None, 4096, FileOptions.None, Security()); }
                catch (IOException ex) when ((ex.HResult & 0xFFFF) is 80 or 183 or 32 or 33 && deadline.Elapsed < TimeSpan.FromSeconds(2)) { Thread.Sleep(10); continue; }
            }
            try { ValidateFile(stream); return stream; } catch { stream.Dispose(); throw; }
        }
    }
    public void AtomicWrite(string name, ReadOnlySpan<byte> bytes)
    { using var held = AcquireLock(); WriteLocked(name, bytes); }
    public void WriteLocked(string name, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > 2 * 1024 * 1024) throw new CoreException("file-limit");
        var target = FilePath(name); var temp = FilePath("temp-" + Guid.NewGuid().ToString("N")); var recovery = FilePath("recovery-" + Guid.NewGuid().ToString("N"));
        FileInfoByHandle tempInfo;
        using (var stream = new FileInfo(temp).Create(FileMode.CreateNew, FileSystemRights.FullControl, FileShare.None, 4096, FileOptions.None, Security()))
        { stream.Write(bytes); stream.Flush(true); tempInfo = ValidateFile(stream); }
        // Existing destination is validated before releasing its full-sharing handle. External editors do not honor our lock.
        var exists = File.Exists(target); FileInfoByHandle? oldInfo = null; byte[]? oldBytes = null;
        if (exists)
        {
            using var old = OpenReader(name); oldInfo = ValidateFile(old);
            if (old.Length > 2 * 1024 * 1024) throw new CoreException("file-limit");
            oldBytes = new byte[checked((int)old.Length)]; old.ReadExactly(oldBytes);
            if (!Stable(oldInfo.Value, Information(old.SafeFileHandle))) throw new CoreException("publish-conflict");
        }
        if (exists) beforeReplace?.Invoke();
        var success = exists ? ReplaceFileW(target, temp, recovery, 0, IntPtr.Zero, IntPtr.Zero) : MoveFileExW(temp, target, 0);
        if (!success) throw new CoreException("publish-conflict"); // Keep temp/recovery for explicit reconciliation, no blind retry.
        if (oldInfo is { } original)
        {
            using var displaced = OpenReader(Path.GetFileName(recovery)); var identity = ValidateFile(displaced);
            if (!SameIdentity(original, identity) || displaced.Length != oldBytes!.Length) throw new CoreException("publish-conflict");
            var actual = new byte[oldBytes.Length]; displaced.ReadExactly(actual);
            if (!actual.AsSpan().SequenceEqual(oldBytes)) throw new CoreException("publish-conflict");
        }
        using (var published = OpenReader(name))
        {
            var identity = ValidateFile(published);
            if (identity.FileIndexHigh != tempInfo.FileIndexHigh || identity.FileIndexLow != tempInfo.FileIndexLow || identity.Volume != tempInfo.Volume || published.Length != bytes.Length) throw new CoreException("publish-conflict");
            var actual = new byte[bytes.Length]; published.ReadExactly(actual);
            if (!bytes.SequenceEqual(actual)) throw new CoreException("publish-conflict");
        }
        try { if (File.Exists(recovery)) File.Delete(recovery); } catch (IOException) { /* Safe cleanup-pending evidence retained. */ }
    }
    public void DeleteLocked(string name)
    { if (!File.Exists(FilePath(name))) return; using (var stream = OpenReader(name)) _ = ValidateFile(stream); File.Delete(FilePath(name)); }
    public void Dispose() { foreach (var handle in ancestors) handle.Dispose(); ancestors.Clear(); }
    private static bool SameIdentity(FileInfoByHandle a, FileInfoByHandle b) => a.Volume == b.Volume && a.FileIndexHigh == b.FileIndexHigh && a.FileIndexLow == b.FileIndexLow;
    private static bool Stable(FileInfoByHandle a, FileInfoByHandle b) => SameIdentity(a, b) && a.SizeHigh == b.SizeHigh && a.SizeLow == b.SizeLow && a.Written.dwHighDateTime == b.Written.dwHighDateTime && a.Written.dwLowDateTime == b.Written.dwLowDateTime && a.Links == b.Links && a.Attributes == b.Attributes;
    internal static FileInfoByHandle Information(SafeFileHandle handle)
    { if (!GetFileInformationByHandle(handle, out var info)) throw new CoreException("identity-failure"); return info; }
    [StructLayout(LayoutKind.Sequential)] internal struct FileInfoByHandle
    { public uint Attributes; public System.Runtime.InteropServices.ComTypes.FILETIME Created,Accessed,Written; public uint Volume,SizeHigh,SizeLow,Links,FileIndexHigh,FileIndexLow; }
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern SafeFileHandle CreateFileW(string path,uint access,uint sharing,IntPtr attributes,uint creation,uint flags,IntPtr template);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool GetFileInformationByHandle(SafeFileHandle handle,out FileInfoByHandle info);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern bool ReplaceFileW(string replaced,string replacement,string backup,uint flags,IntPtr excluded,IntPtr reserved);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern bool MoveFileExW(string source,string target,uint flags);
}

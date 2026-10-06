using System.Collections;

namespace AIUsageBar.Core;

public static class NativeCodex
{
    public static ProcessLaunch Launch(string codexExe, IReadOnlyList<string> arguments, string workingDirectory)
    {
        if (!Path.IsPathFullyQualified(codexExe) || !Path.IsPathFullyQualified(workingDirectory)) throw new CoreException("invalid-path");
        var environment = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
            .ToDictionary(x => (string)x.Key, x => (string)x.Value!, StringComparer.OrdinalIgnoreCase);
        return new(codexExe, arguments, workingDirectory, environment, AllowDescendants: true);
    }
    public static CodexProvider Create(string codexExe, string workingDirectory) => new(
        (arguments, _) => Task.FromResult<ICodexTransport>(new JsonRpcTransport(WindowsProcess.Start(Launch(codexExe, arguments, workingDirectory)))),
        Path.Combine(Environment.SystemDirectory, "where.exe"));
    public static string? FindExecutable() => FindExecutable(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm"));
    internal static string? FindExecutable(string npmRoot) => CodexDiscovery.Discover(npmRoot).FirstOrDefault()?.Path;
    public static string WorkingDirectory()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AIUsageBar", "codex-cwd");
        Directory.CreateDirectory(path); return path;
    }
}

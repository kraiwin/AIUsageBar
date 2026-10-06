namespace AIUsageBar.Tray;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string? smokeRoot = null;
        if (args.Length > 0)
        {
            if (args.Length != 2 || args[0] != "--smoke" || !Path.IsPathFullyQualified(args[1]) || !Directory.Exists(args[1]))
                return 2;
            smokeRoot = Path.GetFullPath(args[1]);
        }

        // Local namespace avoids requiring privileges or changing other sessions.
        using var singleInstance = new Mutex(true, @"Local\AIUsageBar.Windows", out bool firstInstance);
        if (!firstInstance)
            return 3;
        try
        {
            ApplicationConfiguration.Initialize();
            using var context = new TrayContext(smokeRoot);
            Application.Run(context);
            return context.ExitCode;
        }
        finally
        {
            singleInstance.ReleaseMutex();
        }
    }
}

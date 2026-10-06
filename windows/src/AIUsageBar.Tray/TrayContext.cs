using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using AIUsageBar.Core;

namespace AIUsageBar.Tray;

internal sealed class TrayContext : ApplicationContext
{
    private readonly Form dispatcher = new() { ShowInTaskbar = false };
    private readonly ContextMenuStrip menu = new() { AccessibleName = "AIUsageBar เมนูสถานะ" };
    private readonly NotifyIcon notifyIcon;
    private readonly Icon icon;
    private readonly TaskbarMessageWindow taskbarWindow;
    private readonly ToolStripMenuItem refreshItem;
    private readonly ToolStripMenuItem installItem, restoreItem, claudeAgeItem, codexErrorItem, claudeErrorItem;
    private readonly System.Windows.Forms.Timer pollingTimer = new() { Interval = (int)RefreshPolicy.PollingInterval.TotalMilliseconds };
    private readonly string root = AppPaths.Root;
    private readonly ClaudeBridgeInstaller installer = new();
    private string? codexError, discoveryError;
    private bool codexMissing;
    private SnapshotResult snapshot = new(null, false, null);
    private readonly ToolStripMenuItem codexItem;
    private readonly ToolStripMenuItem claudeItem;
    private readonly System.Windows.Forms.Timer smokeTimer = new() { Interval = 100 };
    private readonly System.Windows.Forms.Timer smokeDeadline = new() { Interval = 5000 };
    private readonly string? smokeRoot;
    private readonly RefreshCoordinator codex;
    private int generation;
    private bool closing;
    private bool refreshing;
    private int smokeStep;
    private int refreshCount;
    private int reAddCount;
    private bool menuOpened;
    private bool uiThreadOnly = true;
    private readonly int uiThread = Environment.CurrentManagedThreadId;
    internal int ExitCode { get; private set; }

    internal TrayContext(string? smokeRoot)
    {
        this.smokeRoot = smokeRoot;
        _ = dispatcher.Handle; // Hidden UI-thread handle for safe async completions.
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AIUsageBar.Tray.Assets.AIUsageBar.ico")
            ?? throw new InvalidOperationException("Missing application icon.");
        icon = new Icon(stream);
        notifyIcon = new NotifyIcon { Icon = icon, ContextMenuStrip = menu, Text = UsageText.Tooltip(null, null) };
        menu.Items.Add(new ToolStripMenuItem("AIUsageBar") { Enabled = false });
        codexItem = AddStatus(UsageText.MenuLine("Codex", null, TimeZoneInfo.Local));
        codexErrorItem = AddStatus(""); codexErrorItem.Visible = false;
        claudeItem = AddStatus(UsageText.MenuLine("Claude", null, TimeZoneInfo.Local));
        claudeAgeItem = AddStatus(UsageText.ClaudeAge(null, DateTimeOffset.UtcNow, TimeZoneInfo.Local));
        claudeErrorItem = AddStatus(""); claudeErrorItem.Visible = false;
        menu.Items.Add(new ToolStripSeparator());
        installItem = new ToolStripMenuItem("เชื่อม Claude Code…"); installItem.Click += InstallClaude;
        restoreItem = new ToolStripMenuItem("ยกเลิกการเชื่อม Claude Code"); restoreItem.Click += RestoreClaude;
        menu.Items.Add(installItem); menu.Items.Add(restoreItem);
        refreshItem = new ToolStripMenuItem("รีเฟรชตอนนี้");
        refreshItem.Click += async (_, _) => await RefreshAsync();
        menu.Items.Add(refreshItem);
        CodexProvider? provider = null;
        if (smokeRoot is null)
        {
            Directory.CreateDirectory(root);
            try
            {
                var exe = NativeCodex.FindExecutable(); codexMissing = exe is null;
                if (exe is not null) provider = NativeCodex.Create(exe, NativeCodex.WorkingDirectory());
            }
            catch (Exception ex) { discoveryError = Category(ex); }
        }
        codex = new(async token =>
        {
            if (provider is null) return null;
            try { var reading = await provider.FetchAsync(DateTimeOffset.UtcNow, token); codexError = null; return reading; }
            catch (Exception ex) { codexError = Category(ex); throw; }
        }, _ => { });
        menu.Opening += (_, _) => { if (smokeRoot is null) ReadClaude(); UpdateMenu(); };
        pollingTimer.Tick += async (_, _) => await RefreshAsync();
        menu.Items.Add(new ToolStripSeparator());
        var quit = new ToolStripMenuItem("ออก");
        quit.Click += (_, _) => ExitThread();
        menu.Items.Add(quit);
        menu.Opened += (_, _) => { AssertUiThread(); menuOpened = true; };
        notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                menu.Show(Cursor.Position);
        };
        taskbarWindow = new TaskbarMessageWindow(() =>
        {
            AssertUiThread();
            if (closing) return;
            // Re-register just this icon; never restart Explorer or duplicate a process.
            notifyIcon.Visible = false;
            notifyIcon.Visible = true;
            reAddCount++;
        });
        notifyIcon.Visible = true;
        if (smokeRoot is not null)
        {
            installItem.Enabled = false; restoreItem.Enabled = false;
            smokeTimer.Tick += SmokeTick;
            smokeDeadline.Tick += (_, _) => { ExitCode = 1; ExitThread(); };
            smokeDeadline.Start();
            smokeTimer.Start();
        }
        else
        {
            ReadClaude(); UpdateMenu(); pollingTimer.Start();
            dispatcher.BeginInvoke(async () => await RefreshAsync());
        }
    }

    private ToolStripMenuItem AddStatus(string text)
    {
        var item = new ToolStripMenuItem(text) { Enabled = false, AccessibleName = text };
        menu.Items.Add(item);
        return item;
    }

    private void AssertUiThread()
    {
        if (Environment.CurrentManagedThreadId != uiThread)
        {
            uiThreadOnly = false;
            throw new InvalidOperationException("Tray state requires the UI thread.");
        }
    }

    private async Task RefreshAsync()
    {
        AssertUiThread();
        if (closing || refreshing) return;
        refreshing = true;
        refreshItem.Enabled = false;
        int operation = ++generation;
        try
        {
            if (smokeRoot is null) ReadClaude();
            await codex.RefreshAsync(DateTimeOffset.UtcNow);
            if (closing || generation != operation) return;
            AssertUiThread();
            UpdateMenu();
            refreshCount++;
        }
        catch (OperationCanceledException)
        {
            // Shutdown invalidates this operation; keep the unavailable status.
        }
        finally
        {
            if (!closing && generation == operation)
            {
                refreshing = false;
                refreshItem.Enabled = true;
            }
        }
    }

    private static string Category(Exception ex) => ex is CoreException core ? core.Category : ex.GetType().Name;
    private void ReadClaude()
    {
        try
        {
            Directory.CreateDirectory(root); using var store = new ClaudeSnapshotStore(Path.Combine(root, "claude"));
            snapshot = store.Read(DateTimeOffset.UtcNow);
            claudeErrorItem.Text = snapshot.CaptureError ? "Claude: รับข้อมูลไม่สำเร็จ · แสดงข้อมูลล่าสุดที่บันทึกไว้" : "";
            claudeErrorItem.Visible = snapshot.CaptureError;
        }
        catch (Exception ex)
        {
            snapshot = new(null, false, null); claudeErrorItem.Text = "Claude: อ่านข้อมูลไม่สำเร็จ (" + Category(ex) + ")"; claudeErrorItem.Visible = true;
        }
    }
    private void UpdateMenu()
    {
        AssertUiThread();
        codexItem.Text = codexMissing ? "Codex: ไม่พบ codex CLI" : UsageText.MenuLine("Codex", codex.Reading, TimeZoneInfo.Local);
        var error = codexError ?? discoveryError;
        codexErrorItem.Visible = error is not null;
        codexErrorItem.Text = error is null ? "" : "Codex: ดึงข้อมูลไม่สำเร็จ (" + error + ")";
        if (error is not null && codex.Reading is { } previous) codexItem.Text += " · ข้อมูลล่าสุด " + previous.ReceivedAt.ToLocalTime().ToString("HH:mm");
        claudeItem.Text = UsageText.MenuLine("Claude", snapshot.Reading, TimeZoneInfo.Local);
        claudeAgeItem.Text = UsageText.ClaudeAge(snapshot.ReceivedAt, DateTimeOffset.UtcNow, TimeZoneInfo.Local);
        notifyIcon.Text = UsageText.Tooltip(error is null ? codex.Reading : null, snapshot.CaptureError ? null : snapshot.Reading);
        if (smokeRoot is not null) return;
        try
        {
            var state = installer.State(ClaudeBridgeInstaller.SettingsPath(), root);
            installItem.Enabled = state != BridgeState.Installed; restoreItem.Enabled = state == BridgeState.Installed;
        }
        catch (Exception ex)
        {
            installItem.Enabled = true; restoreItem.Enabled = false;
            claudeErrorItem.Text = "Claude: ตรวจการเชื่อมไม่สำเร็จ (" + Category(ex) + ")"; claudeErrorItem.Visible = true;
        }
    }
    private void InstallClaude(object? sender, EventArgs e)
    {
        if (smokeRoot is not null) return;
        try
        {
            var settings = ClaudeBridgeInstaller.SettingsPath(); var source = AppContext.BaseDirectory;
            var preview = installer.Preview(settings, root, source);
            if (MessageBox.Show("คำสั่งเดิม:\n" + (preview.OriginalCommand ?? "ไม่มี") + "\n\nคำสั่งใหม่:\n" + preview.InstalledCommand + "\n\nยืนยันการเชื่อม Claude Code?", "เชื่อม Claude Code", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            installer.Install(settings, root, source); ReadClaude(); UpdateMenu();
        }
        catch (Exception ex) { MessageBox.Show("เชื่อมไม่สำเร็จ (" + Category(ex) + ")", "Claude Code", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void RestoreClaude(object? sender, EventArgs e)
    {
        if (smokeRoot is not null) return;
        try
        {
            var result = installer.Restore(ClaudeBridgeInstaller.SettingsPath(), root);
            MessageBox.Show(result == RestoreResult.AlreadyDetached ? "statusLine ถูกแก้ไปแล้ว จึงไม่แตะ settings และล้างข้อมูลการเชื่อมแล้ว" : result == RestoreResult.Restored ? "คืนค่า statusLine เดิมแล้ว" : "ยังไม่ได้เชื่อม Claude Code", "Claude Code", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReadClaude(); UpdateMenu();
        }
        catch (Exception ex) { MessageBox.Show("ยกเลิกการเชื่อมไม่สำเร็จ (" + Category(ex) + ")", "Claude Code", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async void SmokeTick(object? sender, EventArgs e)
    {
        // Bounded smoke only touches our hidden window/icon and explicitly supplied scratch.
        smokeTimer.Stop();
        try
        {
            switch (smokeStep++)
            {
                case 0:
                    menu.Show(new Point(20, 20));
                    menu.Close();
                    await RefreshAsync();
                    smokeTimer.Start();
                    break;
                case 1:
                    // Send only to our own HWND, exercising the same handler as Explorer.
                    taskbarWindow.SimulateTaskbarCreated();
                    smokeTimer.Start();
                    break;
                default:
                    bool passed = menuOpened && refreshCount == 1 && reAddCount == 1 && uiThreadOnly && notifyIcon.Visible;
                    ExitCode = passed ? 0 : 1;
                    bool iconVisibleBeforeExit = notifyIcon.Visible;
                    ExitThread();
                    File.WriteAllText(Path.Combine(smokeRoot!, "tray-smoke.json"), JsonSerializer.Serialize(new
                    {
                        schemaVersion = 1, passed, menuOpened, refreshCount, reAddCount, uiThreadOnly,
                        iconVisibleBeforeExit,
                        iconHiddenOnExit = !notifyIcon.Visible,
                        taskbarMessage = "own-window simulation; Explorer was not restarted",
                        nativeProviders = "disabled", quotaData = "unavailable", frameworkOnly = true
                    }));
                    break;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ExitCode = 1;
            ExitThread();
        }
    }

    protected override async void ExitThreadCore()
    {
        if (closing) return;
        AssertUiThread();
        closing = true;
        generation++;
        smokeTimer.Stop();
        smokeDeadline.Stop();
        codex.Disconnect();
        pollingTimer.Stop();
        notifyIcon.Visible = false;
        try { await codex.ShutdownAsync(); } catch (CoreException) { ExitCode = 1; }
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            smokeTimer.Dispose();
            smokeDeadline.Dispose();
            codex.Dispose();
            pollingTimer.Dispose();
            taskbarWindow.Dispose();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            menu.Dispose();
            icon.Dispose();
            dispatcher.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class TaskbarMessageWindow : NativeWindow, IDisposable
{
    private readonly uint taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    private readonly Action reAddIcon;

    internal TaskbarMessageWindow(Action reAddIcon)
    {
        this.reAddIcon = reAddIcon;
        // Hidden top-level HWND receives broadcasts (a message-only HWND would not).
        CreateHandle(new CreateParams { Caption = "AIUsageBar.Tray.Messages" });
    }

    protected override void WndProc(ref Message m)
    {
        if (taskbarCreated != 0 && (uint)m.Msg == taskbarCreated)
            reAddIcon();
        base.WndProc(ref m);
    }

    internal void SimulateTaskbarCreated() => SendMessage(Handle, taskbarCreated, 0, 0);
    public void Dispose() => DestroyHandle();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegisterWindowMessageW")]
    private static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint hwnd, uint message, nint wParam, nint lParam);
}

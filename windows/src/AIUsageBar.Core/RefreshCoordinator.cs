namespace AIUsageBar.Core;

public sealed class RefreshPolicy(DateTimeOffset? lastAttempt = null)
{
    public static TimeSpan MinimumInterval => TimeSpan.FromSeconds(60);
    public static TimeSpan PollingInterval => TimeSpan.FromSeconds(300);
    public DateTimeOffset? LastAttempt { get; private set; } = lastAttempt;
    public TimeSpan Remaining(DateTimeOffset now)
    {
        if (LastAttempt is not { } last) return TimeSpan.Zero;
        if (now < last) { LastAttempt = now; return MinimumInterval; }
        var remaining = MinimumInterval - (now - last);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
    public bool Begin(DateTimeOffset now)
    { if (Remaining(now) > TimeSpan.Zero) return false; LastAttempt = now; return true; }
}

public sealed class RefreshCoordinator : IDisposable
{
    private readonly Func<CancellationToken, Task<UsageReading?>> fetch;
    private readonly Action<DateTimeOffset> persistAttempt;
    private readonly object sync = new();
    private CancellationTokenSource? operation;
    private TaskCompletionSource? completion;
    private int generation;
    private bool disconnected, disposed;
    public RefreshPolicy Policy { get; }
    public UsageReading? Reading { get; private set; }
    public string Status { get; private set; } = "unavailable";
    public RefreshCoordinator(Func<CancellationToken, Task<UsageReading?>> fetch, Action<DateTimeOffset> persistAttempt, DateTimeOffset? lastAttempt = null)
    { this.fetch = fetch; this.persistAttempt = persistAttempt; Policy = new(lastAttempt); }
    public async Task<bool> RefreshAsync(DateTimeOffset now)
    {
        CancellationTokenSource current; int epoch;
        lock (sync)
        {
            if (disposed || disconnected || operation is not null) return false;
            var old = Policy.LastAttempt;
            if (!Policy.Begin(now)) { if (old != Policy.LastAttempt) persistAttempt(Policy.LastAttempt!.Value); return false; }
            // Persistence failure prevents launch. The in-memory attempt remains throttled.
            persistAttempt(now); current = new(); operation = current; completion = new(TaskCreationOptions.RunContinuationsAsynchronously); epoch = generation; Status = "loading";
        }
        try
        {
            var result = await fetch(current.Token).ConfigureAwait(false);
            lock (sync) if (!disposed && !disconnected && generation == epoch)
            { Reading = result; Status = result is null ? "no-data" : "ready"; }
            return true;
        }
        catch (OperationCanceledException) { lock (sync) if (generation == epoch) Status = "cancelled"; return false; }
        catch { lock (sync) if (generation == epoch) Status = "failed"; return false; }
        finally { lock (sync) { if (ReferenceEquals(operation, current)) { operation = null; completion?.TrySetResult(); completion = null; } current.Dispose(); } }
    }
    public void Disconnect()
    { lock (sync) { disconnected = true; generation++; operation?.Cancel(); Reading = null; Status = "disconnected"; } }
    public void Dispose()
    { lock (sync) { if (disposed) return; disposed = true; generation++; operation?.Cancel(); Reading = null; Status = "stopped"; } }
    public async Task ShutdownAsync()
    {
        Task? pending;
        lock (sync) { pending = completion?.Task; Dispose(); }
        if (pending is not null)
        { try { await pending.WaitAsync(TimeSpan.FromSeconds(4)).ConfigureAwait(false); } catch (TimeoutException) { throw new CoreException("cleanup-failure"); } }
    }
}

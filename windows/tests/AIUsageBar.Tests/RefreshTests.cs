using AIUsageBar.Core;

namespace AIUsageBar.Tests;

internal static class RefreshTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return TestCase.Sync("state/attempt-persists-across-disconnect-and-relaunch", () =>
        {
            using var scratch = new Scratch(); var path = Path.Combine(scratch.Root, "private");
            using (var state = new LocalState(path)) state.Write(new LocalStateData(null, Fixture.Now));
            using var reopened = new LocalState(path); var data = reopened.Read();
            Check.True(data.Selected is null); Check.Equal(Fixture.Now, data.LastAttempt);
            Check.True(!new RefreshPolicy(data.LastAttempt).Begin(Fixture.Now.AddSeconds(10)));
        });
        yield return TestCase.Sync("state/corrupt-future-throttle-fails-closed", () =>
        {
            using var scratch = new Scratch(); var path = Path.Combine(scratch.Root, "private");
            using var state = new LocalState(path); using var files = new PrivateFiles(path);
            files.AtomicWrite("local-state.json", "{\"schemaVersion\":1,\"lastAttempt\":\"bad\",\"selected\":null}"u8);
            Check.Throws<CoreException>(() => state.Read());
            state.Write(new(null, Fixture.Now.AddHours(1)));
            var policy = new RefreshPolicy(state.Read().LastAttempt);
            Check.True(!policy.Begin(Fixture.Now)); Check.Equal(TimeSpan.FromSeconds(60), policy.Remaining(Fixture.Now));
        });
        yield return TestCase.Sync("state/oversized-tuple-rejected-before-persistence", () =>
        {
            using var scratch = new Scratch(); var path = Path.Combine(scratch.Root, "private");
            using var state = new LocalState(path); state.Write(new(null, Fixture.Now));
            var tuple = new ExecutableTuple("C:\\" + new string('x', 20_000), "synthetic", "id", 1, new string('A', 64));
            Check.Throws<CoreException>(() => state.Write(new(tuple, Fixture.Now.AddSeconds(1))));
            Check.Equal(Fixture.Now, state.Read().LastAttempt);
        });
        yield return new("refresh/shutdown-joins-cancelled-fetch-cleanup", async () =>
        {
            bool cleaned = false;
            using var coordinator = new RefreshCoordinator(async token =>
            {
                try { await Task.Delay(30_000, token); return null; }
                finally { await Task.Delay(50); cleaned = true; }
            }, _ => { });
            var pending = coordinator.RefreshAsync(Fixture.Now);
            await coordinator.ShutdownAsync();
            Check.True(cleaned && pending.IsCompleted); Check.Equal("stopped", coordinator.Status);
        });
        yield return TestCase.Sync("refresh/60s-cooldown-300s-poll", () =>
        {
            var policy = new RefreshPolicy();
            Check.True(policy.Begin(Fixture.Now));
            Check.Equal(TimeSpan.FromSeconds(60), RefreshPolicy.MinimumInterval);
            Check.Equal(TimeSpan.FromSeconds(300), RefreshPolicy.PollingInterval);
            Check.True(!policy.Begin(Fixture.Now.AddSeconds(59.999)));
            Check.True(policy.Begin(Fixture.Now.AddSeconds(60)));
        });
        yield return TestCase.Sync("refresh/relaunch-does-not-reset-cooldown", () =>
        {
            var policy = new RefreshPolicy(Fixture.Now);
            Check.Equal(TimeSpan.FromSeconds(45), policy.Remaining(Fixture.Now.AddSeconds(15)));
            Check.True(!policy.Begin(Fixture.Now.AddSeconds(59)));
        });
        yield return TestCase.Sync("refresh/backward-clock-starts-new-cooldown", () =>
        {
            var policy = new RefreshPolicy(Fixture.Now);
            var earlier = Fixture.Now.AddHours(-1);
            Check.True(!policy.Begin(earlier));
            Check.Equal(earlier, policy.LastAttempt);
            Check.True(!policy.Begin(earlier.AddSeconds(59)));
            Check.True(policy.Begin(earlier.AddSeconds(60)));
        });
        yield return new("refresh/persist-before-fetch-and-single-inflight", async () =>
        {
            int persisted = 0, fetched = 0;
            var completion = new TaskCompletionSource<UsageReading?>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var coordinator = new RefreshCoordinator(_ =>
            {
                Check.Equal(1, persisted); fetched++;
                return completion.Task;
            }, _ => persisted++);
            var pending = coordinator.RefreshAsync(Fixture.Now);
            Check.True(!await coordinator.RefreshAsync(Fixture.Now.AddMinutes(2)));
            completion.SetResult(UsagePayloadParser.ParseCodex(Fixture.Codex(), Fixture.Now));
            Check.True(await pending);
            Check.Equal(1, fetched); Check.Equal("ready", coordinator.Status);
        });
        yield return new("refresh/persistence-failure-never-launches", async () =>
        {
            int fetched = 0;
            using var coordinator = new RefreshCoordinator(_ => { fetched++; return Task.FromResult<UsageReading?>(null); }, _ => throw new IOException());
            await Check.ThrowsAsync<IOException>(async () => { await coordinator.RefreshAsync(Fixture.Now); });
            Check.True(!await coordinator.RefreshAsync(Fixture.Now.AddSeconds(30)));
            Check.Equal(0, fetched);
        });
        foreach (var stop in new[] { "disconnect", "quit" })
        {
            var mode = stop;
            yield return new($"refresh/{mode}-cancels-ignores-late-result", async () =>
            {
                var completion = new TaskCompletionSource<UsageReading?>(TaskCreationOptions.RunContinuationsAsynchronously);
                CancellationToken active = default;
                using var coordinator = new RefreshCoordinator(token => { active = token; return completion.Task; }, _ => { });
                var pending = coordinator.RefreshAsync(Fixture.Now);
                if (mode == "disconnect") coordinator.Disconnect(); else coordinator.Dispose();
                Check.True(active.IsCancellationRequested);
                completion.SetResult(UsagePayloadParser.ParseCodex(Fixture.Codex(), Fixture.Now));
                await pending;
                Check.True(coordinator.Reading is null);
                Check.Equal(mode == "disconnect" ? "disconnected" : "stopped", coordinator.Status);
                Check.True(!await coordinator.RefreshAsync(Fixture.Now.AddMinutes(2)));
                Check.Equal(Fixture.Now, coordinator.Policy.LastAttempt);
            });
        }
        yield return new("refresh/error-retains-last-reading-and-receipt", async () =>
        {
            bool fail = false;
            using var coordinator = new RefreshCoordinator(_ => fail ? Task.FromException<UsageReading?>(new IOException()) :
                Task.FromResult(UsagePayloadParser.ParseCodex(Fixture.Codex(), Fixture.Now)), _ => { });
            Check.True(await coordinator.RefreshAsync(Fixture.Now));
            fail = true;
            Check.True(!await coordinator.RefreshAsync(Fixture.Now.AddMinutes(2)));
            Check.Equal("failed", coordinator.Status);
            Check.Equal(Fixture.Now, coordinator.Reading!.ReceivedAt);
        });
        yield return new("refresh/valid-no-data-clears-old-reading", async () =>
        {
            bool empty = false;
            using var coordinator = new RefreshCoordinator(_ => Task.FromResult(empty ? null : UsagePayloadParser.ParseCodex(Fixture.Codex(), Fixture.Now)), _ => { });
            await coordinator.RefreshAsync(Fixture.Now); empty = true;
            await coordinator.RefreshAsync(Fixture.Now.AddMinutes(2));
            Check.Equal("no-data", coordinator.Status); Check.True(coordinator.Reading is null);
        });
    }
}

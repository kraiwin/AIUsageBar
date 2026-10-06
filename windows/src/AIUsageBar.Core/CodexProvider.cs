using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace AIUsageBar.Core;

public interface ICodexTransport : IAsyncDisposable
{
    Task<JsonElement> RequestAsync(string method, JsonElement parameters, CancellationToken cancellationToken);
    Task NotifyInitializedAsync(CancellationToken cancellationToken);
    Task FinishAsync(CancellationToken cancellationToken);
}
/// <summary>Two-stage quota reader with explicit transport and configuration guards.</summary>
public sealed class CodexProvider(Func<IReadOnlyList<string>, CancellationToken, Task<ICodexTransport>> factory, string inertPath)
{
    private static JsonElement Parameters(object value) => JsonSerializer.SerializeToElement(value);
    private static async Task Guards(ICodexTransport transport, CancellationToken token)
    {
        var initialized = await transport.RequestAsync("initialize", Parameters(new { clientInfo = new { name = "aiusagebar", title = "AIUsageBar", version = "0.1.0" } }), token);
        if (!initialized.TryGetProperty("userAgent", out var agent) || agent.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(agent.GetString())) throw new CoreException("invalid-handshake");
        await transport.NotifyInitializedAsync(token);
        var features = new List<CodexFeature>(); var cursors = new HashSet<string>(StringComparer.Ordinal); string? cursor = null;
        for (var page = 0; page < 32; page++)
        {
            var response = await transport.RequestAsync("experimentalFeature/list", Parameters(new { limit = 100, cursor }), token);
            var parsed = CodexPolicy.RegistryPage(response); features.AddRange(parsed.Features);
            if (features.Count > 2048) throw new CoreException("unsupported-configuration");
            if (parsed.Cursor is null) { CodexPolicy.AssertRegistry(features); return; }
            if (!cursors.Add(parsed.Cursor)) throw new CoreException("unsupported-configuration"); cursor = parsed.Cursor;
        }
        throw new CoreException("unsupported-configuration");
    }
    public async Task<UsageReading?> FetchAsync(DateTimeOffset receipt, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); deadline.CancelAfter(TimeSpan.FromSeconds(30)); var token = deadline.Token;
        IReadOnlyList<McpServer> inventory;
        await using (var first = await factory(CodexPolicy.Arguments(inertPath), token))
        { await Guards(first, token); inventory = CodexPolicy.Inventory(await first.RequestAsync("config/read", Parameters(new { includeLayers = false }), token), false); await first.FinishAsync(token); }
        // First transport must dispose successfully before a second child can be created.
        await using var second = await factory(CodexPolicy.Arguments(inertPath, inventory), token);
        await Guards(second, token);
        CodexPolicy.AssertSameInventory(inventory, CodexPolicy.Inventory(await second.RequestAsync("config/read", Parameters(new { includeLayers = false }), token), true));
        var quota = await second.RequestAsync("account/rateLimits/read", Parameters(new { }), token);
        if (!quota.TryGetProperty("rateLimits", out _) && !quota.TryGetProperty("rateLimitsByLimitId", out _)) throw new CoreException("invalid-quota");
        // Receipt time is local; no provider observation timestamp is inferred.
        var reading = UsagePayloadParser.ParseCodex(JsonSerializer.SerializeToUtf8Bytes(quota), receipt);
        await second.FinishAsync(token); return reading;
    }
}

/// <summary>Bounded strict JSONL channel over an explicitly supplied console child.</summary>
public sealed class JsonRpcTransport : ICodexTransport
{
    private readonly WindowsProcess process;
    private readonly bool inventoryOnly;
    private sealed record Frame(byte[] Bytes, int RequestId);
    private readonly Channel<Frame> lines = Channel.CreateBounded<Frame>(new BoundedChannelOptions(64) { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
    private readonly CancellationTokenSource stopping = new();
    private readonly Task outputDrain, errorDrain;
    private readonly TaskCompletionSource streamFailed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int nextId = 1, activeRequestId; private bool disposed, requestPending;
    private volatile Exception? streamFailure;
    public JsonRpcTransport(WindowsProcess process) : this(process, inventoryOnly: false) { }
    internal JsonRpcTransport(WindowsProcess process, bool inventoryOnly)
    { this.process = process; this.inventoryOnly = inventoryOnly; outputDrain = Task.Run(() => DrainOutput(stopping.Token)); errorDrain = Task.Run(() => DrainError(stopping.Token)); }
    private async Task DrainError(CancellationToken token)
    {
        try
        {
            var buffer = new byte[16384]; var total = 0; var countLines = 0; bool partialLine = false;
            while (true)
            {
                var count = await process.Error.ReadAsync(buffer, token);
                if (count == 0)
                { if (partialLine && ++countLines > 4096) throw new CoreException("output-limit"); return; }
                total += count; if (total > 16 * 1024 * 1024) throw new CoreException("output-limit");
                for (var i = 0; i < count; i++)
                {
                    if (buffer[i] == 10)
                    { if (++countLines > 4096) throw new CoreException("output-limit"); partialLine = false; }
                    else partialLine = true;
                }
            }
        }
        catch (Exception ex) { streamFailure = ex; streamFailed.TrySetResult(); lines.Writer.TryComplete(new CoreException("io-failure")); }
    }
    private async Task DrainOutput(CancellationToken token)
    {
        try
        {
            var buffer = new byte[16384]; using var line = new MemoryStream(); var total = 0; var countLines = 0; var frameRequestId = 0;
            while (true)
            {
                var count = await process.Output.ReadAsync(buffer, token); if (count == 0) break;
                var chunkRequestId = Volatile.Read(ref activeRequestId);
                total += count; if (total > 16 * 1024 * 1024) throw new CoreException("output-limit");
                for (var i = 0; i < count; i++)
                {
                    if (buffer[i] == 10)
                    {
                        if (++countLines > 4096) throw new CoreException("output-limit");
                        var bytes = line.ToArray(); line.SetLength(0);
                        if (!lines.Writer.TryWrite(new(bytes, frameRequestId))) throw new CoreException("output-limit");
                    }
                    else { if (line.Length == 0) frameRequestId = chunkRequestId; if (line.Length >= 2 * 1024 * 1024) throw new CoreException("output-limit"); line.WriteByte(buffer[i]); }
                }
            }
            if (line.Length != 0)
            { if (++countLines > 4096 || !lines.Writer.TryWrite(new(line.ToArray(), frameRequestId))) throw new CoreException("output-limit"); }
            lines.Writer.TryComplete();
        }
        catch (Exception ex) { streamFailure = ex; streamFailed.TrySetResult(); lines.Writer.TryComplete(new CoreException("io-failure")); }
    }
    private static JsonElement? Envelope(ReadOnlySpan<byte> bytes, int? expected)
    {
        using var doc = UsagePayloadParser.Parse(bytes); var root = doc.RootElement;
        if (root.TryGetProperty("method", out var method))
        {
            if (method.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(method.GetString()) || root.TryGetProperty("result", out _) || root.TryGetProperty("error", out _)) throw new CoreException("invalid-envelope");
            if (root.TryGetProperty("id", out _) || method.GetString() == "account/chatgptAuthTokens/refresh") throw new CoreException("server-request");
            if (root.TryGetProperty("params", out var parameters) && parameters.ValueKind != JsonValueKind.Object) throw new CoreException("invalid-envelope"); return null;
        }
        if (expected is null || !root.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number || !id.TryGetInt32(out var integer) || integer != expected) throw new CoreException("unexpected-response");
        var result = root.TryGetProperty("result", out var value); var error = root.TryGetProperty("error", out var failure);
        if (result == error || root.TryGetProperty("params", out _)) throw new CoreException("invalid-envelope");
        if (error)
        {
            if (failure.ValueKind != JsonValueKind.Object || !failure.TryGetProperty("code", out var code) || !code.TryGetInt32(out _) || !failure.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.String) throw new CoreException("invalid-envelope");
            throw new CoreException("server-error");
        }
        if (value.ValueKind != JsonValueKind.Object) throw new CoreException("invalid-envelope"); return value.Clone();
    }
    public async Task<JsonElement> RequestAsync(string method, JsonElement parameters, CancellationToken cancellationToken)
    {
        var allowed = inventoryOnly
            ? method is "initialize" or "experimentalFeature/list" or "config/read" or "configRequirements/read"
            : method is "initialize" or "experimentalFeature/list" or "config/read" or "account/rateLimits/read";
        if (disposed || requestPending || !allowed) throw new CoreException("invalid-method");
        requestPending = true;
        try
        {
            while (lines.Reader.TryRead(out var pending)) _ = Envelope(pending.Bytes, null);
            if (streamFailure is not null) throw new CoreException("io-failure");
            var id = nextId++; Volatile.Write(ref activeRequestId, id); await Write(new { id, method, @params = parameters }, cancellationToken);
            while (await lines.Reader.WaitToReadAsync(cancellationToken))
            {
                if (streamFailure is not null) throw new CoreException("io-failure");
                if (!lines.Reader.TryRead(out var frame)) continue; var bytes = frame.Bytes;
                if (bytes.Length > (method == "account/rateLimits/read" ? 65536 : 2 * 1024 * 1024)) throw new CoreException("output-limit");
                if (Envelope(bytes, frame.RequestId == id ? id : null) is { } result) return result;
            }
            throw new CoreException("unexpected-eof");
        }
        finally { Volatile.Write(ref activeRequestId, 0); requestPending = false; }
    }
    private async Task Write(object value, CancellationToken token)
    { var bytes = JsonSerializer.SerializeToUtf8Bytes(value); await process.Input.WriteAsync(bytes, token); await process.Input.WriteAsync(new byte[] { 10 }, token); await process.Input.FlushAsync(token); }
    public Task NotifyInitializedAsync(CancellationToken token) => Write(new { method = "initialized", @params = new { } }, token);
    public async Task FinishAsync(CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); deadline.CancelAfter(TimeSpan.FromSeconds(2));
        process.CloseInput(); var exitTask = process.WaitForExitAsync(deadline.Token);
        try
        {
            if (await Task.WhenAny(exitTask, streamFailed.Task) == streamFailed.Task) throw new CoreException("io-failure");
            var exit = await exitTask;
            try { await Task.WhenAll(outputDrain, errorDrain).WaitAsync(deadline.Token); }
            catch (OperationCanceledException) when (process.AllowDescendants && !cancellationToken.IsCancellationRequested) { /* Disposal closes inherited descendant pipes. */ }
            if (streamFailure is not null) throw new CoreException("io-failure");
            while (lines.Reader.TryRead(out var pending)) _ = Envelope(pending.Bytes, null);
            if (exit != 0 || (!process.AllowDescendants && (process.ActiveProcesses != 0 || process.TotalProcesses != 1 || process.TerminatedProcesses != 0))) throw new CoreException("cleanup-failure");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new CoreException("cleanup-failure"); }
        finally
        {
            deadline.Cancel();
            try { await exitTask; } catch (Exception ex) when (ex is OperationCanceledException or CoreException) { /* Original safe failure is reported; provider disposal owns termination. */ }
        }
    }
    public async ValueTask DisposeAsync()
    {
        if (disposed) return; disposed = true;
        try { await process.DisposeAsync(); }
        finally
        {
            stopping.Cancel();
            try { await Task.WhenAll(outputDrain, errorDrain).WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (TimeoutException) { throw new CoreException("cleanup-failure"); }
            finally { stopping.Dispose(); }
        }
    }
}

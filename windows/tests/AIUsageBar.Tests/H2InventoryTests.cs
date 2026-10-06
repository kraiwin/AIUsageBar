using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AIUsageBar.Core;
using Microsoft.Win32.SafeHandles;

namespace AIUsageBar.Tests;

// Preparation only: every executable below is the test assembly's fake child.
internal static class H2InventoryTests
{
    private static readonly string[] Guards = ["hooks", "plugins", "code_mode_host", "background_paginated_rollout_migration", "local_thread_store_compression", "api_key_model_discovery"];
    private const string GuardConfig = """
        {"notify":[],"cli_auth_credentials_store":"file","analytics":{"enabled":false},"otel":{"exporter":"none","trace_exporter":"none","metrics_exporter":"none"},"features":{"hooks":false,"plugins":false,"code_mode_host":false,"background_paginated_rollout_migration":false,"local_thread_store_compression":false,"api_key_model_discovery":false},"model_provider":"openai","model_providers":{},"mcp_servers":{}}
        """;
    // Literal wire fixtures are independent of Policy, parsing and expected-layer construction.
    private const string ConfigWire = """
        {"config":{"notify":[],"cli_auth_credentials_store":"file","analytics":{"enabled":false},"otel":{"exporter":"none","trace_exporter":"none","metrics_exporter":"none"},"features":{"hooks":false,"plugins":false,"code_mode_host":false,"background_paginated_rollout_migration":false,"local_thread_store_compression":false,"api_key_model_discovery":false},"model_provider":"openai","model_providers":{},"mcp_servers":{}},"layers":[{"name":{"type":"sessionFlags"},"version":"v-session","config":{"notify":[],"cli_auth_credentials_store":"file","analytics":{"enabled":false},"otel":{"exporter":"none","trace_exporter":"none","metrics_exporter":"none"},"features":{"hooks":false,"plugins":false,"code_mode_host":false,"background_paginated_rollout_migration":false,"local_thread_store_compression":false,"api_key_model_discovery":false},"model_provider":"openai","model_providers":{},"mcp_servers":{}}},{"name":{"type":"project","dotCodexFolder":$PROJECT$},"version":"v-project","config":{},"disabledReason":"synthetic-untrusted"},{"name":{"type":"user","file":$USER$,"profile":null},"version":"v-user","config":{}},{"name":{"type":"system","file":$SYSTEM$},"version":"v-system","config":{}}],"origins":{"cli_auth_credentials_store":{"name":{"type":"sessionFlags"},"version":"v-session"},"analytics.enabled":{"name":{"type":"sessionFlags"},"version":"v-session"},"otel.exporter":{"name":{"type":"sessionFlags"},"version":"v-session"},"otel.trace_exporter":{"name":{"type":"sessionFlags"},"version":"v-session"},"otel.metrics_exporter":{"name":{"type":"sessionFlags"},"version":"v-session"},"features.hooks":{"name":{"type":"sessionFlags"},"version":"v-session"},"features.plugins":{"name":{"type":"sessionFlags"},"version":"v-session"},"features.code_mode_host":{"name":{"type":"sessionFlags"},"version":"v-session"},"features.background_paginated_rollout_migration":{"name":{"type":"sessionFlags"},"version":"v-session"},"features.local_thread_store_compression":{"name":{"type":"sessionFlags"},"version":"v-session"},"features.api_key_model_discovery":{"name":{"type":"sessionFlags"},"version":"v-session"},"model_provider":{"name":{"type":"sessionFlags"},"version":"v-session"}}}
        """;
    private const string RegistryWire = """
        {"data":[{"name":"hooks","stage":"stable","enabled":false,"defaultEnabled":true},{"name":"plugins","stage":"stable","enabled":false,"defaultEnabled":true},{"name":"code_mode_host","stage":"underDevelopment","enabled":false,"defaultEnabled":false},{"name":"background_paginated_rollout_migration","stage":"underDevelopment","enabled":false,"defaultEnabled":false},{"name":"local_thread_store_compression","stage":"underDevelopment","enabled":false,"defaultEnabled":false},{"name":"api_key_model_discovery","stage":"underDevelopment","enabled":false,"defaultEnabled":false}],"nextCursor":null}
        """;
    private static JsonElement Json(string text) { using var doc = JsonDocument.Parse(text, new() { MaxDepth = 32 }); return doc.RootElement.Clone(); }
    private static JsonElement Params(object? value) => JsonSerializer.SerializeToElement(value);
    private static CoreException Invalid() => new("h2-inventory-invalid");
    private static void Require(bool value) { if (!value) throw Invalid(); }
    private static string PreparationBase
    {
        get
        {
            var directory = new DirectoryInfo(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
            Require(directory.Name == "net10.0-windows"); directory = directory.Parent ?? throw Invalid(); Require(directory.Name is "Release" or "Debug");
            foreach (var name in new[] { "bin", "AIUsageBar.Tests", "tests", "windows" }) { directory = directory.Parent ?? throw Invalid(); Require(directory.Name == name); }
            var repo = directory.Parent ?? throw Invalid(); return Path.Combine(repo.FullName, "build", "H2Preparation");
        }
    }
    private static SafeFileHandle PinDirectory(string path)
    {
        var handle = CreateFileW(path, 0x80, 3, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
        try { Require(!handle.IsInvalid); var info = PrivateFiles.Information(handle); Require((info.Attributes & 0x400) == 0 && (info.Attributes & 0x10) != 0); return handle; }
        catch { handle.Dispose(); throw; }
    }
    private sealed class PreparationScratch : IDisposable
    {
        private readonly List<SafeFileHandle> pins = [];
        public string Root { get; } = Path.Combine(PreparationBase, Guid.NewGuid().ToString("N"));
        public PreparationScratch()
        {
            try
            {
                var repo = Path.GetDirectoryName(Path.GetDirectoryName(PreparationBase))!; var ancestors = new Stack<string>();
                for (string? path = repo; path is not null; path = Path.GetDirectoryName(path)) ancestors.Push(path);
                foreach (var path in ancestors) pins.Add(PinDirectory(path));
                foreach (var path in new[] { Path.GetDirectoryName(PreparationBase)!, PreparationBase, Root }) { Directory.CreateDirectory(path); pins.Add(PinDirectory(path)); }
            }
            catch { foreach (var pin in pins) pin.Dispose(); throw; }
        }
        public void Dispose()
        {
            foreach (var pin in pins) pin.Dispose();
            Require(Path.GetDirectoryName(Root) == PreparationBase && Guid.TryParseExact(Path.GetFileName(Root), "N", out _));
            if (Directory.Exists(Root)) Delete(Root);
        }
        private void Delete(string directory)
        {
            Require(directory == Root || directory.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
            Require((File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0);
            foreach (var path in Directory.GetFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(path); Require((attributes & FileAttributes.ReparsePoint) == 0);
                if ((attributes & FileAttributes.Directory) != 0) Delete(path); else File.Delete(path);
            }
            Directory.Delete(directory);
        }
    }

    private sealed class Context : IDisposable
    {
        private readonly PreparationScratch scratch = new();
        private readonly PrivateFiles files;
        private readonly PrivateFiles projectFiles, machineFiles;
        private readonly Dictionary<string, string> hashes;
        public string Root => files.Root;
        public string Project => Path.Combine(Root, ".codex");
        public string User => Path.Combine(Root, "config.toml");
        public string SystemFile => Path.Combine(Root, "machine", "config.toml");
        public string WindowsRoot { get; } = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        public Context(Action<string>? afterCreation = null)
        {
            PrivateFiles? acquiredRoot = null, acquiredProject = null, acquiredMachine = null;
            try
            {
                files = acquiredRoot = new PrivateFiles(Path.Combine(scratch.Root, "private"));
                projectFiles = acquiredProject = new PrivateFiles(Project); machineFiles = acquiredMachine = new PrivateFiles(Path.GetDirectoryName(SystemFile)!);
                files.AtomicWrite("config.toml", []); projectFiles.AtomicWrite("config.toml", []);
                files.AtomicWrite(".h2-root", "AIUsageBar-H2-Synthetic"u8.ToArray());
                files.AtomicWrite("wire.jsonl", []); files.AtomicWrite("spawn.txt", []);
                afterCreation?.Invoke(Root);
                hashes = Snapshot().Where(x => x.Key is not ("wire.jsonl" or "spawn.txt")).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
                Validate();
            }
            catch { acquiredMachine?.Dispose(); acquiredProject?.Dispose(); acquiredRoot?.Dispose(); scratch.Dispose(); throw; }
        }
        public Dictionary<string, string> EnvironmentBlock() => new(StringComparer.OrdinalIgnoreCase)
        {
            ["SystemRoot"] = WindowsRoot, ["WINDIR"] = WindowsRoot, ["CODEX_HOME"] = Root,
            ["USERPROFILE"] = Root, ["HOME"] = Root, ["APPDATA"] = Root, ["LOCALAPPDATA"] = Root, ["TEMP"] = Root, ["TMP"] = Root
        };
        public byte[] Read(string name) => files.Read(name, 2 * 1024 * 1024);
        public void RestoreUser() => files.AtomicWrite("config.toml", []);
        public Dictionary<string, string> Snapshot()
        {
            // Pinned PrivateFiles directories cannot be renamed. Enumerate one level only;
            // reject unexpected/reparse paths before ANY content hashing, then validated readers reject hardlinks.
            var stores = new[] { (Store: files, Names: new[] { ".h2-root", "config.toml", "writer.lock", "wire.jsonl", "spawn.txt" }), (Store: projectFiles, Names: new[] { "config.toml", "writer.lock" }), (Store: machineFiles, Names: Array.Empty<string>()) };
            foreach (var store in stores)
            {
                var expected = store.Names.Select(x => Path.Combine(store.Store.Root, x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (store.Store == files) { expected.Add(Project); expected.Add(machineFiles.Root); }
                var actual = Directory.GetFileSystemEntries(store.Store.Root); Require(actual.Length == expected.Count);
                foreach (var path in actual)
                { Require(expected.Contains(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0); Require(((File.GetAttributes(path) & FileAttributes.Directory) != 0) == (path == Project || path == machineFiles.Root)); }
            }
            var held = new List<FileStream>();
            try
            {
                // Validate every file's type, ACL and single-link identity before hashing any file.
                foreach (var store in stores) foreach (var name in store.Names) held.Add(store.Store.OpenReader(name));
                var snapshot = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var store in stores) foreach (var name in store.Names)
                    snapshot.Add(Path.GetRelativePath(Root, Path.Combine(store.Store.Root, name)), Convert.ToHexString(SHA256.HashData(store.Store.Read(name, 2 * 1024 * 1024))));
                return snapshot;
            }
            finally { foreach (var stream in held) stream.Dispose(); }
        }
        public void Validate()
        {
            PathIdentity(Root, Root); PathIdentity(Project, Project); PathIdentity(User, User); PathIdentity(SystemFile, SystemFile);
            var snapshot = Snapshot(); foreach (var item in hashes) Require(snapshot.TryGetValue(item.Key, out var hash) && hash == item.Value);
        }
        public void Dispose() { machineFiles.Dispose(); projectFiles.Dispose(); files.Dispose(); scratch.Dispose(); }
    }

    private static IReadOnlyList<string> Policy() => new[]
    {
        "features.hooks=false", "features.plugins=false", "features.code_mode_host=false", "notify=[]", "analytics.enabled=false",
        "otel.exporter=\"none\"", "otel.trace_exporter=\"none\"", "otel.metrics_exporter=\"none\"",
        "features.background_paginated_rollout_migration=false", "features.local_thread_store_compression=false",
        "features.api_key_model_discovery=false", "cli_auth_credentials_store=\"file\"", "model_provider=\"openai\"", "model_providers={}", "mcp_servers={}"
    }.SelectMany(x => new[] { "-c", x }).Concat(["app-server", "--listen", "stdio://", "--strict-config"]).ToArray();
    private static string Wire(Context context) => ConfigWire.Replace("$PROJECT$", JsonSerializer.Serialize(context.Project), StringComparison.Ordinal)
        .Replace("$USER$", JsonSerializer.Serialize(context.User), StringComparison.Ordinal).Replace("$SYSTEM$", JsonSerializer.Serialize(context.SystemFile), StringComparison.Ordinal);
    private static ProcessLaunch FakeLaunch(Context context, string scenario) => new(Fixture.Self, ["--fake-h2", scenario, context.Root, .. Policy()], context.Root, context.EnvironmentBlock());
    private static void PathIdentity(string value, string expected)
    {
        Require(value.Length is > 0 and <= 4096 && !value.StartsWith("\\\\", StringComparison.Ordinal) && Path.IsPathFullyQualified(value));
        Require(!value.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(x => x is "." or ".."));
        Require(string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(value)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(expected)), StringComparison.OrdinalIgnoreCase));
    }
    private static JsonElement Object(JsonElement value) { Require(value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() <= 4096); return value; }
    private static JsonElement Field(JsonElement value, string key) { Object(value); Require(value.TryGetProperty(key, out var result)); return result; }
    private static string Text(JsonElement value, int bound = 4096)
    { Require(value.ValueKind == JsonValueKind.String); var text = value.GetString()!; Require(text.Length != 0 && Encoding.UTF8.GetByteCount(text) <= bound); return text; }
    private static bool Boolean(JsonElement value) { Require(value.ValueKind is JsonValueKind.True or JsonValueKind.False); return value.GetBoolean(); }
    private static bool Optional(JsonElement value, string key, out JsonElement result) => value.TryGetProperty(key, out result) && result.ValueKind != JsonValueKind.Null;
    private static void EmptyObject(JsonElement value) { Object(value); Require(!value.EnumerateObject().Any()); }
    private static void Bounded(JsonElement value, int depth = 0)
    {
        Require(depth <= 32);
        if (value.ValueKind == JsonValueKind.Object)
        { Require(value.EnumerateObject().Count() <= 4096); foreach (var item in value.EnumerateObject()) { Require(Encoding.UTF8.GetByteCount(item.Name) <= 4096); Bounded(item.Value, depth + 1); } }
        else if (value.ValueKind == JsonValueKind.Array)
        { Require(value.GetArrayLength() <= 4096); foreach (var item in value.EnumerateArray()) Bounded(item, depth + 1); }
        else if (value.ValueKind == JsonValueKind.String) Require(Encoding.UTF8.GetByteCount(value.GetString()!) <= 4096);
    }
    private static void EqualJson(JsonElement actual, JsonElement expected)
    {
        Require(actual.ValueKind == expected.ValueKind);
        if (actual.ValueKind == JsonValueKind.Object)
        {
            Require(actual.EnumerateObject().Count() == expected.EnumerateObject().Count());
            foreach (var item in expected.EnumerateObject()) EqualJson(Field(actual, item.Name), item.Value);
        }
        else if (actual.ValueKind == JsonValueKind.Array)
        { Require(actual.GetArrayLength() == expected.GetArrayLength()); for (var i = 0; i < actual.GetArrayLength(); i++) EqualJson(actual[i], expected[i]); }
        else Require(actual.GetRawText() == expected.GetRawText());
    }
    private static void Handshake(JsonElement result, Context context)
    {
        Bounded(result);
        _ = Text(Field(result, "userAgent"), 1024); PathIdentity(Text(Field(result, "codexHome")), context.Root);
        Require(Text(Field(result, "platformFamily"), 64) == "windows" && Text(Field(result, "platformOs"), 64) == "windows");
    }
    private static string? RegistryPage(JsonElement result, Dictionary<string, (bool Enabled, string Stage)> rows)
    {
        Bounded(result);
        var data = Field(result, "data"); Require(data.ValueKind == JsonValueKind.Array && data.GetArrayLength() <= 100);
        foreach (var row in data.EnumerateArray())
        {
            var name = Text(Field(row, "name"), 256); var stage = Text(Field(row, "stage"), 256);
            Require(stage is "stable" or "beta" or "underDevelopment" or "deprecated" or "removed");
            var enabled = Boolean(Field(row, "enabled")); _ = Boolean(Field(row, "defaultEnabled"));
            Require(rows.TryAdd(name, (enabled, stage)) && rows.Count <= 2048);
        }
        return Optional(result, "nextCursor", out var cursor) ? Text(cursor, 4096) : null;
    }
    private static void RegistryComplete(Dictionary<string, (bool Enabled, string Stage)> rows)
    { foreach (var name in Guards) Require(rows.TryGetValue(name, out var row) && !row.Enabled && (row.Stage is "stable" or "beta" or "underDevelopment")); }
    private static void Source(JsonElement source, string type, Context context)
    {
        Require(Text(Field(source, "type"), 256) == type);
        switch (type)
        {
            case "sessionFlags": Require(source.EnumerateObject().Count() == 1); break;
            case "user": PathIdentity(Text(Field(source, "file")), context.User); Require(!Optional(source, "profile", out _)); break;
            case "project": PathIdentity(Text(Field(source, "dotCodexFolder")), context.Project); break;
            case "system": PathIdentity(Text(Field(source, "file")), context.SystemFile); break;
            default: throw Invalid();
        }
    }
    private static IEnumerable<string> OriginKeys() => new[] { "cli_auth_credentials_store", "analytics.enabled", "otel.exporter", "otel.trace_exporter", "otel.metrics_exporter", "model_provider" }.Concat(Guards.Select(x => "features." + x));
    private static bool ExactRequirement(string key, JsonElement? response)
    {
        if (response is not { } root || !Optional(root, "requirements", out var requirements)) return false;
        if (key == "cli_auth_credentials_store") return Optional(requirements, "cliAuthCredentialsStore", out var store) && Text(store, 32) == "file";
        if (key == "model_provider") return Optional(requirements, "modelProvider", out var provider) && Text(provider, 256) == "openai";
        return key.StartsWith("features.", StringComparison.Ordinal) && Optional(requirements, "featureRequirements", out var features) && features.TryGetProperty(key[9..], out var feature) && !Boolean(feature);
    }
    private static void Config(JsonElement result, Context context, JsonElement? requirementResponse = null)
    {
        Bounded(result);
        var config = Object(Field(result, "config")); var notify = Field(config, "notify"); Require(notify.ValueKind == JsonValueKind.Array && notify.GetArrayLength() == 0);
        Require(Text(Field(config, "cli_auth_credentials_store")) == "file" && !Boolean(Field(Field(config, "analytics"), "enabled")));
        var otel = Field(config, "otel"); foreach (var name in new[] { "exporter", "trace_exporter", "metrics_exporter" }) Require(Text(Field(otel, name)) == "none");
        var features = Object(Field(config, "features")); Require(features.EnumerateObject().Count() <= 2048);
        foreach (var name in Guards) Require(!Boolean(Field(features, name)));
        Require(!Optional(config, "model_provider", out var provider) || Text(provider, 256) == "openai");
        EmptyObject(Field(config, "model_providers")); EmptyObject(Field(config, "mcp_servers"));
        var layers = Field(result, "layers"); Require(layers.ValueKind == JsonValueKind.Array && layers.GetArrayLength() <= 64 && layers.GetArrayLength() == 4);
        string[] types = ["sessionFlags", "project", "user", "system"], versions = ["v-session", "v-project", "v-user", "v-system"];
        for (var i = 0; i < layers.GetArrayLength(); i++)
        {
            var layer = Object(layers[i]); Source(Field(layer, "name"), types[i], context); Require(Text(Field(layer, "version"), 512) == versions[i]);
            var content = Object(Field(layer, "config")); if (i == 0) EqualJson(content, Json(GuardConfig)); else EmptyObject(content);
            if (i == 1) Require(Text(Field(layer, "disabledReason")) == "synthetic-untrusted"); else Require(!Optional(layer, "disabledReason", out _));
        }
        var origins = Object(Field(result, "origins")); Require(origins.EnumerateObject().Count() <= 4096);
        foreach (var item in origins.EnumerateObject())
        { Require(OriginKeys().Contains(item.Name, StringComparer.Ordinal)); Source(Field(item.Value, "name"), "sessionFlags", context); Require(Text(Field(item.Value, "version"), 512) == "v-session"); }
        foreach (var key in OriginKeys().Where(x => x != "model_provider" || Optional(config, "model_provider", out _)))
        { if (ExactRequirement(key, requirementResponse)) Require(!origins.TryGetProperty(key, out _)); else _ = Field(origins, key); }
    }
    private static void Requirements(JsonElement result)
    {
        Bounded(result);
        var requirements = Field(result, "requirements"); if (requirements.ValueKind == JsonValueKind.Null) return;
        Object(requirements);
        foreach (var item in requirements.EnumerateObject())
        {
            if (item.Value.ValueKind == JsonValueKind.Null) continue;
            switch (item.Name)
            {
                case "allowedLoginMethods":
                    Require(item.Value.ValueKind == JsonValueKind.Array && item.Value.GetArrayLength() <= 2);
                    var methods = item.Value.EnumerateArray().Select(x => Text(x, 32)).ToArray(); Require(methods.All(x => x is "api" or "chatgpt") && methods.Distinct(StringComparer.Ordinal).Count() == methods.Length); break;
                case "modelProvider": Require(Text(item.Value, 256) == "openai"); break;
                case "modelProviders": EmptyObject(item.Value); break;
                case "cliAuthCredentialsStore": Require(Text(item.Value, 32) == "file"); break;
                case "featureRequirements":
                    Object(item.Value); Require(item.Value.EnumerateObject().Count() <= 2048);
                    foreach (var feature in item.Value.EnumerateObject()) Require(Guards.Contains(feature.Name, StringComparer.Ordinal) && !Boolean(feature.Value)); break;
                // All other NONNULL constraints are deliberately unsupported, including path/URL overrides.
                default: throw Invalid();
            }
        }
    }

    private static async Task<Process> RetainPrimary(WindowsProcess process)
    {
        Process? primary = null;
        try { primary = Process.GetProcessById(process.ProcessId); _ = primary.SafeHandle; return primary; }
        catch { primary?.Dispose(); await process.DisposeAsync(); throw; }
    }
    private static async Task WaitTeardown(Process primary)
    {
        var handle = primary.SafeHandle; var budget = Stopwatch.StartNew();
        while (true)
        {
            var state = WaitForSingleObject(handle, 0);
            if (state == 0) return;
            if (state != 258 || budget.Elapsed >= TimeSpan.FromSeconds(2)) throw new CoreException("cleanup-failure");
            await Task.Delay(10);
        }
    }
    private static async Task<JsonRpcTransport> Construct(WindowsProcess process, Func<WindowsProcess, JsonRpcTransport> make)
    { try { return make(process); } catch { await process.DisposeAsync(); throw; } }
    private sealed class FakeFiles : IDisposable
    {
        private readonly List<SafeFileHandle> directories = [];
        private readonly List<FileStream> streams = [];
        public FileStream Wire { get; }
        public FileStream Spawn { get; }
        public FakeFiles(string root)
        {
            try
            {
                Require(Path.IsPathFullyQualified(root) && !root.StartsWith("\\\\", StringComparison.Ordinal));
                var parent = Path.GetDirectoryName(root)!;
                Require(Path.GetFileName(root) == "private" && Guid.TryParseExact(Path.GetFileName(parent), "N", out _) && string.Equals(Path.GetDirectoryName(parent), PreparationBase, StringComparison.OrdinalIgnoreCase));
                PathIdentity(root, root); var paths = new Stack<string>(); for (string? path = root; path is not null; path = Path.GetDirectoryName(path)) paths.Push(path);
                foreach (var path in paths)
                {
                    directories.Add(PinDirectory(path));
                }
                Security(new DirectoryInfo(root).GetAccessControl());
                using var marker = Open(root, ".h2-root", write: false); Require(marker.Length == "AIUsageBar-H2-Synthetic"u8.Length); var label = new byte["AIUsageBar-H2-Synthetic"u8.Length]; marker.ReadExactly(label); Require(label.AsSpan().SequenceEqual("AIUsageBar-H2-Synthetic"u8));
                Wire = Open(root, "wire.jsonl", write: true); streams.Add(Wire); Spawn = Open(root, "spawn.txt", write: true); streams.Add(Spawn);
                Require(Wire.Length == 0 && Spawn.Length == 0);
            }
            catch { Dispose(); throw; }
        }
        private static FileStream Open(string root, string name, bool write)
        {
            var handle = CreateFileW(Path.Combine(root, name), write ? 0xC0000000 : 0x80000000, 3, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
            if (handle.IsInvalid) { handle.Dispose(); throw Invalid(); } var stream = new FileStream(handle, write ? FileAccess.ReadWrite : FileAccess.Read);
            try { var information = PrivateFiles.Information(handle); Require((information.Attributes & (0x400 | 0x10)) == 0 && information.Links == 1); Security(stream.GetAccessControl()); return stream; }
            catch { stream.Dispose(); throw; }
        }
        private static void Security(FileSystemSecurity security)
        {
            using var identity = WindowsIdentity.GetCurrent(); var user = identity.User!; var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            Require(security.AreAccessRulesProtected && user.Equals(security.GetOwner(typeof(SecurityIdentifier)))); var full = new HashSet<string>(StringComparer.Ordinal);
            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            { var sid = (SecurityIdentifier)rule.IdentityReference; Require(rule.AccessControlType == AccessControlType.Allow && (sid.Equals(user) || sid.Equals(system))); if ((rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl) full.Add(sid.Value); }
            Require(full.Contains(user.Value) && full.Contains(system.Value));
        }
        public void Dispose() { foreach (var stream in streams) stream.Dispose(); foreach (var directory in directories) directory.Dispose(); }
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string path, uint access, uint sharing, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLinkW(string link, string existing, IntPtr security);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeProcessHandle process, uint milliseconds);
    private static async Task Inventory(Context context, string scenario, Func<WindowsProcess, JsonRpcTransport>? make = null, CancellationToken cancellationToken = default)
    {
        context.Validate(); using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); deadline.CancelAfter(TimeSpan.FromSeconds(30)); var token = deadline.Token;
        var process = WindowsProcess.Start(FakeLaunch(context, scenario)); using var primary = await RetainPrimary(process);
        try
        {
            var transport = await Construct(process, make ?? (p => new JsonRpcTransport(p, inventoryOnly: true)));
            await using (transport)
            {
                Handshake(await transport.RequestAsync("initialize", Params(new { clientInfo = new { name = "aiusagebar-h2-inventory", title = "AIUsageBar H2", version = "0.1.0" }, capabilities = new { explicitGatewayOauth = true, experimentalApi = true } }), token), context);
                await transport.NotifyInitializedAsync(token);
                var rows = new Dictionary<string, (bool Enabled, string Stage)>(StringComparer.Ordinal); var cursors = new HashSet<string>(StringComparer.Ordinal); string? cursor = null; bool complete = false;
                for (var page = 0; page < 32; page++)
                {
                    cursor = RegistryPage(await transport.RequestAsync("experimentalFeature/list", Params(new { limit = 100, cursor }), token), rows);
                    if (cursor is null) { RegistryComplete(rows); complete = true; break; } Require(cursors.Add(cursor));
                }
                Require(complete);
                var config = await transport.RequestAsync("config/read", Params(new { includeLayers = true, cwd = context.Root }), token);
                var requirements = await transport.RequestAsync("configRequirements/read", Params(null), token);
                Requirements(requirements); Config(config, context, requirements);
                await transport.FinishAsync(token);
            }
        }
        finally { await WaitTeardown(primary); }
        context.Validate();
    }

    private static string HandshakeWire(string root) => "{\"userAgent\":\"synthetic-h2\",\"codexHome\":" + JsonSerializer.Serialize(root) + ",\"platformFamily\":\"windows\",\"platformOs\":\"windows\"}";
    // JSON Pointer here is only a fixture-mutation helper; it does not interpret protocol origins.
    private static JsonNode Parent(JsonNode node, string pointer, out string key)
    { var parts = pointer.Split('/'); key = parts[^1]; foreach (var part in parts[..^1]) node = node is JsonArray a ? a[int.Parse(part)]! : node[part]!; return node; }
    private static string Changed(string text, string pointer, string mutation)
    {
        var node = JsonNode.Parse(text)!; var parent = Parent(node, pointer, out var key);
        JsonNode? value = mutation is "null" or "missing" ? null : JsonNode.Parse(mutation);
        if (parent is JsonArray array) { if (mutation == "missing") array.RemoveAt(int.Parse(key)); else array[int.Parse(key)] = value; }
        else if (mutation == "missing") ((JsonObject)parent).Remove(key); else parent[key] = value;
        return node.ToJsonString();
    }
    private static void InvalidVariants(string baseline, string pointer, string unsafeValue, Action<JsonElement> validate, bool optional = false)
    {
        foreach (var mutation in new[] { "missing", "null", "42", unsafeValue })
        {
            var value = Json(Changed(baseline, pointer, mutation));
            if (optional && (mutation is "missing" or "null")) validate(value);
            else Check.Throws<CoreException>(() => validate(value));
        }
    }

    public static IEnumerable<TestCase> Cases()
    {
        yield return TestCase.Sync("h2-inventory/policy-environment-bootstrap", () =>
        {
            using var context = new Context(); var environment = context.EnvironmentBlock(); Check.Equal(9, environment.Count); Check.True(environment.Comparer.Equals(StringComparer.OrdinalIgnoreCase));
            foreach (var name in new[] { "PATH", "OPENAI_API_KEY", "CODEX_ACCESS_TOKEN", "HTTPS_PROXY", "OTEL_EXPORTER_OTLP_ENDPOINT", "OPENAI_IDENTITY_TOKEN_FILE", "CODEX_EXEC_SERVER_URL" }) Check.True(!environment.ContainsKey(name));
            var expected = "features.hooks=false|features.plugins=false|features.code_mode_host=false|notify=[]|analytics.enabled=false|otel.exporter=\"none\"|otel.trace_exporter=\"none\"|otel.metrics_exporter=\"none\"|features.background_paginated_rollout_migration=false|features.local_thread_store_compression=false|features.api_key_model_discovery=false|cli_auth_credentials_store=\"file\"|model_provider=\"openai\"|model_providers={}|mcp_servers={}";
            var arguments = Policy(); Check.Equal(expected, string.Join('|', Enumerable.Range(0, 15).Select(i => arguments[i * 2 + 1]))); Check.True(arguments.Take(30).Where((_, i) => i % 2 == 0).All(x => x == "-c"));
            Check.Equal("app-server|--listen|stdio://|--strict-config", string.Join('|', arguments.Skip(30)));
            Check.Throws<CoreException>(() => PathIdentity(Path.Combine(context.Root, "..", "escape"), context.Root));
            File.WriteAllText(Path.Combine(context.Root, "auth.json"), "synthetic"); Check.Throws<CoreException>(context.Validate); File.Delete(Path.Combine(context.Root, "auth.json"));
            File.WriteAllText(context.User, "model_providers={custom={env_key='SYNTHETIC'}}"); Check.Throws<CoreException>(context.Validate);
        });
        yield return TestCase.Sync("h2-inventory/constructor-failure-and-exact-scratch-manifest", () =>
        {
            string? failedRoot = null;
            Check.Throws<CoreException>(() => new Context(root => { failedRoot = root; throw new CoreException("synthetic-construction"); }));
            Check.True(failedRoot is not null && !Directory.Exists(Path.GetDirectoryName(failedRoot)!));
            using var context = new Context(); File.WriteAllText(Path.Combine(context.Root, "unexpected.txt"), "synthetic");
            Check.Throws<CoreException>(context.Validate); File.Delete(Path.Combine(context.Root, "unexpected.txt")); context.Validate();
            Check.Throws<CoreException>(() => new FakeFiles(Path.GetDirectoryName(context.Root)!));
            using var other = new Context(); File.Delete(context.User); Check.True(CreateHardLinkW(context.User, other.User, IntPtr.Zero));
            Check.Throws<CoreException>(context.Validate); File.Delete(context.User); context.RestoreUser(); context.Validate(); other.Validate();
        });
        yield return TestCase.Sync("h2-inventory/handshake-field-matrix", () =>
        {
            using var context = new Context(); var wire = HandshakeWire(context.Root); Handshake(Json(wire), context);
            foreach (var key in new[] { "userAgent", "codexHome", "platformFamily", "platformOs" }) InvalidVariants(wire, key, key == "userAgent" ? "\"\"" : "\"unsafe\"", x => Handshake(x, context));
        });
        yield return TestCase.Sync("h2-inventory/config-consumer-field-matrix", () =>
        {
            using var context = new Context(); var wire = Wire(context); Config(Json(wire), context);
            foreach (var key in new[] { "config", "layers", "origins" }) InvalidVariants(wire, key, "{}", x => Config(x, context));
            InvalidVariants(wire, "config/notify", "[\"synthetic-notify\"]", x => Config(x, context));
            InvalidVariants(wire, "config/cli_auth_credentials_store", "\"auto\"", x => Config(x, context));
            foreach (var key in new[] { "analytics", "otel", "features" }) InvalidVariants(wire, "config/" + key, "{}", x => Config(x, context));
            InvalidVariants(wire, "config/analytics/enabled", "true", x => Config(x, context));
            foreach (var key in new[] { "exporter", "trace_exporter", "metrics_exporter" }) InvalidVariants(wire, "config/otel/" + key, "\"statsig\"", x => Config(x, context));
            foreach (var key in Guards) InvalidVariants(wire, "config/features/" + key, "true", x => Config(x, context));
            InvalidVariants(wire, "config/model_provider", "\"custom\"", x => Config(x, context), optional: true);
            foreach (var key in new[] { "model_providers", "mcp_servers" }) InvalidVariants(wire, "config/" + key, "{\"custom\":{\"env_key\":\"SYNTHETIC\"}}", x => Config(x, context));
            Check.Throws<CoreException>(() => Config(Json(Changed(wire, "config/cli_auth_credentials_store", "missing").Replace("\"model_provider\"", "\"modelProvider\"", StringComparison.Ordinal)), context));
        });
        yield return TestCase.Sync("h2-inventory/layer-origin-field-matrix", () =>
        {
            using var context = new Context(); var wire = Wire(context);
            for (var i = 0; i < 4; i++)
            {
                var row = "layers/" + i + "/";
                foreach (var key in new[] { "name", "version", "config" }) InvalidVariants(wire, row + key, key == "version" ? "\"drift\"" : "{\"unexpected\":true}", x => Config(x, context));
                InvalidVariants(wire, row + "name/type", "\"enterpriseManaged\"", x => Config(x, context));
                if (i > 0) InvalidVariants(wire, row + "name/" + (i == 1 ? "dotCodexFolder" : "file"), "\"C:\\\\outside\"", x => Config(x, context));
            }
            InvalidVariants(wire, "layers/1/disabledReason", "\"drift\"", x => Config(x, context));
            InvalidVariants(wire, "layers/2/name/profile", "\"custom\"", x => Config(x, context), optional: true);
            foreach (var key in OriginKeys())
            {
                InvalidVariants(wire, "origins/" + key, "{}", x => Config(x, context));
                InvalidVariants(wire, "origins/" + key + "/name", "{\"type\":\"system\",\"file\":\"C:\\\\outside\"}", x => Config(x, context));
                InvalidVariants(wire, "origins/" + key + "/version", "\"drift\"", x => Config(x, context));
            }
            var changed = JsonNode.Parse(wire)!; var layers = (JsonArray)changed["layers"]!; var first = layers[0]!.DeepClone(); var second = layers[1]!.DeepClone(); layers[0] = second; layers[1] = first; Check.Throws<CoreException>(() => Config(Json(changed.ToJsonString()), context));
            foreach (var key in new[] { "notify", "model_providers", "mcp_servers" }) Check.True(!Json(wire).GetProperty("origins").TryGetProperty(key, out _));
            var exact = Json("{\"requirements\":{\"featureRequirements\":{\"hooks\":false},\"cliAuthCredentialsStore\":\"file\",\"modelProvider\":\"openai\"}}"); Requirements(exact);
            var filtered = Changed(Changed(Changed(wire, "origins/features.hooks", "missing"), "origins/cli_auth_credentials_store", "missing"), "origins/model_provider", "missing");
            Config(Json(filtered), context, exact); Check.Throws<CoreException>(() => Config(Json(filtered), context)); Check.Throws<CoreException>(() => Config(Json(wire), context, exact));
        });
        yield return TestCase.Sync("h2-inventory/requirements-field-matrix", () =>
        {
            Requirements(Json("{\"requirements\":null}")); Requirements(Json("{\"requirements\":{}}")); Requirements(Json("{\"requirements\":{\"allowedLoginMethods\":[\"api\",\"chatgpt\"]}}"));
            foreach (var key in new[] { "modelProvider", "modelProviders", "cliAuthCredentialsStore", "featureRequirements", "allowedLoginMethods", "chatgptBaseUrl", "sqliteHome", "logDir", "modelCatalogJson" })
            {
                var baseline = "{\"requirements\":{\"" + key + "\":null}}"; Requirements(Json(baseline)); Requirements(Json(Changed(baseline, "requirements/" + key, "missing")));
                foreach (var wrong in new[] { "42", "true", "\"unsafe\"" }) Check.Throws<CoreException>(() => Requirements(Json(Changed(baseline, "requirements/" + key, wrong))));
            }
            Requirements(Json("{\"requirements\":{\"modelProvider\":\"openai\",\"modelProviders\":{},\"cliAuthCredentialsStore\":\"file\",\"featureRequirements\":{\"hooks\":false},\"allowedLoginMethods\":[]}}"));
            foreach (var wire in new[] { "null", "{}", "{\"requirements\":42}", "{\"requirements\":{\"allowedLoginMethods\":[\"api\",\"api\"]}}", "{\"requirements\":{\"allowedLoginMethods\":[null]}}", "{\"requirements\":{\"featureRequirements\":{\"unknown\":false}}}", "{\"requirements\":{\"featureRequirements\":{\"plugins\":true}}}", "{\"requirements\":{\"modelProviders\":{\"custom\":{}}}}", "{\"requirements\":{\"futureConstraint\":true}}" }) Check.Throws<CoreException>(() => Requirements(Json(wire)));
        });
        yield return TestCase.Sync("h2-inventory/string-collection-and-depth-bounds", () =>
        {
            using var context = new Context(); var handshake = HandshakeWire(context.Root); var config = Wire(context);
            Check.Throws<CoreException>(() => Handshake(Json(Changed(handshake, "userAgent", JsonSerializer.Serialize(new string('u', 1025)))), context));
            Check.Throws<CoreException>(() => Config(Json(Changed(config, "layers/0/version", JsonSerializer.Serialize(new string('v', 513)))), context));
            Check.Throws<CoreException>(() => Bounded(Params(new { descriptions = new string('d', 4097) })));
            Check.Throws<CoreException>(() => Bounded(Params(Enumerable.Range(0, 4097).ToArray())));
            Check.Throws<CoreException>(() => Config(Json(Changed(config, "origins", JsonSerializer.Serialize(Enumerable.Range(0, 4097).ToDictionary(i => "k" + i, _ => (object?)null)))), context));
            var deep = new string('[', 34) + "0" + new string(']', 34); Check.Throws<JsonException>(() => Json(deep));
        });
        yield return TestCase.Sync("h2-inventory/registry-field-matrix-and-bounds", () =>
        {
            void Validate(JsonElement value) { var rows = new Dictionary<string, (bool Enabled, string Stage)>(); RegistryPage(value, rows); RegistryComplete(rows); }
            Validate(Json(RegistryWire));
            InvalidVariants(RegistryWire, "data", "[]", Validate);
            foreach (var key in new[] { "name", "stage", "enabled", "defaultEnabled" }) InvalidVariants(RegistryWire, "data/0/" + key, key == "name" ? "\"unknown\"" : key == "stage" ? "\"removed\"" : "\"bad\"", Validate);
            Validate(Json(Changed(RegistryWire, "nextCursor", "missing"))); Validate(Json(Changed(RegistryWire, "nextCursor", "null")));
            foreach (var cursor in new[] { "42", "\"\"", JsonSerializer.Serialize(new string('c', 4097)) }) Check.Throws<CoreException>(() => Validate(Json(Changed(RegistryWire, "nextCursor", cursor))));
            var duplicate = JsonNode.Parse(RegistryWire)!; ((JsonArray)duplicate["data"]!).Add(duplicate["data"]![0]!.DeepClone()); Check.Throws<CoreException>(() => Validate(Json(duplicate.ToJsonString())));
            var oversized = Params(new { data = Enumerable.Range(0, 101).Select(i => new { name = "x" + i, stage = "stable", enabled = false, defaultEnabled = false }), nextCursor = (string?)null }); Check.Throws<CoreException>(() => Validate(oversized));
            var entries = new Dictionary<string, (bool Enabled, string Stage)>();
            for (var page = 0; page < 20; page++) RegistryPage(Params(new { data = Enumerable.Range(page * 100, 100).Select(i => new { name = "x" + i, stage = "stable", enabled = false, defaultEnabled = false }) }), entries);
            Check.Throws<CoreException>(() => RegistryPage(Params(new { data = Enumerable.Range(2000, 49).Select(i => new { name = "x" + i, stage = "stable", enabled = false, defaultEnabled = false }) }), entries));
        });
        foreach (var scenario in new[] { "valid", "login-methods", "blocked-spawn" })
        {
            var mode = scenario; yield return new("h2-inventory/fake-wire-" + mode, async () =>
            { using var context = new Context(); await Inventory(context, mode); var methods = Encoding.UTF8.GetString(context.Read("wire.jsonl")).Split('\n', StringSplitOptions.RemoveEmptyEntries); Check.Equal(5, methods.Length); Check.True(methods.All(x => !x.Contains("account/", StringComparison.Ordinal) && !x.Contains("mcp", StringComparison.Ordinal))); if (mode == "blocked-spawn") Check.Equal("blocked", Encoding.UTF8.GetString(context.Read("spawn.txt"))); });
        }
        foreach (var scenario in new[] { "bad-handshake", "bad-registry", "cycle", "pages", "bad-config", "bad-requirements", "wrong-id", "server-request", "malformed", "oversize", "eof", "bad-exit", "stderr-lines" })
        {
            var mode = scenario; yield return new("h2-inventory/cleanup-after-" + mode, async () => { using var context = new Context(); await Check.ThrowsAsync<CoreException>(() => Inventory(context, mode)); });
        }
        yield return new("h2-inventory/constructor-and-cancellation-cleanup", async () =>
        {
            using var context = new Context(); await Check.ThrowsAsync<CoreException>(() => Inventory(context, "valid", _ => throw new CoreException("synthetic-construction")));
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200)); await Check.ThrowsAsync<OperationCanceledException>(() => Inventory(context, "stall", cancellationToken: cancellation.Token));
        });
        yield return new("h2-inventory/transport-public-H2-whitelist-instance-isolation", async () =>
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30)); var token = deadline.Token;
            using var h2Context = new Context(); var h2Process = WindowsProcess.Start(FakeLaunch(h2Context, "raw")); using var h2Primary = await RetainPrimary(h2Process);
            try
            {
                await using var h2 = await Construct(h2Process, p => new JsonRpcTransport(p, inventoryOnly: true));
                foreach (var method in new[] { "account/rateLimits/read", "mcpServerStatus/list", "account/read", "thread/start", "config/value/write" }) await Check.ThrowsAsync<CoreException>(() => h2.RequestAsync(method, Params(null), token));
                Check.Equal(0, h2Context.Read("wire.jsonl").Length);
                Requirements(await h2.RequestAsync("configRequirements/read", Params(null), token)); await h2.FinishAsync(token);
            }
            finally { await WaitTeardown(h2Primary); }
            using var publicContext = new Context(); var publicProcess = WindowsProcess.Start(FakeLaunch(publicContext, "raw")); using var publicPrimary = await RetainPrimary(publicProcess);
            try
            {
                await using var ordinary = await Construct(publicProcess, p => new JsonRpcTransport(p));
                await Check.ThrowsAsync<CoreException>(() => ordinary.RequestAsync("configRequirements/read", Params(null), token)); Check.Equal(0, publicContext.Read("wire.jsonl").Length);
                var quota = await ordinary.RequestAsync("account/rateLimits/read", Params(new { }), token); Check.Equal("synthetic", quota.GetProperty("fixture").GetString()); await ordinary.FinishAsync(token);
            }
            finally { await WaitTeardown(publicPrimary); }
        });
    }

    public static async Task<int> FakeChild(string[] args)
    {
        if (args.Length != 36) return 2; var scenario = args[0]; var root = args[1]; if (!Path.IsPathFullyQualified(root) || !Directory.Exists(root) || !string.Equals(root, Environment.CurrentDirectory, StringComparison.OrdinalIgnoreCase)) return 2;
        const string expectedPolicy = "features.hooks=false|features.plugins=false|features.code_mode_host=false|notify=[]|analytics.enabled=false|otel.exporter=\"none\"|otel.trace_exporter=\"none\"|otel.metrics_exporter=\"none\"|features.background_paginated_rollout_migration=false|features.local_thread_store_compression=false|features.api_key_model_discovery=false|cli_auth_credentials_store=\"file\"|model_provider=\"openai\"|model_providers={}|mcp_servers={}";
        if (string.Join('|', Enumerable.Range(0, 15).Select(i => args[i * 2 + 3])) != expectedPolicy || !Enumerable.Range(0, 15).All(i => args[i * 2 + 2] == "-c") || string.Join('|', args.Skip(32)) != "app-server|--listen|stdio://|--strict-config") return 2;
        foreach (var name in new[] { "CODEX_HOME", "USERPROFILE", "HOME", "APPDATA", "LOCALAPPDATA", "TEMP", "TMP" }) if (Environment.GetEnvironmentVariable(name) != root) return 2;
        using var files = new FakeFiles(root);
        var ordinal = 0; var registryPage = 0; var featureRequests = 0; var nextRequestId = 1; var output = Console.OpenStandardOutput();
        while (await Console.In.ReadLineAsync() is { } line)
        {
            using var document = JsonDocument.Parse(line); var request = document.RootElement; var method = request.GetProperty("method").GetString()!;
            if (scenario == "raw")
            {
                var expected = method switch
                {
                    "configRequirements/read" => """{"id":1,"method":"configRequirements/read","params":null}""",
                    "account/rateLimits/read" => """{"id":1,"method":"account/rateLimits/read","params":{}}""",
                    _ => "{}"
                };
                try { EqualJson(request, Json(expected)); } catch (CoreException) { return 7; }
            }
            if (scenario != "raw")
            {
                string[] expected = ["initialize", "initialized", "experimentalFeature/list", "config/read", "configRequirements/read"];
                if ((scenario is "cycle" or "pages") && method == "experimentalFeature/list") { }
                else if (ordinal >= expected.Length || method != expected[ordinal++]) return 7;
                var cursor = featureRequests == 0 ? "null" : scenario == "cycle" ? "\"cycle\"" : "\"p" + (featureRequests - 1) + "\"";
                var expectedBody = method switch
                {
                    "initialize" => """{"id":$ID$,"method":"initialize","params":{"clientInfo":{"name":"aiusagebar-h2-inventory","title":"AIUsageBar H2","version":"0.1.0"},"capabilities":{"explicitGatewayOauth":true,"experimentalApi":true}}}""",
                    "initialized" => """{"method":"initialized","params":{}}""",
                    "experimentalFeature/list" => """{"id":$ID$,"method":"experimentalFeature/list","params":{"limit":100,"cursor":$CURSOR$}}""",
                    "config/read" => """{"id":$ID$,"method":"config/read","params":{"includeLayers":true,"cwd":$ROOT$}}""",
                    "configRequirements/read" => """{"id":$ID$,"method":"configRequirements/read","params":null}""",
                    _ => "{}"
                };
                expectedBody = expectedBody.Replace("$ID$", nextRequestId.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal).Replace("$CURSOR$", cursor, StringComparison.Ordinal).Replace("$ROOT$", JsonSerializer.Serialize(root), StringComparison.Ordinal);
                try { EqualJson(request, Json(expectedBody)); } catch (CoreException) { return 7; }
                if (method != "initialized") nextRequestId++;
                if (method == "experimentalFeature/list") featureRequests++;
            }
            await files.Wire.WriteAsync(Encoding.UTF8.GetBytes(line + "\n")); await files.Wire.FlushAsync();
            if (method == "initialized") continue;
            if (scenario == "stall") { await Task.Delay(60_000); return 0; }
            if (scenario == "eof") return 0;
            if (scenario == "blocked-spawn" && method == "initialize")
            {
                string status;
                try { using var child = Process.Start(new ProcessStartInfo(Fixture.Self) { UseShellExecute = false, ArgumentList = { "--sleep" } }); status = "unexpected"; }
                catch (System.ComponentModel.Win32Exception) { status = "blocked"; }
                await files.Spawn.WriteAsync(Encoding.UTF8.GetBytes(status)); await files.Spawn.FlushAsync();
            }
            var result = method switch
            {
                "initialize" => HandshakeWire(root),
                "experimentalFeature/list" => RegistryWire,
                "config/read" => ConfigWire.Replace("$PROJECT$", JsonSerializer.Serialize(Path.Combine(root, ".codex")), StringComparison.Ordinal).Replace("$USER$", JsonSerializer.Serialize(Path.Combine(root, "config.toml")), StringComparison.Ordinal).Replace("$SYSTEM$", JsonSerializer.Serialize(Path.Combine(root, "machine", "config.toml")), StringComparison.Ordinal),
                "configRequirements/read" => scenario == "login-methods" ? "{\"requirements\":{\"allowedLoginMethods\":[\"api\",\"chatgpt\"]}}" : "{\"requirements\":null}",
                "account/rateLimits/read" when scenario == "raw" => "{\"fixture\":\"synthetic\"}",
                _ => "{}"
            };
            if (scenario == "bad-handshake" && method == "initialize") result = Changed(result, "platformOs", "\"other\"");
            if (scenario == "bad-registry" && method == "experimentalFeature/list") result = Changed(result, "data/0/enabled", "true");
            if ((scenario is "cycle" or "pages") && method == "experimentalFeature/list") result = "{\"data\":[],\"nextCursor\":\"" + (scenario == "cycle" ? "cycle" : "p" + registryPage++) + "\"}";
            if (scenario == "bad-config" && method == "config/read") result = Changed(result, "config/otel/metrics_exporter", "\"statsig\"");
            if (scenario == "bad-requirements" && method == "configRequirements/read") result = "{\"requirements\":{\"cliAuthCredentialsStore\":\"keyring\"}}";
            var id = request.GetProperty("id").GetInt32(); var envelope = "{\"id\":" + (scenario == "wrong-id" ? 999 : id) + ",\"result\":" + result + "}";
            if (scenario == "server-request") envelope = "{\"id\":9,\"method\":\"account/chatgptAuthTokens/refresh\",\"params\":{}}";
            if (scenario == "malformed") envelope = "{";
            if (scenario == "oversize") envelope = new string('x', 2 * 1024 * 1024 + 1);
            if (scenario == "stderr-lines") await Console.OpenStandardError().WriteAsync(Encoding.UTF8.GetBytes(new string('\n', 4097)));
            await output.WriteAsync(Encoding.UTF8.GetBytes(envelope + "\n")); await output.FlushAsync();
        }
        return scenario == "bad-exit" ? 17 : 0;
    }
}

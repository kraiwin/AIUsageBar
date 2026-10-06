using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using AIUsageBar.Core;

namespace AIUsageBar.Tests;

internal static class ClaudeTests
{
    private const string Bash = "C:/Program Files/Git/bin/bash.exe";
    public static IEnumerable<TestCase> Cases()
    {
        yield return TestCase.Sync("snapshot/quota-only-invalid-marker-old-receipt-recovery", () =>
        {
            using var scratch = new Scratch();
            using var store = new ClaudeSnapshotStore(Path.Combine(scratch.Root, "private"));
            var payload = Fixture.Utf8("{\"session_id\":\"SYNTHETIC-EXCLUDED\",\"workspace\":{\"secret\":\"SYNTHETIC-EXCLUDED\"},\"rate_limits\":{\"seven_day\":{\"used_percentage\":25,\"resets_at\":1791273600}}}");
            store.Capture(payload, Fixture.Now);
            Check.Equal(75d, store.Read(Fixture.Now).Reading!.Weekly.RemainingPercent);
            var persisted = File.ReadAllText(Path.Combine(scratch.Root, "private", ClaudeSnapshotStore.SnapshotName));
            Check.True(!persisted.Contains("SYNTHETIC-EXCLUDED", StringComparison.Ordinal));
            store.Capture("{"u8, Fixture.Now.AddSeconds(1));
            var error = store.Read(Fixture.Now.AddSeconds(2));
            Check.True(error.CaptureError); Check.Equal(Fixture.Now, error.ReceivedAt);
            Check.Equal(75d, error.Reading!.Weekly.RemainingPercent);
            store.Capture(Fixture.Claude("100"), Fixture.Now.AddSeconds(3));
            var repaired = store.Read(Fixture.Now.AddSeconds(3));
            Check.True(!repaired.CaptureError); Check.Equal(0d, repaired.Reading!.Weekly.RemainingPercent);
            Check.Equal(Fixture.Now.AddSeconds(3), repaired.ReceivedAt);
        });
        yield return TestCase.Sync("snapshot/no-data-tombstone-clears-quota", () =>
        {
            using var scratch = new Scratch();
            using var store = new ClaudeSnapshotStore(Path.Combine(scratch.Root, "private"));
            store.Capture(Fixture.Claude(), Fixture.Now); store.Capture("{}"u8, Fixture.Now.AddSeconds(1));
            var empty = store.Read(Fixture.Now.AddSeconds(1));
            Check.True(empty.Reading is null); Check.True(!empty.CaptureError);
            Check.Equal(Fixture.Now.AddSeconds(1), empty.ReceivedAt);
        });
        yield return TestCase.Sync("snapshot/older-future-receipt-oversize-never-freshen", () =>
        {
            using var scratch = new Scratch();
            using var store = new ClaudeSnapshotStore(Path.Combine(scratch.Root, "private"));
            store.Capture(Fixture.Claude(), Fixture.Now);
            store.Capture(Fixture.Claude("100"), Fixture.Now.AddSeconds(-1));
            Check.Equal(Fixture.Now, store.Read(Fixture.Now).ReceivedAt);
            store.Capture(Fixture.Claude("100"), DateTimeOffset.UtcNow.AddMinutes(1));
            Check.True(store.Read(Fixture.Now).CaptureError);
            Check.Equal(Fixture.Now, store.Read(Fixture.Now).ReceivedAt);
            store.Capture(new byte[2 * 1024 * 1024 + 1], Fixture.Now.AddSeconds(1));
            Check.Equal(Fixture.Now, store.Read(Fixture.Now).ReceivedAt);
        });
        yield return TestCase.Sync("snapshot/strict-schema-marker-and-future-read", () =>
        {
            using var scratch = new Scratch();
            var path = Path.Combine(scratch.Root, "private");
            using var store = new ClaudeSnapshotStore(path);
            store.Capture(Fixture.Claude(), Fixture.Now);
            Check.Throws<CoreException>(() => store.Read(Fixture.Now.AddSeconds(-1)));
            using var files = new PrivateFiles(path);
            files.AtomicWrite(ClaudeSnapshotStore.ErrorName, "{\"schemaVersion\":2,\"state\":\"capture-error\"}"u8);
            Check.Throws<CoreException>(() => store.Read(Fixture.Now));
            files.AtomicWrite(ClaudeSnapshotStore.ErrorName, "{\"schemaVersion\":1,\"state\":\"capture-error\",\"extra\":true}"u8);
            Check.Throws<CoreException>(() => store.Read(Fixture.Now));
            files.AtomicWrite(ClaudeSnapshotStore.SnapshotName, "{\"schemaVersion\":1,\"state\":\"quota\",\"receivedAt\":1790812800,\"weekly\":null}"u8);
            Check.Throws<CoreException>(() => store.Read(Fixture.Now));
        });
        yield return TestCase.Sync("files/open-reader-replacement-and-protected-DACL", () =>
        {
            using var scratch = new Scratch();
            var path = Path.Combine(scratch.Root, "private");
            using var files = new PrivateFiles(path);
            files.AtomicWrite("snapshot.json", "old-complete"u8);
            using var reader = files.OpenReader("snapshot.json");
            var before = reader.GetAccessControl();
            files.AtomicWrite("snapshot.json", "new-complete"u8);
            var oldBytes = new byte[reader.Length]; reader.ReadExactly(oldBytes);
            Check.Bytes("old-complete"u8, oldBytes); Check.Bytes("new-complete"u8, files.Read("snapshot.json"));
            using var current = files.OpenReader("snapshot.json");
            var descriptor = current.GetAccessControl();
            var oldRules = before.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
            var newRules = descriptor.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
            Check.Equal(before.GetOwner(typeof(SecurityIdentifier)), descriptor.GetOwner(typeof(SecurityIdentifier)));
            Check.True(oldRules.All(a => newRules.Any(b => a.IdentityReference.Equals(b.IdentityReference) && a.FileSystemRights == b.FileSystemRights && a.AccessControlType == b.AccessControlType && a.InheritanceFlags == b.InheritanceFlags && a.PropagationFlags == b.PropagationFlags)));
            Check.True(descriptor.AreAccessRulesProtected);
            var user = WindowsIdentity.GetCurrent().User!;
            var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            var identities = descriptor.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().Select(r => r.IdentityReference).ToArray();
            Check.Equal(2, identities.Length); Check.True(identities.Contains(user) && identities.Contains(system));
        });
        yield return TestCase.Sync("files/deterministic/public-create-replace-cleanup", () =>
        {
            using var scratch = new Scratch();
            using var files = new PrivateFiles(Path.Combine(scratch.Root, "private"));
            var original = Enumerable.Repeat((byte)'A', 64).ToArray();
            var proposed = Enumerable.Repeat((byte)'N', 64).ToArray();
            files.AtomicWrite("snapshot.json", original);
            Check.Bytes(original, InspectPrivateFile(files, "snapshot.json").Bytes);
            CheckPublicationCleanup(files.Root);
            files.AtomicWrite("snapshot.json", proposed);
            Check.Bytes(proposed, InspectPrivateFile(files, "snapshot.json").Bytes);
            CheckPublicationCleanup(files.Root);
        });
        yield return TestCase.Sync("files/deterministic/noop-create-zero-replace-once-instance-isolation", () =>
        {
            using var scratch = new Scratch();
            var path = Path.Combine(scratch.Root, "private");
            int callbacks = 0;
            using var hooked = new PrivateFiles(path, () => callbacks++);
            hooked.AtomicWrite("snapshot.json", "original"u8);
            Check.Equal(0, callbacks);
            Check.Bytes("original"u8, InspectPrivateFile(hooked, "snapshot.json").Bytes);
            CheckPublicationCleanup(path);
            hooked.AtomicWrite("snapshot.json", "proposed"u8);
            Check.Equal(1, callbacks);
            Check.Bytes("proposed"u8, InspectPrivateFile(hooked, "snapshot.json").Bytes);
            CheckPublicationCleanup(path);
            // The public instance shares the root, but must not inherit the hook.
            using var ordinary = new PrivateFiles(path);
            ordinary.AtomicWrite("public.json", "first-public"u8);
            Check.Bytes("first-public"u8, InspectPrivateFile(ordinary, "public.json").Bytes);
            Check.Equal(1, callbacks);
            ordinary.AtomicWrite("public.json", "second-public"u8);
            Check.Bytes("second-public"u8, InspectPrivateFile(ordinary, "public.json").Bytes);
            ordinary.AtomicWrite("snapshot.json", "public-later"u8);
            Check.Bytes("public-later"u8, InspectPrivateFile(ordinary, "snapshot.json").Bytes);
            Check.Equal(1, callbacks);
            CheckPublicationCleanup(path);
        });
        yield return TestCase.Sync("files/deterministic/external-replacement-retains-exact-editor-recovery", () => DeterministicRecovery(sameIdentityEdit: false));
        yield return TestCase.Sync("files/deterministic/same-ID-equal-length-flushed-edit-retains-exact-recovery", () => DeterministicRecovery(sameIdentityEdit: true));
        yield return TestCase.Sync("files/pinned-ancestor-blocks-rename", () =>
        {
            using var scratch = new Scratch();
            var ancestor = Directory.CreateDirectory(Path.Combine(scratch.Root, "ancestor"));
            using var files = new PrivateFiles(Path.Combine(ancestor.FullName, "private"));
            Check.Throws<IOException>(() => Directory.Move(ancestor.FullName, ancestor.FullName + "-moved"));
            files.AtomicWrite("snapshot.json", "ok"u8); Check.Bytes("ok"u8, files.Read("snapshot.json"));
        });
        yield return new("files/concurrent-first-publish-poll-complete-bytes-or-bounded-unavailable", async () =>
        {
            using var scratch = new Scratch();
            var path = Path.Combine(scratch.Root, "private");
            using var first = new PrivateFiles(path); using var second = new PrivateFiles(path);
            var a = Enumerable.Repeat((byte)'a', 4096).ToArray(); var b = Enumerable.Repeat((byte)'b', 4096).ToArray();
            await Task.WhenAll(Task.Run(() => first.AtomicWrite("snapshot.json", a)), Task.Run(() => second.AtomicWrite("snapshot.json", b)));
            var sample = first.Read("snapshot.json"); Check.True(sample.SequenceEqual(a) || sample.SequenceEqual(b));
            var writer = Task.Run(() => { for (int i = 0; i < 20; i++) first.AtomicWrite("snapshot.json", (i & 1) == 0 ? a : b); });
            int completeReads = 0, unavailableReads = 0;
            try
            {
                while (!writer.IsCompleted)
                {
                    try { sample = second.Read("snapshot.json"); Check.True(sample.SequenceEqual(a) || sample.SequenceEqual(b)); completeReads++; }
                    catch (FileNotFoundException) { unavailableReads++; }
                    catch (CoreException exception) when (exception.Category is "read-conflict" or "read-failure") { unavailableReads++; }
                }
            }
            finally { await writer; }
            sample = second.Read("snapshot.json"); Check.True(sample.SequenceEqual(a) || sample.SequenceEqual(b));
            Console.WriteLine($"EVIDENCE concurrentCompleteReads={completeReads} boundedUnavailableReads={unavailableReads} finalComplete=true");
        });
        yield return TestCase.Sync("files/reject-hardlink-and-unsafe-existing-root", () =>
        {
            using var scratch = new Scratch();
            Check.Throws<CoreException>(() => { using var _ = new PrivateFiles(scratch.Root); });
            var path = Path.Combine(scratch.Root, "private"); using var files = new PrivateFiles(path);
            files.AtomicWrite("snapshot.json", "unchanged"u8);
            Check.True(CreateHardLinkW(Path.Combine(path, "alias.json"), Path.Combine(path, "snapshot.json"), 0));
            Check.Throws<CoreException>(() => files.Read("snapshot.json"));
            Check.Throws<CoreException>(() => files.AtomicWrite("snapshot.json", "new"u8));
            Check.Bytes("unchanged"u8, File.ReadAllBytes(Path.Combine(path, "alias.json")));
        });
        yield return new("files/reject-junction-ancestor", async () =>
        {
            using var scratch = new Scratch();
            var destination = Directory.CreateDirectory(Path.Combine(scratch.Root, "destination")).FullName;
            var junction = Path.Combine(scratch.Root, "junction");
            var commandInterpreter = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
            using var child = TestProcess.Start(commandInterpreter, [], scratch.Root, commandLine:
                WindowsProcess.QuoteArgument(commandInterpreter) + " /d /c mklink /J " + WindowsProcess.QuoteArgument(junction) + " " + WindowsProcess.QuoteArgument(destination));
            var stdout = Drain(child.Output); var stderr = Drain(child.Error); child.Input.Dispose();
            await child.Observed.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); await Task.WhenAll(stdout, stderr);
            Check.Equal(0, child.Observed.ExitCode);
            try { Check.Throws<CoreException>(() => { using var _ = new PrivateFiles(Path.Combine(junction, "private")); }); }
            finally { Directory.Delete(junction); }
            Check.True(!Directory.Exists(Path.Combine(destination, "private")));
        });
        yield return new("sink/inert-noargs-no-output-no-writes", async () =>
        {
            using var scratch = new Scratch();
            var result = await Sink(scratch, [], [], closeInput: true);
            Check.True(result.Exit != 0); Check.Equal(0, result.Out.Length); Check.Equal(0, result.Err.Length);
            Check.Equal(0, Directory.GetFileSystemEntries(scratch.Root).Length);
        });
        yield return new("sink/EOF-publishes-and-overflow-marks-without-raw-persistence", async () =>
        {
            using var scratch = new Scratch(); var target = Path.Combine(scratch.Root, "private");
            var result = await Sink(scratch, ["--capture-test", "--scratch", target], Fixture.Claude(), true);
            Check.Equal(0, result.Exit); Check.Equal(0, result.Out.Length + result.Err.Length);
            using var store = new ClaudeSnapshotStore(target); var receipt = store.Read(DateTimeOffset.UtcNow).ReceivedAt;
            result = await Sink(scratch, ["--capture-test", "--scratch", target], new byte[2 * 1024 * 1024 + 1], true);
            Check.Equal(0, result.Exit); Check.True(store.Read(DateTimeOffset.UtcNow).CaptureError);
            Check.Equal(receipt, store.Read(DateTimeOffset.UtcNow).ReceivedAt);
            result = await Sink(scratch, ["--capture-test", "--scratch", target], "{}"u8.ToArray(), true);
            Check.Equal(0, result.Exit); Check.True(!store.Read(DateTimeOffset.UtcNow).CaptureError);
            Check.True(store.Read(DateTimeOffset.UtcNow).Reading is null);
        });
        yield return new("sink/complete-JSON-noEOF-deadline-no-ingest-old-receipt-marker-preserved", async () =>
        {
            using var scratch = new Scratch(); var target = Path.Combine(scratch.Root, "private");
            using var store = new ClaudeSnapshotStore(target);
            store.Capture(Fixture.Claude(), Fixture.Now); store.Capture("{"u8, Fixture.Now.AddSeconds(1));
            var before = Directory.GetFiles(target).ToDictionary(f => Path.GetFileName(f), File.ReadAllBytes);
            var result = await Sink(scratch, ["--capture-test", "--scratch", target], Fixture.Claude("100"), false);
            Check.Equal(2, result.Exit); Check.True(result.Duration < TimeSpan.FromSeconds(3));
            foreach (var file in before) Check.Bytes(file.Value, File.ReadAllBytes(Path.Combine(target, file.Key!)));
            Check.Equal(before.Count, Directory.GetFiles(target).Length);
            Check.Equal(Fixture.Now, store.Read(DateTimeOffset.UtcNow).ReceivedAt);
            Check.True(store.Read(DateTimeOffset.UtcNow).CaptureError);
            result = await Sink(scratch, ["--capture-test", "--scratch", target], Fixture.Claude("100"), true);
            Check.Equal(0, result.Exit); Check.True(!store.Read(DateTimeOffset.UtcNow).CaptureError);
        });
    }

    private readonly record struct FileIdentity(uint Volume, uint High, uint Low);
    private sealed record PrivateFileEvidence(FileIdentity Identity, byte[] Bytes);
    private static FileIdentity Identity(PrivateFiles.FileInfoByHandle info) => new(info.Volume, info.FileIndexHigh, info.FileIndexLow);

    // A complete handle read plus explicit semantic checks; no inspection handle
    // escapes this method, so later pathname survival cannot be an orphan-handle illusion.
    private static PrivateFileEvidence InspectPrivateFile(PrivateFiles files, string name)
    {
        using var stream = files.OpenReader(name);
        var before = PrivateFiles.Information(stream.SafeFileHandle);
        Check.Equal(0u, before.Attributes & (0x400u | 0x10u));
        Check.Equal(1u, before.Links);
        var security = stream.GetAccessControl();
        using var currentIdentity = WindowsIdentity.GetCurrent();
        var user = currentIdentity.User!;
        var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        Check.True(security.AreAccessRulesProtected);
        Check.Equal(user, security.GetOwner(typeof(SecurityIdentifier)));
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
        Check.Equal(2, rules.Length);
        Check.Equal(1, rules.Count(rule => rule.IdentityReference.Equals(user)));
        Check.Equal(1, rules.Count(rule => rule.IdentityReference.Equals(system)));
        foreach (var rule in rules)
        {
            Check.Equal(AccessControlType.Allow, rule.AccessControlType);
            Check.Equal(FileSystemRights.FullControl, rule.FileSystemRights);
            Check.True(!rule.IsInherited);
            Check.Equal(InheritanceFlags.None, rule.InheritanceFlags);
            Check.Equal(PropagationFlags.None, rule.PropagationFlags);
        }
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        Check.Equal((long)bytes.Length, stream.Length);
        var after = PrivateFiles.Information(stream.SafeFileHandle);
        Check.Equal(Identity(before), Identity(after));
        Check.Equal(before.SizeHigh, after.SizeHigh); Check.Equal(before.SizeLow, after.SizeLow);
        Check.Equal(before.Written.dwHighDateTime, after.Written.dwHighDateTime);
        Check.Equal(before.Written.dwLowDateTime, after.Written.dwLowDateTime);
        Check.Equal(before.Attributes, after.Attributes); Check.Equal(1u, after.Links);
        return new(Identity(before), bytes);
    }

    private static HashSet<string> RecoveryPaths(string root) => Directory.GetFiles(root, "recovery-*").ToHashSet(StringComparer.OrdinalIgnoreCase);
    private static void CheckPublicationCleanup(string root)
    {
        Check.Equal(0, RecoveryPaths(root).Count);
        Check.Equal(0, Directory.GetFiles(root, "temp-*").Length);
    }

    private static void DeterministicRecovery(bool sameIdentityEdit)
    {
        using var scratch = new Scratch();
        var path = Path.Combine(scratch.Root, "private");
        using var ordinary = new PrivateFiles(path);
        var originalBytes = Enumerable.Repeat((byte)'A', 64).ToArray();
        var editorBytes = Enumerable.Repeat((byte)'E', 64).ToArray();
        var proposedBytes = Enumerable.Repeat((byte)'N', 64).ToArray();
        var laterBytes = Enumerable.Repeat((byte)'L', 64).ToArray();
        Check.True(!originalBytes.SequenceEqual(editorBytes) && !originalBytes.SequenceEqual(proposedBytes) && !editorBytes.SequenceEqual(proposedBytes));
        Check.Equal(originalBytes.Length, editorBytes.Length);
        ordinary.AtomicWrite("snapshot.json", originalBytes);
        var original = InspectPrivateFile(ordinary, "snapshot.json");
        Check.Bytes(originalBytes, original.Bytes);
        // A pre-existing recovery ensures the test cannot choose an arbitrary first file.
        ordinary.AtomicWrite("recovery-prior", "previous-retained-evidence"u8);
        var prior = InspectPrivateFile(ordinary, "recovery-prior");
        FileIdentity expectedRecoveryIdentity;
        if (sameIdentityEdit) expectedRecoveryIdentity = original.Identity;
        else
        {
            ordinary.AtomicWrite("editor-stage", editorBytes);
            var stagedEditor = InspectPrivateFile(ordinary, "editor-stage");
            Check.Bytes(editorBytes, stagedEditor.Bytes);
            Check.True(stagedEditor.Identity != original.Identity);
            expectedRecoveryIdentity = stagedEditor.Identity;
        }
        var beforeRecoveries = RecoveryPaths(path);
        Check.Equal(1, beforeRecoveries.Count);
        int callbacks = 0; bool adversarySucceeded = false;
        using var hooked = new PrivateFiles(path, () =>
        {
            callbacks++;
            // This reader is also closed before mutation. Direct OS edits ignore writer.lock.
            var atBoundary = InspectPrivateFile(ordinary, "snapshot.json");
            Check.Equal(original.Identity, atBoundary.Identity); Check.Bytes(originalBytes, atBoundary.Bytes);
            if (sameIdentityEdit)
            {
                using (var editor = new FileStream(Path.Combine(path, "snapshot.json"), FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                {
                    Check.Equal(original.Identity, Identity(PrivateFiles.Information(editor.SafeFileHandle)));
                    Check.Equal((long)editorBytes.Length, editor.Length);
                    editor.Write(editorBytes); editor.Flush(flushToDisk: true);
                    Check.Equal((long)editorBytes.Length, editor.Length);
                    Check.Equal(original.Identity, Identity(PrivateFiles.Information(editor.SafeFileHandle)));
                }
            }
            else
            {
                Check.True(MoveFileExW(Path.Combine(path, "editor-stage"), Path.Combine(path, "snapshot.json"), 1));
                Check.True(!File.Exists(Path.Combine(path, "editor-stage")));
            }
            var edited = InspectPrivateFile(ordinary, "snapshot.json");
            Check.Equal(expectedRecoveryIdentity, edited.Identity); Check.Bytes(editorBytes, edited.Bytes);
            adversarySucceeded = true;
        });
        var conflict = Check.Throws<CoreException>(() => hooked.AtomicWrite("snapshot.json", proposedBytes));
        Check.Equal("publish-conflict", conflict.Category);
        Check.Equal(1, callbacks); Check.True(adversarySucceeded);
        Check.Bytes(proposedBytes, InspectPrivateFile(ordinary, "snapshot.json").Bytes);
        var afterRecoveries = RecoveryPaths(path);
        var added = afterRecoveries.Except(beforeRecoveries, StringComparer.OrdinalIgnoreCase).ToArray();
        Check.Equal(1, added.Length); Check.Equal(beforeRecoveries.Count + 1, afterRecoveries.Count);
        var exactRecoveryPath = added.Single();
        var initialRecovery = InspectPrivateFile(ordinary, Path.GetFileName(exactRecoveryPath));
        // Independently captured A/E identity and complete E bytes are the oracle,
        // before this initial recovery can serve as a later survival baseline.
        Check.Equal(expectedRecoveryIdentity, initialRecovery.Identity);
        Check.Bytes(editorBytes, initialRecovery.Bytes);
        Check.Equal(0, Directory.GetFiles(path, "temp-*").Length);
        // ALL file inspection/edit handles are closed. Use the PUBLIC constructor
        // instance for a later publication, then open the exact original pathname anew.
        ordinary.AtomicWrite("snapshot.json", laterBytes);
        Check.Equal(1, callbacks);
        Check.Bytes(laterBytes, InspectPrivateFile(ordinary, "snapshot.json").Bytes);
        Check.True(File.Exists(exactRecoveryPath));
        var reopenedRecovery = InspectPrivateFile(ordinary, Path.GetFileName(exactRecoveryPath));
        Check.Equal(expectedRecoveryIdentity, reopenedRecovery.Identity);
        Check.Equal(initialRecovery.Identity, reopenedRecovery.Identity);
        Check.Bytes(editorBytes, reopenedRecovery.Bytes); Check.Bytes(initialRecovery.Bytes, reopenedRecovery.Bytes);
        Check.True(afterRecoveries.SetEquals(RecoveryPaths(path)));
        var priorAfter = InspectPrivateFile(ordinary, "recovery-prior");
        Check.Equal(prior.Identity, priorAfter.Identity); Check.Bytes(prior.Bytes, priorAfter.Bytes);
        Check.Equal(0, Directory.GetFiles(path, "temp-*").Length);
        Console.WriteLine($"EVIDENCE deterministicRace={(sameIdentityEdit ? "same-ID-content" : "external-replacement")} callbackCount={callbacks} adversarySucceeded=true typedPublishConflict=true targetN=true newRecoveryCount={added.Length} initialRecoveryMatchesOracle=true closedInspectionHandlesBeforePublicWrite=true exactRecoveryPathReopened=true retainedIdentityBytesSecurity=true universalCAS=false");
    }

    // Deliberately a separate timing-dependent experiment, never a hidden skip
    // or a flaky requirement in the deterministic mandatory offline suite.
    public static IEnumerable<TestCase> FileCases()
    {
        yield return new("files/external-editor-replacement-stress-retains-ambiguous-recovery", async () =>
        {
            using var scratch = new Scratch(); var path = Path.Combine(scratch.Root, "private");
            using var files = new PrivateFiles(path);
            var app = Enumerable.Repeat((byte)'A', 512 * 1024).ToArray(); var editor = Enumerable.Repeat((byte)'E', 512 * 1024).ToArray();
            files.AtomicWrite("snapshot.json", app);
            // Protected ACLs are identical; only the editor ignores our writer lock.
            for (int i = 0; i < 150; i++) files.AtomicWrite("editor-" + i, editor);
            int conflicts = 0, racedMoves = 0;
            var racer = Task.Run(() =>
            {
                for (int i = 0; i < 150; i++)
                {
                    if (MoveFileExW(Path.Combine(path, "editor-" + i), Path.Combine(path, "snapshot.json"), 1)) Interlocked.Increment(ref racedMoves);
                    Thread.Sleep(1);
                }
            });
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    try { files.AtomicWrite("snapshot.json", app); }
                    catch (CoreException exception) when (exception.Category is "publish-conflict" or "read-failure") { conflicts++; }
                    catch (FileNotFoundException) { conflicts++; }
                }
            }
            finally { await racer; }
            var recoveries = Directory.GetFiles(path, "recovery-*"); int displacedEditor = 0;
            foreach (var recovery in recoveries)
            {
                var retained = File.ReadAllBytes(recovery); Check.True(retained.SequenceEqual(app) || retained.SequenceEqual(editor));
                if (retained.SequenceEqual(editor)) displacedEditor++;
            }
            Console.WriteLine($"EVIDENCE editorMoves={racedMoves} writeConflicts={conflicts} retainedRecoveries={recoveries.Length} retainedDisplacedEditor={displacedEditor} universalCAS=false");
            if (displacedEditor == 0) Console.WriteLine("INCONCLUSIVE boundary=external-editor-between-validation-and-replace no-guaranteed-race-observed");
            Check.True(racedMoves > 0 && conflicts > 0 && displacedEditor > 0);
            files.AtomicWrite("snapshot.json", app); Check.Bytes(app, files.Read("snapshot.json", 512 * 1024));
        });
    }

    private sealed record ProcessResult(int Exit, byte[] Out, byte[] Err, TimeSpan Duration);
    private static async Task<ProcessResult> Sink(Scratch scratch, string[] args, byte[] bytes, bool closeInput)
    {
        using var child = TestProcess.Start(Fixture.Bridge, args, scratch.Root);
        var timer = Stopwatch.StartNew(); var output = Drain(child.Output); var error = Drain(child.Error);
        await child.Input.WriteAsync(bytes).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        if (closeInput) child.Input.Dispose();
        await child.Observed.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        return new(child.Observed.ExitCode, await output.WaitAsync(TimeSpan.FromSeconds(1)), await error.WaitAsync(TimeSpan.FromSeconds(1)), timer.Elapsed);
    }
    private static async Task<byte[]> Drain(Stream stream)
    { using var memory = new MemoryStream(); await stream.CopyToAsync(memory); return memory.ToArray(); }

    public static IEnumerable<TestCase> TeeCases()
    {
        yield return new("tee/pinned-tool-metadata", async () =>
        {
            using var scratch = new Scratch();
            foreach (var executable in new[] { Bash, "C:/Program Files/Git/usr/bin/bash.exe", "C:/Program Files/Git/usr/bin/tee.exe" })
            {
                var tuple = CodexDiscovery.Inspect(executable, "synthetic-tool-metadata-only");
                Console.WriteLine($"EVIDENCE toolPath={executable} fileID={tuple.FileId} SHA256={tuple.Sha256}");
            }
            using (var syntax = TestProcess.Start(Bash, ["--noprofile", "--norc", "-n", Path.Combine(AppContext.BaseDirectory, "claude-tee-experiment.sh")], scratch.Root, BashEnvironment(scratch, "syntax")))
            {
                var syntaxOut = Drain(syntax.Output); var syntaxErr = Drain(syntax.Error); syntax.Input.Dispose();
                await syntax.Observed.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                Check.Equal(0, syntax.Observed.ExitCode); Check.Equal(0, (await syntaxOut).Length + (await syntaxErr).Length);
            }
            using var child = TestProcess.Start(Bash, ["--noprofile", "--norc", "-c", "/usr/bin/tee --version | /usr/bin/head -n 1"], scratch.Root, BashEnvironment(scratch, "metadata"));
            var output = Drain(child.Output); var error = Drain(child.Error); child.Input.Dispose();
            await child.Observed.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            var version = Encoding.UTF8.GetString(await output);
            Check.True(version.StartsWith("tee (GNU coreutils)", StringComparison.Ordinal));
            Console.WriteLine($"EVIDENCE teeVersion={version.Trim()} bashSHA256={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Bash)))}");
            Check.Equal(0, (await error).Length);
        });
        foreach (var candidate in new[] { "substitution", "pipeline" })
        foreach (var scenario in new[] { "normal", "finite-held-open", "EOF-scheduled", "overflow", "early-exit", "exit0", "exit7", "exit255", "parallel-streams", "quoting", "missing-apphost", "missing-DLL", "missing-deps", "missing-runtimeconfig", "missing-runtime", "slow-runtime", "hung-sink", "invalid-parse", "locked-store" })
        {
            var shape = candidate; var mode = scenario;
            yield return new($"tee/{shape}/{mode}-paired-streams-exit-singleexecution-EOF", () => TeePair(shape, mode));
        }
        foreach (var candidate in new[] { "substitution", "pipeline" })
        foreach (var termination in new[] { "TerminateProcess", "MSYS-TERM" })
        {
            var shape = candidate; var kill = termination;
            yield return new($"tee/{shape}/{kill}-all-descendants-two-second-gate", () => TeeCancellation(shape, kill));
        }
    }

    private static Dictionary<string, string> BashEnvironment(Scratch scratch, string counter)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return new() { ["SystemRoot"] = windows, ["WINDIR"] = windows,
            ["PATH"] = "C:/Program Files/Git/usr/bin;" + Path.Combine(windows, "System32"),
            ["HOME"] = scratch.Root, ["USERPROFILE"] = scratch.Root, ["TEMP"] = scratch.Root, ["TMP"] = scratch.Root,
            ["MSYS2_ARG_CONV_EXCL"] = "*", ["AUB_COUNT"] = Path.Combine(scratch.Root, counter).Replace('\\', '/') };
    }
    private static string[] CandidateArguments(string candidate, string original, string sink, string target, string fault) =>
        ["--noprofile", "--norc", Path.Combine(AppContext.BaseDirectory, "claude-tee-experiment.sh").Replace('\\', '/'), candidate, original, sink.Replace('\\', '/'), target.Replace('\\', '/'), fault];

    private static async Task TeePair(string shape, string mode)
    {
        using var scratch = new Scratch();
        byte[] bytes = mode is "overflow" or "early-exit" ? Enumerable.Repeat((byte)'x', 3 * 1024 * 1024).ToArray() : Fixture.Claude();
        if (mode == "invalid-parse") bytes = "{"u8.ToArray();
        var exitCode = mode switch { "exit0" => 0, "exit7" => 7, "exit255" => 255, _ => 37 };
        var original = "printf x >> \"$AUB_COUNT\"; " + (mode == "parallel-streams" ? "(/usr/bin/head -c 262144 /dev/zero >&2) & " : "") +
            (mode == "early-exit" ? ":" : mode == "EOF-scheduled" ? "/usr/bin/cat" : $"/usr/bin/head -c {bytes.Length}") +
            (mode == "parallel-streams" ? "; /usr/bin/head -c 262144 /dev/zero; wait" : "") +
            (mode == "quoting" ? "; printf \"ไทย ' quote \\\\ path\\n\"" : "") + $"; printf 'stderr\\000bytes\\n' >&2; exit {exitCode}";
        var sink = Fixture.Bridge;
        string? deployment = null;
        if (mode.StartsWith("missing-", StringComparison.Ordinal))
        {
            deployment = Directory.CreateDirectory(Path.Combine(scratch.Root, "deployment")).FullName;
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(Fixture.Bridge)!)) File.Copy(file, Path.Combine(deployment, Path.GetFileName(file)));
            var excluded = mode switch { "missing-apphost" => "AIUsageBar.Bridge.exe", "missing-DLL" => "AIUsageBar.Bridge.dll",
                "missing-deps" => "AIUsageBar.Bridge.deps.json", "missing-runtimeconfig" => "AIUsageBar.Bridge.runtimeconfig.json", _ => null };
            if (excluded is not null) File.Delete(Path.Combine(deployment, excluded));
            if (mode == "missing-runtime") File.WriteAllText(Path.Combine(deployment, "AIUsageBar.Bridge.runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net999.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"999.0.0\"}}}");
            sink = Path.Combine(deployment, "AIUsageBar.Bridge.exe");
        }
        var target = Path.Combine(scratch.Root, "capture");
        FileStream? heldLock = null;
        PrivateFiles? lockingFiles = null;
        if (mode == "locked-store")
        {
            lockingFiles = new PrivateFiles(target);
            heldLock = lockingFiles.AcquireLock();
        }
        try
        {
            var baseline = await TeeRun(scratch, ["--noprofile", "--norc", "-c", original], "baseline", mode, bytes);
            var candidate = await TeeRun(scratch, CandidateArguments(shape, original, sink, target,
                mode == "slow-runtime" ? "slow" : mode == "hung-sink" ? "hung" : "none"), "candidate", mode, bytes);
            Check.Bytes(baseline.Out, candidate.Out); Check.Bytes(baseline.Err, candidate.Err); Check.Equal(exitCode, baseline.Exit); Check.Equal(baseline.Exit, candidate.Exit);
            Check.Equal("x", File.ReadAllText(Path.Combine(scratch.Root, "baseline")));
            Check.Equal("x", File.ReadAllText(Path.Combine(scratch.Root, "candidate")));
            var exitDelta = candidate.ExitMs - baseline.ExitMs; var eofDelta = candidate.EofMs - baseline.EofMs;
            Console.WriteLine($"EVIDENCE teeShape={shape} case={mode} bytes={candidate.Out.Length} exitDeltaMs={exitDelta} eofDeltaMs={eofDelta} ownedBeforeCleanup={candidate.Owned}");
            Check.True(exitDelta <= 500 && eofDelta <= 500);
        }
        finally { heldLock?.Dispose(); lockingFiles?.Dispose(); }
    }

    private sealed record TeeResult(int Exit, byte[] Out, byte[] Err, long ExitMs, long EofMs, int Owned);
    private static async Task<TeeResult> TeeRun(Scratch scratch, string[] arguments, string counter, string mode, byte[] bytes)
    {
        using var child = TestProcess.Start(Bash, arguments, scratch.Root, BashEnvironment(scratch, counter));
        var timer = Stopwatch.StartNew(); long eofMs = -1, closeMs = 0;
        var output = Drain(child.Output); var error = Drain(child.Error);
        var eof = Task.Run(async () => { await Task.WhenAll(output, error); eofMs = timer.ElapsedMilliseconds; });
        var producer = Task.Run(async () =>
        {
            try { await child.Input.WriteAsync(bytes); }
            catch (IOException) { /* early-exit original closes input */ }
            catch (ObjectDisposedException) { /* harness closes held-open producer only after observing original EOF */ }
            if (mode == "EOF-scheduled") await Task.Delay(500);
            if (mode != "finite-held-open") { closeMs = timer.ElapsedMilliseconds; child.Input.Dispose(); }
        });
        await child.Observed.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        var exitMs = timer.ElapsedMilliseconds;
        await eof.WaitAsync(TimeSpan.FromSeconds(5));
        int owned = child.JobPids().Length;
        child.Input.Dispose(); await producer.WaitAsync(TimeSpan.FromSeconds(1));
        return new(child.Observed.ExitCode, await output, await error,
            mode == "EOF-scheduled" ? exitMs - closeMs : exitMs, mode == "EOF-scheduled" ? eofMs - closeMs : eofMs, owned);
    }

    private static async Task TeeCancellation(string shape, string termination)
    {
        using var scratch = new Scratch();
        var original = "printf x >> \"$AUB_COUNT\"; /usr/bin/sleep 30 & /usr/bin/cat; wait";
        // Record baseline leakage separately; it never exempts candidate roles.
        int baseline = await ObserveDeath(scratch, ["--noprofile", "--norc", "-c", original], "baseline", termination, false);
        int candidate = await ObserveDeath(scratch, CandidateArguments(shape, original, Fixture.Bridge, Path.Combine(scratch.Root, "private"), "none"), "candidate", termination, true);
        Console.WriteLine($"EVIDENCE teeShape={shape} termination={termination} baselineSurvivors={baseline} candidateSurvivors={candidate} observedBeforeJobCleanup=true");
        if (candidate != 0) Console.WriteLine($"UNSUPPORTED teeShape={shape} reason=owned-descendant-survives-two-seconds");
        Check.Equal(0, candidate);
    }
    private static async Task<int> ObserveDeath(Scratch scratch, string[] arguments, string counter, string termination, bool candidate)
    {
        using var child = TestProcess.Start(Bash, arguments, scratch.Root, BashEnvironment(scratch, counter));
        var output = Drain(child.Output); var error = Drain(child.Error);
        await child.Input.WriteAsync(Fixture.Claude());
        var wait = Stopwatch.StartNew();
        while (!File.Exists(Path.Combine(scratch.Root, counter)) && wait.Elapsed < TimeSpan.FromSeconds(3)) await Task.Delay(10);
        Check.True(File.Exists(Path.Combine(scratch.Root, counter)));
        await Task.Delay(150); // lets finite helper startup join the owned job
        var ownedBefore = child.JobPids();
        Console.WriteLine($"EVIDENCE cancellationCandidate={candidate} top={child.Id} tree={string.Join(',', ownedBefore.Select(SafeImage))}");
        if (termination == "TerminateProcess") child.KillTop();
        else
        {
            // bin/bash.exe is a native redirector; signal the proven MSYS host
            // whose Windows parent is this exact redirector, not a process name.
            var parents = TestProcess.ParentPids();
            Console.WriteLine($"EVIDENCE ownedParentTree={string.Join(',', ownedBefore.Select(id => id + ":" + parents.GetValueOrDefault(id) + ":" + IsMsysBash(id)))}");
            var shells = ownedBefore.Where(IsMsysBash).ToHashSet();
            var target = shells.Single(id => parents.GetValueOrDefault(id) == child.Id);
            Console.WriteLine($"EVIDENCE msysSignalTarget={target} nativeRedirector={child.Id}");
            var command = $"/usr/bin/kill -W -TERM {target}";
            using var signal = TestProcess.Start(Bash, ["--noprofile", "--norc", "-c", command], scratch.Root, BashEnvironment(scratch, "signal"));
            signal.Input.Dispose(); var signalOut = Drain(signal.Output); var signalErr = Drain(signal.Error);
            await signal.Observed.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
            await Task.WhenAll(signalOut, signalErr); Check.Equal(0, signal.Observed.ExitCode);
        }
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(20);
        var survivors = child.JobPids().Where(pid => pid != child.Id).ToArray();
        Console.WriteLine($"EVIDENCE cancellationCandidate={candidate} survivors={string.Join(',', survivors.Select(SafeImage))} callerEOF={output.IsCompleted && error.IsCompleted}");
        // Harness job is still alive here. Finally/Dispose cleans only our job.
        return survivors.Length;
    }
    private static string SafeImage(int id)
    {
        try { using var p = Process.GetProcessById(id); return id + ":" + Path.GetFileName(p.MainModule!.FileName); }
        catch (ArgumentException) { return id + ":exited"; }
        catch (Win32Exception) { return id + ":unavailable"; }
    }
    private static bool IsMsysBash(int id)
    { using var p = Process.GetProcessById(id); return string.Equals(Path.GetFullPath(p.MainModule!.FileName), "C:\\Program Files\\Git\\usr\\bin\\bash.exe", StringComparison.OrdinalIgnoreCase); }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateHardLinkW(string newName, string existingName, nint security);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool MoveFileExW(string source, string target, uint flags);
}

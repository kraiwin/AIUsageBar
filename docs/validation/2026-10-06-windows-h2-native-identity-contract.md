# H2 native identity/cleanup contract — implementation design only

2026-10-06. Read-only design for the native implementation PLAN. No C# edits,
build/tests, vendor CLI, owner-file/token probe or native process occurred.
The [observation manifest](2026-10-06-windows-h2-observation-manifest.md) and
its nine loader-context gates remain authoritative. This contributor proposal
defines a finite handle/path/cleanup design, not an independent review. It does
not authorize native entry or claim runtime identity, isolation or no-access evidence.

## Current seams and smallest change

`WindowsProcess.cs:44–98` already owns the exact CreateProcessW process/thread,
assigns a limit-one kill-on-close/no-breakaway job while suspended and resumes
only afterward. Its local image pin ends when Start returns. Job accounting is
available before Dispose closes the job (`:109–133`). Its existing live accounting
queries/polls remain uncached; this design adds one copied terminal evidence
snapshot, not a one-total-query rule. Existing H2 fake helpers
reopen the primary by PID (`H2InventoryTests.cs:283–301`); the native path should
duplicate the original owned handle instead. `CodexDiscovery.cs:28–47` inspects
the nominated path but closes its stream. `PrivateFiles.cs:44–50` pins ordinary
ancestors with sharing3, denying delete but permitting write sharing; do not
reuse that policy as proof against reparse mutation for an executable chain.

Two Core files suffice; retain public discovery and transport behavior:

| File | Future delta | Producer → consumer → cleanup |
|---|---|---|
| `windows/src/AIUsageBar.Core/NativeLaunchBinding.cs` NEW | Internal sealed per-instance binding/lease, image/ordinary namespace pins, exact-primary duplicate, safe state/snapshots | Acquire nominated manifest → internal Start/checks/coordinator → dispose after signal/recovery decision |
| `windows/src/AIUsageBar.Core/WindowsProcess.cs` UPDATE | Internal Start overload and optional binding; duplicate/check before resume; one copied terminal snapshot just before job close after cleanup attempt; separate failure termination/signal record | Original CreateProcess handle/job → binding-owned duplicate/evidence → exact owned cleanup, never PID reopening |
| `windows/tests/AIUsageBar.Tests/H2NativeIdentityTests.cs` NEW | Framework-only fake-image/process/adversarial namespace/failure/cleanup cases | Independent fixture → internal API → assert signal/counters/releases, no real provider |
| `windows/tests/AIUsageBar.Tests/H2NativeAdmission.cs` NEW (architect integrates) | Same-run metadata/H0 and immutable admission/context checks | Reviewed metadata source/fixture manifest → native coordinator gate → close own token/path handles; no owner contents |
| `windows/tests/AIUsageBar.Tests/Program.cs` UPDATE only at approved implementation dispatch | Register explicit fake identity/admission tests | Offline runner → nonzero focused selection; no native activation from default/offline |

The architect owns the separate runner/fixture/output file list. Identity design
does not require edits to CodexDiscovery, PrivateFiles, CodexPolicy, public
FakeCodexProvider or JsonRpcTransport. Existing InternalsVisibleTo in
`AIUsageBar.Core.csproj` grants test access. No dependency/watchdog/monitor needed.

## Proposed internal API contracts

Names below are concrete proposed signatures for architect integration, not
already implemented APIs:

```csharp
internal sealed class NativeLaunchBinding : IDisposable
{
    internal static NativeLaunchBinding Acquire(ExecutableTuple expected);
    internal void ValidateBeforeCreate(ProcessLaunch launch);
    internal void BindOwnedPrimary(SafeFileHandle originalProcess);
    internal void ValidateCreatedImage();
    internal void RecordJobSnapshot(JobSnapshot snapshot);
    internal void RecordStartupCleanup(bool terminationSucceeded,
        bool primarySignaled, int terminationError, int waitError);
    internal Task WaitForPrimarySignalAsync(TimeSpan budget);
    internal NativeIdentityEvidence Evidence { get; }
}
internal static WindowsProcess Start(ProcessLaunch launch, NativeLaunchBinding binding);
internal readonly record struct JobSnapshot(uint Active, uint Total, uint Terminated);
```

`Evidence` is immutable data copied from the per-instance state: ownedCreated,
primaryAcquired, imageBindingChecked, primarySignaled, lastJobSnapshot nullable,
terminationSucceeded/terminationError/waitError and failureCategory. Its getters
do not invoke OS queries or print handles/paths.
Acquire/Start are single-use; reuse, disposed lease or manifest-path mismatch
fails before creation. The public `Start(ProcessLaunch)` follows the old path
with no binding/new guards; all default constructors and whitelists stay unchanged.
An internal helper/private overload may share the original launch body; no
mutable global callback/environment mode or production-native factory.

## Binding inspected image to CreateProcess selection

1. Accept only the immutable nominated absolute local fixed-NTFS path, no
   relative/UNC/device/ADS/dot components or unreviewed aliases. Pin each existing
   ancestor root-to-leaf with CreateFileW, READ_ATTRIBUTES, OPEN_EXISTING,
   OPEN_REPARSE_POINT|BACKUP_SEMANTICS and **FILE_SHARE_READ only**. Verify every
   handle is an ordinary directory. Existing incompatible data-write/delete
   handles can cause sharing failure: fail closed, never loosen sharing or alter
   owner permissions. Attribute/extended-attribute access is exempt from sharing
   checks; FILE_SHARE_READ does **not** freeze every metadata write.
2. Open the image read-only, OPEN_EXISTING/OPEN_REPARSE_POINT, FILE_SHARE_READ;
   reject directory/reparse/multiple links and keep it for the whole lifetime.
   Record its file ID/volume/size and final normalized native path in memory.
   Call existing CodexDiscovery.Inspect under this held chain, then compare its
   tuple to the held image identity and immutable hash/size/version manifest.
   Hash/PE inspection is source/distribution binding, not signature verification.
3. Query EVERY pinned directory using GetFileInformationByHandleEx,
   FileCaseSensitiveInfo23, a4-byte DWORD Flags structure, mask1. Require
   Flags==0; unsupported API/size/unknown flag/case-sensitive directory stops.
   Recheck ordinary attributes, identity and this case flag immediately before
   CreateProcessW AND at the pre-resume checkpoint; do not infer case state from
   fixed NTFS, a path comparison or a sharing mode. Recheck launch.Executable
   equality. Explicit nonnull applicationName/absolute cwd are
   retained; no shell/search-path fallback. Acquire cannot certify only an earlier
   path-string/attribute snapshot as stable.
4. After creation but **before ResumeThread**, assign the existing job, duplicate
   the exact original process handle and query its executable name in native
   format. Compare that name with the held-image native path under the still-held
   chain. Use one reviewed canonical representation and exact comparison; alias,
   ambiguity, buffer overflow/API failure or mismatch stops before resume.
5. The proposed namespace argument combines held objects, denied incompatible
   data-write/delete opens, named reparse invariants below, held leaf identity,
   case/attribute rechecks and the created process's image name. A name
   comparison alone is not a file-ID/section identity proof. Drive
   alias changes must not be assumed prevented by an open C: ancestor; differing
   created native paths must be rejected. The implementation review must resolve
   API normalization/namespace limits and adversarial tests before accepting this
   binding for the selected fixed-NTFS context. Unsupported binding remains STOP;
   do not substitute an undocumented handle-based process-creation architecture.

Microsoft explicitly excludes attribute/extended-attribute requests from
CreateFile sharing checks. The pinned SDK header establishes class23 and the
DWORD Flags/mask1 representation; it does not establish atomic case-state
immutability. Case-state checks remain snapshots, with check-to-use and transient
change/restore (ABA) limits. A sharing failure for GENERIC_WRITE cannot prove
that a narrower FILE_WRITE_ATTRIBUTES operation would fail.
[CreateFileW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew),
[Microsoft minwinbase.h](https://github.com/microsoft/win32metadata/blob/9a72549a5157598433589c5572a011869f335a1c/generation/WinSDK/RecompiledIdlHeaders/um/minwinbase.h),
[Microsoft winnt.h](https://github.com/microsoft/win32metadata/blob/9a72549a5157598433589c5572a011869f335a1c/generation/WinSDK/RecompiledIdlHeaders/um/winnt.h).

MS-FSA's FSCTL_SET_REPARSE_POINT access check accepts FILE_WRITE_DATA OR
FILE_WRITE_ATTRIBUTES. Its specific operation rejects a nonempty DirectoryFile;
the pinned next component and denied delete/rename can support a nonempty
ordinary-ancestor invariant. It also rejects a standard SYMLINK tag on a nonzero
DataFile; the inspected executable is nonzero. These are named invariants for
those cases, **not** evidence that every reparse tag/FSCTL/metadata operation is
blocked. The SDK encodes this FSCTL with FILE_SPECIAL_ACCESS=FILE_ANY_ACCESS;
filesystem checks remain additional, so GENERIC_WRITE is not a universal gate.
No all-tags metadata-freeze, atomic check/use or ABA proof is claimed. If the
selected pathname/object binding cannot be supported within these limits, entry
remains unsupported/STOP; ordinary OS/source uncertainty cannot be relabeled as
an identity guarantee.
[MS-FSA reparse algorithm](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fsa/4aeefef8-92c3-4abc-af7a-a610caf8a165),
[Microsoft winioctl.h](https://github.com/microsoft/win32metadata/blob/9a72549a5157598433589c5572a011869f335a1c/generation/WinSDK/RecompiledIdlHeaders/um/winioctl.h).

CreateProcessW selects a module by its application pathname; QueryFullProcessImageNameW
returns the created process's image name and supports native format. These APIs
do not expose a cryptographic loaded-section equivalence certificate.
[CreateProcessW](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw),
[QueryFullProcessImageNameW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-queryfullprocessimagenamew).
GetFinalPathNameByHandleW supports normalized/native-volume naming, while
CreateFile sharing controls incompatible later opens until handles close.
[GetFinalPathNameByHandleW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew),
[CreateFileW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew).

## Exact primary and cleanup ownership

BindOwnedPrimary uses DuplicateHandle(GetCurrentProcess, original hProcess,
GetCurrentProcess) with inherit=false, desired SYNCHRONIZE plus
PROCESS_QUERY_LIMITED_INFORMATION and options0. No DUPLICATE_CLOSE_SOURCE,
PID query/OpenProcess/GetProcessById, handle inheritance or ownership transfer
of the original. The noninheritable SafeProcessHandle is binding-owned and
survives WindowsProcess disposal. DuplicateHandle refers to the same kernel
object, avoiding reuse of a process identifier.
[DuplicateHandle](https://learn.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-duplicatehandle).

Launch ordering: create suspended → original handles owned → assign job →
duplicate original → verify created image → resume. Any failure stays in
exact-owned termination/wait cleanup. Start does not dispose the caller's
binding. The BOUND startup-failure path always evaluates two separate calls:
TerminateProcess(original hProcess) with its immediate return/error, THEN
WaitForSingleObject(original hProcess,2000) EVEN if termination returned false.
No boolean short-circuit may skip the wait. Record signal and termination
outcomes independently before closing the original handle; this also supplies
exact-original signal evidence when duplication failed. False termination remains
cleanup-failed/no inventory success even when the handle is signaled. A true
signal permits verified scratch cleanup subject to every other identity/artifact
gate; a missing/false/failed wait retains scratch. No false duplicate-handle
proof is emitted. Public unbound Start retains its existing behavior; only the
internal bound branch receives this failure protocol.

Success: Finish using live checks → awaited bound Dispose with terminal evidence
copy before job close → wait retained primary signal → scratch metadata inspection
→ validated cleanup → release binding/image/ancestors. Failure: preserve safe
cause, await exact owned process/transport cleanup with the same pre-close copy,
wait the duplicated primary (or recorded exact-original startup-failure signal)
and only then consider scratch deletion.
Snapshot failure records null/unknown and safe category; cleanup failure overrides
inventory success. A zero active count alone does not permit deleting scratch.

Deletion after exact-primary signal is handle-bound. For each recorded owned
leaf, release only that leaf's own incompatible pin, retain parent/ancestor pins,
then open with DELETE|FILE_READ_ATTRIBUTES|READ_CONTROL, OPEN_EXISTING,
OPEN_REPARSE_POINT and FILE_SHARE_READ. Before any mutation verify its exact
recorded file ID, type, single-link count and permitted ACL. Mismatch/unknown
stops and retains scratch. SetFileInformationByHandle FileDispositionInfo4 on
that validated handle, then close it. No File.Delete/pathname-delete fallback.
For directories work child-first: after validated children are removed, release
that directory's own pin, acquire/validate its DELETE handle with BACKUP_SEMANTICS
while ancestors stay pinned, mark disposition and close; release parents only
when their own turn is reached. Nonempty/failed deletion stops, never blind
recursion or retries. Reacquisition can race: identity verification prevents
deleting an unrecognized replacement, but does not claim a metadata-freeze.
[SetFileInformationByHandle](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle).

For a bound process, capture ONE COPIED terminal evidence snapshot in
WindowsProcess.DisposeAsync's finally immediately BEFORE job closure, AFTER
cleanup attempts. Startup failure captures the same terminal copy before job
close if a job handle exists. A failed accounting call yields null/unknown and
a fixed safe category; never fabricate zeros. Copy into binding state so it
remains available after disposal. This is one evidence capture, not one total
QueryInformationJobObject call: unchanged Finish's Active/Total/Terminated
properties and disposal's live Active polling continue querying current state.
Do not cache live counters or change public behavior to satisfy snapshot wording.
Do **not** duplicate/retain the job for later queries: another job handle would
extend its lifetime and complicate kill-on-last-close behavior. Each snapshot
must come from one QueryInformationJobObject call, not three properties queried
at different times. Normal acceptance requires active0,total1,terminated0;
forced recovery may signal the primary but never earns natural inventory success.

The30s RPC token remains cooperative. On successful inventory, call Finish with
a fresh uncancelled caller cleanup token independent of the RPC token: existing
Finish internally shares ONE2s deadline between process exit and stdout/stderr
drain completion. It is not separate2s exit and drainer budgets. Process cleanup
has2s, transport Dispose's final drainer wait has2s, and exact-primary signal wait
has2s; these sequential components remain unchanged. Soft overall cleanup10s
checkpoints stop further deletion and retain remaining scratch if exceeded;
no hard bound is implied. Cancelled RPC tokens must not skip disposal/barrier.
WaitForSingleObject(0) polling distinguishes signaled, timeout and API failure;
closing a handle during its wait is not permitted.
[WaitForSingleObject](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-waitforsingleobject).
Synchronous acquisition/creation can outlive budgets; no hard deadline or new
watchdog is introduced. Timeout/missing signal retains bounded owned scratch
and safe categories; no delete retry, unrelated kill, CLI fallback or auto rerun.

## Same-run admission/H0 provenance

H2NativeAdmission must be reviewed new C# metadata code, not a claim that it
reuses the historical in-memory PowerShell source merely because a hash exists.
[Corrected H0](2026-10-06-windows-h0-guard-fix.md) records source SHA256
`3a9c9f0b7fbe35a372bfebbfc14c13123b0bb1141cdf54b2065395219768c496`.
The lead has now recovered its exact source text read-only at
`C:/tmp/aiusagebar-h2-h0-reference.txt`, with that same verified SHA256. This
fulfills reference-text availability; independently review the new compiled C#
port's API/ABI/control/cleanup differential. A matching historical reference hash
does not certify the new port's source or execution.

Mirror process TOKEN_QUERY; reject OpenThreadToken success/impersonation and
every error except ERROR_NO_TOKEN. Query process Statistics10/context stability,
Elevation20 with **capacity4** (allocation may remain4096), Integrity25 with
bounds/SID validation; reject elevated/admin/effective integrity aboveMedium.
Use metadata-only ProgramData known-folder with DONT_VERIFY, fixedC:/NTFS,
top-down ordinary ancestry and absent/absent-by-parent config/requirements states;
no file contents, env-derived system folder, fallback guessing or owner discovery.
Bind source-pinned actual fallback identity separately in the manifest.
All token/folder memory/handles released on every exit; first cleanup failure
overrides pass. Check before image inspection/creation on the same controlled
execution thread, and revalidate token/context immediately before launch; an
async continuation must not be assumed to preserve execution-thread identity.
Unknown/change stops. No token/elevation/metadata API is invoked by this report.

## Required independent fake tests

| Pair | Positive and negative evidence |
|---|---|
| Acquire → Inspect → release | Known fake EXE tuple; wrong hash/PE/link/reparse/ancestor/alias; data-write/rename failures and actual narrow FILE_WRITE_ATTRIBUTES/EA metadata controls tested separately; partial acquisition releases every handle |
| Start → original duplicate → wait | Fast-exiting fake signals retained exact duplicate after original disposal; no PID reopen; duplicate failure/created-image mismatch stops before resume; child marker absent on pre-resume failure |
| Held path → selected image | Independent leaf/ancestor/path-alias substitution and case-state/reparse narrow-rights attempts around inspect/create/pre-resume; actual API result and applicable nonempty/nonzero constraints recorded, unsupported controls not counted as denied by sharing; no identity proof from hash equality or checkpoint rechecks alone |
| Job evidence → disposal → safe state | One copied snapshot after cleanup attempt just before job close, despite multiple uncached live queries; normal0/1/0, recovery distinguishable; accounting failure unknown; no job duplicate or successful output after failed cleanup |
| Startup termination → original signal → release | Independent fake cases terminate=false+signaled, terminate=false+unsignaled, duplicate failure+original signal and snapshot failure; termination error preserved even when signal enables gated scratch cleanup; no short-circuit or false successful inventory |
| Cancellation/parser failure → cleanup → scratch | Every phase failure, cancelled30s token, constructor failure and fresh2s barrier; root survives timeout/nonsignal/unknown identity; adversarial leaf/dir replacement during pin release; DELETE-handle identity validation+disposition only after signal, no pathname fallback |
| H0/context producer → admission | Independent fake API matrix20capacity4 vswronglength, no-token/admin/integrity/known-folder/ancestor failures; all-nine loader contexts and startup clone preserve exclusions; no live machine probe in offline tests |
| Internal/public mode → stdout | H2 exact four methods, quota denied pre-write; public behavior/counters unchanged; any future explicit opt-in route has no default/offline native activation |

Fake tests prove only those contract paths; none invoke Codex/Claude or imply
native compatibility. The future safe evidence contains booleans, categories,
hash/manifest IDs, numeric job counts or unknown, signal/cleanup outcomes and
elapsed intervals. Never output process/file handle values, raw owner paths,
tokens/SIDs, arbitrary errors, config/auth payloads or account-work absence.
No client account/auth RPC is distinct from disclosed internal account work.

## Self-gate

Re-read actual WindowsProcess, CodexDiscovery, PrivateFiles, Core friend access
and H2 retain/cleanup helpers; compare design producer/consumer/release pairs.
Checked official documented APIs and distinguished proposed namespace argument
from a runtime proof, sharing checks from metadata rights, and named reparse
invariants from all-tags/ABA guarantees. New API-control/adversarial tests remain
future implementation work; no source-record count or fixture assertion substitutes
for those tests. Re-read this report against planning-only brief. Only this
document was changed; no Core/test source, build/runtime/H0 probe or owner data
was touched. Syntax/build verification awaits future approved implementation.

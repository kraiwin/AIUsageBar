# Plan: Windows blocker amendment — deterministic C6 proof, C1/C5 decisions

Date: 2026-10-05. Architect supplied the grounded design; lead is the sole file
writer. Status: actual Codex final focused review PROCEED for C6-only PLAN;
owner approved the C6 plan with `go`, then authorized continuous C6 execution
and sub-agent dispatch in the next message on 2026-10-05. Both gates are passed.
Input: local spec `docs/specs/spec-2026-10-05-154712-windows-blocker-amendment.md`.

## Summary

Recommend the next source dispatch cover C6 only: one internal, per-instance
test seam at the validated-reader-close to actual ReplaceFileW boundary, with
deterministic retention proof. C1 remains blocked by real system known-folder
reads before RPC; C5 remains unsupported because no candidate owns the complete
caller tree during startup. No acceptance threshold, native/account/settings
permission, dependency or installation is changed by this proposal.

## Patterns to Mirror

| Category | Source (path:line) | Pattern |
|---|---|---|
| Publication boundary | windows/src/AIUsageBar.Core/PrivateFiles.cs:131 | Read/validate target, dispose reader scope, then use actual Win32 replacement |
| Recovery | windows/src/AIUsageBar.Core/PrivateFiles.cs:140 | ReplaceFileW flags0, unique recovery, compare displaced identity/bytes before deletion |
| Scratch safety | windows/src/AIUsageBar.Core/PrivateFiles.cs:16 | Explicit root, protected user+SYSTEM DACL, pinned parents, reject reparse/hardlinks |
| Native adversary | windows/tests/AIUsageBar.Tests/ClaudeTests.cs:193 | Editor ignores writer.lock and uses actual MoveFileExW(REPLACE_EXISTING) |
| Semantic checks | windows/tests/AIUsageBar.Tests/ClaudeTests.cs:72 | Complete bytes and semantic owner/DACL comparison |
| Registration | windows/tests/AIUsageBar.Tests/Program.cs:24 | Add cases to ClaudeTests.Cases and use existing --offline --case filter |
| Safe failure output | windows/tests/AIUsageBar.Tests/Program.cs:45 | Categories/counts/source frames only, no payload/config/exception messages |
| Process ownership | windows/src/AIUsageBar.Core/WindowsProcess.cs:64 | Suspended launch, assign before resume; limit1 is specific to Codex/fake transport |
| C5 observation | windows/tests/AIUsageBar.Tests/ClaudeTests.cs:375 | Observe all owned descendants and caller EOF before harness cleanup |
| C1 boundary | pinned config/src/loader/mod.rs:268,798,810,816; cli/src/main.rs:1266 | System paths via known folders, direct CLI default LoaderOverrides |

There is no production native supervisor, authenticated warm broker or MSYS
pipe/root-binding convention in the repo. Such designs are not ready-to-use
patterns. The current harness owning a job is not production caller-death proof.

## Files to Change

The three source/project rows are the complete proposed C6-only delta. Lead
owns documentation; source author and independent test/reviewer ownership must
be fixed at dispatch. No serialization CREATE/UPDATE/GET field is introduced.

| File | Action | Why |
|---|---|---|
| windows/src/AIUsageBar.Core/PrivateFiles.cs | UPDATE | Readonly optional per-instance callback via internal constructor; public path remains no-op |
| windows/src/AIUsageBar.Core/AIUsageBar.Core.csproj | UPDATE | SDK InternalsVisibleTo item for existing AIUsageBar.Tests assembly; no package |
| windows/tests/AIUsageBar.Tests/ClaudeTests.cs | UPDATE | Deterministic actual-OS replacement and same-ID content races, evidence survival |
| docs/plans/2026-10-05-windows-blocker-amendment.plan.md | CREATE | Saved architect plan, alternatives and bounded dispatch |
| docs/plans/2026-10-05-windows-blocker-amendment-review.md | CREATE | Actual Codex review provenance and complete dispositions |
| docs/validation/windows-mvp-validation.md | UPDATE | New measured evidence only; preserve historical stress/tee results |
| docs/STATUS.md | UPDATE | Proposed/approved/actual C6 state separate from C1/C5 blockers |
| docs/DECISIONS.md | UPDATE | Actual owner plan/dispatch decisions only after received |
| windows/AGENTS.md | UPDATE | Approved C6-only ownership/scope after gates |
| CHANGELOG.md | UPDATE | Implemented change/evidence after execution, no live-support claim |
| MEMORY.md | UPDATE | Current checkpoint and outstanding choices |

No WindowsProcess, Bridge, Tray, shell experiment, NativeCompatibility, installer,
solution, SDK or NuGet change is required. Source remains untouched this round.

## Tasks

### Task 1: Bound the next delta and keep current gates

- Action: recommend C6-only source work and read-only C1/C5 research. Keep
  C#/NET10/WinForms, framework/Win32 only, existing job guards and strict C5:
  every descendant <=2s; caller exit/stdout/stderr EOF overhead <=500ms;
  original bytes/exit/single execution preserved. The optional owner question
  about strict versus baseline-equivalence has no answer yet; strict remains.
- Mirror: mandatory offline versus unsupported/inconclusive experiment separation.
- Validate: actual Codex plan review with dispositions, owner plan approval and
  second dispatch gate precede source. Existing approval is no native permission.

| C6 option | Benefit | Cost/risk | Recommendation |
|---|---|---|---|
| Internal per-instance boundary callback | Small exact-window control over real OS algorithm | Seam remains in release assembly; friend exposes internals to test assembly | Choose; no public/runtime/plugin/config activation |
| Internal native-file-operation adapter | Could orchestrate more failures | Larger API/construction change and encourages OS mocking | Defer unless callback cannot express this exact race |

Another lucky stress replay does not meet deterministic acceptance.

### Task 2: Smallest boundary seam

- Action: readonly optional callback through an internal constructor overload;
  keep the public constructor behavior. Invoke only for existing destination,
  immediately after the old-reader scope closes and before actual ReplaceFileW.
- Mirror: PrivateFiles.cs:133–140; retain locks/size/security/temp/flush/API flags,
  recovery comparison, output verification and cleanup order.
- Validate: exact reader-disposal boundary; default path no callback; no static
  mutable hook, public setter, environment flag, plugin or reflection activation.
  Existing SDK supports InternalsVisibleTo: lead inspected installed
  Microsoft.NET.GenerateAssemblyInfo.targets lines116–122 (SDK10.0.401).

The callback does not simulate ReplaceFileW, add CAS, change compare-after-replace
or introduce rollback. It controls timing; actual filesystem operations remain.

### Task 3: Deterministic actual-Win32 recovery proof

- Action: add these cases under `files/deterministic` in ClaudeTests.Cases:
  1. External replacement: arrange protected scratch original A, staged editor E
     and proposed bytes N. Record actual A/E identities, close readers. Inside
     boundary callback use existing MoveFileExW(stageE,target,REPLACE_EXISTING)
     directly; assert success. Do not use second AtomicWrite under held lock.
  2. Same-ID content edit: direct file handle overwrite/flush at that boundary,
     equal-length different bytes, preserving identity; prove byte comparison
     independently of file-ID mismatch.
  3. No-race controls: public construction succeeds for first create and existing
     replace, with complete reads and expected cleanup. Internal counting no-op
     callback is called zero times on create and exactly once on replace, with
     both operations successful. A separate public instance is unaffected by
     the hooked instance. Race cases assert exactly one callback invocation.
- Mirror: ClaudeTests.cs:72,193–229 and native MoveFileExW declaration;
  PrivateFiles.Information internal actual-handle helper.
- Validate: fresh private scratch per case; A/E/N are pairwise distinct. Record
  recovery-path set before controlled write; after adversary succeeds and
  publish-conflict occurs, require exactly one new recovery pathname from the
  set difference, not an arbitrary enumerated old file. Record this exact path,
  volume/file-index identity and complete bytes through validated handles.
  BEFORE treating that file as a survival baseline, external-replacement recovery
  MUST equal the recorded staged E volume/file-index identity and complete E
  bytes; same-ID-content recovery MUST equal the preserved A volume/file-index
  identity and complete edited bytes. Observing arbitrary recovery bytes/ID is
  not an oracle; expected identity/content are captured independently beforehand.
  Same-ID edit additionally proves equal length, successful flush and unchanged
  identity before/after mutation. Both races require target bytes N after conflict.
  Check recovery/target protected DACL, current owner, only expected user+SYSTEM
  allow/full-control rules, regular file/no reparse/single-link semantics.
  Close ALL inspection handles before later ordinary public publication; then
  reopen the EXACT original recovery pathname, require same identity/full bytes
  and security again. An old open handle surviving pathname deletion is no proof.

In these controlled successful replacements target MUST contain N after conflict:
comparison follows replacement. Assert this, never accept arbitrary bytes or
claim CAS/rollback. These cases do not cover every
API partial failure or power-loss durability. [ReplaceFileW partial outcomes](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew).

### Task 4: Self-gate implementation and evidence

- Action: re-read all three source/project edits against Tasks2/3; independently
  review default seam path and exact native operation before downstream use.
- Mirror: framework-only build and safe test output.
- Validate after owner source authorization, from windows/:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_NOLOGO = '1'
dotnet build AIUsageBar.Windows.slnx -c Release --no-restore
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline --case files/deterministic
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline
```

Future callback/friend declaration/test assembly binding/filter/safe helpers
are proposed integration dependencies, not verified implementation. Lead checked
SDK support separately; reviewer did not verify that external fact. Focused run
must select a nonzero count and execute ALL registered deterministic cases,
not succeed with an empty/misnamed filter. Require0warnings/errors, new cases and mandatory suite pass without skip. Actual
counts/durations only;80/0/0 is historical until new run. Stop on failed adversary,
unreached boundary, lost/mismatched recovery or regressions. Unchanged failed tee
need not rerun. No substitution of timing stress for deterministic result.

### Task 5: C1 isolation choices, no native launch

| Branch | Contract | Owner setup/cost | Limit |
|---|---|---|---|
| Keep offline/fake work | No native CLI; finish read-only source audit | None | Does not deliver native/live usage |
| Owner-provisioned disposable Win11x64 lab | Fresh guest/synthetic profile and ProgramData fixtures; no owner auth/host profile exposure; networking disabled at VM boundary; pin binary/policy | VM/image/license/runtime/tooling and transfer method; possible OS setup requires separate approval | New guest contract, not host scratch proof or authenticated host certification |

- Action/Mirror: preserve pinned bootstrap/known-folder audit and binary/policy
  binding. A host account still shares machine ProgramData; dead proxy/job/env
  are not filesystem/network isolation. A modified CLI certifies a different exe.
- Validate: full source audit of auth/cloud/client/telemetry/known-folder fallback
  and selected RPC effects before future lab harness. Guest system layers must
  explicitly be synthetic, not ignored because outside CODEX_HOME.

Owner choice: retain deferral or specify/provision disposable lab. This PC's
WindowsSandbox.exe lookup is absent. No OS feature, account or installation here.
[Windows Sandbox isolation/configuration](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-configure-using-wsb-file)
describes network disable/read-only mappings; no sandbox was launched or verified.

### Task 6: C5 architecture study, no source-ready supervisor claim

| Option | Ownership contract | Status |
|---|---|---|
| Bridge disabled | Offline tray/inert sink, no settings/launcher | Supported bounded choice now |
| First-party native supervisor | Native stage owns supported full launch tree before CLR/original children; suspended→no-breakaway job→resume; caller-root death closes last keeper | Unresolved outer-shell/root contract and build toolchain; not source-ready |
| Cooperative warm broker | Bash builtin authenticated pipe connect before children; OS client PID + held process/root handles; noninherited keeper duplicated into exact outer root; inner shell assigned before helpers; close ALL broker job refs before READY | Study only; MSYS pipe access/root binding/pre-READY/fallback unproven |

- Mirror: WindowsProcess suspended/handle discipline, not its limit1 as a Claude
  multi-child policy. [Job lifetime](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects)
  and [handle duplication](https://learn.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-duplicatehandle)
  provide primitives, not proof of this architecture.
- Validate before chosen branch source manifest/dispatch:
  - Actual Claude2.1.289 shell/root/flags/stdin/termination. Official docs route
    through Git Bash when installed, otherwise PowerShell; native command text
    alone cannot be assumed a native outer root. [Windows statusline](https://code.claude.com/docs/en/statusline#windows-configuration).
  - Ownership before every child's startup, including parent death pre-READY;
    readiness only after CLR cannot protect earlier startup.
  - Authenticated pipe peer + held handles, not caller-supplied PID/process name.
    [Client PID API](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getnamedpipeclientprocessid)
    does not authenticate outer ancestry. Nested-job compatibility must be proven.
  - Original exact streams/exit/single execution; finite payload/held stdin,
    background output, missing deployment/broker, no raw input files/rerun fallback.
  - Normal completion/Tray quit without unrelated termination or leaked keeper;
    strict2s and500ms during pre-connect/handshake/pre-resume/stalled CLR phases,
    observed before harness cleanup.

Reject warm broker if these cannot be concrete. Already-running alone is no
startup proof. Standalone managed helper cannot observe parent before CLR entry.
Owner choices: toolchain/root contract investigation, later actual statusline
context observation, optional strict/baseline question. Current strict remains.
MSVC/clang lookup and VC tools query reported none; no installer/NativeAOT exception.

## Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Seam at wrong boundary | Medium | After reader dispose/before unchanged real API; invocation and adversary success asserted |
| Writer-lock deadlock | High if second AtomicWrite used | Stage editor beforehand; direct OS edit ignoring app lock |
| ID-only test misses same-ID modification | Medium | Equal-length same-ID edit plus complete recovery bytes |
| Proof mistaken for CAS/rollback | High | Assert possible N remains; distinguish detection/recovery from prevention |
| Internal seam exposed as runtime setting | Low | Readonly per instance, default no-op; no public/env/plugin activation |
| Later write deletes conflict evidence | Medium | Verify earlier recovery unchanged after later normal publication |
| C6 mistaken for C1/C5 closure | High | Separate gates; native/bridge/settings remain off |
| VM proof misapplied to owner host | High | Bind guest context/binary/policy; W3 auth reassertion separate |
| Supervisor/broker with unowned startup/fallback | High | Concrete full caller/handle/fallback contract before source |
| Tooling/setup silently added | High | Owner choices; C6-only needs none |

## Review and owner dispositions

Historical first review: actual Codex CLI0.160.0/gpt-6.1-sol completed, exit0, verdict
fix-then-proceed:1major/3minor/2info. Prompt/raw/summary outside repo at
C:/tmp/codex-reviews/*-20261005-160331.*; exact paths/full result in companion
review report. Dispositions below were subsequently closed by the delta/focused
reviews recorded afterward; their runtime requirements remain future proof.

| ID | Disposition | Closure required |
|---|---|---|
| M1 major | Accepted: unique new recovery path via set-difference; close inspection handles then reopen identical pathname after later publication | Actual OS retained ID/bytes/security at original path, not surviving orphan handle |
| m1 minor | Accepted: public create/replace, internal no-op create0/replace1, separate public-instance isolation, recovery cleanup controls | Deterministic cases must run after source approval |
| m2 minor | Accepted: distinct A/E/N, equal-length same-ID edit with explicit identity/flush proof; target N required for both races | Native successful edit/replacement and typed conflict, no arbitrary target/CAS claim |
| m3 minor | Accepted: explicit semantic DACL/owner/user+SYSTEM/regular-singlelink checks on target/recovery and after reopening | Real handles/semantic rights, not textual SDDL equivalence |
| i1 info | Accepted: label unimplemented integrations/partial excerpts; focused nonempty selection/build/full-suite evidence mandatory | No historical80/0/0 reused as current proof |
| i2 info | Accepted: C6 detection+retention only, C1/C5 unresolved/strictgates and source authorization unchanged | No native/live/settings/dependency or OS setup |

At the review checkpoint no source approval or dispatch had occurred. Owner
subsequently approved the plan and continuous C6 team execution; runtime proof
remains pending until the implementation and new checks complete.

Delta review CLI0.160.0/gpt-6.1-sol exit0 closed all six original plan findings,
still fix-then-proceed with1minor/1info: bind INITIAL recovery to adversary E
identity/bytes or preserved A identity+edited bytes, before survival checks.
Lead accepted and added that explicit oracle above. Final focused actual Codex
review CLI0.160.0/gpt-6.1-sol exit0, verdict PROCEED, closed m4 in PLAN with no
remaining material finding. Raw/prompt/summary stamp20261005-161541 in companion
report. All plan findings/dispositions are closed; implementation/runtime proof
is still future work and C1/C5 gates remain unchanged.

## Approved bounded dispatch — 2026-10-05

Owner approved the plan with `go`, then requested continuous execution using
sub-agents to limit main context. This confirms the second dispatch gate:
backend-dev owns PrivateFiles.cs/Core.csproj; qa-engineer owns ClaudeTests.cs
deterministic cases; lead owns integration/review/docs. Preserve other agents'
edits and all existing W1/source behavior. Scope is exactly three source files,
actual scratch Win32 proof, no dependency/install/native/account/settings.
Both gates are confirmed for C6 only. C1/C5 and all other restrictions remain.

## C6 execution result — 2026-10-05

Three source/project files implemented per approved ownership. New Release
build exit0,0warnings/errors; focused deterministic4/0/0 (194ms); mandatory
offline84/0/0 (11748ms), no skips. Independent native GPT read-only source review
found no actionable C6 defect; lead reread source/logs and compared baseline
hashes. [Execution evidence](../validation/2026-10-05-windows-c6-validation.md).
This closes controlled deterministic C6 proof only; C1/C5 remain blocked and
strict. No real CLI/account/settings/install/dependency/commit/push activity.

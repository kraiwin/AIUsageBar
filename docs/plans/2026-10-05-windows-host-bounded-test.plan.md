# Plan: H0-only bounded Windows-host metadata preparation

2026-10-05. Architect supplied the reduced design; lead is sole writer.
DECISIONS28 permits planning/model review only. No script creation/run or real
accounts. The earlier H0/H1 draft received6Major/1Minor/1Info, fix-then-proceed;
[actual review](2026-10-05-windows-host-bounded-test-review.md) preserves it.

## Summary

Recommend one reviewed in-memory PowerShell command block querying privileges
and metadata of the actual ProgramData Codex config locations. No provider CLI,
application build, config/auth contents or explicit runtime report by H0. H1 ownership
experiments and H2 native execution are excluded, not proven by narrowing scope.
C1/C5 remain blocked; strict2s/500ms stays unchanged.

Future command authoring/execution requires approval of this reviewed plan and
explicit dispatch. Current planning go grants neither. H0 is the smallest useful
prerequisite record; H1 requires its unresolved actor/keeper/timeout protocols.

## Patterns to Mirror

| Category | Source (path:line) | Pattern |
|---|---|---|
| Authorization | docs/DECISIONS.md:278 | Planning only |
| No extra artifact | docs/DECISIONS.md:278 | Planning docs only; runtime block in memory |
| Machine namespace | docs/validation/2026-10-05-windows-c1-isolation-study.md:30 | Actual known folder, not env override |
| Native gaps | docs/validation/2026-10-05-windows-c1-isolation-study.md:106 | Startup/account exclusion unresolved |
| Safe output | windows/tests/AIUsageBar.Tests/Program.cs:45 | Categories, no raw data |
| No unrelated kill | windows/AGENTS.md:35 | No OS/process/settings changes |

This small PowerShell metadata bootstrap has no equivalent reusable app helper.
Do not import the test runner, PrivateFiles/Scratch, app assemblies or actors/jobs.

## Files to Change

Current lead-owned documentation only:

| File | Action | Why |
|---|---|---|
| docs/plans/2026-10-05-windows-host-bounded-test.plan.md | UPDATE | Reduced H0 scope |
| docs/plans/2026-10-05-windows-host-bounded-test-review.md | UPDATE | Actual review/dispositions |
| docs/STATUS.md | UPDATE | Actual planning state |
| MEMORY.md | UPDATE | Current checkpoint |
| CHANGELOG.md | UPDATE | Material planning outcome |
| docs/DECISIONS.md | UPDATE if needed | Received instructions only |

Future implementation filesystem manifest: NONE. After both owner gates lead/QA
authors one complete reviewed command-source string in memory, not a .ps1 file.
No app/test/project/SDK, runtime store/cache, bridge/installer/settings, H1/H2
artifact or directory is created. Only planning/review/status documents persist.

## Tasks

### Task 1: Reduced-scope review

- Action: obtain actual CLI delta review; record every disposition. Removing H1
  does not fix its protocols or make it implementation-ready.
- Mirror: plan-gate approval/dispatch rules.
- Validate: all future execution belongs to H0, source hashes unchanged, no
  build/test/probe or provider invocation claims.

### Task 2: Future in-memory source/static gate

- Action: lead/QA supplies one complete immutable command-source string, no
  parameters/alternate mode/source-file import/profile/module loading. No file
  reading/writing/directory creation/pinning/deletion lifecycle exists in H0.
  Controlled alphanumeric interop identifier is part of the source BEFORE review
  and hashing; no post-hash interpolation or source replacement.
- Mirror: source reread and parser validation, without app/SDK imports.
- Validate: Parser.ParseInput of this exact string, AST and line-by-line
  PowerShell/C# PInvoke inspection; record UTF-8 SHA256. Immediately before
  execution recompute/equal the reviewed digest, ScriptBlock.Create from THAT
  same string and invoke once in the SAME tool invocation. Mismatch/parse failure
  means fixed failure record and no block invocation. No command is authored or
  executed during current planning.

Approved target drive is literal C: on this PC, not an environment value.
GetDriveTypeW must return DRIVE_FIXED and GetVolumeInformationW NTFS. There is
no workspace/artifact-root preparation or filesystem mutation to authorize.

### Task 3: Future in-process privilege bootstrap

- Action: trusted tool-provided PowerShell Core7+ context only. No nested shell/profile
  load/policy change/module import/download/dotnet. Script-local variables/error
  handling; unique run-specific type name, reject pre-existing type. Add-Type
  capability failure stops. Inline Add-Type -TypeDefinition without OutputAssembly,
  external source or alternate compiler creates in-memory interop only.
- Action: BEFORE known-folder/target queries use OpenProcessToken(GetCurrentProcess,
  TOKEN_QUERY) and GetTokenInformation(TokenElevation, TokenIntegrityLevel).
  Require elevation=false and integrityRID<=Medium0x2000; check effective admin
  membership=false using CheckTokenMembership(NULL, built-in Administrators SID),
  not an account/group-name approximation. Failed/indeterminate/malformed data
  or elevated/effective-admin state stops. No user identity/SID/token data output.
- Mirror: OS token metadata rather than provider auth stores.
- Validate: close owned token/SID allocations/buffers in finally. These queries
  read no Codex/Claude credential file. No child environment/dump is required.

Existing shell hooks/state predate H0 and are not undone/certified. Primary docs:
[Add-Type](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/add-type?view=powershell-7.5),
[token query](https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-gettokeninformation).

The trusted tool-provided PowerShell command session is the invocation context;
the block must not spawn a nested shell. Reject thread impersonation before
privilege checks: OpenThreadToken(GetCurrentThread,TOKEN_QUERY,OpenAsSelf=true)
success means impersonation and stops after closing that handle; only
ERROR_NO_TOKEN permits using the process token. Any other failure stops.
Validate integrity SID with IsValidSid, mandatory-label authority and expected
RID structure before reading/comparing the RID. No unknown SID is treated as
Medium. Free built-in admin SID with its matching allocator; failed membership
query stops independently of the boolean. Keep token queries for this same
execution thread and reject an unexpected token/context change.

No-OutputAssembly is an OWNED-OUTPUT constraint, not a proof that PowerShell/
Roslyn/runtime cannot internally use temporary files or processes. The script
does not explicitly create runtime files or launch a compiler process, but
unobserved compiler/host internal effects are not certified absent. There is
no runtime cache/profile backup or rollback claim. If this existing host cannot
perform inline compilation under these limits, stop; no workaround/tool install.

### Task 4: Future known-folder/attribute metadata

- Action: SHGetKnownFolderPath(FOLDERID_ProgramData, KF_FLAG_DONT_VERIFY0x4000,
  hToken=NULL). No CREATE/INIT/DEFAULT_PATH or fallback guess/env. Check HRESULT;
  keep path only in memory, CoTaskMemFree in finally even if call fails.
- Action: reject empty/relative/UNC/device/extended/unexpected path BEFORE
  traversal. Resolved drive must be literal approved C:, ordinal-ignore-case;
  other drives stop before target attributes. Only ordinary absolute local path;
  verify drive/filesystem through read-only volume metadata. Unknown/remote/
  nonNTFS stops. No executable lookup/profile discovery.
- Action: top-down GetFileAttributesW of drive root, ProgramData ancestors,
  OpenAI, Codex, config.toml and requirements.toml. Directories ordinary, target
  files ordinary non-reparse. Missing ancestor stops dependent probes, no
  enumeration. INVALID_FILE_ATTRIBUTES captures immediate Win32 error; ONLY2/3
  mean missing, all denied/network/other failures stop/inconclusive. Do not use
  File.Exists/Directory.Exists that conceal error categories.
- Mirror: actual machine namespace source finding.
- Validate: present machine file/reparse/wrong type/inaccessible/unknown stops.
  NEVER open/hash/read/copy/edit/delete owner config/auth. Absence is point-in-time
  metadata, not native/account isolation or completed transitive audit.

No-verify retrieval deliberately differs from native startup, not a bootstrap
replay. Primary docs:
[known-folder flags](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/ne-shlobj_core-known_folder_flag),
[known-folder API](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shgetknownfolderpath),
[file attributes](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfileattributesw).

### Task 5: Future output/failure/cleanup

- Action: one fixed in-memory/console schema: privilege flags, known-folder/
  local-drive/ancestor result, config/requirements states absent/present/
  not-checked/unknown, outcome and bounded numeric API-error category. Always
  nativeAuthorized=false, c1Status=blocked, c5Status=blocked. No raw paths/user/
  SID/env/content/payload/arbitrary exception messages. No runtime report/log/
  transcript/cache/state file explicitly created by H0; compiler/host internal
  effects retain the qualification in Task3.
- Action: soft elapsed budget checkpoints before/after each call. A completed
  over-budget call stops further queries as inconclusive. Synchronous native
  calls cannot guarantee hard completion/cancellation; Ctrl+C may not immediately
  interrupt. No workers or shell/unrelated-process kill. Finally releases owned
  handles/buffers/pointers on unwind; no exit terminating the current shell.
- Mirror: safe categories and exact owned cleanup.
- Validate: source has no filesystem writer/network/CLI/process-creation,
  process-termination or job API branch; querying its own process/token is allowed.
  Intended operations do not prove absence of all OS/runtime accesses; no global
  monitor. Add-Type can remain loaded for the shell lifetime, no unload guarantee.

Exact output contract: one object, one terminal record, including early failure.
Fields/types: schemaVersion:int=1; phase:string=H0; privilegeClear:bool;
knownFolderResolved:bool; localDriveClear:bool; ancestryClear:bool;
configState/requirementsState:string enum {absent,absent-by-parent,present,
not-checked,unknown}; outcome:string enum {metadata-clear,blocked,inconclusive};
category:string enum {none,input,artifact,host-capability,compile,privilege,
impersonation,known-folder,drive,ancestry,machine-config,api,budget,cleanup};
apiError:int32=0 for no failed API call (including expected2/3 absence), otherwise
the immediate unexpected Win32 error or signed HRESULT;
nativeAuthorized:bool=false; c1Status/c5Status:string=blocked.
Initialize flags false, states not-checked, outcome inconclusive before any
operation. Unknown means an attempted query failed; not-checked means no query
was made; absent-by-parent requires a positively classified missing ancestor.
metadata-clear requires all four Clear/Resolved flags true and both target
states absent/absent-by-parent. Present/reparse/wrong type/elevation/impersonation
is blocked; failed capability/query/compile/budget/cleanup is inconclusive.
First terminal result stops further queries. Attempt EVERY applicable owned
handle/pointer release in finally; retain the first cleanup failure without
preventing later releases. ANY cleanup failure overrides prior clear, blocked
OR already-inconclusive outcome: outcome=inconclusive, category=cleanup and
apiError=the FIRST cleanup failure's immediate native error, or0 when it has no
native error. Preserve previous metadata-state fields. Emit exactly ONE final
14-field record AFTER cleanup and only when control returns. Compiler,
parser and native diagnostics are captured/suppressed, not printed; wrapper
catch emits this fixed record instead of raw throw/exception/source text.
Soft budget=10000ms from invocation including inline compilation, checked
before/after each operation; max64 query operations, max32 path components,
max240 path characters. Limits stop further calls, never skip finally cleanup.
No hard termination guarantee. C5's2s/500ms does NOT apply to this metadata-only
script and remains unchanged for deferred H1; it is not silently relaxed.
One terminal record is required on normal return or caught failure/unwind; a
stalled native call cannot be promised to produce a record before control returns.

| Effect | Cleanup | Limit |
|---|---|---|
| Reviewed command text | In-memory only | No code file/directory/namespace cleanup |
| In-memory interop | Shell-owned | May remain loaded; no forced termination |
| Token/buffers/known-folder memory | finally on unwind | Stalled native call may not promptly unwind |
| OS metadata | Own token + targeted local namespace | Point-in-time; no contents/global monitor |
| Existing shell/OS behavior | Outside H0 ownership | No sandbox/no-egress/global rollback |

### Task 6: Future invocation/interpretation

- Action: after static self-gate and both gates, the reviewed source/digest are
  already fixed. Recompute hash and compare; invoke ONCE in the same tool session:

```powershell
& ([System.Management.Automation.ScriptBlock]::Create($reviewedSource))
```

- Mirror: exact reviewed artifact and phase-scoped call.
- Validate: fixed output/stop category; no retry with elevation/tool/settings/env
  changes. Lead may later write a sanitized doc summary; H0 writes no report.
- Action: H0 records obvious metadata prerequisites only. Even absent machine
  files leave H2 closed: account/source/binary/startup/observation gaps remain.
  H1/C5 requires a separate concrete protocol plan/review. No SDK or provider CLI.

The wrapper compares SHA256(UTF8($reviewedSource)) to the previously recorded
reviewed digest immediately before this call; no later substitutions/parameters.
Parse errors and mismatch produce only the fixed input failure record, no
invocation. This binds the reviewed command text, not native binary isolation.

## Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Planning becomes run permission | High | Plan approval + explicit dispatch |
| Absence becomes C1 closure | High | Blocked fields/point-in-time limitation |
| Denied query called missing | Medium | Immediate error, only2/3 missing |
| Remote/reparse traversal | Medium | No-verify path, local checks, top-down stop |
| Type collision | Low | Unique name, reject existing |
| Hard timeout overstated | Medium | Soft budget/native cancellation caveat |
| Broad cleanup | Low | No namespace cleanup; owned handles/pointers only |
| Deferred H1 called fixed | High | Unresolved dispositions |

## Codex review disposition

| Finding | Disposition |
|---|---|
| M1 future compiled pre-SDK guard | Eliminated in H0: no SDK/build/apphost, existing PowerShell token guard before target queries |
| M2 premanaged keeper/stages | Deferred/unresolved: all H1 actors/jobs/stages excluded |
| M3 actor identity/args/lifetime | Deferred H1; H0 exact immutable reviewed/hash-checked command string/no modes |
| M4 SDK/actor env/scratch | SDK/actor branch eliminated; H0 preparation and compiler effects separately bounded, no global absence claim |
| M5 experiment deadlines | Deferred H1; H0 soft checkpoints/cancellation limits, no hard bound claim |
| M6 app/state integration | Eliminated in H0: no app/test/state changes; H1 concerns remain unresolved |
| m1 external absence overclaim | Accepted: intended APIs/flags only, no monitor/global absence guarantee |
| i1 scope/provenance | Retain H2 exclusion/actual CLI record, obtain delta review of replacement scope |

Final actual CLI review180638 gave PROCEED for this H0-only PLAN, no remaining
material finding. No command implementation/runtime result or owner approval
of the revised plan/dispatch is claimed. Planning artifacts only.

Historical delta174844: actualCLI exit0, fix-then-proceed2Major/2Minor. Lead
initially added a parent/create/pin protocol and exact schema/budget/token/hash
clauses. That filesystem proposal was subsequently superseded by the in-memory
amendment below; no removed protocol is claimed present or concurrency-safe.
Original M4 is narrowed, not proof of no compiler/host internal effects.

Focused175816: actualCLIexit0/fix-then-proceed, D-m1/D-m2 closed in PLAN,
D-M1/D-M2 not yet closed. Lead corrected briefing count17 to ACTUAL14fields;
no3fields invented. Architect supplied minimal amendment: remove the unnecessary
filesystem artifact entirely. D-M1 filesystem lifecycle eliminated from current
scope, NOT proven concurrency-safe; D-M2 cleanup overrides all outcomes/APIerrors
with first cleanup failure, complete record emitted AFTER cleanup.

Final actualCLI180638, CLI0.160.0/gpt-6.1-sol/OpenAI/medium, exit0:
PROCEED for PLAN. D-M1 eliminated from current scope; D-M2/D-m1/D-m2 closed
IN PLAN, no remaining material H0 finding. Lead read exact raw315–327; full
provenance in companion review report. External references not certified by
reviewer; lead used primary docs during preparation. H1/H2 remain unresolved/
excluded; C1/C5 and strict criteria unchanged. Execution still requires owner
approval/dispatch and future exact-source/parser/self-gate. No command authored
or run during planning, no app source/probe/accounts/setup/commit/push.

## H0 authorized attempt — 2026-10-06

Owner subsequently instructed “ลุย h0” (DECISIONS29), authorizing command
authoring/self-gate and one H0 invocation. QA/lead prepared exact in-memory
source, independent native GPT code review found no static defect, parser0errors
and immediate UTF-8 hash equality preceded execution. Actual result was
inconclusive/privilege/apiError24 BEFORE target metadata; both machine files
remain not-checked. No retry/alternative/elevation. [H0 evidence](../validation/2026-10-06-windows-h0-preflight.md).
This supersedes approval-pending checkpoint only; C1/C5/H1/H2 stay blocked.

## Corrected H0 replay — 2026-10-06

Owner authorized investigation/fix/review/one replay (DECISIONS30). Same-token
guard-only matrix isolated TokenElevation capacity4096 error24 vs4success.
Class20-only correction reviewed by author/native GPT; parser0/hash bound.
ONE corrected H0 returned metadata-clear/error0, four prerequisite flags true,
config/requirements absent-by-parent. [Fix/ledger/evidence](../validation/2026-10-06-windows-h0-guard-fix.md).
H0 metadata snapshot only; nativeAuthorizedfalse/C1C5blocked/H1H2excluded.
No app source or codefile/provider/account/setup/publication change follows.

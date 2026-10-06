# Windows C6 — deterministic replacement-conflict evidence

2026-10-05 · Windows 11 Pro x64/build 26200 · .NET SDK 10.0.401/runtime 10.0.12.
Scope: approved [C6 amendment plan](../plans/2026-10-05-windows-blocker-amendment.plan.md),
offline synthetic fixtures and private scratch files only. Owner approved the
plan with `go`, then authorized continuous C6 execution with sub-agents.

## Result and source boundary

C6's two controlled race cases and two default-path controls pass on the real
Windows filesystem. The mandatory offline suite now has **84 passed, 0 failed,
0 skipped**; the earlier 80-case result remains historical.

The source delta is limited to:

- [PrivateFiles.cs](../../windows/src/AIUsageBar.Core/PrivateFiles.cs): readonly,
  optional per-instance callback through an internal constructor, invoked after
  the validated existing reader closes and before actual `ReplaceFileW`.
- [AIUsageBar.Core.csproj](../../windows/src/AIUsageBar.Core/AIUsageBar.Core.csproj):
  SDK `InternalsVisibleTo` for the existing test assembly; no package added.
- [ClaudeTests.cs](../../windows/tests/AIUsageBar.Tests/ClaudeTests.cs): four
  mandatory `files/deterministic` cases and handle-based evidence helpers.

The public constructor supplies null. There is no static mutable hook, public
setter, environment/config/plugin activation. Actual Win32 replacement, locking,
flush, comparison, verification and cleanup remain in place.

Lead compared SHA256 against the 31-entry planning baseline at
`C:/tmp/aiusagebar-windows-blocker-source-baseline.json`: changed entries are
exactly the three source/project files above, `windows/AGENTS.md` for approved
scope and `windows/README.md` for current test evidence. The other 26 entries
match. Git's tracked Mac source/test/Config/Xcode
diff is empty; no Mac build/test was run on this PC.

## Commands and measured results

QA ran these commands from `windows/` with:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_NOLOGO = '1'
dotnet build AIUsageBar.Windows.slnx -c Release --no-restore
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline --case files/deterministic
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline
```

| Check | Actual result | Runner/MSBuild elapsed | Invocation elapsed | Exit |
|---|---|---|---|---|
| Release build | 0 warnings, 0 errors | 3.77 s | 4243 ms | 0 |
| Focused deterministic suite | selected 4; 4 passed, 0 failed, 0 skipped | 194 ms | 1180 ms | 0 |
| Full mandatory offline suite | 84 passed, 0 failed, 0 skipped | 11748 ms | 12705 ms | 0 |

Completed safe logs, locally ignored:
`build/WindowsValidation/C6/release-build.log`, `focused-deterministic.log`,
`full-offline.log`. Tests report `native=false`, `account=false`,
`experimental=False`; fixtures and child RPC are synthetic. No real provider
CLI/account probe or tee/file experiment was run for this C6 validation.
QA reread its tests, parsed `run-summary.json`, and checked log counts/evidence
and final source hashes. The full run includes the existing file cases; no
additional focused existing-file suite was run.

## Independent source review

The `c6_review` native GPT sub-agent independently traced the full C6 publication
and recovery path read-only, using separate context. Its final result was no
actionable C6 findings, with delivery conditional on Release build, nonempty
focused four-case run and full offline evidence; all three checks above passed.
It ran no tests and made no edits. Review covered the constructor/default path,
reader-close boundary, real native adversary, initial oracle, handle disposal,
later public write and exact-path recovery reopening.

This is native GPT separate-context code review, **not Claude or actual Codex CLI
code review**. The earlier actual Codex CLI review approved the PLAN only;
provenance and dispositions remain in the
[plan review report](../plans/2026-10-05-windows-blocker-amendment-review.md).

## What the cases prove

| Case under `files/deterministic/` | Required oracle and observed outcome |
|---|---|
| `public-create-replace-cleanup` | Public create and replacement produce complete expected bytes; no temp/recovery leftovers; semantic file security checked |
| `noop-create-zero-replace-once-instance-isolation` | Internal counting callback: create 0, replace 1; successful writes and cleanup; separate public instance sharing the root does not inherit the callback |
| `external-replacement-retains-exact-editor-recovery` | Direct successful `MoveFileExW(REPLACE_EXISTING)` moves staged editor E over original A at the boundary; displaced recovery matches independently captured E identity and full bytes |
| `same-ID-equal-length-flushed-edit-retains-exact-recovery` | Direct handle overwrite/flush changes A's bytes to E with equal length and preserved identity before/after mutation; recovery matches captured A identity and complete E bytes |

Both race cases use pairwise distinct A/E/N bytes, require exactly one callback
invocation and independently verify adversary success before publication.
Both throw typed `CoreException` category `publish-conflict`, with target bytes
**N** after replacement. Each requires exactly one new recovery pathname by set
difference, while a pre-existing `recovery-prior` prevents arbitrary-file proof.

Every initial target/recovery inspection uses real handles and checks a regular,
non-reparse, single-link file; protected DACL; current-user owner; exactly the
current user and SYSTEM with explicit allow/full-control rules and no inheritance.
Complete bytes and volume/file-index identity are checked against independently
captured expectations. All inspection/edit handles close before a later ordinary
public publication. The test then reopens the **exact original recovery pathname**
and rechecks identity, full bytes and security; the prior recovery also survives
unchanged. This excludes a surviving orphan handle as evidence of pathname survival.

Both logs report callbackCount=1, adversarySucceeded=true,
typedPublishConflict=true, targetN=true, newRecoveryCount=1,
initialRecoveryMatchesOracle=true, closedInspectionHandlesBeforePublicWrite=true,
exactRecoveryPathReopened=true, retainedIdentityBytesSecurity=true and
universalCAS=false. These are backed by assertions in the cases above.

## Interpretation and remaining gates

This proves **comparison-after-replacement conflict detection and retained
evidence for these controlled successful replacements**. It does not provide
CAS, rollback, all Win32 partial-failure coverage or power-loss durability.
The new target N remains after conflict; recovery preserves displaced editor
content. Earlier timing-dependent stress remains historical/inconclusive.

C1 native isolation and C5 production bridge remain blocked. C5 retains the
strict every-descendant <=2 s and caller exit/stdout/stderr EOF overhead <=500 ms
gates; silence on baseline equivalence does not relax them. No native/live quota,
account import, settings/bridge installation, OS/toolchain setup, autostart,
commit/push or release is established by C6. See the
[prior validation and debug ledger](windows-mvp-validation.md) and
[C1 pinned source audit](2026-10-05-windows-codex-audit.md).

# Windows W1/W2a validation — 2026-10-05

## Latest amendment: C6 deterministic proof

Owner approved the C6 plan and continuous team dispatch (DECISIONS26).
[C6 validation](2026-10-05-windows-c6-validation.md) records the new Release
build (0 warnings/errors), focused4/0/0 and full offline84/0/0 with no skips.
Actual Win32 external replacement and equal-length same-ID edit prove typed
conflict and retained expected editor evidence at the exact pathname after a
later public publication. This is detection plus retention, not CAS/rollback.
C1/C5 remain blocked/strict. The historical W1 and failed/inconclusive
experiments below are preserved, not rerun or relabeled as passing.

Scope approved in DECISIONS25: offline tray/core/tests and isolated synthetic
process/shell/filesystem experiments. W1 is validated in that scope; W2a
experiments expose blockers. This is not a supported live-usage Windows MVP.
No native Codex/Claude/account probe, production settings/bridge install,
autostart, commit, push, binary/tag/release or Mac source change in this round.

## Actual environment and final build

- Windows11 Pro x64, build26200; .NET SDK10.0.401, framework-dependent output.
- Windows development Version0.1.0; final tray assembly0.1.0.0. Mac build6 unchanged.
- Lead ran from `windows/` with DOTNET_CLI_TELEMETRY_OPTOUT=1,
  DOTNET_GENERATE_ASPNET_CERTIFICATE=false, DOTNET_NOLOGO=1.
- `dotnet build AIUsageBar.Windows.slnx -c Release`: exit0,0warnings/0errors,
  elapsed1.47s. All four assets files contain zero NuGet package entries;
  NuGet.Config clears sources, no RID/self-contained download.
- `dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline`:
  lead exit0, **80passed/0failed/0skipped**, elapsed9391ms. QA independently ran
  the final self-gated suite80/0/0, elapsed8708ms.
- Lead logs: gitignored `build/WindowsValidation/lead-build.txt`,
  `lead-offline.txt`, `lead-exits.json`; final timestamp14:06:04 +07.

Default offline tests exercise synthetic data only: strict types/missing/zero,
Codex bucket/window selection, reset/freshness, persisted60s cooldown/300s
policy/relaunch/late-result cancellation/shutdown, metadata-only discovery,
registry/inventory/TOML/command-line bounds, actual fake executable pipe/job
behavior, two-child sequencing/strict wire envelopes/EOF/no-LF/late faults,
stdout and stderr16MiB/4096line limits, private ACL/reparse/hardlink/lock/reader
behavior and quota-only snapshot/error recovery, inert/EOF/overflow/no-EOF sink.
No actual account response or supported CLI guard context was established.

## Real offline tray smoke

Lead rechecked source and ran the final built tray with explicit scratch
`--smoke`: exit0. WinForms message loop/NotifyIcon/menu were real.
Evidence `build/WindowsValidation/tray/tray-smoke.json`:
menuOpenedtrue, refreshCount1, reAddCount1, uiThreadOnlytrue,
iconVisibleBeforeExittrue, iconHiddenOnExittrue, nativeProvidersdisabled,
quotaDataunavailable. TaskbarCreated was sent to our HWND; no Explorer restart.

Single-instance rejection was checked with an own pre-held mutex: app exit3,
no smoke file/icon startup, no unrelated process touched. Evidence
`build/WindowsTraySecondInstance/second-instance.json`.
Final tray apphost SHA256:
`df2dfa14174ee5f7f84728790e3f429be577e204f4bccde3780bfb86d5e29d42`.
Keep its full framework-dependent output directory; EXE alone is insufficient.

Keyboard/Narrator, high contrast, DPI100/150/200%, actual Explorer restart,
sleep/wake, idle-resource measurements, live providers and other OS/architecture
contexts remain untested. The smoke does not imply those passed.

## W2a tee experiment — unsupported

QA final `--tee-experiment`: **34passed/9failed/0skipped out of43**, exit1,
elapsed57745ms. Failures remain explicit, not converted into offline passes.
Ledger is gitignored `windows/tests/AIUsageBar.Tests/bin/tee-experiment-final-2.txt`.
Tools pinned by path/file identity/SHA: Git bin Bash redirector, usr Bash,
GNU tee8.32. Shell syntax check passed. Synthetic clean environment/scratch only.

Both pipeline and input-substitution candidates:

- Normal, invalid parse,3MiB overflow, missing deployment, arbitrary exits and
  parallel stdout/stderr comparisons pass where recorded. Original executes once.
- Slow runtime adds caller EOF latency3095ms(substitution)/3081ms(pipeline),
  exceeding500ms; hung sink hits5s observation timeout.
- Pipeline finite-payload original still waits while producer keeps stdin open.
- TerminateProcess leaves baseline4/candidate6 survivors after2s; verified
  MSYS TERM leaves baseline3/candidate5. Caller EOF remains false. All candidate
  job descendants are counted before harness cleanup, including original child
  tree, substitution hosts, tee and conhost. Harness then removes its own job.

Conclusion: **do not write/install a production tee launcher or installer**.
An alternative supervisor/cancellation design requires a concrete plan amendment
and review. Actual Claude statusline spawn/flags/stdin/kill behavior remains
unaudited. Baseline leakage cannot waive the strict candidate cleanup bar.

## W2a external-editor race — inconclusive

Timing-dependent `--file-experiment` is separate from the deterministic W1 suite.
Final replay: exit1,0passed/1failed/0skipped,3042ms;
142editor moves/3write conflicts/0retained recoveries, exact race not observed.
Earlier stronger replay observed145moves/3conflicts/3complete recovery files,
including2complete displaced-editor byte copies, but its last assertion had an
incorrect read bound. That observation is not a deterministic passing test.

Recovery identity/content comparison is source-reviewed; open-reader replacement,
semantic DACL preservation, locks/first-publication race and bounded safe polling
are proven by offline tests. The precise validation-to-replace external-editor
window remains **unproven/inconclusive**. No universal CAS/power-loss guarantee
or production settings-write permission follows. Logs remain under ignored bin/.

Rapid ReplaceFileW publishing can cause a namespace gap: reader retries once5ms,
then may fail safely/unavailable. Successful reads must contain complete old/new
bytes. Missing snapshot has no receipt, distinct from a valid no-data tombstone;
it cannot freshen old quota. Hardlink/ACL/reparse checks were not weakened.

## Actual Job Object accounting limitation

Limit1 rejected inherited CreateProcess with1816(ERROR_NOT_ENOUGH_QUOTA), while
the **parent-owned** job counters remained total1/terminated0, active0 after exit.
Finish accepted those exact counters: they do not detect every attempted spawn.
A named offline limitation case reports this explicitly.

Separate explicit failed association of an owned suspended secondary produced
1816,total2/active2/terminated0 at measurement; the harness manually terminated
and waited for that never-resumed child. Do not assume failed assignment already
reaped it. This distinguishes failed association from rejected creation.

Exact successful accounting checks and no-breakaway limit1 enforcement stay in
place. Neither counters nor absent completion-port messages prove no spawn
attempt. Microsoft documents [accounting association semantics](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_accounting_information)
and [non-guaranteed notification delivery](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_associate_completion_port).
Native W2b remains blocked, including its MCP detector control, pending the
[C1 source audit/isolation contract](2026-10-05-windows-codex-audit.md).

## Debug and review ledger

| Breadcrumb | Outcome / conclusion |
|---|---|
| Initial65case run48pass17fail | Fixture was future-dated; failed scratch deletion masked original failures; source/Win32/ACL paths needed tracing |
| Fixed past fixture + focused replays | Timestamp hypothesis confirmed; initial assumption every IOException was stdin disposal disproved by safe stack showing Scratch.Dispose |
| Source cleanup hardening | Independently discovered stdin-dispose bypass closed; each stream closes even on failure, owned job termination/wait still runs |
| ACL/junction harness differentials | Protected owner/user+SYSTEM rules pass; textual SDDL control flag equality was invalid; correct junction command fixture passes; no security weakening |
| Late stderr overflow repro | Stopped drainer can block child's writer; Finish now races stream failure with root exit and performs failure cleanup instead of accepting quota |
| Blocked-create versus explicit-assignment controls | Counters do not observe every rejected create attempt; actual enforcement retained and limitation recorded |
| Concurrent reader repro | Namespace gap produces bounded safe unavailable, not partial/fresh data; no unbounded retry |
| Final lead build/offline/smoke |0warnings/0errors;80/0/0; native own-window tray lifecycle passed |

[Independent native GPT source review](2026-10-05-windows-independent-review.md)
closed RPC-success final validation, displaced-recovery deletion and EOF-frame
findings in source. Final source delta also checked stream-failure race,
cleanup-all-streams, bounded reader and LocalState16KiB read/write symmetry.
This is separate-context GPT review, not Claude or Codex CLI review.

SDK first-use in the first worker build automatically installed an ASP.NET
development certificate. User was informed; no trust/removal command ran.
Subsequent commands disable certificate generation and SDK telemetry. This
machine side effect is not an app dependency or an account/provider operation.

## Closure and remaining work

W1 offline implementation delivered; W2a experiments completed with explicit
unsupported/inconclusive gates. Source/secret/scope/XML/JSON/Markdown checks and
lead artifact reread apply to this delivery. W2b native isolation, W3 account
quota/compatibility import and W4 production bridge/settings remain gated.
Mac source/project unchanged; no Windows support inferred from Mac126tests.

# Windows H2 — preparation and synthetic inventory evidence

2026-10-06 · Windows 11 x64 · .NET SDK 10.0.401/runtime 10.0.12.
Owner approved both preparation plan and dispatch (DECISIONS33). Native H2
execution is outside this authorization. This record covers synthetic fixtures,
owned test children and private scratch files only.

## Source and behavior

The approved source boundary is three files:

- `windows/src/AIUsageBar.Core/CodexProvider.cs`: internal immutable per-instance
  inventory whitelist. Public transport retains its previous method set. H2
  accepts only initialize, experimentalFeature/list, config/read and
  configRequirements/read; rejects account/quota requests before writing stdin.
- `windows/tests/AIUsageBar.Tests/H2InventoryTests.cs`: test-only launch policy,
  fresh synthetic environment, typed bounded validators, independent literal
  wire fixtures, fake child and cleanup/negative tests.
- `windows/tests/AIUsageBar.Tests/Program.cs`: explicit fake child routing and
  26 mandatory offline H2 cases. No native route was added.

The matrix covers handshake fields, config guards, tagged source layers,
scalar-leaf origins, requirements, feature registry paging/types/bounds,
invalid envelopes and owned cleanup. Every executable is this project's test
fixture. Fake inventory does not invoke FakeCodexProvider's quota coordinator.
The public transport regression uses synthetic quota frames only.

Scratch is assembly-derived `build/H2Preparation/<guid>/private`, with pinned
ordinary ancestry and protected stores. Fake child arguments, marker, output
handles and full request bodies are validated. Snapshot validates the exact
one-level manifest and file identity/ACL before reading synthetic contents.
Constructor failures release acquired resources. These properties are test
contracts, not evidence about a native provider's filesystem accesses.

## Verification ledger

Lead runs from `windows/` with telemetry opt-out, ASP.NET certificate generation
disabled, no logo and workload update notifications disabled. Existing SDK and
packs only; no restore, installation or dependency download.

```powershell
dotnet build AIUsageBar.Windows.slnx -c Release --no-restore
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build --no-restore --no-launch-profile -- --offline --case h2-inventory/
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build --no-restore --no-launch-profile -- --offline
```

Safe local logs are under ignored `build/H2PreparationValidation/`.
Final Release build and mandatory offline suite pass on the corrected source.
Every test run reports native=false, account=false, experimental=False.

| Final check | Result | Suite/build elapsed | Invocation elapsed | Exit |
|---|---|---|---|---|
| Release build after teardown correction | 0 warnings/errors | 1.22 s | 2023 ms | 0 |
| Corrected cleanup replay | 13 passed, 0 failed, 0 skipped | 3810 ms | 5548 ms | 0 |
| Corrected focused H2 | 26 passed, 0 failed, 0 skipped | 6581 ms | 7762 ms | 0 |
| Corrected full mandatory offline | 110 passed, 0 failed, 0 skipped | 18061 ms | 19232 ms | 0 |

Final logs: `release-build-teardown.log`, `cleanup-corrected-1.log`,
`focused-h2-final.log`, `full-offline-final.log`. The corrected cleanup group
passes in the targeted replay, focused H2 and final full run. This establishes
observed mitigation on this host; it is not a universal stress/OS-ordering proof.

| Earlier check | Result | Suite/build elapsed | Invocation elapsed | Exit |
|---|---|---|---|---|
| First Release build | CS8602 at H2InventoryTests.cs:38–39; 2 errors | 4.78 s | 5406 ms | 1 |
| Nullable-corrected build | 0 warnings/errors | 1.13 s | 1408 ms | 0 |
| Initial focused H2 | 26 passed, 0 failed, 0 skipped | 4102 ms | 4962 ms | 0 |
| Initial full offline | 106 passed, 4 failed, 0 skipped | 20465 ms | 21988 ms | 1 |
| Cleanup reproduction | 9 passed, 4 failed, 0 skipped | 4355 ms | not recorded | 1 |

Logs: `release-build.log`, `release-build-final.log`, `focused-h2.log`,
`full-offline.log`, `cleanup-repro.log`. Earlier failures are retained and never
relabelled as passes.

## Debug record

The compiler could not infer nullable flow through custom Require checks.
Explicit `Parent ?? throw Invalid()` preserves fail-closed path validation;
the next Release build had zero errors and warnings.

The full run then exposed intermittent IOException/80070020 at scratch
File.Delete, despite a passing initial focused run. The 13-case reproduction
failed in different scenarios, ruling out one specific malformed RPC fixture.
Source tracing found that production job disposal checks ActiveProcesses zero
without explicitly awaiting the retained primary process handle before test
scratch deletion. Fake child output handles deny delete sharing. This is a
consistent teardown-race hypothesis, rather than proof of exact kernel ordering.
The test-only correction retains the primary Process.SafeHandle immediately
before RPC and performs a bounded WaitForSingleObject signal wait after awaited
transport/process disposal, before context deletion. It covers all three owned
starts and constructor/invalid-response/cancellation exits. Cleanup has its own
fresh two-second budget; no delete retry, PID reopen during teardown or unrelated
kill is used. Handle-retention failure disposes the original owned process and
surfaces failure; it does not certify an exit barrier in that failure path.
Corrected targeted, focused and full runs above no longer reproduce the sharing
failure. No production process code is changed.

## Review provenance and limits

The actual Codex CLI reviewed the preparation PLAN, with final PROCEED and
0 Major/0 Minor/2 Info; see the companion plan review. Implementation review
uses an independent native GPT research thread, not actual Codex CLI or Claude
code review. Existing threads handled Core, QA and read-only review separately
because new thread dispatch was unavailable. Lead re-reads their artifacts
before every downstream build/test. Independent static review accepted the
nullable correction and retained-handle ABI/ownership/all-exit paths before
replays; final safe logs confirm the counts above.

## Final self-gate

Lead compared the 26-entry C1/C5 source baseline: only CodexProvider.cs and
Program.cs changed among those existing entries; H2InventoryTests.cs is new.
Other production policies/process/discovery/Tray/Bridge/projects/dependencies
are unchanged. Tracked Mac source/test/Config/Xcode diff is empty. Existing
uncommitted project work is retained, with no commit or push.

| Source | SHA256 |
|---|---|
| CodexProvider.cs | E885ED0C9FFDB8FDADA00B55648EDCCB2D443335E879835BF4DCACBD4138709E |
| Program.cs | 847850E350E3730A325CF0926EACD75BBEE233F5EEAF218D22574995F54B6DCC |
| H2InventoryTests.cs | B4C010271C487DFA696BC91EA65D0D520C692882E33A4ED21EB49BE6E26DDA69 |

The three-file scope, no native route, typed fixtures, pre-stdin method denial,
cleanup ownership, fake-only execution and dependency restrictions were checked
against the approved brief. Compilation and nonempty focused/full suites supply
syntax and behavior evidence. Documentation was re-read against measured logs.

Preparation cannot establish strict isolation, no egress, absence of rejected
spawn attempts, C1/C5 pass or native compatibility. Native H2 requires a
separate reviewed amendment, provider-secret-path closure, observation manifest,
fresh prerequisites and explicit run approval. Missing access/network
observations remain unknown. No native provider, real account, settings change,
installer, commit, push or release was performed.

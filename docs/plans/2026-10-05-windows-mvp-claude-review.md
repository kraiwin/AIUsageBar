# Windows MVP — actual Claude plan review

Date: 2026-10-05 Asia/Bangkok. Verdict: **fix-then-proceed for PLAN**. Implementation/dispatch remains paused pending disposition.

## Provenance and scope

- Reviewer: native Claude Code CLI 2.1.289, direct `claude.exe -p` invocation; no Codex role was used as a substitute for Claude.
- Launch: `--safe-mode --strict-mcp-config --tools "" --disable-slash-commands --no-session-persistence --output-format json --permission-mode dontAsk`.
- CLI exit code 0; result subtype success, is_error false, one turn, 230813 ms. Stderr empty. These prove completion of the static model review, not Windows behavior.
- Input: numbered copies of the Windows MVP plan as of this review, Mac CodexLaunchPolicy.swift, ClaudeOriginalCommand.swift and ClaudeBridgeInstaller.swift; no tools enabled, no external-document checks or executed tests.
- Local raw result: `C:/tmp/claude-reviews/result-20261005-115213.json`; input: `C:/tmp/claude-reviews/prompt-20261005-115213.txt`. Raw CLI envelope is not committed.
- The following reviewer text is preserved verbatim. Its line citations refer to the numbered input snapshot, before this report was linked from the plan.
Reviewer model reported in CLI metadata: `claude-opus-5-5`.

## Claude review (verbatim)

**Intent:** Add a native Windows tray edition that reproduces the Mac's guarded Codex quota read and Claude statusline capture. W1–W2 must prove process, isolation, and bridge behavior offline before any live account or settings activity.

**Simpler alternative (bridge only):** Drop the native-wrapper launcher and its readiness handshake. Let the Bash launcher run the original command itself and split stdin to a capture-only sink: `tee --output-error=warn-nopipe >(capture-sink) | bash -c "$orig"`. The original's stdin, stdout, stderr and exit code then never pass through .NET. A missing apphost, DLL or runtime only loses the capture. There is no pre/post-handoff boundary and no duplicate-run risk. This removes most of M3 and the hardest part of line 200. Tee behavior in Git Bash still needs a W2 proof, but that proof is much smaller.

## Flows traced

- **Codex two-child sequence.** Plan :98–105 against `CodexLaunchPolicy.swift:24-41` (static overrides plus the child-B `mcp_servers` override), `:62-74` (registry), `:76-110` (inventory and same-inventory check).
  - Verified Mac behavior: runtime assertions only. There is no executable binding, and child A runs with the user's real MCP entries enabled (`requireDisabled:false`).
  - Planned Windows contracts: the digest binding, the Job Object, and the inert-bridge placeholder.
- **Original-command passthrough.** Plan :118–120 against `ClaudeOriginalCommand.swift:27-121`.
  - Verified Mac behavior: a POSIX process group, signal forwarding, a 0.5s post-exit drain, a bounded capture, and a 2s ingest deadline (`:142`).
- **Install and restore.** Plan :122–126 against `ClaudeBridgeInstaller.swift:71-115` and `:119-196`.
  - Verified Mac behavior: artifacts are written before settings, the checkpoint is written before cleanup, and each unlink is fsynced with metadata removed last (`:190-195`).

## Findings

**1. Blocker: the W2 "isolation" boundary is undefined and can cross the unauthorized W3 line.**
- **Evidence:** plan :195 ("isolated scratch home/config/workdir, no user auth copy"), :103 (handles are allowlisted, but the environment block is not specified), :99.
- **Consequence:**
  - The inherited environment (`OPENAI_API_KEY`/`CODEX_API_KEY`, `CODEX_*`, proxy vars, `RUST_LOG`) can authenticate an "isolated" child. That allows account-scoped network activity at startup, which W3 is not authorized for.
  - On Windows, overriding `USERPROFILE` does not redirect known-folder lookups. Rust's `dirs` crate uses `SHGetKnownFolderPath`; confirm this in the :194 source audit. So "scratch home" may still read the real profile.
  - Credential Manager is per-user and cannot be redirected by environment variables.
  - `RUST_LOG` can also trip the 4096-line stderr bound (:104).
- **Minimal change:** Specify the harness launch contract.
  - Build the environment block from scratch (`SystemRoot`, `PATH`-minimal, `TEMP`, scratch `CODEX_HOME`) and strip `OPENAI_*`, `CODEX_*`, `*_PROXY` and `RUST_LOG`.
  - Force file-based credential storage via `-c` (verify the 0.160 key name).
  - Set `HTTPS_PROXY`/`HTTP_PROXY` to a dead loopback port so any network attempt fails visibly.
  - Make the audit enumerate every home-relative read, and disclose real-profile reads as non-isolated.
  - Apply the same environment policy to the production launch (defined there to preserve CLI-managed auth).

**2. Major: the W2 positive-control gate is not satisfiable as written; an enforceable Windows mechanism is available but unused.**
- **Evidence:** :196 requires a positive control for "each claimed canary … without model/API use". :195 forbids thread requests, and :100 limits RPC to four methods. In app-server, stdio-MCP, hook and notify invocation happens around threads or turns, not during `initialize`/`config/read`. So no permitted action can trigger them.
- **Consequence:** The gate either cannot pass or gets satisfied by running the canary binary directly. That proves nothing about Codex. Child A's real-MCP exposure (Mac `:76`, `requireDisabled:false`) stays observation-only.
- **Minimal change:** Set `JOB_OBJECT_LIMIT_ACTIVE_PROCESS = 1` on each per-operation job (:103), and assert `JOBOBJECT_BASIC_ACCOUNTING_INFORMATION.TotalProcesses == 1` at cleanup.
  - Positive control: a fake child that spawns is blocked or counted. This is testable offline in W1.
  - This turns "no process-based guard was invoked" into enforcement that also holds in W3.
  - Keep config assertions for the non-process paths (HTTP MCP, OTEL, analytics).
  - If `codex.exe` 0.160 legitimately spawns something at startup, it fails closed and that is recorded as a W2 finding.

**3. Major: W2 proof is unauthenticated, but acceptance treats it as guard validation "before real account fetch".**
- **Evidence:** :242, and W3 :204–206 (the first live run is "quota-only real fetch").
- **Consequence:** Authenticated sessions may load layers or flags that W2 never saw: cloud or managed requirements, and server-side feature state. Plan :55 calls these unsupported but has no way to detect them in W2.
- **Minimal change:** Define W3's first live run as a guard-proof run with explicit pass criteria before quota is trusted: registry and effective-config assertions, exact inventory, job `TotalProcesses == 1`, zero owned children. Reword :242 to "validated in isolation (W2) and re-asserted under auth at W3 entry".

**4. Major: "revalidation" after an executable change has no defined actor or procedure.**
- **Evidence:** :98 ("changed digest invalidates proof and requires revalidation before quota"), :112.
- **Consequence:** If the tray only accepts digests proven by a developer-run W2, every Codex update bricks the app until a rebuild. If the tray "revalidates" using its runtime assertions alone, the digest binding adds nothing. Mac has no such binding.
- **Minimal change:**
  - Persist the confirmed record: path, file ID, size, version, SHA-256.
  - On a mismatch, enter a "Codex changed, reconfirm" state that needs a user click, then rely on the per-launch runtime assertions plus the job-limit enforcement from finding 2.
  - Owner decides whether an unknown version without W2 evidence is blocked or warn-and-allow. Record that decision before W1.

**5. Major: the bridge launcher handshake has no feasible channel specified, and it does not own cancellation across MSYS.**
- **Evidence:** :119, :200, :118.
- **Consequence:**
  - Extra file descriptors (fd 3 and up) from Git Bash are not inherited as usable handles by native Windows programs. The obvious readiness channel therefore doesn't exist, and file-based handshakes add polling and concurrency races between overlapping statusline invocations.
  - An MSYS `exec` of a native program leaves a stub bash process. If Claude terminates that bash process (likely TerminateProcess, unverified), the native wrapper and the original command are orphaned. On Mac this case is handled by process-group signals (`ClaudeOriginalCommand.swift:39-45`, `:107-108`).
  - Passing the original command through native argv also exposes it to MSYS path conversion and globbing.
- **Minimal change:** Adopt the tee-split design from the top. If the native wrapper is kept instead, then:
  - Specify the channel, e.g. a `wrapper --probe </dev/null` that prints a magic token, followed by `exec`.
  - Have the wrapper read the original command from the digest-verified install manifest rather than from argv.
  - Add parent-death detection (open the parent process handle and wait on it) that terminates the wrapper's job.
  - Make "Claude kills the shell mid-run" a named W2 case.

**6. Major: the atomic-replace contract is internally inconsistent, and the snapshot reader can break the writer.**
- **Evidence:** :124–125, :111 (5s snapshot poll), :121.
- **Consequence:**
  - The temp file is created with "no sharing", but rename-replace needs DELETE access to it.
  - `ReplaceFileW` is path-based, has partial-failure states (`ERROR_UNABLE_TO_MOVE_REPLACEMENT[_2]`), and requires releasing the validation handles.
  - Handle-based rename preserves the *temp* file's DACL, not the destination's. So "atomic + handle identity + preserve existing ACL" is not met by any single primitive as written.
  - If the tray opens the snapshot without `FILE_SHARE_DELETE`, the bridge's replace fails with a sharing violation every time they overlap. That produces spurious capture errors.
- **Minimal change:** Specify the contract concretely:
  - Open the temp file with `DELETE` access.
  - Copy the destination's security descriptor (`GetSecurityInfo`) onto the temp file.
  - Recheck the destination's file ID and bytes.
  - Call `SetFileInformationByHandle(FileRenameInfoEx, REPLACE_IF_EXISTS | POSIX_SEMANTICS)`, then reopen and verify.
  - The tray reader must open with `READ|WRITE|DELETE` sharing and retry on a torn read.
  - Document the remaining window (between recheck and rename) as the disclosed race.

**7. Minor: restore and reinstall cleanup cannot delete in-use bridge images on Windows.**
- **Evidence:** :126, :120 against Mac `ClaudeBridgeInstaller.swift:190-195`. On Mac, unlinking a running binary succeeds.
- **Consequence:** A statusline invocation still running from pre-restore settings causes `DeleteFile` on the apphost or DLL to fail. Restore stops mid-cleanup with a generic I/O failure.
- **Minimal change:** After the settings restore is checkpointed, retry deletion of locked artifacts within a bounded time. If still locked, report "cleanup pending" and keep the manifest; never delete the manifest early. Add an "artifact in use" case to the W4 interruption matrix.

## Verdict

**fix-then-proceed** for the PLAN.

The biggest reason is finding 1. W2's central claim, "isolated, no auth", has no concrete launch contract covering environment, home resolution, credential store and network. As written it can both produce an unsound proof and drift into account activity that the owner has not authorized.

Findings 2 and 5 should be resolved in the plan text before dispatch. Each has a simpler, testable replacement: job process limits for the no-spawn proof, and the tee-split launcher for the bridge. Findings 3, 4, 6 and 7 are small edits to plan text.
## Lead triage — claims versus evidence (historical, before corrections)

All seven findings are recorded for disposition; none of the proposed replacements is adopted solely because a reviewer suggested it. Owner's W1-W2 scope approval remains valid, but the plan needs corrections/verification before dispatch.

- C1 isolation: accepted as a plan-contract gap. Environment, credential-store selection, known-folder reads and network boundaries need an audited test contract. A dead HTTP proxy is not proof of zero network access; code can bypass it. Production must preserve the explicitly approved CLI-managed auth lifecycle, rather than silently applying a no-auth test environment.
- C2 positive controls: partially disputed. Existing Mac evidence in `../validation/2026-10-03-codex-inventory-prerequisites.md` and `../validation/2026-10-03-codex-startup-side-effects.md` records `mcpServerStatus/list` invoking synthetic MCP without threads or model turns. Those documents were not in Claude's four-file input. Thus the broad claim that no offline MCP positive control is possible is not established. The Windows plan still needs an explicit test-only RPC exception and separate hook/notify source-audit coverage. The proposed job active-process limit requires a Windows spike and precise accounting semantics before adoption; enforcement is not automatically equivalent to the original canary gate.
- C3 authenticated entry: accepted clarification pending plan edit. Isolated W2 evidence must remain distinct from runtime invariants re-asserted at W3 entry; no W3 activity is authorized here.
- C4 executable-change revalidation: accepted lifecycle gap. Use fail-closed unknown-version/proof invalidation under existing scope unless owner explicitly chooses another policy; do not silently introduce warn-and-allow.
- C5 bridge handshake/cancellation: unresolved design detail. Tee-split and native-wrapper alternatives both require actual MSYS byte/backpressure/early-exit/cancellation proof. A separate `--probe` then exec does not itself prove readiness of the later invocation. Do not replace the design without comparing these flows and recording why.
- C6 file contract: accepted specificity gap; proposed Win32 flags and APIs remain unverified suggestions. Reader sharing, security descriptor handling, replacement failures, remaining editor race and postconditions need source documentation and an isolated Windows spike before closure.
- C7 in-use artifact cleanup: accepted Windows-specific test requirement; bounded cleanup-pending state must retain ownership metadata and support retry.

No Windows source/build/test/probe or real user settings/account operation occurred in this review. This report is a static cross-model review, not runtime validation.

## Follow-up

Original reviewer text and the initial lead triage above are preserved as history. C1–C7 plan corrections were reconciled, then reviewed twice through the global opus/high launcher. See [verbatim delta results and lead dispositions](2026-10-05-windows-mvp-claude-delta-review.md) for the conditional review/lead closure and remaining Windows proof gates; no runtime pass is implied.

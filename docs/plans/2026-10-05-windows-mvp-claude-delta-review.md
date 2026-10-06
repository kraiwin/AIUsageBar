# Windows MVP — actual Claude delta reviews

Date: 2026-10-05 Asia/Bangkok. Original C1–C7 review remains in [original report](2026-10-05-windows-mvp-claude-review.md). No Windows source/runtime validation occurred in this document/review round.

## Delta 1 — reconciled C1–C7 plan

Main Codex called the existing global launcher directly; actual review is from Claude CLI, not a GPT substitute. Requested opus/high; reported model claude-opus-5-5. Launcher exit0/validation passed; Claude exit0/is_error false/subtype success. Duration 310799ms; stderr empty.

- Exit: `C:/tmp/claude-reviews/20261005-122910-107480-windows-plan-delta-38488-exit.txt`
- Result: `C:/tmp/claude-reviews/20261005-122910-107480-windows-plan-delta-38488-result.json`
- Numbered input: `C:/tmp/claude-reviews/windows-plan-reconciled-input.txt`
- Reviewed plan bytes SHA256: `73ab68d017bacf7f1223d132dbe40ce3d0f6d18ae65ae890727f3155ffc74878`
- Input bytes SHA256: `8e826140cb242ef63f31fde569fc4e18a008d750ff19d71f5d97102f3a03f864`

Verdict: **fix-then-proceed**, 1 major / 7 minor. Excerpts included plan, original review, Mac MCP inventory/startup evidence, Mac launch policy/original-command/installer source. All line references below bind that input snapshot, not later plan line numbering. Reviewer assumptions about Git Bash redirectors, conhost and rename semantics are explicitly unverified.

## Delta 1 verbatim result

# Windows MVP plan: reconciled delta review (C1–C7)

In this review, `plan:N` means `docs/plans/2026-10-05-windows-mvp-plan.md` line N. `review:N` means `docs/plans/2026-10-05-windows-mvp-claude-review.md`. Other files are cited by base name. I read only the supplied excerpts. I ran nothing and browsed nothing.

## Goal and simpler alternative

**Goal:** decide whether the reconciled plan is specific enough to dispatch W1 (offline core, tray, fake-process transport) and W2 (isolated harnesses). The native Codex probe and the production Claude bridge stay explicitly blocked.

**Smaller alternative:** split W2 into two named sub-phases.
- **W2a:** fake-job accounting, the tee experiment, and scratch filesystem primitives. None of these launch the CLI.
- **W2b:** the native Codex harness, gated on the C1 audit record.

The plan already behaves this way (plan:111, :134, :217) but never names the split. Naming it would make the dispatch boundary for agents mechanical rather than interpretive. This fits naturally with finding 4.

A second simplification: defer compatibility-record *import* (plan:100, :162) to W3. In W1–W2 nothing consumes the record for a live fetch, and its schema is not yet defined (finding 7).

## Flows traced

**Codex: discovery → child A → child B → cleanup** (plan:100–107, :113–119)
- Discovery: tuple of path, version, file ID, size, SHA-256 → compatibility record.
- Launch: clean environment block → `CreateProcessW` suspended → job assigned with an active-process limit of 1 → resume.
- Child A: initialize → paginated registry → `config/read` inventory → close stdin → reap.
- Before B: job accounting check → command-line length check using A's inventory.
- Child B: launch with placeholder overrides → registry, config and same-inventory assertions → quota (W3 only).
- Cleanup: bounded wait → terminate job → wait for zero processes → dispose handles.
- The sequencing matches the Mac reference (`CodexLaunchPolicy.swift:28-41`, `:76-111`). The plan correctly places the length check after A (plan:105, :298).

**Claude: statusline → launcher → snapshot → tray** (plan:132–142)
- Claude's Git Bash → launcher → `tee >(sink) | bash -c "$orig"`.
- The sink takes a bounded copy → temp file → replacement → snapshot or error marker.
- The tray reads with full sharing every 5 s.
- Restore and cleanup: plan:143, C7.

**Settings replacement** (plan:140–141)
- Validate the destination under a read handle with pinned ancestors.
- Write the temp file, close it, recheck the destination, release.
- `ReplaceFileW` with flags 0 and a recovery backup → reopen and verify postconditions.

## Verified versus claimed

**Verified by supplied evidence (macOS only):**
- 0.160.0 `mcpServerStatus/list` invokes synthetic stdio MCP servers without thread or model requests (`codex-inventory-prerequisites.md:14,26`; `codex-startup-side-effects.md:19`). This does refute the broad claim at review:48.
- Source-tag layer and override semantics.
- PC inventory metadata (plan:21–31).

**Claimed or untested on Windows:**
- All job-object behavior and accounting.
- Console and conhost interaction with jobs.
- Codex Windows bootstrap paths and the credential-store key.
- GNU tee `--output-error` behavior under MSYS.
- How Claude spawns, feeds stdin to, and kills the statusline process on Windows.
- `ReplaceFileW` and rename behavior with open readers.
- Every UI and accessibility item.

The plan labels these honestly as unproven (plan:13, :280, :316).

## Findings

### 1. Major: the tee-split acceptance criteria are not tied to the baseline command and miss two failure modes the pipeline introduces

**Evidence:** plan:132–134, :217, :264. Mac reference `ClaudeOriginalCommand.swift:55-63`, which applies a 0.5 s post-exit deadline independent of stdin EOF.

**Consequence:** W2 can produce a false pass or a false fail.

- **(a) New dependency on stdin EOF.** In `tee … | bash -c "$orig"`, the pipeline does not complete until tee exits, and tee exits only at stdin EOF. If Claude keeps stdin open after writing the payload, the launcher hangs after the original has finished. Running the original directly would have exited. The listed tests (plan:133) cover an early-exiting original but not a producer that keeps stdin open.
- **(b) Caller-pipe lifetime.** Claude's completion likely waits for EOF on the stdout and stderr pipes, not only for process exit. Two ways this could stretch the statusline:
  - the process-substitution subshell or the native sink inherits the launcher's stdout or stderr handle (MSYS-to-native handle inheritance is unverified);
  - sink output is not redirected *inside* the substitution (`>(sink >/dev/null 2>&1)`).
  
  In either case a slow sink lengthens or pollutes the statusline even though the exit code is correct. "Suppress tee/sink diagnostic output" (plan:132) does not state where the redirect must sit, and no test measures caller-side EOF timing.
- **(c) Cancellation criterion is undefined in three ways:**
  - **Target:** `Git/bin/bash.exe` is commonly a redirector stub for `usr/bin/bash.exe` (an assumption, not in evidence).
  - **Primitive:** `TerminateProcess` gives no chance for traps; an MSYS signal does.
  - **Bar:** "all descendants terminate within the measured deadline" (plan:134) is stricter than what happens when Claude kills a directly run original today. Without a job, the original's descendants may also outlive the shell in the baseline. As written, the gate can fail for reasons the bridge did not cause, which pushes toward an invented supervisor.
- **(d) Nested shell semantics.** The nested `bash -c "$orig"` loses non-exported shell state if Claude invokes its shell with different flags (for example login or `-l`). The plan does not require recording Claude's exact invocation.

**Correction:** state these in the W2 experiment contract.
- Every case runs twice: the original run directly under the same harness invocation, and the same original through the launcher. Pass means identical stdout and stderr bytes, identical exit code, single execution, and **no added latency to caller-pipe EOF or launcher exit beyond a stated bound**.
- Add a "producer holds stdin open after the payload" case.
- If (a) fails, the launcher must not wait on tee. One example shape: `bash -c "$orig" < <(/usr/bin/tee --output-error=warn-nopipe >(sink >/dev/null 2>&1) 2>/dev/null)`, whose exit status is simply `$?`. This must be measured, not assumed.
- Cancellation:
  - Name the killed process: the exact process the harness spawned, mirroring Claude.
  - Name the primitive: `TerminateProcess`.
  - Pass means survivors are no more than in the baseline run, plus a sink that exits within its own deadline.
- Pin `/usr/bin/tee` by absolute path.
- Source-audit Claude's shell flags, stdin close behavior and kill method alongside the existing termination audit (plan:134).

### 2. Minor: console creation mode is unspecified, and `TotalProcesses == 1` depends on it

**Evidence:** plan:105 ("hidden console") versus plan:117 (limit 1; `TotalProcesses == 1` on an accepted run).

**Consequence:** "Hidden" usually means `CREATE_NO_WINDOW`, which still creates a console host. My assumption (not verified, not in evidence): on Windows 8+, `conhost.exe` is associated with the client's job. If so:
- the limit blocks console creation, or the accounting reports 2, and
- the C2 candidate is falsely blocked under the plan's own rule (no weakening of the limit).

Also, "query accounting after … handle cleanup" (plan:117) could be read as closing the job handle first. With kill-on-close, that ends the job and the query becomes impossible.

**Correction:** specify `DETACHED_PROCESS` (stdio over the pipe handle list, no console) for Codex jobs, or state the expected conhost count explicitly. The W1 fake child must be a console-subsystem executable launched with exactly the production flags. Query accounting before closing the job handle.

### 3. Minor: the snapshot writer's replacement primitive is unnamed

**Evidence:** plan:142 ("same-volume replacement"; reader shares read, write and delete). C6 disposition at plan:325.

**Consequence:** The reader side is fixed, but whether the writer succeeds while a reader handle is open depends on the primitive.
- A plain `MoveFileExW(REPLACE_EXISTING)` or .NET `File.Move(overwrite)` may fail when the destination has open handles, unless POSIX rename semantics apply. This is an assumption, not covered by the supplied contracts.
- That would produce exactly the spurious capture errors C6 was meant to remove. The bounded retry at plan:139 masks this only probabilistically.

**Correction:** name the primitive for private immutable files, for example:
- `SetFileInformationByHandle(FileRenameInfoEx, REPLACE_IF_EXISTS|POSIX_SEMANTICS)`, or
- `ReplaceFileW` with a private backup.

Forbid `File.Move` for this path. Make "reader handle open during replace succeeds, reader sees old or new content consistently" a named W1 case.

### 4. Minor: the native-probe lockout is procedural only, while agents will be dispatched

**Evidence:**
- plan:111, :212 (native startup forbidden until the audit);
- plan:310 (backend-dev owns "isolated compatibility probes");
- plan:100 (version is persisted, but the source of the version is unstated).

**Consequence:** An implementer can run the native harness, or `codex.exe --version`, "to test the harness" before the C1 audit exists. That crosses the stated gate.

**Correction:**
- The harness's native mode refuses to start unless given an explicit flag plus a lead-recorded audit file bound to the executable tuple.
- Dispatch instructions state that native mode is written but not executed in W1–W2.
- W1 discovery derives the version only from package metadata and never executes `codex.exe`, including `--version`.

### 5. Minor: environment allowlist values are ambiguous

**Evidence:** plan:113. Only `TEMP`/`TMP`/`CODEX_HOME` are explicitly scratch. `HOME`, `USERPROFILE`, `APPDATA` and `LOCALAPPDATA` are listed without a stated value.

**Consequence:** These could be populated with the real profile values, which defeats the purpose. Separately, variables absent from the block may cause fallback to real locations rather than failure: `ProgramData` (a plausible system-config root on Windows), `SystemDrive`, `PATHEXT`.

**Correction:** state that all profile variables point into the scratch root. Add to the C1 audit checklist what the exact build does when `ProgramData` and `SystemDrive` are absent.

### 6. Minor: three C6 detail gaps in the settings-replacement candidate

**Evidence:** plan:140–141.

1. **Ancestor share mode.** Pinned ancestor handles are "without delete sharing", but their read/write share mode is unspecified. If they deny write sharing, the rename inside `ReplaceFileW` fails every time.
2. **Unused file-ID check.** The supplied API contract says the resulting file ID is the replacement file's ID. The plan only says "do not assert it preserves file ID". Recording the temp file's ID before closing it, then asserting equality afterwards, would cheaply detect temp substitution in the close-to-replace window.
3. **Backup location unstated.** The recovery backup's location is not specified: the user's `.claude` directory or the private same-volume directory.

**Correction:** specify `FILE_SHARE_READ|FILE_SHARE_WRITE` for pinned ancestors, add the post-replace ID equality check, and state the backup directory and its cleanup owner. The W2 scratch tests already planned cover the rest.

### 7. Minor: compatibility-record schema is undefined, yet W1 implements its import

**Evidence:** plan:100, :162, :323.

**Consequence:** W1 would invent a format before the W2 harness that produces it exists. This risks a second, divergent format.

**Correction:** either add a minimal v1 schema to the plan, or (simpler) defer import to W3. W1 then only persists the tuple and shows the "Codex เปลี่ยนแล้ว" state.

### 8. Minor: the W1 sink needs a required scratch target and must drain stdin independently

**Evidence:** plan:133, :135, :176.

**Consequence:**
- A sink that defaults to `%LOCALAPPDATA%/AIUsageBar/Windows/` could write test snapshots where the W1 tray reads real state.
- Its self-enforced deadline gives no bound while the .NET runtime is still starting. Input larger than the pipe buffer then stalls tee and the original command.

**Correction:**
- In W1–W2, the capture mode takes a required explicit output directory with no default.
- A dedicated reader drains stdin into the bounded buffer and discards beyond 2 MiB, never blocked by parsing, locking or writing.
- The "large input + slow runtime start" case is measured, as already listed.

## Examined with no material finding

- **C1:** gating logic, the dead-proxy-as-tripwire-only wording (plan:115), and "blocked audit ≠ permission" (plan:212).
- **C2:** test-only RPC kept out of the production provider (plan:102, :164); accounting interpretation consistent with the supplied `TotalProcesses` semantics (plan:117).
- **C3:** re-assertion at W3 entry (plan:119, :221).
- **C4:** fail-closed policy on unknown versions, with no warn-and-allow (plan:100).
- **C7:** cleanup-pending flow (plan:143).
- **Manifest:**
  - no `OriginalCommand.cs`;
  - W4 files marked as deferred;
  - four projects match the `.slnx`;
  - no current task calls for native readiness or a fallback (plan:328).
- **Disposition table:** consistent with design text.
- **Historical wording:** clearly marked historical (plan:292, :308).

## Assumptions and missing context

- The following are from general knowledge, not evidence:
  - the Git Bash stub/redirector;
  - conhost membership in the job;
  - NTFS rename behavior when the destination has open handles.
- Claude Code's Windows statusline spawn, stdin and kill behavior is unknown to this review.
- Production (W3) environment policy (inherit versus allowlist, and API-key variables changing auth mode) is not specified. It is out of scope for W1–W2 and should be decided at W3 entry.
- Binary-to-source correspondence for the npm-distributed `codex.exe` is not established. The C1 audit is a source audit of tag `rust-v0.160.0`, and the tuple binding hashes the binary, not provenance.

VERDICT: fix-then-proceed

## Lead disposition for delta 1

| ID | Disposition | Verification still required |
|---|---|---|
| D1 major | Accepted: paired direct-original/candidate cases, held-open producer stdin, caller EOF/exit bound500ms, sink redirection inside substitution, pinned tee, exact shell/kill target. Compare pipeline/input-substitution candidates; no claimed success. Keep strict2s gate for every descendant in the candidate launcher tree (nested original host, original children, substitution hosts, tee and sink); baseline leakage recorded separately, no relaxed cleanup | W2a byte/exit/startup/backpressure/EOF/TerminateProcess/signal tests. Claude actual flags/stdin/kill behavior audit before live install; candidate unsupported on failure |
| D2 minor | Accepted clarity: detached suspended Unicode/extended startup flags with pipe stdio, fake console children identical to production; query accounting before job close | Microsoft flags docs checked. Conhost claim remains reviewer assumption, not established fact; fake/runtime behavior must pass |
| D3 minor | Accepted: private snapshot ReplaceFileW flags0/recovery backup; absent no-overwrite rename, no File.Move fallback; named open-reader replacement test | W1 replacement/ACL/old-new content/cleanup; no assumed NTFS result |
| D4 minor | Accepted: native mode requires explicit flag and tuple/policy-bound lead audit file, W2a/W2b split; discovery reads metadata only | Audit incomplete, initial dispatch must not execute native Codex/Claude including version commands |
| D5 minor | Accepted explicit scratch values for every profile variable; audit missing ProgramData/SystemDrive/PATHEXT fallbacks | Version-pinned C1 source coverage remains prerequisite |
| D6 minor | Accepted: ancestor read/write sharing without delete, temp ID/output ID equality, same-volume protected private recovery directory and installer ownership | W2a sharing/reparse/race/partial-failure tests; W4 production deferred |
| D7 minor | Accepted simpler scope: defer compatibility-record schema/import to reviewed W3 amendment; W1 persists metadata tuple only | No live connection or import in W1 |
| D8 minor | Accepted: mandatory explicit scratch sink target; independent bounded draining/discard, 2s entry-lifetime deadline; startup latency tested separately | W1 sink correctness and W2a slow-start/large-input/lock/EOF tests; no claimed pre-runtime deadline |

These dispositions are proposed plan resolutions awaiting another actual Claude review. They do not certify runtime acceptance. No CLI bootstrap/auth audit was completed; no Windows source/build/account/settings operation or commit/push occurred.

## Delta 2 — D1–D8 corrections

Actual global launcher run by main Codex; requested opus/high, model_used claude-opus-5-5. Launcher0/validation passed; Claude0/is_error false/success; duration191032ms, stderr empty. Conditional verdict **fix-then-proceed**,3minor (E1–E3), no major/blocker findings. Claude explicitly says these are wording-level corrections that may be closed by lead self-check without a third full review; no unconditional Claude proceed is claimed.

- Exit: `C:/tmp/claude-reviews/20261005-123912-869343-windows-plan-delta2-49248-exit.txt`
- Result: `C:/tmp/claude-reviews/20261005-123912-869343-windows-plan-delta2-49248-result.json`
- Numbered input: `C:/tmp/claude-reviews/windows-plan-delta2-input.txt`
- Reviewed plan SHA256: `cb1541d14054d16d1c7b745cb4253f81d40f34895e944f761be2b34b21337f1c`
- Input SHA256: `4e20c5daf363866e2acc9eee54e64ff9d2b668d2a19126993f1a94d0abeb8de7`

Input included updated plan, delta1 report/dispositions, Mac startup evidence, Mac launch policy and original-command lines1–125. C1–C7 consistency was checked against plan dispositions, not another full reread of original report or installer. No tools, browsing or runtime tests by reviewer. Line references bind this input snapshot.

## Delta 2 verbatim result

# Windows MVP plan: second delta review (D1–D8)

**Citation labels:** `plan:N` is `docs/plans/2026-10-05-windows-mvp-plan.md` line N. `delta:N` is `docs/plans/2026-10-05-windows-mvp-claude-delta-review.md` line N. Swift and validation files are cited by base name.

**Scope of evidence:** I used only the supplied numbered input. I ran nothing, browsed nothing and read no other files.

**Missing context:**
- The `ClaudeOriginalCommand.swift` excerpt is truncated at line 125. I relied only on lines 27–121.
- The original C1–C7 report and the Mac inventory-prerequisite evidence were not supplied. I checked C1–C7 consistency only against the plan's own disposition table (plan:323–331).
- The lead's Microsoft process-creation-flags check (input line 3) is a lead-reported claim. I treat it as a claim, not as evidence I verified.

## Goal and simpler alternative

**Goal:** decide whether the revised plan defines W1 (offline core and tray) and W2a (fake-process, synthetic Bash/sink and scratch filesystem experiments) tightly enough to dispatch, with W2b (native Codex) mechanically refused until the C1 audit record exists.

**Smaller alternative, recommended but optional:** do not write `NativeCompatibility.cs` native mode in the initial dispatch. Write it in a separate dispatch after the C1 audit, because:
- The audit-record schema (plan:214) needs sanitized source callsites and coverage categories that only the audit itself will settle.
- Writing the validator first repeats the D7 pattern: inventing a format before its producer exists.
- Deferring the file removes the lockout question entirely. W1/W2a progress is unaffected.

If the lead keeps native mode in the initial dispatch, finding 2 applies.

## Flows traced

**Codex in W1:**
- Package metadata gives path, metadata version, file ID, size and SHA-256 (plan:100).
- The tuple is persisted in `LocalState` (plan:163). The dialog shows the path only, and the provider stays disconnected (plan:175).
- Nothing consumes the tuple for a fetch. Import is deferred to W3 (plan:100, :163–164, :328).
- This is consistent across design, manifest and disposition. D7 is resolved.

**Codex fake transport in W1/W2a:**
- Launch: `CreateProcessW` with suspended, detached, Unicode and extended-startupinfo flags, plus a handle allowlist and `STARTF_USESTDHANDLES` (plan:105).
- Job: limit-1 job with kill-on-close and no breakaway, assigned while the child is suspended (plan:117). Then resume.
- Run: concurrent drain (plan:106). The root exits, and references are released.
- Accounting: queried while the job handle is still held, expecting active 0, total 1, terminated 0. Then the job is closed.
- D2 is resolved. Input line 3 correctly leaves conhost and `AllocConsole` behavior to fake and runtime accounting rather than asserting it.

**Claude in W2a:**
- The harness starts a top-level Bash with the statusline command.
- The candidate runs `bash -c "$orig" < <(tee … >(sink >/dev/null 2>&1) 2>/dev/null)` (plan:132).
- The sink requires an explicit scratch directory and uses an independent bounded reader (plan:133).
- Writes use `ReplaceFileW` (flags 0) or no-overwrite `MoveFileExW`, with a recovery backup (plan:144). The tray reads with full sharing (plan:143).
- Cancellation: `TerminateProcess` on the top-level PID, observe survivors, then clean up the harness job (plan:134).

**Settings replacement in W2a scratch (W4 production):**
- Pin ancestors with read/write sharing and no delete sharing.
- Write the temp file, record its ID and close it.
- Recheck the destination, then call `ReplaceFileW` with an operation-unique same-volume private backup.
- Verify that the destination ID equals the temp ID, plus bytes, security and link checks (plan:140–142).
- D6 is resolved.

## Verified versus claimed

- **Verified:** nothing new. This round contains no runtime, build or source-audit evidence, and the plan says so (plan:5, :338; delta:225).
- **Mac-only verified evidence:** the guard and MCP-status results in `codex-startup-side-effects.md:16–23`. The plan does not present them as Windows proof (plan:118, :326).
- **Explicit experiments allowed to fail, correctly framed:**
  - tee/procsub EOF and backpressure behavior;
  - MSYS↔native handle inheritance;
  - Git Bash executable identity;
  - `ReplaceFileW` file-ID and sharing postconditions;
  - whether ancestor pinning permits child replacement;
  - the limit-1 job under the detached flags;
  - Codex Windows bootstrap paths and the credential key.

  These have fail-closed outcomes and are not missing contracts.

## Findings

### 1. Minor: two definitions in the D1 cancellation and EOF gates are still open, and the delta doc words the gate differently

**Evidence:**
- plan:134 uses both "candidate-added descendant" and "candidate-owned descendants".
- delta:216 says "all-owned-descendants 2s gate".
- plan:133 says "add at most 500ms over baseline", and lists a held-open-producer case.

**Consequence:**
- **(a) Classification.** "Candidate-owned" is not defined mechanically.
  - In the baseline, the shell running the original is the killed top-level process.
  - In the candidate, the nested `bash -c "$orig"` is a new process that `TerminateProcess` on the top level does not reach.
  - If a tester classifies that nested shell as "original", the gate can pass while the bridge leaves a running original shell that the baseline would not have.
  - "All-owned" in delta:216 could instead be read as including the original's own children, which is the bar the lead deliberately did not adopt.
- **(b) Non-terminating baseline.** In the held-open-producer case, an original that reads stdin to EOF never finishes in the baseline either. "500ms over baseline" is then undefined, and the case could be recorded as a pass or as a hang.

**Correction:**
- Define the classification in plan:134 and align delta:216 to the same wording:
  - *candidate-owned* = every process not present in the baseline tree for the same synthetic original, explicitly including the nested `bash -c` host, process-substitution subshells, tee and the sink;
  - the synthetic original's own children are compared against the baseline and recorded.
- Split the held-open-producer case into two variants:
  - an original that reads only the payload and exits, where the 500ms bound applies;
  - an original that reads to EOF, where timing is measured from the moment the producer closes stdin. Both runs need a harness case timeout that counts as failure.
- Design note, not a required correction: the stated redirect covers only the sink. The tee-hosting subshell can also hold the caller's stderr (or the write end feeding the original's stdin) until Claude closes stdin. Redirect stdout/stderr for every candidate-added process except the one running the original, for example `exec 2>/dev/null` at the start of each substitution. The existing caller-EOF measurement would detect this anyway.

### 2. Minor: the W2b lock has no safe way to test its accept path, which invites exactly the D4 crossing

**Evidence:**
- plan:214: the harness refuses without `--native` plus a matching record, and initial dispatch "writes native mode but must not execute it".
- plan:187, :315: backend-dev owns the "isolated compatibility probes".

**Consequence:**
- Refusal paths can be tested with bad records. But an implementer verifying that a *valid* record unlocks launch needs a record bound to some executable tuple.
- If the only available tuple is the real `codex.exe`, the natural test launches native Codex before C1.
- In addition, the "lead-reviewed" record is self-attested. An agent can author one that passes validation, so the lock is mechanical against accidents but not against a well-meaning implementer.

**Correction:** state in plan:214 and the dispatch brief that:
- the accept path is tested only with a fake console-child tuple under the test project and its scratch root;
- the real `codex.exe` tuple must never appear in an agent-created record;
- only the lead creates a record for the real executable, after the audit;
- validation additionally refuses any record whose path resolves under the npm Codex package directory unless the record file is in a lead-only location outside the repo and scratch roots.

If the alternative above is taken, this finding disappears.

### 3. Minor: the sink's outcome when its 2s deadline fires before stdin EOF is unspecified

**Evidence:**
- plan:133: drain "until EOF or its 2s lifetime deadline … then closes input".
- plan:136: the invalid-input marker.
- Mac reference: ingestion happens only after EOF; the post-exit deadline discards the capture (`ClaudeOriginalCommand.swift:54–63`, `:70`, `:119`).

**Consequence:** in the held-open-producer case, the sink may hold a complete JSON object without EOF. The W1 implementer must choose between three behaviors:
- parsing partial bytes, which risks ingesting truncated input or diverging from Mac;
- writing an invalid-input marker, which could show a persistent capture failure if real Claude holds stdin open;
- a silent no-op.

Each choice produces a different W2a result and different UI semantics.

**Correction:**
- State that no ingestion happens without EOF; deadline expiry abandons the capture.
- Pick the visible result: either mirror Mac (no write, previous snapshot retained) or write a distinct versioned "incomplete" marker category. Either way, preserve the previous receipt time as plan:136 requires.
- Add the case as a named W1 sink test.

## Examined with no material finding

- **D1, remainder:**
  - paired direct/candidate runs under identical invocation;
  - pinned tee and its version;
  - exit capture from `$?` or `PIPESTATUS` with errexit/pipefail disabled;
  - an exact top-level PID with `TerminateProcess`, and MSYS signals tested separately;
  - survivors observed before harness-job cleanup;
  - a Claude spawn/stdin/kill audit before live install;
  - the strict candidate-owned gate, with baseline leakage recorded but not used to relax it (plan:132–134).

  This addresses my prior concern that the gate could fail for causes outside the bridge, without weakening ownership.
- **D2:** the flag set, the absence of `CREATE_NO_WINDOW`/`CREATE_NEW_CONSOLE`, fake children launched with the exact flags, accounting queried before job close, and the note that failed associations increment the total (plan:105, :117).
  - plan:72 still says "launch … hidden". This is harmless because plan:105 is specific.
- **D3:** the snapshot writer primitive is named; there is no `File.Move` fallback; the open-reader replacement test is named; and backup cleanup can stay pending (plan:144).
- **D4:** discovery reads metadata only and never runs `--version` (plan:100). W2a/W2b are named separately (plan:213), and the record binds tuple, policy and methods (plan:214). The remaining gap is finding 2.
- **D5:** every profile variable points into scratch; `ProgramData`/`SystemDrive`/`PATHEXT` fallbacks are audited; and the plan states that known folders are not redirected by environment variables (plan:113).
- **D6:** ancestor sharing mode, the temp-ID/output-ID equality check, the same-volume private backup with a cross-volume block, and installer ownership of recovery files (plan:142).
- **D7:** W1 tuple-only scope is consistent across plan:100, :163, :164, :175 and :328.
- **D8:** a mandatory scratch target, an independent reader, the deadline measured from sink entry, and runtime startup measured separately with no pre-start deadline claimed (plan:133, :178). The remaining gap is finding 3.
- **C1–C7 consistency:**
  - C2's separate canary job capacity (plan:118) does not conflict with limit-1 production jobs.
  - C3's W3 reassertion (plan:119, :225) is consistent.
  - C4's disposition now matches D7 (plan:328).
  - C5's manifest has no `OriginalCommand.cs`, and the launcher is W4-only (plan:179).
  - C6 and C7 are unchanged and consistent.
  - The acceptance checklist (plan:263, :268) matches the gates.
- **Scope:** W3/W4, accounts, settings and publication remain unauthorized (plan:146, :317). Runtime gates are nowhere claimed as passed.

## Recommendation

All three findings are wording-level contract fixes. They can be written into the plan and the dispatch brief. In my view, they do not require a third full Claude review; a lead self-check of the edited lines is sufficient. After those edits, W1/W2a dispatch is reasonable, with W2b refused until the C1 audit record exists. Preferably, also defer writing native mode until that audit is complete.

VERDICT: fix-then-proceed

## Lead disposition and closure for delta 2

| ID | Correction / disposition | Self-check and remaining runtime gates |
|---|---|---|
| E1 minor | Accepted classification/timing gap; define strict cleanup as every candidate launcher descendant including nested original host and original children. This keeps the earlier all-descendants gate, stronger than merely excluding baseline children. Split payload-only exit versus read-to-EOF producer cases; measure latter from scheduled stdin close,5s paired-case timeout | Corrected plan clauses and D1 disposition reread together. Helpers/substitution host caller-output redirection explicit;500ms original exit/EOF bound and2s cleanup remain W2a experiments; baseline leaks cannot waive cleanup |
| E2 minor | Accept simpler alternative: defer NativeCompatibility.cs/native-mode authoring entirely until completed C1 audit, not merely execution. Initial dispatch cannot invoke real Codex/Claude even for version metadata | Manifest/task/dispatch boundary checked. Later audit-grounded harness schema/record by lead; validator accept-path uses fake tuple, never a fabricated real-Codex record |
| E3 minor | Accept explicit no-EOF behavior: mirror Mac discard, no parse/ingestion/write at deadline even for complete JSON; preserve snapshot/receipt/existing marker | Named W1 no-EOF/deadline/unchanged state test and later valid-capture recovery described. No impossible post-deadline marker write or fresh quota claim |

Lead conclusion after corrected-line self-check: W1/W2a plan is ready to propose for owner dispatch under the reviewer's conditional recommendation. Native W2b still requires complete C1 source audit before harness authoring/execution; bridge experiments may fail and block production independently. C1 audit is still incomplete. This is **lead closure of a conditional Claude review**, not a new Claude verdict and not runtime acceptance.

## Final document self-gate — 2026-10-05

Lead reread corrected plan/dispositions, dispatch ownership, STATUS/MEMORY/CHANGELOG against the approved review brief. Python checks passed for local Markdown links/table structure in all six edited documents, C1–C7 coverage and E1–E3 boundary clauses. Both original raw result strings remain verbatim in this report; both exit records confirm opus/high, claude-opus-5-5, success/is_error false/exit0/validation passed, with empty stderr. Normal Git whitespace check passed; no Windows directory or Mac source/project/config diff. Historical memory snapshots are labelled as superseded. No Windows build/app tests/native probe/settings/install/commit/push was run. Owner dispatch remains pending.

# Plan: Windows system-tray MVP

Date: 2026-10-05 (Asia/Bangkok). Status: **owner approved W1/W2a initial dispatch on 2026-10-05; implementation in progress**. Native W2b/production gates remain below.

Latest completed actual Claude delta review: **fix-then-proceed**, 3 minor wording-level contracts (E1–E3). Claude explicitly recommends lead self-check after these corrections, without another full review. Corrections and self-check are recorded in [delta report](2026-10-05-windows-mvp-claude-delta-review.md); lead considers W1/W2a ready to propose for owner dispatch, with W2b audit/harness execution and production bridge still gated. This is conditional review plus lead closure, not a fabricated Claude proceed verdict or Windows runtime proof. Original C1–C7 and D1–D8 dispositions remain recorded. See [original review](2026-10-05-windows-mvp-claude-review.md).
Repository baseline: `main`, `ca2eb2b`; Mac fix baseline `65b85cd`.
Requested deliverable: inspect PC/native CLI/WSL, propose Windows stack and scope, then wait before implementation. “mpv” in the request is interpreted as MVP, consistent with the Windows handoff.

## Summary

Add a native Windows edition under `windows/`, keeping `AIUsageBar/`, `AIUsageBarTests/`, `Config/` and `AIUsageBar.xcodeproj` unchanged. Recommend C# / .NET 10 / Windows Forms with a standard tray icon, Thai tooltip/menu and two providers: guarded native Codex app-server and a Claude Code latest statusline snapshot.

Start with Windows 11 x64 and native CLIs on this PC. WSL, Windows 10, ARM64, installer, signing, startup registration and binary publication are deferred. Installed tools establish feasibility only: Windows guard behavior, quota integration and bridge shell behavior are **not yet proven**.

## Evidence from this PC

Read `AGENTS.md`, `docs/PROJECT_BRIEF.md`, `docs/DECISIONS.md`, `docs/STATUS.md`, `docs/DEVELOPMENT.md`, `MEMORY.md` and `docs/HANDOFF-2026-10-05-111324-windows-start.md`.

| Check | Observed result | What it proves / limitation |
|---|---|---|
| Git | `main` tracks `origin/main`; HEAD `ca2eb2b`; clean before planning | Correct Windows handoff checkout; no Mac build artifacts |
| OS | Windows 11 Pro, 10.0.26200, x64; AMD Ryzen 5 9600X | Actual first validation target; no claim about other builds |
| .NET | SDK 10.0.401 selected; WindowsDesktop runtime 10.0.12 present | Existing WinForms build/runtime prerequisites; no install required now |
| Codex command | PATH resolves npm `codex.ps1`; `codex --version` = 0.160.0 | Shell shim exists; app must resolve native executable instead |
| Native Codex | `%APPDATA%/npm/node_modules/@openai/codex/node_modules/@openai/codex-win32-x64/vendor/x86_64-pc-windows-msvc/bin/codex.exe`; direct version = 0.160.0 | Native executable works for version/help; not an account or guard test |
| app-server help | Native help includes `--strict-config`, `--listen stdio://`, `-c` | Flags exist in this installed build; runtime policy still needs tests |
| Claude | `%USERPROFILE%/.local/bin/claude.exe`, version 2.1.289 | Native CLI installed; login/subscription/weekly payload not inspected |
| Git Bash | `C:/Program Files/Git/bin/bash.exe` exists, Git 2.55.0.windows.3 | Candidate shell; actual statusline invocation still needs isolated proof |
| PATH bash | `C:/Windows/system32/bash.exe` | WSL launcher; must not select this as Git Bash |
| WSL | WSL2; only `docker-desktop` listed, running/default | No user development distro to target; do not enter or modify Docker's distro |
| Context env | `CODEX_HOME`, `CLAUDE_CONFIG_DIR`, `CLAUDE_CODE_GIT_BASH_PATH` unset in this process | Does not exclude settings-level or other session overrides |

Inspection used OS/toolchain metadata, executable discovery, version/help and `wsl --status` / `wsl --list --verbose`. No auth/config payload was opened, no quota RPC was called and no CLI settings, bridge or system policy was changed. No broad environment dump is retained.

## Stack recommendation and alternatives

| Option | Tradeoff | Recommendation |
|---|---|---|
| C# / .NET 10 / WinForms | Built-in `NotifyIcon`, `ContextMenuStrip`, dialogs and message loop; Windows-only; standard desktop runtime required | Choose for small native tray MVP |
| WPF + WinForms tray | Better rich-window styling, but two UI frameworks for an icon/menu product | Defer unless a richer dashboard is requested |
| WinUI 3 | Adds Windows App SDK deployment/runtime considerations | Unnecessary for this scope |
| Electron / Tauri | Adds web UI/runtime or Rust/web toolchains and packages | Larger dependency surface than required |

.NET 10 is LTS; SDK/runtime already present here. Runtime/application dependencies: Microsoft .NET desktop framework and Win32 APIs only; no third-party NuGet package. Use a small dependency-free console test executable with test groups, named assertions, summary and nonzero failure exit, rather than claiming `dotnet test` discovers tests without an adapter. Reconsider a standard test framework only with explicit dependency rationale.

Portable **source-build / framework-dependent local output** first. Do not promise a single standalone EXE: tray and console bridge apphosts require their DLL, deps/runtimeconfig and referenced assemblies. No self-contained binary distribution in this proposal.

## Proposed scope and support matrix

| Context | MVP position | Evidence needed before supported claim |
|---|---|---|
| Windows 11 x64, this PC | Initial target | Release build, offline suite, native UI/lifecycle and real provider checks |
| Native Codex 0.160.0, default direct context | First candidate | Isolated two-child inventory/guard canaries and effective layer tests |
| Native Claude 2.1.289 + confirmed Git Bash | First candidate | Shell/byte passthrough proof, isolated installer round trips and actual statusline delivery |
| Native Claude with PowerShell statusline | Deferred | Separate quoting/encoding/cancellation proof; not silently treated as Bash |
| WSL Codex or Claude | Deferred | Explicit distro/user/context plus Linux child ownership, paths and auth separation |
| Named Codex profiles, managed/cloud/enterprise contexts | Unsupported unless separately validated | No guard weakening or undocumented fallback |
| Windows 10 / ARM64 / other CLI versions | Unvalidated | Separate compatibility evidence; CLI updates invalidate prior guard proof |

Owner may choose WSL in MVP, but that changes the plan before implementation; do not share native auth or snapshots with WSL implicitly.

In scope: weekly remaining/reset, distinct freshness/error/no-data, connect/disconnect Codex, refresh, Claude preview/install/restore, quit, per-user local state and personal source-build usage. Optional 5-hour details only when payload supplies them, following existing semantics.

Out of scope: API billing, browser-only accounts, token/cookie reader, model turns, HTTP listener/service, shared compiled Swift core, Mac migration, taskbar injection, always-visible long taskbar text, startup/registry/task scheduler changes, updater/analytics/telemetry, installer/signing/release/push.

## Tray UX

- One original application icon in the notification area; Windows may place it in overflow. User can pin it through Windows settings; app does not alter taskbar policy.
- Click/right-click opens a Thai menu showing Codex and Claude weekly remaining, reset time, source and freshness. `—` means unavailable; `0%` means genuinely zero. Expired reset must not appear current.
- Short tooltip within `NotifyIcon.Text` bounds; full details in menu. Claude explicitly says “จาก Claude Code ล่าสุด เวลา …” (receipt time, no account binding or server-fresh claim).
- Actions: “เชื่อม Codex CLI…”, “ตัดการเชื่อม Codex”, “รีเฟรช”, “ติดตั้ง Claude Code bridge…”, “คืน statusline เดิม / ตัด Claude”, “ออก”. Installer dialog shows exact settings destination and command change before applying.
- No persistent taskbar window or console for tray. Dedicated bridge is a console-subsystem executable because statusline stdin/stdout/stderr must work. Launch child console processes hidden without changing their streams.
- Menu remains usable during I/O. UI state returns to WinForms UI thread; stale completion after disconnect/shutdown is ignored using operation generation.
- Validate keyboard, Narrator, high contrast and 100/150/200% DPI on actual Windows. Check Explorer restart re-adds one icon. Record actual evidence, not inferred accessibility parity with Mac.

## Patterns to mirror

Line references are baseline references, not instructions to change Mac source.

| Category | Source | Pattern |
|---|---|---|
| Quota parser | `AIUsageBar/Providers/UsagePayloadParser.swift:11`, `:57`, `:94` | Select `codex` bucket; weekly duration 10080 min; finite numeric 0..100; missing is not zero; Claude `seven_day` |
| Freshness | `AIUsageBar/Models/UsageFreshness.swift:17` | Receipt versus provider observation; reject future timestamps; expire at reset |
| Throttle | `AIUsageBar/Models/RefreshPolicy.swift:5`, `:11` | 300s polling, 60s attempt cooldown; backward wall-clock jump starts cooldown |
| Coordinator | `AIUsageBar/App/RefreshCoordinator.swift:56`, `:87`, `:159` | Confirm path, serialize fetch, persist attempt before launch, cancel/invalidate on disconnect |
| Two-child sequence | `AIUsageBar/Providers/CodexQuotaProvider.swift:60` | Inventory child finishes/cleans up before second guarded quota child |
| Policy | `AIUsageBar/Providers/CodexLaunchPolicy.swift:23`, `:72`, `:85`, `:113` | Registry + effective config assertions; same inventory, disabled transports; no `mcp list` fallback |
| RPC | `AIUsageBar/Providers/CodexProcessTransport.swift:28`, `:112`, `:141` | Narrow methods, bounded strict envelopes, paginated registry and observation timing |
| Snapshot | `AIUsageBar/ClaudeBridge/ClaudeSnapshot.swift:15`, `:26`, `:43` | Quota-only schema 1; valid no-quota tombstone; serialized atomic capture |
| Settings | `AIUsageBar/ClaudeBridge/ClaudeSettingsCommand.swift:18`, `:58` | Byte-span edits preserve unrelated settings; duplicate keys/unsupported encoding rejected |
| Installer | `AIUsageBar/ClaudeBridge/ClaudeBridgeInstaller.swift:41`, `:71`, `:119` | Exact preview, backup, ownership hashes, restore checkpoint and metadata-last cleanup |
| Original command | `AIUsageBar/ClaudeBridge/ClaudeOriginalCommand.swift:27` | Preserve input/output/error/exit; Windows shell/process strategy must be newly proven |

No existing Windows code or C# conventions exist in this repo. WinForms/PInvoke structure below is a proposed Windows design, not an established internal pattern.

## Windows process, config and storage design

### Codex

1. Resolve npm/native layout by reading paths/package metadata, never invoking Node/shims for application fetch. W1 discovery does not execute Codex, even `--version`; version comes from validated package metadata and is labelled as metadata, not binary version proof. Show absolute `codex.exe` for user confirmation. Persist path, metadata version, file ID, size and SHA-256. The source-audit gate must bind this candidate before W2b can execute it and verify its reported version. A versioned compatibility record binds the verified tuple and probe-policy revision. A mismatch enters “Codex เปลี่ยนแล้ว — ต้องตรวจความเข้ากันได้ใหม่”; a confirmation click alone cannot create proof. The developer/operator reruns gated W2b on a changed executable, obtains an independently reviewed sanitized result, then W3 may import that exact record and reconfirm the path. Compatibility schema/import is deferred to a reviewed W3 amendment, not invented in W1; W1 stores the tuple and remains disconnected. Record import will be bounded/strict and is not signature or publisher provenance. Unknown versions/digests remain blocked, without warn-and-allow or automatic live probes. Rerunning the harness after CLI upgrades need not inherently require an application rebuild. Hold a non-write/non-delete-shared validated executable handle through launch where supported; test replacement races. Matching Mac version does not prove Windows behavior or binary/source provenance.
2. Default working directory is confirmed native user home, not the repository/project. Declare environment context; honor official CLI-managed auth, never read tokens. Unsupported context is an explicit error, not a request to reset user config.
3. Keep a narrow production provider: initialize/initialized, paginated `experimentalFeature/list`, `config/read`, `account/rateLimits/read` only. Child A inventories and verifies guards without quota, then fully closes. Child B overrides inventoried MCP entries with disabled transport placeholders, verifies registry/config and exact inventory, then reads quota. The isolated W2 harness has a separate test-only `mcpServerStatus/list` exception for synthetic stdio MCP positive controls; this method never enters the production provider or reads user MCP transports.
4. Match Mac invariants for hooks/plugins/code-mode host, notify, analytics and OTEL. `remote_control` is Removed/no-op in 0.160; not evidence of a functional guard. Do not invent Windows-specific features. Check effective resolution across isolated user/project/CLI layers before live use.
5. Replace `/usr/bin/false` with the bundled native bridge's **no-argument inert mode** (exits nonzero, no stdout, no writes, no shell). Encode its absolute Windows path as a TOML string. HTTP disabled placeholder remains `https://example.invalid/`. Prove placeholders are disabled and never invoked in guarded flow; do not copy original MCP transports/args/env/headers.
6. Windows transport uses `CreateProcessW` with `CREATE_SUSPENDED|DETACHED_PROCESS|CREATE_UNICODE_ENVIRONMENT|EXTENDED_STARTUPINFO_PRESENT`, explicit application path, audited Windows argument quoting, inherited stdio handle allowlist and `STARTF_USESTDHANDLES`, and a per-operation Job Object with `KILL_ON_JOB_CLOSE`. No `CREATE_NEW_CONSOLE` or `CREATE_NO_WINDOW` for these Codex jobs; detached console-subsystem children communicate only through explicit pipes. W1 fake console children must use these exact flags and prove stdio/job behavior; console-host assumptions do not waive accounting. Fully quoted command line must fit 32,767 UTF-16 code units including the terminating NUL; reject before every launch. Child B's inventory-dependent override length is checked after A has been reaped and before B exists; do not pretend that inventory was known before A. Boundary tests include escaped quotes/backslashes, long names and surrogate pairs. Assign job before resuming; failure terminates the owned suspended child, waits for exit and fails closed. Job handles do not leak to child; no breakaway flags; test nested-job behavior in this runner. Never terminate unrelated Codex/Claude/Docker processes.
7. Drain stdout/stderr concurrently; discard diagnostic content, retain safe error categories only. UTF-8 JSONL accepts LF and CRLF, partial lines and EOF; reject malformed/oversize frames, unexpected IDs/server requests including token-refresh request, and stale responses. Use the Mac bounds: config line 2 MiB, quota line 64 KiB, each output stream 16 MiB total, 4096 lines, registry 32 pages/2048 entries, inventory 256 entries. Maximum fetch deadline 30s across both children plus bounded cleanup.
8. On success/error/timeout/cancel/disconnect/quit: close stdin, bounded wait, terminate owned job if needed, wait for zero owned processes, dispose handles/pipes and drain tasks before returning. Cleanup failure remains an error. Test crash cleanup via a disposable test parent, never by killing a user's process.

### W2 launch isolation and process-proof contract (C1–C3)

W2 means isolated synthetic configuration and **no intentional account operations**, not an OS filesystem/network sandbox. Native CLI startup is forbidden until the version-specific source audit accounts for its bootstrap auth, home/known-folder, system/managed/cloud, proxy, telemetry and credential-store paths for the exact RPCs below. If source or necessary behavior cannot be verified, mark the native probe blocked and continue W1/fake-process/shell/filesystem tests; do not launch to find out using the owner's profile.

- Construct a new Unicode child environment rather than inheriting the session. The initial allowlist is trusted `SystemRoot`/`WINDIR`, a minimal absolute Windows system `PATH`, and `TEMP`, `TMP`, `CODEX_HOME`, `HOME`, `USERPROFILE`, `APPDATA`, `LOCALAPPDATA` each explicitly mapped to declared directories inside the scratch root. No inherited `OPENAI_*`, `ANTHROPIC_*`, `CODEX_*`, proxy, `RUST_LOG`, plugin, config, auth-helper or editor integration variables; only the explicit scratch `CODEX_HOME` exception is reintroduced. Audit exact-build fallback behavior when `ProgramData`, `SystemDrive` and `PATHEXT` are absent; do not add real-profile/system-config defaults without an audited amendment. Parent environment values/secret contents are never dumped. HOME/profile variables are conveniences, not proof that Windows known-folder APIs are redirected.
- Pin the expected canonical home/config paths and source callsites in the sanitized audit. Force the exact version's file-only CLI credential backend with an audited override (candidate `cli_auth_credentials_store="file"`; confirm the key/enum before use). The scratch home must contain no auth file and the harness must never copy/read owner credentials. Verify that no app-server bootstrap or selected RPC consults Credential Manager, real profile auth, cloud-auth startup or external credential helpers despite that choice. Unexpected real-profile/managed dependency blocks the native probe; do not modify global policy/config to bypass it.
- Set dead loopback HTTP/HTTPS/ALL proxies and clear NO_PROXY as a diagnostic tripwire, after auditing that this CLI uses them. This is **not** a network-denial mechanism. If the audited startup path can still initiate account/network activity outside the proxies, W2 does not authorize running it; record that prerequisite blocker or propose a separately approved isolation mechanism. No firewall changes, new Windows account, AppContainer or admin setup is silently added.
- Use synthetic user/project/CLI layers under the scratch roots, with trust only in the child overrides, synthetic stdio canaries and disabled `https://example.invalid/` transports. Assert returned layer metadata/inventory against the exact fixtures; unexpected paths/layers fail closed. W2 sends no `account/rateLimits/read`, account/auth/login/logout, thread, turn or model requests. Audit the startup path before even initialize; later assertions are not a substitute for startup authorization.
- For Codex inventory/quota jobs, additionally propose `JOB_OBJECT_LIMIT_ACTIVE_PROCESS` with limit1 together with kill-on-close/no-breakaway, assigned while suspended. Verify semantics with fake console children using the exact detached launch flags before any native launch. After root exit and releasing owned process/pipe references, query accounting while the parent still holds the job handle, then close it: `ActiveProcesses == 0`, `TotalProcesses == 1`, `TotalTerminatedProcesses == 0` on an accepted Codex run. Failed associations can increment total count, so do not mistake a blocked spawn for a clean run. Observe limit notifications/accounting where supported. This enforces the no-descendant boundary while the root is alive, not a network sandbox or full proof that no API spawn was attempted. Any normal CLI descendant requirement blocks this candidate rather than weakening the limit. Bridge experiments use a different harness policy because they intentionally need multiple processes; harness cleanup alone is not production launcher-death proof.
- Positive controls are distinct: a fake root attempting a child must exercise job-limit rejection/accounting; an isolated native MCP control uses test-only `mcpServerStatus/list`, synthetic transports only, and its own bounded job with enough capacity for the canary. It proves the detector works, not that production permits descendants. Prior Mac evidence records this RPC without model/thread use; Windows remains unproven. Hook/notify turn-triggered paths are covered by pinned-source disable/dispatch audit, not a falsely claimed runtime positive control. No model turns are introduced for coverage.
- W2 results are bound to executable tuple, policy, fixtures and test context. Production W3 uses approved CLI-managed auth/environment and confirmed user home, not the no-auth test environment. At authorized W3 entry, both children must re-assert registry/config/layer constraints, exact inventory and job accounting before quota is accepted. Unsupported authenticated layers fail closed. W2 evidence alone never certifies every authenticated/managed context.

### Polling and app state

- Codex every 300s, at least 60s between attempts including manual refresh/wake/relaunch. Persist UTC last attempt before launch; in-process deadlines use monotonic time. Backward clock and corrupted state must not permit a burst; fail closed or enforce a new cooldown.
- Single per-user tray instance using a user-scoped lock/mutex; second launch does not fetch. Disconnect invalidates in-flight result and removes selected CLI path, retaining cooldown to prevent reconnect bypass.
- Check Claude snapshot every 5s; reread does not change receipt time. Reset/freshness UI reevaluates against current clock. Codex freshness threshold is 300s, matching `AIUsageBar/UI/StatusItemController.swift:154` and `AIUsageBar/UI/UsageMenuView.swift:103`.
- `%LOCALAPPDATA%/AIUsageBar/Windows/` holds preferences, cooldown and bridge directory; no auth, raw config or raw statusline input. Persist selected paths, operational timestamps and nonsecret executable version/identity/digest needed to bind compatibility evidence. Native Windows snapshots remain separate from any future WSL context.

### Claude bridge and settings

- Candidate settings is native `%USERPROFILE%/.claude/settings.json` (this process has no `CLAUDE_CONFIG_DIR`); resolve effective supported config root at preview. Show actual path and shell. Do not alter managed/project settings or remove overrides to force data delivery.
- MVP shell is explicit Git for Windows Bash absolute path, confirmed in preview. Docs now also allow PowerShell; terminal being PowerShell does **not** establish statusline shell. If effective shell cannot be established or differs from tested Bash context, block install with actionable text; no shell migration or env/settings changes behind the scenes.
- **Simpler W2 bridge candidate (C5): Bash owns the original command, .NET is a capture-only sink.** The experiment pins `/usr/bin/tee` and its version. Compare a tee pipeline with a process-substitution input candidate such as `bash -c "$orig" < <(/usr/bin/tee --output-error=warn-nopipe >(sink >/dev/null 2>&1) 2>/dev/null)`. The latter aims to avoid making launcher exit depend on producer stdin EOF; neither shape is claimed correct. Redirect sink stdout/stderr inside the substitution, preventing inherited caller output handles from delaying EOF; test the native/MSYS handle boundary. Original stdout/stderr remain inherited. Disable shell errexit/pipefail locally; immediately save original exit via `$?` for input substitution or its `PIPESTATUS` element for a pipeline. Return original status, never sink/tee status, and never rerun original. No raw input files or command-text logs. Verify spaces/Thai/apostrophes, quoting, JSON escaping and MSYS conversion. The original stays one Bash string; no `eval` or reconstructed shell fragments. Before production, audit Claude's exact shell executable/flags/environment, stdin-close behavior and kill primitive; nested Bash is unsupported if it changes those semantics.
- The tee-split is a **synthetic experiment**, not approved production wrapper code. Every case runs original directly and through the candidate under identical harness invocation/flags. Require exact stdout/stderr bytes, same original exit and single execution; launcher exit and caller-side stdout/stderr EOF add at most 500ms over baseline. Cases include producer holding stdin open after payload and original exit; missing apphost/DLL/deps/runtimeconfig/runtime; slow runtime startup; sink startup/parse/overflow/lock failure; slow/hung sink; input exceeding pipe capacity; simultaneous stdout/stderr; original early exit and arbitrary exit codes. The W1–W2 sink requires an explicit scratch output directory with no profile/default target. A dedicated input reader is independent of parsing/locking/writes, captures at most 2 MiB and drains/discards excess until EOF or its 2s lifetime deadline (measured from sink entry), then closes input. Runtime startup lies outside that deadline and is measured by the harness. An overflow/error marker must not block draining or original passthrough. Candidate fails if backpressure/EOF/deadline gates cannot hold; no pre-start runtime deadline is falsely claimed. No `--probe`-then-exec readiness claim or fd3+ handoff to a native executable is used.
- Held-open producer has two named paired cases: (1) original reads the finite payload and exits while producer keeps stdin open, where the500ms exit/caller-EOF delta applies; (2) original reads until EOF, where producer closes stdin at a scheduled time and both timing deltas are measured from that close. Each paired run has a5s harness observation timeout; no terminating baseline means failure/inconclusive timing, never a pass. Redirect stdout/stderr of candidate-added helpers/substitution hosts away from caller pipes except the host actually executing original; verify EOF timing rather than infer MSYS inheritance.
- **Cancellation is a blocking experiment:** record exact spawned Bash path/file identity and process tree, including any redirector; do not infer `Git/bin/bash.exe` is the executing shell. Run baseline and candidate with identical original command. Kill exactly the synthetic top-level PID returned by harness `CreateProcessW`, using `TerminateProcess` (also test MSYS signals separately); do not terminate the harness job until observing survivors. Record baseline survivors and candidate survivors, caller EOF and sink/tee lifetime. No candidate-added descendant may survive beyond 2s after top-level death; retain the stricter supported-bridge gate that all candidate-owned descendants terminate within 2s, even if the direct-command baseline leaks. Baseline leakage is recorded separately and is not evidence of a bridge regression or justification to weaken that gate. Only then terminate the disposable harness job to clean up failed cases. Audit actual Claude spawn/flags/stdin/termination separately before live install; if inaccessible/unverified, live install remains blocked. Tee alone supplies no Windows job ownership guarantee. Failure records an unsupported bridge candidate; no invented fallback or settings mutation. A native supervisor/handshake alternative needs a concrete reviewed parent-death design and amendment. W1 and other W2 evidence progress independently.
- Cancellation ownership definition: the strict2s gate covers **every descendant of the candidate launcher in the synthetic harness**, including nested `bash -c` original host, original command and its own children, substitution hosts, tee and native sink. Mark candidate-added helper roles separately for baseline comparison, but no role exemption can satisfy cleanup; never classify the nested original host away. This deliberately keeps the earlier all-descendants acceptance bar. Baseline original-child leakage is recorded separately as baseline behavior, not a bridge regression; it still cannot waive candidate cleanup. Observe the entire known-owned tree before harness-job termination.
- Versioned installed bridge directory contains **all** required launcher/apphost/DLL/deps/runtimeconfig files; ownership manifest hashes every installed file. Do not copy only `.exe`. Capture is bounded at 2 MiB independently of original passthrough: overflow abandons capture and queues a safe marker while the reader continues bounded draining/discard; the tested tee must continue original stdin. Parsing/capture errors never replace original exit or truncate streams. Test an actual synthetic built capture sink/launcher, not only parser fakes. Production installer remains blocked until candidate/cancellation gates and design review pass.
- Atomically save quota-only schema 1 or valid no-data. Invalid input requires a distinct safe error marker/state (no payload), preserving previous snapshot and receipt timestamp. Marker schema is versioned, validated by reader and serialized with capture/restore; later valid input clears it. Include marker ownership/cleanup in manifest. UI displays capture failure separately from valid no-data and labels any retained quota as previous data. Mandatory tests cover invalid input, overflow, old/future receipt time and recovery; invalid input cannot make old quota fresh.
- Sink deadline before stdin EOF: never parse or ingest the partial capture, even if its bytes already form complete JSON. Mirror the Mac no-EOF discard: abandon capture and perform **no write**, retaining previous snapshot, receipt and any existing marker unchanged. Named W1 test covers complete JSON without EOF, deadline, no quota/marker ingestion and old receipt preservation; a later valid EOF-terminated capture publishes normally and clears any pre-existing capture-error marker. No post-deadline filesystem operation may extend the2s lifetime or delay original execution.
- Settings mutation uses exact preview bytes and compare-before-write, backed up before mutation. Byte-token surgery keeps whitespace/CRLF/padding/unrelated keys. Accept UTF-8 without BOM initially; reject BOM/UTF-16 and duplicate keys without rewriting or transcoding user settings.
- Private app/backup/metadata/snapshot files use protected DACL for current user and SYSTEM, not POSIX chmod. Refuse unsafe ownership/other-user write access, reparse points/junction traversal and hard-linked files; use handle-based identity checks. Preserve existing settings security descriptor rather than broadly changing `.claude` permissions.
- Same-directory unique temp, exclusive create, flush, atomic replacement using appropriate Windows APIs, existing ACL preservation and bounded sharing-violation handling. Cross-process installer/capture lock, recheck file identity/content before mutation. Our lock cannot force unrelated editors to cooperate: test concurrent replacement, report conflicts, do not claim a universal CAS or power-loss guarantee from `File.Move`/`ReplaceFile` alone.
- **Concrete settings replacement candidate (C6):** read/validate destination with `GENERIC_READ` and `FILE_SHARE_READ` while hashing bytes, file identity, security descriptor and link/reparse facts. Pin validated ancestor directory handles without delete sharing; hold the private app lock with no sharing. Create a same-volume unique temp with `CREATE_NEW`, `GENERIC_READ|GENERIC_WRITE|DELETE`, share0, under the pinned parent; write and flush, then close that temp handle before `ReplaceFileW` must open it. Recheck destination bytes/identity immediately before releasing its validation handle. For an existing settings file use `ReplaceFileW` with flags0 and an operation-unique recovery backup path; do not ignore ACL/merge errors. The documented API preserves the replaced file's DACL/attributes; verify those postconditions, do not assert it preserves file ID. On absent destination use no-overwrite same-volume rename (`MoveFileExW` flags0) of a temp created with the intended DACL; newly appearing destination is a conflict. Reopen and verify bytes/security/reparse/link postconditions after either path.
- `ReplaceFileW` has partial-failure outcomes, not a general CAS: any ambiguous result keeps journal, original backup and extant temp/recovery files, reconciles their identities/content and reports conflict; no blind retry or cleanup. Sharing-only retries total at most2s, with a full content/identity recheck each time. Path replacement requires closing destination/temp validation handles, leaving an unavoidable external-editor race; if an editor replaced the destination in that window, the operation recovery backup must retain the actual displaced bytes. Detection is not a guarantee that no transient edit was overwritten. Declare that limitation in exact preview; an unsafe/unknown postcondition blocks live installer and retains evidence. W2 must prove primitive behavior with competing writer handles/rename races; W4 adds full installer fault matrix. No user ACL removal and no claim of power-loss durability/universal CAS.
- C6 handle/backup details: pinned ancestor directories use `FILE_SHARE_READ|FILE_SHARE_WRITE` without delete sharing and `FILE_FLAG_BACKUP_SEMANTICS`; scratch tests must establish this prevents ancestor rename without blocking child-file replacement. Record temp file ID while its exclusive handle is open, then require resulting destination ID to equal that temp ID after replace/rename, alongside bytes/security/link checks. Recovery backups live in a protected current-user+SYSTEM operation directory under our private app root on the same volume, with an operation-unique path recorded in the journal; if that volume differs, block settings replacement rather than invent a cross-volume move. Installer owns retained recovery files and removes them only after explicit successful reconciliation/cleanup; raw settings backups never enter logs, repo or snapshots.
- **Snapshot reader/writer contract differs from settings:** immutable private snapshot/error files use owner+SYSTEM DACL on temp, flush, close and same-volume replacement. The tray opens with `GENERIC_READ` and `FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE`, reads a bounded handle snapshot and validates schema/time, without holding the installer destination lock. Our writers never modify a published file in place. Check length/identity around reads and retry once for concurrent noncooperating changes; then report safe read error. Test poll-vs-replace and ACL preservation. This reader must not take settings' restrictive sharing mode and block normal snapshot replacement.
- Private snapshot/error writer primitive: under the private writer lock, for an existing destination use `ReplaceFileW` flags0 with an operation-unique private recovery backup; for absence use `MoveFileExW` flags0 (no overwrite). No `File.Move(overwrite)` fallback. Close temp before replacement, bind temp ID and validate output; preserve recovery evidence on ambiguity. Backup cleanup can remain pending while old reader handles are open and must not convert a successful publish into fresh quota from invalid input. Named W1 test: hold a full-sharing reader handle open across replace; replacement succeeds and old handle reads complete old bytes while a new handle reads complete new bytes. Test ACL, absent-destination conflict and bounded recovery/cleanup separately. These are API candidates, not runtime proof.
- Restore only owned command/object while retaining unrelated later edits. Originally absent settings can be removed only when still wholly our created content. Journal/checkpoint binds restored settings digest or absence and backup digest; retry works after interruption at every cleanup boundary. Validate remaining file hashes first; remove ownership manifest last. For apphost/DLL still in use after settings restore, retry owned artifact deletion for at most2s then return “คืน statusline แล้ว — รอเก็บไฟล์ที่ยังใช้งานอยู่”; keep manifest/checkpoint/remaining ownership evidence, no forced termination of the user's CLI. Next explicit restore retries cleanup only if restored-settings digest and surviving artifacts still match. Test locked apphost/DLL, concurrent capture and retry; successful settings restoration is distinct from complete cleanup. Never recursively remove a user settings directory.
- Installing, restoring or local account checks require explicit approved execution scope and the exact preview where applicable. This planning turn authorizes none of these writes/calls.

## Files to change after approval

All listed Windows files are CREATE; Mac code/project files have no changes. Names are the bounded initial manifest; if implementation needs another source/dependency, update plan with reason before adding it. Phase responsibilities below are binding: W1–W2 may create offline core/tray/inert sink and isolated test candidates only. W3 live provider activation and W4 production settings/installer/launcher are deferred. Files named for later phases are not permission to implement or activate them in W1–W2.

| File | Action | Purpose |
|---|---|---|
| `windows/AGENTS.md` | CREATE | Windows scope, build/checks, native context and self-gates |
| `windows/README.md` | CREATE | Prerequisites, local build/run/remove, context and side effects |
| `windows/.gitignore` | CREATE | bin/obj/publish/test scratch output; no account artifacts |
| `windows/global.json` | CREATE | Reviewed .NET 10 SDK baseline/roll-forward policy |
| `windows/AIUsageBar.Windows.slnx` | CREATE | Four project build entry |
| `windows/Directory.Build.props` | CREATE | Nullable, warnings, target/version settings; no custom download/build hooks |
| `windows/NuGet.Config` | CREATE | Clear external package sources for framework-only offline restore; no packages/downloads. Dispatch-time build manifest clarification, not a dependency change |
| `windows/src/AIUsageBar.Core/AIUsageBar.Core.csproj` | CREATE | Windows core, no package references |
| `windows/src/AIUsageBar.Core/Usage.cs` | CREATE | Models, parser, freshness and safe provider states |
| `windows/src/AIUsageBar.Core/RefreshCoordinator.cs` | CREATE | Serialization, cooldown, disconnect/quit generations |
| `windows/src/AIUsageBar.Core/LocalState.cs` | CREATE | W1 offline selected tuple/cooldown persistence; compatibility schema/import deferred to reviewed W3 amendment; no credentials |
| `windows/src/AIUsageBar.Core/CodexDiscovery.cs` | CREATE | W1 metadata-only native discovery/tuple validation, no executable invocation; W2b gated developer/operator revalidation; W3 proof import deferred |
| `windows/src/AIUsageBar.Core/CodexPolicy.cs` | CREATE | Override encoding, registry/inventory invariants |
| `windows/src/AIUsageBar.Core/CodexProvider.cs` | CREATE | W1 fake two-child orchestration/RPC tests; W2 isolated config/registry only; quota activation requires W3 approval |
| `windows/src/AIUsageBar.Core/WindowsProcess.cs` | CREATE | Safe handles, suspended launch/job/pipes/deadline/cleanup |
| `windows/src/AIUsageBar.Core/PrivateFiles.cs` | CREATE | W1 private snapshot/reader primitives; W2 scratch Win32 replacement/ACL/race experiments; W4 production settings use only after gates |
| `windows/src/AIUsageBar.Core/ClaudeSnapshot.cs` | CREATE | Schema, bounded ingest and timestamp-safe serialized writes |
| `windows/src/AIUsageBar.Core/ClaudeSettings.cs` | CREATE W4 | Production byte surgery after candidate review; W2 parser/fixture experiments stay in test harness |
| `windows/src/AIUsageBar.Core/ClaudeInstaller.cs` | CREATE W4 | Preview/apply/restore, complete artifact manifest/checkpoint and cleanup-pending retry; no production installer in W1–W2 |
| `windows/src/AIUsageBar.Tray/AIUsageBar.Tray.csproj` | CREATE | WinExe/WinForms, local icon resource |
| `windows/src/AIUsageBar.Tray/Program.cs` | CREATE | STA entry, single instance/message loop |
| `windows/src/AIUsageBar.Tray/TrayContext.cs` | CREATE | Thai menu/tooltip, ApplicationContext/coordinator, lifecycle/Explorer recreation; concrete dispatch implementation name |
| `windows/src/AIUsageBar.Tray/ConnectionDialog.cs` | CREATE W3/W4 if needed | W1 metadata picker lives in TrayContext; no empty dialog stub. Future proof import/connection/exact bridge preview remain gated |
| `windows/src/AIUsageBar.Tray/Assets/AIUsageBar.ico` | CREATE | Own icon, multiple DPI sizes |
| `windows/src/AIUsageBar.Tray/Assets/generate_icon.py` | CREATE | Standard-library source for our original multi-size ICO; reproducible asset generation, no fetched asset/package/build hook |
| `windows/src/AIUsageBar.Bridge/AIUsageBar.Bridge.csproj` | CREATE | Console bridge/helper/inert mode, no extra runtime packages |
| `windows/src/AIUsageBar.Bridge/Program.cs` | CREATE | W1 inert mode and test-only capture requiring explicit scratch target, independent bounded drain/lifetime; no original execution or native handoff |
| `windows/src/AIUsageBar.Bridge/claude-launcher.sh` | CREATE W4 | Production launcher only after W2 tee-split/cancellation gates and independent design review; no native-wrapper fallback |
| `windows/tests/AIUsageBar.Tests/AIUsageBar.Tests.csproj` | CREATE | Dependency-free executable referencing core |
| `windows/tests/AIUsageBar.Tests/Program.cs` | CREATE | Named case runner; fake RPC/child/canary subprocess modes |
| `windows/tests/AIUsageBar.Tests/UsageTests.cs` | CREATE | Synthetic parser/freshness/quota-only schema assertions |
| `windows/tests/AIUsageBar.Tests/RefreshTests.cs` | CREATE | Cooldown/relaunch/disconnect/clock/late-result assertions |
| `windows/tests/AIUsageBar.Tests/CodexTests.cs` | CREATE | Policy/RPC/inventory/timeout/cancel/job/stream cases |
| `windows/tests/AIUsageBar.Tests/ClaudeTests.cs` | CREATE | W1 schema/sink tests; W2 scratch filesystem/ACL/race primitives; W4 full install/restore interruption and locked-artifact matrix |
| `windows/tests/AIUsageBar.Tests/claude-tee-experiment.sh` | CREATE W2 | Synthetic tee-split launcher only; streams/exit/backpressure/top-level death, never installed into user settings |
| `windows/tests/AIUsageBar.Tests/NativeCompatibility.cs` | CREATE W2b after C1 audit | Deferred from initial dispatch. Audit-grounded native harness only after lead verifies C1 and records contract; explicit native flag/tuple-bound audit, no auth/quota |
| `docs/validation/2026-10-05-windows-environment.md` | CREATE | Sanitized inspection evidence from this planning turn |
| `docs/validation/windows-mvp-validation.md` | CREATE later | Actual build/test/probe/UI evidence and limitations |
| `README.md` | UPDATE after implementation | Separate Mac and Windows instructions without replacing Mac |
| `docs/DECISIONS.md` | UPDATE | Proposed stack/context now; approved values only after owner decision |
| `docs/STATUS.md`, `MEMORY.md`, `CHANGELOG.md` | UPDATE | Planning status now, actual progress/results later |

No shared runtime core/fixtures are created in phase one. Tests reuse Mac semantics through new synthetic cases; source license remains MIT. No CI changes in this plan.

## Tasks and gates

### W0 — Plan and owner choices (this turn)

- Finish inspection and this proposal; independent Codex review of plan; record every finding disposition.
- Owner chooses stack, native/WSL boundary and icon+menu UX before any Windows source.
- Stack/native/tray/W1–W2 approval is recorded in DECISIONS24. Complete C1–C7 plan dispositions and actual Claude delta re-review before proposing the second dispatch gate; source stays untouched until owner dispatch approval. Approval of this document/review work does not authorize live accounts, settings, bridge install or publication.

### W1 — Offline tray and core

- Create four-project layout, basic Thai tray with real unavailable states and safe models/coordinator. No live provider startup by default, no quota numbers from fixtures in normal app.
- Implement fake child tests and job/pipe transport; inert bridge mode available from local bundled bridge output.
- Run from `C:/Python/AIUsage/windows` so `windows/global.json` controls SDK selection: `dotnet build AIUsageBar.Windows.slnx -c Release` and `dotnet run --project tests/AIUsageBar.Tests -c Release -- --offline`; nonzero failures, no skipped mandatory cases. Build/test commands are **planned**, not run successfully yet.
- Manual offline tray/menu/quit and single-instance checks on actual PC; renderer tests do not prove Narrator/Explorer behavior.

### W2 — Native compatibility before accounts

- W2a is fake-process accounting, synthetic Git Bash/sink and scratch filesystem experiments: no Codex/Claude CLI invocation. W2b is native Codex compatibility only after C1 prerequisites. Dispatch must name these separately; creation of a harness is not permission to execute native mode.
- Initial dispatch does not write native mode or `NativeCompatibility.cs` and never launches real Codex/Claude, including version commands. Lead performs the read-only C1 source audit first. Only after complete coverage, lead records the audit schema/contract and may release W2b harness authoring within approved isolated scope; avoid inventing a validator before its producer/audit exists. That future harness must refuse launch without explicit `--native` plus a lead-created audit record binding schema version, path/file ID/size/SHA-256, package tag/source commit, policy, allowed methods, scratch/environment and complete startup/auth/known-folder/network coverage with source callsites. Unknown schema, incomplete coverage or tuple/policy mismatch blocks before CreateProcessW. Accept-path validator tests use fake console-child tuples under scratch, never the real Codex tuple; agents may not create real-executable audit records. Audit records are local checkpoints, not binary provenance or an OS sandbox.
- Audit exact CLI Windows startup/config source paths from official versioned source; do not treat current online docs as pinned 0.160 implementation proof.
- Only after the source-audit isolation prerequisites above: opt-in native harness uses its explicit clean environment, file-only unauthenticated scratch config and declared fixture paths. Test config/registry only; never quota/account/auth/model/thread/login/logout. A blocked bootstrap audit is a native W2 blocker, not permission to run anyway.
- Inventory/layer assertions, registry guards, disabled child B inventory and no-spawn accounting are distinct gates. Synthetic MCP positive control uses the explicitly test-only MCP-status RPC in a separate canary job. Hook/notify paths use source event/disable audit; no runtime positive-control claim for untriggered events. Native Windows proof remains separate from prior Mac MCP evidence.
- Negative controls: unknown/missing required guard/feature, unexpected registry stage, pagination loops, strict-config incompatibility, inventory drift, spawn attempt, invalid transport kind. Confirm bounded cleanup/no owned children on all outcomes.
- If guard or isolation proof fails, stop real Codex integration and keep tray usable with an explicit unsupported-config state. No mcp-list fallback or relaxed config.
- Separately test confirmed Git Bash byte/exit passthrough and quoting using synthetic commands/settings. A failed/missing capture sink must preserve original execution once; no native readiness handshake or rerun fallback is claimed. Actual Claude statusline shell still requires authorized live delivery later.
- Bridge spike prototypes only the tee-split capture candidate described above. Prove raw streams, original single execution/exit, capture deployment failures, bounded backpressure/overflow, early exit and top-level Bash death cleanup. If unresolved, report the bridge-specific blocker and leave all settings unchanged. Production wrapper/supervisor/installer source is deferred until candidate selection and independent review; W1 and other W2 evidence still progress.

### W3 — Native Codex personal integration

- Only after W2 evidence/review and approved account execution scope: confirm the exact executable compatibility record and enter an authenticated guard-proof run. Registry/config/layer/inventory/job assertions must pass again in both children before quota is trusted; any unsupported live context fails closed. Then perform quota-only real fetch, retaining safe field-presence/semantic checks. W2's no-auth context is not reused as production auth.
- Compare every field used by UI with the actual response shape, parser result and rendered states; never attach raw account payload to docs/fixtures. Verify failure/reset/stale and attempt persistence across relaunch.
- Official CLI may access network/refresh its own auth/write its state; app does not read credentials or configure sandbox accounts/firewall/registry. Do not claim quota-only launch implies zero official CLI side effects.

### W4 — Claude bridge

- Implement preview/backup/apply/restore with native filesystem semantics and synthetic temporary settings first. Test existing command, no statusline, absent file, unrelated edits, encoding rejection, lock/ACL/reparse/hardlinks, failed atomic replace and each interrupted cleanup boundary.
- Also interrupt install after each launcher/companion/backup/metadata write and settings replacement; prove safe retry/rollback with partial deployment. Test editor-held handles, destination replacement between compare/replace, ancestor junction/rename attempts and sharing timeout; verify external edits are preserved or flagged with displaced bytes retained, and ownership evidence survives failure. Include in-use apphost/DLL cleanup-pending and capture-during-restore cases.
- Independent review of settings/ownership/shell/process paths before live install.
- After explicit execution scope and exact preview approval: install personal bridge, validate original statusline output and real snapshot delivery, restore exact original bytes when unchanged, reinstall and confirm new payload. Do not generate a model turn merely to get quota; use an actual user session's naturally supplied data.
- If actual shell differs from proven shell, report unsupported context and return to plan decision, preserving existing settings.

### W5 — Windows closure

- Actual tray/refresh/quit/relaunch, Explorer restart (only with permission to restart process), sleep/wake, disconnect during fetch, timeout, offline and child cleanup. Never restart Explorer or kill user CLI processes merely for a test without authorization.
- Measure a documented 60s idle interval after fetch: CPU, private memory, owned child count (must be zero), handle stability across repeated refresh. Target under 1% average CPU on this PC; record actual memory and explain observed growth rather than invent a Mac-equivalent ceiling.
- Build/offline suite and independent review; reread changed files/diff against brief. Document unsupported/unrun scenarios. Mac regression on a Mac/approved CI remains pending on PC; do not claim Xcode tests ran here.
- Update docs with actual results. Commit/push/release/install/startup remain scoped to separate user instruction.

## Risks

| Risk | Likelihood | Mitigation / stop condition |
|---|---|---|
| Matching CLI version hides platform/config differences | Medium | W2 gates with Windows source/isolated canaries; fail closed |
| Running child escapes before job assignment | Medium | Suspended launch, assign before resume, no breakaway, handle tests |
| Wrong Bash or quoting changes existing statusline | High if inferred from PATH | Explicit Git Bash, byte tests, exact preview, live shell proof before supported claim |
| Bridge exe copied without .NET companion files | High with naive port | Versioned complete output manifest and hashes; test missing artifact recovery |
| Antivirus/editor holds files or replaces settings concurrently | Medium | Atomic APIs, bounded sharing handling, identity/content checks, conflict and retained recovery evidence |
| Windows ACL/reparse behavior treated as chmod/rename | High with mechanical Swift port | Native filesystem implementation and adversarial synthetic tests |
| Quota absent under current plan/session | Possible | Show no-data; do not prompt login based only on missing weekly quota |
| Tray overflow obscures usage versus Mac text | Expected | Explain icon/menu UX and optional user pinning before approval |
| WSL scope expands authentication/process complexity | High if added silently | Native-only initial proposal; explicit separate distro/context plan |

## Acceptance checklist

- [x] Owner approves C#/.NET10/WinForms, native Windows11 x64, Thai icon/menu and W1–W2 offline/isolated scope (“go”, 2026-10-05); W3/W4 live scope is not approved.
- [x] Windows source/build output isolated under `windows/`; Mac source/project/config unchanged. Lead validation logs use gitignored build/WindowsValidation.
- [x] W1 Build and80mandatory offline tests pass with recorded SDK/OS; zero NuGet package entries. SDK first-use development certificate side effect disclosed separately; future commands suppress it.
- [ ] Windows Codex isolation prerequisites and fixture config/registry/no-spawn/controls validated in W2; runtime assertions re-established under approved auth at W3 entry before quota is trusted.
- [ ] No thread/model/login/logout, direct credential reads, raw payload persistence or config weakening.
- [ ] Quota parser/UI retains missing/zero/reset/freshness distinctions and real field checks.
- [ ] 300s polling and persisted 60s throttle survive relaunch/disconnect/clock change.
- [ ] Owned child/job/pipe cleanup proven across completion/cancel/timeout/quit/crash.
- [ ] Claude tee-split candidate proves original streams/exit/no rerun, bounded sink backpressure and top-level cancellation; production bridge remains blocked until these gates pass; full artifact set recoverable.
- [ ] Invalid-input marker remains distinct from no-data, retains old receipt time, clears after valid input and participates in ownership/restore.
- [ ] Exact install/restore/reinstall and interruptions at every install/restore boundary tested with fixtures before authorized actual settings writes.
- [ ] Native tray/UI/DPI/accessibility/lifecycle and resource evidence recorded, with untested contexts explicit.
- [ ] No installer/autostart/registry/updater/telemetry/publish scope added.

## Official references inspected on 2026-10-05

- [OpenAI Windows guidance](https://developers.openai.com/codex/windows/) — native Windows route exists; does not prove quota startup guards.
- [OpenAI app-server rate limits](https://learn.chatgpt.com/docs/app-server#6-rate-limits-chatgpt) — current quota fields; installed version still requires fixture/runtime verification.
- [Claude statusline](https://code.claude.com/docs/en/statusline#windows-configuration) — Git Bash when installed, PowerShell otherwise, forward-slash path guidance; `seven_day` quota fields can be absent.
- [Claude native setup](https://code.claude.com/docs/en/setup) — native Windows supported; Git Bash is optional in current docs, so do not assume mandatory everywhere.
- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) — .NET 10 LTS.
- [WinForms NotifyIcon](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon?view=windowsdesktop-10.0) — framework tray/menu API.
- [Windows Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects) and [kill-on-close limits](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_limit_information) — child ownership/lifetime mechanism.
- [CreateProcessW](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw) and [ReplaceFileW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew) — launch and replacement primitives, not proof that our implementation is correct.
- [Job accounting](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_accounting_information) and [CreateFileW sharing](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew) — API contracts rechecked during reconciliation; fake/native accounting and replacement/sharing behavior remain W1/W2 tests. Windows-specific Codex bootstrap/auth source audit remains incomplete; this document does not certify the candidate credential key or permit a native probe before that gate.
- [Process creation flags](https://learn.microsoft.com/en-us/windows/win32/procthread/process-creation-flags) — detached/suspended/Unicode/extended startup flags; exact console-subsystem fake tests precede native use. Reviewer conhost assumptions are not accepted as verified platform behavior.

## Independent review and disposition

Native Codex CLI independent context review completed with exit 0 on 2026-10-05. Original verdict: **fix-then-proceed**, five blockers and two suggestions. Reviewer read only the draft plan, Windows handoff, Mac launch policy and Mac installer supplied inline; no reviewer tool calls or app changes. CLI emitted configured hook status messages; do not interpret this model review as an isolated/no-hook quota startup probe.

Local raw evidence: `C:/tmp/codex-reviews/raw-20261005-112659.md`; reviewer summary: `C:/tmp/codex-reviews/summary-20261005-112659.md`. These private paths are review evidence, not repo dependencies.

| Finding | Disposition in revised plan | Implementation proof still required |
|---|---|---|
| M1: fully quoted Windows command line can overflow | Accepted: 32,767 UTF-16 units including NUL; prelaunch check for each child, B after A's inventory | Boundary/escaping/surrogate tests |
| M2: same-path exe replacement invalidates guards | Accepted: version/file identity/SHA-256 binding and revalidation; validated handle through launch | Upgrade/replacement race tests |
| M3: partial apphost/runtime deployment breaks existence-only fallback | Historical resolution was native readiness/handoff; superseded by C5's synthetic tee-split capture-only candidate. Complete deployment is still required; missing sink/runtime must not rerun or change original execution | W2 actual built sink/tee deployment-failure, streams, backpressure and cancellation gates; W4 installer deferred |
| M4: bounded capture must preserve unbounded passthrough | Accepted: abandon 2 MiB capture only; keep original streams/exit, concurrent draining | Actual bridge overflow/deadlock/early-exit/cancel tests |
| M5: invalid input state was optional | Accepted: mandatory safe versioned error marker, old receipt retained, valid input clears, owned cleanup | Schema/reader/UI/recovery tests |
| m1: partial install recovery missing | Accepted: faults after every artifact/settings write, plus partial companion deployment | Install and restore fault matrix |
| m2: concrete sharing/identity contract missing | Accepted: documented sharing modes, pinned parents, bounded conflict, postcondition/journal and explicit remaining race | Windows filesystem spike with competing editor handles |

Human verification correction: review said reject excessive inventory “before either child”; inventory is discovered by child A. Revised plan checks each command line before its own launch and inventory-generated arguments before child B only. No live guard bypass follows from this sequencing.

Simpler delivery suggestion accepted: initial implementation approval should cover **W1–W2 offline skeleton and isolated compatibility spikes** first. W3 real account fetch and W4 personal bridge writes follow proven prerequisites and approved execution scope. Owner can choose this phased scope without approving live account/settings activity now.

Independent native Codex CLI delta re-review completed (exit 0): **proceed for PLAN**, no new major/minor findings, M1–M5 and m1–m2 dispositions accepted. Local evidence: `C:/tmp/codex-reviews/raw-20261005-113134.md` and `C:/tmp/codex-reviews/summary-20261005-113134.md`. Scope was only the revised Windows process/config/storage design and disposition sections; no reviewer tool calls. Launcher/handshake, executable replacement, actual stream preservation and filesystem sharing remain proof gates in W2/W4, not passed tests. Subsequent self-check clarified that build commands run from `windows/` for SDK pinning and that nonsecret executable proof metadata can be persisted; these two clarifications were not part of delta review scope.

Final planning self-gate: reread plan and companion documents against request; local Markdown links/table structure and Git whitespace check passed (normal Windows CRLF handling); no diff in Mac source/tests/Config/Xcode. No Windows source/build/tests, real account quota calls or bridge/settings changes occurred. Owner approval remains pending.

## Owner approval and proposed dispatch — 2026-10-05

Owner replied “go” to the session summary recommending C#/.NET10/WinForms, native Windows11 x64, Thai tray icon/menu, and W1–W2 offline/isolated work. This approves that scope and reuses the existing independent plan review/dispositions. The preceding planning self-gate is historical; owner approval is now received.

Proposed initial dispatch is **W1/W2a only**, with these disjoint write boundaries:

| Owner | Files / responsibility | Scope restriction |
|---|---|---|
| backend-dev | `windows/src/AIUsageBar.Core/` and `windows/src/AIUsageBar.Bridge/` project files/inert sink | Fake-only provider/process development, private snapshot primitives, explicit scratch sink; no W4 settings/installer/production launcher files or native harness |
| worker | `windows/src/AIUsageBar.Tray/`, solution/global.json/Directory.Build.props/windows .gitignore/README | Offline Thai tray/icon/build wiring, path display only; no CLI/account/settings install activation |
| qa-engineer | `windows/tests/AIUsageBar.Tests/` project/runner/fake-child cases and synthetic tee/filesystem experiments | Every candidate-launcher descendant under strict2s gate; paired held-open EOF cases; native harness file/mode deferred until C1 audit |
| lead | Windows AGENTS, repo docs, integration/review/self-gate and read-only C1 source audit | Only lead may record real-executable audit/release W2b after prerequisites; checks every artifact before downstream use |

All agents are not alone in the codebase: preserve others' edits, coordinate interface changes before editing another owner's files, and leave Mac files untouched. Initial dispatch never executes real Codex/Claude, even version commands; no agent fabricates a native audit record. Fake console children use exact detached production flags; sink target is required scratch; no-EOF deadline discards without writes. Independent review follows implementation of process/config/shell/filesystem paths. W2b harness authoring/execution, W3 compatibility import/auth activation and W4 production bridge remain outside initial dispatch, with gates above.

Owner confirmed the second dispatch gate with “go” after the corrected-plan/team summary on 2026-10-05; see DECISIONS25. W1/W2a implementation is now authorized. Native W2b, live account quota, user settings/production bridge installation, autostart, release, commit and push remain gated/out of scope.

## Actual Claude review disposition — 2026-10-05 (conditional review; lead corrections checked)

Claude Code CLI completed the original review and two delta reviews; each reported `claude-opus-5-5`, exit0/success. Verbatim results remain in the linked reports. Delta2 recommends lead closure for its3minor wording fixes; those are recorded below and in the delta report. No Windows implementation/runtime proof exists. C2 corrects a factual claim using Mac evidence, without substituting it for Windows tests.

| ID | Reconciled plan disposition | Remaining evidence / stop condition |
|---|---|---|
| C1 blocker | Accepted: explicit clean environment, candidate file-only backend, known-folder/bootstrap audit and no-account/network startup gate; production auth context is separate | Pinned Windows source audit/key/path coverage incomplete. Native W2 blocked until verified; W1/fake/shell/filesystem work may proceed after dispatch. Proxies are diagnostic only |
| C2 major | Accepted control split; broad MCP impossibility disputed by Mac MCP-status evidence. Test-only synthetic RPC separate from production, active-process limit/accounting separate from hook/notify source coverage | Fake-job enforcement/accounting first; native synthetic MCP detector control later only after C1. No model/thread request; Mac results are not Windows proof |
| C3 major | Accepted: W2 records isolated context; W3 reasserts both children's effective authenticated guards before trusting quota | W3 still unauthorized; fail closed on unsupported live layers/inventory/job state |
| C4 major | Accepted: developer/operator reruns gated W2b on changed tuple/policy; W3 schema/import must be defined in a reviewed amendment before live use | Unknown digest/version blocked; W1 metadata-only tuple persistence/disconnected state, no proof import. W2b result is compatibility evidence, not provenance/signature |
| C5 major | Accepted as candidate experiment: Bash owns original command, tee splits to bounded capture-only sink; removes unproven native handoff and OriginalCommand.cs from manifest | W2 streams/single execution/exit, missing deployment, backpressure and top-level death must pass. Native supervisor alternative needs reviewed amendment. Production launcher/installer deferred until candidate gates/review |
| C6 major | Accepted: concrete temp-close/ReplaceFileW flags0/recovery backup, absent-file no-overwrite rename, snapshot sharing distinct from settings, remaining race disclosed | Official API semantics checked; W2 scratch sharing/ACL/reparse/race/partial-failure tests remain. W4 full installer matrix/live preview not authorized; no universal CAS/durability claim |
| C7 minor | Accepted: bounded 2s cleanup-pending after restore checkpoint, manifest retained, explicit safe cleanup-only retry | W4 locked apphost/DLL/capture-vs-restore/fault tests remain; no forced termination or complete-cleanup claim while files remain |

Historical reconciliation self-check before DECISIONS25 dispatch: no task called for native-wrapper readiness/fallback; C1–C7/D1–D8/E1–E3 were reconciled and lead checked conditional delta2 corrections. Owner dispatch was pending at that planning snapshot. The implementation outcome below supersedes that next step.

## W1/W2a actual outcome — 2026-10-05

Owner confirmed dispatch, source implemented within the manifest/ownership
boundaries. Lead Release build0warnings/0errors; mandatory80/0/0; real own-window
tray/menu/lifecycle smoke and single-instance boundary passed. Version0.1.0 is
development output only. [Actual evidence](../validation/windows-mvp-validation.md)
separates these from native/production proof.

- W2a tee43cases:34passed/9failed, both candidates unsupported for cancellation,
  startup/caller EOF, and pipeline held-open producer. No production launcher.
- W2a external-editor file experiment: final inconclusive/exit1; deterministic
  validation-to-replace race proof remains open. Private recovery comparison is
  source-reviewed; no universal CAS claim or settings write authorization.
- Accounting clarification from actual controls: rejected inherited CreateProcess
  may leave total1/terminated0 despite error1816; failed explicit association has
  different counters. Exact accepted accounting/limit1 remains unchanged, and
  no claim of detecting every spawn attempt is allowed. The earlier candidate
  statement about failed associations was not proof about all failed creations.
- Source C1 audit located correct login auth storage path and pinned the Windows
  ProgramData known-folder/default-loader boundary. Native W2b remains blocked;
  no real-executable audit record or harness exists.
- Independent native GPT source review and runtime debugging closed source
  findings; source closure does not close the unsupported/inconclusive gates.

W1 offline delivered; W2a experimental evidence delivered with these stop
conditions. W2b/W3/W4 remain gated. No commit/push/release or Mac source change.

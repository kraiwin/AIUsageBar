# Future H2 bounded native inventory — observation manifest

2026-10-06. Owner authorized read-only readiness/observation planning after H2
preparation. **No native run, source implementation, host probe or tracing setup
is authorized by this document.** It describes evidence for one later approved
inventory invocation. Inventory validity, cleanup and access coverage are
separate results; C1 remains blocked even if the inventory validates.

Inputs: [preparation evidence](2026-10-06-windows-h2-preparation.md),
[reviewed preparation plan](../plans/2026-10-06-windows-h2-inventory.plan.md),
[host contract](2026-10-06-windows-c1-host-contract.md),
[startup source closure](2026-10-06-windows-c1-startup-closure.md).
Source observations below refer to the current local code, read without running
it. Existing framework/Win32 facilities suffice for a limited inventory;
no availability probing, installation, administrative policy, ETW/Procmon,
firewall/tracing session or network query was performed for this manifest.
Completion here means the selected investigation/documentation scope only.
The actual readiness reviewer corroborated the supplied primary excerpts;
other closure traces remain scoped author assertions from the companion
[secret-path study](2026-10-06-windows-h2-secret-path-closure.md), including
malformed-skills-config handling and the second project-root helper not supplied
in that review packet. Omission is a review coverage limit, not evidence those
assertions are false or independently verified. No shipped-binary/runtime
absence claim follows from either category.

| State | Current readiness | Evidence/limit |
|---|---|---|
| CURRENT preparation proof | Complete historical fake evidence | Recorded focused26/full110 passes and Release0warnings/errors; no new test run in this task |
| NATIVE adaptation | Required, no command route implemented | Held image/primary identity, actual system/layer fingerprints, native scratch manifest and immutable sanitized serializer/timeout contract still needed |
| Future OBSERVABLE | Typed RPC inventory, job counts before close, retained primary signal, nominated scratch metadata/known-fixture hashes | Must be implemented/reviewed and separately approved; no launch follows this report |
| Access/network UNKNOWN | File/keyring/DNS/PAC/network attempts/egress coverage inconclusive | No monitors or OS denial established; no strict isolation/C1 pass |
| Admission status | Native not authorized | Fresh machine status, held EXE pin, synthetic root manifest and reachable secret-path closure must each be clear in the later exact run manifest |

## Existing mechanisms and adaptation limits

| Mechanism | Existing source | What it can establish |
|---|---|---|
| Explicit environment/argv/cwd and suspended launch | `WindowsProcess.cs:44–98` | Explicit launch, handle allowlist and limit-one/no-breakaway job assigned before resume; not filesystem/network containment |
| EXE tuple | `CodexDiscovery.cs:28–47` | Explicit nominated candidate, ordinary ancestry, single-link/x64 PE, size/file ID/hash; Inspect closes its pin on return |
| Launch image pin | `WindowsProcess.cs:55` | Read sharing denies write/delete while Start runs; pin is disposed when Start returns, so it does not cover the whole native lifetime |
| Request mode | `CodexProvider.cs:63–65,133–147` | Immutable H2 mode allows four exact methods, rejecting others before stdin; public mode retains its old quota permission |
| Successful finish/accounting | `CodexProvider.cs:159–177` | Bounded drain and successful exit/job counts checked before disposal: active=0,total=1,terminated=0 |
| Owned failure cleanup | `WindowsProcess.cs:111–133`; `CodexProvider.cs:179–191` | Terminate owned job if active, release handles and drainers; failure is surfaced. Job counters are unavailable after job closure |
| Primary signal barrier | `H2InventoryTests.cs:283–301,351–376` | Test coordinator retains a primary handle, awaits disposal and separately waits for process signal before deleting scratch; not a native-run route |
| Scratch fixtures and typed export | `H2InventoryTests.cs:80–146,166–281` | Validated private synthetic paths/fixtures and bounded typed response; unchanged fixtures do not prove absence of reads elsewhere |

These files are under `windows/src/AIUsageBar.Core/` and
`windows/tests/AIUsageBar.Tests/`. H2 preparation is fake-only:
`FakeLaunch` at line158 selects the test assembly, and Program line12 routes
only `--fake-h2`. Current Context expects `Root/machine/config.toml`, fake layer
versions and a fixed one-level fixture/wire/spawn manifest. It cannot be used
unchanged for real machine-layer identities, native version fingerprints or
SQLite/log/cache outputs. No dormant native switch exists.

Future implementation must explicitly derive the real expected layer identities,
version fingerprints and native scratch entries from pinned source plus known
synthetic fixture bytes. It must not learn a permissive expected manifest from
the native response or accept arbitrary files found after launch. Nonempty lower
provider/MCP maps cannot be erased with CLI `{}`; every approved contributing
layer must meet the reviewed empty-map contract.

## Frozen run manifest and prerequisites

Before any runtime approval, a reviewed immutable manifest must contain:

- Exact nominated EXE location and tuple: x64 PE, single link, size, file ID,
  SHA256 `fdda5fa3cf3fb3d000b876720742857676293e4315e4b045fae6f8bd7e866d1d`,
  package `0.160.0-win32-x64` and source commit
  `a956835d020762cb2b570053af06f643a11c0ecc`. Recheck without version/help;
  signature/tarball/source-equivalence limits in the host contract remain.
- External read-only image handle denying write/delete retained from before
  tuple inspection through process cleanup, and identity equality against that
  same nominated image. Never rely solely on Inspect/Start's temporary pins.
  An image pin constrains that image object; it is not an OS isolation boundary.
  Holding that object does not alone bind `CreateProcessW` pathname selection
  to it. The later implementation plan must provide a reviewed argument for
  that binding, explicitly addressing executable pathname/ancestor substitution
  between inspection and creation and the object selected at launch. Two
  matching inspected tuples alone are insufficient; no particular new
  architecture is prescribed by this gate.
- Fresh separately approved H0 metadata for actual known-folder/fallback machine
  targets, no file contents. Newly present/unknown policy stops before launch.
  Record elapsed freshness and the remaining check-to-use race; H0 does not lock
  an absent namespace or authorize a replay by itself.
- Closed named provider-secret checklist for the exact bootstrap/reload tuple,
  including skills/bundled/watcher/home paths mapped by the secret-path study.
  A newly identified reachable secret route stops entry until its named callsite
  and exclusion are resolved. Residual unenumerated whole-binary coverage is
  disclosed uncertainty, not an invented exhaustive-audit gate. Concrete
  fixture/policy/image/machine/context bindings remain admission gates.
  Do not discover owner auth stores
  by probing them. Ordinary public trust/known-folder/proxy metadata remains a
  separately disclosed allowed OS effect, not a provider secret.
- One synthetic private root and explicit complete ancestry/marker inventory;
  canonical local paths, ordinary directories, protected ACLs and file identity
  checks before known synthetic contents are read. No external gitdir/commondir.
  User/project/environment/auth fixtures and expected missing files are named;
  no owner workspace/profile/config file supplies fixture contents.
- Exact argv/TOML guards and no-model-fetch conjunction from the reviewed plan:
  hooks/plugins/code_mode_host/migration/compression/discovery false, notify=[],
  File auth, analytics false, all OTEL exporters none; base auth=None, default
  provider, no command/env_key/bearer/catalog/baseURL/custom provider routes;
  no remote/Noise/persisted environment routes.
- Exactly nine case-insensitive environment entries from an empty dictionary:
  SystemRoot,WINDIR plus synthetic CODEX_HOME,USERPROFILE,HOME,APPDATA,
  LOCALAPPDATA,TEMP,TMP. No PATH, inherited auth, proxy, CA, OTEL or runtime hooks.
  These values are input hygiene, not known-folder or network redirection proof.
- Held primary process identity/handle tied to the exact owned launch, preserved
  through teardown; no PID reopen during cleanup. Current fake helper initially
  reopens by PID immediately after Start, so future design must address that
  acquisition window rather than assuming a PID alone establishes identity.
  Acquisition failure disposes the exact originally owned process and yields
  cleanup/inventory inconclusive, never an exit-barrier pass.
- One invocation only, immutable request/output contracts, cleanup ownership,
  reviewed timeout/recovery strategy and explicit later owner run approval.

## Loader-context binding gate

The future immutable manifest must replace each root ID below with exact
approved synthetic identities/fixture hashes and expected machine namespace
metadata. Set the owned process cwd to the nominated synthetic root; bind
config/read's cwd separately even if it is the same path. Explicit CODEX_HOME
is the same existing synthetic home in every context. Guard predicate **G** is
the complete reviewed bootstrap exclusion tuple: unchanged auth=None; File
storage; empty credential/remote/Noise inputs; default provider with no custom
command/env_key/bearer/catalog/baseURL selection and discovery=false; plugins,
hooks, code_mode_host,migration,compression=false; notify=[]; analytics=false;
all OTEL exporters none; bundled skills explicitly false; named synthetic
instruction/skills/git/ancestry/empty provider+MCP fixtures and fresh machine
absence. Requirements and every contributing layer must preserve G.
Paths in this table are relative to `codex-rs/`; abbreviated config_manager,
config_manager_service and request_processors paths are under `app-server/src/`.

| Loader context | Source path/trace at pinned commit | Exact root binding required in future manifest | Same-guard/fallback evidence and STOP |
|---|---|---|---|
| Bootstrap initial config | `app-server/src/lib.rs:541–545`; `config_manager.rs:321–339,429–468` | load_startup_config(None) resolves to owned PROCESS_CWD via `core/src/config/mod.rs:1501–1530`; bind CODEX_HOME, default harness overrides and complete process-cwd ancestry | Pre-start fixtures/CLI/requirements establish G. strict_config=true excludes initial default-config fallback; unknown/lost binding stops entry |
| Post-cloud-loader initial config | `app-server/src/lib.rs:555–573`; `config_manager.rs:189–200` | load_latest_config(None) uses same PROCESS_CWD/home/machine tuple | Same G. Strict-config excludes app-server load_default_config fallback; do not collapse this second load into the initial one |
| Startup residency synchronization | `app-server/src/lib.rs:587–589`; `config_manager.rs:176–185` loads latest(None) | Same PROCESS_CWD/home/machine fixture tuple; do not omit because it precedes RPC | Same G and unchanged auth. Failure logs while original startup config remains retained; that original object must independently preserve G |
| initialize internal account workspace-routing | `request_processors/account_processor/workspace_routing.rs:131–150,198–231` → read_account(None) → load_latest_config(None) → provider | Explicit PROCESS_CWD/root/ancestry, not an inferred config/read cwd | Successful reload preserves G. With !cached-ChatGPT auth, failed reload can select original startup `self.config` clone EVEN with strict startup: frozen startup clone must independently preserve G and default-provider/cached-auth predicates. Unbound reload or clone stops entry |
| experimentalFeature/list, each page | `request_processors/catalog_processor.rs:346–374`, no threadId → load_latest_config(None) | Same PROCESS_CWD/home/machine tuple on every reload/page; threadId absent | Same G, exact fixtures/requirements on each reload; no loaded-thread context admitted |
| config/read service reload | `config_manager_service.rs:119–135` → load_config_layers(Some(cwd)) | Explicit CONFIG_READ_CWD nominated synthetic root and its complete ancestry | Same G and independently expected layers/origins; nominated response inventories THIS context only |
| config/read additional runtime reload | `request_processors/config_processor.rs:103–108` → load_latest_config(Some(cwd)) | Same explicit CONFIG_READ_CWD but distinct loader call | Same G before provider/feature consumers; service output alone does not establish this second call's invariant |
| configRequirements/read | `config_manager_service.rs:179–191,446–448` load_thread_agnostic_config → `config_manager.rs:481–500` load_config_layers(None) directly | HOME+MACHINE/CLI/managed/cloud thread-agnostic stack; NO project discovery, and NOT process-cwd substitution | Same G for applicable shared layers/requirements; do not expect project layer evidence or silently substitute nominated cwd |
| Shared policy/cloud pre-load, every caller | `app-server/src/application_network.rs:32–96`; `config_manager.rs:429,481` | Local application requirements use fixed loader overrides without RPC cwd; current manager cloud loader uses the same home/system/fallback/CLI binding | Same actual system identity/fresh absence and auth=None cloud exclusion on every call. Unknown local policy/cloud source change stops, regardless matching nominated-cwd export |

`load_latest_config(None)` and `load_config_layers(None)` are different paths:
ConfigBuilder uses harness cwd, fallback cwd, or current process directory;
thread-agnostic layer loading does not perform that substitution. An
initialize/config/read response cannot attest another internal root or fallback
clone. Entry remains **STOP** until the saved native-specific manifest binds
every listed source context, exact root, fixture/requirement invariants and
fallback behavior, with independent positive/negative fixture evidence.
This is documentation of a required future contract, not completed fixtures.
The source mapping was supplied by the companion source investigator; the
actual earlier review excerpts did not independently corroborate every call.

## Exact phases of the future single invocation

| Phase | Actions | Evidence/output and stop condition |
|---|---|---|
| A — admission, before process creation | Check approval/secret-path closure, held image tuple, fresh H0, full synthetic manifest and environment | Safe booleans/hash IDs only. Failure=not-run; no fallback image/provider or automatic replay |
| B — scratch baseline | Enumerate only nominated scratch entries, reject unexpected/reparse/link/ACL identities before content reads; hash only known synthetic fixture bytes | Entry-class counts and known-fixture hash match. Native artifact classes need a prior source-grounded manifest, not fake Snapshot reuse |
| C — owned startup | Use explicit suspended/Unicode/detached launch and pipe handles; job limit1/no-breakaway before resume; establish exact retained primary identity | Named stage/launch success/handle acquisition. Failure disposes owned handles/process; no attach to unrelated processes |
| D — bootstrap and inventory RPC | H2 instance only; initialize own aiusagebar-h2-inventory name, explicitGatewayOauth=true, experimentalApi=true; initialized notification; bounded feature pages; config/read(includeLayers=true, nominated cwd); configRequirements/read(params:null) | Validate handshake, registry, typed config/origins/layers and nullable requirements; preserve every loader-context/fallback binding above. Wrong type/ID/guard/layer/cursor/limit stops; no raw config output |
| E — normal or failed cleanup | Success calls Finish, captures successful job counters before job closure, then awaits disposal and retained primary signal; all parser/cancellation/constructor failures await exact owned cleanup | Inventory-valid cannot precede full successful cleanup. Failed-path unavailable counters are unknown, not copied from a prior success |
| F — scratch after cleanup | Revalidate named scratch identities/classes; compare known fixture hashes and allowed native metadata changes after primary signal | Unexpected entries are metadata-only unsupported effects; do not hash/open unknown contents, recurse through unknown directories or delete them blindly |
| G — final record and release | Emit sanitized result after all cleanup/release attempts; release image/primary/root handles | Inventory status and coverage status separate. Cleanup failure overrides success. Retained unexpected scratch requires explicit reviewed recovery |

The H2 CLIENT request set is ONLY initialize, experimentalFeature/list, config/read,
configRequirements/read; initialized is the existing notification. No CLIENT
account/auth/quota RPC, including expected failures; no MCP/status/thread/turn/
tool/write request or canary. Never call FakeCodexProvider.FetchAsync or use the
public transport constructor for this run. Existing method-isolation test at
`H2InventoryTests.cs:519–537` proves pre-write denial against a fake child;
its public regression deliberately permits synthetic quota and demonstrates
why the future coordinator must select `inventoryOnly:true` explicitly.
Initialize can schedule internal account work; its no-cwd reload and provider
construction must satisfy the context gate above. Client method denial does
not mean no account-related execution inside the native server.

## Budgets, outputs and recovery

Preserve the reviewed bounds: 32 pages/2048 feature rows, 100 rows/page,
no duplicate names/cursors, depth32, config frame2MiB, each stdout/stderr
16MiB/4096lines; no new combined-stream budget. RPC lifecycle has a30s linked
cancellation deadline. Existing Finish, process disposal, transport drainer
wait and retained-primary wait each have their own2s budget; they do not prove
a single2s end-to-end cleanup bound. Record each elapsed interval and whole
cleanup duration against the separately approved acceptance threshold.

Synchronous image/metadata/launch/filesystem calls and cooperative cancellation
do not create a hard wall-clock watchdog. Current stack has no reviewed outer
watchdog for one native invocation. A later amendment may retain explicit
cooperative30s RPC cancellation and sequential2s cleanup waits, with owner
acceptance that synchronous calls can outlive those budgets and a terminal
record may never arrive. It must disclose that limit and the owned recovery
strategy; no new watchdog architecture is mandatory for that choice. Only if
a hard deadline is chosen must its reviewed design name the keeper, held owned
handles, outer termination authority and terminal-record failure semantics.
No hard bound is claimed here. Do not install monitoring tools or invent a
timeout guarantee as part of this manifest.

Safe proposed terminal fields: manifestHash, phase, invocationCount,
inventoryStatus(not-run/valid/invalid/inconclusive), errorCategory,
cleanupStatus(not-started/clear/failed/inconclusive), typedGuardMatch,
layerIdentityMatch, requirementsCompatible, pageCount,rowCount,
jobActive/jobTotal/jobTerminated(number or unknown), primarySignaled,
knownFixtureHashesMatch, expectedArtifactMetadataMatch,
fileAccessCoverage=unknown, keyringAccessCoverage=unknown,
networkAttemptCoverage=unknown, egressCoverage=unknown,
accessCoverage=inconclusive, c1Status=blocked, elapsed interval numbers.
Finalized fields/documentation must say `noClientAccountAuthRpc=true` only when
the sent-client-method evidence supports it, alongside explicit internal-account-
task disclosure. Do not use an `account=false`/"no account work" result label
for a native run: cached-auth/provider-dependent internal work remains disclosed.
Schema must be finalized before implementation; these are planning fields,
not an already implemented serializer. Never output raw paths from response,
config/auth bodies, environment values, headers, diagnostics or unknown filenames.
Known synthetic paths map to manifest entry IDs in the sanitized record.

Recovery is limited to the exact owned process/job and exact validated scratch
entries. No retry/elevation/CLI fallback/login/settings mutation/firewall edit,
unrelated process kill or broad recursive cleanup. Any unvalidated entry or
identity mismatch stops automatic cleanup and retains bounded private evidence
for a later explicit recovery decision. A failed attempt does not authorize a
second invocation; loss of the owning runner/terminal record is inconclusive. Never
delete the scratch root until the exact retained primary handle is signaled.
Timeout/signal/identity failure retains that bounded owned scratch and reports
safe failure categories; job count zero alone does not authorize deletion.

## Observation gaps that remain unknown

| Event class | This manifest's available evidence | Conclusion that must not be made |
|---|---|---|
| Reads/mutations outside nominated scratch | None; returned layers are a parser inventory, not a complete I/O log | No owner-file access or no outside-scratch write |
| Credential Manager/keyring calls | None; source predicates narrow expected routes, no runtime API tracing | No keyring access from File auth alone |
| DNS/PAC/proxy/TLS OS retrieval | None; no trace/network observer is installed/started | No network attempts from scrubbed env or disabled exporters |
| Successful outbound traffic | None; no boundary denial or complete observer | Zero egress, even if no socket is visible after exit |
| Rejected spawn attempts | Job counters and known fake positive control prove limited enforcement/accounting | No spawn attempts; rejected attempts need not create countable children |
| All Windows/platform state reads | Public trust/profile/proxy routes are disclosed source expectations | Public ROOT access equals provider credential access |

The smallest useful result is **typed inventory valid with cleanup clear,
access coverage inconclusive** for that one exact executable/context, or an
invalid/inconclusive result with bounded frames/resources; bounded does not mean
a hard wall-clock deadline. It does not certify authenticated usage,
host owner-secret absence, native compatibility generally, no egress, strict
isolation, C1/C5 pass or a production bridge. Source gates cannot replace
missing runtime observations; accepted missing observations cannot waive
named reachable provider-secret/context prerequisites.

## Self-gate

Re-read the report against assigned planning-only scope and the actual current
launch/Inspect/transport/fake coordinator/cleanup code. Distinguished existing
implementation from required future adaptation, fake evidence from native
observations and per-stage cancellation from a hard watchdog. Checked relative
input links and local line anchors. Markdown has no executable syntax to run;
no build/test/runtime/availability probe or owner-file/environment inspection
occurred. Only this document was added; other agents' edits were preserved.

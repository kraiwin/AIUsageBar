# C1 startup closure packet — pinned source, no execution

2026-10-06. Owner authorized read-only continuation after H0 metadata-clear.
This report closes five bounded source groups; **C1 remains blocked**. No
Codex/app-server/version invocation, account/config-content read, native probe,
installation, app/harness change, commit or push occurred here.

Source pin: `openai/codex` commit
`a956835d020762cb2b570053af06f643a11c0ecc`. Existing audit text was reused;
28 additional official raw source files were downloaded under
`C:/tmp/aiusagebar-windows-source-audit`, with path/URL/SHA256 recorded in
`c1-startup-fetched.json`. No reference scripts/builds ran. Paths below are
relative to `codex-rs/` at that commit. Rust tasks/native threads are distinguished
from child OS processes. Source conditionals are not runtime observations.

The [H0 replay](2026-10-06-windows-h0-guard-fix.md) establishes its measured
point-in-time machine-parent absence only. It does not prohibit a later machine
file appearing, suppress known-folder fallback, disable bootstrap tasks, prove
project-layer completeness, or authorize native execution.

## 1. Construction before RPC: model worker and local state

`app-server/src/lib.rs:968–996` constructs MessageProcessor with plugin startup
tasks unless an embedded runtime option requests otherwise; production default
is Start (`app-server/src/lib.rs:475–485`). MessageProcessor construction (`app-server/src/message_processor.rs:299–388`) creates the thread store, ThreadManager, models manager/catalog and
starts models-refresh/turn-cost workers **before initialize/config assertions**.

`app-server/src/models_refresh_worker.rs:40–58` immediately executes
list_models(Online) in a Tokio task; the 270s sleep happens afterwards. This is
not a model turn and not itself an OS child. `app-server/src/model_catalog.rs:33–45` first checks
managed provider requirements. `models-manager/src/manager.rs:540–579,665–669`
allows refresh when backend auth, command auth or supported API-key discovery
exists; otherwise returns without fetching. A permitted Online refresh reaches
the endpoint at 582 onward and can write the catalog cache. **No-auth alone is
insufficient if provider/command credentials survive config or environment.**
The conditional no-account/default-provider branch is now narrowed by
`model-provider/src/models_endpoint.rs:84–88,218–236`: uses_codex_backend requires
resolved backend auth; has_provider_api_key is true from provider env_key or
experimental_bearer_token **configuration presence**, not just a present env
secret; command auth is a separate provider-info predicate. The smallest
source-grounded synthetic contract is base auth=None, default provider with no
custom auth/env_key/bearer token/catalog/base URL override, no custom provider
selection and api_key_model_discovery=false. Then all three
should_refresh_models terms are false, so the Online worker returns before
endpoint fetch. `model-provider/src/auth.rs:188–198` shows custom provider auth
creates a separate external bearer manager, which must be excluded. This is
conditional source reasoning, not runtime no-request evidence.
Endpoint/provider command execution implementation remains unclosed; do not certify zero network/children by assuming the worker is delayed.
[Pinned immediate worker](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/app-server/src/models_refresh_worker.rs#L40).

`core/src/thread_manager.rs:446–503` local-thread-store creation can start rollout
migration when state DB exists AND background_paginated_rollout_migration=true;
compression starts after that migration or independently when compression=true.
Registry specs at `features/src/lib.rs:1171–1187` give both features real keys,
default false. Neither guard is asserted by current Windows CodexPolicy.
These are task/file effects, not proven OS subprocesses. SQLite initialization/
corruption recovery already documented in the [prior C1 study](2026-10-05-windows-c1-isolation-study.md)
remains independent. `app-server/src/turn_cost_worker.rs:100–115` returns None unless an OTLP
log or metrics exporter exists (and provider is not Bedrock); disable every
exporter rather than assuming no turn means no startup worker.

## 2. Plugin startup: actual process routes and callbacks

`app-server/src/message_processor.rs:540–557` calls maybe_start_plugin_startup_tasks_for_config.
`core-plugins/src/manager.rs:2812–2934` gates the entire warmup block on
config.plugins_enabled. Inside it:

- Curated sync is admitted when plugins_enabled AND remote global catalog is
  inactive AND marketplace policy accepts the curated git source (748–763).
  The manager creates a native worker thread (3287–3340), calls
  sync_openai_plugins_repo and refreshes caches. Exact curated repository
  transport implementation is outside this bounded packet.
- Configured marketplace auto-upgrade starts a native thread (2823–2855).
  `core-plugins/src/marketplace_upgrade.rs:225–274` checks remote revision then clones;
  `core-plugins/src/marketplace_upgrade/git.rs:19–139,184–192` constructs git ls-remote/clone/
  checkout/sparse-checkout/rev-parse commands and **spawns OS processes**.
  This conditional pre-RPC child route is confirmed, not a claim that a child
  actually ran. Empty marketplace configuration narrows this branch but does
  not disable the curated/remote branches.
- Async warmups load auth, refresh remote installed/bundle/catalog caches,
  and featured plugin IDs (2892–2934). Auth/catalog/config-specific gates apply
  inside those helpers; plugins=false is the common outer exclusion.
- `app-server/src/effective_plugin_change.rs:30–68` clears skills/plugin caches,
  invalidates MCP runtimes and refreshes hook runtimes; nonempty materialized
  plugins can enqueue trust-hook config edits (hook edit at 91 onward).

`features/src/lib.rs:1456–1461` declares plugins stable/default true.
Disabling a Removed plugin_hooks compatibility key is not this gate. Require
real plugins=false in effective runtime registry, in addition to known synthetic
bootstrap layers. A post-start assertion cannot undo already dispatched warmups.
[Pinned plugin gate](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/core-plugins/src/manager.rs#L2812).

## 3. Initialize, hooks/notify and MCP versus registry

`app-server/src/request_processors/initialize_processor.rs:155–164` permits automatic gateway
login when explicit_gateway_oauth capability is absent; the pinned v1 Rust definition at
`app-server-protocol/src/protocol/v1.rs:45–50` and JSON schema
`app-server-protocol/schema/json/v1/InitializeParams.json:33` support wire
capabilities.explicitGatewayOauth=true in any future synthetic request. `198–213` registers Desktop installation
on Windows only for Stdio client name exactly Codex Desktop. Use our own client
name, never that name. Initialize tracks analytics at 185; disable analytics
before startup, not after initialize.

`core/src/thread_manager.rs:521–582` constructor creates an empty threads map,
skills/plugin/MCP managers and disabled code-mode provider unless enabled or
disable_in_process_fallback. It does not call Session::spawn in that constructor.
Thread spawn reaches Session::spawn at 2270 through separate thread entrypoints;
future whitelist must exclude all thread/turn/resume/fork/tool requests.
`hooks/src/events/session_start.rs` defines SessionStart/SubagentStart event
handlers; engine dispatch/process implementation and every session callsite are
not exhaustively closed here. Hooks=false is a real stable gate (feature spec
1225–1230), not proof obtained by trying a model/thread positive control.

Legacy notification is independently dangerous:
`hooks/src/legacy_notify.rs:29–70` serializes an AfterAgent completion payload,
builds configured argv and spawns an OS process. notify=[] removes configured
argv; registry hooks=false alone must not substitute for notify=[] evidence.
No completion event is introduced in no-account config/registry work.
SkillsWatcher creates an OS file watcher and async event loop at
`app-server/src/skills_watcher.rs:37–72`; this is not proof of a process child.
Watcher/backend and bundled-skill initialization dependencies remain unclosed.

Experimental feature registry is **not MCP server status**:
`app-server/src/request_processors/catalog_processor.rs:346–411` reloads config and maps the
compile-time FEATURES entries to name/stage/runtime-enabled/default-enabled.
Unknown config keys only warn (`features/src/lib.rs:650–669`) and cannot add
FEATURES rows. Therefore config echo of an unknown false key is no guard proof.
remote_control is a Removed/no-op key; do not count it as functional protection.

`app-server/src/request_processors/mcp_processor.rs:289–347` mcpServerStatus/list loads runtime
MCP config/auth and calls collect_mcp_server_status_snapshot_with_detail; it is
not the feature registry and may initialize transports. It remains a separate
synthetic test-only positive-control exception, never a production whitelist
method or owner-transport discovery method. Its snapshot collector dependency
is not fully audited in this packet. Config/experimentalFeature reads themselves
do not directly call this status collector in the read handlers inspected.

## 4. Config stack, project ancestry and machine fallback

`config/src/loader/mod.rs:266–283` loads machine config; 337–413 computes project
trust/root from merged lower layers and managed sources before adding project
layers. `config/src/loader/project_discovery.rs:15–39` overlays already-read managed configuration
and resolves relative paths at each source base directory. `config/src/loader/mod.rs:1593–1700`
walks cwd ancestry up to inclusive project_root, reverses the order, inspects
.codex directories/config and tracks disabled layers for untrusted projects.
Untrusted/disabled does not mean the loader never reads that layer.

`git-utils/src/trust.rs:13–16,71–164` resolves git/worktree roots via filesystem
metadata, .git/HEAD/gitdir/commondir reads **without invoking git executable**.
Those metadata references can still point beyond a naive scratch cwd boundary.
Do not treat scratch cwd as an inventory of all accessible ancestor/worktree
paths. Synthetic ancestry/markers and every inspected path need a manifest.

`app-server/src/config_manager_service.rs:119–176,179–191` read/requirements-read
reload layers, apply managed exact values, serialize effective config and
origins, optionally return layers. `app-server/src/request_processors/catalog_processor.rs:346–374` registry read
also reloads config. Read RPCs may re-enter cloud/system loading; they are not
standalone pure serialization of a frozen startup snapshot.

`config/src/loader/mod.rs:798–820` known-folder failure falls back to
C:/ProgramData/OpenAI/Codex. H0 must not silently treat one successful resolution
as a permanent guarantee. No host machine files were read in this investigation.
The earlier auth/cloud/environment/proxy/CA/telemetry maps still apply unchanged.

## 5. Boolean prerequisites for a future host no-account plan

These are necessary checks for a proposed plan, **not permission or a passing
certificate**. Native execution must remain off until all are resolved and a
concrete plan is reviewed/approved. A host experiment cannot claim guest-level
filesystem/network isolation.

| Predicate | Required truth before launch/acceptance | Current evidence |
|---|---|---|
| P0 owner scope | Explicit native config/feature-registry-only authorization; no quota/account/auth calls even expected failures | This round authorizes source reads only |
| P1 binary/context | Exact pinned binary/hash/distribution binding, x64 context, default direct entrypoint, synthetic home/environment/cwd and ancestry | Source pin only; no new binary/runtime check |
| P2 machine boundary | Fresh approved metadata evidence, actual known-folder/fallback behavior, race/freshness limits stated; no contents read or settings altered | H0 point-in-time absence only |
| P3 pre-RPC synthetic policy | Real hooks/plugins/code_mode_host false; notify=[]; analytics false; all OTEL exporters none; migration/compression false; no remote environment routes; complete no-model-fetch conjunction: base auth=None AND default provider AND no custom command auth/env_key/bearer/catalog/base URL/custom provider selection AND api_key_model_discovery=false | Source recognizes routes; current policy does not assert every extra predicate |
| P4 initialize | Our own name; explicit_gateway_oauth=true; no Desktop registration branch; analytics already off | Source conditional only |
| P5 read validation | Registry includes unique required live-stage false rows; do not accept Removed/unknown keys; complete effective layers and inventory; strict parse/fail closed | Needs future synthetic runtime evidence |
| P6 no descendants | Suspended→limit1/no-breakaway job assignment→resume; account children before job close, no-MCP/no-hook/no-git markers throughout startup; distinct positive control proves detector | Existing fake transport proof only; native startup unobserved |
| P7 RPC scope | initialize/initialized, experimentalFeature/list, config/read only; bounded pages/frames/deadline; separately approved synthetic mcpServerStatus/list canary | No RPC executed |
| P8 effects/cleanup | Strict all-event coverage requires approved observation of pre-assertion file/network/descendants and complete cleanup, without raw sensitive logs. A future bounded inventory is evidence only for its enumerated paths/events; proxy/env/job are not OS no-network proof | No all-access monitors currently established; missing coverage remains inconclusive, never a C1 pass |

Ordinary OS public CA/trust-store metadata reads are not themselves owner
credential access. Custom CA/TLS file paths, provider auth and account/keyring
routes remain separate concerns; environment scrubbing and source route gates
must not equate a public trust-store read with an auth-store read.

Unknowns deliberately retained: exact model endpoint command-provider execution,
curated sync/remote plugin transport, MCP collector/connection implementation,
hook dispatcher/session callsites, watcher/skills/backend library effects,
release provenance and host runtime network/file behavior. Five source groups
are mapped; there is **no exhaustive audit-complete or native-pass claim**.
Recommendation: retain C1 block while lead determines whether P3/P6/P8 can be
made concrete under existing host contract; if not, disposable guest remains
the isolation branch. No additional source-research permission is requested.

## Self-gate

Re-read the authored report and targeted source excerpts. Verified all 28 fetched
source SHA256 values, cited relative source path/line anchors and local report
links. Rechecked the no-auth model predicates and initialize schema. No executable artifact or runtime test was created. All claims identify
source facts, conditionals, existing observations or future prerequisites.

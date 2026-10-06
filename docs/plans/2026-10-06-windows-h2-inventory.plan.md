# Plan: H2 bounded inventory preparation, separately approved native run

2026-10-06. Owner selected bounded H2 planning (DECISIONS32). Delegated
architectural design came from the existing research-agent thread; no new
architect role was spawned. Lead is sole plan writer. Actual CLI delta review
gave PROCEED for preparation PLAN. Owner subsequently approved BOTH plan and
dispatch for PREPARATION (DECISIONS33); source/build/fake tests now authorized.
Native execution remains outside that approval.

## Summary

Prepare a test-only policy, typed inventory validators and synthetic process
tests. Native execution is a separately reviewed/approved later amendment.
Bounded host inventory cannot establish strict isolation/C1 pass; unavailable
access/network observations remain inconclusive. Provider secrets and all
account/auth/quota calls remain forbidden, even when normal OS reads, scratch
state and possible unauthenticated early attempts are accepted in the contract.

## Patterns to Mirror

| Category | Source (path:line) | Pattern and limit |
|---|---|---|
| Explicit launch | windows/src/AIUsageBar.Core/WindowsProcess.cs:7,44 | Executable/cwd/environment supplied, no parent-env copy |
| Owned startup | WindowsProcess.cs:52–80 | Image pin, suspended/no-breakaway/limit1 job before resume |
| JSONL | windows/src/AIUsageBar.Core/CodexProvider.cs:54 | Bounded drainer/envelope/requestID/finish/disposal |
| Method set | CodexProvider.cs:131 | Current hardcoded whitelist lacks configRequirements/read |
| Inspection | windows/src/AIUsageBar.Core/CodexDiscovery.cs:34 | Explicit PE/x64/single-link/identity/hash, no execution |
| Fake routing | windows/tests/AIUsageBar.Tests/Program.cs:11,24 | Explicit synthetic child modes, nonempty offline filter |
| Transport cases | windows/tests/AIUsageBar.Tests/CodexTests.cs:123,157,210 | Strict frames/IDs/cleanup positive and negative fixtures |
| Bootstrap facts | docs/validation/2026-10-06-windows-c1-startup-closure.md | Pre-RPC guards, model/plugin/init/registry/source conditions |
| Host contract | docs/validation/2026-10-06-windows-c1-host-contract.md | No env containment/no-egress proof; shared host token |
| H0 | docs/validation/2026-10-06-windows-h0-guard-fix.md | Point-in-time metadata, nativeAuthorized=false |

No complete H2 parser/credential-path exclusion manifest/universal monitor
exists. Existing FakeCodexProvider.FetchAsync launches two children and asks
quota; H2 MUST NOT call it. Current CodexPolicy remains fake/production unchanged.

## Files to Change

Future preparation source, only after reviewed-plan approval and dispatch:

| File | Action | Why |
|---|---|---|
| windows/tests/AIUsageBar.Tests/H2InventoryTests.cs | CREATE | Test-only policy/parser/coordinator/fixtures/fake child/public-versus-H2 regressions |
| windows/tests/AIUsageBar.Tests/Program.cs | UPDATE | Register h2-inventory/ offline cases and explicit fake-H2 route; no native route |
| windows/src/AIUsageBar.Core/CodexProvider.cs | UPDATE | Internal immutable per-instance H2 closed whitelist; public behavior unchanged |

Lead documentation:

| File | Action | Why |
|---|---|---|
| docs/plans/2026-10-06-windows-h2-inventory.plan.md | CREATE | Saved design/dispositions |
| docs/plans/2026-10-06-windows-h2-inventory-review.md | CREATE | Actual CLI evidence |
| docs/validation/2026-10-06-windows-h2-preparation.md | CREATE after implementation | Measured fake/build evidence and run prerequisites |
| docs/STATUS.md, docs/DECISIONS.md, MEMORY.md, windows/AGENTS.md, CHANGELOG.md | UPDATE | Actual instructions/scope/results |
| windows/README.md | UPDATE after implementation | Approved fake modes/replay commands and limitations |

No CodexPolicy/FakeCodexProvider algorithm, WindowsProcess, CodexDiscovery,
CodexTests, Tray/Bridge/installer/projects/dependencies/Mac change. Existing
Core test friend access is available. Future native routing needs an amendment
to H2InventoryTests/Program, not a dormant usable route in preparation.

## Tasks

### Task 1: Freeze gates and owners

- Action: actual CLI review/dispositions, owner plan approval and dispatch
  precede source. Backend owns Core transport delta; QA owns new test/routing;
  lead integrates/reviews/docs, everyone preserves others' edits.
- Mirror: existing plan-gate and synthetic-only source ownership.
- Validate: preparation permits existing framework code/build/offline fakes ONLY;
  no nominated Codex version/help/invocation, owner config contents/accounts.

Native-phase approval must separately disclose shared Windows token, normal
known-folder/proxy/public-root access, scratch writes, possible unauthenticated
attempts and incomplete observations. It does not waive provider-secret exclusion.

### Task 2: Native entry prerequisites, not current runtime claims

- Action: maintain a source-grounded reachable provider-secret checklist for
  exact bootstrap policy. File storage/synthetic absent auth, no PAT/agent/
  workload/env credentials, no keyring selection/custom provider auth/remote
  Noise/persisted environments.toml. Unknown reachable secret route blocks run.
- Mirror: source reports and exact pinned binary context.
- Validate: baseauth=None AND defaultprovider AND no command auth/env_key/bearer/
  catalog/baseURL/custom-provider selection AND api_key_model_discovery=false.
  No missing/null echo alone proves safe runtime defaults. Explicitly close
  HostSkillsService/bundled initialization/watcher backend/home fallbacks;
  SkillsWatcher constructor alone does not scan registered roots.

Source preparation may proceed while entry is blocked. Public roots differ
from provider secrets; CODEX_HOME does not create filesystem/OS isolation.

### Task 3: Internal H2 transport method mode

- Action: immutable internal per-instance inventory-only constructor/factory in
  CodexProvider.cs, using existing friend access. Public constructor keeps its
  current set/behavior. Reuse drainer/framing/cleanup, no global/env switch.
- Mirror: JsonRpcTransport entry/RequestAsync/NotifyInitialized/Finish/Dispose.
- Validate: H2 allows ONLY initialize, experimentalFeature/list, config/read,
  configRequirements/read requests; initialized remains existing notification.
  Quota/account/auth/MCP/status/thread/turn/tool/write rejected BEFORE stdin.
  Public construction still rejects configRequirements/read; H2 cannot enable
  quota even if supplied to a wrong caller. Handshake params already caller-
  supplied, no handshake API change. Requirements params is JSON null, not {}.

### Task 4: Test-only policy and independent synthetic fixtures

- Action: H2 policy stays in new test file, not current CodexPolicy. Exact argv/
  TOML/source keys: features.hooks/plugins/code_mode_host=false; notify=[];
  analytics.enabled=false; otel.exporter/trace_exporter/metrics_exporter="none";
  features.background_paginated_rollout_migration/local_thread_store_compression/
  api_key_model_discovery=false; cli_auth_credentials_store="file".
- Mirror: existing escaping/ProcessLaunch and source registry definitions.
- Validate: real live-stage keys; remote_control Removed is not protection.
  All three OTEL kinds explicitly none: raw config defaults metrics_exporter to
  Statsig, while provider construction maps it to None when analytics=false.
  H2 deliberately requires explicit none AND analytics=false rather than
  relying on that conditional/default distinction. Managed override
  conflicts fail. No owner-provider/bootstrap-sensitive values are imported.

Create empty case-insensitive environment dictionary with approved SystemRoot/
WINDIR and nominated synthetic CODEX_HOME/USERPROFILE/HOME/APPDATA/LOCALAPPDATA/
TEMP/TMP only; no PATH or inherited secret/proxy/CA/OTEL/runtime hooks. Additions
need source-grounded reason/review. Synthetic user/project/CLI layers have
known hashes, canonical/private roots, explicit ancestry/markers and no external
gitdir/commondir. Missing/malformed/escaped paths fail before child construction.

`mcp_servers={}` and `model_providers={}` are explicit fixture keys, but empty
CLI tables do NOT erase lower table entries in the pinned recursive merge.
Every contributing synthetic lower layer and the merged map must be empty;
unknown/nonempty lower inputs stop before child construction. No native
owner-layer contents are read to make these fixtures.

### Task 5: Exact fake wire sequence and bounds

- Action: coordinator exercises closed whitelist against explicitly fake child.
- Mirror: bounded IDs/deadline/paging plus pinned protocol schemas.
- Validate: independent literal expected wire fixtures, not generated by the
  validator/policy being tested; record synthetic methods/params only.

Sequence: initialize(clientInfo.name=aiusagebar-h2-inventory, own title/version,
capabilities.explicitGatewayOauth=true AND experimentalApi=true; no attestation/
extensions), validate userAgent/codexHome/platformFamily/platformOs, notify
initialized, paginate experimentalFeature/list(limit100,cursor,threadId omitted),
config/read(includeLayers=true,cwd=synthetic nominated), configRequirements/read
(params:null), close stdin/finish/drain/account/dispose. Never Codex Desktop.

UserAgent nonempty, codexHome exact synthetic, platformFamily/platformOs Windows.
Feature pages<=32/entries<=2048/unique names/no repeated cursor/<=100perpage;
rows name/stage/enabled/defaultEnabled typed, nextCursor nullable. Required live
rows false; unknown/Removed echoes don't satisfy. MCP inventory<=256 names;
config frame<=2MiB; EACH stdout/stderr stream separately<=16MiB/4096lines;
no new combined budget. Finite total30s and existing2s
cleanup. Requirements/origins/layers share bounded frame/depth/collection checks.

Immediately after transport creation enter `await using`; if process creation
succeeds but transport construction fails, await disposal of that exact owned
process. On handshake/paging/config/requirements/parser/timeout/cancellation
failures, await disposal before surfacing a safe outcome. Success requires
FinishAsync then successful awaited disposal before inventory-valid is emitted.
Dispose failure overrides success; record parser cause/cleanup status without raw
messages. Negative fixtures cover every stage and zero owned survivors after
cleanup; never credit observer cleanup as pre-cleanup runtime evidence.

### Task 6: Typed partial export and full consumer field matrix

- Action: config wrapper/origins/layers are camelCase; inner Config snake_case
  with flattened additional/optional fields. It is NOT resolved runtime Config.
- Mirror: pinned v2/config.rs:281–315,321–335,389–440,737–740 and v1:45–84.
- Validate: explicit synthetic notify/analytics/all OTEL/File storage/features
  evidence required; missing/null guard values stop acceptance. Cross-check with
  unique known live-stage registry rows. Null model_provider allowed only as
  default-provider source contract or explicit approved defaultID; custom
  selection denied. model_providers corroborates known fixture/map, excludes
  command/env_key/bearer/catalog/baseURL routes; fixture/source predicates,
  not null echo, establish no-model-fetch expectation.

Origins/layers: name is a tagged ConfigLayerSource OBJECT, not string. Expected
layer set/order/identity/version includes disabled project layers and exactly
the expected EMPTY System row even when its file is absent: loader always
pushes that row. Match its known-folder file identity, empty config and fresh
absence evidence; nonempty/unexpected System identity stops. Presence of a row
alone never proves machine-file presence. Unexpected managed/cloud stops.
Compare every consumed casing/type/nullability field
against independently authored full synthetic fixtures (RPC equivalent of
real-response curl field comparison). No raw returned config in output/logs.

Requirements may be null. Object fields may be absent/null per schema; nonnull
modelProvider/modelProviders/cliAuthCredentialsStore/chatgptBaseUrl/
featureRequirements must be typed/compatible. Also handle returned path-sensitive
sqliteHome/logDir/modelCatalogJson constraints source-grounded; unsupported
bootstrap constraint fails. Ordinary unknown descriptive fields need not reject
the entire extensible protocol. Missing required guard evidence never defaults
to safe. All new parsed fields require positive/missing/wrong-type/null/unsafe
consumer fixtures; no create-only state or parser truth from its own producer.

### Task 6A: Explicit consumer contract (M1 acceptance matrix)

All result envelopes must be objects; `result:null` is invalid. Global H2 parsing
uses depth<=32, bounded collections and strings, no raw config logging. Paths
canonical Windows absolute identities compared ordinal-ignore-case against
the exact fixture/manifest; path ambiguity/escape stops. Nonpath source/version/
map keys compare ordinal exact. Every row below has literal positive, missing,
wrong-type, null and unsafe/conflicting fixture variants as applicable.

| Wire path | Shape/bound | Synthetic acceptance / consumer decision | Producer |
|---|---|---|---|
| initialize result.userAgent | required nonempty string<=1024bytes | Presence/type only, never print raw | v1.rs:73–84 |
| initialize result.codexHome | required absolute string<=4096bytes | Exact canonical nominated synthetic home | v1.rs:73–84 |
| initialize result.platformFamily/platformOs | required strings<=64bytes | Both exact windows for Windows fixture/context | v1.rs:73–84 |
| features result.data | required array<=100 | Total<=2048/32pages, unique name across pages | experimental_feature.rs:42–71 |
| data[].name/stage | required nonempty strings<=256bytes | Known guard name/live stable,beta,underDevelopment; Removed can't satisfy | same |
| data[].enabled/defaultEnabled | required booleans | Guard enabled=false; defaultEnabled metadata need not false | same |
| result.nextCursor | absent/null or nonempty string<=4096bytes | Null/absent terminal; cycle/repetition stops | same |
| config result.config | required object | Partial typed API export, not runtime-default dump | v2/config.rs:281–315,402–407 |
| config.notify | required array length0 | Empty[] and layer content; no leaf origin exists for empty array | fingerprint.rs:7–47 |
| config.cli_auth_credentials_store | required string | Exact file and scalar origin | config API flatten/storage source |
| config.analytics.enabled | required boolean | false and scalar origin | config API/source policy |
| config.otel.exporter/trace_exporter/metrics_exporter | required strings | Each none and its scalar origin, absent/null stops | config types.rs:599–656 |
| config.features | required object<=2048keys | Six named guards below explicit boolfalse + registry corroboration | feature registry/config export |
| config.model_provider | absent/null or string<=256bytes | Only established default openai; fixture/source predicates still required | v2/config.rs:281–315 |
| config.model_providers | required object length0 | No custom provider definitions; lower/merged maps empty; no origin leaf for{} | flatten/merge.rs:86–135 |
| config.mcp_servers | required object length0 | No endpoints/providers; reject nonempty, lower maps empty | flatten/merge.rs:86–135 |
| result.layers | required array<=64 when includeLayers=true | High-to-low precedence, no type sorting, disabled rows inventoried | service.rs:156–175 |
| layers[].name | required tagged object | type+variant identity table below, match full expected identities not one pertype | v2/config.rs:30–108 |
| layers[].version/config | required nonempty string<=512bytes/object | Exact version and independent fixture content | v2/config.rs:321–335 |
| layers[].disabledReason | absent/null or string<=4096bytes | Exact allowed disabled fixture; disabled contributes no origins/value | same |
| result.origins | required object<=4096entries | Keys are LEAF dotted TOML paths/array indices, not camelcased mapkeys | fingerprint.rs:7–47/state.rs:458–513 |
| origins[key].name/version | required taggedobject/nonempty string<=512bytes | Expected contributing enabled layer identity/version | v2/config.rs:321–324 |
| requirements result.requirements | required member null or object | Null member valid; null RPC result invalid | v2/config.rs:737–740 |
| requirements.allowedLoginMethods | absent/null or array of api/chatgpt<=2 | No duplicates, empty/subset valid, no login action | config_processor.rs:391–411 |
| requirements.modelProvider | absent/null or string | Only approved default openai | v2/config.rs:413–458 |
| requirements.modelProviders | absent/null or object length0 | Nonempty definitions rejected in bounded subset; nested names are TOML/snake_case, not camelcase | same |
| requirements.cliAuthCredentialsStore | absent/null or file/keyring/auto/ephemeral string | Only null/absent/file compatible; others reject | same |
| requirements.chatgptBaseUrl | absent/null or string | Minimal subset accepts null/absent ONLY; nonnull unsupported | same |
| requirements.featureRequirements | absent/null or object<=2048 boolvalues | Known supported predicate keys only, compatible false guards; unknown/conflict stops | same |
| requirements.sqliteHome/logDir/modelCatalogJson | absent/null or PathUri string | Minimal subset accepts null/absent; nonnull unsupported pending exact scratch representation | same |
| other requirements properties | bounded JSON | Other NONNULL constraints unsupported; no inferred safe consumer | same |

Required feature scalar keys: hooks, plugins, code_mode_host,
background_paginated_rollout_migration, local_thread_store_compression,
api_key_model_discovery. Provider/MCP empty maps are a deliberately smaller
contract than generic server/provider parsing; former <=256 inventory bound is
only a hard resource ceiling/negative fixture, not acceptance of256 servers.

Tagged source table (all type values case exact): packagedDefaults(file),
mdm(domain,key), system(file), enterpriseManaged(id,name), user(file,nullable
profile), project(dotCodexFolder), sessionFlags(tag only),
legacyManagedConfigTomlFromFile(file), legacyManagedConfigTomlFromMdm(tag only).
Identity strings required/nonempty/bounded<=4096bytes; user profile may be absent/
null or exact selected synthetic profile. Validate tags/fields, then match full
ordered fixture identity set; unknown/unexpected tags stop. Returned layers
filter packagedDefaults but default origins may also be filtered. Don't invent
one layer pertype: multiple user/profile/project identities can share a type.

Origins scalar keys are exact dotted TOML paths:
cli_auth_credentials_store, analytics.enabled, otel.exporter/trace_exporter/
metrics_exporter and features.<each six guard names>; model_provider only when
explicit. Layer merge processes enabled low-to-high contributors; later leaf
metadata wins. Returned layer ARRAY is high-to-low. Empty collections produce
NO origins, so validate notify/model_providers/mcp_servers via merged values AND
layer contents. Packaged-default and exact-requirement-controlled origins are
filtered; no origin-per-exported-field rule. Explicit scalar guards require
expected contributing CLI/synthetic layer/version OR an explicitly checked
compatible exact requirement. Missing such evidence stops, not safe default.

Requirements null is normal only when no requirements and default effective
login methods api/chatgpt; an object containing only allowedLoginMethods can
also be normal. Add independent valid fixture for that shape. Compatible exact
requirements never justify ignoring their returned object because origin was
filtered. Every bounded-subset rejection is unsupported/inventory-invalid,
not proof the native CLI is globally unsafe or incompatible.

### Task 7: Preparation-only registration and verification

- Action: new file owns fake-H2 child, parser/policy/coordinator, public/H2
  method regressions; Program registers only offline/fake-H2 preparation paths.
  Native mode unavailable; root result labels native=false/account=false.
- Mirror: existing safe category/count/source-only runner and fake process jobs.
- Validate: focused --offline --case h2-inventory/ selects NONZERO/all new cases.
  Tests: exact methods/null params/capabilities, forbidden quota/MCP before write,
  public/H2 mode isolation; full layers/nullable requirements; wrong casing/types/
  missing guards/unknown-Removed registry; paging duplicates/limits/cycles;
  enabled guard/migration/provider override/managed conflict/layer drift;
  empty env/root escape; malformed/stale/server-request/oversize/EOF/drain/
  cleanup; fake blocked-child accounting limitations. Default offline regressions
  remain mandatory; no native canary or relabeling failed tee experiments.

After owner dispatch: source/AST/XML reread and independent static review before
downstream tests. Exact SDK10.0.401/global.json rollForward=disable, prerelease
false; WindowsDesktop ref/runtime10.0.12 already present in environment evidence.
Lead metadata-only check confirmed sdk10.0.401 and DesktopRef10.0.12 directories,
and all4project.assets.json exist with0package entries (Core0libraries, other3
only1Core project entry). NuGet sources clear/NuGetAuditfalse; new source files
use implicit compilation, no project/dependency change. Recheck these fixed
prerequisites before future command; missing stops, no SDK/restore/install/download.
Exact documented process-local controls (no system env changes), from windows:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
dotnet build AIUsageBar.Windows.slnx -c Release --no-restore
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build --no-restore --no-launch-profile -- --offline --case h2-inventory/
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build --no-restore --no-launch-profile -- --offline
```

Build0warnings/errors, actual focused/fullcounts/durations/no skip, independent
review and source hashes recorded. Prior84/84 remains historical until rerun.
No build/tests are authorized or run during current planning.

### Task 8: Later native-run amendment, not hidden activation

- Action: after prep evidence and secret-path closure, save immutable argv/env/
  fixture/RPC/observation/image manifest, actual CLI review and owner run approval.
- Mirror: owned WindowsProcess and H0 metadata scope, not OS containment.
- Validate: exact nominated EXE SHA256
  fdda5fa3cf3fb3d000b876720742857676293e4315e4b045fae6f8bd7e866d1d,
  reinspection PE/x64/identity/size/hash without version/help; external no-write/
  no-delete image handle held through tuple inspection/launch/cleanup and identity
  comparison. Unsafe/new image/path stops. Package/source correspondence retains
  signature/tarball/source-equivalence limits; do not invent reproducibility.

Fresh approved machine/H0 and synthetic ancestry before launch; owner config
contents not read. Unknown provider-secret path stops regardless accepted normal
OS effects. Available inventory evidence: returned typed fields/guards/layers/
requirements, measured job counters/cleanup, synthetic scratch before/after.
Absent file/keyring/DNS/PAC/network-attempt monitoring remains unknown; no zero-
egress/no-attempt/strict-isolation/C1-pass claim. Limit1 does not prove rejected
spawn attempts absent. Native outcome can be inventory-valid yet access coverage
inconclusive. No auto-native launch after build/H0/fake suite or source prep.

## Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Post-RPC guards treated as bootstrap protection | High | Synthetic pre-start predicates and secret checklist |
| Provider presence enables Online fetch | High | Full conjunction, negative fixtures, typed nullable export |
| Core method mode broadens default behavior | Medium | Internal immutable instance policy and public regressions |
| Requirements/null/casing wrong | Medium | Pinned schema and every consumer-field pair |
| Owner-home/skills secret route hidden | High | Named closure prerequisite; native entry stops |
| Metadata/image pin treated as lasting isolation | High | Fresh checks, held pin, explicit race/provenance limits |
| Partial observation becomes C1 pass | High | Inventory status distinct from access unknowns |
| Preparation enables native mode | High | No native routing until reviewed amendment/run approval |

## Codex review disposition

At the review checkpoint: Actual CLI delta review completed, PROCEED for preparation planning. At that time no source edits/build/tests/probe/native/account/
settings/install/commit/push. Owner chose contract/planning only, not this source
plan or dispatch. C1/C5/H1 remain blocked; H2 native remains separately gated.

First actual CLI review20261006-110522: fix-then-proceed1Major/3Minor/2Info,
exit0/CLI0.160/gpt-6.1-sol/OpenAI/medium. Accepted M1 explicit Task6A matrix and
source provenance/nullable/merge details; m1 awaited ownership on every exit;
m2 per-stream bounds preserved; m3 exact SDK/assets/env contract. Native GPT
countercheck also accepted: tagged layer sources, expected empty System row,
raw Statsig default vs analytics-disabled runtime None distinction. Current
plan corrections were closed by actual CLI delta, not a claimed runtime result.

Final actual delta20261006-112110-h2-inventory-delta-4694, CLI0.160.0,
gpt-6.1-sol/OpenAI/medium, exit0/raw65619bytes: M1/m1/m2/m3 closed at PLAN level;
0Major/0Minor/2Info. Lead read exact raw834–860. Info: some cited source/SDK
observations were author assertions outside this review's excerpts; native
authorization/secret closure remains separate. No original inference replaced
those review limits. Full provenance in companion review report.

At the planning checkpoint owner had selected contract only. Owner subsequently
approved plan AND dispatch for three-file preparation/offline/build/review/docs
under DECISIONS33. Native amendment/run remain separately gated. Planning
self-gate checked refs/Markdown and26 baseline source hashes unchanged; new
implementation evidence must be measured, never inferred from that baseline.


## Preparation execution checkpoint — 2026-10-06

DECISIONS33 owner approved both gates. Three-file implementation completed;
independent native GPT implementation review accepted the final delta. Release
build0warnings/errors, focused26/26 and full offline110/110,0failed/0skipped.
Initial compiler and intermittent cleanup failures are retained in the
[preparation evidence](../validation/2026-10-06-windows-h2-preparation.md), with
explicit nullable checks and test-only retained-process signal waits as scoped
corrections. Native H2 remains outside this approval and has no runner route.

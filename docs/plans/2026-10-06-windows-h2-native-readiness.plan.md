# Plan: Native H2 readiness after synthetic preparation

2026-10-06. DECISIONS34 authorizes read-only source closure, observation planning
and model review. This plan is a readiness decision and contract for the next
amendment, not approval to implement or invoke native H2. Lead is sole writer;
existing research threads supply source/observation evidence and independent
skeptical review. No new architect/reviewer role was spawned.

## Summary

Determine whether the exact four-RPC inventory can avoid known reachable
provider-secret routes on this host, and specify the evidence a later native
attempt could actually supply. Retain offline mode if a reachable secret route
is unclosed. Fake tests are preparation evidence only; neither their success
nor this model review enables a native route.

## Patterns to Mirror

| Category | Source | Pattern and limit |
|---|---|---|
| Current proof | docs/validation/2026-10-06-windows-h2-preparation.md | Release0warnings/errors, focused26/26 and full110/110 are historical fake-only evidence |
| Closed request set | windows/src/AIUsageBar.Core/CodexProvider.cs:63,133 | Explicit immutable inventoryOnly:true; pre-stdin denial; public quota coordinator forbidden |
| Direct launch | windows/src/AIUsageBar.Core/WindowsProcess.cs:44 | Explicit image/argv/env/cwd; suspended then limit-one/no-breakaway job; no OS file/network isolation |
| Image tuple | windows/src/AIUsageBar.Core/CodexDiscovery.cs:28 | PE/x64/size/fileID/hash with short-lived pin; external whole-run image pin required later |
| Cleanup | windows/tests/AIUsageBar.Tests/H2InventoryTests.cs:283,351 | Retained process signal after disposal before scratch cleanup; fake PID acquisition is not native identity proof |
| Typed fixtures | H2InventoryTests.cs:116,234 | Exact fake manifest/layer versions cannot be relabelled native expectations |
| Host limits | docs/validation/2026-10-06-windows-c1-host-contract.md | Shared Windows token, known folders/proxy/public trust, selected source mapping and binary-provenance limits |

## Files to Change

Current authorized artifacts only:

| File | Action | Purpose |
|---|---|---|
| docs/validation/2026-10-06-windows-h2-secret-path-closure.md | CREATE | Exact source-reachable secret-route checklist, predicates and residual admission stops |
| docs/validation/2026-10-06-windows-h2-observation-manifest.md | CREATE | Current/future/unknown evidence, phases, output and recovery contract |
| docs/plans/2026-10-06-windows-h2-native-readiness.plan.md | CREATE | Grounded readiness decision and later amendment requirements |
| docs/plans/2026-10-06-windows-h2-native-readiness-review.md | CREATE | Actual Codex CLI input/verdict/provenance/dispositions |
| docs/DECISIONS.md, docs/STATUS.md, MEMORY.md, CHANGELOG.md | UPDATE | Authorization, measured planning outcome and next exact gate |

No application/test/project source or dependency changes. Do not add a dormant
native switch. If a later code amendment becomes justified, it must enumerate
its exact source files, producer/consumer/cleanup/test pairs and negative cases
in a new saved implementation plan; this document is not that dispatch permit.

## Tasks

### Task 1: Freeze source and authorization

- Action: Read current project decisions/state. Bind official Codex source to
  a956835d020762cb2b570053af06f643a11c0ecc (rust-v0.160.0), and selected dependency
  sources to their pinned lock checksums. Read source only, never owner profile
  auth/config contents or environment secret values.
- Mirror: Existing C1 source audit and read-only research threads.
- Validate: Source SHA256/citation anchors; initial application-source baseline
  C:/tmp/aiusagebar-h2-readiness-source-baseline.json. No vendor invocation,
  version/help, app-server, build/test, tracing setup or host policy change.
  Actual Codex CLI model review is a separately authorized planning workflow.

### Task 2: Close known secret routes for the exact bootstrap and RPC set

- Action: Trace startup through initialize/initialized, feature paging,
  config/read(includeLayers=true,cwd=synthetic root), and
  configRequirements/read(params:null), including config reload and skills
  watcher/host/bundled/home callers. Classify each route as excluded by an
  explicit pre-start predicate, synthetic-only, allowed ordinary OS metadata,
  or unclosed. A post-RPC config echo is not bootstrap protection.
- Mirror: Source-auth factory, fresh nine-entry environment, synthetic empty
  auth/environment/provider layers and the no-model-refresh conjunction.
- Validate: File auth and synthetic CODEX_HOME; absent PAT/agent/workload/Noise/
  remote variables and environments.toml; no command/env_key/bearer/catalog/
  baseURL/custom provider; no owner skills/config/auth discovery. Source mapping
  is finite and conditional, never universal runtime absence of access.
  The source packet closes the inspected skills/bundled/watcher/AGENTS routes
  conditionally. Future policy must explicitly select supported
  skills.bundled.enabled=false (default/malformed fallback is true), preserve
  that choice on reload and add independent typed positive/negative fixtures.
  A regular synthetic .git directory and single-link HEAD must terminate both
  inspected git-discovery helpers; the H2 project marker alone does not.
  initialize also schedules internal read_account(None) workspace-routing work;
  auth_cached=None and default/no-custom provider exclude its backend branch.
  Do not turn 'no client account RPC' into 'no account-related internal code'.
  Enumerate all nine source contexts individually: initial strict startup;
  post-cloud-loader startup; residency synchronization; initialize/internal
  load_latest_config(None) and its original-startup-config fallback; feature
  paging; config/read layer service; config/read runtime-feature reload;
  thread-agnostic requirements; shared system/cloud pre-load policy.
  Every concrete cwd/CLI-override/root/fallback binding must preserve
  the same predicates before provider construction. A returned nominated-cwd
  config inventory cannot certify an earlier/different no-cwd context.
  ConfigBuilder None selects process cwd; direct load_config_layers(None) in
  the requirements path means NO project context. They are different APIs.
  Strict startup excludes default-config fallback, but initialize's failed
  no-cwd reload may still select the original startup clone with cached
  non-ChatGPT auth. That clone must preserve every original exclusion; fallback
  does not undo accesses attempted by the failed load. No later response
  corroborates those earlier contexts. Shared policy/cloud source bindings also
  remain explicit. Native entry stays STOP until the nine-row source table,
  exact root/fixture/CLI/machine/guard identities and positive/negative fixtures
  are part of the immutable implementation manifest.

### Task 3: Define only observations available under the bounded contract

- Action: Separate typed inventory, owned process accounting/signal and known
  scratch metadata from unobserved file/keyring/DNS/PAC/attempt/egress events.
  Specify admission, scratch baseline, owned startup, four-RPC sequence,
  cleanup, after-state and sanitized terminal record.
- Mirror: Existing held handles, stream bounds and safe typed categories.
- Validate: No raw returned config/path/error/account dump. Unknown scratch
  entries are metadata-only unsupported effects; do not open/hash contents or
  recurse/delete blindly. No monitor installation, admin/ETW/firewall session,
  broad owner-home discovery, or new dependency.

### Task 4: Make the readiness decision before any future source work

- Action: Bind the source checklist and observation manifest into a decision
  table. If secret closure is conditional, preserve every predicate as a later
  admission guard. Any unclosed reachable provider-secret route keeps native
  entry STOP regardless of accepted normal OS reads/network-observation gaps.
- Mirror: Preparation plan Task2 and Task8; no automatic fallback or retry.
- Validate: Native admission also requires fresh approved H0, a whole-lifetime
  held image tuple, exact actual-layer/version derivation and a prior approved
  native scratch/artifact manifest. Holding/comparing an inspected image object
  alone does not bind the executable selected by CreateProcessW's pathname.
  The later reviewed design must address pathname/ancestor substitution between
  inspection and creation, without prescribing a new architecture here.
  Fake v-* versions and Root/machine are not
  expected native values. Signature/tarball/source equivalence remains a named
  provenance gap requiring explicit acceptance or closure later.

### Task 5: Review the frozen readiness contract

- Action: Independent native GPT skeptic reviews the two source reports; main
  re-reads artifacts and checks references/hashes. Assemble a self-contained
  immutable packet with at most five named evidence labels; actual installed
  Codex CLI reviews the PLAN, with no tools, repository traversal or code edits
  requested. Record model/provider/effort/exit/raw/input hashes and findings.
- Mirror: plan-review outsider intent/simpler-alternative/assumptions/gaps/verdict
  workflow and prior successful global task launcher.
- Validate: Inspect recorded output for actual verdict and action markers;
  failed/incomplete run is not replaced by native GPT. Dispositions must remain
  explicit. A PROCEED verdict is about this readiness contract, never native
  authorization or evidence that unknown runtime events did not occur.

### Task 6: Publish the accurate checkpoint

- Action: Update status/memory/changelog from the measured source/review outcome.
  Keep previous110/110 proof labelled historical; do not run tests again for a
  documentation-only task or infer native compatibility from them.
- Validate: Re-read all authored artifacts, relative links/citation anchors,
  application-source hashes unchanged, tracked Mac diff empty and no commit/push.
  Surface remaining work as exact future gates rather than a generic request
  to run or automatic implementation dispatch.

## Timeout and recovery contract for a later amendment

The smallest bounded proposal may explicitly choose cooperative30s RPC
cancellation and sequential existing2s cleanup waits. These are per-stage soft
budgets, not an aggregate2s or hard wall-clock guarantee. Synchronous metadata,
filesystem and process creation may block, and the terminal record may not
arrive. A later owner-reviewed amendment must disclose that choice and its
recovery limitations. It does not inherit approval to kill unrelated processes,
change network policy or retry the invocation.

If a hard deadline is chosen instead, that separate design must name the keeper,
retained exact owned handles, termination authority and missing-terminal-record
semantics. No outer watchdog is automatically required or implemented here.
Never delete scratch until the exact retained primary process is signaled;
capability/identity/signal/unexpected-entry failure retains nominated private
scratch and reports inconclusive cleanup, not success.

## Readiness decision

**The selected source investigation and documentation scope is complete; current native admission is STOP.**
The [source packet](../validation/2026-10-06-windows-h2-secret-path-closure.md)
verified14 source hashes (13 initial plus application_network.rs for the delta).
The source author compared the in-memory notify8.2.0 archive digest with its
pinned lock checksum; lead rehashed the extracted sources, not a cached archive. The
[observation manifest](../validation/2026-10-06-windows-h2-observation-manifest.md)
separates available future evidence from unobserved access/network classes.

| Gate | Current evidence | Decision and exact next requirement |
|---|---|---|
| Inspected provider-secret routes | Conditional source closure for the four-handler tuple; no additional specific live secret read identified as unclosed under the stated predicates | Preserve all predicates; a newly identified reachable secret route must be named and excluded before entry. This is not whole-binary/runtime absence proof |
| Bootstrap and every reload context | Supported bundled disable bool is known; existing fake policy omits it; internal read_account(None) reload/fallback has a distinct context | STOP until startup, each RPC reload, internal no-cwd reload and startup-config fallback are independently bound to permitted synthetic context and the same exclusion predicates. config/read(cwd=root) cannot corroborate other contexts |
| Git ancestry | Two discovery helpers independently inspect .git/HEAD; current fake fixture lacks it | STOP until regular pinned synthetic .git/HEAD boundary and missing/unreadable/reparse negative admission cases are designed |
| Native layer and scratch expectations | Fake Root/machine path, v-* versions and fixed manifest are unsuitable | STOP until actual system/fallback identity, source-derived layer fingerprints and exact permitted native artifact/mutation classes are saved before launch |
| Image and primary identity | Source/installed-hash association is known only within disclosed provenance limits; current pins end at method return and fake primary helper reopens PID | STOP until whole-lifetime held image, exact owned primary acquisition/recovery and inspected-image-object versus CreateProcessW pathname/ancestor selection are explicitly bound and tested under a separately reviewed plan |
| Machine prerequisites | H0 old snapshot metadata-clear only | STOP until a fresh separately approved metadata preflight for the exact native manifest; no owner contents or automatic H0 replay |
| Runtime access/network coverage | File/keyring/DNS/PAC/attempt/egress observers absent | Keep UNKNOWN/inconclusive; permitted ordinary OS effects do not waive secret-route predicates. C1/C5 remain blocked |
| Code and run authorization | No native route exists; current approval is read-only planning | Next concrete implementation plan must enumerate source files and all fixture/validator/cleanup/negative-test pairs, receive actual CLI review and owner plan/dispatch approval. Native run approval is separate |

Auth-change/notification consumers and exact shipped-build features remain
finite-source/provenance coverage limitations, not invented additional live
secret reads. Do not require an impossible exhaustive whole-binary audit to
close this documentation task. Do not waive an actual newly named secret path
because broader observations are inconclusive. Strict C1 remains blocked.

The next useful step is a concrete native-specific fixture/identity/validator
implementation plan, with those guards derived before source dispatch. This
readiness contract does not approve that implementation or a native invocation.

## Verification provenance

Selected supplied primary excerpts corroborate important bundled/host/Git trust/
internal-account/provider branches, while the source auditors trace additional
handler/watcher/config-loader/malformed-skill branches in named pinned files.
The initial CLI packet did not excerpt every one of those producers, including
malformed skills fallback and the second git-discovery helper. Those conclusions
remain scoped auditor assertions for that review; source hashes and independent
native GPT counterchecks are separate evidence. They are not runtime results or
an independent CLI certification of the entire source graph. A delta may add
selected excerpts without expanding the evidence claim to whole-binary proof.
Future sanitized output must preserve 'no client account/auth RPC' together
with initialize's disclosed internal account task; never label it 'no account
work'.

## Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Owner home reached despite env redirects | High | Trace Windows known-folder callers and exact four-RPC callsites; unclosed route stops entry |
| Config echo treated as pre-start guard | High | Require immutable input predicates before launch, then validate export separately |
| Fake manifests become native allowlists | High | Separate source-derived native layer versions/artifact classes and negative tests |
| Missing observers mistaken for safety evidence | High | Access coverage inconclusive, C1/C5 blocked, no zero-egress/no-attempt claim |
| New watchdog architecture introduced without need | Medium | Explicit cooperative versus hard-deadline choice and calibrated bounds |
| Image or PID identity lost between checks | High | Held whole-lifetime image and exact owned primary identity in later design |
| Model review mistaken for runtime approval | High | Review provenance, unchanged source and no native route/run authorization |

## Codex review disposition

Actual first review20261006-121702: fix-then-proceed, 1 Major/2 Minor/1 Info,
CLI0.160.0/gpt-6.1-sol/OpenAI/medium, exit0. Full provenance in
[readiness review](2026-10-06-windows-h2-native-readiness-review.md).

| ID | Accepted amendments before the actual delta (historical) |
|---|---|
| M1 | Nine explicit startup/RPC/internal/shared-policy loader contexts, Builder None versus layer None, strict-startup default fallback exclusion and internal original-startup-clone fallback. Native STOP until exact guards/roots/fixtures are encoded independently |
| m1 | Require an explicit reviewed binding between held inspected image object and CreateProcessW executable pathname/ancestor selection; tuple equality alone insufficient |
| m2 | Completion applies to selected investigation/docs; primary excerpts, auditor assertions, native GPT countercheck and runtime unknowns remain distinct |
| i1 | Future serializer says no CLIENT account/auth RPC while disclosing initialize's internal account task; never no account work |

Actual delta20261006-122505 gave **PROCEED for readiness contract**,
0 new Major/0 Minor/0 Info, CLI0.160.0/gpt-6.1-sol/OpenAI/medium exit0.
M1/m1/i1 closed at readiness-plan level and m2 closed within the stated review
scope (raw1157–1160). Future implementation/admission obligations remain open;
this is not a source plan/dispatch permit or native run approval. Lead read
canonical raw1151–1166 and recorded evidence; no tool-action markers.
CLI did not independently verify14 source hashes or claims outside excerpts;
source readers/lead did those checks separately. This plan's selected research/
documentation scope is complete. Native admission remains STOP until the
explicit future manifest, implementation plan and owner gates are satisfied.

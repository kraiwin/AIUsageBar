# H2 provider-secret path closure — read-only readiness packet

2026-10-06. Scope: owner-authorized source investigation/planning after fake H2
preparation. **No native invocation or run approval is supplied.** No vendor CLI,
version/help, SDK/build, owner config/auth/environment values, account call,
settings or app-source change occurred in this investigation. C1 strict remains
blocked; this is conditional source-route evidence, not whole-process monitoring.

## Binding and coverage

Official source pin: `a956835d020762cb2b570053af06f643a11c0ecc` in openai/codex.
Existing [startup closure](2026-10-06-windows-c1-startup-closure.md),
[host contract](2026-10-06-windows-c1-host-contract.md) and
[isolation study](2026-10-05-windows-c1-isolation-study.md) remain in force.
14 fetched text-file records are in
`C:/tmp/aiusagebar-windows-source-audit/h2-secret-closure-fetched.json`, each with
SHA256/path/official URL. Notify 8.2.0 archive from static.crates.io matched pinned
Cargo.lock SHA256 `4d3d07927151ff8575b7087f245456e549fea62edf0ec4e565a5ee50c8402bc3`;
only its lib.rs/windows.rs source text was extracted. Nothing was built/executed.
Paths below are relative to codex-rs unless explicitly prefixed dependencies/.

## Source-route matrix

| Route | Source fact / conditional effect | Required future predicate; coverage limit |
|---|---|---|
| HostSkillsService construction | `core/src/thread_manager.rs:541–546` creates HostSkillsService. `ext/skills/src/host_service.rs:125–143` initializes empty caches/extra roots and calls bundled install only if bundled_skills_enabled | Do not call this constructor owner-skill scanning. Bundled install is a separate scratch effect |
| Embedded bundled skills | `skills/src/lib.rs:58–99,135–155` uses supplied CODEX_HOME/skills/.system, reads its marker, may remove old tree and write embedded assets | Existing canonical synthetic home, no reparse/hardlinks/unknown content. Prefer separately reviewed `skills.bundled.enabled=false` to exclude installer entirely; it is not present in current fake policy |
| Bundled disable semantics | `config/src/skills_config.rs:29–54,119–135`: bundled.enabled is a supported bool, default true; absent or malformed skill config falls back to true | Future native policy/fixture/parser must explicitly corroborate false before startup and on reload; `features.plugins=false` does not disable bundled install |
| Owner global skill root | `ext/skills/src/host_roots.rs:28–42,94–111` calls dirs::home_dir and adds real home/.agents/skills alongside CODEX_HOME/skills and its system cache when resolving skill roots | This is an actual owner-home route if discovery is invoked. USERPROFILE/HOME redirection does not redirect Windows known folders. Exclude snapshot/watchable/skills/tool/thread requests and plugin callbacks; do not certify that route isolated by environment |
| Skills watcher constructor | `app-server/src/skills_watcher.rs:37–72` constructs FileWatcher, subscriber/event task and supplied-home system-cache path. It does not register watched roots | `register_thread_config:96–135` separately loads plugins and calls watchable_skill_root_paths; no such call in the four read handlers inspected. Runtime extra-root registration remains excluded |
| Windows watcher backend | `file-watcher/src/lib.rs:379–395` creates recommended watcher with empty watched_paths. Notify lib.rs:411 selects Windows ReadDirectoryChangesWatcher. `dependencies/notify-8.2.0/src/windows.rs:93–164,478–489` starts a worker with empty watches; directory CreateFileW at192 and ReadDirectoryChangesW at308 are behind Watch/add_watch | Constructor creates thread/synchronization state, not an OS process or provider-file scan in the inspected path. No Watch registration means those path reads are not selected. This selected dependency/source branch is verified; shipped-binary feature equivalence still residual |
| Global AGENTS provider | `codex-home/src/instructions/mod.rs:33–37` constructor only stores supplied home and empty state. `41–62` load reads AGENTS.override.md/AGENTS.md under that supplied home | Do not confuse registration with load. Synthetic home must contain only nominated absent/empty synthetic instruction files; no owner-home fallback in this provider. No instruction-load call in the inspected four handlers |
| Config-relative home metadata | `core/src/config/mod.rs:3480–3485` obtains home_directory for permission path expansion | Ordinary OS home metadata is distinct from reading credentials. Synthetic permissions/config must exclude external ~/absolute read roots; this is not evidence of owner secret access or of redirected known folders |
| Project/config ancestry | `config/src/loader/mod.rs:1266–1282` discovers marker root AND independently resolves checkout/git trust roots. `git-utils/src/trust.rs:71–129` walks nearest .git ancestors; missing HEAD in a .git directory continues upwards | An H2 marker alone does not stop independent git lookup. Future native fixture needs reviewed regular synthetic .git/HEAD (no gitdir/commondir links) or another complete ancestry exclusion. Current fake fixture lacks this native boundary |
| Config stack/system paths | `config/src/loader/mod.rs:266–283,798–820` loads system layer and uses known-folder ProgramData with fixed-path fallback. Missing layer still produces empty System row | Refresh explicitly approved metadata absence and bind actual known-folder/fallback path before native entry. H0 old snapshot is not permanent; no owner contents are read to repair uncertainty |
| File auth/other credentials | `login/src/auth/manager.rs:1488–1590` loads env/ephemeral/configured store; storage.rs:502–535 selects File vs Keyring/Auto. Workload/PAT/agent branches exist independently | Empty allowlisted environment, explicit File store, no synthetic auth record, no PAT/agent/workload/custom provider credentials. Prior packet maps assertion/HTTP/keyring routes; File selection narrows calls, not capability of the Windows token |
| Remote executor | `exec-server/src/environment.rs:183–224` checks Noise before provider; provider loads synthetic environments.toml or env fallback, then build can start connections | Remove all Noise/remote markers and require persisted environments.toml absent. URL=none alone does not override those routes; local/default behavior and any proposed none flag remain separately reviewed |
| Proxy/PAC/TLS roots | Prior host contract closes Windows known folders, current-user public trust roots and WinHTTP user-proxy/PAC route | Empty proxy/CA/OTEL env excludes inherited inputs but not platform proxy discovery or public roots. Allowed public-root/proxy access and possible unauthenticated attempts must be disclosed, not equated to provider secrets or zero network |

Official references for new facts:
[HostSkillsService](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/ext/skills/src/host_service.rs#L125),
[bundled installation](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/skills/src/lib.rs#L58),
[bundled knob](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/config/src/skills_config.rs#L119),
[global skills roots](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/ext/skills/src/host_roots.rs#L28),
[notify Windows source](https://docs.rs/crate/notify/8.2.0/source/src/windows.rs).

## Bootstrap through the exact inventory read flow

MessageProcessor construction at `app-server/src/message_processor.rs:299–388`
starts local-store/models/workers and creates skill/plugin/MCP managers before
RPC. Keep the full prior model conjunction: base auth=None, default provider,
no command/env_key/bearer/catalog/base URL/custom selection, API discovery=false.
Keep plugins/hooks/code_mode_host false, notify=[], analytics false, all three
OTEL exporters none, migration/compression false. Plugin startup callback cannot
be relied on to remain inert if its outer plugins gate is lost on config reload.

The exact four RPCs (plus initialized notification) have these inspected routes:

1. **initialize**: own name, explicitGatewayOauth=true, experimentalApi=true,
   attestation/extensions absent. `app-server/src/request_processors/initialize_processor.rs:155–213` preserves
   explicit-login policy and avoids the Codex Desktop installation-registration
   branch. Warning notifications at234–250 do not load skills/instructions.
   `app-server/src/message_processor.rs:809–829` then invokes account workspace-routing
   notification and thread connection registration. The latter only inserts
   live connection metadata (`app-server/src/thread_state.rs:368–380`).
2. **config/read**: `app-server/src/request_processors/config_processor.rs:103–131` calls the
   config service and ALSO load_latest_config, then updates supported feature
   fields in the response. The service at119–176 reloads layers/origins; it is
   not a frozen startup snapshot. Neither inspected handler calls skill snapshot
   or watcher-root registration. Preserve every bootstrap predicate on reload.
3. **configRequirements/read**: config_processor.rs:132–145 reads managed
   requirements via service179–191 and maps effective login-method metadata.
   No skill/instruction/MCP-status collection in this handler. It can return a
   normal allowedLoginMethods-only object; null is not guaranteed.
4. **experimentalFeature/list**: catalog_processor.rs:346–411 reloads config
   and maps FEATURES. threadId must be absent/null to avoid loaded-session
   config. It does not enter catalog skills_list handlers at526 onward.

**Implicit initialize account work must be disclosed.**
`app-server/src/request_processors/account_processor/workspace_routing.rs:131–150` spawns an
internal read_account(None) task even though no account RPC is sent. At194–239
it reloads config and checks auth_cached/provider state. The backend
get_accounts_check at296–302 is inside the ChatGPT-auth/account branch; auth=None
and the default no-custom-provider conjunction exclude it in this source flow.
The default provider account_state subcall at `model-provider/src/provider.rs:554–595`
uses cached auth; auth=None yields no account. Gateway/AWS alternative admission
at369–391 must remain excluded by the default-provider/no-custom-auth contract.
It does not introduce a new credential loader in the inspected default branch.
Do not state that initialize invokes no account-related code. This conditional
exclusion is source evidence, not observation of actual secret/network access.

ThreadManager starts with no loaded threads. Direct stdio is not the managed
daemon: `app-server/src/lib.rs:786–787,1006–1025` gates saved daemon recovery on
UnixSocket plus managed_daemon. No thread/turn/resume/fork/tool/skills/AGENTS/MCP
status RPC is permitted in the inventory flow. Extending that whitelist requires
new route review; current source closure is not transferable to those calls.

## Explicit reload-context binding — actual review M1

**New identified unchecked binding, not an observed secret read:** a future
manifest must encode every context below before native entry. A nominated-cwd
config/read response does not validate a no-cwd internal reload. Until these
bindings and fallback invariants have independent fixtures, native admission is
STOP. This is a finite caller/context requirement, not an exhaustive-audit gate.

`core/src/config/mod.rs:1501–1530` resolves ConfigBuilder cwd as harness override,
else fallback_cwd, else actual process current directory, then passes Some(cwd)
to the loader. `app-server/src/config_manager.rs:189–200,429–468` supplies default
harness overrides, fixed manager CODEX_HOME/loader overrides/current CLI overrides
and the caller's fallback_cwd. Thus load_latest_config(None) means **process cwd
with project discovery**, whereas load_config_layers(None) means **no project
context**. These are different APIs; neither is inferred from a later response.

| Caller/context | Pinned producer | Binding/fallback required in the future manifest |
|---|---|---|
| Bootstrap initial config | app-server/src/lib.rs:541–545; config_manager.rs:321–339 | load_startup_config(None) -> process cwd. Explicit ProcessLaunch.WorkingDirectory is the canonical nominated scratch root; exact argv/default harness overrides fixed. strict_config=true returns load failure; default-config fallback excluded |
| Post-cloud-loader initial config | app-server/src/lib.rs:555–573 | load_latest_config(None) -> same process cwd/predicates. strict-config returns error, excluding app-server load_default_config fallback |
| Residency synchronization | app-server/src/lib.rs:587–589; config_manager.rs:176–185 | load_latest_config(None) -> same process cwd. Failure only logs; retained startup config remains the original validated object, not a newly accepted unsafe config |
| config/read layer response | app-server/src/config_manager_service.rs:119–135 | Explicit params.cwd=root -> load_config_layers(Some(root)). Bind canonical nominated cwd, user/system/CLI layers and synthetic git ancestry |
| config/read runtime-feature refresh | app-server/src/request_processors/config_processor.rs:103–131 | Independently load_latest_config(Some(params.cwd)) -> ConfigBuilder nominated root. Both response load AND feature-refresh load preserve exclusions; missing params.cwd would select different APIs/contexts and is unsupported |
| configRequirements/read | app-server/src/config_manager_service.rs:179–191,446–448; config_manager.rs:481–500 | load_thread_agnostic_config -> load_config_layers(None) directly: no project layers. Same manager home, system/requirements, CLI overrides and cloud source. Do not demand project rows from this query or conflate it with process-cwd config |
| experimentalFeature/list | app-server/src/request_processors/catalog_processor.rs:346–374 | threadId absent/null -> load_latest_config(None) -> process cwd. Loaded-session route excluded, not asserted equivalent to config/read(root) |
| Initialize's implicit workspace routing | app-server/src/request_processors/account_processor/workspace_routing.rs:131–150,198–231 | read_account(None) -> load_latest_config(None) -> process cwd. If reload fails AND cached auth is not ChatGPT, uses self.config startup clone. This fallback exists even with strict-config. Clone must retain every startup exclusion; auth=None and no custom provider must remain true before provider creation |
| Shared policy/cloud pre-load | app-server/src/application_network.rs:32–96; config_manager.rs:429,481 | Local application requirements load uses fixed loader overrides, not an RPC cwd; cloud loader is the current manager loader. Same actual system/fallback identity/absence and auth=None cloud exclusion must hold on every call |

The manifest must bind actual process cwd independently of RPC params, exact
CLI overrides/strict flag/manager home, synthetic ancestry and all policy sources.
No cwd-changing instruction is sent by this whitelist; that fact alone does not
measure the runtime process cwd. Keep the process-working-directory binding and
selected source context explicit. Repeated loads can race synthetic/system source
changes; a returned snapshot or H0 absence is not an immutable namespace guarantee.

For the internal fallback, strict startup establishes the original configuration
object; the later clone preserves that object's provider/policy values rather
than loading default settings. Its use is conditionally compatible only while
cached auth remains None and those original guards are unchanged. A failed
no-cwd reload still may have traversed inputs before falling back; fallback does
not undo accesses or grant permission to accept unexpected machine/project input.
The concrete native contract must reject unexpected context/source changes and
label missing observations unknown.

Positive/negative preparation fixtures must distinguish explicit Some(root),
process-cwd None and thread-agnostic None, exercise the strict-startup error path
and internal startup-clone fallback, and mutate guards/ancestry across contexts.
No client account/auth RPC is allowed; internal account-related execution above
remains disclosed in schema/output. These are future binding obligations, not
claims that current fake110 fixtures already verify this native call graph.

## Actionable next-plan changes and remaining gates

The remaining native plan can now name the bundled installer, global skill
discovery, watcher backend and implicit workspace-routing branches precisely.
It should propose explicit skills.bundled.enabled=false with source/wire fixtures,
and a regular synthetic git/HEAD boundary for all discovery paths. These are
proposals only; no policy/source implementation was changed by this report.

For the git boundary, the inspected find_git_checkout_root at
`config/src/loader/mod.rs:1542–1565` and trust resolver at
`git-utils/src/trust.rs:105–129` stop at a .git directory with successful HEAD
metadata, returning the nominated scratch root without reading a gitdir target.
These helpers do not require a complete Git repository and do not invoke a git
executable. The future fixture should use a regular non-reparse .git directory
and regular single-link synthetic HEAD; absence/unreadability causes upward
continuation and must be a negative pre-entry case. This closes these helpers
only, not every possible git utility in other startup branches.

Current fake H2 Context is **not a native-ready fixture**: its exact allowed
file manifest cannot tolerate legitimate native SQLite/log/cache changes, its
System file path is synthetic rather than actual ProgramData, its v-* versions
are literals rather than native layer fingerprints, and its ancestry fixture
does not close independent git lookup. Fake110/110 preparation evidence, where
recorded by lead, cannot establish any native path or no-secret assertion.

Before requesting native run, the lead must close or explicitly block:

- A new immutable native-specific argv/environment/root/layer/allowed-scratch-
  mutation manifest, including reload paths, bundled knob and synthetic git;
  metadata/image pin/provenance and actual System/fallback identity.
- Every known provider-secret entry under File/auth-none/default provider and
  absent remote/Noise inputs; unexpected managed/project policy blocks entry.
- The inspected four-RPC routes are conditionally closed for the named secret
  entries under the stated predicates; no additional specific live provider-
  secret read was identified as unclosed in those handlers. This does not add
  an invented exhaustive whole-binary-audit prerequisite. The actual entry
  blockers are the concrete fixture/policy/image/machine/reload bindings above,
  including the newly enumerated internal None-context/startup-clone obligation.
  If preparation of that immutable tuple reveals a NEW reachable secret route,
  name its callsite and exclusion before entry; accepted public OS uncertainty
  does not waive that specific route.
- Observation classes: returned inventory fields/job/scratch evidence are bounded;
  absent keyring/file/DNS/PAC/network-attempt monitors stay unknown. This report
  does not install monitors or certify no access/no attempts/no egress.

Residual coverage uncertainty is separate from a known live secret path:
`app-server/src/message_processor.rs:298` registers user-verification auth-change
watching, and additional transitive consumers outside the four-handler trace
were not exhaustively enumerated here. This report does not identify a new
provider-store read from that registration under an unchanged auth=None state;
it must not invent one from the watcher name. Whole-binary/notification-consumer
coverage is therefore a disclosed source coverage limit, not a named new STOP
on its own. Static source is not whole-process monitoring or shipped-feature
proof. The four-handler tuple, unchanged auth state and exact policy must remain
in the concrete review; widening it requires new route evidence.

No arbitrary owner-profile file access is authorized by ordinary OS public-root
acceptance. Conditional exclusion of inspected routes is not whole-binary proof,
and static dependency source does not prove the exact shipped build features.
H2 native implementation/run still need a reviewed concrete amendment and owner
approval; C1 strict cannot be marked passed by a bounded host inventory result.

## Self-gate

Re-read this report against fetched/pinned callsites and prior packets. Verified
manifest SHA256 values and cited local source path/line anchors; checked local
report links. No executable artifact was produced and no runtime test occurred.
Source facts, conditional exclusions, new proposals and remaining gates are
separate; no exhaustive audit-complete/native-ready claim is made.

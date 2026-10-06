# C1 isolation study — source evidence and bounded next step

Date: 2026-10-05. Owner authorized read-only team research. **C1 remains blocked;
no native runtime pass and no complete transitive audit is claimed.** No Codex or
Claude invocation, owner account/config inspection, installation, OS/settings
change, source implementation, commit or push occurred in this study.

## Source binding and evidence scope

Source is official `openai/codex`, commit
`a956835d020762cb2b570053af06f643a11c0ecc` (previous audit resolved tag
`rust-v0.160.0`). Existing text and tree inventory were reused from
`C:/tmp/aiusagebar-windows-source-audit`; 33 additional source files are recorded
with SHA256 in `c1-study-fetched.json` there. Only raw source text was downloaded;
no reference scripts or builds ran. One guessed `app-server/src/config_api.rs`
fetch returned 404; tree inventory corrected the route to
`request_processors/config_processor.rs`. The failed path is not evidence.

The previous [C1 audit](2026-10-05-windows-codex-audit.md) establishes the host
known-folder blocker. This study expands the effect map; it does not erase that
blocker or complete every dependency path. All paths below are relative to
`codex-rs/` at the pinned commit. `Verified` means read in source, not observed
on this PC. `Possible` means a conditional source route; no request was observed.

## Bootstrap and RPC effect map

| Boundary | Verified source evidence | Isolation consequence |
|---|---|---|
| CLI entry | `cli/src/main.rs:1266` passes default LoaderOverrides into app-server | No audited production CLI override for both machine config layers established |
| Machine configuration | `config/src/loader/mod.rs:266,789–816`: default Windows system namespace uses SHGetKnownFolderPath(FOLDERID_ProgramData), falls back to C:/ProgramData, then OpenAI/Codex config/requirements | CODEX_HOME, USERPROFILE and a ProgramData environment override do not redirect this API; a second host user still shares machine data |
| User home fallback | `utils/home-dir/src/lib.rs:13–60`: nonempty CODEX_HOME must exist/be a directory and is canonicalized; otherwise dirs::home_dir() plus .codex | Require explicit existing synthetic home. Transitive Windows dirs fallback is not fully audited; changing environment alone is not an OS boundary |
| Pre-RPC configuration/auth | `app-server/src/lib.rs:541–589`: startup config, bootstrap auth, cloud loader, latest config, second auth manager and residency synchronization | An assertion after initialize cannot undo earlier effects; invalid-config default fallback at 570 must be accounted for |
| Auth storage selection | `login/src/auth/storage.rs:502–535`: File selects file storage; Keyring/Auto select keyring paths; Ephemeral selects in-memory storage | File mode narrows this storage factory, not all auth routes |
| Other auth inputs | `login/src/auth/manager.rs:1488–1590`: optional API key branch, ephemeral precedence, access-token environment branch, then configured persistent store; `shared_from_auth_config:2797–2811` also activates workload identity | app-server passes enable_codex_api_key_env=false, but that is not a universal auth-env prohibition. Use an allowlisted synthetic child environment, not inherited owner environment |
| Workload identity | `login/src/auth/workload_identity.rs:127–183,249–263`: process env markers include federation rule and identity-token file; requires absolute assertion path and chooses token endpoint | An inherited environment can refer outside scratch. Assertion file read and token exchange are confirmed below; cannot certify no credential access from CODEX_HOME alone |
| Refresh/authority | `login/src/auth/manager.rs:212–213,1631,1726–1729,2848`: refresh/revoke endpoint routes and refresh URL override; personal-token/agent auth constructors are awaited during load | Possible auth HTTP route and storage update; PAT/agent/exchange routes are now traced below. No claim that every launch refreshes |
| Cloud startup/background | `cloud-config/src/bundle_loader.rs:46–68` starts a task immediately calling get_latest then background refresh. `cloud-config/src/service.rs:183–207` checks auth eligibility, prefers identity-matched valid cache, otherwise fetches; 241 onward handles retries/unauthorized recovery; 342 onward writes cache | Possible HTTP and synthetic-home disk changes before any requested RPC, conditional on auth. Client construction alone is not proof of network; actual service fetch path exists |
| Client policy | `app-server/src/config_manager.rs:140–160` constructs cloud endpoint and restricts its factory to that endpoint; startup config at 319 onward installs local policy before cloud access | Destination restriction is not network denial. This policy must not be substituted for VM boundary network disable |
| Environment startup | `app-server/src/lib.rs:615–630` calls EnvironmentManager::from_env/from_codex_home; `exec-server/src/environment.rs:183–225` prepares/initializes it | Remote bootstrap and platform proxy routes are traced below; no no-spawn/no-network runtime certification yet |
| Telemetry/analytics | `core/src/otel_init.rs:16–95` maps exporter configuration and analytics/default flag to metrics exporter; `app-server/src/lib.rs:633–645,700–704` builds provider, records process start and creates analytics client | Optional configured OTEL network exporters and analytics must be disabled in the synthetic policy and separately observed; SDK DOTNET telemetry env is unrelated |
| Analytics worker | `analytics/src/client.rs:314–322` enables queue unless analytics_enabled=Some(false); worker at 172–225; send path at 933 onward obtains auth/factory and sends eligible batches | initialize is itself tracked (`app-server/src/request_processors/initialize_processor.rs:185`), as are initialized requests at 254–261. Read-only RPC does not imply no analytics |
| Startup disk writes | `app-server/src/lib.rs:655–667` initializes SQLite; corruption recovery at 1370 onward moves damaged DB and reinitializes | Synthetic SQLite/log paths and file-change observation required; app-server is not a filesystem read-only process |
| Config RPC | `app-server/src/request_processors/config_processor.rs:103–145` read/requirements-read delegate to config service; write at 148–177,244–272; reload can clear caches/start migration at 216–329 | Allowlist only exact intended read RPCs. Read service reloads config layers and serializes effective values/origins; broader registry/startup paths remain unclosed; writes excluded |
| Usage RPC | `app-server/src/request_processors/account_processor.rs:1115–1160`: requires backend auth, builds backend client, fetches rate limits and optionally detailed reset credits concurrently | Source-only route: usage retrieval intentionally needs account/network. ALL quota/account/auth RPCs remain prohibited in W2b, even calls expected to fail unauthenticated; separate owner approval required |
| Other account endpoints | `app-server/src/message_processor.rs:1750–1795` includes logout/login/cancel/reset-credit/email alongside account reads | Do not send them. RPC whitelist must exclude mutations, turns/threads, tools, login and email |

Representative official pinned references:
[bootstrap](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/app-server/src/lib.rs#L541),
[auth](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/login/src/auth/manager.rs#L1488),
[cloud task](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/cloud-config/src/bundle_loader.rs#L46),
[usage](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/app-server/src/request_processors/account_processor.rs#L1115),
[analytics](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/analytics/src/client.rs#L314).

## Targeted transitive follow-up completed in this study

The following confirms concrete conditional routes behind earlier bootstrap
calls. These are source observations, not effects observed on this PC.

- PAT: `login/src/auth/personal_access_token.rs:40–51,79–87` awaits metadata
  hydration and sends bearer-authenticated GET to the whoami endpoint. Thus
  an inherited access-token environment value can cause pre-RPC HTTP during
  auth loading even when the app-server API-key branch is disabled.
- Agent identity: `login/src/auth/agent_identity.rs:133–164` loads a record,
  conditionally registers a missing task, and verifies JWT records;
  `agent-identity/src/lib.rs:247–254,315–331` confirms JWKS GET and signed task
  registration POST. Existing complete records need not register again.
  This is another conditional bootstrap network route, not proof of every launch.
- Workload identity: `workload-identity/src/assertion.rs:10–19` opens and reads
  the configured assertion file; `workload-identity/src/exchange.rs:178–196`
  reads that assertion then POSTs the token endpoint. Installation via the
  shared auth manager can resolve external auth before RPC. An allowlisted
  child environment must remove these markers, access tokens and endpoint
  overrides; a synthetic home alone cannot constrain their source paths.
- Environment: `exec-server/src/environment.rs:183–223` prepares from env or
  provider snapshot, then builds the manager. `exec-server/src/environment_bootstrap.rs:41–60`
  explicitly starts remote connections during build, distinct from preparation.
  `exec-server/src/environment_provider.rs:62–64` reads CODEX_EXEC_SERVER_URL. Noise rendezvous,
  persisted provider/environment-registry and concrete transport implementations
  are not exhaustively closed; omit remote/environment markers and use only
  synthetic configuration, then observe startup before RPC in an approved lab.
- Proxy/platform: `http-client/src/outbound_proxy/windows.rs:53,124–142,192`
  resolves Windows system proxy, uses WinHttpGetIEProxyConfigForCurrentUser,
  and invokes WinHttpGetProxyForUrl for PAC/autodetection routes. Those can
  involve platform network activity; the exact Windows API/dependency behavior
  is outside this source-only packet. Proxy environment denial alone is not
  no-egress evidence. `login/src/outbound_proxy.rs:29–40` adds endpoint-restricted
  local bootstrap routing, not an OS isolation boundary.
- TLS files: `http-client/src/custom_ca.rs:66–67,423,530` selects
  CODEX_CA_CERTIFICATE/SSL_CERT_FILE and reads the selected file during transport
  construction. `otel/src/otlp.rs:221` reads explicitly configured TLS files.
  Keep certificate/TLS fixtures synthetic too; these can be filesystem effects
  without an HTTP request being sent.
- OTEL transport: `otel/src/provider.rs:194–205` returns no provider when no
  exporter is enabled; `otel/src/provider.rs:445–528` builds batch gRPC/HTTP log exporters,
  using endpoint/headers/TLS and policy wrapper. Span and metrics exporters
  are additional paths, not proven absent merely by disabling log export.
  All three exporter kinds and analytics must be disabled in the guest policy;
  inherited transport variables and platform trust/proxy behavior still require
  observation. Third-party exporter/backend implementation is not fully audited.
- Config read: `app-server/src/config_manager_service.rs:119–176` reloads
  cwd-sensitive or thread-agnostic layers, applies managed exact requirements,
  and emits effective config/origins/optional layers; requirements-read at
  179–191 reloads thread-agnostic config. This is not a configuration mutation
  endpoint, but loading can traverse previously mapped system/cloud routes.

Remaining unknowns are full startup/plugin/MCP/registry descendants, persisted
executor provider/Noise transports, third-party Windows home/keyring/TLS/exporter
internals and exact release-binary provenance. These do not weaken the concrete
known-folder blocker or justify native execution. They remain items for a future
approved lab plan/source closure checklist, not hidden passing assumptions.

## Viable isolation choices

| Choice | Setup/cost | What it can establish |
|---|---|---|
| Continue offline/fakes and finish source closure | No installation or account use | Existing W1/W2a and stronger prerequisite evidence; no native usage |
| Disposable Win11 x64 guest, fresh local synthetic account | Owner supplies lawful OS image/license, virtualization runtime, CPU/RAM/disk and sanitized transfer; may require separate OS/tool approval | Native bootstrap/RPC behavior in that exact guest, pinned binary and synthetic policy |
| Windows Sandbox | Requires supported edition/features; previous lookup did not find WindowsSandbox.exe, availability still unproven | Potential disposable guest, only after supported configuration and requirements are checked; not selected or launched |
| New host user/scratch environment/dead proxy/job | Lower apparent cost but shared machine namespace and OS/network remain | Does not meet current owner-isolation contract; not recommended |

The VM branch must have networking disabled **at the virtualization boundary**,
no owner shares/profile mount, clipboard/drive/printer/device redirection off,
and no owner login/auth material. Transfer a sanitized source/binary/fixture
bundle only, not the owner workspace/home. Windows Sandbox documents network
and clipboard defaults enabled and warns mapped host folders expose data; even
read-only mapping permits reads. See [Microsoft Sandbox configuration](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-configure-using-wsb-file).
Generic VM controls must be verified for the selected product rather than
assuming Sandbox XML applies to it. No hypervisor has been chosen here.

## Exact future evidence contract

Before native execution, finish the unresolved dependency paths above and bind
an unmodified executable's distribution/version/hash to the pinned source as
far as official provenance permits. Source commit does not prove executable
identity. A modified CLI would certify a different executable.

An owner-approved guest plan must record guest build/architecture, fresh account,
image/reset point, binary hash, allowlisted environment, actual known-folder
resolutions, local-only policy, no owner data, and hypervisor no-network/no-share/
redirection configuration. Guest ProgramData/OpenAI/Codex config.toml and
requirements.toml must be explicitly absent or synthetic fixtures with recorded
hashes; include missing, valid and malformed/fail-closed cases. Synthetic current
directory/project ancestry, user config, auth file, OTEL/analytics, SQLite and
environment registry paths must be included, not silently ignored.

Runtime evidence must cover from process creation through shutdown: file/registry
access scope and mutations, descendant creation/canary (plus positive control),
network attempts and successful egress separately, safe redacted RPC transcripts,
config/read layer inventory/managed policy, exact request allowlist, and
process/handle cleanup. The W2b whitelist permits config/registry only; any
test-only MCP canary requires its separately approved synthetic scope. ALL
quota/account/auth RPCs require separate approval, even expected error cases. Monitoring/tool setup belongs
in the separately approved plan; this report does not authorize installing it.
If observations are missing, report inconclusive rather than pass. Denied network
attempts can coexist with zero egress and must not be called zero network effects.

Guest results cannot certify host known-folder/auth/keyring behavior, owner
credentials, authenticated rate-limit response fields or live usage. Those require
a distinct later authenticated-context decision and evidence; no network or
owner account exception is inferred from guest approval.

## Recommendation and owner choice

The authorized bounded read-only investigation is complete as this source risk
and isolation-choice packet. C1 stays blocked; remaining transitive coverage is
explicit in this report rather than a claim of exhaustive absence. No additional owner
`go` is needed for this report. The next owner decision concerns runtime scope:
continue offline/fake support, or identify an already available disposable
Win11 guest and approve a concrete lab setup/observation plan. Do not create,
install or launch a lab until that scope is agreed. No current authorization
permits account/auth/quota RPCs.

## Self-gate

Re-read report against source and Task 5. Checked cited local source files/line
anchors and additional-source manifest hashes. Checked relative prior-audit link.
Markdown has no executable syntax/build to validate; no runtime tests were run.
Source calls, conditional effects and unresolved transitives are distinguished;
no account/network/native test or host-isolation pass is implied.

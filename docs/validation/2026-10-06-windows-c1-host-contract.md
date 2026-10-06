# C1 host/no-account contract after H0

2026-10-06. Read-only source/distribution research and a proposed future evidence
contract. **C1 remains blocked; H2 is not authorized or executed by this report.**
The [corrected H0](2026-10-06-windows-h0-guard-fix.md) established point-in-time
metadata-clear/absent-by-parent for the machine config namespace. It did not
establish owner-account isolation, no file reads elsewhere, or network absence.
The [prior C1 study](2026-10-05-windows-c1-isolation-study.md) remains applicable.
This host has no available VM and no permission to install tools or change OS,
network features or policy. Those constraints are inputs, not reasons to run a
weaker native test automatically.

## Evidence binding

Official Codex source commit: `a956835d020762cb2b570053af06f643a11c0ecc`, tag
`rust-v0.160.0`. Source-only downloads were added beneath the existing audit root
`C:/tmp/aiusagebar-windows-source-audit`; no upstream build/script ran.
Only the two previously nominated package manifests and executable were read
from the known npm distribution location; there was no broad profile discovery,
environment-value/account/config/auth inspection or provider invocation.

| Distribution observation | Value |
|---|---|
| Wrapper manifest name/version | `@openai/codex`, `0.160.0` |
| Native dependency alias | `@openai/codex-win32-x64` = `npm:@openai/codex@0.160.0-win32-x64` |
| Native manifest name/version | `@openai/codex`, `0.160.0-win32-x64` |
| Wrapper manifest SHA256 | `29c350dfcd8d33749852c16e2f5dcde528409d7e1d3f5d916fe3c576f3914820` |
| Native manifest SHA256 | `1b84c615ffa8e448e7286f99fe263351666219d9acb830f92a71a344e1292a3b` |
| Physical nominated `vendor/x86_64-pc-windows-msvc/bin/codex.exe` SHA256 | `fdda5fa3cf3fb3d000b876720742857676293e4315e4b045fae6f8bd7e866d1d` |

Official [npm version metadata](https://registry.npmjs.org/@openai%2Fcodex/0.160.0-win32-x64)
provides tarball integrity
`sha512-/gCFcuOmGlQkgGivWCtY8BNEDixh70Pue0HZOk9S8bj2vUIE1GLD9zcNB/KqqDJmn0S+lIttauLpzQnw6HOCHA==`.
Its [provenance payload](https://registry.npmjs.org/-/npm/v1/attestations/@openai%2fcodex@0.160.0-win32-x64)
names the same package digest, release tag, audited git commit and
`.github/workflows/rust-release.yml`, with
[release invocation](https://github.com/openai/codex/actions/runs/36897822835/attempts/5).
Payloads were decoded as metadata; attestation signatures/trust were **not**
verified. The native tarball was not downloaded or compared with the installed
EXE. Package/version/source-tag consistency is established; installed-package
integrity, cryptographic provenance verification and reproducible source/binary
equivalence are not. Future H2 must rehash its exact executable and explicitly
accept or close this remaining provenance gap.

## Remaining source routes narrowed

All Codex links below refer to the pinned commit. Dependency source archives
were downloaded from the official crates registry and extracted as text only.
Archive SHA256 matched the corresponding pinned
[Cargo.lock](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/Cargo.lock)
checksums for `dirs 6.0.0`, `dirs-sys 0.5.0`, `keyring 3.6.3`,
`rustls-native-certs 0.8.3`, `rustls-platform-verifier 0.7.0`, and
`opentelemetry-otlp 0.31.0`. This closes selected calls, not every transitive
dependency or feature in the shipped binary.

| Route | Source finding | Host contract consequence |
|---|---|---|
| Persisted executor provider | [environment_toml.rs:222–235,319](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/exec-server/src/environment_toml.rs#L222) reads `CODEX_HOME/environments.toml`; absence falls back to env provider. At 113–193 entries select WebSocket URL/bearer token or a stdio program/args/env/cwd. | An empty synthetic config.toml alone is insufficient. Explicitly absent or synthetic environments.toml is required; configured transports can create network/child effects. |
| Noise precedence | [environment.rs:183–224,616](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/exec-server/src/environment.rs#L183) checks Noise env before provider; build starts connections. [remote.rs:230–250](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/exec-server/src/remote.rs#L230) POSTs `/cloud/environment/{id}/connect` with resolved auth; returned rendezvous data selects WebSocket. [remote/direct.rs:252](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/exec-server/src/remote/direct.rs#L252) constructs connector; retry loops exist. | Remove every remote/Noise marker, not merely URL. Do not describe preparation as connection-free once build runs. Full transport descendant closure remains incomplete. |
| Remote disable flag | [environment_provider.rs:106](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/exec-server/src/environment_provider.rs#L106) treats URL `none` as Disabled/no local, whereas absent URL selects local. | `CODEX_EXEC_SERVER_URL=none` is a possible separately reviewed policy choice; it does not override a persisted TOML provider or Noise precedence and is not a sandbox. |
| Windows home/dirs | [dirs 6.0.0 win.rs](https://docs.rs/crate/dirs/6.0.0/source/src/win.rs) maps home to known_folder_profile; [dirs-sys 0.5.0 lib.rs](https://docs.rs/crate/dirs-sys/0.5.0/source/src/lib.rs) calls SHGetKnownFolderPath with null token for Profile/RoamingAppData/LocalAppData. | USERPROFILE/APPDATA/LOCALAPPDATA replacement is no proof of redirected Windows known folders. Explicit CODEX_HOME avoids its own fallback only; other home-dir callers remain subject to the host token. |
| Keyring | [keyring-store Cargo.toml](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/keyring-store/Cargo.toml) enables windows-native; [store lib.rs:66](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/keyring-store/src/lib.rs#L66) calls Entry/get_password; [keyring 3.6.3 windows.rs](https://docs.rs/crate/keyring/3.6.3/source/src/windows.rs) reads/writes/deletes via CredReadW/CredWriteW/CredDeleteW. | File auth mode narrows the audited auth storage factory, not Windows credential capability of the process nor all secret/MCP/plugin routes. No keyring API was invoked here. |
| Proxy | [outbound_proxy.rs:380,897](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/http-client/src/outbound_proxy.rs#L380) reads upper/lower proxy env; [windows.rs:124](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/http-client/src/outbound_proxy/windows.rs#L124) invokes user IE proxy/PAC APIs. | An empty proxy env or unreachable proxy does not prohibit platform proxy queries, direct fallback or attempted traffic. |
| Native TLS roots | [custom_ca.rs:242–252](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/http-client/src/custom_ca.rs#L242) loads native roots; [rustls-native-certs Windows source](https://docs.rs/crate/rustls-native-certs/0.8.3/source/src/windows.rs) opens current-user ROOT. Its lib.rs supports SSL_CERT_FILE and SSL_CERT_DIR. | No account login is not no current-user platform state. A custom CA adds to native roots on this path; it is not a substitute isolated trust store. |
| Windows verifier | [rustls-platform-verifier 0.7.0 windows.rs](https://docs.rs/crate/rustls-platform-verifier/0.7.0/source/src/verification/windows.rs) uses CertGetCertificateChain and revocation flags; cache-only retrieval flags at 277–293 belong to test/debug fake-root construction. | Do not infer production TLS is offline from dependency tests. Actual selected production branch and Windows retrieval behavior require observation; no network effect observed here. |
| OTEL | [core otel_init.rs:68–91](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/core/src/otel_init.rs#L68) separately selects logs/traces/metrics. [OTLP 0.31.0 exporter source](https://docs.rs/crate/opentelemetry-otlp/0.31.0/source/src/exporter/) reads generic/signal endpoint/header/timeout/compression variables, with HTTP/gRPC transport defaults. | Disable all exporter kinds and analytics in effective config, and exclude inherited OTEL variables. Client construction, worker construction, attempted send and successful egress are distinct evidence categories. |

## Exact synthetic environment proposal

Construct a Unicode child environment from an empty dictionary; never copy the
parent environment and scrub a blacklist afterward. Proposed **only** entries:
`SystemRoot` = approved Windows installation, `WINDIR` = same,
`TEMP`/`TMP` = explicit synthetic scratch, `CODEX_HOME` = existing canonical
synthetic home, and `USERPROFILE`/`APPDATA`/`LOCALAPPDATA` = explicit synthetic
directories. Invoke the nominated EXE by absolute path with explicit synthetic
cwd; omit PATH and COMSPEC unless a later source-grounded requirement identifies
their necessity. Directory redirects are input hygiene, not OS containment.
Any additional variable must be listed, justified and approved in the concrete
H2 command. `CODEX_EXEC_SERVER_URL=none` is optional only after verifying its
compatibility with the intended exact read RPC.

Consequently the following exact names/patterns are absent, including Windows
case variants: `OPENAI_API_KEY`, `CODEX_API_KEY`, `CODEX_ACCESS_TOKEN` (includes
the PAT bootstrap branch), `CODEX_REFRESH_TOKEN_URL_OVERRIDE`,
`CODEX_REVOKE_TOKEN_URL_OVERRIDE`, `CODEX_APP_SERVER_LOGIN_CLIENT_ID`,
`OPENAI_FEDERATION_RULE_ID`, `OPENAI_IDENTITY_TOKEN_FILE`,
`OPENAI_WORKLOAD_IDENTITY_CONTEXT`, `CODEX_EXEC_SERVER_URL` unless approved
`none`, `CODEX_EXEC_SERVER_NOISE_REGISTRY_URL`,
`CODEX_EXEC_SERVER_NOISE_ENVIRONMENT_ID`, `CODEX_EXEC_SERVER_NOISE_AUTH_TOKEN`,
`CODEX_EXEC_SERVER_NOISE_CHATGPT_ACCOUNT_ID`, `NODE_REPL_AUTH_TOKEN`,
`HTTP_PROXY`, `HTTPS_PROXY`, `ALL_PROXY`, `NO_PROXY` and lowercase equivalents,
`CODEX_CA_CERTIFICATE`, `SSL_CERT_FILE`, `SSL_CERT_DIR`, and every `OTEL_*`
variable. This enumeration is an explanation of the allowlist's exclusions,
not a claim that these are every auth/environment variable in all descendants.
[Auth constants](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/login/src/auth/manager.rs#L953),
[workload/Noise names](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/protocol/src/shell_environment.rs#L8).

Synthetic config must explicitly select File credential storage, disable
analytics and logs/traces/metrics exporters, and exclude plugins/MCP/remote
providers/credentials/endpoint overrides. Empty/no synthetic auth records;
environments.toml absent or an explicitly reviewed local-only fixture. Record
effective config and origins, strict-config/failure behavior, synthetic project
ancestry, database/log/state paths and exact fixture hashes. The executable
still runs under the owner's Windows token; none of these inputs removes its
capability to access owner's OS stores.

For the immediate Online model worker, carry the complete conjunction from the
[startup closure](2026-10-06-windows-c1-startup-closure.md): base auth=None AND
default provider AND no custom command auth, provider `env_key`, experimental
bearer token, custom catalog, base URL override or custom provider selection AND
`api_key_model_discovery=false`. Configuration presence can satisfy a provider
API-key predicate even when the corresponding secret environment variable is
absent. Under this conjunction all three should_refresh_models terms are false
and the source worker returns before fetching. No-auth alone is insufficient;
this conditional source conclusion is not runtime no-request evidence.
[Pinned refresh predicates](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/models-manager/src/manager.rs#L540),
[provider predicates](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/model-provider/src/models_endpoint.rs#L218).

Public trust certificates and known-folder/proxy metadata are not provider
secrets. Current-user ROOT enumeration establishes a normal OS trust route,
not a Codex/Claude credential read. A useful host contract can explicitly allow
ordinary OS trust/profile/proxy access while still forbidding provider credential
files/keyring secret calls and every account RPC. The startup study supplies
conditional auth-none/model-worker reasoning above; an exporter or HTTP client
being constructed is not proof that a request is sent. Unexpected
unauthenticated network attempts and scratch writes must remain separately
observable and reportable even if permitted by a narrower experiment.

## Minimum future H2 and limits on this host

The next safe action within current permission is completing source/plan
closure. No automatic H2 follows H0. A future host H2 can establish only a
**bounded observed no-account result** if the owner separately accepts shared
host token, disclosed ordinary OS trust/profile/proxy accesses, possible
unauthenticated early network attempts/scratch writes, and provenance/observation
limitations. Provider credential reads and account RPCs remain forbidden.
That acceptance would change the evidence contract; it must not be recorded as
passing the existing strict owner-isolation C1 threshold.

Before requesting runtime approval, a concrete immutable plan must specify:

1. Exact EXE hash, absolute argv/cwd/environment/config fixtures and named
   Windows OS accesses that are allowed. Refresh H0 metadata immediately under
   the same bounded authorization; do not assume yesterday's absent parent
   stays absent or silently tolerate a newly present host policy.
2. Exact RPC allowlist: initialize, initialized and specifically approved
   config/requirements/registry read operations only. No wildcard `read` rule;
   enumerate methods/params. ALL account/auth/quota RPCs remain excluded even
   expected unauthenticated failures; no turns, tools, login, MCP canary,
   config writes or bridge operations without separate scope.
3. For the existing strict P8 effects threshold, observation from process
   creation through shutdown for file/registry/store
   access, mutations, child creation/attempts, network attempts/DNS/PAC/TLS and
   successful traffic separately, RPC completion and complete handle/job cleanup.
   Include positive controls that demonstrate each monitor captures its claimed
   event class. Post-launch config results or netstat snapshots cannot cover
   bootstrap calls or prove zero attempted connections.
4. A hard external deadline and pre-resume descendant containment, accounting
   and cleanup evidence. Process-count limit one proves enforcement when
   tested; it does not by itself prove no spawn attempts or no network.
5. Fail-closed handling of monitor gaps, unexpected OS-store access, new machine
   policy, auth/remote markers or successful traffic: stop and mark inconclusive
   or failed. Redacted results, no credential contents or raw auth headers.

No existing approved monitor has demonstrated all these event classes, and no
OS/network enforcement installation or policy change is permitted. Therefore
this report cannot provide an execution-ready proof of strict no-owner-access
or zero egress/attempts on the host. Runtime source expectation is not runtime
evidence. Owner approval of a narrower bounded experiment would still need a
named, available observation mechanism and an explicit statement of which
claims stay inconclusive. Continue offline/fakes when those prerequisites are
unavailable; an external already-isolated lab is an option only if supplied and
approved later, not an instruction to install a VM.

A narrower approved inventory can still provide useful bounded evidence:
enumerated synthetic fixture hashes/state before and after, captured exact
RPCs/effective config, job accounting and cleanup for observed processes.
The concrete plan must name only available observation mechanisms and the
paths/event classes they actually cover. Missing file/registry/keyring access
or network-attempt/egress coverage stays **inconclusive**; unchanged fixture
hashes do not prove no reads, and an empty network snapshot does not prove no
attempts. Such an inventory is not a strict P8/C1 pass and needs explicit owner
acceptance of the narrower result. This report does not posit an available
universal monitor or authorize monitoring-tool installation.

## Self-gate

Re-read this document against the assigned source/distribution/no-account scope;
checked the actual primary source lines behind its new claims. Six official
crate archive hashes match pinned Cargo.lock; package metadata and physical
EXE hash are separately labeled. Markdown contains no executable implementation;
no app build/runtime test was applicable or run. Existing app/source/docs edits
were preserved; only this report was added to the repo. No CLI invocation,
account/config/auth contents, settings/tool installation, model turn, commit or
push occurred. Full transitive closure, cryptographic attestation verification,
installed-tarball comparison and host observation are explicitly unresolved.

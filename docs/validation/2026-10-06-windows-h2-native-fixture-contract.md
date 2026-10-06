# H2 native fixture contract — source-derived inputs and artifact classes

2026-10-06. Read-only design evidence for the native implementation plan. No
native CLI/version/help, owner credential/config/environment content, SDK/build,
installation, settings or app-source edit occurred. This packet specifies
proposals and source expectations; it supplies no native execution approval,
whole-binary equivalence or strict C1 pass.

Pin: openai/codex `a956835d020762cb2b570053af06f643a11c0ecc`. Source cache is
`C:/tmp/aiusagebar-windows-source-audit`. The new packet manifest
`h2-native-fixture-fetched.json` records 22 source text records with SHA256 and
official URL; dotenvy 0.15.7 archive SHA256 matched pinned Cargo.lock
`1aaf95b3e5c8f23aa320147307562d361db0ae0d51242340f558153b4eb2439b`.
No reference code was built or run. Paths below are relative to codex-rs unless
prefixed dependencies/. [Secret/reload closure](2026-10-06-windows-h2-secret-path-closure.md)
and [observation manifest](2026-10-06-windows-h2-observation-manifest.md) remain prerequisites.

## Deterministic layer fingerprints and field origins

`config/src/fingerprint.rs:51–79`: serialize the TOML value to serde_json Value,
recursively sort object keys, preserve array order, serialize compact UTF-8 JSON,
SHA256 those bytes, lowercase hexadecimal with `sha256:` prefix. This is **not
the hash of the TOML file bytes**. `config/src/state.rs:122–143` binds that version
to the layer's resolved config. The loader resolves relative paths before the
entry (`config/src/loader/mod.rs:551–559`); the chosen empty/literal guard layers
avoid dynamic path-valued TOML fields. CLI dotted keys are built into nested
tables in input order (`config/src/overrides.rs:10–15`).

For this known ASCII fixture, no general TOML parser or third-party dependency
is needed in the later test harness: use independently authored JSON golden
bytes and equality/hash tests. Any change to exact argv/guard values invalidates
the golden version; do not hash whatever the native child returns as the oracle.

- Empty TOML user/project or missing system layer -> JSON `{}` ->
  `sha256:44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a`.
- Selected physical .env/user/project TOML fixture bytes are the fixed ASCII
  comment `# AIUsageBar H2 synthetic` followed by exactly one LF (no BOM/CR).
  Length26, file-content SHA256
  `48311959c00e8d77c92643da443861b0c7eda546208de270e420fe97d0fed68c`.
  Comment-only TOML still has the empty-table layer version 44136f…;
  file-byte hash and semantic layer version remain separate.

Exact proposed 16-key CLI override set (current 15 guards plus bundled disable)
has this canonical JSON; a separate local standard-library hash calculation
gave `sha256:74550a1497e3dff62050c1942a2c1cac1e49392d9562d472f179942cc83c0278`:

```json
{"analytics":{"enabled":false},"cli_auth_credentials_store":"file","features":{"api_key_model_discovery":false,"background_paginated_rollout_migration":false,"code_mode_host":false,"hooks":false,"local_thread_store_compression":false,"plugins":false},"mcp_servers":{},"model_provider":"openai","model_providers":{},"notify":[],"otel":{"exporter":"none","metrics_exporter":"none","trace_exporter":"none"},"skills":{"bundled":{"enabled":false}}}
```

Use explicit `.git/HEAD` rather than adding project_root_markers to this golden
set; adding that or sqlite/log path overrides requires a new independent golden.
Do not accept a returned native v-* fake version.

Expected config/read(includeLayers=true,cwd=project) high-to-low layers:

| Source tagged identity | Config/version | Status |
|---|---|---|
| sessionFlags | Exact guard map above / 74550a… version | Enabled |
| project(dotCodexFolder=project/.codex) | Empty object / 44136f… version | Default untrusted unless explicit reviewed trust is provided |
| user(file=home/config.toml,profile=null) | Empty object / 44136f… version | Enabled, base default profile only |
| system(file=actual known-folder ProgramData/OpenAI/Codex/config.toml) | Empty object / 44136f… version | Expected even if file absent; bind fresh absence/fallback identity |

`app-server/src/config_manager_service.rs:156–175` returns all layers high-to-low,
including disabled rows, but filters packagedDefaults. `config/src/loader/mod.rs:
266–283,589–608` pushes the system row and maps NotFound to empty table. Missing
file presence is not proved by a row, and another actual system path/version
is not interchangeable with the fake fixture's machine path.

Default project disabledReason is source-derived, not `synthetic-untrusted`:
`config/src/loader/mod.rs:1086–1104` returns
`To load project-local config, hooks, and exec policies, add {trust_key} as a trusted project in {user_config_file}.`
With an explicitly untrusted trust entry it uses a different literal. Windows
trust key is canonical path spelling followed by ASCII lowercasing
(`config/src/loader/mod.rs:1370–1400`); record the exact canonical project and user-file values
before launch. Unsupported spelling/normalization must stop rather than learn
the child's reason as the expected one.

New config export `skills.bundled.enabled` is a boolean leaf; its origin key is
exactly `skills.bundled.enabled`, associated with enabled SessionFlags/version.
`config/src/skills_config.rs:29–54,119–135` supports it, default true; malformed
or absent skill config can fall back true. It is not a feature-registry row.
`config_toml.rs` serializes SkillsConfig and API Config flattens additional
fields; require the explicit returned false predicate and its leaf origin.
Generic defaults/nulls cannot stand in for that evidence. Empty notify/maps have
no leaf origin. Requirements can filter scalar origins; keep the prior exact-
requirement reconciliation and nullable/login-method-only object cases.
[Fingerprint source](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/config/src/fingerprint.rs#L51).

## Minimum immutable synthetic inputs and all reload contexts

Proposed layout under the separately reviewed run/private root:
home/config.toml and project/.codex/config.toml contain the selected nonempty
comment-only bytes, project/.git/HEAD is a regular single-link fixed synthetic file, and regular non-reparse .git/.codex
directories. Also preseed a regular `.h2-directory-pin` in BOTH
home/tmp/arg0 and the nominated sibling temp directory, using the same26comment
bytes/hash483119… and held read/data-write/delete-denial input handles. Each
previously empty leaf then has a held next component. Initial alias-parent state
is only that sentinel, with no prior alias/other directories or files.
`git-utils/src/trust.rs:105–129` and
`config/src/loader/mod.rs:1542–1565` stop when a .git directory has successful
HEAD metadata. A complete Git repository is unnecessary for these helpers;
no git executable is invoked there. Missing/unreadable HEAD continues ancestor
search and is a negative pre-entry case. No gitdir/commondir/reparse indirection.

No project_root_markers override is needed for these default .git marker paths.
Keep home separate from project: the loader otherwise skips a .codex folder
equal to CODEX_HOME (`config/src/loader/mod.rs:1680–1687`). Hold known input handles/ancestor pins
under the reviewed immutable-file contract; input-byte hashes and filesystem
identity are separate measurements.

The **nine contexts** in the companion source closure must remain explicit:
initial bootstrap None, post-cloud None, residency None, config/read layers
Some(project), config/read runtime refresh Some(project), requirements direct
layer None/no-project, feature-list None/process-cwd, initialize's implicit
workspace-routing None/process-cwd with startup-clone fallback, and shared
local-policy/cloud pre-load. Bind OS process cwd=project independently of RPC
params, manager home=home and exact CLI guards in each context. Strict startup
rejects default fallback, but the later internal !cachedChatGPT startup-clone
fallback still exists. These are known bindings, not a no-account RPC statement:
no CLIENT account/auth/quota RPC is permitted; internal account work is disclosed.

Requirements/read has no project context. `app-server/src/config_manager_service.rs:
179–191,446–448` returns None when composed requirements are empty; the API mapper
at `app-server/src/request_processors/config_processor.rs:391–411` returns
requirements:null only when requirements are absent AND effective allowed login
methods equal [api,chatgpt]. Otherwise it emits an object with allowedLoginMethods.
File auth storage alone is not a login-method restriction. Future literal
fixtures must cover null and the normal allowedLoginMethods-only object, including
absent/null optional fields; unexpected nonnull managed constraints stop the
minimal native tuple rather than being learned from output.

## Earlier CLI dotenv and alias effects

`arg0/src/lib.rs:162–175` loads dotenv before alias setup/app-server startup.
`305–325` opens CODEX_HOME/.env and imports non-CODEX variables. An empty inherited
environment is not enough. Select a preseeded **nonempty comment-only synthetic
.env**, exact26bytes/hash483119… above, with held read/share-read handles denying
data-write/delete throughout startup/run. This supersedes the earlier zero-byte
EOF proposal; unrelated nonempty contents are not accepted. Dotenvy
`dependencies/dotenvy-0.15.7/src/lib.rs:130–131` opens only the supplied path;
`dependencies/dotenvy-0.15.7/src/iter.rs:146–147` turns a comment line into an empty parsed line and
`dependencies/dotenvy-0.15.7/src/parse.rs:42–47` returns None for empty/comment.
The iterator then reaches EOF: zero imported keys. There is no parent .env
search on this API. Malformed-line errors are skipped by flatten, while other
valid lines could still import values; exact-byte mismatch stops, never infer
zero imports merely from parse errors.

Two explicit alias-profile choices exist; the native plan must choose one:

1. **Narrow source guard proposal:** TEMP/TMP=run/private with home below it.
   `arg0/src/lib.rs:347–358`, in !debug_assertions builds, rejects alias setup
   if home.starts_with(actual std::env::temp_dir()). `194–195` warns and proceeds.
   This is a source conditional, not proof the nominated binary's std temp path
   or build mode matches. Actual std/platform temp selection and source-build
   correspondence are residuals; unexpected alias artifacts are metadata-only
   unsupported effects, never retroactively added to the manifest.
2. **Explicit alias class proposal:** allow home/tmp/arg0 plus at most one newly
   generated codex-arg0-prefixed directory with exactly .lock, apply_patch.bat
   and applypatch.bat (source21–25,361–418). Pre-entry home/tmp/arg0 must contain
   ONLY our held regular .h2-directory-pin, no prior aliases/other entries.
   Janitor494–539 has **NO name-prefix filter**: it skips entries for which
   path.is_dir() is false; ANY directory can reach .lock open and deletion.
   Our non-reparse regular data-file sentinel is skipped by type before that
   open/delete branch, not by its name. Missing/extra/wrong-type prior entry
   stops before invocation; do not assume the janitor is limited to codex-arg0.
   Child self-PATH
   can prepend aliases/package path even when parent PATH is absent; this is a
   known internal change, not inherited-environment contamination. Four-RPC
   whitelist must not execute these helpers. Pinned tempfile3.27.0 naming
   source closes the generated component: lib.rs:195,230 has six random chars
   and util.rs:7–18 uses alphanumeric characters, with no suffix. Match exactly
   `codex-arg0[A-Za-z0-9]{6}`, not a broad codex-* wildcard. Source archive SHA256
   matched Cargo.lock `32497e9a4c7b38532efcdebeef879707aa9f794296a4f0244f6f69e9bc8574bd`.

Alias generation failure can leave partial scratch state before warning/continue;
normal TempDir guard removal can erase artifacts before after-snapshot. Therefore
absence after shutdown cannot prove no alias writes occurred. Do not hash/open
new .bat contents or infer them safe by extension. Their producer is source-
known; their observation remains metadata-only under the selected profile.
[CLI bootstrap source](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/arg0/src/lib.rs#L162).

## Source-known app-server output classes (derive before launch)

`app-server/src/lib.rs:655,735,777` initializes state/log storage and installation
ID before RPC. `state/src/runtime.rs:130–204` opens and migrates five databases.
`state/src/sqlite.rs:28–32,310–351` names them and selects WAL. Recovery source
`state/src/runtime/recovery.rs:201–210` names database sidecars -wal/-shm.
Default sqlite home is CODEX_HOME when config/env override absent
(`core/src/config/mod.rs:4076–4093`); inherited CODEX_SQLITE_HOME is excluded.

| Predetermined class/path under home | Source condition | Inspection contract |
|---|---|---|
| state_5.sqlite, logs_2.sqlite, goals_1.sqlite, memories_1.sqlite, queue_1.sqlite | Initial state runtime opens each; migrations/backfill/log writes may change bytes | Regular non-reparse single-link metadata only; NO database/query/content hash |
| Each of those five names plus -wal, -shm and -journal | Sidecars may be transient/present/absent; SQLx leaves initial journal mode unset and auto-vacuum setup precedes explicit WAL | Exact fixed names/classes; no wildcard arbitrary sqlite file |
| installation_id | installation_id.rs:17–60 reads/creates/rewrites UUID | Producer-owned mutable metadata only, no identifier content; preseed option has special writer-compatible pin requirements below |
| .sqlite-maintenance.lock | runtime/reclamation.rs:17,37–66,109–121 worker first waits60s before attempting ownership | Optional conditional exact name; do not claim immediate creation or zero maintenance from a cooperative30s budget |
| home/tmp/arg0 classes | Earlier alias profile above, only if explicitly selected | Profile-specific predeclared metadata; never new allowlist learned from run |

Installation_id always opens with read+WRITE+create even when a valid preseeded
UUID exists (`core/src/installation_id.rs:24–36`). A deny-write input pin would
break bootstrap. Prefer generated opaque mutable metadata-only output; if the
plan chooses a known synthetic UUID, a read/share-read+WRITE/no-delete handle
must be explicitly distinct from immutable config/.env/HEAD pins. Any optional
post-signal own-fixture byte/hash check is not an immutable-across-run claim.

Pinned SQLx-sqlite0.9.0 options source179–183 does not set journal_mode unless
requested; archive SHA256 matches Cargo.lock
`488e99c397a62007e4229aec669a179816339afc6d2620ca6fa420dbee2e982c`.
State open setup sets auto_vacuum before its WAL pragma. SQLite documents the
rollback-journal name as database path plus -journal, motivating that exact
optional class **before** the run, not after seeing a new file.
[SQLite temporary-file naming](https://www.sqlite.org/tempfiles.html).

Fresh home must have no sessions or archived_sessions trees: startup backfill
at `rollout/src/state_db.rs:103–163` and metadata.rs:263–272 checks those roots
independently of background_paginated_rollout_migration. Absence skips their
rollout scanning. LocalThreadStore constructor `thread-store/src/local/mod.rs:
258–270` only stores WriterLockCoordinator; `rollout/src/writer_lock.rs:35–40`
constructor records the directory without creating thread-writer-locks.

thread_history_1.sqlite and memories_v2_1.sqlite are not permitted to become
accepted just because SQLite supports them: initial block does not open the
former, and runtime.rs:147,274–281 keeps v2 lazy unless pre-existing. Fresh
fixture and no thread/memory RPC exclude those creation branches in this packet.
Any appearance is unsupported metadata pending a new source review.

Corruption recovery can create home/db-backups/dynamic directories and move
databases (`app-server/src/lib.rs:1379–1436`; recovery.rs:155–174). Fresh fixtures
must contain no databases to trigger pre-existing corruption. Any db-backups
appearance is an unsupported effect, not a permit to read or recursively delete
it. Stdio excludes UnixSocket startup locks; managed-daemon recovery/socket
artifacts are excluded by the tuple. App-server inspected logging uses stderr
and the SQLite logs pool, not a blanket license for arbitrary home/log files.
New log/cache/bundled/remote files outside the named classes stop metadata-only.

Proposed finite metadata ceilings are **design rejection bounds, not source
maximum guarantees**: five DBs+fifteen sidecars, optional maintenance lock, one fixed
installation_id; max64 inspected owned entries overall; max64MiB per mutable
named file and256MiB aggregate mutable bytes. If an alias profile is chosen,
declare maxone generated directory/three fixed children, .lock<=4096bytes
and each bat<=65536bytes as explicit DESIGN rejection limits before dispatch. Any too-large/unknown object stops without
content inspection or automatic recovery. The implementation plan may choose
tighter reviewed bounds; no claim that native will pass them without evidence.

## Narrow metadata proof for nonempty comment fixtures

Share-read/deny-data-write/delete does **not** deny WRITE_ATTRIBUTES or freeze
all filesystem metadata. [MS-FSA FSCTL_SET_REPARSE_POINT](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fsa/4aeefef8-92c3-4abc-af7a-a610caf8a165)
accepts FILE_WRITE_DATA OR FILE_WRITE_ATTRIBUTES. Its selected standard-symlink
branch rejects a DataFile with nonzero stream size; its DirectoryFile branch
requires an empty directory. Consequently fixed nonempty .env/config bytes,
with data truncation/write and deletion denied, close the identified zero-size
standard IO_REPARSE_TAG_SYMLINK conversion path. HEAD is already nonempty.
Each relevant ancestor must contain its held next component before this
nonempty-directory argument is credited; do not claim it during empty setup.

Dotenv comment behavior above and [TOML comment semantics](https://toml.io/en/v1.0.0#comment)
preserve import0 and empty-table44136… while avoiding zero-byte inputs. A local
standard-library TOML parse of those26known ASCII bytes returned empty map;
this is an independent fixture check, not native Codex/Rust parser execution.

The held `.h2-directory-pin` markers in arg0 and sibling temp are required
inputs, not mutable native output classes. Their exact name cannot collide
with the source generator codex-arg0 plus six alphanumeric chars. Arg0 janitor's
is_dir guard preserves a regular sentinel; there is no prefix-name guarantee
for arbitrary other entries. The source proof starts only AFTER safe setup
has established regular marker identity/nonempty bytes and the completed held
component chain. Setup itself still has a disclosed race/metadata gate.

This is a **narrow branch proof**. It does not rule out other supported
name-surrogate/reparse tags, metadata ABA, case-sensitivity changes, previously
held attribute authority, setup races or filesystem/filter differences. Before/
after reparse/identity/case queries and finite adversarial tests remain explicit
later gates, not evidence that namespace metadata is immutable. Do not weaken
those checks, infer whole-path stability from share flags, or add OS policies.
No test reparse operation or fixture filesystem mutation was attempted here.

## Safe metadata and no-content boundary

Check only exact predeclared paths/classes one level at a time. Validate actual
parent identity/reparse state before descending; unknown directory => stop, no
recursion. Reject links/reparse/unexpected types before any optional known-input
hashing. Mutable native outputs are never opened for content/hash/query. Known
fixture bytes are authored by us and may be hashed through validated held
handles. Unknown files are retained and described only by safe category/count/
metadata; no raw path/data/exception text, no blind recursive delete. Preserve
final cleanup/signal evidence separately from scratch effects.

This matrix is derived from selected source, not exhaustive SQLite/stdlib/VFS
temporary-file behavior. Unsupported extra effects cause inconclusive/unsupported
inventory; they do not become a permissive post-launch allowlist. Source packet
completion does not certify native file access, account absence, no attempted
network, no egress or C1 strict isolation. Native implementation/run still require
the reviewed immutable plan and owner approval.

## Self-gate

Re-read this contract against fetched source and companion context table. Verified
source manifest hashes, source anchors/local report links and independent ASCII
golden JSON hashes. No runtime validation was performed. Named source artifacts,
conditional branches, design bounds and unresolved platform/build equivalence
are distinguished.

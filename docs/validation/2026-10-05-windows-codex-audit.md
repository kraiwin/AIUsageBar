# Windows Codex C1 source audit — blocked prerequisite

Date: 2026-10-05. Scope: read-only official source, no CLI execution,
account/config reads or native harness authoring. This is partial source evidence,
not a complete bootstrap/auth/network audit or Windows runtime proof.

## Pinned source and corrected storage path

Official `openai/codex` tag `rust-v0.160.0` resolved through the GitHub tree API
to commit `a956835d020762cb2b570053af06f643a11c0ecc`; tree was not truncated.
Downloaded source text only into `C:/tmp/aiusagebar-windows-source-audit/`.
No reference build or scripts were run. Fetch manifest records file SHA256.

The previous 404 was a wrong path: storage is
`codex-rs/login/src/auth/storage.rs`, not `core/src/auth/storage.rs`.
[Pinned storage factory](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/login/src/auth/storage.rs#L502)
selects FileAuthStorage for the file enum, whereas auto/keyring use keyring paths.
[Pinned enum](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/config/src/types.rs#L115)
serializes lowercase `file`; this alone does not certify all app-server auth paths.

## Concrete unresolved isolation boundary

The planned environment redirection does not isolate system configuration:

- [Loader system-layer call](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/config/src/loader/mod.rs#L266)
  resolves and attempts to load system config before user config.
- [Windows path resolver](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/config/src/loader/mod.rs#L789)
  chooses an explicit LoaderOverrides system path if supplied, otherwise
  SHGetKnownFolderPath(FOLDERID_ProgramData), with C:/ProgramData fallback,
  and appends OpenAI/Codex/config.toml or requirements.toml. This is not the
  scratch APPDATA/USERPROFILE/ProgramData environment variable.
- [Direct CLI app-server entry](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/cli/src/main.rs#L1266)
  passes default LoaderOverrides. No audited supported CLI way to redirect both
  system layers before startup has been established in this audit.
- [App-server startup](https://github.com/openai/codex/blob/a956835d020762cb2b570053af06f643a11c0ecc/codex-rs/app-server/src/lib.rs#L522)
  loads bootstrap config/auth, installs a cloud-config loader, then loads latest
  config before RPC handling. Runtime config assertions cannot retroactively
  make bootstrap an isolated operation.

Lead conclusion: **do not release W2b** under the current scratch-only contract.
System configuration/requirements resolution can depend on non-scratch paths;
the existence or contents of actual system files were not inspected. This is a
source-grounded boundary concern, not a claim that owner credentials were read
or that a network call actually happened. Do not remove managed configuration,
change firewall/accounts, or run native Codex to experiment around it.

Remaining audit: complete auth/cloud/client startup/network/telemetry paths,
known-folder and missing-env fallbacks, registry/config RPC side effects,
binary/source provenance limitations, and an approved supported isolation
contract if current prerequisites cannot hold. No passing real-executable audit
record has been created. NativeCompatibility.cs remains deferred. W1/W2a
fake-process/synthetic-shell/private-file work continues independently.

## Self-check

Reread source callsites and this report; tag/commit/path references bind the
downloaded official text. The known-folder/default-loader path is sufficient
to keep the gate closed without pretending the rest of the audit passed.

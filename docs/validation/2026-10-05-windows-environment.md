# Windows planning environment inspection

Date: 2026-10-05, Asia/Bangkok. Repo: `C:/Python/AIUsage`, baseline `ca2eb2b`, `main` tracking `origin/main`.

This is read-only environment/CLI discovery evidence, **not Windows build, account integration, guard proof or bridge validation**.

| Inspection | Result |
|---|---|
| `Get-CimInstance Win32_OperatingSystem` | Windows 11 Pro 10.0.26200, 64-bit |
| CPU architecture | AMD Ryzen 5 9600X, architecture 9 (x64) |
| `dotnet --info`, `dotnet --list-sdks` | Selected SDK 10.0.401; SDKs 10.0.103/300/302/401; Desktop runtime 10.0.12 present |
| `Get-Command codex` | npm PowerShell shim under `%APPDATA%/npm/codex.ps1` |
| `codex --version` | `codex-cli 0.160.0` |
| Native executable discovery | `%APPDATA%/npm/node_modules/@openai/codex/node_modules/@openai/codex-win32-x64/vendor/x86_64-pc-windows-msvc/bin/codex.exe` |
| Direct native `--version` | `codex-cli 0.160.0` |
| Direct native `app-server --help` | `--strict-config`, `--listen stdio://`, `-c` present; command described experimental |
| `Get-Command claude`, `claude --version` | `%USERPROFILE%/.local/bin/claude.exe`; `2.1.289 (Claude Code)` |
| Git / Git Bash | Git 2.55.0.windows.3; `C:/Program Files/Git/bin/bash.exe` exists |
| `Get-Command bash` | `C:/Windows/system32/bash.exe`, WSL launcher; not the Git Bash candidate |
| `wsl --status`, `wsl --list --verbose` | Default WSL2 distro `docker-desktop`, running; only listed distro |
| Selected environment checks | `CODEX_HOME`, `CLAUDE_CONFIG_DIR`, `CLAUDE_CODE_GIT_BASH_PATH` unset in current process; no settings-level assertion |
| Initial `git status --short --branch` | Clean `main...origin/main` |

WSL output was UTF-16 in captured tool output; interpreted the visible distro/version fields only. No distro shell was entered, no distro installed and no Docker state changed.

Read npm launcher script only to understand native executable routing. Version/help calls are not proof of login, correct subscription, native quota RPC, guard/config-layer behavior, process cleanup or the shell used by an actual Claude statusline. No credential/config contents, quota account response, settings mutation, bridge installation or system policy changes were part of inspection.

Recommendation: native Windows 11 x64, C#/.NET 10 WinForms, native Codex/Claude first; WSL postponed because no user development distro was observed. Owner approval remains pending in [the plan](../plans/2026-10-05-windows-mvp-plan.md), which includes current official documentation and required isolated compatibility tests before real account use.

Planning validation: independent native Codex CLI plan review and delta re-review completed; final verdict `proceed for PLAN`, with all initial findings disposed in the plan. This model review used normal CLI context (configured hook status messages observed), so it does not count as isolated quota/startup-guard evidence. Reread written documents, checked local Markdown links/table columns and Git whitespace; Mac source/tests/Config/Xcode diff empty. Windows build/integration gates remain pending.

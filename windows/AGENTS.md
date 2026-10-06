## Live edition — 2026-10-06

Owner อนุมัติ [Windows live plan](../docs/plans/2026-10-06-windows-live.codex-plan.md)
และสั่ง implement โดยใช้ “กติกาสำหรับ Codex” ในแผนแทนขั้นตอนวางแผน/รีวิวของงานนี้
ยกเลิกข้อห้ามรัน Codex/Claude จริง ระดับความระวังเท่า Mac: Codex quota-only
ผ่าน policy ปิด MCP/hooks/plugins/analytics และ kill-on-close job; ห้าม log secret/raw payload
Claude เชื่อมผ่านเมนูยืนยันในแอป สำรอง/คืน statusLine; เทสใช้ temp settings เท่านั้น
ไม่มี installer/autostart/NuGet ภายนอก/แตะ Mac/commit/push โดยไม่ได้รับคำสั่ง
ผล implementation และคำสั่งตรวจจริงอยู่ใน docs/plans/2026-10-06-windows-live.codex-report.md

Historical (superseded by Live edition):

# Windows edition — W1/W2a implementation

## H2 preparation complete — 2026-10-06

Owner approved reviewed docs/plans/2026-10-06-windows-h2-inventory.plan.md and
dispatch (DECISIONS33). c1_host_contract owns ONLY Core/CodexProvider.cs;
c1_study owns ONLY Tests/H2InventoryTests.cs + Tests/Program.cs. Lead owns
integration/build/test runs/docs; host_plan_safety is read-only independent
review. Existing threads reused; no new role-agent metadata asserted.
No native route or Codex/Claude invocation, even version/help; only fake
children/offline tests. Public transport behavior and all Mac/other app code
remain. No dependency/restore/install/accounts/settings/commit/push.
You are not alone; preserve other owners' edits. Self-gate/reread before
lead starts downstream build/test. Four documented SDK controls apply.
Final build0warnings/errors; focused26/26 and full offline110/110,0skips.
Test-only retained primary kernel-signal waits correct observed scratch cleanup
sharing failures; no production cleanup change. Evidence:
`docs/validation/2026-10-06-windows-h2-preparation.md`.
Native H2 still needs a separately reviewed amendment/run approval; do not
interpret preparation success as C1/C5 pass or access/network isolation proof.

## Current C6 dispatch — 2026-10-05

Owner approved docs/plans/2026-10-05-windows-blocker-amendment.plan.md and
continuous sub-agent execution through implementation/tests/review/docs.
For this delta backend-dev owns only Core/PrivateFiles.cs and Core.csproj;
qa-engineer owns only Tests/ClaudeTests.cs; lead owns integration/review/docs.
Both plan/dispatch gates passed. C1/C5 remain blocked with existing strict
thresholds. All native/account/settings/install/dependency/commit/push
restrictions below remain. Preserve other agents' edits; you are not alone.

Read the root AGENTS, docs/DECISIONS, docs/STATUS and
docs/plans/2026-10-05-windows-mvp-plan.md before changes.

Owner confirmed the second dispatch gate with `go` on 2026-10-05.
Current execution scope is W1/W2a offline/fake/synthetic only.

- C#/.NET 10, WinForms, Windows 11 x64. Framework/Win32 only;
  no third-party NuGet, build hooks, downloads, installer or autostart.
- Keep Mac source/tests/Config/Xcode untouched. No commit/push/release.
- Do not invoke real Codex/Claude, even --version. No account/auth/config reads,
  quota calls, model turns, user CLI settings or bridge installation.
- NativeCompatibility.cs/native mode is deferred until the lead completes C1
  version-pinned bootstrap/auth/known-folder/network source audit.
- Production ClaudeSettings.cs, ClaudeInstaller.cs and claude-launcher.sh are
  deferred. Tee experiments belong under the test project and scratch only.
- Tests use explicit temporary roots; capture sink requires an explicit scratch
  output target, never defaults to a user profile. No raw payload/log dump.
- Codex transport fake console children use suspended/detached/Unicode/extended
  startup flags and explicit pipe handles; assign a no-breakaway kill-on-close
  limit-1 job before resuming. Query accounting before closing the job.
- Tee tests compare direct and candidate stdout/stderr/exit/single execution and
  caller EOF timing. Observe every launcher descendant before harness-job
  cleanup; strict 2s cleanup includes nested original shell and original children.
  A failed candidate blocks production bridge; it does not block offline tray.
- Never kill unrelated processes, restart Explorer, change firewall/registry or
  alter real accounts/settings for tests.
- Re-read all written files against requirements, run relevant build/tests,
  inspect the real path and preserve others' edits before claiming complete.
  Lead verifies every agent artifact before downstream execution.

Write ownership: backend-dev owns src/AIUsageBar.Core and src/AIUsageBar.Bridge;
worker owns src/AIUsageBar.Tray and solution/SDK/build/NuGet config/README/ignore;
qa-engineer owns tests/AIUsageBar.Tests; lead owns this file and repo docs.
Do not edit another owner's files without coordinating. You are not alone.

Planned verification from windows/: dotnet build AIUsageBar.Windows.slnx -c
Release; dotnet run --project tests/AIUsageBar.Tests -c Release -- --offline.
No build/test success is implied until evidence exists.

Before every .NET invocation set DOTNET_CLI_TELEMETRY_OPTOUT=1,
DOTNET_GENERATE_ASPNET_CERTIFICATE=false and DOTNET_NOLOGO=1 to avoid optional
SDK first-use telemetry/certificate actions. A prior first build emitted an
automatic development-certificate installation; it was reported, not trusted
or deleted. No web component is required by this app.

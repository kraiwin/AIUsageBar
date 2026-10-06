# AIUsageBar Windows — Codex + Claude live

Windows 11 x64 · C# / .NET 10 / WinForms / Win32 ไม่มี NuGet ภายนอก, installer หรือ autostart
ต้องมี .NET SDK **10.0.401**, Codex ที่ติดตั้งผ่าน npm และล็อกอินไว้แล้ว และ Git Bash
(`C:\Program Files\Git\bin\bash.exe` หรือ `CLAUDE_CODE_GIT_BASH_PATH`) สำหรับคำสั่ง statusLine เดิม

## Build และเปิดใช้งาน

รัน PowerShell จาก `windows/`:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:DOTNET_NOLOGO='1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='1'
dotnet build AIUsageBar.Windows.slnx -c Release
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline
& .\src\AIUsageBar.Tray\bin\Release\net10.0-windows\AIUsageBar.Tray.exe
```

คลิกไอคอนใน notification area (อาจอยู่ใน overflow) เพื่อดูโควตารายสัปดาห์ภาษาไทย
Codex ดึงตอนเปิดแอปและทุก 5 นาที; เมนู **รีเฟรชตอนนี้** ใช้ระยะห่างขั้นต่ำ 60 วินาที
ดึงล้มเหลวแสดง category และระบุข้อมูลล่าสุด; ไม่พบ CLI แสดงข้อความตามจริง

เลือก **เชื่อม Claude Code…** ตรวจคำสั่งเดิมและคำสั่งใหม่ในหน้าต่าง แล้วกด OK
แอปสำรอง statusLine เดิมทั้ง object และเปลี่ยนเฉพาะ statusLine ใน settings.json
ส่งข้อความใน Claude Code หนึ่งข้อความ แล้ว **เปิดเมนู tray ใหม่** เพื่ออ่าน snapshot ทันที
Claude อ่านจากไฟล์ในเครื่องทุกครั้งที่เปิดเมนู ไม่ติด throttle ของ Codex
เลือก **ยกเลิกการเชื่อม Claude Code** เพื่อคืน statusLine เดิมก่อนถอนแอป
หากผู้ใช้แก้คำสั่งเอง แอปจะไม่ทับ settings และจะล้าง backup เมื่อ Restore ถูกเรียก
สถานะ Partial เปิดให้ติดตั้งใหม่ โดยใช้ settings ปัจจุบันเป็นค่าเดิมสำหรับรอบใหม่

Settings ใช้ `CLAUDE_CONFIG_DIR/settings.json` ถ้าตั้งไว้ หรือ `~/.claude/settings.json`
ข้อมูลแอปอยู่ใน `%LOCALAPPDATA%\AIUsageBar`: `claude/`, `claude-bridge.json`, `bridge/`, `codex-cwd/`
เก็บ snapshot เฉพาะ quota ไม่เก็บ raw payload/token; คำสั่งเดิมรันผ่าน Git Bash และ job ฆ่า process ลูกเมื่อจบ
Bridge จำกัด stdin 2 MB/2 วินาที และคำสั่งเดิม 5 วินาที
เก็บ output build ทั้งโฟลเดอร์ไว้ด้วยกัน อย่าคัดลอกเฉพาะ EXE; เลือก **ออก** เพื่อปิดแอป

## ตรวจ Codex จริง

ตั้ง SDK controls ด้านบนก่อนรัน:

```powershell
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --live-codex
```

พิมพ์เฉพาะ `CODEX weekly_remaining=... resets_at=... elapsed_ms=...` หรือ `CODEX error=<category>`
โหมดนี้ใช้บัญชีจริงและแยกจาก `--offline`; ปิด MCP/hooks/plugins/analytics ตาม CodexPolicy เดิม
ผลรอบนี้: Release 0 warning/error; offline **141/141**, 0 failed/0 skipped; Codex live exit 0; tray smoke exit 0
การดูเมนูและเชื่อม/คืนค่า Claude จริงยังรอ owner ตรวจด้วยตาตาม [รายงาน](../docs/plans/2026-10-06-windows-live.codex-report.md)

## Historical — W1/W2a และ experiments

ข้อความด้านล่างเก็บหลักฐานและคำสั่งเดิม ข้อจำกัดที่ระบุว่ายังไม่เปิด live ถูกแทนด้วย
[แผน live ที่อนุมัติ](../docs/plans/2026-10-06-windows-live.codex-plan.md) ส่วน smoke/experiments ยังใช้ได้

# AIUsageBar Windows — ออฟไลน์ W1/W2a

รุ่นพัฒนาสำหรับ Windows 11 x64 ใช้ C# / .NET 10 / WinForms และ Win32
เท่านั้น แยกจากแอป Mac ไม่เพิ่ม NuGet package, installer หรือ autostart

เมนูภาษาไทยแสดง Codex และ Claude เป็น `—` เมื่อยังไม่มีข้อมูล ไม่มี quota
จำลองในโหมดปกติ Actions เชื่อมบัญชีและติดตั้ง bridge ยังปิดอยู่
เมนูข้อมูลไฟล์ Codex อ่าน metadata ของไฟล์ที่เลือกเท่านั้น ไม่เรียก CLI
ไม่อ่าน auth/config และไม่รับรองว่าไฟล์นั้นปลอดภัยหรือเข้ากันได้
การรีเฟรชปัจจุบันประเมินสถานะออฟไลน์ ไม่มีการดึงข้อมูลบัญชี

## Build จาก source

ต้องมี .NET SDK **10.0.401** และ WindowsDesktop targeting/runtime packs
ที่ติดตั้งไว้แล้ว `global.json` ปิด roll-forward และ `NuGet.Config` ล้าง
package sources: dependency ที่ไม่มีในเครื่องจะทำให้ build ล้มเหลว
แทนการดาวน์โหลดจาก NuGet ไม่มี RID หรือ self-contained runtime download

รัน PowerShell จาก `windows/`:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_NOLOGO = '1'
dotnet build AIUsageBar.Windows.slnx -c Release
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline
```

ชุด mandatory offline ล่าสุดหลัง H2 preparation ผ่าน **110/110**,0failed/0skipped
และ Release build0warnings/0errors บน Windows11 x64 นี้. H2 focused26/26
ใช้ fake child/policy/typed inventory fixtures เท่านั้น ไม่มี native route.
ดู [H2 preparation validation](../docs/validation/2026-10-06-windows-h2-preparation.md).
ผล C6 เดิม84/84 และ focused deterministic4/4 เก็บแยกใน
[C6 validation](../docs/validation/2026-10-05-windows-c6-validation.md).

Replay H2 จาก `windows/` ด้วย SDK controls ข้างต้น และเพิ่ม
`DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1`:

```powershell
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build --no-restore --no-launch-profile -- --offline --case h2-inventory/
```

`--fake-h2` เป็น child protocol ภายในชุดทดสอบ ต้องผ่าน synthetic-root/argument
validation; ไม่ใช่คำสั่งใช้งาน native provider. Native H2 ต้องมี amendment,
secret-path closure, observation manifest และ run approval แยก.
ตัวเลขเป็นผลตรวจในขอบเขต offline ไม่ใช่ supported native CLI/account integration.

W2a experiments แยกจาก mandatory suite และคืน nonzero เมื่อ gate ไม่ผ่าน:

```powershell
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --tee-experiment
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --file-experiment
```

Tee สอง candidate มี34passed/9failed/43cases: strict cancellation/EOF/startup
gates ไม่ผ่าน จึง unsupported. File experiment รอบสุดท้าย inconclusive/exit1;
ไม่รับรอง race window หรือ universal CAS. Replay กลุ่มเฉพาะได้ด้วย
`--case <prefix>` หลัง mode. ตัว runner ไม่มี native/account mode.
ดู [หลักฐานและ ledger](../docs/validation/windows-mvp-validation.md).

ตัวแปร environment ข้างต้นจำกัด SDK telemetry/first-use certificate creation
สำหรับ process ที่รันคำสั่ง ไม่แก้ registry หรือ CLI account settings
SDK first-use ใน build แรกของรอบนี้รายงานว่าได้สร้าง ASP.NET development
certificate แล้ว ไม่มีการรัน trust command แอปนี้ไม่ใช้ certificate นั้น

เปิดแอปออฟไลน์เองหลัง build:

```powershell
& .\src\AIUsageBar.Tray\bin\Release\net10.0-windows\AIUsageBar.Tray.exe
```

ไอคอนอาจอยู่ใน overflow ของ notification area คลิกซ้ายหรือขวาเปิดเมนู
เลือก `ออก` เพื่อปิด แอปมี single-instance ต่อ Windows session และไม่มี
taskbar window อย่าคัดลอกเฉพาะ EXE: output เป็น framework-dependent
ต้องเก็บ DLL, deps/runtimeconfig และ referenced assemblies ที่ build มาด้วย

## Smoke แบบชั่วคราว

โหมดนี้เปิด message loop/NotifyIcon จริง ใช้เฉพาะ window ของแอปเอง
เปิดและปิดเมนู รีเฟรชสถานะออฟไลน์ ส่งข้อความ TaskbarCreated ให้ HWND
ของแอป แล้วถอด icon และจบ process อัตโนมัติ ไม่ restart Explorer
มี UI timer จำกัดเวลา 5 วินาที และตัวอย่าง runner รอ process ไม่เกิน
10 วินาที ต้องระบุ directory scratch ที่มีอยู่แล้วและเป็น absolute path:

```powershell
$smokeRoot = Join-Path $env:TEMP ('AIUsageBar-tray-smoke-' + [Guid]::NewGuid())
New-Item -ItemType Directory -Path $smokeRoot | Out-Null
$trayProcess = Start-Process -FilePath .\src\AIUsageBar.Tray\bin\Release\net10.0-windows\AIUsageBar.Tray.exe -ArgumentList @('--smoke', ('"' + $smokeRoot + '"')) -WindowStyle Hidden -PassThru
if (-not $trayProcess.WaitForExit(10000)) { throw 'Tray smoke did not exit within 10 seconds; inspect this owned process.' }
if ($trayProcess.ExitCode -ne 0) { throw ('Tray smoke failed: ' + $trayProcess.ExitCode) }
Get-Content -LiteralPath (Join-Path $smokeRoot 'tray-smoke.json')
```

Exit `2` คือ argument ไม่ถูกต้อง; `3` คือมี instance อยู่แล้ว
JSON มีเฉพาะ boolean/count และคำอธิบายการตรวจ ไม่มี path/account/payload
Smoke ไม่อ่านหรือเขียนข้อมูลบัญชี ไม่เปิด path picker และไม่ทิ้งแอปทำงาน

## หลักฐานและข้อจำกัด

Build/test/runtime proof ใช้ผลจริงที่ lead บันทึกใน
[Windows validation](../docs/validation/) ไม่ถือว่าคำสั่งข้างต้นผ่านโดยอัตโนมัติ
Smoke ตรวจ handler ของ TaskbarCreated ผ่านข้อความจำลองใน window ของเรา
การ restart Explorer จริง, Narrator, high contrast, 100/150/200% DPI,
sleep/wake และการเชื่อมข้อมูลบัญชีจริงยังต้องตรวจแยก

Core รองรับการทดสอบ parser/freshness/throttle/refresh cancellation และ
fake process/snapshot primitives โหมด Tray ยังไม่บันทึก attempt ลง profile
เพราะไม่มี live fetch หรือ approved account storage integration
Native Codex, compatibility import และ production Claude launcher/settings
installer ยังไม่เปิดใช้งาน ตาม [approved plan](../docs/plans/2026-10-05-windows-mvp-plan.md)

ไอคอนรูปมิเตอร์สองแถบเป็น artwork ของโปรเจกต์เอง มีขนาด
16/24/32/48/64/256 pixel ใน `src/AIUsageBar.Tray/Assets/AIUsageBar.ico`
สร้างซ้ำได้ด้วย Python standard library โดยรัน `Assets/generate_icon.py`
ไม่ใช่โลโก้ OpenAI หรือ Anthropic ไม่มี build hook เรียก generator

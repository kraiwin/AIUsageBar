# แผน (Codex): Windows tray ใช้งานได้จริง — Codex + Claude live

สถานะ: APPROVED 2026-10-06 (owner เคาะ "go")
รีวิว: Codex gpt-6-astra/high 2 รอบ — รอบ 1 fix-then-proceed (1 blocker/2 major/2 minor → แก้ครบ), รอบ 2 fix-then-proceed (ปิด 8/9; major ที่เหลือ = การรอท่อ output ของ process ลูก → แก้แล้วใน S1 ข้อ 2 + S1-T10/T11 โดยไม่รีวิวรอบ 3 ตามเพดาน)
ผู้เขียนแผน: Claude (skill plan-to-codex) · ผู้ลงมือ: Codex CLI · วันที่ 2026-10-06

## เป้าหมาย
ทำให้แอป tray บน Windows (`windows/`) แสดง **โควตารายสัปดาห์จริง** ของ Codex และ Claude บนเครื่องนี้ เหมือนแอป Mac
โค้ดส่วนประกอบหลักมีอยู่แล้ว (ตัวรัน process + job object, ตัวคุย JSON-RPC กับ codex, ตัวอ่านโควตา, ที่เก็บ snapshot ของ Claude, หน้า tray) — งานนี้คือ **ต่อเข้าของจริง** ไม่ใช่เขียนใหม่

## การตัดสินใจของ owner (2026-10-06) — ใช้แทนข้อห้ามเดิม
1. **รัน codex CLI จริงกับบัญชีของ owner ได้** ระดับความระวังเท่าแอป Mac: ขอแค่ `account/rateLimits/read`, ปิด MCP server / hooks / plugins / analytics ผ่าน `-c` แบบเดิม (`CodexPolicy.Arguments`), ฆ่า process ลูกด้วย job object เมื่อจบ. ข้อห้าม "ห้ามรัน Codex/Claude จริงแม้แต่ --version" ใน `windows/AGENTS.md` และงานพิสูจน์ C1/C5/H0/H2 **ถูกยกเลิก** สำหรับงานนี้
2. **ต่อ Claude ผ่านเมนูในแอป** — แก้เฉพาะ `statusLine` ใน settings.json ของ Claude Code, เก็บค่าเดิมไว้, มีเมนูยกเลิกที่คืนค่าเดิม
3. ยังคง: ไม่มี installer / autostart / NuGet package ภายนอก / แตะโค้ด Mac / commit / push; ห้ามพิมพ์หรือ log ข้อมูลบัญชี token หรือ payload ดิบ

## นอกขอบเขต (ห้ามทำ)
- ห้ามแก้ไฟล์ Mac (`AIUsageBar/`, `AIUsageBarTests/`, `Config/`, `AIUsageBar.xcodeproj`)
- ห้ามลบ/รื้อโค้ดหรือเทส H2/C6/tee เดิม (ปล่อยไว้ แม้ไม่ได้ใช้)
- ห้ามแก้ `~/.claude/settings.json` ของจริงระหว่างเทส — เทสอัตโนมัติใช้ไฟล์ใน temp dir เท่านั้น (การติดตั้งจริงเป็นขั้นที่ owner กดเองใน S3)
- ห้ามเพิ่มฟีเจอร์ที่ไม่อยู่ในแผน (autostart, หน้าต่างกราฟ, การตั้งค่า, แจ้งเตือน)
- ห้ามทำเอกสาร validation/study/handoff ใหม่ — เอกสารที่แก้ได้มีแค่ใน S4

## Convention ที่ต้องตาม
| เรื่อง | ตัวอย่าง (path:line) | แบบที่ใช้ |
|---|---|---|
| Error | `windows/src/AIUsageBar.Core/Usage.cs:5` | โยน `CoreException("<kebab-category>")` ห้ามใส่ข้อความ/ค่าจากบัญชีใน category |
| รัน process | `windows/src/AIUsageBar.Core/WindowsProcess.cs:8,44` | `WindowsProcess.Start(new ProcessLaunch(exe, args, cwd, env))` — env ต้องส่งเองทั้งชุด |
| JSON-RPC | `windows/src/AIUsageBar.Core/CodexProvider.cs:52-63,133` | `new JsonRpcTransport(process)`; method ที่อนุญาตถูก whitelist ไว้แล้ว |
| ลำดับเรียก codex | `windows/src/AIUsageBar.Core/CodexProvider.cs:14-49` | child แรกอ่าน inventory → child ที่สองปิด MCP แล้วขอโควตา (เหมือน Mac) |
| ไฟล์ส่วนตัว | `windows/src/AIUsageBar.Core/PrivateFiles.cs:17` | โฟลเดอร์ ACL เฉพาะ user+SYSTEM; parent ต้องมีอยู่ก่อน |
| เทส | `windows/tests/AIUsageBar.Tests/UsageTests.cs:9`, `CodexTests.cs:67`, `Program.cs:22-27,59` | sync: `TestCase.Sync("<group>/<name>", () => ...)`; async: `new TestCase("<group>/<name>", async () => ...)` (ไม่มี `TestCase.Async`) + `Check.*`; ลงทะเบียนใน `Cases()` แล้วต่อเข้า `--offline` ใน Program.cs |
| ข้อความ UI | `windows/src/AIUsageBar.Tray/TrayContext.cs:42-60` | ภาษาไทย, `—` เมื่อไม่มีข้อมูล |

## คำสั่ง build/test
รันจาก `windows/` ใน PowerShell (ตั้ง env ก่อนทุกครั้ง):
```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'; $env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'; $env:DOTNET_NOLOGO='1'; $env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='1'
dotnet build AIUsageBar.Windows.slnx -c Release
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline --case <prefix>
```
Baseline ที่ Claude รันก่อนเขียนแผน (2026-10-06): build 0 warning / 0 error; `--offline` → `SUMMARY passed=110 failed=0 skipped=0`

**เกณฑ์ผ่านรวมทุกชิ้น:** build 0 error และ `--offline` failed=0 skipped=0 โดยจำนวน passed = 110 + เคสใหม่ที่เพิ่ม (เทสเดิมห้ามหาย)

## ชิ้นงาน

### S1 — Codex ตัวจริง
- ทำอะไร:
  1. เปลี่ยนชื่อ `FakeCodexProvider` → `CodexProvider` (ทั้ง src และเทสที่อ้างถึง) — logic การเรียก RPC เดิมไม่เปลี่ยน
  2. **อนุญาต process ลูกแบบเลือกได้** (codex จริงและ Git Bash สร้าง process ลูก แต่ job ปัจจุบันจำกัดไว้ 1 ตัว — `WindowsProcess.cs:66`, และ `CodexProvider.cs:170` เช็ค `TotalProcesses != 1`):
     - เพิ่ม `bool AllowDescendants = false` เป็น parameter ตัวท้าย (มีค่าเริ่มต้น) ของ record `ProcessLaunch` และ property อ่านได้บน `WindowsProcess`
     - `AllowDescendants=false` (ค่าเริ่มต้น) = พฤติกรรมเดิมทุกอย่าง (flag `0x2000|0x8`, limit 1)
     - `AllowDescendants=true` = job flag `0x2000` (kill-on-close) **อย่างเดียว** ไม่ใส่ `0x8` และไม่ตั้ง `ActiveProcessLimit`
     - `JsonRpcTransport.FinishAsync`: ถ้า process เป็น `AllowDescendants=true` → ยังเช็ค `exit != 0`, stream failure และ envelope ที่ค้างใน channel เหมือนเดิม แต่**ไม่เช็ค** `TotalProcesses`/`TerminatedProcesses`; และ **การรอ output/error drain (`CodexProvider.cs:167`) ที่หมดเวลา 2 วินาทีเพราะ process ลูกยังถือท่อ stdout/stderr ค้างไว้ ไม่ถือเป็น error** — process หลักจบด้วย exit 0 แล้ว = สำเร็จ ปล่อยให้ `DisposeAsync` ฆ่าทั้ง job (ซึ่งปิดท่อ) ต่อ. กรณี `false` ลำดับและการเช็คเดิมทุกข้อไม่เปลี่ยน
  3. เพิ่ม static class `NativeCodex` ใน Core:
     - `ProcessLaunch Launch(string codexExe, IReadOnlyList<string> arguments, string workingDirectory)` — env = **สำเนา environment ทั้งหมดของ process ปัจจุบัน**, `AllowDescendants = true`
     - `CodexProvider Create(string codexExe, string workingDirectory)` — factory = `WindowsProcess.Start(Launch(...))` แล้วห่อด้วย `new JsonRpcTransport(process)`; `inertPath` = `Path.Combine(Environment.SystemDirectory, "where.exe")`
     - `string? FindExecutable()` — เรียก `CodexDiscovery.Discover(%APPDATA%\npm)` คืน path ตัวแรก หรือ `null` ถ้าไม่พบ
     - working directory = `%LOCALAPPDATA%\AIUsageBar\codex-cwd` (สร้างถ้ายังไม่มี เป็นโฟลเดอร์ว่าง)
  4. เพิ่มโหมด `--live-codex` ในตัวรันเทส (**ไม่**อยู่ใน `--offline`): หา codex → `Create(...)` → `FetchAsync` → พิมพ์บรรทัดเดียว `CODEX weekly_remaining=<0-100 ปัด 0 ตำแหน่ง> resets_at=<ISO-8601 UTC> elapsed_ms=<n>` exit 0; ล้มเหลว → พิมพ์ `CODEX error=<CoreException.Category หรือชื่อ exception type>` exit 1. ห้ามพิมพ์อย่างอื่นจาก payload
- ไฟล์ที่แก้ได้: `windows/src/AIUsageBar.Core/CodexProvider.cs`, `windows/src/AIUsageBar.Core/NativeCodex.cs` (ใหม่), `windows/src/AIUsageBar.Core/WindowsProcess.cs` (เฉพาะ `AllowDescendants`), `windows/tests/AIUsageBar.Tests/CodexTests.cs`, `windows/tests/AIUsageBar.Tests/H2InventoryTests.cs` (เฉพาะ rename), `windows/tests/AIUsageBar.Tests/NativeCodexTests.cs` (ใหม่), `windows/tests/AIUsageBar.Tests/Program.cs`
- เคสเทส (ใน `--offline`):
  | ID | สถานการณ์ / input | ผลที่ต้องได้ |
  |---|---|---|
  | S1-T1 | `Launch("C:\\x\\codex.exe", ["a","b"], "C:\\w")` | `Executable`=`C:\x\codex.exe`, `Arguments` = `["a","b"]` ตรงลำดับ, `WorkingDirectory`=`C:\w` |
  | S1-T2 | ตั้ง env `AIUSAGEBAR_TEST_MARKER=1` ใน process เทส แล้วเรียก `Launch` | `Environment` มี key นั้นค่า `1` และมี `USERPROFILE` เท่ากับของ process ปัจจุบัน |
  | S1-T3 | `Launch` โดย `codexExe` เป็น path ไม่เต็ม (`codex.exe`) | โยน `CoreException` |
  | S1-T4 | `CodexDiscovery.Discover` บน temp npm root ที่ไม่มี package | คืนลิสต์ว่าง (และ `FindExecutable` ที่ชี้ root นั้นคืน `null` — ทำให้ root ส่งเข้าได้แบบ internal เพื่อเทส) |
  | S1-T5 | เทส Codex เดิมทั้งหมดหลัง rename | ผ่านทุกตัว จำนวนเท่าเดิม |
  | S1-T6 | `WindowsProcess.Start` รัน `C:\Windows\System32\cmd.exe /c "ping -n 1 127.0.0.1 >nul"` ด้วย `AllowDescendants=true` (env = สำเนา env ปัจจุบัน) แล้วรอจบ | exit code 0; `TotalProcesses >= 2` (ping ถูกสร้างจริง); `ActiveProcesses == 0`; `DisposeAsync` ไม่โยน |
  | S1-T7 | คำสั่งเดียวกับ T6 แต่ `AllowDescendants=false` | exit code ≠ 0 (สร้าง ping ไม่ได้); `TotalProcesses == 1` — ยืนยันว่าค่าเริ่มต้นยังเข้มเท่าเดิม |
  | S1-T8 | `AllowDescendants=true` รัน `cmd.exe /c "start /b ping -n 30 127.0.0.1 >nul"` (cmd จบแต่ ping ค้าง) แล้ว `DisposeAsync` | ก่อน dispose `ActiveProcesses >= 1`; `DisposeAsync` จบภายใน 3 วินาทีโดยไม่โยน (job ฆ่า ping ทิ้ง) |
  | S1-T9 | `NativeCodex.Launch(...)` | `AllowDescendants == true` |
  | S1-T10 | **ระดับ transport:** `new JsonRpcTransport(WindowsProcess.Start(...))` รัน `cmd.exe /c "start /b ping -n 30 127.0.0.1 >nul & exit 0"` ด้วย `AllowDescendants=true` (ping สืบทอดท่อ stderr ค้างไว้) → เรียก `FinishAsync` แล้ว `DisposeAsync` | `FinishAsync` ไม่โยนและจบภายใน 4 วินาที; `DisposeAsync` ไม่โยนและจบภายใน 3 วินาที |
  | S1-T11 | ระดับ transport, `AllowDescendants=true`, รัน `cmd.exe /c "exit 3"` → `FinishAsync` | โยน `CoreException` category `cleanup-failure` (exit ≠ 0 ยังเป็น error) |
- คำสั่งตรวจ:
  1. `--offline` → failed=0 skipped=0 และ `--offline --case native-codex/` ผ่าน ≥10 เคส
  2. **ของจริง:** `dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --live-codex` → exit 0 และบรรทัด `CODEX weekly_remaining=` มีตัวเลข 0–100
- ถ้าของจริงไม่ผ่าน (เช่น `unsupported-configuration` เพราะ codex จริงตอบ config/feature ต่างจาก fixture) → แก้ได้ตามกติกา 2 รอบ เฉพาะในไฟล์ที่แก้ได้ของ S1 และห้ามผ่อนการปิด MCP/hooks/plugins; ยังไม่ผ่าน → หยุด รายงาน error category จริง

### S2 — Claude bridge (ตัวรับข้อมูล + ติดตั้ง/ยกเลิก)
- ทำอะไร:
  1. **ที่เก็บ:** root ของแอป = `%LOCALAPPDATA%\AIUsageBar` (ส่ง root เข้าได้เพื่อเทส). snapshot ของ Claude = `<root>\claude` (ใช้ `ClaudeSnapshotStore` เดิม), ไฟล์ backup = `<root>\claude-bridge.json`, ตัว bridge ที่ติดตั้ง = `<root>\bridge\`. ทุกจุดที่เปิด `ClaudeSnapshotStore`/`PrivateFiles` (Bridge, Tray, installer) ต้อง `Directory.CreateDirectory(<root>)` ก่อน เพราะ `PrivateFiles` ต้องการ parent ที่มีอยู่แล้ว (`PrivateFiles.cs:24`)
  2. **`ClaudeBridgeInstaller`** (ใหม่ ใน Core) ทำงานกับ path ที่ส่งเข้ามา:
     - `SettingsPath()` = `%CLAUDE_CONFIG_DIR%\settings.json` ถ้าตั้งไว้ ไม่งั้น `%USERPROFILE%\.claude\settings.json`
     - `Preview(settingsPath, root, bridgeSourceDir)` → คืน `(originalCommand หรือ null, installedCommand)` ไม่เขียนอะไร
     - `Install(...)`: อ่าน settings (UTF-8, ≤1 MB, ต้องเป็น JSON object; ไฟล์ไม่มี = object ว่าง) → ถ้า `statusLine.command` เท่ากับ installedCommand อยู่แล้ว = ไม่ทำอะไร → ไม่งั้น: คัดลอกไฟล์ทั้งหมดใน `bridgeSourceDir` ที่ชื่อขึ้นต้น `AIUsageBar.` ไป `<root>\bridge\` → เขียน backup `{"schemaVersion":1,"hadStatusLine":<bool>,"original":<statusLine เดิมทั้ง object หรือ null>,"installedCommand":"..."}` **ก่อน** → แล้วเขียน settings ใหม่ที่เปลี่ยนเฉพาะ `statusLine` = `{"type":"command","command":<installedCommand>}` + คัดลอก key อื่นใน statusLine เดิม (เช่น `padding`) ยกเว้น `type`/`command`. key อื่นทั้งไฟล์คงเดิม (ใช้ `JsonNode`). เขียนแบบ atomic (ไฟล์ temp ในโฟลเดอร์เดียวกัน แล้ว `File.Replace`/`File.Move` overwrite)
     - `installedCommand` = `"<root>/bridge/AIUsageBar.Bridge.exe" statusline` — path ใช้ `/` และครอบด้วย `"`
     - `State(settingsPath, root)` → enum `BridgeState`:
       - `NotInstalled` = ไม่มี backup
       - `Installed` = มี backup **และ** `statusLine.command` ใน settings ตอนนี้ = `installedCommand` ใน backup
       - `Partial` = มี backup แต่ settings ไม่ได้ชี้มาที่ bridge (เกิดจากเขียน settings ล้มหลังเขียน backup, ลบ backup ล้มหลัง restore, หรือ user แก้ statusLine เองภายหลัง)
     - `Install` เมื่อ state = `Partial` → ลบ backup เก่าทิ้ง แล้วติดตั้งใหม่ตามปกติ (ค่า settings ปัจจุบันคือความจริง)
     - `Restore(settingsPath, root)` → คืน enum `RestoreResult`:
       - `NotInstalled` → คืน `RestoreResult.NothingToDo` ไม่แตะอะไร
       - `Installed` → คืน `statusLine` เดิม (หรือลบ key ถ้า `hadStatusLine=false`) เขียนแบบ atomic **แล้วค่อย**ลบ backup → `RestoreResult.Restored`
       - `Partial` → **ไม่แตะ settings** ลบ backup อย่างเดียว → `RestoreResult.AlreadyDetached`
     - ให้ installer มี hook ภายใน (`internal`, แบบเดียวกับ `beforeReplace` ใน `PrivateFiles.cs:18`) สำหรับทำให้การเขียน settings ล้มในเทส
  3. **Bridge โหมด `statusline [--root <abs>]`** (ใน `AIUsageBar.Bridge/Program.cs`, คงโหมด `--capture-test` เดิม):
     - root ค่าเริ่มต้น = `%LOCALAPPDATA%\AIUsageBar`; ส่ง `--root` ได้สำหรับเทส
     - อ่าน stdin ให้จบ (≤2 MB, ไม่เกิน 2 วินาที) → บันทึกลง `ClaudeSnapshotStore(<root>\claude)` — error ตอนบันทึก **กลืน** ไม่ให้กระทบ statusline
     - **input เกิน 2 MB หรือ stdin ไม่จบภายใน 2 วินาที** → เรียก `store.Capture("{"u8, ...)` เพื่อบันทึกเครื่องหมาย capture-error (แบบเดียวกับ `--capture-test` เดิม, error ตอนบันทึกกลืน), **ไม่รัน** คำสั่งเดิม, ไม่พิมพ์อะไร, exit 0
     - อ่าน `original.command` จาก backup; มี → รัน Git Bash `bash.exe -c "<original command>"` (path = env `CLAUDE_CODE_GIT_BASH_PATH` ถ้ามี ไม่งั้น `C:\Program Files\Git\bin\bash.exe`) ผ่าน `WindowsProcess.Start` ด้วย `AllowDescendants=true` และ env = สำเนา env ปัจจุบัน; เขียน stdin เป็น bytes เดิมแล้ว `CloseInput`; คัดลอก stdout ของมันออก stdout ของเรา; คืน exit code ของมัน; เกิน 5 วินาที → `DisposeAsync` (ฆ่าทั้ง job) แล้วคืน 1
     - ไม่มี original (หรือ `original` = null) → ไม่พิมพ์อะไร exit 0
     - โหมด `--capture-test` เดิมต้องทำงานเหมือนเดิมทุกอย่าง
  4. Tray csproj เพิ่ม `ProjectReference` ไป Bridge เพื่อให้ไฟล์ Bridge ถูกคัดลอกไปอยู่ข้าง Tray (ใช้เป็น `bridgeSourceDir` ใน S3)
- ไฟล์ที่แก้ได้: `windows/src/AIUsageBar.Core/ClaudeBridgeInstaller.cs` (ใหม่), `windows/src/AIUsageBar.Core/AppPaths.cs` (ใหม่ ถ้าต้องการรวม path), `windows/src/AIUsageBar.Bridge/Program.cs`, `windows/src/AIUsageBar.Tray/AIUsageBar.Tray.csproj`, `windows/tests/AIUsageBar.Tests/ClaudeBridgeTests.cs` (ใหม่), `windows/tests/AIUsageBar.Tests/Program.cs`, `windows/tests/AIUsageBar.Tests/AIUsageBar.Tests.csproj` (ถ้าต้องอ้าง Bridge)
- เคสเทส (ใน `--offline`, ใช้ temp dir ทั้งหมด):
  | ID | สถานการณ์ / input | ผลที่ต้องได้ |
  |---|---|---|
  | S2-T1 | settings = `{"model":"x","hooks":{"a":1},"statusLine":{"type":"command","command":"bash ~/s.sh","padding":2}}` → `Install` | `model`, `hooks` เท่าเดิมแบบ deep-equal; `statusLine` = `{"type":"command","command":<installedCommand>,"padding":2}`; backup มี `hadStatusLine:true` และ `original` = statusLine เดิมทั้ง object |
  | S2-T2 | ต่อจาก T1 → `Restore` | settings deep-equal กับต้นฉบับ T1; ไฟล์ backup หายไป |
  | S2-T3 | settings ไม่มี `statusLine` → `Install` → `Restore` | หลัง restore ไม่มี key `statusLine` และ key อื่น deep-equal ต้นฉบับ |
  | S2-T4 | `Install` สองครั้งติดกัน | ครั้งที่สองไม่เปลี่ยน settings และ backup (เนื้อไฟล์ byte เท่าเดิม) |
  | S2-T5 | หลัง `Install` แก้ `statusLine.command` เป็น `"other"` → `State` แล้ว `Restore` | `State` = `Partial`; `Restore` คืน `AlreadyDetached`; settings ยังมี `"other"` byte เท่าเดิม; backup หายไป |
  | S2-T6 | settings เป็น JSON เสีย (`{bad`) → `Install` | โยน `CoreException`; ไฟล์ settings byte เท่าเดิม; ไม่มี backup |
  | S2-T7 | ไม่มีไฟล์ settings → `Install` | สร้าง settings ที่มีแค่ `statusLine`; backup `hadStatusLine:false`; `Restore` แล้วได้ `{}` |
  | S2-T8 | settings temp มี statusLine command = `cat` → `Install` (bridgeSourceDir = โฟลเดอร์ build ของ Bridge ที่ `Fixture.Bridge` ชี้) → รัน **exe ตัวที่ถูกคัดลอกไป `<tmp>\bridge\`** ตาม path ใน installedCommand ด้วย `statusline --root <tmp>`, stdin = fixture Claude ที่ `seven_day.used_percentage=42` | stdout byte เท่ากับ stdin; exit 0; `ClaudeSnapshotStore(<tmp>\claude).Read` ได้ Weekly.UsedPercent = 42 (พิสูจน์ว่าตัวที่ติดตั้งมีไฟล์ครบ รันได้จริง) |
  | S2-T9 | bridge `statusline --root <tmp>` ไม่มี backup, stdin = fixture เดิม | stdout ว่าง; exit 0; snapshot ถูกบันทึก |
  | S2-T10 | original = `sleep 30` | bridge จบภายใน 7 วินาที, exit 1; ไม่มี `sleep.exe` ที่ parent chain มาจาก bridge ค้างอยู่หลังจบ (เช็ค `Process.GetProcessesByName("sleep")` เทียบรายการก่อน/หลัง ต้องไม่มี PID ใหม่) |
  | S2-T11 | stdin เป็น JSON เสีย, original = `echo ok` | stdout = `ok` + newline; exit 0 (การบันทึกล้มไม่กระทบ statusline) |
  | S2-T12 | `Install` โดย hook ทำให้การเขียน settings ล้ม | `Install` โยน `CoreException`; settings byte เท่าเดิม; `State` = `Partial`; จากนั้น `Install` อีกครั้ง (ไม่มี hook) สำเร็จ → `State` = `Installed`; `Restore` → settings deep-equal ต้นฉบับ |
  | S2-T13 | สร้างสภาพ `Partial` (มี backup แต่ settings เป็นค่าเดิม) → `Restore` | คืน `AlreadyDetached`; settings byte เท่าเดิม; backup หายไป; `State` = `NotInstalled` |
  | S2-T14 | bridge `statusline --root <tmp>` (backup มี original = `echo ok`), stdin ขนาด 3 MB | stdout ว่าง; exit 0; `Read` ได้ `CaptureError == true` |
  | S2-T15 | bridge `statusline --root <tmp>` (backup มี original = `echo ok`), เปิด stdin ค้างไว้ไม่ปิด 5 วินาที | bridge จบเองภายใน 4 วินาที; stdout ว่าง; exit 0 |
  | S2-T16 | `--capture-test` เทสเดิมทั้งหมดใน ClaudeTests | ผ่านเท่าเดิม |
  (เคส T8–T15 ต้องมี Git Bash ที่ `C:\Program Files\Git\bin\bash.exe` — มีอยู่บนเครื่องนี้)
- คำสั่งตรวจ: `--offline` → failed=0 skipped=0 และ `--offline --case claude-bridge/` ผ่าน ≥15 เคส

### S3 — Tray แสดงข้อมูลจริง + เมนู
- ทำอะไร:
  1. เพิ่ม static class `UsageText` ใน Core (ฟังก์ชันล้วน เพื่อเทสได้):
     - `MenuLine(string name, UsageReading? reading, TimeZoneInfo tz)` → มีข้อมูล: `"<name>: เหลือรายสัปดาห์ <R>% · รีเซ็ต <dd/MM HH:mm>"` (R = `100 - UsedPercent` ปัดด้วย `Math.Round(x, MidpointRounding.AwayFromZero)`, เวลาแปลงเป็น tz ที่ส่งเข้า); ไม่มี → `"<name>: เหลือรายสัปดาห์ —"`
     - `Tooltip(UsageReading? codex, UsageReading? claude)` → `"Codex <R>% · Claude <R>%"` ใช้ `—` แทนตัวที่ไม่มี; ยาวไม่เกิน 127 ตัวอักษร
     - `ClaudeAge(DateTimeOffset? receivedAt, DateTimeOffset now, TimeZoneInfo tz)` → `"Claude: อัปเดตจาก Claude Code ล่าสุด <HH:mm>"`; ไม่มี → `"Claude: ยังไม่มีข้อมูลจาก Claude Code"`
  2. `TrayContext`:
     - **Codex** fetch = `NativeCodex.Create(...).FetchAsync` ผ่าน `RefreshCoordinator` เดิม (หา exe ตอนเริ่ม; ไม่พบ → เมนูแสดง `"Codex: ไม่พบ codex CLI"`); รีเฟรชอัตโนมัติทุก `RefreshPolicy.PollingInterval` (5 นาที) + ตอนเปิดแอป + เมนู `"รีเฟรชตอนนี้"` (ใช้ throttle 60 วินาทีเดิม)
     - **Claude** = อ่าน `ClaudeSnapshotStore(<root>\claude).Read(now)` ตรงๆ **ไม่ผ่าน `RefreshCoordinator` และไม่มี throttle** (เป็นการอ่านไฟล์ในเครื่อง): อ่านทุกครั้งที่เมนูกำลังเปิด (`ContextMenuStrip.Opening`), ตอนเปิดแอป, ทุกรอบรีเฟรชอัตโนมัติ และทันทีหลัง `Install` สำเร็จ
     - Codex ล้มเหลว → `RefreshCoordinator` เดิมคงตัวเลขล่าสุดไว้แต่ย่อ error เหลือ `"failed"` (`RefreshCoordinator.cs:40-50`) ดังนั้นให้ fetch delegate ของ Tray จับ `CoreException` เก็บ `Category` ไว้ในฟิลด์ก่อน rethrow แล้วแสดงบรรทัด `"Codex: ดึงข้อมูลไม่สำเร็จ (<category>)"` (exception ชนิดอื่นแสดงชื่อ type) — สำเร็จรอบถัดไปให้ลบบรรทัดนี้
     - เมนู `"เชื่อม Claude Code…"` → MessageBox แสดงคำสั่งเดิมและคำสั่งใหม่จาก `Preview` ปุ่ม OK/Cancel → OK = `Install` (bridgeSourceDir = โฟลเดอร์ของ `AIUsageBar.Tray.exe`); เมนู `"ยกเลิกการเชื่อม Claude Code"` → `Restore`: `Restored` → แจ้งว่าคืนค่าเดิมแล้ว, `AlreadyDetached` → แจ้งภาษาไทยว่า statusLine ถูกแก้ไปแล้วจึงไม่แตะ settings และล้างข้อมูลการเชื่อมแล้ว. เปิด/ปิดเมนูตาม `State`: `Installed` → เปิด "ยกเลิก", ปิด "เชื่อม"; `NotInstalled`/`Partial` → เปิด "เชื่อม", ปิด "ยกเลิก". Install/Restore ล้ม → MessageBox แสดง category ไม่แสดงเนื้อ settings
     - ตัดคำว่า "ออฟไลน์" / "รุ่นทดสอบออฟไลน์" / เมนูที่ disabled ถาวรของ W1 ออก; เมนู `"ออก"` คงไว้; โหมด `--smoke` เดิมต้องยังผ่าน (ในโหมด smoke **ห้าม**เรียก codex จริงหรือแตะ settings — ใช้ fetch ว่างแบบเดิม)
- ไฟล์ที่แก้ได้: `windows/src/AIUsageBar.Core/UsageText.cs` (ใหม่), `windows/src/AIUsageBar.Tray/TrayContext.cs`, `windows/src/AIUsageBar.Tray/Program.cs`, `windows/tests/AIUsageBar.Tests/UsageTextTests.cs` (ใหม่), `windows/tests/AIUsageBar.Tests/Program.cs`
- เคสเทส (ใน `--offline`; tz = `TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")` = UTC+7):
  | ID | สถานการณ์ / input | ผลที่ต้องได้ |
  |---|---|---|
  | S3-T1 | Codex used 28, reset `2026-10-12T02:00:00Z` | `"Codex: เหลือรายสัปดาห์ 72% · รีเซ็ต 12/10 09:00"` |
  | S3-T2 | reading = null | `"Codex: เหลือรายสัปดาห์ —"` |
  | S3-T3 | used 28.5 | เหลือ `72%` (71.5 ปัดขึ้น) ; used 0 → `100%` ; used 100 → `0%` |
  | S3-T4 | Tooltip codex used 28, claude null | `"Codex 72% · Claude —"` |
  | S3-T5 | Tooltip ทั้งคู่ null | `"Codex — · Claude —"` และความยาว ≤127 |
  | S3-T6 | ClaudeAge receivedAt `2026-10-06T03:15:00Z` | `"Claude: อัปเดตจาก Claude Code ล่าสุด 10:15"` ; null → `"Claude: ยังไม่มีข้อมูลจาก Claude Code"` |
- คำสั่งตรวจ:
  1. `--offline` → failed=0 skipped=0 และ `--offline --case usage-text/` ผ่าน 6 เคส
  2. smoke เดิมใน `windows/README.md` (ส่วน "Smoke แบบชั่วคราว") → exit 0
  3. **ของจริง (owner ตรวจด้วยตา — Codex แค่เขียนวิธีไว้ในรายงาน ไม่ต้องรอ และห้ามรายงานว่าผ่าน):** เปิด `AIUsageBar.Tray.exe` → ภายใน 30 วินาทีเมนูแสดงตัวเลข Codex; กด "เชื่อม Claude Code…" → ส่งข้อความใน Claude Code หนึ่งข้อความ → **เปิดเมนู tray ใหม่** (Claude อ่านทุกครั้งที่เปิดเมนู ไม่ต้องรอ throttle) → Claude มีตัวเลข; กด "ยกเลิกการเชื่อม" → statusLine กลับเป็นค่าเดิม

### S4 — อัปเดตเอกสาร (สั้น)
- ทำอะไร:
  1. `windows/README.md`: เปลี่ยนหัวและส่วนแรกเป็นวิธีใช้จริง (build → เปิด tray → เชื่อม Claude → ยกเลิก), คำสั่ง `--live-codex`, ข้อจำกัด (ต้องมี codex จาก npm + Git Bash; ไม่มี installer/autostart). ส่วน smoke / experiments เดิมย้ายไปไว้ท้าย ไม่ต้องลบ
  2. `windows/AGENTS.md`: เพิ่มส่วนบนสุด `## Live edition — 2026-10-06` ระบุว่า owner ยกเลิกข้อห้ามรัน Codex/Claude จริง (อ้างแผนนี้) และกติกาใหม่ = ระดับความระวังเท่า Mac; ข้อความเดิมด้านล่างให้ขึ้นต้นว่า `Historical (superseded by Live edition):`
  3. `docs/DECISIONS.md`: เพิ่มข้อใหม่ต่อท้าย (เลขถัดไป) เนื้อความ = หัวข้อ "การตัดสินใจของ owner (2026-10-06)" ของแผนนี้แบบย่อ 3 บรรทัด
  4. `docs/STATUS.md`: เพิ่มหัวข้อบนสุดใต้บรรทัด "อัปเดต" ชื่อ `## Windows live edition` 3–5 บรรทัด: ทำอะไรเสร็จ, ผลคำสั่งตรวจจริง (ตัวเลขจากที่รันจริงเท่านั้น), สิ่งที่ owner ต้องตรวจด้วยตา
  5. `CHANGELOG.md`: เพิ่มรายการใต้ส่วน Unreleased (หรือสร้างหัวนี้ถ้าไม่มี) 1–3 บรรทัด
- ไฟล์ที่แก้ได้: `windows/README.md`, `windows/AGENTS.md`, `docs/DECISIONS.md`, `docs/STATUS.md`, `CHANGELOG.md`
- หมายเหตุ: ไฟล์ docs เหล่านี้มีการแก้ค้างที่ยังไม่ commit อยู่แล้ว (ณ ตอนเขียนแผน `git diff --numstat` = CHANGELOG `37 0`, DECISIONS `104 0`, STATUS `96 3`) และโฟลเดอร์ `windows/` ทั้งหมดยัง **untracked** — **เพิ่มต่อ ห้ามทับหรือลบของเดิม, ห้าม `git add`**
- คำสั่งตรวจ:
  1. **ก่อนเริ่ม S4** รัน `git diff --numstat -- CHANGELOG.md docs/DECISIONS.md docs/STATUS.md` จดผลลงรายงาน (= baseline ของ S4) และคัดลอก `windows/README.md`, `windows/AGENTS.md` ไว้ใน temp dir นอก repo
  2. **หลัง S4** รันคำสั่งเดิมอีกครั้ง → แต่ละไฟล์ตัวเลขช่องลบ (คอลัมน์ที่ 2) **เท่ากับ baseline** และช่องเพิ่มมากกว่า baseline
  3. `git diff --no-index <สำเนาใน temp> windows/README.md` และของ `AGENTS.md` → README: ส่วน smoke/experiments เดิมยังอยู่ครบ (ย้ายที่ได้); AGENTS.md: ข้อความเดิมยังอยู่ครบ มีแค่ส่วนใหม่ด้านบนและคำนำหน้า `Historical (superseded by Live edition):`

## รายงานผล → `docs/plans/2026-10-06-windows-live.codex-report.md`

## กติกาสำหรับ Codex (อ่านก่อนเริ่ม — ใช้กับงานนี้เท่านั้น)

แผนนี้ **ผ่านการรีวิวและ owner เคาะแล้ว = frozen** หน้าที่ของคุณคือ **ลงมือเขียนโค้ด + เทสตามแผน** ไม่ใช่วางแผนหรือรีวิว

**ลำดับความสำคัญ:** กติกาข้อนี้ใช้ **แทน** ขั้นตอนวางแผน / รีวิว / validation / handoff ใน `AGENTS.md`, `docs/DEVELOPMENT.md` หรือเอกสาร process อื่นของ repo — สำหรับงานนี้เท่านั้น. ข้อห้ามด้าน **ความปลอดภัย** ของ repo (ห้าม commit/พิมพ์ secret, token, cookie, ข้อมูลบัญชี) **ยังใช้เต็มที่**

### ห้าม
1. ห้ามเขียนแผนใหม่ แผนย่อย แผนเตรียมแผน ไฟล์สัญญา (contract) หรือเอกสาร study/validation เพิ่ม
2. ห้ามรีวิวแผน ห้ามขอให้ model/agent อื่นรีวิว ห้าม spawn sub-agent เพื่อตรวจ/รีวิว
3. ห้ามทำ research/สำรวจนอกสิ่งที่ชิ้นงานต้องใช้ — อ่านโค้ดที่เกี่ยวข้องได้ แต่อย่าเปิดประเด็นใหม่
4. ห้ามแก้ไฟล์นอก "ไฟล์ที่แก้ได้" ของชิ้นงานนั้น (จำเป็นจริง → หยุดแล้วถาม)
5. ห้ามทำให้เทสผ่านด้วยการลด/ข้าม/ลบเทส หรือแก้ค่า expected ที่แผนกำหนด
6. ห้ามแก้ไฟล์แผนนี้ (ยกเว้นติ๊กสถานะในส่วนรายงานท้ายงาน)
7. ห้าม `git commit` / `git push` เว้นแต่ owner สั่งในข้อความที่ส่งมา

### ทำ
1. ทำ **ทีละชิ้นตามลำดับ** ในแผน
2. แต่ละชิ้น: เขียนโค้ด + เขียนเทสครอบ **ทุกเคส** ในตารางเคสของชิ้นนั้น — ใส่ ID เคส (เช่น `S1-T2`) ในชื่อเทสหรือ comment ให้ไล่กลับได้
3. รัน "คำสั่งตรวจ" ของชิ้นนั้น → ผ่าน = ไปชิ้นถัดไป พร้อมพิมพ์ 1 บรรทัด `✅ S<n> ผ่าน (<คำสั่ง> → <ผลสั้น>)`
4. ไม่ผ่าน → แก้ได้ **ไม่เกิน 2 รอบ** ต่อชิ้น; รอบที่ 2 ยังไม่ผ่าน → **หยุดทั้งงาน** แล้วรายงาน (error จริง + สิ่งที่ลองแล้ว)
5. ถ้าเห็นว่าแผน **ผิด / กำกวม / ขาด** จนทำต่อไม่ได้ → **หยุด** ยกบรรทัดในแผนที่มีปัญหามาตรงๆ + เสนอทางแก้ 1–2 ทาง — ห้ามตีความเองหรือด้นสด
6. เรื่องเล็กที่แผนไม่ได้พูดถึง (ชื่อตัวแปร, helper ภายในไฟล์, การจัดโค้ด) → ตัดสินใจเองได้ ตาม convention ของโค้ดรอบๆ ไม่ต้องถาม

### จบงาน (หรือหยุดกลางทาง)
เขียนรายงาน **ไฟล์เดียว** ตาม path ที่แผนระบุในหัวข้อ "รายงานผล" รูปแบบ:

```
# รายงาน Codex — <ชื่องาน>
| ชิ้น | สถานะ (ผ่าน/ไม่ผ่าน/ไม่ได้ทำ) | คำสั่งตรวจ | ผลจริง (ตัวเลข/ข้อความสั้น) | ไฟล์ที่แก้ |
## ที่ยังไม่ได้ทำ / หยุดเพราะ
## จุดที่ตัดสินใจเองนอกแผน (ถ้ามี)
## ข้อสงสัยที่ owner ควรรู้
```
ห้ามเขียนว่า "ผ่าน" ถ้าไม่ได้รันคำสั่งตรวจจริงในรอบนี้

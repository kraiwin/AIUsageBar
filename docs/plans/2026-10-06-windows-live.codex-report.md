# รายงาน Codex — Windows tray Codex + Claude live

วันที่ 2026-10-06 · ลงมือตามแผน frozen `2026-10-06-windows-live.codex-plan.md` และกติกาสำหรับ Codex
ไม่มีแผนใหม่/รีวิว/model reviewer/sub-agent/commit/push/git add ในงานนี้

| ชิ้น | สถานะ (ผ่าน/ไม่ผ่าน/ไม่ได้ทำ) | คำสั่งตรวจ | ผลจริง (ตัวเลข/ข้อความสั้น) | ไฟล์ที่แก้ |
|---|---|---|---|---|
| S1 | ผ่าน | Release build; `--offline`; `--offline --case native-codex/`; `--live-codex` | build 0 warning/0 error; offline120/0/0 (16219ms); focused10/0/0 (2492ms); live exit0, weekly_remaining=53, resets_at=2026-10-10T03:39:24.0000000Z, elapsed_ms=1535 | Core/CodexProvider.cs, NativeCodex.cs, WindowsProcess.cs; Tests/CodexTests.cs, H2InventoryTests.cs, NativeCodexTests.cs, Program.cs |
| S2 | ผ่าน | Release build; `--offline`; `--offline --case claude-bridge/` | build0/0; offline135/0/0 (25485ms); focused15/0/0 (9155ms); installed EXE stdout ตรง stdin, quota42; timeout ไม่เหลือ sleep PID ใหม่ | Core/ClaudeBridgeInstaller.cs, AppPaths.cs; Bridge/Program.cs; Tray/AIUsageBar.Tray.csproj; Tests/ClaudeBridgeTests.cs, Program.cs |
| S3 | ผ่าน (อัตโนมัติ) | Release build; `--offline`; `--offline --case usage-text/`; smoke ตาม README | build0/0; offline141/0/0 (25148ms); focused6/0/0 (20ms); smoke exit0, passed=true, menuOpened=true, refreshCount=1, reAddCount=1, uiThreadOnly=true, iconHiddenOnExit=true, nativeProviders=disabled | Core/UsageText.cs; Tray/TrayContext.cs, Program.cs; Tests/UsageTextTests.cs, Program.cs |
| S4 | ผ่าน | `git diff --numstat -- CHANGELOG.md docs/DECISIONS.md docs/STATUS.md`; `git diff --no-index` เทียบสำเนา README/AGENTS; readback | baseline37/0,104/0,96/3 → หลัง40/0,110/0,104/3; ช่องลบเท่าเดิม ช่องเพิ่มมากกว่าเดิม; README/AGENTS เดิมอยู่ครบ; `git diff --check` exit0 | windows/README.md, windows/AGENTS.md, docs/DECISIONS.md, docs/STATUS.md, CHANGELOG.md |

Path ย่อในตาราง: Core=`windows/src/AIUsageBar.Core/`, Bridge=`windows/src/AIUsageBar.Bridge/`, Tray=`windows/src/AIUsageBar.Tray/`, Tests=`windows/tests/AIUsageBar.Tests/`
รายงานนี้เป็น artifact เพิ่มอีกไฟล์ตาม path ที่แผนกำหนด ไม่แก้ไฟล์แผน
เทสทั้งหมด141 = baseline110 + native10 + bridge15 + usage-text6; S1-T5/S2-T16 อ้างเทสเดิมที่ยังลงทะเบียนครบผ่าน comments ในไฟล์เทสใหม่

คำสั่ง .NET ที่ใช้ (จาก `windows/`; บางรอบเรียก path เดียวกันจาก repo root):

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:DOTNET_NOLOGO='1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='1'
dotnet build AIUsageBar.Windows.slnx -c Release
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline --case native-codex/
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --live-codex
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline --case claude-bridge/
dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline --case usage-text/
```

รัน focused ตามลำดับของชิ้นงานและรัน full offline หลังแต่ละชิ้น ไม่ได้รัน commands ทั้งหมดใน block เป็นชุดเดียว
Live Codex รัน2ครั้งใน S1: ครั้งแรก exit0/weekly_remaining53/elapsed1463ms และหลังแก้เทส exit0/elapsed1535ms ข้างต้น
ไม่มี raw quota/config/account payload หรือ credential ใน output

## ที่ยังไม่ได้ทำ / หยุดเพราะ

S1–S4 implementation และคำสั่งตรวจอัตโนมัติครบ ไม่มีหยุดกลางทาง
**ไม่ได้ตรวจด้วยตาในโหมด live และไม่ได้ติดตั้ง bridge ลง Claude settings จริง** ตามแผน S3 ให้ owner ตรวจเอง ไม่ถือว่าผ่าน

ขั้นตอน owner ตรวจ:

1. เปิด `windows/src/AIUsageBar.Tray/bin/Release/net10.0-windows/AIUsageBar.Tray.exe` (เก็บไฟล์ข้าง EXE ทั้งโฟลเดอร์)
2. เปิดเมนูไอคอน notification area/overflow ภายใน30วินาทีตรวจว่ามีตัวเลข Codex
3. กด **เชื่อม Claude Code…** ตรวจคำสั่งเดิม/ใหม่ แล้ว OK
4. ส่งข้อความใน Claude Code หนึ่งข้อความ แล้ว **เปิดเมนู tray ใหม่** ตรวจตัวเลข Claude และเวลาที่รับ snapshot โดยไม่ต้องรอ throttle
5. กด **ยกเลิกการเชื่อม Claude Code** ตรวจว่า statusLine กลับเป็นค่าเดิม
6. เลือก **ออก** เพื่อปิด

## จุดที่ตัดสินใจเองนอกแผน (ถ้ามี)

- เลือก `AppPaths` รวม root และ helper กำหนด protected ACL user+SYSTEM ของโฟลเดอร์แอป ก่อนเปิด PrivateFiles สำหรับ backup; ตรวจ reparse ancestors ก่อนกำหนด ACL ไม่แก้หรือผ่อน PrivateFiles เดิม
- เทส cmd ส่ง `/c <command>` เป็น argument เดียวให้ quote helper เดิม เพื่อให้ cmd แปลคำสั่งที่แผนระบุได้จริง ไม่แก้ production quoting ที่อยู่นอกสิทธิ์ S1
- S1-T6 รอสั้นๆ แบบมี deadline จน job accounting เป็น ActiveProcesses=0 ก่อน assertion เพราะ primary process signal อาจมาก่อน accounting update; expected เดิมทั้งหมดคงเดิม
- Bridge drain stdout/stderr พร้อมเขียน stdin เพื่อไม่ให้ท่อเต็ม และกลืน broken-pipe ตอน original เช่น echo จบก่อนอ่าน stdin; ไม่ส่ง stderr ของคำสั่งเดิมออก log
- Tray รอ `RefreshCoordinator.ShutdownAsync` เมื่อออก เพื่อให้ process cleanup จบ; tooltip ใช้ `—` ระหว่าง error และเมนูระบุข้อมูล Codex ล่าสุดพร้อมเวลา
- เอกสารเดิมทั้งหมดเก็บไว้ภายใต้ Historical; ส่วน live อยู่ด้านบน ทำให้ smoke/experiments และประวัติไม่สูญหาย

## ข้อสงสัยที่ owner ควรรู้

ผลล้มเหลวและการแก้ตามเพดานในแผน:

- S1 แรก buildผ่าน แต่ full117/3/0 และ focused7/3/0: S1-T6/T8 เป็น TestFailure, T10 เป็น cleanup-failure; สาเหตุ cmd ที่รับ `/c` และ command แยกกันผ่าน quote helper ตีความคำสั่งผิด
- S1 แก้รอบ1 รวม `/c <command>`: focused9/1/0 เหลือ T6; แก้รอบ2 เพิ่ม bounded accounting wait: focused10/0/0 แล้ว full120/0/0 ผ่าน ผล T6 exit0/total3/active0; ไม่ลดหรือเปลี่ยน expected
- S2 แรก buildผ่าน แต่ focused2/13/0: `unsafe-owner` จาก ACL สืบทอดบน root ที่ Directory.CreateDirectory สร้าง; แก้รอบ1ด้วย helper protected ACL ภายในไฟล์ที่อนุญาต → focused15/0/0 และ full135/0/0
- S3 build/focused/full/smoke ผ่านรอบแรก; S4 เอกสารเพิ่มอย่างเดียวผ่านรอบแรก `diff --no-index` exit1 หมายถึงพบการเพิ่มเนื้อหาที่ตั้งใจ ไม่ใช่ตรวจล้มเหลว
- คำสั่งแก้ source ครั้งแรกเจอ Python cp874 decode error; เปลี่ยน PYTHONUTF8=1 และรันการแก้ต่อสำเร็จก่อน build ไม่แก้ dependency หรือไฟล์นอก scope

สำเนา S4 ก่อนแก้อยู่ใน `%TEMP%/AIUsageBar-live-s4-add27cd4-9419-404b-847c-f7b9f75f2d06/` (`README.md`, `AGENTS.md`)
ตรวจ diffจริงทั้งสองไฟล์ และ assert เนื้อหาเดิมยังเป็น suffix ของไฟล์ใหม่ ได้ `preserved-original=true` ทั้งคู่
README ส่วน Smoke แบบชั่วคราว/experiments คงเดิมครบ

ไม่มี installer/autostart/release/tag/binary publication ไม่มีเพิ่ม NuGet ภายนอก ไม่มีแก้ Mac source/tests/Config/Xcode
H2/C6/tee เดิมยังอยู่; native H2 inventory-mode route ยังไม่ได้เพิ่ม งานนี้ต่อ Codex quota provider ตาม live plan
Build output และโฟลเดอร์ temp ทดสอบไม่ใช่ release; ไม่ได้เปิด tray live ทิ้งไว้

## S2-FIX

วันที่ 2026-10-06 · แก้ตามคำสั่ง owner เฉพาะ `windows/src/AIUsageBar.Bridge/Program.cs` และ `windows/tests/AIUsageBar.Tests/ClaudeBridgeTests.cs` แล้วต่อท้ายรายงานนี้ ไม่มีวางแผน/รีวิว/commit/push

- โหมด statusline ส่ง `Environment.CurrentDirectory` ของ bridge เป็น working directory ให้ bash แทน root ของแอป เพื่อคงโฟลเดอร์ที่ Claude Code เรียกมา
- เพิ่ม `claude-bridge/S2-T17-original-inherits-working-directory`: original command=`pwd -W`, process ของ bridge ใช้ temp dir X ที่แยกจาก root; ตัด newline และ normalize slash ก่อนเทียบ path แบบ OrdinalIgnoreCase พร้อมตรวจ exit0
- อ่านโค้ดที่แก้กลับเทียบคำสั่งแล้ว; รันจาก `windows/` โดยตั้ง SDK controls ทั้ง4ตามแผน

| ชิ้น | สถานะ | คำสั่งตรวจ | ผลจริง |
|---|---|---|---|
| S2-FIX | ผ่าน | `dotnet build AIUsageBar.Windows.slnx -c Release` | exit0, 0 warning/0 error |
| S2-T17 + regression | ผ่าน | `dotnet run --project tests/AIUsageBar.Tests -c Release --no-build -- --offline` | exit0; S2-T17 PASS (574ms); SUMMARY passed=142 failed=0 skipped=0 elapsedMs=33693 |

ผ่านรอบแรก ไม่มีแก้ซ้ำหรือลด expected; ไม่เรียกบัญชีจริงหรือแตะ Claude settings จริง งานตรวจ live ด้วยตาที่รายงานเดิมระบุยังไม่ได้ทำ

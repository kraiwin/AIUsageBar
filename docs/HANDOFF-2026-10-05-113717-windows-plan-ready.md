# AIUsageBar — Handoff หลังวางแผน Windows บน PC

บันทึก 2026-10-05 11:37:17 Asia/Bangkok · workspace `C:/Python/AIUsage` · PowerShell

## เป้าหมายและสถานะล่าสุด

ผู้ใช้ให้ clone repoเดิม `kraiwin/AIUsageBar` ลง PC เพื่อทำ Windows edition จากแอป Mac ที่ใช้งานได้แล้ว จากนั้นสั่งอ่าน MEMORY/handoff Windows ตรวจ native CLI/WSL และเขียนแผน system-tray MVP แยก `windows/` โดยคง Mac source/build เดิม พร้อมเสนอ stack/ขอบเขตก่อน implement

งานสำรวจและแผนเสร็จแล้ว แผนผ่าน independent native Codex CLI review หลังแก้ findings และ delta re-review แต่ **ยังไม่มี Windows source/build/tests หรือ compatibility proof จากการรัน provider จริง**

คำสั่งล่าสุดคือ “งั้นเขียน handoff ให้หน่อย จะเปิด session ใหม่” เป็นการขอเอกสารรับช่วง ยังไม่มีคำสั่งชัดเจนให้ proceed implementation, เรียกบัญชีจริง หรือติดตั้ง bridge อย่าเปลี่ยนข้อเสนอเป็น approved decision โดยอัตโนมัติ

handoff นี้แทนงานถัดไปใน [Windows start handoff จาก Mac](HANDOFF-2026-10-05-111324-windows-start.md) สำหรับการทำต่อบน PC; handoff เก่ายังใช้เป็นข้อกำหนดและบริบทได้

## อ่านก่อนเริ่ม session ใหม่

1. `AGENTS.md` และ user-level instructions ของ session/เครื่องนี้
2. [PROJECT_BRIEF](PROJECT_BRIEF.md), [DECISIONS](DECISIONS.md) โดยเฉพาะข้อ22–23, [STATUS](STATUS.md), [DEVELOPMENT](DEVELOPMENT.md)
3. `MEMORY.md` และ handoff นี้
4. [Windows MVP plan](plans/2026-10-05-windows-mvp-plan.md) — stack, support matrix, tray UX, file manifest, W0–W5, guards/process/storage/bridge/test gates และ review dispositions
5. [Windows environment evidence](validation/2026-10-05-windows-environment.md)

ตรวจ Git และเครื่องจริงก่อนใช้ snapshot นี้ หาก source/CLI/config/context เปลี่ยนต้องตรวจความสอดคล้องใหม่ ไม่ต้องสำรวจซ้ำทั้งหมดเมื่อยังเหมือนเดิม

## ผลตรวจ PC ที่มีหลักฐาน

| รายการ | ผล |
|---|---|
| OS/CPU | Windows11 Pro 10.0.26200 x64, AMD Ryzen5 9600X |
| .NET | SDK10.0.401 selected, WindowsDesktop runtime10.0.12 present |
| Codex | npm `codex.ps1` บน PATH; native `codex.exe`0.160.0 ทำงานผ่าน direct version/help |
| Native Codex path | `%APPDATA%/npm/node_modules/@openai/codex/node_modules/@openai/codex-win32-x64/vendor/x86_64-pc-windows-msvc/bin/codex.exe` |
| app-server help | มี `--strict-config`, `--listen stdio://`, `-c`; ยังไม่ได้พิสูจน์ guards/runtime |
| Claude Code | native `%USERPROFILE%/.local/bin/claude.exe`2.1.289 |
| Git Bash | `C:/Program Files/Git/bin/bash.exe` มีอยู่, Git2.55.0.windows.3 |
| PATH bash | `C:/Windows/system32/bash.exe` เป็นWSL launcher ไม่ใช่Git Bashที่ควรเลือก |
| WSL | WSL2 มีเพียง `docker-desktop` running/default; ไม่เข้า distro และไม่เปลี่ยน Docker |
| Current-process env | `CODEX_HOME`, `CLAUDE_CONFIG_DIR`, `CLAUDE_CODE_GIT_BASH_PATH` unset; ไม่รับรอง settings/sessionอื่น |

ไม่ได้เปิด credential/config payload, เรียก account quota, login/logout, model/thread หรือแก้ settings/bridge/system policy ผล version/help ไม่พิสูจน์การ login หรือ weekly quota จริง

## ข้อเสนอที่ส่ง owner แล้ว

- C#/.NET10 + WinForms `NotifyIcon`/`ContextMenuStrip`; ใช้ Microsoft desktop framework/Win32 ไม่มี third-party NuGet ในข้อเสนอแรก
- Windows11 x64 + native Codex/Claudeก่อน; Claude bridgeจำกัด confirmed Git Bash เลื่อน WSL, PowerShell statusline, Windows10/ARM64
- หนึ่งไอคอน tray + tooltip/menuภาษาไทย แสดง weekly remaining/reset/freshness/refresh/quit ไม่ใช่ข้อความยาวบน taskbar แบบMac
- Sourceใหม่อยู่ `windows/`; ไม่ย้าย/แก้ `AIUsageBar/`, `AIUsageBarTests/`, `Config/`, `AIUsageBar.xcodeproj` และไม่สร้าง shared compiled Swift core
- Portable framework-dependent source-build output; console bridgeต้องมี apphost/DLL/deps/runtimeconfigครบ ไม่สัญญาsingle EXE
- ไม่รวม installer/signing/autostart/registry/task scheduler/updater/telemetry/binary release/push
- แนะนำขอบเขต implementation แรก **W1–W2: offline tray/core/tests + isolated native compatibility/shell/filesystem spikes** ก่อน W3บัญชีจริงและ W4bridgeส่วนตัว

Ownerยังต้องเลือก stack/context/tray UX และสั่ง proceed พร้อมขอบเขต ใน sessionใหม่หาก userให้คำสั่งชัดแล้วให้บันทึกตามนั้นและทำงานต่อ อย่าถาม permissionซ้ำสำหรับสิ่งที่ได้รับอนุญาตแล้ว

## ผล independent review

รอบแรก native Codex CLI exit0, verdict `fix-then-proceed`: 5blockers/2suggestions แก้และบันทึกdispositionครบในท้ายแผน

- จำกัด fully quoted Windows command line32,767 UTF-16 unitsรวมNUL ตรวจแต่ละlaunch; inventoryได้จากAจึงตรวจoverrideของBหลังAจบ
- Bind compatibilityกับversion/file identity/SHA-256; replacementที่pathเดิมยกเลิกproof
- Bash launcher/native bridge readinessก่อนอ่านstdin/เริ่มoriginal; fallbackเฉพาะknown pre-handoff failure ห้ามrerunจากexit codeหลังhandoff และไม่อ้างexactly-onceภายใต้arbitrary crash
- Capture2MiBแยกจากpassthrough; overflow/parse failureต้องไม่ตัดoriginal stdin/stdout/stderr/exit และต้องพิสูจน์deadlock/cancelจริง
- Invalid inputเป็นsafe error state/markerที่บังคับ เก็บsnapshot/receiptเดิมและclearเมื่อvalid input; ไม่ปนกับvalid no-data
- เพิ่มfault matrixทุกinstall/restore boundary และconcreteWindows sharing/identity/conflict contract พร้อมยอมรับraceกับexternal editor

Delta re-review native Codex CLI exit0, verdict **`proceed for PLAN`**, ไม่มีmajor/minorใหม่ รับdispositionsครบ ไม่ใช่ownerapprovalหรือผลimplementation/testsผ่าน

Evidence localนอกrepo:

- `C:/tmp/codex-reviews/raw-20261005-112659.md`, `summary-20261005-112659.md`
- `C:/tmp/codex-reviews/raw-20261005-113134.md`, `summary-20261005-113134.md`

Reviewอ่านไฟล์/sectionsแบบinline ไม่มีreviewer tool calls แต่ CLIแสดงconfigured hook status จึง **ไม่ใช่isolated/no-hook startup probe** ในW2 บันทึกผลreview/ข้อจำกัดไว้ในแผนแล้ว ไม่ต้องมีrawไฟล์เหล่านี้เพื่อเข้าใจแผน

หลังจัดprompt deltaมีself-checkเพิ่ม: รันbuildจากcwd `windows/` เพื่อให้global.jsonเลือกSDK และอนุญาตpersist nonsecret executable-proof metadata สองclarificationsนี้อยู่นอกdelta review scope

## งานถัดไป

1. ตรวจGit/อ่านแผน แล้วรับownerdecisionของstack/native-first/trayและW1–W2 หากยังไม่มีคำสั่งproceedให้เสนอชุดนี้ก่อน ห้ามเริ่มsourceโดยเดาว่าhandoffคือapproval
2. เมื่ออนุมัติ: บันทึกDECISIONS/specตามจริง ปฏิบัติตามgrill/plan-gateที่applicableในsessionใหม่ โดยreuseแผน/reviewนี้ ไม่สร้างworkflowหรือreviewซ้ำโดยไร้เหตุผล
3. ทำเฉพาะไฟล์ในmanifestใต้ `windows/` พร้อมWindows-specific AGENTS; offline tray/models/coordinator/console test harness ไม่มีlive account startupโดยdefault
4. Buildจาก `C:/Python/AIUsage/windows`: `dotnet build AIUsageBar.Windows.slnx -c Release`; offlinecheck `dotnet run --project tests/AIUsageBar.Tests -c Release -- --offline` — **คำสั่งในอนาคต ยังไม่รันเพราะไม่มีproject**
5. W2: auditversionedofficialWindowsCLI source + isolatedscratch config/canaries/positive-negativecontrols ก่อนrealquota รวมsuspendedCreateProcess/jobcleanup/limits/encoding และactualbridgeBashhandshake/streams/filesystemspike
6. หากguardหรือbridgeproofไม่ผ่าน ให้failclosed/คงsettingsเดิม ไม่ใช้mcp-listfallback/ลดstrict-configหรือกลบmissingเป็น0 ทำส่วนindependentต่อได้และรายงานblockerจริง
7. W3realCodex/W4liveClaudeinstallต้องมีexecution scopeและexactpreviewที่เหมาะสมก่อน แผนผ่านไม่ได้อนุญาตบัญชี/settingswrites; ไม่สร้างmodelturnเพื่อกระตุ้นquota
8. Self-gateก่อนclaimเสร็จ/triggerdownstream: reread, relevantbuild/tests, compareworkingMacreferenceทีละrequirement, actualfield/UI/lifecyclecheck และindependentreviewเส้นทางprocess/config/settings บันทึกผล/สิ่งไม่ตรวจ ไม่อ้างXcodeรันบนPC

## Git / artifact snapshot

- Branch `main` tracks `origin/main`; HEAD `ca2eb2b` (`docs: hand off Windows MVP planning to PC [skip ci]`); origin `https://github.com/kraiwin/AIUsageBar.git`
- **งานเอกสารรอบPCยังไม่commit/push** รักษาworkingtreeนี้ไว้ ไม่reset/clean/discardเพื่อกลับไปhandoffเก่า
- Modifiedก่อนhandoff: `.gitignore`, `CHANGELOG.md`, `MEMORY.md`, `docs/DECISIONS.md`, `docs/STATUS.md`
- Untrackedก่อนhandoff: `docs/plans/2026-10-05-windows-mvp-plan.md`, `docs/validation/2026-10-05-windows-environment.md`; handoffนี้จะเป็นuntrackedเพิ่ม
- Local-only proposed spec: `docs/specs/spec-2026-10-05-112624-windows-system-tray-mvp.md` ถูกignoreผ่าน`.gitignore`; ยังไม่agreedและไม่ควรforce-add
- NoMacsource/project/configdiff; noWindowsfolder/source/build/testsในรอบนี้; Mac0.1.0/build6และผล126testsเดิมเป็นMacevidenceเท่านั้น
- Planning self-gate: rereadเอกสาร, localMarkdownlinks/tablecolumnsและGitwhitespacecheckผ่าน ไม่มีappbuild/testในPC ขอบเขตhandoffใหม่จะตรวจซ้ำก่อนส่งมอบ

## Prompt สำหรับ session ใหม่

```text
อ่าน AGENTS.md, MEMORY.md และ docs/HANDOFF-2026-10-05-113717-windows-plan-ready.md
จากนั้นอ่าน docs/plans/2026-10-05-windows-mvp-plan.md, DECISIONSข้อ23 และตรวจGit/CLIstateจริง
รับช่วง Windows edition ใน C:/Python/AIUsage โดยแยก windows/ คงMacsource/Xcodeเดิม
แผนผ่านCodexreviewแล้ว แต่ยังไม่ได้implementและงานdocsยังไม่commit/push
เสนอให้ผมยืนยัน C#/.NET10/WinForms + nativeWindows11 x64ก่อน + icon/menuไทย และเริ่มเฉพาะW1–W2offline/isolatedก่อน
ถ้าผมให้คำสั่งproceedชัดในsessionนี้ ให้บันทึกscopeและทำตามแผนภายในscopeนั้น ไม่ขอซ้ำ
ห้ามอนุมานว่าเปิดsessionใหม่คืออนุมัติaccountquota/bridgeinstall/autostart/release/push
```

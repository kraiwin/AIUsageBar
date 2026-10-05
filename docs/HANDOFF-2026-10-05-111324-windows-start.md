# AIUsageBar — Handoff สำหรับเริ่ม Windows บน PC

บันทึก 2026-10-05T11:13:24.464168+07:00 · จาก Mac workspace `/Users/kraiwin/Documents/AIUsageBar`

## เป้าหมายและสถานะ

Mac MVP สำหรับใช้ส่วนตัวเสร็จแล้ว; source fix ล่าสุดคือ `65b85cd57fc692ae28d29fb0b5cffc5cf4bbc580` บน main ที่ https://github.com/kraiwin/AIUsageBar
ผู้ใช้ต้องการนำ repo นี้ลง PC เพื่อเริ่ม Windows edition ของผลิตภัณฑ์เดียวกัน โดยแยก `windows/` และคง Mac source/build เดิม
รอบส่งต่อนี้ทำเฉพาะเอกสารและ commit/push แบบ no-ci; ยังไม่มี Windows source/build/tests/CLI compatibility proof
เริ่มบน PC จากการตรวจสภาพเครื่องและ native Windows/WSL แล้วเสนอแผน MVP ที่ทำได้จริงก่อนเชื่อมบัญชีหรือติดตั้ง bridge

## อ่านก่อนและแหล่งข้อมูลที่ต้องยึด

อ่าน AGENTS.md, docs/PROJECT_BRIEF.md, docs/DECISIONS.md, docs/STATUS.md, docs/DEVELOPMENT.md, MEMORY.md และ handoff นี้
ข้อกำหนดเดิมจำนวนมากเป็น Mac-specific: Swift/Apple frameworks, Xcode, Application Support, /bin/sh, POSIX permissions, process groups, ad-hoc signing ใช้กับ Windows ตรง ๆ ไม่ได้
อย่านำชื่อ path/คำสั่ง Mac มาใช้บน PC หรือถือว่า Mac validation พิสูจน์ Windows แล้ว ให้แยก platform policy และบันทึกข้อเสนอ stack/dependencies/Windows minimum ใน DECISIONS ก่อน implementation
AGENTS บน PC/local session อาจมีกฎเครื่องมือและทีมเพิ่มเติม ให้ตรวจจริง อย่าใช้ /Users path สำหรับ workspace บน Windows

## ขอบเขตที่ผู้ใช้เลือกและสิ่งที่ยังไม่สรุป

- ใช้ repo เดิม `kraiwin/AIUsageBar`, เพิ่ม `windows/` โดยไม่ย้าย AIUsageBar/, AIUsageBarTests/, Config/ หรือ Xcode project ในรอบเริ่มต้น
- Mac คงใช้ได้ รุ่น Windows ตั้งเป้า system tray ข้างนาฬิกา เมนูภาษาไทย แสดง weekly remaining/reset/freshness/refresh/quit
- Windows tray มาตรฐานเป็น icon + tooltip/menu ไม่ใช่ข้อความยาวบนแถบแบบ Mac; หากต้องการตัวเลขเห็นตลอดต้องเสนอ UX/ข้อจำกัดให้เลือก ไม่ดัดแปลง taskbar ด้วย unofficial injection
- ต้องสำรวจก่อนว่า Windows version/architecture, Codex CLI และ Claude Code อยู่ native Windows หรือ WSL distro ใด; ยังไม่สรุปว่าต้องรองรับสองแบบทั้งหมด
- Stack ยังไม่เลือก: พิจารณา native Windows เช่น C#/.NET กับ Windows UI ที่เหมาะสำหรับ tray เป็นตัวเลือก ไม่ใช่ข้อสรุปหรืออนุมัติ dependency ใดแล้ว
- หลีกเลี่ยง external dependencies เช่นเดิม; หากต้องใช้ให้แจ้งเหตุผล ขอบเขต และตรวจ dependency/script ก่อนติดตั้งหรือรัน ไม่ย้าย Mac ไป framework ใหม่เพื่อให้ share code โดยอัตโนมัติ
- เวอร์ชันแรก/installer/signing/auto-start และวิธีแจก Windows ยังไม่สรุป ไม่อนุมานว่าการ push handoff อนุญาต binary release, auto-start, registry modification หรือ account settings writes

## ข้อกำหนดร่วมที่ต้องรักษา

1. แสดง subscription weekly usage ของ Codex/Claude ไม่ใช่ API billing; ไม่เดาข้อมูล missing=0
2. ไม่มี token/cookie reader หรือ credential cache ของเรา ให้ official CLI จัดการ auth lifecycle; ไม่พิมพ์ raw auth/config/stderr/account payload หรือเก็บไว้ใน public docs/fixtures
3. Codex quota-only: inventory+guarded quota, ไม่สร้าง thread/model turn ไม่ login/logout ไม่ใช้ mcp-list fallback และไม่ลด strict-config เพื่อให้ดูเหมือนใช้งานได้
4. Claude latest snapshot มีป้ายเวลารับ ไม่อ้างว่า server ยืนยันล่าสุดหรือผูกบัญชีปัจจุบัน; valid no-quota → no-data/—, invalid input แยก error ไม่ทำให้ snapshot เก่าสดขึ้น
5. Polling Codex 5 นาที, throttle อย่างน้อย 1 นาทีรวม relaunch; snapshot check 5 วินาทีเป็น baseline Mac ที่ปรับบน Windowsได้เมื่อมีเหตุผล/ข้อสรุป
6. Bridge ต้อง preview/backup/compare-before-write, รักษา settings/statusline เดิมและ unrelated edits, restore/retry หลัง cleanup interruption ได้; ไม่ลบ settings จริงเพื่อจำลอง first install
7. ไม่มี updater/analytics/telemetry; ไม่เพิ่ม host/network listener หรือสิทธิ์กว้างเงียบ ๆ
8. ใช้ fixtures สังเคราะห์ offline tests; งาน process/config/settings สำคัญต้อง independent review ก่อนปิด ไม่มีค่าบัญชีจริงใน repository

## ผล Mac ที่เสร็จแล้วและข้อจำกัด

ดู docs/validation/2026-10-05-fix-closure.md:
- 126 XCTest/0 failed/0 skipped, Release build, synthetic CLI smoke2/2 และ native Codex independent-context review ผ่าน
- UI Macจริง restore/install/disconnect/reinstall คืน settings ตรง bytes เดิม; helper/wrapper ตรง executable ใหม่ เปิด default Menu Barแล้ว
- First install absent settings และ interrupted cleanup มี synthetic regression; BOM/UTF-16 ที่ parser ไม่รองรับถูกปฏิเสธก่อนเขียน
- Mac source version0.1.0/build6; ไม่ bump ใน handoff docs-only นี้ ไม่มี tag/binary/GitHub Release/notarization
- Audible VoiceOver, real sleep/offline/timezone และ macOS14/Intel ยังไม่ตรวจ ไม่เป็นงานบังคับก่อนเริ่ม Windows
- ผู้ใช้แจ้งว่าเปิด Claude Code session ใหม่แล้ว แต่ session นี้ไม่ได้ตรวจว่า snapshot ใหม่เข้ามาจริง ห้ามอ้างว่ายืนยันแล้ว

## โค้ดที่ใช้เป็น reference ภายใน repo

| เรื่อง | ไฟล์/กลุ่มไฟล์ |
|---|---|
| Quota model/parser/freshness | AIUsageBar/Models/, AIUsageBar/Providers/UsagePayloadParser.swift |
| Codex two-child orchestration | AIUsageBar/Providers/CodexQuotaProvider.swift |
| Child overrides/registry/config invariants | AIUsageBar/Providers/CodexLaunchPolicy.swift |
| Bounded JSONL RPC/envelope | AIUsageBar/Providers/CodexRPCSession.swift |
| Process timeout/cleanup | AIUsageBar/Providers/CodexProcessTransport.swift — POSIX ต้องออกแบบ Windows ใหม่ |
| CLI discovery | AIUsageBar/Providers/CodexExecutableDiscovery.swift — Mac paths ไม่ใช่ Windows implementation |
| Claude capture/schema | AIUsageBar/ClaudeBridge/ClaudeSnapshot.swift, ClaudeBridgeCommand.swift |
| Original command/wrapper | AIUsageBar/ClaudeBridge/ClaudeOriginalCommand.swift — /bin/sh ต้องเปลี่ยนตาม shell จริง |
| Preview/settings/ownership/retry | AIUsageBar/ClaudeBridge/ClaudeBridgeInstaller.swift, ClaudeSettingsCommand.swift, ClaudePrivateFiles.swift |
| Polling/cancel/quit/UI text | AIUsageBar/App/RefreshCoordinator.swift, AIUsageBar/UI/ |
| Offline acceptance cases | AIUsageBarTests/ — ใช้ test semantics/fixtures เป็น reference ไม่คัดลอก Unix assumptions |

อย่าเริ่มจากสร้าง shared compiled core; Swift code ไม่ได้ compile Windows ได้ครบโดยอัตโนมัติ เริ่มใช้เอกสาร protocol และชุด synthetic cases ร่วมก่อน เสนอ shared fixtures เมื่อจำเป็น
Reference CodexBar อยู่ใน gitignored reference/ บน Mac จะไม่ติดมากับ clone; ไม่ต้องมีเพื่อเริ่ม Windows หากต้องศึกษาใหม่ให้ตรวจ code/dependencies ก่อนรัน

## ลำดับเริ่มบน PC

1. Clone/pull public repo ลง working directory บน PC แล้วตรวจ branch/main, HEAD, git status และเอกสารล่าสุด; ไม่ใช้ Mac build/ artifacts/backups ที่ไม่ติด Git
2. ตรวจ Windows version/architecture และ installed toolchain/CLI presence+versions แบบไม่เปิด credential/config contents ไม่ dump full environment หรือ CLI config
3. ตรวจว่า CLIใช้งานที่ไหน native/WSL หากสำรวจจากเครื่องไม่ได้ให้ถามผู้ใช้เพียงข้อที่จำเป็น ศึกษา official docs ของเวอร์ชันปัจจุบันก่อนสรุป supported route
4. เขียน docs/plans/<date>-windows-mvp-plan.md พร้อม stack/tray UX/paths/shell/account effects/process ownership/cleanup/timeout/encoding+CRLF/ACL/atomic replacement/retry/test gates และ support matrix ที่เล็ก
5. พิสูจน์ quota path startup guards บน Windows/WSL ด้วย synthetic isolated config/canaries/positive controlก่อน real account RPC; ข้อพิสูจน์ Mac0.160 ไม่รับรอง WindowsหรือCLIใหม่ guard/configไม่ตรงให้ failclosed
6. เสนอแผนและ trade-offs ให้ owner ตัดสินใจ stack/target context ก่อน implementation ที่เปลี่ยน platform baseline; หลังอนุมัติทำใน windows/ พร้อม Windows-specific local instructions โดยคง app Mac เดิม
7. Implement offline tray skeleton + parsers/process testsก่อน real Codex แล้วค่อย Claude preview/installer/integration เมื่อ explicit plan/account/settings scope พร้อม
8. ตรวจบน PCจริง: tray/menu/refresh/quit/relaunch+Explorer restart/sleep/cancellation/owned child cleanup และ install→restore→reinstallตามsupport context บันทึกผลจริง; Mac tests ใช้เป็น regression บน Mac/CIที่อนุมัติ ไม่อ้างว่ารัน XcodeบนPCได้
9. อัปเดต README แยก Mac/Windows วิธี build/tests/install/remove, STATUS, DECISIONS และ CHANGELOGตามผลจริง ก่อน commit/pushชุดWindowsให้ตรวจ scope/review/secret checks ตามคำสั่ง session นั้น

## Git snapshot และการส่งต่อ

ก่อนเขียนเอกสาร: main HEAD65b85cd tracks origin/main, working treeสะอาด; remote fetch/push URLเป็น https://github.com/kraiwin/AIUsageBar.git
รอบนี้เปลี่ยนเฉพาะ handoff/MEMORY/DECISIONS/STATUS/CHANGELOG ไม่มี Windows implementation หรือการแก้ Mac code/build metadata
ผู้ใช้สั่ง /ship-no-ci อนุญาต scoped docs commit/pushไปorigin/main ใส่ [skip ci] เป็น convention ไม่รับรอง CIทุกระบบ Offline ship gate รอบนี้รันใหม่ 126 tests/0 failures, TEST SUCCEEDED ที่ build/windows-handoff-ship-tests.log; ไม่มี live account transports ระหว่าง XCTest เพราะ startup ถูกกันด้วย XCTestConfigurationFilePath ผล pushอ่านกลับจะบันทึกในคำตอบ; อย่าใช้ handoffนี้อ้าง hashของcommitเอกสารที่ยังไม่สร้าง

## Prompt สำหรับ Codex บน PC

อ่าน MEMORY.md และ handoff Windows ล่าสุด ตรวจ repo และสภาพ Windows/native CLI/WSL แล้วเขียนแผน Windows system-tray MVP ใน repo นี้โดยแยก windows/ คง Mac เดิมไว้ ตรวจ compatibility/guardsก่อนเชื่อมบัญชี และเสนอ stack/ขอบเขตให้ผมเลือกก่อนเริ่ม implementation

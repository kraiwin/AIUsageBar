# สถานะ AIUsageBar

อัปเดต: 2026-10-04 · **พร้อมใช้ส่วนตัวผ่าน Codex CLI/Claude Code บน Mac นี้** ตาม S1–S3; เผยแพร่ public source แล้วที่ [kraiwin/AIUsageBar](https://github.com/kraiwin/AIUsageBar)

## Local fix หลัง publication

แก้ Claude first install แล้ว: ไม่ต้องมี statusline/settings.json เดิม; quota-only silent capture พร้อมคืนค่าและรักษา unrelated edits Final XCTest **120/120**, Release build และ synthetic Release CLI install→collect→restore **2/2** ผ่าน Independent-context Codex review ปิด blocker แล้ว ไม่ใช่ Claude review ยังไม่อัปเดตแอป/bridge ที่ใช้งานจริงและยังไม่ commit/push ดู [validation](validation/2026-10-04-claude-first-install.md)

ผู้ใช้สั่งปิดคืนนี้: commit/push เฉพาะเอกสารและ handoff; source/tests และการลบ Tools ยังเป็น local WIP ไม่เผยแพร่ ไม่เปลี่ยนเวอร์ชันและไม่ติดตั้งจริง เริ่มงานต่อเมื่อผู้ใช้สั่ง

README เพิ่มวิธีอัปเดต bridge หลัง build, shell limitation และคำอธิบาย internal docs; ลบ Tools entrypoint ที่ไม่ใช้ คง Codex strict-config จนมีหลักฐาน/validation สำหรับเปลี่ยนขอบเขต guard

## เวอร์ชันและหลักฐานจาก publication เดิม

- Public source version `0.1.0` build **6**; `origin` = `https://github.com/kraiwin/AIUsageBar.git`, branch `main` ไม่มี tag/GitHub Release/binary release
- Final Release build ผ่าน; XCTest **113/113**, failed0/skipped0; independent review ปิด findings ครบและตรวจ delta สุดท้ายแล้ว
- Real Codex weekly/reset + actual Claude Code latest snapshot แสดงพร้อมกันแล้ว; Claude install→exact restore→reinstall ผ่าน; Cmd-Q ผ่าน
- Default menu-mode60.60s: CPU0.48%, peakRSS96.75MiB, zero owned children หลัง fetch
- หลักฐาน/คำสั่ง/ผลก่อนหน้าและข้อจำกัดทั้งหมด: [S1–S3 validation](validation/2026-10-03-s1-s3.md)

## Milestones

| ID | ผลลัพธ์ | สถานะและขอบเขต |
|---|---|---|
| M0 | ขอบเขต/ระบบบริหารงาน | เสร็จ |
| M1 | วิธีเชื่อม CLI และ guards | ลงมือแล้ว default direct Codex0.160 และ latest Claude snapshot; ไม่มี credential reader |
| M2 | Menu Bar ภาษาไทย | ใช้งานจริงแล้ว text-only ชื่อเต็ม, status/freshness/refresh/quit, optional live window |
| M3 | สอง providers | จริงทั้งสองค่ายบน Mac นี้; Claude เป็น ingest-time snapshot ไม่มี account binding |
| M4 | ความถูกต้อง/ความทนทาน | personal-use acceptance ผ่าน tests/review/live flows/resource; audibleVoiceOver, actualsleep/wake/offline/timezoneยังไม่ได้ตรวจ |
| M5 | source-build preparation | public source เผยแพร่แล้ว พร้อม README ภาษาไทย/license/credits และ pre-publication scope checks |

## วิธีใช้งานบน Mac นี้

Final validated bundle: `build/ReleaseValidation/Build/Products/Release/AIUsageBar.app` เปิดอยู่ในโหมด Menu Bar ปกติ เลือก native Codex path เดิมแล้ว Claude bridge ติดตั้งที่ `~/Library/Application Support/AIUsageBar/` โดย backup/metadata/private snapshot อยู่นอก Git

Build จาก source ด้วยคำสั่งใน README หรือ Xcode ไม่ต้อง Apple Developer Program; App Sandbox ปิด/Hardened Runtime เปิด/ad-hoc ไม่มี XPC/notarization/updater/telemetry

## งานต่อไปและข้อจำกัด

1. ใช้งานส่วนตัวและเก็บ feedback ได้ ไม่ต้องกลับไปทำ startup prerequisites เดิมถ้า CLI/config ไม่เปลี่ยน
2. Optional validation ที่ยังไม่ได้ตรวจ: audible VoiceOver, sleep/wake/offline/OS-clock/timezone จริง และ runtime macOS14/Intel ระบุไว้ตามจริง ไม่ใช่ gate ที่ผ่านแล้ว
3. Public source อยู่ที่ `kraiwin/AIUsageBar` แล้ว; source commit แรก `2f0fe19` อ่านกลับตรงกับ remote `main` ไม่มี tag/binary/GitHub Release การเปลี่ยนต่อไปใช้ local gate/review/secret scope ก่อน commit/push
4. Claude valid latest ไม่มี quota → no-data/—; malformed ไม่เป็น0; snapshot/sessionล่าสุดไม่รับรองบัญชีปัจจุบัน Codex CLI/configต่างอาจ failclosedตามguard; ไม่มี mcp-list fallback
5. Source implementation และ publication supersede baseline ก่อนหน้าใน handoff/plans เดิม ยึด DECISIONS ข้อ20–21 และ validation ล่าสุด

## บันทึกความคืบหน้า

| วันที่ | ผลลัพธ์ | การตรวจ | งานถัดไป |
|---|---|---|---|
| 2026-10-03 | มี Git scaffold, brief และ decisions; เพิ่มระบบบริหารงานและ Engineering Baseline v1 | ตรวจไฟล์ใน repo, local Git และแหล่งอ้างอิงมาตรฐาน; ยังไม่มี app tests | เริ่ม M1 |
| 2026-10-03 | ตรวจเครื่องมือ, clone/read CodexBar และสรุปช่องทางข้อมูลที่มีเอกสารรองรับ | environment validation + usage-sources; ไม่รัน reference และไม่อ่าน token/เรียก usage จริง | เคาะวิธีเชื่อม แล้วเริ่ม skeleton/provider tests |
| 2026-10-03 | จัดทำแผน MVP 0.1.0 พร้อมงาน T1–T6, dependencies และเกณฑ์รับงาน | QA review เอกสารไม่พบข้อขัดแย้งสำคัญ; ลิงก์/diff ผ่าน; ยังไม่มี source/build | เริ่ม T1–T2 โดยไม่แตะบัญชี |
| 2026-10-03 | สร้าง app/test targets, icon/UI ไทย, model/parser/freshness/state; แก้ findings และตรวจ bundle สิทธิ์ปกติ | 39 XCTest ผ่าน, build ผ่าน, independent review ไม่พบ blocking issue; UI smoke popup ยังรอ | ตรวจ popup แล้วเคาะวิธีเชื่อม T3–T4 |
| 2026-10-03 | เพิ่มข้อความสองค่ายและ native tooltip ในโหมดตัวอย่างแยกจากข้อมูลจริง | build ผ่าน, 39 regression tests ผ่าน, review lifecycle/labeling ไม่พบ blocking issue; preview เห็น Claude 58% / Codex 72% ผ่าน Computer Use | รับ feedback ตัวอย่างและเลือกวิธีเชื่อมจริง |
| 2026-10-03 | ย่อข้อความ Menu Bar โดยคงคำย้ำข้อมูลสมมติใน tooltip/preview; เสนอทางเลือกไอคอน | แก้เฉพาะข้อความและ build number; ยังไม่เปลี่ยนไอคอนหรือเชื่อมบริการ | เลือกไอคอน/รับ feedback ความกว้าง |
| 2026-10-03 | ผู้ใช้เลือกข้อความล้วน; ลบ icon ใน status button และเพิ่ม renderer/selection สำหรับ 1/2 ค่าย | 44 tests ผ่าน, Release build ผ่าน; UI ทดลองเลือก 0/1/2 ค่ายผ่าน Computer Use | รับ feedback ข้อความแล้วเคาะการเชื่อมจริง |
| 2026-10-03 | ผู้ใช้ตรวจว่าดูดีและให้ปิดรอบงาน UI/text example | README/build/tests/ข้อจำกัดบันทึกแล้ว; ยังไม่ถือว่า MVP เชื่อมสองค่ายเสร็จ | รอเปิดรอบเชื่อมจริงหลังอนุมัติวิธี |
| 2026-10-03 | อนุมัติ CLI lifecycle/Claude snapshot; เพิ่ม offline Codex RPC และข้อเสนอ Sandbox bridge | 61/61 tests, Release build 5 และ independent review ผ่านใน offline scope; ยังไม่เรียกบัญชีจริง | ข้อเสนอรอบนั้นถูกแทนด้วย one-app plan ในบันทึกถัดไป |
| 2026-10-03 | ศึกษาและร่าง one-app plan: UI Sandbox + bundled non-Sandbox XPC, Codex real refresh; ถอน manual collector สำหรับ MVP | อ่าน Apple/OpenAI/Anthropic docs และ reference source; ยังไม่ build/run spike หรือเปลี่ยน source/สิทธิ์; Claude review ยังไม่เกิด | ส่งแผนให้ Claude ตรวจ แล้วปรับก่อน implementation |
| 2026-10-03 | เปลี่ยนเป้าหมาย public source-build; ทดสอบ startup ก่อนแก้แผน แล้วตัด XPC/Sandbox spike/provenance/session splitting | CLI 0.160.0 guarded/canary quota result สำเร็จ, MCP positive control ทำงาน, config เดิมไม่เปลี่ยน; ไม่มี app source/build ใหม่ | ส่ง revised plan ให้ Claude review ก่อน S1 |
| 2026-10-03 | รับ Claude review; แก้เอกสาร B1–B4 และ Application Support bridge/user-level settings | Reviewer verified MCP 7 vs config 3 และ guard/Node/notification findings; ยังไม่แก้แอปหรืออ้าง discovery offline | ปิด discovery checks ก่อน S1; เคาะ C3 ก่อน S2 |
| 2026-10-03 | อ่านภาคผนวก Claude review และปรับ discovery หลักเป็น app-server inventory+quota สองรอบ | เอกสารเท่านั้น ไม่มี CLI/network/source changes; layer completeness/no-spawn proof ยังไม่ทำ | พิสูจน์สองเงื่อนไขก่อน S1; C3 เลือก ก แล้ว; ตรวจสองเงื่อนไขก่อน S1 |
| 2026-10-03 | ใช้ sub-agents พิสูจน์ prerequisites ก่อน S1; C3 เลือก กแล้ว | source audit + inventory 5/all-disabled/no-spawn/positive-control 2 และ independent review ผ่าน supported default scope; ไม่ขอ quotaหรือ build/app source ใหม่ | S1 ใน default context ตามข้อจำกัด/placeholder map ที่ตรวจแล้ว |
| 2026-10-03 | ผู้ใช้รับ CLI-only personal use; จัด execution plan 12 ลำดับและ full handoff สำหรับ session ใหม่ทำ S1–S3 ต่อเนื่อง | ตรวจ Git/build config และอ่าน xcresult เดิม 61/0/0 ไม่รัน tests/build ใหม่; ยังไม่ implementation/bridge/publication | รับช่วงจาก MEMORY แล้วเริ่ม S1 ตามแผน |

เพิ่มบันทึกเมื่อจบชุดงานที่มีผลลัพธ์ ไม่ต้องบันทึกทุกคำสั่งหรือซ้ำรายละเอียด changelog

| 2026-10-03 | ทำ S1–S3 ด้วย sub-agents ขนานจริง: Codex guarded two-child/native discovery, Claude native wrapper/installer/snapshot, real UI/coordinator และเตรียม public source | Final113/0/0, Release build6, independent reviewผ่าน; actualทั้งสองproviders/install-exactrestore-reinstall/CmdQ/CPU0.48%RSS96.75MiBผ่านในMacนี้ ข้อจำกัดในvalidation | ใช้งานส่วนตัว/feedback; publicationเมื่อdestination+คำสั่งชัดเจน |

| 2026-10-03 | Owner ยืนยัน kraiwin/AIUsageBar public และให้เผยแพร่ source0.1.0/build6 | Ship gate113/0/0; redacted unrelated private workflow docs; source snapshot84files; GitHubmain readbackตรง2f0fe19 ไม่มี binary/tag; scaffoldเดิมอยู่local-only branch | ใช้งาน/feedback; optional platform/accessibility checksตามข้อจำกัด |

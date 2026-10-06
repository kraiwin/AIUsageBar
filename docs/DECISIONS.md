# AIUsageBar — ค่าตั้งต้นที่ตกลงแล้ว

บันทึก 2026-10-03 — เติมช่องที่ `PROJECT_BRIEF.md` เขียนว่า "ยังไม่ได้สรุป"
ให้ถือเป็นค่าเริ่มต้นสำหรับ MVP หากเจอเหตุผลที่ต้องเปลี่ยน ให้อธิบายและถามผู้ใช้ก่อน อย่าเปลี่ยนเงียบ

## 1. ค่าพื้นฐานของแอป

| เรื่อง | ค่าที่ตกลง | หมายเหตุ |
|---|---|---|
| macOS ขั้นต่ำ | 14 (Sonoma) | ใช้ SwiftUI `MenuBarExtra` ได้ ไม่ต้องพึ่ง AppKit มาก |
| ภาษา/framework | Swift + SwiftUI/AppKit/Foundation/Security ของ Apple | ห้ามเพิ่ม dependency ภายนอก (ไม่มี SwiftPM package ภายนอก) |
| รูปแบบแอป | Menu bar อย่างเดียว ไม่มีไอคอนใน Dock (`LSUIElement = YES`) | |
| Polling | ดึงข้อมูลอัตโนมัติทุก 5 นาที + ปุ่ม "รีเฟรช" ในเมนู | ห้ามถี่กว่า 1 นาที เพื่อไม่โดน rate limit |
| License ของโค้ดเรา | MIT | ใส่ไฟล์ `LICENSE` ตอนเตรียมเผยแพร่ (ขั้นตอนที่ 8) |
| Auto-update / analytics / telemetry | ไม่มี | |

## 2. ที่เก็บ reference (CodexBar)

- Clone ไว้ที่ `reference/CodexBar/` ภายในโฟลเดอร์นี้ — **อยู่ใน `.gitignore` แล้ว** ไม่เข้า Git ของเรา
- เหตุผล: sandbox ของ Codex เขียนได้เฉพาะใน workspace; วางนอกโฟลเดอร์ต้องขอ escalation ทุกครั้ง
- การ clone ต้องใช้เครือข่าย → ขอ escalation ครั้งเดียวตอน clone (ผู้ใช้รู้แล้วและจะกดอนุญาต)
- ใช้คำสั่ง `git clone --depth 1` อ่านอย่างเดียว **ห้ามรัน script/build/`swift build`/`make` ใน reference**
- ห้ามคัดลอกโค้ดจาก reference มาวางตรงๆ — เขียนใหม่จากความเข้าใจ (ดูเรื่อง license ใน brief)

## 3. แหล่งข้อมูล usage — ผลศึกษาและการอนุมัติ

**สถานะ: [อนุมัติวิธีตามข้อ 12 และสถาปัตยกรรมตามข้อ 14 แล้ว; implementation ได้รับคำสั่งตามข้อ 20]** — 2026-10-03 ดู [usage-sources.md](research/usage-sources.md)

- สมมติฐานเดิมว่าไม่มีช่องทางที่มีเอกสารรองรับทั้งหมดไม่ถูกต้อง: Codex มี local app-server RPC สำหรับ rate limits และ Claude Code มี quota ใน status-line JSON
- HTTP OAuth endpoints ใน reference ยังไม่ถือเป็น public supported usage API; ไม่เลือกมาเชื่อมโดยอัตโนมัติ
- ผู้ใช้ยอมรับความเสี่ยงนี้ แต่มีเงื่อนไข:
  - แยกโค้ดดึงข้อมูลแต่ละบริการเป็น provider (โมดูลแยกต่อบริการ) ของตัวเอง เปลี่ยน endpoint ได้จุดเดียว
  - ดึงไม่สำเร็จ / รูปแบบข้อมูลเปลี่ยน → แสดง "โหลดไม่สำเร็จ" พร้อมเวลาที่สำเร็จครั้งล่าสุด
  - **ห้ามแสดงตัวเลขเก่าเหมือนเป็นค่าปัจจุบัน** ห้ามเดา/สร้างตัวเลขเสมือน
  - parse แบบเข้มงวด: field ที่คาดไว้ไม่มี = error ไม่ใช่ 0
- ก่อนเลือกวิธี ให้เขียนสรุปลง `docs/research/usage-sources.md`: endpoint, credential ที่ใช้, field ที่ได้, ข้อจำกัด, ลิงก์อ้างอิง
  แล้วรอผู้ใช้เคาะก่อนเริ่มเชื่อมข้อมูลจริง

## 4. Credential — อ่านอย่างเดียว ใช้สิทธิ์เท่าที่จำเป็น

**สถานะ: [ตรวจ storage จากเอกสาร/reference และ file existence แล้ว; ยังไม่ตรวจ Keychain/บัญชีจริง]** — ไม่เปิดเผย credential values

- Claude: reference รองรับ credential file และ Claude Code Keychain item; เครื่องนี้ไม่พบ `.credentials.json` แต่ยังไม่ได้ตรวจ item หรือ access permission และไม่ได้สรุปว่าไม่มี login
- Codex: เอกสารรองรับ file/keyring/auto/ephemeral; เครื่องนี้พบ `~/.codex/auth.json` โดยตรวจว่ามีไฟล์เท่านั้น ไม่ได้สรุปว่าเป็น active credential store
- วิธี official CLI/status-line snapshot อนุมัติแล้วตามข้อ 12; กฎด้านล่างยังมีผลกับโค้ดของเรา โดยยกเว้นเฉพาะ CLI-managed lifecycle ตามข้อ 12
- กฎ:
  - **อ่านอย่างเดียว** ไม่เขียน/แก้/refresh token ของแอปอื่น (ถ้า token หมดอายุ → แสดง "ต้องล็อกอินใหม่ใน Claude Code / Codex")
  - ไม่ copy token ไปเก็บซ้ำ (ไม่ลงไฟล์, ไม่ลง UserDefaults, ไม่ลง Keychain ของเรา) — อ่านใช้แล้วทิ้งในหน่วยความจำ
  - **ห้ามพิมพ์ token/cookie/header ลง log, console, crash report หรือ tool output** — log ได้แค่ status code และชื่อ endpoint
  - ไม่อ่าน browser cookies ในระยะแรก
  - ปลายทางเครือข่ายทั้งหมดต้องเป็น HTTPS และระบุเป็นค่าคงที่ในโค้ด ตรวจสอบได้จุดเดียว

## 5. ข้อความ UI ภาษาไทย (ตั้งต้น)

| สถานะ | ข้อความ |
|---|---|
| ปกติ | ใช้ไปแล้ว 42% · เหลืออีก 58% · รีเซ็ตเมื่อ จ. 6 ต.ค. 09:00 |
| ไม่มีข้อมูลจากบริการ | ไม่มีข้อมูล |
| ยังไม่ล็อกอิน / token หมดอายุ | ต้องเชื่อมบัญชี — ล็อกอินใน Claude Code (หรือ Codex) ก่อน |
| ดึงไม่สำเร็จ | โหลดไม่สำเร็จ · สำเร็จล่าสุด 10:35 |
| กำลังโหลด | กำลังโหลด… |

ตัวเลขในตัวอย่างเป็นตัวอย่างข้อความเท่านั้น ห้ามใช้เป็นค่าจริงหรือ placeholder ในแอป
เวลาแสดงตามเขตเวลาของเครื่อง รูปแบบวันที่แบบไทย

## 6. โครงสร้างโฟลเดอร์

```
AIUsageBar/
├── AGENTS.md              กฎทำงาน (Codex อ่านอัตโนมัติ)
├── README.md              ภาพรวมสั้นๆ
├── .gitignore
├── docs/
│   ├── PROJECT_BRIEF.md   บริบท + MVP + ขั้นตอน
│   ├── DECISIONS.md       ไฟล์นี้
│   └── research/          ผลศึกษา (สร้างตอนทำขั้นที่ 4)
├── reference/             CodexBar สำหรับอ่าน — gitignored
└── AIUsageBar/            source code ของแอป (สร้างตอนทำขั้นที่ 6)
    ├── App/               entry point, MenuBarExtra
    ├── Providers/         ClaudeProvider, CodexProvider — ดึงข้อมูลแต่ละบริการ
    ├── Models/            โครงข้อมูล usage
    └── UI/                view ภาษาไทย
```

โครงใต้ `AIUsageBar/` เป็นข้อเสนอ ปรับได้ตามรูปแบบโปรเจกต์ที่เลือก (Xcode project หรือ Swift Package) แต่ให้คงการแยก Providers ไว้

## 7. ขอบเขตที่ยังไม่อนุญาต

- ยังไม่สร้าง remote, ไม่ push, ไม่ release, ไม่ notarize/sign เพื่อแจกจ่าย
- งานใดที่ต้องเขียนนอก workspace หรือใช้เครือข่าย (นอกจาก clone reference และเรียก endpoint usage ตอนทดสอบ) → ถามผู้ใช้ก่อน

## 8. การบริหารงานและเอกสาร

เพิ่ม 2026-10-03 ตามคำขอให้จัดการสถานะ ความคืบหน้า changelog และ version อย่างเป็นระบบ

- ใช้ `docs/STATUS.md` เป็นสถานะปัจจุบันและความคืบหน้าแห่งเดียว; ใช้ milestone พร้อมหลักฐานแทนเปอร์เซ็นต์ประมาณ
- ใช้ `CHANGELOG.md` แยก Unreleased ออกจาก release จริง
- ใช้ `docs/DEVELOPMENT.md` กำหนดวงจรทำงาน เกณฑ์เสร็จ การตรวจความปลอดภัยและ release
- ตั้งชื่อแนวทางโค้ดและคุณภาพว่า **AIUsageBar Engineering Baseline v1** ตามคำขอให้มีมาตรฐานอ้างอิง: ใช้ ISO/IEC 25010:2023, NIST SSDF v1.1 และ Swift API Design Guidelines ประกอบเกณฑ์ที่ตรวจได้; ไม่อ้าง certification หรือ full compliance
- เป้าหมายเวอร์ชันแรก `0.1.0`; ยังไม่มี release; เมื่อมี app target เก็บ version/build number ใน build configuration แห่งเดียว
- การจัดระบบนี้ไม่เปลี่ยนขอบเขต MVP หรือข้อจำกัดการเชื่อมบัญชีและเผยแพร่เดิม

## 9. ค่าการทำงานของ T1–T2 (2026-10-03)

ผู้ใช้สั่งเริ่มข้อ 1–2 ของแผน; ไม่ถือเป็นการอนุมัติข้อ 3–4 หรือการเชื่อมบัญชี

- ใช้ native Xcode app/test targets, Swift 6 strict concurrency และ warnings-as-errors; ไม่มี dependency library ภายนอก
- แอปเริ่มด้วย state ว่างของสองค่าย ไม่มี provider transport, subprocess, credential access, polling หรือ network; รีเฟรชถูก disable จนกว่าจะเชื่อมบริการ
- ความสดต้องมี observation time ที่ยืนยันได้; receipt/file-read time เพียงอย่างเดียว = snapshot ที่ยังยืนยันความสดไม่ได้
- policy ของ core รับ maxAge จาก caller; UI เตรียมใช้ 5 นาทีตาม polling เดิม โดยยังไม่มีข้อมูลจริงเข้ามาและยังไม่เปิด polling
- เมื่อ reset ผ่านไปซ่อน quota เก่าและรอรอบใหม่; เมื่อ timestamp ผิดซ่อนตัวเลขทุกสถานะ รวม loading/failure
- เมื่อ refresh ล้มเหลวเก็บค่าเดิมได้พร้อมข้อความล้มเหลว/ข้อมูลครั้งก่อน; ไม่เลื่อนเวลาสำเร็จของ provider เมื่ออ่าน snapshot ซ้ำ
- app bundle ปกติใช้ local ad-hoc signing และ App Sandbox ที่ไม่มี network entitlement; ไม่ฉีด base debug entitlements ใน Release
- Debug test host ใช้สิทธิ์ชั่วคราวที่ Xcode เพิ่มสำหรับ XCTest; ไม่ใช้ test-host bundle เป็นชุดทดลองปกติ และไม่อ้างว่า local ad-hoc build ได้ notarize/hardened-runtime certification
- วิธีเชื่อมจริงและ CLI-managed refresh ยังรออนุมัติตามข้อ 3–4 เหมือนเดิม

## 10. ตัวอย่างข้อความและ hover (2026-10-03)

ผู้ใช้สั่งให้ทำตัวอย่างโดยแสดงตัวเลขสมมติ และย้ำว่ายังไม่เชื่อมบริการ คำสั่งนี้อนุญาตข้อยกเว้นจากกฎห้ามแสดงตัวเลขจำลองเฉพาะโหมดตัวอย่างที่ระบุชัด

- เปิดเฉพาะด้วย `--demo`; ค่า Claude เหลือ 58% / Codex เหลือ 72% อยู่ใน UI example type แยกจาก model/provider/store/cache
- ผู้ใช้ขอตัด “ตัวอย่าง · เหลือ” จาก Menu Bar เพื่อลดความกว้าง จึงเหลือ `Claude 58% · Codex 72%`; tooltip และ preview ยังติดป้ายตัวอย่าง/ข้อมูลสมมติ และไม่อ้างเวลา fetch หรือยอดบัญชีจริง
- ใช้ AppKit `NSStatusItem`/`button.toolTip`/variable width โดยตรง และยังใช้ SwiftUI สำหรับเนื้อหาเมนู; ไม่เพิ่ม dependency
- โหมดตัวอย่างเปิดหน้าต่าง preview เพื่อให้ดู layout และลอง hover ข้อความบน Menu Bar ได้; โหมดปกติยังเป็น Menu Bar-only ไม่เปิดหน้าต่างนี้
- ค่าเลือกโหมดไม่บันทึกลง settings; ออกจากแอปแล้วเปิดโดยไม่ใส่ argument จะกลับสู่โหมดปกติ
- ยังคง App Sandbox ไม่มี network entitlement; ไม่อ่าน credential ไม่เรียก CLI/server และไม่อนุมัติวิธีเชื่อมจริงจากการทดลองนี้
- อัปเดต: ผู้ใช้เลือกข้อความชื่อเต็มบรรทัดเดียวและตัดไอคอนซ้ายสุดออกตามข้อ 11

## 11. Text-only และจำนวนบริการ (2026-10-03)

- ผู้ใช้เลือกชื่อเต็มบรรทัดเดียว ไม่มีไอคอนด้านซ้ายบน Menu Bar; app bundle icon ใน Finder ไม่ใช่ส่วนที่ขอเปลี่ยน
- ตัวสร้างข้อความรับรายชื่อบริการที่จะแสดง; รองรับ Claude อย่างเดียว, Codex อย่างเดียว หรือทั้งสอง โดยไม่แสดงค่าของบริการที่ไม่ได้เลือก
- ไม่มีข้อมูลใช้ `—`; 0% แสดง `0%` ตามจริง; ค่า invalid ไม่เป็นตัวเลข quota และกรณีไม่มีบริการใช้ `AIUsageBar` เพื่อให้เมนูยังเรียกได้
- โหมดตัวอย่างมี checkbox ทดลองเลือกค่ายใน memory เท่านั้น; title, tooltip และแถวใน preview/menu ใช้ selection เดียวกัน
- เมื่อเชื่อมจริงในอนาคตจึงนำรายการบริการที่เปิดใช้งาน/เชื่อมตามวิธีที่อนุมัติมาให้ renderer; ตอนนี้ยังไม่อ่าน login/credential หรือเลือกค่ายจริงอัตโนมัติ
- ไม่เลือกการแสดงสองบรรทัด หลอด ถัง หรือ vendor icons จาก mockups/คำปรึกษานี้

## 12. อนุมัติวิธีเชื่อมข้อมูลจริง (2026-10-03)

ผู้ใช้ตอบ “go” หลังข้อเสนอเชื่อม Codex ก่อน Claude ใน session รับช่วง handoff:

- อนุมัติ Codex ผ่าน official CLI app-server; แอปไม่รับหรือเก็บ token แต่ให้ official CLI จัดการ/refresh credential ของตัวเองตามปกติ เป็นข้อยกเว้นเฉพาะ CLI-managed lifecycle จากข้อ 4
- อนุมัติ Claude status-line snapshot; ยอมรับว่าข้อมูลอาจเก่าเมื่อไม่ได้ใช้งาน Claude Code และปุ่มรีเฟรชอ่าน snapshot ล่าสุดเท่านั้น
- ยังต้องรักษา status line เดิม ทำ preview/backup ก่อนติดตั้ง และขอ escalation สำหรับการเขียนนอก workspace
- ไม่ถือว่าอนุมัติการปิด App Sandbox, เพิ่มสิทธิ์ทั่วไป, remote listener, publish/push/release หรืออ่าน token โดยตรง; ต้องตรวจ architecture/side effects ก่อนบัญชีจริง

## 13. แผนเปิดแอปเดียวและการรีวิว (2026-10-03) — ข้อเสนอ XPC ถูกแทนด้วยข้อ 14

- ผู้ใช้ให้ศึกษาทางเลือกและเขียนแผนก่อน แล้วจะให้ Claude ช่วยรีวิว ยังไม่สั่ง implement architecture ใหม่ในรอบนี้
- ถอน manual Terminal collector + Codex refresh แบบอ่าน snapshot จากคำแนะนำ MVP เพราะไม่ตรงประสบการณ์เปิดแอปเดียวและ Codex refresh ดึงยอดใหม่
- แผนสำหรับรีวิวคือ [one-app integration plan](plans/2026-10-03-one-app-integration-plan.md): UI คง Sandbox และเสนอ embedded non-Sandbox XPC service พร้อม offline spike/permission gates **(ข้อเสนอรอบนั้นถูกแทนด้วยข้อ 14; ลิงก์ไฟล์เดิมตอนนี้เป็นแผนใหม่)**
- ข้อเสนอไม่ใช่การอนุมัติ service/ingest target ที่มีสิทธิ์ user กว้าง, signing/notarization, global install หรือ account smoke; ไม่มีการเปลี่ยนสิทธิ์หรือ Swift/Xcode ในรอบแผน
- official CLI-managed refresh และข้อจำกัด Claude snapshot ที่อนุมัติในข้อ 12 ไม่ต้องขอซ้ำ; session/account metadata policy และ visible snapshot marker ยังให้ reviewer ท้าทายก่อนสรุป

## 14. GitHub public source-build และ single non-Sandbox app (2026-10-03)

ผู้ใช้เปลี่ยนเป้าหมายการแจกจ่ายและสั่งทดสอบ side effects ก่อนแก้แผน แล้วส่ง Claude รีวิวอีกรอบก่อน implementation ข้อนี้แทนข้อเสนอ XPC ในข้อ 13 และ baseline Sandbox ในข้อ 9 สำหรับงานเชื่อมจริง

- เป้าหมายคือ GitHub **public source repository** ให้ผู้ใช้ build เองด้วย Xcode ไม่เข้า App Store ไม่ notarize ไม่สมัคร Apple Developer Program และไม่ตั้ง Developer ID/Team ID เป็น prerequisite
- ใช้แอปเดียวที่ไม่เปิด App Sandbox แต่เปิด Hardened Runtime/local ad-hoc signing โค้ดเรียก Codex CLI อยู่ใน module แยก API แคบ ไม่มี runCommand/readFile ทั่วไป ตัด XPC และ P1 XPC spike/peer signing/provenance framework
- ก่อนงานอื่นทดสอบ MCP/hooks/notify ของ app-server quota flow แล้ว: [controlled startup probe](validation/2026-10-03-codex-startup-side-effects.md) ได้ result จริง ไม่พบ canary invocation ใน quota-only path; MCP positive control ทำงาน และ child guards ไม่แก้ config เดิม ไม่ถือว่ารับรองทุก config/CLI version
- Codex path ค้นหาอัตโนมัติครั้งแรก แล้วให้ผู้ใช้ confirm หรือเลือกเอง ถ้าเปิดไม่เจอแจ้งตรง ๆ ไม่ทำระบบตรวจ provenance
- Claude ใช้ snapshot ที่ได้รับล่าสุด พร้อมป้าย “จาก Claude Code ล่าสุด เวลา X”; X เป็นเวลารับ ไม่อ้างว่าเป็น server fetch หรือบัญชีปัจจุบัน ไม่ทำระบบแยก sessions/raw session ID/pseudonym
- Claude installer preview/backup/rollback ยังคงจำเป็นและทำหลัง Codex ใช้ได้ รักษา statusline เดิมและขอ filesystem escalation เมื่อติดตั้งนอก workspace
- README ต้องมีขั้นตอน Xcode source build, no-notarization disclosure และไฟล์/โปรแกรมที่แอป/CLI แตะ แยก actual build 5 จากพฤติกรรมที่จะทำ
- เป้าหมาย public repo อนุมัติแล้ว แต่คำสั่งรอบนี้คือ probe/แก้แผน/ส่งรีวิว **ยังไม่ create remote/push/publish หรือ implement** โค้ด/build 5 ปัจจุบันยัง Sandbox/offline ไม่เปลี่ยน entitlements จากการแก้เอกสาร
- อนุมัติ CLI-managed credential refresh ตามข้อ 12 ยังมีผล ไม่ถามซ้ำ; ไม่มีอนุมัติให้โค้ดเราอ่าน/เก็บ token/cookies หรือเพิ่ม model turns เพื่อกระตุ้น usage
- module boundary ไม่เท่ากับ OS Sandbox: ยอมรับว่าแอปมีสิทธิ์ user ปกติ และ CLI มี network/auth/state side effects ตามที่ตรวจ ต้องอธิบายใน README ไม่อ้าง runtime host firewall จากการ audit config

แผนล่าสุด: [one-app source-build integration](plans/2026-10-03-one-app-integration-plan.md)

## 15. รับ Claude review และแก้แผนก่อน Codex S1 (2026-10-03)

- รีวิว [one-app plan review](plans/2026-10-03-one-app-integration-plan-review.md) ยืนยันทิศทางเดิม ไม่เปลี่ยน architecture; รอบนี้แก้เอกสาร B1–B4 ก่อนเริ่ม source implementation
- MCP discovery ใช้ native `codex mcp list --json` พร้อม guards รอบแรก แล้ว inline disable map และ list รอบสองยืนยันทุกตัว disabled ไม่เขียน TOML/config-layering parser เอง และต้องตรวจ source/network/keyring/spawn/canary ก่อนนำไปใช้
- หลัง app-server initialize ต้อง config/read และ assert effective guards ก่อน quota เพราะ unknown guard อาจถูกข้ามเงียบ ๆ [source audit](research/2026-10-03-mcp-discovery-source-audit.md) พบ config/read echo unknown bool feature ได้ จึงเพิ่ม experimentalFeature/list จาก registry/runtime state ในแผนร่วมกับ config/read; mcp list มี HTTP OAuth/keyring paths ไม่อนุมัติให้ถือว่า local-only ต้องปิดขอบเขต/วิธีจำกัดก่อนใช้จริง
- Notification valid ที่ไม่มี id ให้ discard รวมชื่อใหม่ที่ไม่รู้จัก ยกเว้น disguised token-refresh และ malformed envelope response/server-request ที่มี id ยัง strict
- npm/nvm resolution เลือก native binary ภายใน package ให้ผู้ใช้ยืนยัน ไม่รันผ่าน Node/custom shell shim ไม่มี provenance framework
- Claude native helper/wrapper ติดตั้งใน ~/Library/Application Support/AIUsageBar/ หลัง Codex ใช้ได้ installer user-level เฉพาะ statusLine.command คง padding/keys อื่นพร้อม preview/backup/rollback และไม่แก้ project overrides
- **C3 ยังรอ owner เลือก:** แนะนำ ก—valid latest input ที่ไม่มี quota เขียน no-data snapshot ทับค่าเดิมให้ UI ขึ้น —; ยังไม่บันทึกว่าได้รับอนุมัติจากผู้ใช้ ข้อนี้เป็นก่อน S2 ไม่ขวางการแก้แผน Codex
- รอบนี้ไม่แก้ Swift/Xcode/entitlements ไม่ติดตั้ง bridge และไม่ publish/push

## 16. ภาคผนวก Claude review — เปลี่ยน discovery เป็นสอง app-server child (2026-10-03)

- อ่านภาคผนวกไฟล์ review เดิมแล้ว: Claude ยืนยัน source findings เรื่อง mcp list auth-status/network และ config/read unknown feature echo
- แผน B1 ใหม่แทนข้อเสนอ list×2 ในข้อ 15: guarded app-server child แรกอ่าน config inventory ไม่ขอ quota แล้ว child ที่สองใช้ disable map + registry/config assertions ก่อน quota
- ยังเป็นข้อเสนอในเอกสาร ต้องพิสูจน์ config/read ครบ effective layers และ no-spawn canary ของ registry/config flow ใน child แรกก่อนใช้จริง ผล probe เดิมไม่แทนสอง checks นี้
- mcp list ไม่เป็น workflow หลัก; fallback มี HTTP/keyring effects ต้องเสนอ owner หากสอง checks ไม่ผ่าน ยังไม่ได้อนุมัติ fallback
- C3 missing-quota ของ Claude ยังรอ owner (แนะนำ no-data → —) ไม่อนุมานว่า review appendix เป็นการตอบข้อ ก/ข
- รอบนี้อ่าน/แก้เอกสารเท่านั้น ไม่รัน CLI/network/canary ไม่แก้ source/entitlements/global settings ไม่เริ่ม S1

## 17. Owner เลือก C3 และให้ตรวจสองเงื่อนไขก่อน S1 (2026-10-03)

- ผู้ใช้เลือก **ก**: valid latest Claude input ไม่มี quota ให้เขียน no-data snapshot ทับค่าเดิม และ UI แสดง — ป้องกันตัวเลขจากบัญชีเดิมค้าง ไม่เปลี่ยน missing เป็น 0
- malformed/invalid input ยังเป็น error แยกจาก valid no-data; ไม่ยกข้อมูลครั้งก่อนเป็นค่าปัจจุบัน
- ผู้ใช้สั่งให้ตรวจ config inventory layer completeness/no-spawn RPC ก่อนเริ่ม S1 โดย sub-agent จริง
- ตรวจสองเงื่อนไขก่อน S1 ในรอบนี้; ไม่ถือว่าอนุมัติ fallback mcp list ที่มี HTTP/keyring effects, public push หรือ bridge install

## 18. ผล prerequisite default context (2026-10-03)

- config/read+registry+canary สองเงื่อนไขผ่านใน CLI0.160.0 default direct app-server context ตาม [validation](validation/2026-10-03-codex-inventory-prerequisites.md); user/project/CLI MCPครบ 5, รอบสอง disabled ทุกตัว, no-spawn flow 0 marker และ positive control 2 marker
- ผลไม่ครอบคลุม named v2 profile/enterprise cloud or MDM runtime ทุกแบบ; direct app-server entrypoint ไม่ forward --profile-v2 ใช้ supporteddefaultcontext และ fail closed เมื่อ invariants ไม่ตรง
- disabled map ต้องเติม transport placeholder ที่ปิดไว้เพื่อ bootstrap project-only MCP: stdio=/usr/bin/false, HTTP=https://example.invalid; ไม่คัดลอก secret/transport arguments เดิม ต้อง validate kind
- remote_control feature ของรุ่นนี้ Removed/no-op ไม่ถือเป็น functional guard; final pair ไม่ฉีด fake key จึงไม่อ้าง unknown-guard negative test ผ่าน
- prerequisite work นี้ไม่มี quota/model/thread ใหม่และยังไม่แก้ Swift/Xcode/entitlements ไม่ build แอปใหม่ ไม่ install bridge/publish/push

## 19. แผนทำ S1–S3 ต่อเนื่องใน session ใหม่ และ CLI-only ใช้ส่วนตัว (2026-10-03)

- ผู้ใช้ยอมรับข้อจำกัดว่าต้องใช้ Codex CLI/Claude Code และให้มุ่งแอปที่ใช้เองบน Mac นี้ ไม่ขยายช่องทาง browser-only หรือ enterprise/profile support ก่อน MVP
- ผู้ใช้ขอแผนงานเป็นไฟล์เพื่อเปิด session ใหม่ แล้วให้ทำทุกลำดับต่อเนื่อง จัดไว้ใน [S1–S3 execution plan](plans/2026-10-03-s1-s3-execution-plan.md) พร้อม prompt เริ่ม implementation
- รอบนี้เขียนแผนและ full handoff เท่านั้น; session ใหม่เมื่อได้รับ prompt เริ่มงานให้ทำ implementation/tests/account smoke/review/docs ตามแผน ไม่ถามทำต่อซ้ำทีละเฟส แต่ยังใช้ filesystem/tool approvals ตามจริง
- เป้าหมาย public source-build เดิมไม่ถูกยกเลิกจากการยอมรับ personal CLI-only; เตรียม source ให้พร้อมหลัง S1–S3 ปลายทาง GitHub/คำสั่ง publication ยังต้องชัดก่อน create/push และไม่ทำ publication ในรอบ handoff นี้
- ไม่รับประกันว่าไม่มีจุดหยุด: CLI/config/test failures, OS/Keychain/tool permissions และ GitHub destination ที่ขาดเป็น blockers จริง ให้ทำส่วน independent ต่อและถามเฉพาะสิ่งจำเป็น ไม่ข้าม guards หรืออ้างเสร็จเพื่อให้ดูทำรวดเดียว
- ไม่อ้างว่าปิด session แล้ว Codex ยังทำต่ออยู่; เริ่มต่อใน session ใหม่จาก state/หลักฐานล่าสุด


## 20. ลงมือ S1–S3 และ local personal install (2026-10-03)

- ผู้ใช้สั่ง session นี้ให้ทำ implementation/tests/review/docs ต่อเนื่องตาม execution plan ใช้ sub-agents ขนานจริง และเตรียม public source โดยไม่ถามทำต่อทุกขั้น จึงแทนข้อความรอบแผนเดิมที่รอเริ่ม implementation
- ทำ Codex native two-child provider, Claude native bridge/installer และ UI/coordinator ตามข้อ 14, 17–19 แล้ว; official CLI ยังเป็นเจ้าของ auth lifecycle แอปไม่อ่าน credential/browser cookies
- ติดตั้ง Claude bridge ระดับผู้ใช้จาก exact UI preview ที่เก็บ bytes เดิม พร้อม backup/compare-before-write; ตรวจ restore settings ตรง bytes เดิมและติดตั้งกลับเพื่อใช้งานส่วนตัวบน Mac นี้
- Optional `--show-window` แสดง live menu เดียวกันเพื่อ keyboard/accessibility inspection; ค่าเริ่มต้นยังเป็น Menu Bar ไม่มี Dock icon ไม่เปลี่ยนเป็น demo
- UI/snapshot check ทุก 5 วินาที; Codex polling ยังคงทุก 5 นาที และ request อย่างน้อย 1 นาทีตามข้อ 1 ตัวเลขใน status title หมายถึงเหลือรายสัปดาห์ มี `*` สำหรับ snapshot และ `~` สำหรับค่าครั้งก่อน
- ไม่สร้าง remote/commit/tag/push/publish ในงานนี้; public source preparation ไม่ใช่การเผยแพร่ ข้อจำกัด runtime/ผลจริงอยู่ใน STATUS และ validation ล่าสุด


## 21. Owner ยืนยัน public source publication (2026-10-03)

- ผู้ใช้ระบุ owner เป็นตนเอง และตอบ “go” ต่อข้อเสนอ `kraiwin/AIUsageBar` visibility public, README ภาษาไทย จึงอนุญาต create repo/remote, commit และ push source ไปปลายทางนี้ แทนข้อห้ามเฉพาะรอบก่อนหน้า
- Source version แรกคง `0.1.0` build6 ตามแผน ยังไม่มี release ก่อนหน้า ไม่สร้าง tag/GitHub Release/binary และไม่ notarize
- Gate รอบ ship รัน XCTest ใหม่113/0/0 โดยใช้ offline synthetic tests; ไม่เรียก usage/account integration ซ้ำระหว่าง gate
- ก่อนเผยแพร่ตัดรายละเอียด config-backup/workflowส่วนตัวที่ไม่เกี่ยวกับแอปออกจาก public docs; ต้นฉบับ localเก็บใน gitignored `build/PublicationPrivateDocs/`
- Public history เริ่มจาก source snapshot ที่ตรวจแล้ว; เก็บ scaffold history เดิมใน local-only branch เพื่อไม่เผยรายละเอียดโปรเจกต์อื่นใน commitเก่า ไม่มี reset/amend/force-push


## 22. ส่งต่องาน Windows บน PC ใน repo เดิม (2026-10-05)

- ผู้ใช้ขอ handoff สำหรับ PC และสั่ง /ship-no-ci เพื่อให้ clone/pull ไปทำต่อ อนุมัติเฉพาะเอกสารรับช่วงและ commit/push ไป origin/main ของ kraiwin/AIUsageBar ในรอบนี้
- ทิศทางคือ Windows edition ใน repo เดิม แยก windows/ โดยคง Mac source/Xcode layout ไว้ เริ่มตรวจ native Windows/WSL และเขียนแผนบน PC ก่อนimplementation
- Windows stack/minimum OS/CLI context/installer/signing/dependenciesยังไม่สรุป; Swift/Apple baselineยังใช้กับMac ไม่อนุมานว่าใช้กับWindowsได้ตรง ๆ
- ส่งต่อ docs-only ไม่ bump0.1.0/build6 ไม่สร้างtag/binary/GitHub Release หรือแก้Mac app/bridgeที่ใช้งานจริง [skip ci]เป็นconventionไม่รับรองทุกworkflowหยุด

## 23. ข้อเสนอ Windows MVP จาก PC (2026-10-05) — ยังรอ owner เลือก

- ผู้ใช้สั่งอ่าน MEMORY/handoff ล่าสุด ตรวจ native CLI/WSL และเขียนแผนเสนอ stack/ขอบเขตก่อน implement; รอบนี้เป็นงานเอกสาร ไม่ใช่คำสั่งลงมือ source
- พบ Windows 11 x64, .NET SDK10.0.401/Desktop runtime10.0.12, native Codex0.160.0 และ Claude2.1.289; WSLมีเพียง docker-desktop ดู [environment evidence](validation/2026-10-05-windows-environment.md)
- **เสนอ** C#/.NET10 + WinForms NotifyIcon, icon/tooltip/menuภาษาไทย, native Windowsก่อน (Codex quota + Claude latest snapshotผ่าน Git Bashที่ยืนยัน); เลื่อนWSL/PowerShell statusline/Windows10/ARM64/installer/autostart/signing/binary release
- Sourceใหม่แยก windows/ คงMac source/tests/Config/Xcodeไว้เหมือนเดิม ไม่สร้างshared compiled core ไม่เพิ่มthird-party NuGetในข้อเสนอแรก
- [Windows MVP plan](plans/2026-10-05-windows-mvp-plan.md) ระบุ process/job cleanup, native guards, encoding/ACL/atomic replacement, shell/preview/restore และtest gates; version/helpไม่ใช่compatibility/account proof ต้องตรวจisolated probesก่อนreal quota/settings
- Stack/support contextและimplementationยังไม่อนุมัติ ไม่มีWindows build/testหรือbridge installในรอบวางแผนนี้
- Native Codex CLI review พบ5blockers/2suggestions แก้แผนและบันทึกdispositionครบ; delta re-reviewให้ proceed for PLAN ไม่มีmajor/minorใหม่ แต่launcher/guards/filesystemยังต้องพิสูจน์จริง ไม่ใช่ผลWindows testผ่าน แนะนำเริ่มอนุมัติเฉพาะW1–W2offline/isolatedก่อน

## 24. Owner อนุมัติ Windows W1–W2 (2026-10-05)

- ผู้ใช้ตอบ “go” ต่อสรุปรับช่วงและข้อเสนอเริ่ม W1–W2 จึงอนุมัติ C#/.NET10/WinForms, native Windows11 x64, tray icon/tooltip/menuภาษาไทย แยก source ใต้ windows/ คง Mac source/Xcode เดิม
- อนุมัติ offline tray/core/tests และ isolated native compatibility/process/shell/filesystem probes ตามแผนที่ผ่าน review แล้ว ไม่มี third-party NuGet ในขอบเขตนี้
- W3 real account quota และ W4 live bridge/user settings writes ยังไม่อยู่ใน scope; ไม่รวม autostart/release/commit/push
- plan-gate ข้ออนุมัติแผนผ่านแล้ว; เสนอทีม backend-dev/worker/qa-engineer และรอ owner ยืนยัน dispatch gate ที่สองก่อน source implementation

## 25. Owner อนุมัติ dispatch Windows W1/W2a (2026-10-05)

- ผู้ใช้ตอบ “go” ต่อข้อเสนอทีม backend-dev(core/process/inert sink), worker(tray/build/icon), qa-engineer(tests/synthetic experiments) จึงผ่าน dispatch gate ที่สอง
- ลงมือเฉพาะ W1/W2a offline/fake/synthetic ตาม corrected plan/conditional Claude review และ lead self-check; native W2b harness ทั้งการเขียนและรันรอ C1 audit ครบก่อน
- ไม่มีการอนุมัติ real Codex/Claude invocation, account quota, compatibility import, production bridge/settings/install, autostart, release, commit หรือ push ใน dispatch นี้

## 26. Owner อนุมัติ C6 plan และ continuous team dispatch (2026-10-05)

- ผู้ใช้ตอบ `go` หลังรับช่วง C6 plan-ready จึงอนุมัติแผน C6 ที่ผ่าน actual Codex CLI review แล้ว และข้อความถัดมาขอไปต่อรวดเดียวพร้อมแตก sub-agents จึงยืนยัน dispatch gate ที่สองสำหรับ C6
- backend-dev รับผิดชอบ PrivateFiles.cs/Core.csproj; qa-engineer รับผิดชอบ ClaudeTests.cs; lead รับผิดชอบ integration, independent review และเอกสาร ทำ implementation/tests/review/docs ต่อเนื่องในขอบเขตสาม source files ตามแผน
- C1/C5 ยังคง blocked และ strict เดิม ไม่มี native/account/settings/install/dependency/commit/push/release permission เพิ่ม

## 27. Owner อนุมัติ C1/C5 read-only team study (2026-10-05)

- ผู้ใช้ตอบ `go` ต่อข้อเสนอให้ทีมศึกษา C1 isolation และ C5 bridge แบบ read-only แล้วเสนอแผนพร้อมข้อแนะนำก่อนลงมือ
- แยกทีม C1 pinned-source/isolation, C5 architecture และ independent skeptic; เขียนผลศึกษา/decision plan ได้ แต่ไม่แก้ app/harness source ไม่รัน real CLI/account/context probe ไม่ติดตั้ง/enable OS/toolchain หรือเขียน settings
- เกณฑ์ C5 ทุก descendant <=2s และ caller exit/stdout/stderr EOF overhead <=500ms คงเดิม; C1/C5 ยัง blocked จนมีหลักฐานและการอนุมัติขั้นถัดไป ไม่ถือการศึกษาเป็น implementation/dispatch/installation permission

## 28. Owner เลือก host-test planning only, ไม่มี VM (2026-10-05)

- ผู้ใช้แจ้งว่าไม่มี VM และสั่ง “go เตรียมแผนทดสอบบนเครื่องนี้แบบจำกัดก่อน ยังไม่รันทดลองหรือใช้บัญชีจริง” จึงเลือกเตรียมแผนบน Windows host นี้แทน guest branch
- อนุมัติ architect/skeptic read-only planning และ actual Codex CLI model-review workflow ตาม plan-gate; ไม่ใช่ native app-server/Claude compatibility invocation หรือการใช้บัญชีจริงของแอปทดลอง
- ยังไม่อนุมัติแผนฉบับใหม่/source dispatch/build/test/probe/install/settings/account/quota. แผนต้องระบุ preflight, effects, exact scope, recovery limitations และ stop gates โดยไม่อ้าง scratch/job เป็น isolation หรือรับรอง rollback ของ network/auth effects
- C1/C5 blockers และ strict2s/500ms คงเดิม จนมีหลักฐานหรือ owner เปลี่ยนขอบเขตอย่างชัดเจน

## 29. Owner สั่งดำเนิน H0 ตามแผน reviewed (2026-10-06)

- ผู้ใช้สั่ง “ลุย h0” หลังส่งมอบแผน H0 in-memory ที่ actual Codex CLI ให้ PROCEED จึงอนุมัติการเตรียมคำสั่ง ตรวจ source/parser/hash และรัน H0 หนึ่งครั้งในขอบเขตนั้น
- main/QA เตรียมคำสั่งในหน่วยความจำและตรวจรับก่อน execution; เป็น tooling ไม่มี app source หรือ filesystem implementation artifact และไม่ขอ dispatch ซ้ำสำหรับคำสั่ง H0 ที่สั่งให้ดำเนินแล้ว
- อนุญาตเฉพาะ OS privilege/known-folder/target metadata และ fixed14-field result. ไม่อ่านเนื้อหา config/auth ไม่เรียก provider CLI/บัญชี/quota ไม่ SDK/build/install/settings/commit/push. H1/H2 และ C1/C5 ยัง gated; ผล metadata ไม่ใช่ native integration ผ่าน

## 30. Owner อนุมัติแก้ guard และ H0 replay หนึ่งครั้ง (2026-10-06)

- ผู้ใช้สั่งตรวจ root cause/แก้ guard/ให้ทีม review/รัน H0 ใหม่หนึ่งครั้งในขอบเขตเดิม จึงอนุมัติ bounded guard-only token diagnostic และ corrected metadata replay หลัง self-gate
- รัน diagnostic เปรียบเทียบ capacity บน token handle เดียว ไม่อ่านค่าจาก bufferหรือ config; พบ TokenElevation4096 fail24/4ผ่าน จึงแก้เฉพาะcapacityชนิดนั้น. Native GPT author/reviewer ตรวจdeltaก่อน parser/hashและ corrected H0หนึ่งครั้ง
- H0 replay ได้ metadata-clear แต่ nativeAuthorized=false/C1C5blocked; ไม่มีการอนุมัติ H1/H2/provider/account/quota/setup/source publication เพิ่ม

## 31. Read-only C1 continuation หลัง H0 (2026-10-06)

- ผู้ใช้ตอบ go หลัง H0 replay metadata-clear; เดินงานถัดไปตาม STATUS โดยปิด source paths/binary distribution metadata และเตรียม native-contract decision เท่านั้น ไม่อนุมานว่าอนุมัติ H2 หรือบัญชีจริง
- Source/distribution readers และ native GPT skeptic ตรวจ startup/plugin/model/initialize/registry/platform/transitive paths; เขียนรายงานที่แยก conditional source evidence จาก runtime และตรวจ hash ของ source dependencies ที่ใช้
- ส่งคำถาม optional เลือก strict isolation หรือ bounded inventory evidence; ยังไม่มี owner choice/approval ในรอบนี้ การเตรียมแผนไม่ใช่ native/source dispatch/account/network-effect permission

## 32. Owner เลือก bounded H2 inventory planning (2026-10-06)

- ผู้ใช้เลือก “H2 แบบจำกัด เตรียมแผนต่อ” จึงเลือกขอบเขตหลักฐาน bounded inventory สำหรับแผนขั้นถัดไป ไม่คงข้ออ้าง strict isolation/zero effects ที่ยังพิสูจน์ไม่ได้บน host
- แผนต้องเปิดเผย shared Windows token, ordinary OS known-folder/proxy/public-root reads, approved scratch state และ possible unauthenticated early network attempts; ส่วนที่ไม่มี observation คง inconclusive. Provider secrets/account/auth/quota/turn/tool/MCP/statusline/settings mutation ยังห้าม
- อนุมัติเฉพาะ delegated architectural design/source reads/actual Codex CLI plan review และเอกสาร. ยังไม่ source implementation/build/test/native execution/account/install/network-policy/commit/push. Preparation และ native run ต้องแยก approval; การเลือก contract ไม่ใช่ C1/C5 ผ่าน
- ใช้ research-agent thread เดิมทำ architectural design และ CLI-review routing เพราะ thread limit; ไม่อ้างว่า spawn architect/codex-reviewer role ใหม่สำเร็จ หรือ native GPT เป็น actual CLI review

## 33. Owner อนุมัติ H2 preparation plan และ dispatch (2026-10-06)

- ผู้ใช้ระบุ “อนุมัติแผนและ dispatch H2 preparation” จึงผ่านทั้งสอง gates สำหรับแผนที่ actual Codex CLI ให้ PROCEED แล้ว
- ลงมือเฉพาะ3sourcefiles: CodexProvider.cs internal H2method mode; H2InventoryTests.cs policy/parser/coordinator/fake fixtures/tests; Program.cs offline/fake-H2 routing. รวม self-gate/build/focused/full offline/review/docs โดย existing stack ไม่มีdependency/restore/installเพิ่ม
- Reuseทีมเดิม: c1_host_contractรับCore delta, c1_studyรับnewtests+runner, host_plan_safetyรับread-onlyreview, lead integration/run/docs. ไม่ใช่roleagentใหม่ ทุกคนรักษางานคนอื่น
- NativeH2/provider/บัญชี/quota/CLIcompatibility/H1/bridge/settings/autostart/commit/push/release ยังไม่อนุมัติ ไม่เพิ่มnative routeแฝงจากpreparation

- Execution evidence for decision33: preparation completed in the approved three-file scope. Release build0warnings/errors, focusedH2 26/26, full offline110/110 (0failed/0skipped); independent native GPT review accepted. Initial failures and corrections are retained in [H2 validation](validation/2026-10-06-windows-h2-preparation.md). Native approval boundary is unchanged.

## 34. Read-only native H2 readiness planning after preparation (2026-10-06)

- Owner replied `go` after preparation completion and the proposed next step: provider-secret-path closure and observation planning. Continue delegated source research, exact evidence manifests, skeptical review and actual Codex CLI plan-review workflow.
- This authorization does not launch native H2 or read real provider account/config/auth contents. No source implementation, SDK/build/test, installation, OS/network policy, settings, commit/push/release in this round.
- A concrete later amendment must retain separate source/dispatch/run gates, unknown-path stops and calibrated inventory-versus-access coverage. Existing research threads are reused; no new role agent is asserted.

- Execution checkpoint for decision34: selected source investigation/observation/readiness contract complete.14 source hashes verified; actualCLI first1Major/2Minor/1Info -> deltaPROCEED for READINESS CONTRACT,0newfindings/exit0. Nine loader contexts/fallback, image-object/launch-path binding and review scope are explicit. No native/source approval follows; next concrete native fixture/identity/context/validator implementation plan and owner gates remain separate. [Readiness review](plans/2026-10-06-windows-h2-native-readiness-review.md) records provenance and source/runtime limits.

## 35. Concrete native H2 implementation planning (2026-10-06)

- Owner replied `go` after the reviewed readiness contract and proposed next step: native fixture/guard/identity implementation PLAN. Authorize delegated architectural design, bounded pinned-source contracts, saved implementation plan and actual Codex CLI plan review.
- No app source, SDK/build/tests, fresh real H0 or native invocation is authorized in this planning round. No real provider account/config/auth contents, installation, OS/network policy, settings, commit/push/release.
- An actual architect agent now designs the plan; existing source/identity readers supply contracts, lead alone writes the saved plan. Reviewer routing may reuse an existing contributor thread if the old reviewer is unavailable; actual CLI review is separately recorded and never replaced by contributor comments.
- The concrete source amendment may propose an explicit test-runner native entry, immutable reviewed manifest and single-attempt discipline only after owner plan AND dispatch approval. Build/fake tests do not authorize invoking it. Owner run approval remains separate and must include fresh metadata guard plus ONE nominated native child; this avoids another unspecified code-plan phase without silently enabling runtime effects.

## 36. Short resume after planning break (2026-10-06)

- Owner said “ไป” after the pause and proposed short next segment. Resume final draft corrections and self-gate, then report before a long review segment. No new source/build/H0/native permission follows.
- Required launch checkpoint callback merged;22 source hashes/local links/unchanged26 app baseline checked. Actual CLI implementation-plan review not started; next segment is that review, not source implementation. Pause handoff remains historical.

## 37. Actual implementation-plan review segment (2026-10-06)

- Owner said “ไป” after the short draft/self-gate result and proposed Codex CLI review. Run one actual model-review invocation on a frozen five-label packet, then report findings before opening another long study/fix segment.
- Existing contributor thread routes the installed CLI; its comments are not independent code review. Actual external CLI verdict is separate. No source/build/H0/native/account-contents/settings/install/push permission.
- Input C:/tmp/aiusagebar-h2-native-implementation-input.md SHAa935095d6dfa8beab564219e5992781098b74a0f0f6098baa8a8b5e1f45b22c3,136744bytes/2167lines. Invocation20261006-134939-h2-native-implementation-13644 is MODEL REVIEW only; wrapper version metadata and exec are not H2 experiments.

- Decision37 execution: one actualCLI review completed fix-then-proceed5Major/1Minor/1Info; no followup study/fix/delta or source/native. All findings accepted but open. Next bounded contract-correction segment must precede actual delta and owner source/dispatch approval.

## 38. Windows live edition — owner อนุมัติ implementation (2026-10-06)

- อนุญาต Codex CLI จริงกับบัญชี owner เพื่อ account/rateLimits/read ผ่าน CodexPolicy ปิด MCP/hooks/plugins/analytics และ job ฆ่า process ลูก ระดับความระวังเท่า Mac; ยกเลิกข้อห้าม native เดิมสำหรับงานนี้
- ต่อ Claude ผ่านเมนูแอป แก้เฉพาะ statusLine สำรองค่าเดิมและมี Restore; เทสอัตโนมัติใช้ temp settings ห้ามแตะ settings จริง
- ใช้ [แผน frozen](plans/2026-10-06-windows-live.codex-plan.md) และกติกาสำหรับ Codex แทน process วางแผน/รีวิว; ไม่มี installer/autostart/NuGet ภายนอก/แตะ Mac/commit/push และห้าม log secret/raw payload

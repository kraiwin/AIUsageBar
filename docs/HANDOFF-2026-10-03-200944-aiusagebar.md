# HANDOFF — AIUsageBar (2026-10-03 20:09:44 +07 +0700)

สร้างด้วย skill `codex-handoff` โหมด `--full` ตามคำขอให้แทนเอกสารรอบก่อน

ฉบับ public ตัดรายละเอียด workflow ส่วนตัวที่ไม่เกี่ยวกับแอปออก; สถานะในไฟล์เป็นประวัติ ณ เวลานั้น

## TL;DR

AIUsageBar มี native macOS Menu Bar app และ offline data core แล้ว; ยังไม่เชื่อมบัญชีจริง
ผู้ใช้รับ UI ข้อความล้วนชื่อเต็มบรรทัดเดียว ไม่มี icon ซ้าย: `Claude 58% · Codex 72%` ใน demo
Version 0.1.0 build 4; ผล XCTest เดิมวันที่ 2026-10-03 ที่อ่านรอบนี้ 44 ผ่าน / 0 fail / 0 skip
โค้ดแอปยังไม่ได้ commit ในเวลาที่เขียน handoff นี้

## ทำไมและสิ่งที่ตกลง

- ต้องการ weekly quota ของบัญชีสมาชิก Claude/Codex ภาษาไทย ไม่ใช่ dashboard ค่าใช้จ่าย API
- พัฒนาเองด้วย Swift และ Apple frameworks; CodexBar เป็น reference อ่านอย่างเดียว ห้ามรัน scripts/build หรือเพิ่ม dependency โดยอัตโนมัติ
- เลือกชื่อเต็มข้อความล้วนหนึ่งบรรทัด; ไม่ย้อนเสนอหลอด ถัง battery vendor logos หรือสองบรรทัดเป็น default
- UI รองรับ 0/1/2 ค่าย แต่ checkbox ปัจจุบันทดลองหน้าตาเท่านั้น ไม่ได้ตรวจ login; normal launch ไม่ใส่ตัวเลขสมมติ
- `--demo` แสดง 58/72 เฉพาะ UI พร้อม tooltip/preview ย้ำข้อมูลสมมติ; missing ใช้ `—`, zero จริงเป็น `0%`
- ผู้ใช้ปิดรอบ UI แล้ว ไม่ได้อนุมัติการเชื่อมจริงหรือการเผยแพร่แอป; handoff นี้ไม่เริ่มพัฒนาต่อ

## สถานะที่ตรวจจริง

### App repo

- `~/Documents/AIUsageBar`, branch `main`, HEAD `cf11c03`; ไม่มี remote จาก `git remote`
- ไม่มี staged changes ใน `git status -sb`; modified: `.gitignore`, `AGENTS.md`, `README.md`, `docs/DECISIONS.md`, `docs/PROJECT_BRIEF.md`
- Untracked: Xcode project, `AIUsageBar/`, `AIUsageBarTests/`, `Config/`, CHANGELOG, MEMORY และ docs development/status/plans/research/validation/handoff
- งานใหม่หลัง initial scaffold ยังไม่ commit ทั้งหมด; ห้าม reset/clean ทิ้ง
- `Config/App.xcconfig`: 0.1.0/build 4, macOS minimum 14, Swift 6 strict concurrency
- `StatusItemController.swift` ตั้ง `button.image = nil` และ native toolTip; source มี selection และ formatter ตาม UI ที่ตกลง
- rg source ไม่พบ `Process(`, `URLSession`, `auth.json`, `cookies`, `Keychain`; integration ยังไม่ทำตาม DECISIONS ข้อ 9
- อ่าน `build/TestResults/TextOnly-001.xcresult`: Passed 44/0/0 บน arm64 macOS 27.0.1; ไม่ได้ build/test ใหม่ในรอบเอกสาร
- Intel/macOS 14 runtime และ automated hover/VoiceOver ไม่ได้ยืนยัน; อย่าอ้างว่าครอบคลุมแล้ว
- รอบนี้ไม่ได้ตรวจ process ใหม่: ก่อนหน้านี้พบ app PID 70378 และ preview PID 67731/port 8786; ถือเป็นข้อมูลเก่า ต้องตรวจใหม่ก่อนใช้

## Pick up here — เริ่มต่อ

1. อ่าน `AGENTS.md` → `docs/PROJECT_BRIEF.md` → `docs/DECISIONS.md` → `docs/STATUS.md` → `docs/plans/2026-10-03-mvp-plan.md`
2. ตรวจสถานะสดด้วย `git status -sb` ใน app repo; expected ณ handoff นี้: app มีงานค้าง
3. หากผู้ใช้ขอเก็บ app commit ให้เก็บเฉพาะ source/docs ของแอปที่ค้าง; ยังไม่มีการอนุมัติ app remote/push/release/notarization
4. เปิด `docs/research/usage-sources.md` ก่อน T3–T4: รอเคาะ Codex CLI-managed refresh (กฎ no-refresh เดิมยังมีผล) และยอมรับ Claude statusline snapshot ที่อาจเก่าเมื่อไม่ใช้งาน
5. เมื่ออนุมัติวิธีเชื่อมแล้ว ทำ Codex ก่อน Claude ตามแผน; ตรวจ Sandbox/IPC/credential side effects ก่อนเพิ่ม transport ไม่แอบเพิ่มสิทธิ์อ่าน credential
6. Claude bridge ต้องรักษา statusline เดิมพร้อม preview/backup; การเขียนนอก workspace ต้อง escalation และยังไม่อนุมัติจากการทำ handoff

## Gotchas / ของอยู่ไหน

- `AIUsageBar/UI/StatusItemController.swift`: NSStatusItem/popover/tooltip; `MenuBarText.swift`: renderer; `UsageDemoView.swift`: sample-only UI
- `AIUsageBar/Providers/UsagePayloadParser.swift`: pure parser รับ provider result ไม่ใช่ RPC envelope; `Models/UsageFreshness.swift`: freshness
- ต้องรักษา observed-time semantics: snapshot ไม่สดเพราะเพิ่งอ่านไฟล์; reset ผ่าน/invalid timestamp ซ่อนค่า; failed refresh ติดป้ายข้อมูลเก่า ไม่แสดงเหมือนปัจจุบัน
- Codex weekly ต้องระบุ window จริง; Claude parser ใช้ statusline numeric epoch ตาม research; optional missing ไม่ใช่ 0
- Build/test commands อยู่ README และ `docs/DEVELOPMENT.md`; Release bundle อยู่ `build/DerivedData/Build/Products/Release/AIUsageBar.app`
- Quit instance เดิมก่อนเปิดด้วย/ไม่มี `--demo`; bundle ID อาจชน Debug/Release ให้ใช้ full path
- `build/`, `reference/`, `work/visualizations/` เป็น ignored local artifacts ไม่เอาเข้า source commit
- ใช้ Mac เดิมและ Chrome extension ตาม AGENTS; ไม่ fallback PC/Atlas
- ใช้ AIUsageBar Engineering Baseline v1 ตาม DECISIONS/DEVELOPMENT; ไม่อ้าง ISO certification

## ส่งต่อ session ใหม่

> อ่าน AGENTS.md, MEMORY.md และ handoff ล่าสุดก่อน เรามี AIUsageBar 0.1.0 build 4 แบบ text-only กับ offline core แล้ว แต่ยังไม่เชื่อมข้อมูลจริงและโค้ดยังไม่ได้ commit ตรวจ Git แล้วเริ่มจากวิธีเชื่อม Codex/Claude ที่ยังรออนุมัติ โดยรักษา UI ที่รับแล้ว

# Brief รับช่วงใน Codex Desktop — ปิดชุด Claude first-install fix

บันทึก: 2026-10-05T10:53:07.280052+07:00 · workspace `/Users/kraiwin/Documents/AIUsageBar`

## สถานะและเป้าหมาย

ผู้ใช้อนุมัติให้ทำตามคำแนะนำ: ปรับ Codex error → ตรวจ tests/build/review → real Mac UI Claude preview/install/disconnect/reinstall → อัปเดตแอป/bridge ที่ใช้จริง → commit/push source พร้อมหลักฐาน
Session นี้ทำ error hint และ offline validation แล้ว แต่ Computer Use ใช้ native UI ไม่ได้ ผู้ใช้จึงขอ brief/prompt ส่งต่อไป Codex Desktop ไม่ใช่ให้ทำงานติดตั้งต่อใน session นี้
ยังไม่ติดตั้งจริง ไม่ commit/push ไม่ bump version ไม่สร้าง tag/binary/release
มี review concern ใหม่เรื่อง restore retry ที่ต้องตรวจและแก้ก่อนปิดชุด fix; tests ที่ผ่านยังไม่ครอบคลุมกรณี interruption นี้

## อ่านก่อนเริ่มและกฎที่ต้องรักษา

อ่าน AGENTS.md → docs/PROJECT_BRIEF.md → docs/DECISIONS.md → docs/STATUS.md → docs/DEVELOPMENT.md → MEMORY.md → brief นี้
ใช้ Swift/Apple frameworks ไม่มี dependency เพิ่ม ไม่อ่าน/พิมพ์ credential/cookie/raw config/stderr/account payload
คง `--strict-config` และ Codex guards; ไม่ใช้ mcp-list fallback ไม่เริ่ม S1–S3 ใหม่
ปลายทางที่อนุมัติคือ `kraiwin/AIUsageBar` public source เท่านั้น ไม่ push local-history/pre-publication ไม่แจก binary/tag/notarize
ใช้ Computer Use ของ Mac นี้สำหรับ UI ไม่ใช้ browser/PC connection อื่น; CLI/API ใช้ได้เมื่อเหมาะกับงาน

## Git/source ปัจจุบัน

- Branch main, HEAD `f2643b6`; local tracking main...origin/main ไม่มี ahead/behind ณ ตอนตรวจ ไม่ fetch remote สด
- origin `https://github.com/kraiwin/AIUsageBar.git`; app version คง 0.1.0/build6
- ไม่มี staged changes; source fix เดิมยังอยู่ใน working tree:
  - AIUsageBar/ClaudeBridge/ClaudeBridgeCommand.swift, ClaudeBridgeInstaller.swift, ClaudePrivateFiles.swift, ClaudeSettingsCommand.swift
  - AIUsageBar/UI/StatusItemController.swift และ AIUsageBarTests/ClaudeBridgeTests.swift
  - ลบ Tools/ClaudeBridge/main.swift ที่ไม่อยู่ใน build
- รอบนี้เพิ่ม hint ใน AIUsageBar/App/RefreshCoordinator.swift: เฉพาะ childExited/unsupportedConfiguration ระบุว่า unknown config หรือ unsupported setup **อาจ**เป็นสาเหตุ; errors อื่นคงข้อความเดิม ไม่มี raw diagnostics
- ปรับ indentation หนึ่งจุดใน installer หลัง build/tests (whitespace เท่านั้น) และเพิ่ม CHANGELOG Unreleased; brief/MEMORY/STATUS ถูกอัปเดตท้ายรอบ
- README/เอกสาร first-install เดิมอยู่ใน commit เอกสารแล้ว แต่ source fix ยังไม่เผยแพร่

## หลักฐานที่ตรวจใหม่ 2026-10-05

- Debug XCTest: 120 tests / 0 failures, `TEST SUCCEEDED`, log `build/october-fix-tests.log`
- Release: `BUILD SUCCEEDED`, log `build/october-fix-release.log`
- คำสั่งใช้ project/scheme AIUsageBar; Debug destination `platform=macOS,arch=arm64`, derivedDataPath `build/OctoberFixValidation`; Release destination `generic/platform=macOS`, derivedDataPath `build/OctoberFixRelease`
- Release executable: `build/OctoberFixRelease/Build/Products/Release/AIUsageBar.app/Contents/MacOS/AIUsageBar`
- Release synthetic CLI smoke 2/2: existing settings without statusLine และ absent settings → preview ไม่เขียน → install → รัน command ผ่าน /bin/sh → silent quota-only snapshot → disconnect → exact bytes/file absence
- Smoke ใช้ TemporaryDirectory ใต้ build ลบเมื่อจบ ไม่แตะ ~/.claude จริง ไม่เรียกบัญชีจริง
- Xcode มี AppIntents metadata extraction skipped (ไม่มี framework นี้); XCTest log มี Apple linkd service connection diagnostics; tests/build สำเร็จ ไม่มี Swift source compile warning ที่พบ
- git diff --check ผ่านก่อนการเขียน brief; build/test processes สองงาน exit0 จบแล้ว
- Computer Use: cua.getApp("AIUsageBar") และ cua.getState() แจ้ง `Sky Computer Use native pipe startup failed`; inventory apps/browsers ว่าง ไม่ได้คลิก UI ไม่ใช่หลักฐานว่าแอปผู้ใช้เสีย

## Review concern ที่ต้องปิดก่อนติดตั้ง/push

Waluigi (native Codex reviewer, independent context ไม่ใช่ Claude review) พบจาก static review:
`ClaudeBridgeInstaller.restore` อ่าน settings แบบ required ที่บรรทัด115; เส้นทาง settingsExisted=false ลบ settings ที่142–146 ก่อนลบ artifacts ที่148–150
หาก process หยุดหรือ cleanup ล้มเหลวหลัง settings ถูกลบ แต่ metadata ยังอยู่ การเรียก restore ซ้ำจะ missingFile และ preview ถูก metadata กัน ทำให้ UI install/disconnect ต่อไม่ได้
ให้ยืนยันด้วย synthetic regression ที่จำลอง settings หายแต่มี valid owned backup/metadata/artifacts แล้วทำ recovery ที่ retry ได้ โดยไม่ลบไฟล์ที่ไม่เป็นเจ้าของ/ไม่กลืน conflict ห้ามลบ settings จริงเพื่อจำลอง
ผล review ฉบับสุดท้าย: P2 หนึ่งจุดตามข้างต้นยังค้าง; ส่วนอื่นไม่พบ credential logging/guard weakening เพิ่ม การลบ Tools ไม่กระทบ app entrypoint Reviewer ตรวจแบบ static ไม่รัน tests/UI/live/network; ไม่อ้างว่าปิด blocker แล้ว

## ลำดับทำต่อ

1. ตรวจ Git/diff/artifacts ใหม่ รักษา WIP ทั้งชุด ไม่ย้อนกลับไฟล์เดิม
2. ปิด review concern เรื่อง interruption/retry ด้วย tests และ independent review; ถ้าแก้ source ให้รัน tests/Release ตาม delta
3. เชื่อม Computer Use บน Mac เดิม ตรวจ app/session แล้วใช้ Release build ใหม่ ตรวจ UI Claude preview → install → disconnect → reinstall พร้อม backup/compare-before-write ตาม DECISIONS
4. Owner มี bridge/statusline เดิม: restore ก่อนติดตั้งรุ่นใหม่ ตรวจ exact restored bytes privately และ binary/helper digest โดยไม่พิมพ์ settings เนื้อหา ใช้ settings เดิมเพื่อ preservation smoke ไม่ลบ settings จริงเพื่อจำลอง first install
5. ตรวจว่าตัวใช้งานจริงเป็น build ใหม่ คง Menu Bar default; ถ้าต้องใช้ --show-window ให้ปิดโหมดตรวจเมื่อจบ บันทึก UI หลักฐานตามจริง ไม่อ้าง first-install real-account coverage จาก preservation flow
6. อัปเดต STATUS/CHANGELOG/validation แล้วตรวจ diff/secrets/scope ก่อน commit/push ไป origin/main ตามอนุมัติ ตรวจ remote readback ไม่ต้องถามทำต่อทีละเฟส ถ้าเครื่องมือ/OS block ให้รายงานสิ่งที่ต้องให้ผู้ใช้ทำเฉพาะจุด

## Prompt สั้น

อ่าน MEMORY.md และ handoff ล่าสุด แล้วปิดชุด Claude first-install fix: แก้ restore retry ตาม review, ทดสอบ/build/review, ตรวจ UI ติดตั้ง–ถอด–ติดตั้งกลับบน Mac นี้ แล้วอัปเดตตัวใช้งานจริงและ commit/push source พร้อมหลักฐานไป kraiwin/AIUsageBar คง strict-config และอย่าเผยข้อมูลลับ

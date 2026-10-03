# Handoff — Claude first install และงานก่อนเผยแพร่ fix

บันทึก 2026-10-04 00:06:59 +07 +0700 (Asia/Bangkok)

## สรุปสถานะ

ผู้ใช้ง่วงและสั่งเขียน handoff เพื่อหยุดงานคืนนี้ ไม่มีคำสั่ง commit/push หรือติดตั้งจริงในรอบ handoff นี้
ข้อ 1 แก้ใน local source แล้ว: ไม่เคยตั้ง statusline หรือไม่มี settings.json ติดตั้ง quota-only bridge แบบเงียบได้ คืนค่าได้และคง unrelated edits
Final XCTest 120/120, Release build และ synthetic Release CLI smoke 2/2 ผ่านตาม artifacts จากรอบก่อน ไม่รันทดสอบใหม่เพื่อเขียน handoff
ยังไม่ commit/push fix, ไม่อัปเดต app/bridge ที่ใช้จริง และยังไม่ได้ลอง UI install/disconnect รอบนี้กับ settings จริง

## ความเห็น Claude ที่ผู้ใช้ส่งมา

- Claude รายงานว่าอ่านโค้ด fix แล้วเห็นว่าถูก โดยเฉพาะ insertion/removal, exact restore และรักษาค่าที่ผู้ใช้แก้เพิ่ม; ไม่ได้รันทดสอบซ้ำเอง
- Claude ยอมรับการคง --strict-config แต่เสนอให้ข้อความ error ชัดว่า “อาจเป็นเพราะไฟล์ตั้งค่า Codex มีค่าที่รุ่นนี้ไม่รู้จัก”
- Claude แนะนำลองกดติดตั้งและถอดบน Mac จริงก่อน push
- นี่เป็นผล static review ที่ผู้ใช้ส่งมาในแชต ไม่ใช่ผล review จากเครื่องมือของ session นี้ และไม่ใช่ real-install validation

## Git และไฟล์ปัจจุบัน

- Workspace: /Users/kraiwin/Documents/AIUsageBar; branch main; HEAD 0c61736 (docs: record verified public source publication)
- Source publication commit ก่อนหน้า 2f0fe19; app config ยัง 0.1.0/build6 ไม่ bump version รอบนี้
- HEAD...origin/main = 0/0 จาก local remote-tracking ref; ไม่ fetch หรืออ่าน GitHub สดในรอบ handoff จึงไม่อ้างสถานะ remote ณ วินาทีนี้
- ไม่มี staged changes ก่อนเขียน handoff; fix ทั้งชุดยัง uncommitted
- Modified source: ClaudeBridgeCommand.swift, ClaudeBridgeInstaller.swift, ClaudePrivateFiles.swift, ClaudeSettingsCommand.swift ใต้ AIUsageBar/ClaudeBridge/; AIUsageBar/UI/StatusItemController.swift; AIUsageBarTests/ClaudeBridgeTests.swift
- Modified docs: README.md, CHANGELOG.md, docs/DEVELOPMENT.md, docs/STATUS.md
- Deleted: Tools/ClaudeBridge/main.swift (ไม่อยู่ใน build; app entrypoint ใช้ ClaudeBridgeCommand อยู่แล้ว)
- Untracked ก่อน handoff: docs/validation/2026-10-04-claude-first-install.md; หลัง handoff มีไฟล์นี้และ MEMORY.md pointer เพิ่มด้วย
- ไม่มี build/test job ค้างจากรอบก่อนตามผล completion ที่ได้รับ; ไม่ตรวจ process inventory ใหม่ ไม่เปลี่ยน session ของแอปที่กำลังใช้งาน

## หลักฐานและรายละเอียดที่ต้องรักษา

ดู [validation ล่าสุด](validation/2026-10-04-claude-first-install.md) และ [STATUS](STATUS.md)

- build/bridge-fix-test.log: TEST SUCCEEDED; final 120 tests/0 failures (เพิ่ม 7 จากเดิม113)
- build/bridge-fix-release.log: BUILD SUCCEEDED; executable: build/BridgeFixRelease/Build/Products/Release/AIUsageBar.app/Contents/MacOS/AIUsageBar
- Synthetic Release smoke: existing settings without statusLine และ absent settings → install → run installed shell command → silent quota-only snapshot → disconnect → exact bytes/file absence ผ่าน2/2 ไม่แตะ ~/.claude จริง
- ใหม่ใช้ collect mode, snapshot parser เดิม, ไม่เก็บ raw stdin ไม่อ่าน token; exclusive rename ป้องกันทับ settings ที่เกิดขึ้นระหว่าง compare/write
- Restore metadata schema1 ที่ไม่มี settingsExisted ยังได้; ถ้า quota-only statusLine ถูกเพิ่ม fields จะ conflict แทนการลบงานผู้ใช้
- README บอกวิธี disconnect/reinstall หลัง build ใหม่ และข้อจำกัด /bin/sh สำหรับ inline zsh syntax; ไม่มีปุ่ม updater ใหม่
- Main Codex เขียน fix; Waluigi ตรวจ independent context และ delta ไม่มี blocker ค้าง เป็น Codex review ไม่ใช่ Claude review; ความเห็น Claude ล่าสุดแยกไว้ด้านบน
- คง internal docs ตาม workflow พร้อมอธิบายใน README ไม่ลบ handoff/MEMORY/AGENTS ทั้งหมดเอง

## เริ่มต่อจากตรงนี้

1. อ่าน AGENTS.md → docs/PROJECT_BRIEF.md → docs/DECISIONS.md → docs/STATUS.md → docs/DEVELOPMENT.md → MEMORY.md → handoff นี้ แล้วตรวจ git status -sb / git diff / logs ใหม่ อย่าเริ่ม implementation S1–S3 ซ้ำจาก handoff เก่า
2. ปรับ Codex error messaging ตามข้อเสนอ Claude โดยตรวจจุดที่แยก startup/config failure ได้ก่อน หากยังวินิจฉัยไม่ได้ ให้ใช้คำว่า “อาจ” ในบริบทที่เหมาะสม ไม่กล่าวว่าเป็นสาเหตุแน่นอน และไม่พิมพ์ raw config/stderr ที่อาจมีข้อมูลลับ คง --strict-config เว้นแต่มีหลักฐานและการตัดสินใจใหม่
3. เมื่อผู้ใช้กลับมาสั่งทำต่อ ให้ตรวจ Release app และลอง UI preview → install → disconnect → reinstall บน Mac เดิมด้วย backup/compare-before-write ตามข้ออนุญาตใน DECISIONS และข้อความคำสั่งใหม่ อย่าตัด bridge ปัจจุบันทิ้งคืนนี้/จาก handoff อย่างเดียว
4. Real settings ของ owner เคยมี statusline เดิม จึงทดสอบ preservation/UI ได้ แต่ไม่ใช่หลักฐาน no-statusline จากบัญชีจริง ใช้ synthetic tests เป็นหลักฐานเส้นทาง first install แยกกัน ห้ามลบ settings จริงเพื่อจำลองผู้ใช้ใหม่
5. เมื่อแก้ error code ให้รัน tests/build ตาม delta; บันทึกผล UI smoke จริงและข้อจำกัด อัปเดต STATUS/CHANGELOG ตรวจ diff/secrets/license และ scope ก่อนเสนอ commit/push ตามคำสั่ง/ข้ออนุญาตที่มี ห้ามอนุมานว่าขอ handoff คือสั่ง push
6. ไม่ push local-history/pre-publication, ไม่ส่ง private backups/credentials/raw payloads, ไม่แจก binary/tag/notarize ในขอบเขตนี้

## Prompt สำหรับ session ถัดไป

> อ่าน MEMORY.md และ handoff ล่าสุด ตรวจ working tree แล้วทำ local fix ต่อ: ปรับข้อความ Codex error ตามข้อเสนอ Claude โดยคง strict-config ตรวจ UI Claude install/disconnect/reinstall บน Mac จริงพร้อม backup และทดสอบตามการเปลี่ยนแปลง อัปเดตหลักฐานก่อนเตรียม commit/push; อย่าเริ่ม S1–S3 ใหม่ อย่าเผยแพร่หรือแตะ settings จริงเพียงเพราะ handoff นี้ ไม่มีข้อมูลลับใน outputs

## คำสั่งปิดคืนนี้หลัง handoff

ผู้ใช้ชี้แจงว่า ship รอบคืนนี้หมายถึง commit/push เอกสารและ handoff เท่านั้น ยังไม่ให้ลงมือทำงานต่อ Source fix ข้อ1 ยังเป็น local WIP ไม่ stage/push source/tests หรือการลบ Tools entrypoint

Codex เริ่มเกินขอบเขตโดยเพิ่ม error hint และ bump0.1.1/build7 จากนั้นย้อนเฉพาะสองการเปลี่ยนแปลงนั้นและ release draft แล้ว กลับเป็น0.1.0/build6 ไม่มี release ใหม่ Gate ที่รันระหว่างนั้นผ่าน120tests/Release แต่ไม่ใช่งานที่ต้องทำต่อคืนนี้ Computer Use อ่านแอปไม่ได้ (timeout); ไม่ได้กดติดตั้ง/ถอดหรือเปลี่ยน settings จริง Process UI smoke ที่เปิดเพื่อทดสอบหยุดแล้วจากการตรวจ PID ไม่อ้างว่าทดสอบ UI ผ่าน

Session ถัดไปให้ตรวจ Git ใหม่: เอกสารอาจอยู่ใน remote แล้ว แต่ source fix ยังอยู่เฉพาะ working tree ต้องรับคำสั่งเริ่มงานต่อก่อนปรับ error messaging/real UI smoke หรือเผยแพร่ source fix

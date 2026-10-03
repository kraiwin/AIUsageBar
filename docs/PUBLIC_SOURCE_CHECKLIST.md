# เช็กลิสต์เตรียม public source

เอกสารนี้ใช้ตรวจ source ก่อนนำไปไว้ใน GitHub public repository ตาม [DECISIONS ข้อ 14](DECISIONS.md) และ [แผน S1–S3](plans/2026-10-03-s1-s3-execution-plan.md) การเตรียม source ไม่ได้แปลว่าเผยแพร่แล้วหรืออนุญาตให้ push ไปปลายทางที่ยังไม่ยืนยัน

## เตรียมไว้ใน repository แล้ว

- [x] `README.md` มีขั้นตอน build ด้วย Xcode/CLI, local ad-hoc signing, วิธีเชื่อมและถอด Claude/Codex, ข้อจำกัดการใช้งาน และ side effects ที่ทราบ
- [x] `LICENSE` ระบุ MIT; `THIRD_PARTY_NOTICES.md` ให้เครดิต CodexBar ในฐานะแรงบันดาลใจและระบุว่าไม่ได้รวม source/assets ของ reference
- [x] เอกสารระบุ Swift/Apple frameworks และไม่มี third-party library dependency ที่รวมมากับ source; Xcode/macOS SDK และ CLI ที่ผู้ใช้ติดตั้งเองเป็น prerequisites
- [x] เป้าหมาย build คือ source build บน macOS 14+ ด้วย Xcode; signing เป็น local ad-hoc (`CODE_SIGN_IDENTITY = -`) โดยไม่ต้องมี Team ID/Developer ID หรือบัญชี Apple Developer แบบชำระเงิน
- [x] `.gitignore` กัน `build/`, `.build/`, `DerivedData/` และ `reference/` ซึ่งเป็น artifacts ในเครื่องหรือ source อ้างอิง

รายการส่วนนี้ยืนยันเอกสาร/ไฟล์; ผล build/runtime และขอบเขตที่ตรวจอยู่ใน validation ด้านล่าง

## ผลตรวจ source preparation

อัปเดตหลัง [validation S1–S3](validation/2026-10-03-s1-s3.md): ผ่าน final113tests/Release/real providers/install-restore-reinstall/independentreview/sourcecandidateaudit84files ไม่มี matching credential patterns/build artifacts/symlinks การตรวจนี้ครอบคลุม personal-use scope บน Mac นี้ ไม่ใช่การรับรองทุก OS หรือ audibleVoiceOver ที่ยังไม่ได้ตรวจ ก่อน publication ตรวจ staged scope ซ้ำ

- [x] ผู้ดูแลรอบงานยืนยันใน `STATUS.md` ว่า Release build และ test suite ของ source ปัจจุบันผ่าน พร้อมผล CLI smoke ที่จำเป็น โดยไม่บันทึก quota, account payload หรือ credential
- [x] ตรวจ UI และ workflow บน Mac จริง รวมถึง Codex refresh และ Claude snapshot/installer/restore; ระบุข้อจำกัดหรือผลที่ยังไม่รองรับตามจริง
- [x] independent review และการตรวจ diff สุดท้ายเสร็จ; README, source, entitlements, signing configuration และ validation ตรงกัน
- [x] ตรวจ Git scope และไฟล์ที่ staged/untracked ทีละรายการก่อน publication เพื่อยืนยันว่าไม่มี credential, token, cookie, account/config/raw transcript, private fixture หรือไฟล์ส่วนตัวอื่น ห้ามถือว่า `.gitignore` เพียงอย่างเดียวเป็นหลักฐานว่าปลอดข้อมูลลับ
- [x] ตรวจว่าไม่มี binary, derived build output หรือสำเนา source จาก `reference/` ปะปนใน source ที่จะเผยแพร่
- [x] ยืนยัน license/credits และการระบุโปรแกรมภายนอกกับ source ที่ส่งจริง รวมถึงตรวจไฟล์ที่เพิ่มภายหลังว่ามี license หรือ notice ที่ต้องแนบหรือไม่
- [x] Owner ยืนยัน `kraiwin/AIUsageBar`, public และตอบ “go” ให้สร้าง repo/commit/push source; ไม่มี binary release/tag/notarization/App Store

## ขอบเขตและข้อมูลที่ห้ามเผยแพร่

ให้ source repository มี source, Xcode project/configuration, tests, คู่มือ และ notices ที่จำเป็นต่อการ build จาก source เท่านั้น ตัด `build/`, `DerivedData/`, build logs/results, `reference/` และสำเนาแอปออก

อย่านำข้อมูลจาก `~/Library/Application Support/AIUsageBar/` เข้า repository: ตำแหน่งนี้ใช้เก็บ Claude wrapper/helper, latest quota-only snapshot หรือ no-data state, ownership metadata และ `claude-settings-backup.json` ซึ่งอาจมี settings เดิมส่วนตัว รวมทั้งไฟล์สำรองหรือสถานะติดตั้งอื่น ห้ามแนบ `~/.claude/settings.json`, Codex CLI state/config/auth, Keychain data, cookies, logs หรือ fixture ที่มาจากบัญชีจริงด้วย

แอป build เองสำหรับใช้ส่วนตัวเป็น local artifact; ไม่แจก binary ที่ signed ด้วย Developer ID และไม่อ้างว่าได้รับ notarization. ผู้ใช้ source build ต้องมี Xcode/macOS SDK และติดตั้ง/login official Codex CLI กับ Claude Code เองตามที่ใช้ การเชื่อมต่อ CLI อาจใช้ network และจัดการ auth/state/logs ตามพฤติกรรมของ CLI นั้น

สถานะการทดสอบและความพร้อมจริงให้ยึด [STATUS](STATUS.md) หลังเจ้าของรอบงานอัปเดตผล S1–S3; เช็กลิสต์นี้ไม่แทนผล build, tests, live Claude/Codex smoke หรือ UI review

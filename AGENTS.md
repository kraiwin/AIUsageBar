# AIUsageBar — instructions for the next session

อ่าน docs/PROJECT_BRIEF.md แล้วตามด้วย docs/DECISIONS.md ก่อนเริ่มงาน เพื่อรับบริบทและค่าตั้งต้นที่ผู้ใช้ตกลงไว้แล้ว
DECISIONS.md เติมช่องที่ brief ระบุว่า "ยังไม่ได้สรุป" — ถ้าสองไฟล์ขัดกัน ให้ยึด DECISIONS.md และแจ้งผู้ใช้

จากนั้นอ่าน `docs/STATUS.md` และทำงานตาม `docs/DEVELOPMENT.md`
สำหรับการรับช่วง session ใหม่ ให้อ่าน `MEMORY.md` และ dated handoff ที่ชี้ไว้ แล้วตรวจ Git/state ปัจจุบันก่อนใช้ snapshot
เมื่อจบชุดงาน อัปเดต STATUS ตามผลจริงและ CHANGELOG เมื่อมีการเปลี่ยนแปลงสำคัญ
ห้ามระบุว่า build/test/release ผ่านหรือเสร็จหากยังไม่มีหลักฐาน

- โปรเจกต์นี้แยกจากโปรเจกต์อื่น และไม่ใช้ workflow/QA/skills ของงานอื่น
- ผู้ใช้อนุญาตให้เริ่มพัฒนาแอป macOS Menu Bar ภาษาไทย แสดง Claude และ Codex weekly usage
- ใช้ CodexBar เป็น reference เท่านั้น ไม่ใช่นำแอปเดิมมาเปลี่ยนภาษาแล้วใช้ทั้งหมด
- ก่อนรัน build/install/script จาก reference ให้ตรวจโค้ดและ dependency ที่เกี่ยวข้อง
- แนวทางเริ่มต้น: Swift + framework ของ Apple ไม่เพิ่ม dependency ภายนอก หากจำเป็นต้องเปลี่ยนแนวทาง ให้อธิบายเหตุผลก่อน
- ห้าม commit credential, token, cookies หรือข้อมูลบัญชี ห้ามพิมพ์ค่าลับใน log/tool output
- ใช้ชื่อและไอคอนของเราเอง ชื่อ AIUsageBar เป็นชื่อชั่วคราว
- เป้าหมายล่าสุดคือ GitHub public source-build (DECISIONS ข้อ 14); ไม่เข้า App Store ไม่ notarize ไม่สมัคร Apple Developer รอบปัจจุบันผู้ใช้สั่ง implementation/tests/review/docs S1–S3 และ local personal install ตาม DECISIONS ข้อ 20; ผู้ใช้อนุญาต public source publication ไป `kraiwin/AIUsageBar` ตาม DECISIONS ข้อ 21; ไม่รวม binary/tag/notarization
- เคารพข้อจำกัด sandbox หากเขียนนอก workspace ต้องขอ escalation ผ่านเครื่องมือ

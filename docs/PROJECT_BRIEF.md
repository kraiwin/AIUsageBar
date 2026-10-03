# AIUsageBar — บริบทและแผนเริ่มงาน

บันทึกจากการสนทนา วันที่ 2026-10-03

## เป้าหมาย

สร้างแอป macOS ที่แสดง usage ของ Claude และ Codex บนแถบขวาบนของหน้าจอ
ชื่อบน Mac คือ Menu Bar; ไอคอนเรียก Menu Bar Icon หรือ Status Item
ผู้ใช้สนใจโควตารายสัปดาห์ (weekly usage) ของบัญชีสมาชิก ไม่ใช่เพียงค่าใช้จ่าย API
UI และเอกสารมุ่งผู้ใช้ภาษาไทย

## เหตุผลและการตัดสินใจ

ผู้ใช้กังวล supply chain attack จึงไม่ต้องการติดตั้งแอปสำเร็จรูปของผู้อื่น
ตกลงศึกษา CodexBar เป็นแนวทาง แล้วเขียนแอปขนาดเล็กของเราเอง
การ clone/build โค้ดของคนอื่นเองยังมีความเสี่ยงจาก source, dependency และ build script
จึงเริ่มจากอ่าน reference โดยยังไม่รัน script หรือ build ของ reference
ใช้ Swift และ framework ที่มากับ macOS เพื่อลด dependency ภายนอก
ยังไม่รับประกันว่าวิธีดึง usage จะทำได้ครบ ต้องศึกษาการเข้าถึงจริงก่อน

## Reference และ license

- Repository: https://github.com/steipete/CodexBar
- License: https://github.com/steipete/CodexBar/blob/main/LICENSE
- ตรวจ ณ วันที่บันทึก: MIT License, Copyright (c) 2026 Peter Steinberger
- เอกสาร reference ระบุว่าใช้ OAuth/CLI สำหรับ Codex และ OAuth/browser cookies/CLI fallback สำหรับ Claude
- เป็นข้อมูลของ reference ยังไม่ใช่การเลือกวิธีเชื่อมบัญชีของแอปเรา
- หากเขียนใหม่จากแนวคิด โดยทั่วไปไม่ต้องแนบ MIT ของ reference แต่ผู้ใช้ยินดีให้เครดิตแรงบันดาลใจ
- หากคัดลอก/ดัดแปลงโค้ดหรือส่วนสำคัญ ต้องคง copyright และ permission notice ตาม MIT
- ตรวจ license ของ asset และ dependency แยก หากนำมาใช้
- ใช้ชื่อ/ไอคอนของเราเอง ไม่อ้างว่าเป็นแอปทางการของ Anthropic หรือ OpenAI

ข้อความ README ที่เสนอ (ใช้เมื่อเป็นข้อเท็จจริง):
> ได้รับแรงบันดาลใจจาก CodexBar โดยพัฒนาขึ้นใหม่เพื่อผู้ใช้ภาษาไทย

## ขอบเขต MVP ที่เสนอ

- Menu Bar app ขนาดเล็ก
- Claude และ Codex: weekly usage, เวลาที่รีเซ็ต และ session usage ถ้าข้อมูลรองรับ
- ภาษาไทย เช่น ใช้ไปแล้ว / เหลืออีก / รีเซ็ตเมื่อ
- แสดงสถานะไม่มีข้อมูล/ต้องเชื่อมบัญชี/โหลดไม่สำเร็จตามจริง
- วิธีเก็บ credential และปลายทางเครือข่ายตรวจสอบได้ ใช้สิทธิ์เท่าที่จำเป็น
- ไม่เพิ่ม auto-update หรือฟีเจอร์อื่นที่ไม่จำเป็นในระยะแรก

รายละเอียด UI, วิธี authentication, polling interval, minimum macOS และ license ของโค้ดเรา ยังไม่ได้สรุป
→ อัปเดต 2026-10-03: ค่าตั้งต้นสรุปไว้ใน [DECISIONS.md](DECISIONS.md) แล้ว (วิธี authentication ยังต้องศึกษาก่อน ตามหัวข้อ 3–4)
อย่าเดาค่า usage หรือสร้างตัวเลขเสมือนให้ผู้ใช้เข้าใจว่าเป็นข้อมูลจริง

## ขั้นตอนสำหรับ session ใหม่

1. อ่าน AGENTS.md และไฟล์นี้ ตรวจสถานะโฟลเดอร์/Git และเครื่องมือ Swift/Xcode ของ Mac
2. เริ่ม local Git repo หากยังไม่มี (ยังไม่ต้องสร้าง remote)
3. Clone CodexBar แยกเป็น reference ที่ไม่รวมใน Git repo ของเรา; ตรวจ source/license โดยยังไม่รัน script
4. ศึกษาวิธีอ่าน usage ของแต่ละบริการ ตรวจเอกสารทางการที่เกี่ยวข้อง และสรุป credential/endpoint/ข้อจำกัด
5. เลือกวิธีเชื่อมข้อมูลที่เล็กและตรวจสอบง่าย หลีกเลี่ยงการอ่าน browser cookies ทั้งชุดโดยไม่จำเป็น
6. สร้าง Swift Menu Bar skeleton ภาษาไทย แล้วเชื่อมข้อมูลจริงทีละบริการ
7. ทดสอบการแสดงค่า เวลารีเซ็ต และสถานะผิดพลาด พร้อมบันทึกวิธี build และข้อจำกัด
8. เตรียม README/เครดิต/license และตรวจ secret ก่อนเสนอเผยแพร่ GitHub public

## สถานะ handoff

อัปเดต 2026-10-03: มี local Git, เอกสารบริหารงาน, source แอป Menu Bar และ offline data core แล้ว; ยังไม่มีการเชื่อมบัญชีหรือ release ที่เผยแพร่
สถานะปัจจุบันและงานถัดไปดู [STATUS.md](STATUS.md); วิธีทำงานดู [DEVELOPMENT.md](DEVELOPMENT.md)
ไม่ต้องให้ผู้ใช้เล่าบริบทใหม่ ให้เริ่มจากงานถัดไปใน STATUS โดยยึด DECISIONS

## เป้าหมายแจกจ่ายล่าสุด (2026-10-03)

ผู้ใช้เลือก GitHub public source repo ให้ build เองด้วย Xcode: single non-Sandbox app + Hardened Runtime ไม่มี XPC/App Store/notarization/Apple Developer Program ยึด DECISIONS ข้อ 14 และ revised one-app plan; แผนผ่าน review และผู้ใช้สั่งลงมือ S1–S3/local personal install แล้วตาม DECISIONS ข้อ 20; ผลจริงอยู่ใน STATUS public source publication อนุมัติแล้วตาม DECISIONS ข้อ 21 ไม่มี binary/tag/notarization

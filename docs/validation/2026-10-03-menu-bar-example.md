# ตัวอย่าง Menu Bar + tooltip — 2026-10-03

ผู้ใช้อนุญาตตัวเลขสมมติสำหรับ preview โดยเฉพาะ; ไม่มีการเชื่อมบัญชีหรือ server

## ผลตรวจ

- Build Release ผ่านบน Xcode 27.0; build number 2
- Existing XCTest regression: 39 passed / 0 failed / 0 skipped (`build/TestResults/MenuBarDemo-001.xcresult`)
- Reviewer ตรวจ lifecycle, opt-in flag และ sample labeling แบบ read-only ไม่พบ blocking finding
- ใช้ public `NSStatusItem` API เพื่อให้ข้อความกว้างตามเนื้อหาและกำหนด `button.toolTip` โดยไม่เพิ่ม package
- ค่า 58/72 อยู่เฉพาะ UI example; default launch ยังไม่แสดงเปอร์เซ็นต์และไม่สร้าง provider transport

## UI ที่เห็นจริง

เปิด app bundle ใน Release ด้วย `--demo`; Computer Use จับ popup ของ status-only app ไม่ได้ จึงมี normal preview window เฉพาะโหมดตัวอย่าง
ตรวจ window/AX tree และ screenshot เห็นชื่อสองค่าย, Claude เหลือ 58%, Codex เหลือ 72%, ป้ายตัวอย่าง และข้อความว่าไม่ได้เชื่อม server
หน้าต่าง preview ช่วยให้ทดสอบ layout ได้ ไม่ถือเป็นหลักฐานว่าการ hover tooltip หรือ popup จาก Menu Bar ผ่านการตรวจด้วย screenshot แล้ว

Menu Bar title ล่าสุดที่ผู้ใช้ขอให้ย่อ: `Claude 58% · Codex 72%` (build 3)
Tooltip ที่กำหนดมีป้ายข้อมูลตัวอย่าง, weekly remaining ของทั้งสองค่าย และคำย้ำว่าไม่ใช่ยอดจริง
ยังไม่ได้จับภาพ tooltip ขณะ hover ผ่าน Computer Use; native API ถูกกำหนดแล้ว ให้ผู้ใช้ลอง hover บน Mac จริง

## ขอบเขต

ไม่อ่าน credential, ไม่เรียก model/CLI/network, ไม่เก็บ sample metrics และไม่แก้ global settings
Sample flag ไม่ persist; ออกจากแอปแล้วเปิดโดยไม่มี argument เพื่อกลับสู่โหมดปกติ
ยังไม่รับรอง macOS 14/Intel runtime หรือการเชื่อมข้อมูลจริง

# Local quota bridge — ข้อเสนอหลังตรวจ Sandbox

2026-10-03 · **ข้อเสนอเก่าที่ถอนจากคำแนะนำ MVP แล้ว — เก็บเป็นประวัติเท่านั้น**

ผู้ใช้ต้องการเปิดแอปเดียวและให้ Codex รีเฟรชยอดใหม่จริง จึงไม่ใช้ manual Terminal collector เป็นแนวทางผลิตภัณฑ์
แผนปัจจุบันสำหรับรีวิว: [one-app integration plan](2026-10-03-one-app-integration-plan.md)
ข้อความ “แนะนำ/รออนุมัติ” ด้านล่างคือ snapshot ของข้อเสนอเดิม ไม่ใช่งานถัดไปหรือสิทธิ์ที่ได้รับอนุมัติ

## เหตุผล

ผู้ใช้อนุมัติ official Codex CLI lifecycle และ Claude snapshot แล้วตาม DECISIONS ข้อ 12
แต่ Release app ปัจจุบันอยู่ใน App Sandbox ไม่มีสิทธิ์ network หรือไฟล์บัญชีภายนอก
Process ที่เรียกจากแอปจะรับ Sandbox ของแอป และการเลือก executable ด้วย bookmark ไม่ได้ให้สิทธิ์ execute
จึงไม่ใช้ direct Process จากแอปเป็นทางเชื่อมจริง และไม่ปิด Sandbox เงียบ ๆ

หลักฐาน: [Apple sandbox violations](https://developer.apple.com/documentation/security/discovering-and-diagnosing-app-sandbox-violations), [sandbox inheritance](https://developer.apple.com/library/archive/documentation/Miscellaneous/Reference/EntitlementKeyReference/Chapters/EnablingAppSandbox.html), [file access](https://developer.apple.com/documentation/security/accessing-files-from-the-macos-app-sandbox)

## ทางเลือกที่แนะนำให้อนุมัติ

คงแอป UI ใน Sandbox แล้วแยก Swift command-line collector ที่ทำงานด้วยสิทธิ์ผู้ใช้ปกติภายนอก Sandbox
ในระยะแรกเปิด collector ด้วย Terminal และหยุดด้วย Ctrl-C; ยังไม่ติดตั้ง LaunchAgent หรือ auto-start

```text
Official Codex CLI ← stdio RPC → quota collector → codex.json
Claude Code statusline → preserving wrapper → claude.json
                                  ↓
                 โฟลเดอร์ quota-only ที่ผู้ใช้เลือก
                                  ↓ read-only bookmark
                       AIUsageBar (App Sandbox)
```

- Collector เรียก native official executable ที่ตรวจแล้ว ไม่ใช้ shell หรือ PATH ที่เปลี่ยนได้ระหว่างรัน
- RPC allowlist: initialize, initialized, account/rateLimits/read; ไม่เริ่ม model turn ไม่ login/logout ไม่ reset credit ไม่ส่งอีเมล
- CLI จัดการ credential ของตัวเองตามการอนุมัติเดิม; โค้ดของเราไม่อ่าน/เก็บ token ไม่เก็บ raw stdout/stderr หรือบัญชี
- Collector ดึงทุก 300 วินาที; timeout/EOF/cancellation ต้องปิดเฉพาะ child ของตัวเอง พร้อม failure snapshot เพื่อไม่แสดงยอดเก่าเหมือนสด
- ส่งเฉพาะ quota ที่ validate แล้ว, source, request/receive time และสถานะที่ไม่เปิดเผยบัญชี; actual freshness ต้องพิสูจน์จาก CLI implementation/version ก่อนระบุ verified observation
- UI เพิ่ม files.user-selected.read-only และ bookmarks.app-scope เท่าที่จำเป็น; NSOpenPanel เลือกโฟลเดอร์ snapshot เฉพาะที่ไม่มี credential
- เลือกโฟลเดอร์แทนไฟล์เพื่อรองรับ atomic replace; snapshot private (directory 0700/file 0600), จำกัดขนาด/schema ไม่ตาม symlink และไม่รับคำสั่ง executable จาก snapshot
- UI ไม่มี network entitlement และไม่อ่าน .codex/.claude; bookmark ใช้ได้เฉพาะช่วงอ่าน แล้วหยุด security scope
- **ปุ่มรีเฟรชของ Codex เปลี่ยนเป็นอ่าน snapshot ล่าสุด** ไม่สั่ง server fetch ทันที; UI ต้องบอกเมื่อ collector หยุด/ข้อมูลเก่า
- Claude wrapper ส่ง stdin ให้ statusline เดิมครบและคง stdout เดิม; snapshot ไม่มี transcript/workspace/email/session ID/raw stdin
- ก่อนสลับบัญชีต้องหยุด/clear snapshot และกำหนดความเป็นเจ้าของ snapshot; ไม่อ้างว่าเป็นบัญชีปัจจุบันหากพิสูจน์ไม่ได้ โดยเฉพาะ Claude หลาย sessions
- เตรียม diff/backup/rollback ก่อนติดตั้ง global; การเขียนนอก workspace ขอ escalation แยกเมื่อพร้อม

## ขอบเขตใหม่ที่รอผู้ใช้ตัดสิน

1. อนุญาต collector ของเรานอก App Sandbox (สิทธิ์ผู้ใช้ปกติ ไม่ใช้ root)
2. เพิ่มสิทธิ์ UI อ่านเฉพาะโฟลเดอร์ snapshot ที่เลือก
3. ยอมรับ manual refresh ของ Codex แบบอ่าน snapshot เช่นเดียวกับ Claude

หากต้องการให้ปุ่ม Codex ดึงใหม่ทันที: ทางเลือกคือ embedded XPC broker แยก process ที่ไม่มี Sandbox ซึ่งเพิ่มขอบเขต IPC/สิทธิ์และงานตรวจ ใช้ [Apple XPC services](https://developer.apple.com/library/archive/documentation/MacOSX/Conceptual/BPSystemStartup/Chapters/CreatingXPCServices.html) เป็นหลักฐานออกแบบ ไม่เลือกอัตโนมัติ

## หลักฐานก่อนใช้งานจริง

- Offline protocol tests: handshake, fragmented JSONL, strict IDs, redacted errors, byte bounds, EOF/server requests
- Transport tests: missing executable, timeout, cancellation, broken pipe, child exit/cleanup โดยใช้ fake process ไม่มีบัญชี
- ตรวจ version/executable/config/effective hosts และ plugin/MCP side effects โดยไม่พิมพ์ secrets ก่อน smoke
- Snapshot schema/atomic/concurrent writer/ownership/account invalidation tests และ Release sandbox file-access smoke
- เทียบ quota จริงกับ CLI/provider ในเครื่อง; ไม่ลง account payload ใน source/log/validation
- ยังไม่ถือว่า T3/T4 เสร็จจนมีหลักฐานจริงครบ

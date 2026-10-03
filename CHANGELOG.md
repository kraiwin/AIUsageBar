# Changelog

บันทึกการเปลี่ยนแปลงที่มีผลต่อผู้ใช้หรือการดูแลโปรเจกต์ ไม่ใช้แทน Git log
รายการใหม่อยู่บนสุด; ย้าย Unreleased ไปใต้เวอร์ชันและวันที่เมื่อ release จริงเท่านั้น

## [Unreleased]

## [0.1.0] - 2026-10-03

เผยแพร่ source ครั้งแรกที่ [kraiwin/AIUsageBar](https://github.com/kraiwin/AIUsageBar) สำหรับ build เอง ไม่มี binary release หรือ tag

### S1–S3 CLI integration (2026-10-03)

- เชื่อม Codex native app-server สอง child ด้วย inventory/registry/config guards, strict envelopes/limits และ owned process-group cleanup
- Claude native statusline wrapper/helper พร้อม quota-only atomic latest/no-data, installer preview/backup/command-only compare-before-write/restore/conflict/fallback
- Real UI/freshness markers, polling5min/throttle1min ข้ามrelaunch, cancellation/generation/retired-task quit และ optional live-window/CLI path selection
- ปิด Sandbox คง Hardened Runtime/ad-hoc; build6; ลด UI/snapshot cadence5sec และ duplicate writes
- เพิ่ม tests/review; final113passed/0failed/0skipped, actual Mac two-provider/install-restore-reinstall/quit/resource smoke ตาม validation ไม่ถือเป็น release
- เพิ่ม MITLICENSE/credits/source-build-use-remove README และ public source checklist; เตรียม source สำหรับ public publication

### Added
- Execution plan S1–S3 สำหรับทำต่อเนื่องใน session ใหม่ พร้อม handoff; ยอมรับ CLI-only personal usage และคง public source preparation เป็นขั้นหลัง implementation
- หลักฐาน prerequisites สอง app-server child: inventory ของ user/project/CLI, runtime registry guard checks, no-spawn canary และ positive control พร้อมขอบเขต default CLI0.160 และ disabled transport placeholder
- Offline Codex JSONL RPC session สำหรับ initialize และ quota read พร้อมขอบเขตข้อมูล/error ที่ไม่เก็บข้อความบัญชี; ยังไม่เรียก CLI จริง
- แผน one-app integration ฉบับ public source-build: single non-Sandbox app + Hardened Runtime, narrow Codex provider, user-confirmed CLI path และ latest Claude snapshot; รอ Claude รีวิวก่อน implementation
- ผล controlled Codex startup probe สำหรับ MCP/hooks/notify พร้อม child-only config guards และ MCP positive control (ไม่ใช่ integration ของแอป)
- เอกสารขอบเขต MVP และค่าตั้งต้นสำหรับแอป macOS Menu Bar ภาษาไทย
- ระบบติดตามสถานะ งานถัดไป เวอร์ชัน และเกณฑ์ตรวจงานก่อนส่งมอบ
- AIUsageBar Engineering Baseline v1 พร้อมแหล่งอ้างอิง กฎ review โค้ด Swift และหลักฐานคุณภาพที่ต้องตรวจ
- หลักฐานเครื่องมือพัฒนาและผลศึกษา official CLI/status-line usage sources พร้อมข้อจำกัดก่อนเชื่อมบัญชีจริง
- แผนพัฒนา MVP 0.1.0 พร้อมลำดับงาน เกณฑ์รับงาน และจุดตัดสินใจก่อนเชื่อมบัญชี
- Native macOS Menu Bar shell ภาษาไทย พร้อมไอคอนของเราและสถานะยังไม่เชื่อมบริการ
- Offline usage model/parser สำหรับ Claude/Codex, freshness และการเก็บสถานะแยกค่าย พร้อม 39 XCTest cases
- Xcode app/test targets และคำสั่ง build/test ที่ทำซ้ำได้; app bundle ปกติไม่มี network/test-only filesystem entitlements
- โหมดตัวอย่าง `--demo` แสดงข้อความสองค่ายบน Menu Bar, native hover tooltip และ preview window พร้อมป้ายตัวเลขสมมติทุกจุด

### Changed
- แก้แผนตาม Claude review: ใช้ app-server inventory+quota สองรอบ/registry+config guard assertions ตามภาคผนวกรีวิว, ข้าม valid unknown notification, resolve npm native binary และติดตั้ง Claude bridge นอก .app; ยังไม่แก้ source
- แก้ข้อมูล handoff ให้ตรงกับสถานะที่มี local Git แล้ว
- ย่อข้อความตัวอย่างบน Menu Bar เป็น `Claude 58% · Codex 72%` ตามคำขอ โดยคงป้ายข้อมูลสมมติใน tooltip และ preview
- เลือก text-only ชื่อเต็มบรรทัดเดียวและตัด icon ซ้ายสุด; renderer รองรับบริการที่เลือกหนึ่งหรือสองค่าย พร้อมตัวเลือกทดลองใน preview

### Fixed
- ไม่ให้ quota ที่ timestamp ผิดกลับมาแสดงหลัง loading/failure
- ไม่แสดง quota ของ metered product อื่นเป็นยอด Codex

มี development build ในเครื่องแล้ว; ยังไม่มี release ที่เผยแพร่หรือการเชื่อมบัญชีจริง

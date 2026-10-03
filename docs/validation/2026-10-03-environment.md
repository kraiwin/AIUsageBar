# ตรวจสภาพแวดล้อมพัฒนา — 2026-10-03

ตรวจบน Mac ที่ใช้ทำงานนี้ โดยยังไม่ได้ build หรือเปิดแอป AIUsageBar

| รายการ | คำสั่ง / วิธีตรวจ | ผล |
|---|---|---|
| Developer directory | `xcode-select -p` | `/Applications/Xcode.app/Contents/Developer` |
| Swift | `swift --version` | Apple Swift 6.4, swiftlang-6.4.0.34.1 |
| Xcode | `xcodebuild -version` | 27.0, build 27A266a |
| macOS SDK | `xcrun --sdk macosx --show-sdk-version` | 27.0 |
| ระบบที่ทดสอบ | `sw_vers` | macOS 27.0.1, build 26A434 |
| Architecture | `uname -m` | arm64 |
| Codex CLI | ตรวจในงาน global config session เดียวกัน | 0.160.0 |
| Claude CLI | ตรวจ executable path เท่านั้น | พบ `~/.local/bin/claude`; ยังไม่ได้ตรวจเวอร์ชัน/เรียกบัญชี |

macOS ขั้นต่ำของแอปยังเป็น 14 ตาม DECISIONS; การมี SDK 27 ไม่ใช่หลักฐานว่าแอปผ่านการทดสอบบน macOS 14
คำสั่ง SDK มี warning เรื่อง fs event/cache ใน sandbox แต่คืนเวอร์ชันได้; ยังไม่มีผล compile จึงไม่ถือว่าการ build ผ่านแล้ว

## Reference

- Clone `steipete/CodexBar` แบบ shallow ลง `reference/CodexBar`
- Commit ที่ตรวจ: `a53a6fe19e62cbeded0bc06a316c2ff1a80cf228`
- LICENSE ที่อ่าน: MIT, Copyright (c) 2026 Peter Steinberger
- ไม่รัน script, build, tests หรือ install ของ reference; ไม่คัดลอกโค้ดมาเป็น source ของเรา

## ตรวจ credential/config แบบไม่เปิดเผยค่า

- พบ `~/.codex/auth.json` โดยตรวจว่ามีไฟล์เท่านั้น ไม่อ่านค่า
- ไม่พบ `~/.claude/.credentials.json`; ยังสรุปสถานะ login หรือ Keychain จากผลนี้ไม่ได้
- พบ Claude statusLine ที่ตั้งไว้และ `~/.claude/statusline.sh`; ยังไม่ได้แก้หรือรัน script เดิม
- ยังไม่เรียก usage endpoint หรือ account/rateLimits/read ด้วยบัญชีจริงในงาน AIUsageBar

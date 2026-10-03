# AIUsageBar

แอป macOS Menu Bar ภาษาไทย แสดงโควตารายสัปดาห์ที่ **เหลือ** ของ Codex และ snapshot ล่าสุดจาก Claude Code ใช้ official CLI ที่ผู้ใช้ติดตั้งและเข้าสู่ระบบเอง ไม่ใช่ API billing และไม่รองรับบัญชีที่ใช้เว็บอย่างเดียว

Development `0.1.0` (build 6) เชื่อม Codex และ Claude snapshot แล้ว ผลล่าสุด 113/113 tests และ Release build ผ่านบน Mac นี้

Public source สำหรับ build เอง: [kraiwin/AIUsageBar](https://github.com/kraiwin/AIUsageBar) เวอร์ชัน source `0.1.0` ไม่มี binary release/tag; การตรวจจริงและข้อจำกัดล่าสุดอยู่ใน [STATUS](docs/STATUS.md)

## Build และเปิดแอป

ต้องมี Xcode พร้อม macOS SDK; deployment target macOS 14 ใช้ Swift และ Apple frameworks เท่านั้น ไม่มี library dependencies ภายนอก ไม่มี shell build phase

1. เปิด `AIUsageBar.xcodeproj` เลือก scheme `AIUsageBar` และ destination เป็น Mac ของคุณ
2. ใช้ signing ที่โปรเจกต์ตั้งไว้: Manual / local ad-hoc (`CODE_SIGN_IDENTITY = -`) ไม่ต้องเลือก team หรือสมัคร Apple Developer Program
3. เลือก Release ใน Product → Scheme → Edit Scheme → Run → Info → Build Configuration แล้ว Build
4. ใน Products คลิกขวา `AIUsageBar.app` → Show in Finder แล้วเปิดแอป คลิกข้อความบน Menu Bar เพื่อเปิดเมนู ไม่มี Dock icon

ใช้ CLI ได้ด้วย:

```sh
xcodebuild -project AIUsageBar.xcodeproj -scheme AIUsageBar \
  -configuration Release -destination 'generic/platform=macOS' \
  -derivedDataPath build/DerivedData build

open build/DerivedData/Build/Products/Release/AIUsageBar.app
```

แอปเดียวปิด App Sandbox และเปิด Hardened Runtime ใช้สิทธิ์ของผู้ใช้ปกติ การแยก provider ไม่ใช่ขอบเขตสิทธิ์ของ OS ไม่ได้ notarize ไม่เข้า App Store และไม่แจก binary ที่มี Developer ID การเปิด source/build ที่ได้มาจากภายนอกอาจมีคำเตือนตามวิธีดาวน์โหลดและ Gatekeeper; ไม่ควรปิด Gatekeeper ทั้งเครื่อง

CLI สามารถเปิดพร้อมเลือก path แบบชัดเจนได้ (ถือเป็นการยืนยัน path):

```sh
open build/DerivedData/Build/Products/Release/AIUsageBar.app --args --codex-path /absolute/path/to/native/codex
```

ใช้ `--show-window` เพื่อเปิดหน้าต่าง live menu สำหรับตรวจด้วย keyboard; `--show-menu` เปิด popover ทันที ทั้งสองใช้ข้อมูลจริง ไม่มีผลเปลี่ยนเป็น demo

## เชื่อม Codex

ติดตั้งและ login official Codex CLI เองก่อน ในเมนูเลือก “เชื่อม Codex CLI…” ตรวจ native path แล้วกดยืนยัน หรือเลือก native executable เอง แอปค้นหา npm/nvm/Homebrew package layout โดยไม่รัน Node หรือ login shell ไม่ตรวจ provenance/signature ของ CLI

ถ้า nvm/package update ทำให้ path เดิมหาย ต้องเลือกใหม่ แอปไม่สลับ executable เงียบ ๆ รองรับ default direct app-server context ที่ตรวจด้วย Codex CLI 0.160.0 ไม่รับรอง named v2 profile/enterprise/MDM/cloud ทุกแบบ เมื่อ registry/config/guards ไม่ตรงจะโหลดไม่สำเร็จแทนการลดข้อจำกัด

ทุกครั้งที่รีเฟรชได้ แอปเปิด native child สองรอบตามลำดับ: inventory แล้ว guarded quota ตรวจ feature registry แบบแบ่งหน้าและ config ก่อน `account/rateLimits/read` ปิด integrations ด้วย child-only overrides และ disabled MCP transport placeholders ไม่แก้ config ของผู้ใช้ ไม่ใช้ `mcp list` เป็น fallback ไม่สร้าง thread/model turn หรือ login/logout RPC

UI/Claude snapshot อ่านทุก 5 วินาที; Codex รีเฟรชทุก 5 นาที และปุ่มรีเฟรช/wake/เปิดแอปใหม่ไม่เรียก Codex ถี่กว่า 1 นาที เก็บเฉพาะเวลาเริ่ม attempt และ CLI path ใน UserDefaults การเปิดตลอด 24 ชั่วโมงตาม cadence ปกติเท่ากับประมาณ 576 child processes; manual refresh/error อาจต่าง ข้อมูลเก่า/โหลดไม่สำเร็จ/ถึงเวลา reset แสดงตามจริง ไม่เปลี่ยน field ที่ขาดเป็น 0

## เชื่อม Claude Code

ต้องมี user-level `~/.claude/settings.json` ที่ตั้ง `statusLine.type` เป็น `command` และมี `statusLine.command` เดิมอยู่แล้ว เลือก “ติดตั้ง Claude Code bridge…” ในเมนู ตรวจ preview ของคำสั่ง/ปลายทางแล้วติดตั้ง แอปสำรองและเปลี่ยนเฉพาะ command token คง padding และ keys อื่น เปรียบเทียบ bytes ก่อนเขียน; ถ้ามี conflict จะไม่เขียนทับ

เปิด Claude Code session ใหม่หรือให้ session โหลด settings ใหม่เพื่อเรียก statusline wrapper คำสั่งเดิมรับ stdin bytes เดิมและส่ง stdout/stderr/exit เหมือนเดิม จากนั้น helper บันทึกเฉพาะ quota แบบ atomic ไม่เก็บ raw stdin, transcript, workspace, session ID, email หรือ credential ไม่เพิ่ม jq/python/node เป็น dependency ของ bridge (คำสั่ง statusline เดิมอาจมี dependencies ของตัวเอง)

snapshot ล่าสุดอาจมาจาก session อื่น ไม่มี account binding ป้าย “จาก Claude Code ล่าสุด เวลา X” คือเวลารับ snapshot **ไม่ใช่เวลาที่ server ยืนยันยอด** project/local/managed statusline overrides อาจทำให้ session นั้นไม่ส่งข้อมูล แอปไม่แก้ project settings

input ที่ valid แต่ไม่มี weekly quota เขียน no-data ทับทันทีและแสดง `—`; invalid input ไม่เป็น 0 และไม่เลื่อนเวลาข้อมูลเก่า การอ่านไฟล์ซ้ำไม่ทำให้ quota สดขึ้น

ติดตั้ง/คืนค่าผ่าน CLI ได้เช่นกัน (preview อาจแสดงคำสั่ง statusline เดิม ควรดูใน terminal ส่วนตัว):

```sh
APP_EXECUTABLE="$PWD/build/DerivedData/Build/Products/Release/AIUsageBar.app/Contents/MacOS/AIUsageBar"
BRIDGE_DIRECTORY="$HOME/Library/Application Support/AIUsageBar"

"$APP_EXECUTABLE" preview "$HOME/.claude/settings.json" "$BRIDGE_DIRECTORY" "$APP_EXECUTABLE"
"$APP_EXECUTABLE" install "$HOME/.claude/settings.json" "$BRIDGE_DIRECTORY" "$APP_EXECUTABLE"

# คืน statusline เดิมก่อนถอดการติดตั้ง
"$APP_EXECUTABLE" disconnect "$HOME/.claude/settings.json" "$BRIDGE_DIRECTORY"
```

CLI `install` เป็นคำสั่ง apply โดยตรง: ผู้ใช้ควรตรวจ `preview` ก่อน ไฟล์ wrapper/helper อยู่ใน Application Support จึงไม่ย้ายตาม app ถ้า helper หาย คำสั่งเดิมยังทำงาน ถ้า wrapper หาย คำสั่ง settings มี fallback ไปคำสั่งเดิม อย่าลบทั้งโฟลเดอร์เองเมื่อมี conflict

## ข้อความบน Menu Bar และการถอดการติดตั้ง

`Claude — · Codex —` หมายถึงยังไม่มีค่าที่แสดงได้; `0%` คือเหลือ 0 จริง เครื่องหมาย `*` คือ Claude snapshot และ `~` คือข้อมูลเก่าหรือครั้งก่อน รายละเอียดสถานะและเวลา reset อยู่ในเมนู/tooltip ไม่แสดงเลขเก่าที่ reset ผ่านแล้วเป็นค่าปัจจุบัน เวลาใช้ timezone ของเครื่อง

“ตัดการเชื่อม Codex” หยุด request และลบ path ที่เลือก “คืน statusline เดิม / ตัด Claude” คืนเฉพาะ command ของ installation ที่เราเป็นเจ้าของแล้วลบ owned artifacts ถ้า command หรือไฟล์ของเราเปลี่ยน จะให้แก้ conflict ก่อน ไม่เขียนทับข้อมูลใหม่ของผู้ใช้ จากนั้นออกจากแอปและลบ `.app` ได้ ไม่มี updater, analytics หรือ telemetry ของแอป

## ไฟล์และโปรแกรมที่ใช้

| รายการ | หน้าที่และผลข้างเคียง |
|---|---|
| native Codex CLI ที่ยืนยัน | stdio RPC สอง child ต่อ refresh; official CLI จัดการ auth/Keychain/network/refresh/state/logs ของตัวเอง |
| Codex-resolved config ผ่าน RPC | bounded in-memory inventory/guard assertions; ไม่อ่าน/parse TOML layering เอง ไม่ cache transport/env/header ไม่แก้ config |
| `~/.claude/settings.json` | user-level command-only edit มี compare-before-write/backup/restore |
| `~/Library/Application Support/AIUsageBar/claude-wrapper`, `claude-helper` | native executable ที่สำเนาจาก build ของเรา รองรับ original-command fallback |
| `claude-latest.json` ใน directory เดียวกัน | private quota-only atomic snapshot หรือ no-data tombstone |
| `claude-settings-backup.json`, `claude-install.json` | private original settings backup และ ownership metadata ไม่ควรแชร์/commit |
| UserDefaults `local.aiusagebar.app` | CLI path และเวลา request ล่าสุด ไม่มี token |
| `build/` และ `reference/` | local artifacts/reference ที่ gitignored ไม่ใช่ runtime dependencies |

ไม่มี token/cookie reader/cache ของเรา และไม่อ่าน browser cookies CLI อาจเข้าถึง network และเขียน credential/state/logs ของตัวเองตาม config การตรวจ guard ไม่ใช่ firewall ที่บังคับ host ทุก packet; `remote_control` เป็น Removed/no-op ในรุ่นที่ตรวจ จึงไม่ใช้เป็นหลักฐานปิดฟังก์ชัน

## Tests และข้อจำกัด

```sh
xcodebuild -project AIUsageBar.xcodeproj -scheme AIUsageBar \
  -configuration Debug -destination 'platform=macOS,arch=arm64' \
  -derivedDataPath build/DerivedData test
```

ใช้ synthetic fixtures ใน unit tests ไม่ต้องมีบัญชีจริง Real CLI smoke บันทึกเฉพาะผลตรวจที่ตัดข้อมูลบัญชีแล้ว ดู [STATUS](docs/STATUS.md) และ `docs/validation/` สำหรับผลจริง ยังไม่รับรอง runtime macOS 14 หรือ Intel หากไม่มีหลักฐานที่ระบุไว้

โหมดตัวอย่างใช้เฉพาะ `--demo`:

```sh
open build/DerivedData/Build/Products/Release/AIUsageBar.app --args --demo
```

ออกจาก instance ปกติก่อนเปิดตัวอย่าง; ตัวเลข demo สมมติและแยกจาก provider/cache

ได้รับแรงบันดาลใจจาก CodexBar และพัฒนาขึ้นใหม่ ดู [เครดิต](THIRD_PARTY_NOTICES.md) Source ใช้ [MIT](LICENSE) ไม่ใช่แอปทางการของ OpenAI หรือ Anthropic

เอกสารหลัก: [DECISIONS](docs/DECISIONS.md), [DEVELOPMENT](docs/DEVELOPMENT.md), [execution plan](docs/plans/2026-10-03-s1-s3-execution-plan.md), [MEMORY](MEMORY.md)

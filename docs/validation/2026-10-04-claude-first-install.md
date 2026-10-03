# Claude first-install fix — 2026-10-04

## ผลจริง

- เพิ่ม quota-only `collect` เมื่อ user-level settings ไม่มี `statusLine`; ไม่ต้องมีคำสั่ง statusline เดิม และรองรับ settings.json ที่ยังไม่มี
- Preview ไม่เขียนไฟล์; apply เปรียบเทียบ bytes/absence ก่อนเขียนและสร้างไฟล์ใหม่ด้วย exclusive atomic rename เพื่อไม่ทับไฟล์ที่เกิดขึ้นระหว่างตรวจและเขียน
- Existing command: เปลี่ยน/คืนเฉพาะ command token ตามเดิม คง padding และ unrelated bytes
- Quota-only: เพิ่ม/ลบเฉพาะ statusLine ที่เป็นเจ้าของ; หากมี fields เพิ่มใน statusLine ให้ conflict; unrelated keys ที่เพิ่มภายหลังคงอยู่
- Originally absent settings: ลบไฟล์ที่สร้างเฉพาะเมื่อเนื้อหาที่คืนยังตรง backup; หากมี unrelated edits คงไฟล์ไว้ อาจคง directory ที่สร้างไว้
- Ownership metadata schema1 เดิมที่ไม่มี settingsExisted ยังคง restore ได้
- collect เก็บ quota/no-data ผ่าน parser เดิม ไม่มี stdout/stderr และคืน exit0 แม้ input ผิด; ไม่เก็บ raw input

## Verification

```sh
xcodebuild -project AIUsageBar.xcodeproj -scheme AIUsageBar -configuration Debug \
  -destination 'platform=macOS,arch=arm64' -derivedDataPath build/BridgeFix test
xcodebuild -project AIUsageBar.xcodeproj -scheme AIUsageBar -configuration Release \
  -destination 'generic/platform=macOS' -derivedDataPath build/BridgeFixRelease build
```

- Final XCTest: 120 tests, 0 failures, 0 skips; TEST SUCCEEDED (`build/bridge-fix-test.log`)
- Final Release: BUILD SUCCEEDED (`build/bridge-fix-release.log`)
- Release CLI smoke 2/2 synthetic cases: existing settings without statusLine และ missing settings → install → execute installed shell command → silent quota-only snapshot → disconnect → exact restoration/file absence. Fixture อยู่ใน temporary directory ใต้ build และลบหลังตรวจ ไม่แตะ ~/.claude จริง
- Waluigi independent-context review พบ shell command ขาด ] ระหว่างทำงาน แก้แล้ว เพิ่ม test ที่เรียก installedCommand จริง และทดสอบซ้ำ Reviewer ตรวจ insertion/removal แยก 79 กรณี และตรวจ exclusive rename/legacy metadata delta ไม่พบ blocker คงค้าง
- Reviewer เป็น native Codex ไม่ใช่ Claude/model review ผู้แก้รอบนี้คือ main Codex ไม่ใช่ Donkey Kong
- git diff --check ผ่าน ไม่เพิ่ม dependency ไม่อ่าน credential
- ไม่ได้ติดตั้ง/อัปเดต app หรือ bridge ในบัญชีจริง ไม่ได้ทดสอบ UI preview ใหม่บน Mac จริง ไม่ commit/push รอบนี้

## ข้อสังเกตอื่น

1. คง --strict-config: [Codex v0.160.0 CLI source](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/cli/src/main.rs) ระบุว่าปฏิเสธ config fields ที่รุ่นนั้นไม่รู้จัก จึงเป็น compatibility risk จริง แต่ยังไม่มี diagnostic ยืนยันว่าสาเหตุของ load failure เดิมมาจาก flag นี้ Removing flag จะยังมี resolved-config/registry guards เฉพาะ keys ที่เราตรวจ แต่ลดขอบเขต fail-closed สำหรับ unknown fields จึงยังไม่เปลี่ยนในรอบแก้ first install
2. README เพิ่มขั้นตอน disconnect/reinstall หลัง build ใหม่ เพราะ helper/wrapper เป็นสำเนา executable ไม่มี updater
3. Wrapper/fallback ใช้ /bin/sh -c จริง README ระบุข้อจำกัด inline zsh syntax และการเลือก shell ชัดเจน [Claude Code statusline docs](https://code.claude.com/docs/en/statusline) ระบุ shell และยกตัวอย่าง Bash แต่ยังไม่มีหลักฐานว่า wrapper เปลี่ยนจาก shell ใดของ Claude เดิม
4. ลบ Tools/ClaudeBridge/main.swift ที่ไม่มี build/reference entrypoint; app entrypoint ยัง dispatch ClaudeBridgeCommand ตามเดิม
5. คง internal project docs ตาม workflow ที่กำหนด เพิ่มคำอธิบายใน README ว่าเป็นเอกสารพัฒนา/รับช่วง ไม่ใช่ขั้นตอนติดตั้ง

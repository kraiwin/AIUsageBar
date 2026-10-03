# Public source publication — 2026-10-03

ผู้ใช้ยืนยัน owner ของตน (`kraiwin`) และตอบ “go” ต่อข้อเสนอ `kraiwin/AIUsageBar`, public, README ภาษาไทย จึงอนุญาตสร้าง repo/remote, commit และ push source รอบนี้ ตาม DECISIONS ข้อ21

## Gate และ scope

- รัน local/offline XCTest รอบ ship ใหม่: `xcodebuild -project AIUsageBar.xcodeproj -scheme AIUsageBar -configuration Debug -destination 'platform=macOS,arch=arm64' -derivedDataPath build/DerivedData -resultBundlePath build/TestResults/Ship-001.xcresult test` ผ่าน **113/0/0** ตรวจด้วย xcresulttool ไม่เรียก account/usage integration เป็น gate
- Release0.1.0/build6 ไม่เปลี่ยน executable/version metadata จาก validated Release ใน S1–S3; เป็น planned first source version จึงไม่ bump เป็น0.2.0 และไม่สร้าง tag/binary/GitHub Release
- ตรวจ staged source84files: ไม่มี build/reference/cache/auth/account fixtures/symlinks/credential patterns หรือ unrelated private config-backup workflow ใน source snapshot; independent read-only corpus review ไม่พบ blockerค้าง `git diff --cached --check` ผ่าน
- ตัด private/unrelated workflow จาก public handoffs/MEMORY/decision/status docs และ anonymize absolute home paths; original local copiesอยู่ `build/PublicationPrivateDocs/` (gitignored/private permissions) ไม่ส่งไป GitHub
- Initial local scaffold historyมี referenceถึงprojectอื่น จึงเผยแพร่ clean root snapshot ที่ treeตรงกับ staged sourceทุกไฟล์ ด้วย isolated worktree/normal commit เก็บ scaffoldเดิมใน local-only `local-history/pre-publication` ไม่ reset/amend/force-push และไม่ส่ง branchนั้น
- ใช้ commit author `kraiwin` กับ GitHub noreply email ใน repo-local config ไม่เผย personal email ใน public commitใหม่ ไม่มีการแก้ global Git/Claude/Codex credential config

## ผลการเผยแพร่ที่อ่านกลับ

- `gh repo create kraiwin/AIUsageBar --public --description ...` สำเร็จ ตรวจกลับ PUBLIC/emptyก่อน upload
- Source root commit **`2f0fe19af6aa04f8501498a48a6a63ebf1fa2979`** — `feat: release 0.1.0 source build`
- `git push -u origin main` สำเร็จ แล้ว `git ls-remote origin refs/heads/main` ตรง local source HEADข้างต้น; GitHub API `commits/main` ตรงกันและ parentsว่าง ยืนยัน clean root source history
- GitHub API repo readback: owner/name `kraiwin/AIUsageBar`, visibility **PUBLIC**, default branch **main**, not empty
- URL: [https://github.com/kraiwin/AIUsageBar](https://github.com/kraiwin/AIUsageBar)
- หลังอ่านกลับ อัปเดต publication docs ใน commitแยกเพื่อให้สถานะไม่ค้างว่า local-only; source/code/versionไม่เปลี่ยน จึงไม่รัน unit testsซ้ำเพียงเพื่อเอกสาร

ไม่ได้สร้าง CI/deployment, GitHub Release/tag, notarization หรือแจกbinary การผ่าน local gateไม่ใช่การรับรอง runtime macOS14/Intel หรือ audibleVoiceOver; ข้อจำกัดจริงคงอยู่ใน S1–S3 validation/README

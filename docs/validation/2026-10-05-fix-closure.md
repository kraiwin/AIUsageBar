# ปิดชุด first-install / restore retry fix — 2026-10-05

## ผลจริง

- คง version0.1.0/build6, Swift/Apple frameworks, strict-config และ Codex guards; ไม่มี dependency/credential reader/updater เพิ่ม
- Restore retry เก็บ private checkpoint ผูก exact restored settings digest หรือ absence และ backup digest ก่อน cleanup ลบ wrapper→helper→snapshot→backup→metadata พร้อม fsync แต่ละ boundary; metadata เดิมไม่มี checkpoint ยังคงคืนค่าได้
- รองรับ interruption หลังลบ settings ที่เดิมไม่มี และหลังลบ backup; ปฏิเสธ settings ที่สร้างกลับ/แก้ไข, backup/binary ที่เปลี่ยน และ symlink/hardlink/สิทธิ์ไม่ปลอดภัย ไม่ลบ unrelated user files
- Preview ปฏิเสธ BOM/UTF-16 ที่ scanner ไม่รองรับก่อน mutation; เก็บ regression สำหรับ BOM objects มี/ไม่มี statusLine
- Codex error hint ระบุ config incompatibility เป็นสาเหตุที่เป็นไปได้โดยไม่แสดง raw stderr; ไม่ลด strict-config

## Tests/build/review

```sh
xcodebuild -project AIUsageBar.xcodeproj -scheme AIUsageBar -configuration Debug \
  -destination 'platform=macOS,arch=arm64' -derivedDataPath build/OctoberCloseTests test
xcodebuild -project AIUsageBar.xcodeproj -scheme AIUsageBar -configuration Release \
  -destination 'generic/platform=macOS' -derivedDataPath build/OctoberCloseRelease build
```

- Final post-BOM XCTest: **126 tests, 0 failures, 0 skipped**, TEST SUCCEEDED; `build/october-close-tests.log`
- Final post-BOM Release: BUILD SUCCEEDED; `build/october-close-release.log`
- Release synthetic CLI smoke **2/2**: existing settings without statusLine / absent settings → preview no mutation → install → execute shell command → silent quota-only snapshot → disconnect exact bytes/absence. Temporary fixtures under build removed afterward; no real settings deletion to simulate first install
- Retry regressions cover 15 cleanup-boundary recoveries across existing command, quota-only and absent settings, precheckpoint absence, conflicts and unsafe remaining artifacts
- Diddy Kong implemented retry/tests; Waluigi independent-context native Codex review found BOM P2, fixed by main + regression by Diddy Kong; final review has no material finding remaining. This is not Claude or independent-model review
- git diff --check passed; source scope/secret checks completed before commit. Private baseline/settings/metadata/logs stay in ignored build or Application Support, not publication

## Real Mac UI and installed artifacts

- Computer Use initial app lookup timed out; local Mac inventory succeeded, then exact Release app path resolved. No browser/device switch
- Found multiple older local app instances; stopped them before final UI smoke to ensure one new instance
- Final Release optional live window inspected: Thai provider states, connect/install/disconnect/refresh/quit controls present
- UI restore of old bridge → original settings matched private backup byte-for-byte; all owned artifacts removed
- UI preview/install of new bridge → backup matched original and wrapper/helper SHA-256 matched final Release executable
- UI disconnect of new bridge → original bytes restored exactly and owned artifacts removed
- UI preview/reinstall → final backup/binary checks passed; preview command contents filtered from tool output
- Live Codex quota loaded successfully with unchanged strict-config. No raw account/CLI payload stored in this report
- Cmd-Q terminated all AIUsageBar instances; reopened final Release without inspection flags. One process runs from `build/OctoberCloseRelease/Build/Products/Release/AIUsageBar.app/Contents/MacOS/AIUsageBar`; default Menu Bar startup verified by launch arguments/source. Computer Use app observation times out when no window is present, so normal-mode screenshot is not claimed
- Claude snapshot cleared by disconnect as designed; awaiting actual Claude Code session to load updated settings and send next snapshot. Did not create a model turn merely to refresh quota

## Limits and publication

Real Mac flow preserves existing settings/statusline; it is not real-account absent-settings first-install coverage. First-install and interrupted cleanup use synthetic tests. Audible VoiceOver, actual sleep/offline/timezone, macOS14/Intel remain unverified. No tag, binary release, notarization or Apple Developer enrollment.

Source destination: `origin/main` at `https://github.com/kraiwin/AIUsageBar.git`, explicitly authorized by user. Commit/push result is recorded in Git and final response; no local-history branch is published.

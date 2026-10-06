# Changelog

บันทึกการเปลี่ยนแปลงที่มีผลต่อผู้ใช้หรือการดูแลโปรเจกต์ ไม่ใช้แทน Git log
รายการใหม่อยู่บนสุด; ย้าย Unreleased ไปใต้เวอร์ชันและวันที่เมื่อ release จริงเท่านั้น

## [Unreleased]

- Windows live edition: Codex quota จริงผ่าน guarded app-server, Claude statusline bridge พร้อมสำรอง/คืนค่าเดิม และ tray ภาษาไทย/รีเฟรช 5 นาที
- เพิ่ม31เคส: offline141/141, focused10+15+6, Release0warning/error, live Codex และ tray smoke exit0; Claude UI จริงยังรอ owner ตรวจ

- Actual Codex CLI review ของ native H2 implementation plan ได้ fix-then-proceed (5Major/1Minor/1Info): lifecycle, cleanup/accounting และ consumer hash binding ยังต้องแก้ในแผน. เก็บ verdict/provenance/dispositions; ยังไม่แก้ source หรือรัน native.

- รวมร่าง native H2 implementation plan หลังจุดพัก: callback ตรวจ guard ก่อน Create/Resume, fixture/comment/sentinel และขอบเขต metadata/cleanup; self-gate source22 hashes ผ่าน. ยังไม่เริ่ม actual CLI plan review หรือแก้ app source/รัน native.

- เพิ่ม native H2 readiness source packet และ observation manifest: แยก conditional secret-route exclusions, bundled skills/reload, synthetic Git boundary และ implicit initialize account work จาก runtime coverage ที่ยังไม่ทราบ. เป็น read-only planning; ยังไม่เพิ่ม source/native route หรือรันจริง. Actual CLI delta ให้ PROCEED เฉพาะ readiness contract หลังแก้1Major/2Minor/1Info; 0findingใหม่. ครบ9loader contexts/fallback และ held-image object/launch-path binding gates; native admission ยัง STOP.

- เพิ่ม H2 preparation ตามแผนที่ owner อนุมัติ: internal per-instance inventory whitelist, typed bounded validators, literal wire fixtures และ26 fake/offline cases. แก้ nullable guard และเพิ่ม test-only process teardown barrier หลังพบ scratch-sharing race; ไม่แก้ production cleanup หรือเพิ่ม native route. หลักฐาน build/focused/full อยู่ใน docs/validation/2026-10-06-windows-h2-preparation.md; native run/บัญชีจริงยังแยก approval.

- เพิ่ม C1 startup/platform/distribution closure และ H2 decision packet หลังH0: conditional model-fetch/plugins/initialize gates, publicOSstore vs providersecrets, package/hash/provenance limitationsและ sourcechecksums. Native GPT reviewปรับguard/evidencecontractครบ แต่ยังไม่มี native run/source changes; รอ ownerเลือกระดับหลักฐานก่อนconcrete H2 plan

- แก้ H0 privilege guard หลัง same-token differential ยืนยัน TokenElevation ต้องใช้ query capacity4บน host นี้แทน4096; other-class controlsผ่าน. Team staticreview/parser/hashก่อน corrected replayหนึ่งครั้งได้ metadata-clear/error0, system targetsabsent-by-parent. Scopeเฉพาะmetadata ไม่ใช้บัญชี/CLIจริง ไม่แก้ appsource; C1/C5/H1/H2 gatesคงเดิม

- ดำเนิน H0 in-memory preflight หนึ่งครั้งตาม owner instruction: static review/parser/hash ผ่านก่อน invoke แต่ runtime หยุด inconclusive/privilege/error24 ก่อนตรวจ system config metadata. บันทึก14-field resultตามจริง ไม่รันซ้ำ ไม่เรียก CLI/บัญชีจริงหรือแก้ app source; C1/C5/H1/H2 ยัง gated

- เตรียม H0 host metadata plan ตามคำสั่ง planning-only/no real accounts: ลดจาก H0/H1 harness เป็น in-memory PowerShell command ไม่มี filesystem artifact. Actual Codex CLI final180638 ให้ PROCEED for PLAN;14-field safe schema/token/known-folder/cleanup/source-hash contractsครบ. ยังรอ owner plan/dispatch ไม่ author/run คำสั่ง ไม่แก้ app source/build/probe/settings/accounts; H1/H2/C1/C5 ยัง gated

- เพิ่ม C1/C5 read-only team studies และ decision proposal: pinned auth/bootstrap/network-effect map, disposableguest isolation requirements และ C5 upstreamcaller admission gap. Independent native GPT skeptic finding quotaRPCscopeแก้แล้ว ไม่มีfindingค้าง. C1/C5ยังblocked; ไม่แก้ app/harness source ไม่build/probe/install/account/settings/commit/push ไม่ถือเป็นimplementation-readyplan

- ปิด Windows C6 deterministic proof: internal readonly per-instance callback ตรงก่อน ReplaceFileW และ test friend assembly; เพิ่ม4กรณี actualWin32 race/controls/security/closed-handle exact recovery pathname survival. Release0warnings/errors, focused4/4/full offline84/84ไม่มีskip และ independent native GPT review ไม่มีfindingค้าง. เป็น conflict detection+retained evidence ไม่ใช่CAS/rollback; C1/C5ยังblocked ไม่มีnative/account/settings/install/commit/push

- เพิ่ม handoff Windows C6 plan-ready สำหรับ sessionใหม่: exactactualreview/proof/state/ownership/approval boundary และงานค้าง C1/C5; MEMORYชี้ latesthandoff ไม่มีsourcechange/commit/pushในคำขอhandoff

- เพิ่มarchitect Windows C6-only blocker amendment และ actualCodexplanreview/dispositions ครบถึงfocusedPROCEEDforPLAN: deterministic realWin32raceผ่านinternalper-instance testseam, initialexpectedadversaryID/bytes+closedhandlepathname-survival controls. ยังไม่แก้source/รันtestsใหม่ รอownerplan/dispatch C1/C5strictgatesยังคงเดิม

- เพิ่ม Windows W1 offline edition แยกใต้windows/: C#/.NET10/WinForms trayไทย/iconของเรา, strictusage/freshness/throttle/state/fakeRPC, suspendedJob/pipe transport, privatequota-onlysnapshotและinert/scratchsink. Lead Release0warnings/errors,80offline testsและnativeown-windowtray/singleinstance smokeผ่าน ไม่มีrealCLI/account/settingsinstallหรือMacsourcechanges
- W2a experimental proofแยกจากmandatorysuite: tee34/9/43ยังunsupportedจากdescendantcleanup/EOF/startup; externaleditorracefinalinconclusive. บันทึกpinnedC1knownfolderblockerและJobcounterattemptdetectionlimitation คงnative/productionbridgegates ไม่ถือว่าWindowsliveusageพร้อม ไม่มีcommit/push/release

- Reconcile Windows plan C1–C7 และ actual Claude delta review ผ่าน global opus/high สองรอบ; แก้ D1–D8/E1–E3 พร้อม verbatim evidence/dispositions. แผนพร้อมเสนอ W1/W2a dispatch ตาม conditional review และ lead self-check; เลื่อน native harness จน C1 audit ครบ, ระบุ tee EOF/exit/all-descendants cleanup และ no-EOF sink discard ชัดเจน ยังไม่มี Windows source/build/tests หรือ commit/push

- เพิ่มhandoffรับช่วงWindowsdraftC1–C7และactualClaudere-reviewผ่านgloballauncherที่ติดตั้งแล้ว แยกtoolsmoke/testsออกจากWindowsruntimeproof และแก้capacityprovenanceเป็นCodexตามหลักฐานที่ผู้ใช้นำมา ยังไม่มีWindowssourceหรือreviewซ้ำสำเร็จ

- เพิ่ม actual Claude Windows plan review พร้อม provenance/verbatim report/lead triage: fix-then-proceed และ 7 pending dispositions ก่อน dispatch ยังไม่เปลี่ยน source หรืออ้าง Windows runtime ผ่าน

- Owner อนุมัติ Windows C#/.NET10/WinForms/native Windows11 x64/trayไทย และ W1–W2 offline/isolated; บันทึก scope ใน decisions/spec/plan รอ dispatch gate ก่อนสร้าง source ยังไม่มี Windows build/tests

- ตรวจสภาพ PC/native CLI/WSL และเพิ่มแผน Windows system-tray MVP เสนอ C#/.NET10/WinForms/native Windowsก่อน แยก windows/ คง Mac sourceเดิม; stack/contextและimplementationยังรอ owner อนุมัติ ไม่มีWindows build/testหรือaccount/bridge integrationในรอบวางแผน

- เพิ่ม handoff Windows สำหรับเริ่มบน PC ใน repoเดิม แยก windows/ พร้อมรายการตรวจ native/WSL และแผนก่อนimplementation; ยังไม่รองรับ Windows ในแอปปัจจุบัน

- แก้ restore retry หลัง interruption: เก็บ checkpoint ส่วนตัวก่อน cleanup ลบ metadata ท้ายสุด ตรวจ settings/backup digest และ ownership ของไฟล์ที่เหลือ
- ปฏิเสธ settings encoding ที่ byte scanner ไม่รองรับ (เช่น BOM/UTF-16) ก่อนเขียน ป้องกัน JSON เสียระหว่าง first install

- เพิ่มข้อความ Codex load failure สำหรับ CLI ที่หยุดหรือ config ไม่รองรับ: ระบุว่า unknown config ของ CLI อาจเป็นสาเหตุ ไม่แสดง raw diagnostics และคง strict-config

- แก้ Claude bridge ให้ติดตั้งได้เมื่อไม่เคยตั้ง statusline หรือยังไม่มี settings.json: เก็บ quota อย่างเดียวแบบเงียบ พร้อม restore/conflict protection และรักษา unrelated edits
- เพิ่มวิธีอัปเดต wrapper/helper หลัง build และข้อจำกัด shell ใน README; ชี้แยกเอกสารสำหรับผู้พัฒนา
- ลบ standalone Tools/ClaudeBridge/main.swift ที่ไม่อยู่ใน build; entrypoint ใช้ executable ของแอปตามเดิม

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

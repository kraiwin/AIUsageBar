# สถานะ AIUsageBar

อัปเดต: 2026-10-06 · **พร้อมใช้ส่วนตัวผ่าน Codex CLI/Claude Code บน Mac นี้** ตาม S1–S3; เผยแพร่ public source แล้วที่ [kraiwin/AIUsageBar](https://github.com/kraiwin/AIUsageBar)

## Windows live edition

S1–S4: Codex native quota, Claude statusline bridge/Install/Restore, tray ภาษาไทย และเอกสารใช้งานเสร็จตามแผนที่ owner อนุมัติ
Release build 0 warning/error; offline **141 passed / 0 failed / 0 skipped** (เดิม110 + ใหม่31); focused native10/10, bridge15/15, usage-text6/6
Codex live exit0 ได้ weekly_remaining 0–100; tray smoke exit0 (native disabled ใน smoke) ไม่แตะ Claude settings จริงระหว่างเทส
Owner ยังต้องตรวจด้วยตา: เปิด tray ดู Codex → เชื่อม Claude → ส่งข้อความ → เปิดเมนูใหม่ดู snapshot → ยกเลิกตรวจค่าเดิม; [รายงานและข้อจำกัด](plans/2026-10-06-windows-live.codex-report.md)
สถานะ/gates ของ native H2 ด้านล่างเป็นประวัติก่อน owner อนุมัติ live edition; ไม่มี commit/push รอบนี้

## Native H2 implementation plan — actual review requires fixes

DECISIONS37 one actual CLI review completed: **fix-then-proceed —5Major/1Minor/1Info**, CLI0.160.0/gpt-6.1-sol/OpenAI/medium, exit0; recorded tool-action markers0. [Exact review/dispositions](plans/2026-10-06-windows-h2-native-implementation-review.md) bind the immutable136744byte packet and actual raw2187–2207. Exit0 means review execution completed, not plan approval.

Major corrections: existing shared Finish budget, copied terminal snapshot versus live accounting, prepare/run fixture lifecycle, executing-consumer code/hash validation, and independent signal check after termination failure. Minor separates access uncertainty from execution failure; Info requires limited namespace acceptance record. All accepted, **not fixed yet**; no delta review or expanded study in this bounded review/report segment.

The [eight-file draft](plans/2026-10-06-windows-h2-native-implementation.plan.md) is NOT implementation-ready/source-approved.26 app-source baseline entries unchanged; no build/test/H0/native experiment. Next bounded segment: correct named plan contracts, self-gate, then actual delta review. Owner plan+dispatch and native run gates remain separate. Earlier readiness/110fakeproof below remain historical.

## Native H2 readiness — reviewed contract complete; admission STOP

DECISIONS34 authorizes read-only source/observation planning only. [Secret-path source packet](validation/2026-10-06-windows-h2-secret-path-closure.md) and [observation manifest](validation/2026-10-06-windows-h2-observation-manifest.md) identify conditional route exclusions, bundled-skills false policy, regular synthetic Git/HEAD boundary, nine distinct loader contexts and initialize's internal account work. Lead verified14 source hashes; selected-source closure is not whole-binary/runtime absence evidence.

[Readiness plan](plans/2026-10-06-windows-h2-native-readiness.plan.md) and [actual CLI review](plans/2026-10-06-windows-h2-native-readiness-review.md): first121702 fix-then-proceed1Major/2Minor/1Info. Accepted fixes explicitly bind no-cwd reload/startup clone fallback, held-image-object versus launch-path selection, scoped completion/provenance and no CLIENT account/auth RPC distinction. Independent native GPT countercheck accepted the amended nine-context table. Actual CLI delta122505: **PROCEED for readiness contract**, 0 new Major/0 Minor/0 Info, exit0; prior findings closed at plan level. Lead read raw1151–1166, checked input hashes unchanged and recorded no tool-action markers. CLI coverage is selected excerpts;14 source hashes were independently checked by source readers/lead.

Native admission remains **STOP** until an exact native-specific policy/fixture/layer/scratch/image/primary/context/machine manifest and separately reviewed implementation/owner plan+dispatch/run gates exist. No source/SDK/build/test/native experiment or provider account/config/auth contents in this round; model review is the separately authorized CLI workflow. Previous fake110/110 remains historical below; C1/C5 strict coverage remains blocked. Next: a concrete native-specific fixture/identity/context/validator implementation plan with exact source files and negative tests; no automatic source dispatch or native invocation.

## H2 bounded preparation — complete; native remains gated

Owner approved both plan and dispatch (DECISIONS33). Implementation is limited to CodexProvider.cs internal immutable inventory whitelist, new H2InventoryTests.cs and Program.cs fake/offline routing. No native route. [Plan](plans/2026-10-06-windows-h2-inventory.plan.md) received [actual Codex CLI plan review](plans/2026-10-06-windows-h2-inventory-review.md): final PROCEED, 0 Major/0 Minor/2 Info. Implementation uses existing Core/QA/review research threads; independent native GPT static review accepted the final delta.

[Preparation evidence](validation/2026-10-06-windows-h2-preparation.md) retains initial compiler failure and intermittent scratch-sharing failures. Explicit nullable checks and test-only retained primary-process signal waits corrected the observed paths without production cleanup changes. Final Release build: 0 warnings/errors; corrected cleanup replay 13/13 and focused H2 26/26, no skips. Final full mandatory offline suite: **110 passed, 0 failed, 0 skipped**, 18061 ms, exit0. Corrected cleanup passes across targeted/focused/full runs; this is observed mitigation, not universal OS-ordering proof.

Native H2 requires a separately reviewed amendment/run approval, provider-secret-path closure and observation manifest. C1/C5/H1 remain blocked; H0 metadata-clear remains point-in-time evidence. No real provider/account/settings/install/commit/push/release in preparation.

## C1 source closure after H0 — decision scope pending, no native run

Read-only continuationตาม DECISIONS31 ส่งมอบ [startup closure](validation/2026-10-06-windows-c1-startup-closure.md) และ [host/distribution contract](validation/2026-10-06-windows-c1-host-contract.md). ปิด5source groupsและตรวจ28official source hashes/6crate checksums; packageversion/tag/attestation payloadตรงsourcepinและเก็บphysicalEXEhash แต่ไม่อ้างsignature verification/tarball comparison/source equivalence

Model Online refreshมีauth/provider predicates ไม่ใช่ทุกstartupต้องHTTP; syntheticต้อง authNone/defaultprovider/no customcommand/env_key/bearer/catalog/baseURL/providerselection/api_key_model_discoveryfalseครบ. Plugin startupมีreal gate; initializeต้อง ownname+explicitGatewayOauthtrueเพื่อไม่เข้าDesktopregistration/auto-loginrelaxation. PublicOS ROOTcerts/knownfoldersไม่ใช่ providersecrets. IndependentnativeGPTreview2reportsแล้วปรับfullguardconjunctionและstrict-vs-boundedevidence distinctionครบ

[H2 decision packet](plans/2026-10-06-windows-h2-inventory-decision.md) เสนอเลือก strict isolation หรือ bounded inventory ที่ยอมรับnormalOSreads/scratchstate/possibleunauthenticatedattempts แต่providercredential/accountRPCยังห้าม. Optionalquestionรอownerเลือกscope; ไม่มีคำตอบ/defaultไม่ใช่อนุมัติ. ยังไม่implementation-ready/actualCLIreview และarchitectdispatchใหม่ติดthreadlimit ไม่อ้างว่าได้architectplan. ไม่มีnativeCLI/appserver/account/configcontents/source/build/install/settings/commit/pushในรอบนี้; C1/C5/H1/H2ยังgated, H0metadata-clearเดิมไม่เปลี่ยน

## H0 guard fixed — corrected replay metadata-clear

Owner อนุมัติตรวจ root cause/แก้ guard/team review/รัน H0 ใหม่หนึ่งครั้ง (DECISIONS30). [Guard fix validation](validation/2026-10-06-windows-h0-guard-fix.md) ยืนยันจาก token handle เดียว: TokenElevation capacity4096 fail/error24 แต่4bytesผ่าน; Statistics/Integrity controls4096ผ่าน จึงไม่ใช่ blanket buffer/ABI/access failure. แก้เฉพาะ capacity ของ class20, author+independent native GPT ตรวจ delta, parser0errors และ immediate source hash ก่อน ONE corrected H0 replay

ผล replay: **metadata-clear/category none/apiError0**, privilege/known-folder/local-drive/ancestry flags true; config/requirements ทั้งคู่ **absent-by-parent**. ไม่พบ parent ในnamespaceที่ตรวจจึงไม่ probe targetต่อ ไม่อ่าน contents และไม่แสดง rawpath. Exit0/wall1.1662652s; fixed14-field record. ผลแรก inconclusive เก็บไว้ด้านล่าง ไม่แก้ย้อนหลังว่าเคยผ่าน

ไม่มี app source/code file/CLI/provider account/quota/SDK/build/install/settings/commit/push.26source hashesคงเดิม. H0 ผ่านเฉพาะ snapshot metadata prerequisites; nativeAuthorized=false, C1/C5 blocked และ H1/H2 excluded. งานต่อคือต้องปิด source/binary/startup/observation/owner-account boundaries ก่อนเสนอ native phase แยก; ห้ามใช้ H0 เป็นเหตุปลด gates หรือรัน native ต่อ

## H0 authorized attempt — inconclusive, privilege guard stopped

Superseded by the corrected H0 replay below; retain this heading as historical
first-attempt evidence.

Owner สั่ง “ลุย h0” ตาม DECISIONS29. QA เตรียมคำสั่งในหน่วยความจำ, independent native GPT ตรวจ source, lead อ่านซ้ำ/parser0errors/UTF8hash ก่อน invoke หนึ่งครั้ง. [H0 evidence](validation/2026-10-06-windows-h0-preflight.md) เก็บ14-field result และ source hash: outcome=inconclusive, category=privilege, apiError=24. Tool exit0 หมายถึงคืน record สำเร็จ ไม่ใช่ metadata gate ผ่าน

Privilege guard ยังตรวจไม่สำเร็จ ก่อน known-folder/target attributes; config.toml/requirements.toml ทั้งคู่ not-checked. ไม่ทราบ exact failing native call จาก schema นี้ และไม่สรุปว่า process elevated. ไม่รันซ้ำ/เปลี่ยนสิทธิ์/โหลด module หรือ workaround. ไม่มี provider CLI/config contents/account/quota/SDK/build/install/settings/commit/push. App source คงเดิม; C1/C5/H1/H2 ยัง blocked/excluded

งานถัดไป: ตรวจ API contract ของ privilege guard แบบ read-only ก่อนเสนอคำสั่งแก้ที่ review/parser/hash แล้วขออนุมัติ H0 replay แยก. ไม่มี native launch หรือผล metadata ผ่านจาก attempt นี้

## Windows host plan — reviewed planning checkpoint 2026-10-05

Owner ไม่มี VM และสั่งเตรียมแผนบนเครื่องนี้เท่านั้น ยังไม่ทดลอง/ใช้บัญชีจริง ตาม DECISIONS28. [Host plan](plans/2026-10-05-windows-host-bounded-test.plan.md) ได้รับ actual Codex CLI review: ร่าง H0/H1 fix-then-proceed6Major/1Minor/1Info → ลด H0-only → delta2Major/2Minor → focused ยังติด artifact lifecycle → architect ตัด filesystem artifact ทั้งหมด → final180638 **PROCEED for PLAN**. [Review report](plans/2026-10-05-windows-host-bounded-test-review.md) เก็บผลจริง/dispositions/input snapshots รวม invalid174752 ที่ไม่ใช้เป็น verdict

Future H0 เป็นคำสั่ง PowerShell ในหน่วยความจำที่อ่าน source/AST และ hash ก่อน invoke ครั้งเดียว ตรวจ OS privilege/known folder/target metadata เฉพาะ presence/type. ไม่อ่าน config/auth contents ไม่สร้าง/ลบ code file หรือเขียน runtime report โดยคำสั่ง; compiler/host internal effects ไม่ได้รับการรับรองว่าไม่มี. มี14-field schema, soft10000ms ไม่ใช่ hard timeout, cleanup ทุก resource ก่อน emit record. Final actualCLI0.160/gpt-6.1-sol/OpenAI/medium exit0: D-M1 eliminated from scope, D-M2/D-m1/D-m2 closed IN PLAN ไม่มี material finding ค้าง. Leadอ่าน rawผลจริงและเอกสารซ้ำ

ณ planning checkpoint ยังรอ owner อนุมัติ; ต่อมาผู้ใช้สั่งดำเนิน H0 แล้ว ผล inconclusive อยู่ด้านบน.26 source/project/config/shell hashes คงเดิม. H1/H2 excluded/unresolved; C1/C5 blocked และ strict2s/500ms ของ C5 คงเดิม. การผ่าน H0 planning ไม่ใช่ผล Windows native/account integration ผ่าน

## C1/C5 read-only study — complete; runtime/production gates remain blocked

Owner อนุมัติทีมศึกษาแบบ read-only ตาม DECISIONS27. [C1 study](validation/2026-10-05-windows-c1-isolation-study.md) ขยาย pinned-source map ถึง PAT/agent/workload auth, remote environment bootstrap, proxy/PAC, TLS files, OTEL/analytics และ config layers; ตรวจ33additional source hashes. เป็น conditional source evidence ไม่ใช่ผล runtime และไม่อ้างครบทุก transitive path

[C5 study](validation/2026-10-05-windows-c5-bridge-study.md) ชี้ช่อง R:Git redirector→S:MSYS shell→later admission; inner native supervisor/warm broker ยังไม่ควบคุม upstream ก่อนสร้างลูก. Background output/EOF, keeper handles, cancellation endpoints และ actualClaude2.1.289 context ยังต้องพิสูจน์ ไม่เริ่มเขียน broker/supervisor จาก studyนี้

[Decision proposal](plans/2026-10-05-windows-c1-c5-decision-study.plan.md) แนะนำประเมิน disposable Win11x64 guest สำหรับ C1 และคงC5disabled. VM availability/virtualization product ยังไม่ทราบ; ส่งคำถาม optional ให้ownerแล้ว ยังไม่มีคำตอบ ไม่ถือ silence เป็นการเลือก/อนุมัติ lab. เป็นแผนตัดสินใจ ไม่ใช่implementation-readyหรือactualCodexCLIreview/source-dispatch

Independent native GPT skeptic ตรวจทั้งสองreportsและdecisionplanแล้ว ไม่มีactionablefindingค้าง. รับและแก้ finding: แม้expected unauthenticated errorก็ห้าม quota/account/auth RPC ในW2b; runtimewhitelistยังเป็นconfig/registryและtest-onlyMCPcanaryที่ผ่านprerequisites/อนุมัติแยก. Leadอ่านartifactซ้ำและตรวจ26source/project/config/shellhashesคงเดิม. ไม่มีbuild/test/native/account/setup/settings/sourcechangesหรือcommit/pushในรอบศึกษา; C6ผล84/0/0เดิมยังแยกไว้ด้านล่าง

งานถัดไป: owner เลือกคงoffline หรือระบุ/เลือกguestเพื่อเตรียมconcrete labsetup/observationplan; ไม่มีการติดตั้งหรือเปิดlabจนขอบเขตนั้นได้รับอนุมัติ. C1/C5ยังblocked เกณฑ์strict2s/500msเดิม; guestproofไม่รับรองauthenticatedhostusage

## Windows blocker amendment — C6 deterministic proof ผ่านแล้ว

รับช่วงจาก [handoff16:22](HANDOFF-2026-10-05-162248-windows-c6-plan-ready.md) แล้วตรวจ Git จริง main/ca2eb2b; owner อนุมัติแผนด้วย go และยืนยัน continuous sub-agent dispatch ตาม DECISIONS26

[Architect C6-only plan](plans/2026-10-05-windows-blocker-amendment.plan.md) พร้อมแล้ว:3sourcefiles internalperinstance seam + realWin32 deterministicrace, no newdependency/native/account/settings. [ActualCodexreview](plans/2026-10-05-windows-blocker-amendment-review.md) CLI0.160/gpt-6.1-sol: firstfix-then-proceed1major3minor2info→deltaclosed6และเพิ่ม1minor→focusedPROCEED; findings/dispositionsครบทุกข้อ

ทีม backend-dev(Core/csproj), qa-engineer(ClaudeTests) ทำ C6-only ครบแล้ว; independent native GPT reviewer ไม่พบ actionable finding และ lead อ่าน source/logs ซ้ำ ตรวจ scope เทียบ baseline. Release build exit0/0warnings/0errors, focused deterministic **4passed/0failed/0skipped** (194ms), full mandatory offline **84passed/0failed/0skipped** (11748ms). [C6 validation](validation/2026-10-05-windows-c6-validation.md) บันทึก real Win32 external replacement และ same-ID equal-length flushed edit: typed conflict/targetN/known recovery ID+bytes/security และปิด inspection handles ก่อน later public publication/reopen pathname เดิม. เป็น detection+retained evidence ไม่ใช่ CAS/rollback หรือ comprehensive durability proof

Optionalstrict-vs-baselineไม่ได้คำตอบ ใช้strict2s/500msเดิม; C1/C5ยังblockedและVM/toolchain/actualCLI-context optionsยังต้องกำหนด/อนุมัติ ไม่ติดตั้งอะไร ไม่มีquotaprobe/ผู้ใช้settingswrites/commit/push. งานถัดไปคือ owner เลือก C1 lab/isolation หรือ C5 architecture investigation; ไม่เริ่ม native/live/production bridge จากผล C6

## Windows edition — W1 offline ส่งมอบแล้ว / W2a มี blockers

Ownerยืนยันdispatchด้วยgoแล้วตามDECISIONS25 ทีมลงมือ C#/.NET10/WinForms แยกใต้windows/ คงMacเดิม [Windows build/run](../windows/README.md)

ผล W1 historical: Release build0warnings/0errors, mandatoryoffline **80passed/0failed/0skipped** (9391ms), final native own-window tray smokeexit0(menu/refresh/UIthread/icon re-add/quit) และsingle-instance boundaryexit3. C6 full suite ใหม่84/0/0 ดูด้านบน; ไม่รัน tray smoke ซ้ำสำหรับ delta นี้. Version0.1.0 developmentเท่านั้นไม่มีrelease ตัวเลขnormalTrayยังเป็นunavailable ไม่ใช่quotaจำลอง/บัญชีจริง. หลักฐานและledgerอยู่ใน [Windows validation](validation/windows-mvp-validation.md)

W2a tee experiment **34passed/9failed/43cases**,exit1: pipeline/substitutionยังunsupportedเพราะowneddescendantsเกิน2sและstartup/callerEOFเกิน500ms; pipelineรอproducerstdin. External-editor file experimentรอบล่าสุด **inconclusive/exit1** แยกจากmandatorysuite ไม่รับรองvalidation→replace raceหรือuniversalCAS. Productionlauncher/installerยังdeferred

NativeW2bยังblocked: [pinned source audit](validation/2026-10-05-windows-codex-audit.md) พบProgramData known-folder/systemlayerไม่redirectด้วยscratchenv. Joblimit1enforcementผ่านfakecontrols แต่counterไม่ตรวจทุกrejectedCreateProcessattempt; ห้ามนำไปอ้างno-attempt proof. ไม่มีnativeharness/accountprobe/settingswrites

[IndependentnativeGPTsource review](validation/2026-10-05-windows-independent-review.md) ปิดRPCsuccessfulfinish/trailingfaults,displacedbackupretentionและEOFframefindingsแล้ว; runtime debugปิดstderrdeadlock/cleanupbounds/state16KiBและtestfixture/harnessissues. ไม่ใช่ClaudeหรือCodexCLIcode review. ActualClaude planreviewsก่อนdispatchคงอยู่ใน [delta report](plans/2026-10-05-windows-mvp-claude-delta-review.md)

งานถัดไป: ออกแบบและreviewisolation/nativecontextหรือbridgecancellationamendmentตาม C1/C5 blockers; controlled C6 race proof ปิดแล้วตามรายงานด้านบน. W3livequota/compatibilityimport และW4productionbridge/settings/install ยังไม่อนุมัติ. ยังไม่commit/push; Windowsbinary/tag/releaseไม่มี. SDKfirstuseครั้งแรกเคยสร้างdevcertificateอัตโนมัติแจ้งผู้ใช้แล้ว ไม่trust/remove; คำสั่งถัดไปปิดcertgeneration/SDKtelemetry

WindowsNarrator/highcontrast/DPI/Explorerrestartจริง/sleepwake/resource60s/liveusageยังไม่ตรวจ ไม่อ้างMac126testsเป็นWindowsproof. Capacityprovenanceเดิมยังเป็นCodexserver_overloaded ไม่ใช่Claudequota

## ปิดชุด fix 2026-10-05

แก้ first-install/restore retry และ Codex error hint แล้ว โดยคง strict-config และ guards เดิม Final XCTest **126/126**, failed0/skipped0; Release build, synthetic Release CLI smoke2/2 และ independent-context Codex review ผ่าน ไม่มี material finding ค้าง ตรวจ UI บน Mac นี้: คืน bridge เดิม → ติดตั้ง build ใหม่ → ถอด → ติดตั้งกลับ; settings คืนตรง bytes เดิมทุกครั้ง และ helper/wrapper ตรง executable ใหม่ เปิดแอปกลับโหมด Menu Bar ปกติแล้ว

หลักฐานและข้อจำกัด: [fix closure validation](validation/2026-10-05-fix-closure.md) แทนงานค้างใน [handoff ก่อนปิดชุด](HANDOFF-2026-10-05-105307-desktop-fix-validation.md) แอป/bridge ใช้ `0.1.0` build6 ตามเดิม ไม่มี binary/tag/notarization; รอบนี้เผยแพร่ source fix ตามคำสั่งผู้ใช้

Restore มี checkpoint ส่วนตัวและลบ metadata ท้ายสุด ทำให้ retry หลัง settings/backup ถูกลบระหว่าง cleanup ได้ โดยยังตรวจ ownership/digest/สิทธิ์และปฏิเสธ conflict Preview ปฏิเสธ BOM/UTF-16 ที่ byte scanner ไม่รองรับก่อนเขียน

## เวอร์ชันและหลักฐานจาก publication เดิม

- Public source version `0.1.0` build **6**; `origin` = `https://github.com/kraiwin/AIUsageBar.git`, branch `main` ไม่มี tag/GitHub Release/binary release
- Final Release build ผ่าน; XCTest **113/113**, failed0/skipped0; independent review ปิด findings ครบและตรวจ delta สุดท้ายแล้ว
- Real Codex weekly/reset + actual Claude Code latest snapshot แสดงพร้อมกันแล้ว; Claude install→exact restore→reinstall ผ่าน; Cmd-Q ผ่าน
- Default menu-mode60.60s: CPU0.48%, peakRSS96.75MiB, zero owned children หลัง fetch
- หลักฐาน/คำสั่ง/ผลก่อนหน้าและข้อจำกัดทั้งหมด: [S1–S3 validation](validation/2026-10-03-s1-s3.md)

## Milestones

| ID | ผลลัพธ์ | สถานะและขอบเขต |
|---|---|---|
| M0 | ขอบเขต/ระบบบริหารงาน | เสร็จ |
| M1 | วิธีเชื่อม CLI และ guards | ลงมือแล้ว default direct Codex0.160 และ latest Claude snapshot; ไม่มี credential reader |
| M2 | Menu Bar ภาษาไทย | ใช้งานจริงแล้ว text-only ชื่อเต็ม, status/freshness/refresh/quit, optional live window |
| M3 | สอง providers | จริงทั้งสองค่ายบน Mac นี้; Claude เป็น ingest-time snapshot ไม่มี account binding |
| M4 | ความถูกต้อง/ความทนทาน | personal-use acceptance ผ่าน tests/review/live flows/resource; audibleVoiceOver, actualsleep/wake/offline/timezoneยังไม่ได้ตรวจ |
| M5 | source-build preparation | public source เผยแพร่แล้ว พร้อม README ภาษาไทย/license/credits และ pre-publication scope checks |

## วิธีใช้งานบน Mac นี้

Final validated bundle: `build/OctoberCloseRelease/Build/Products/Release/AIUsageBar.app` เปิดอยู่ในโหมด Menu Bar ปกติ เลือก native Codex path เดิมแล้ว Claude bridge ติดตั้งที่ `~/Library/Application Support/AIUsageBar/` โดย backup/metadata/private snapshot อยู่นอก Git

Build จาก source ด้วยคำสั่งใน README หรือ Xcode ไม่ต้อง Apple Developer Program; App Sandbox ปิด/Hardened Runtime เปิด/ad-hoc ไม่มี XPC/notarization/updater/telemetry

## งานต่อไปและข้อจำกัด

1. ใช้งานส่วนตัวและเก็บ feedback ได้ ไม่ต้องกลับไปทำ startup prerequisites เดิมถ้า CLI/config ไม่เปลี่ยน
2. Optional validation ที่ยังไม่ได้ตรวจ: audible VoiceOver, sleep/wake/offline/OS-clock/timezone จริง และ runtime macOS14/Intel ระบุไว้ตามจริง ไม่ใช่ gate ที่ผ่านแล้ว
3. Public source อยู่ที่ `kraiwin/AIUsageBar` แล้ว; source commit แรก `2f0fe19` อ่านกลับตรงกับ remote `main` ไม่มี tag/binary/GitHub Release การเปลี่ยนต่อไปใช้ local gate/review/secret scope ก่อน commit/push
4. Claude valid latest ไม่มี quota → no-data/—; malformed ไม่เป็น0; snapshot/sessionล่าสุดไม่รับรองบัญชีปัจจุบัน Codex CLI/configต่างอาจ failclosedตามguard; ไม่มี mcp-list fallback
5. Source implementation และ publication supersede baseline ก่อนหน้าใน handoff/plans เดิม ยึด DECISIONS ข้อ20–21 และ validation ล่าสุด

## บันทึกความคืบหน้า

| วันที่ | ผลลัพธ์ | การตรวจ | งานถัดไป |
|---|---|---|---|
| 2026-10-03 | มี Git scaffold, brief และ decisions; เพิ่มระบบบริหารงานและ Engineering Baseline v1 | ตรวจไฟล์ใน repo, local Git และแหล่งอ้างอิงมาตรฐาน; ยังไม่มี app tests | เริ่ม M1 |
| 2026-10-03 | ตรวจเครื่องมือ, clone/read CodexBar และสรุปช่องทางข้อมูลที่มีเอกสารรองรับ | environment validation + usage-sources; ไม่รัน reference และไม่อ่าน token/เรียก usage จริง | เคาะวิธีเชื่อม แล้วเริ่ม skeleton/provider tests |
| 2026-10-03 | จัดทำแผน MVP 0.1.0 พร้อมงาน T1–T6, dependencies และเกณฑ์รับงาน | QA review เอกสารไม่พบข้อขัดแย้งสำคัญ; ลิงก์/diff ผ่าน; ยังไม่มี source/build | เริ่ม T1–T2 โดยไม่แตะบัญชี |
| 2026-10-03 | สร้าง app/test targets, icon/UI ไทย, model/parser/freshness/state; แก้ findings และตรวจ bundle สิทธิ์ปกติ | 39 XCTest ผ่าน, build ผ่าน, independent review ไม่พบ blocking issue; UI smoke popup ยังรอ | ตรวจ popup แล้วเคาะวิธีเชื่อม T3–T4 |
| 2026-10-03 | เพิ่มข้อความสองค่ายและ native tooltip ในโหมดตัวอย่างแยกจากข้อมูลจริง | build ผ่าน, 39 regression tests ผ่าน, review lifecycle/labeling ไม่พบ blocking issue; preview เห็น Claude 58% / Codex 72% ผ่าน Computer Use | รับ feedback ตัวอย่างและเลือกวิธีเชื่อมจริง |
| 2026-10-03 | ย่อข้อความ Menu Bar โดยคงคำย้ำข้อมูลสมมติใน tooltip/preview; เสนอทางเลือกไอคอน | แก้เฉพาะข้อความและ build number; ยังไม่เปลี่ยนไอคอนหรือเชื่อมบริการ | เลือกไอคอน/รับ feedback ความกว้าง |
| 2026-10-03 | ผู้ใช้เลือกข้อความล้วน; ลบ icon ใน status button และเพิ่ม renderer/selection สำหรับ 1/2 ค่าย | 44 tests ผ่าน, Release build ผ่าน; UI ทดลองเลือก 0/1/2 ค่ายผ่าน Computer Use | รับ feedback ข้อความแล้วเคาะการเชื่อมจริง |
| 2026-10-03 | ผู้ใช้ตรวจว่าดูดีและให้ปิดรอบงาน UI/text example | README/build/tests/ข้อจำกัดบันทึกแล้ว; ยังไม่ถือว่า MVP เชื่อมสองค่ายเสร็จ | รอเปิดรอบเชื่อมจริงหลังอนุมัติวิธี |
| 2026-10-03 | อนุมัติ CLI lifecycle/Claude snapshot; เพิ่ม offline Codex RPC และข้อเสนอ Sandbox bridge | 61/61 tests, Release build 5 และ independent review ผ่านใน offline scope; ยังไม่เรียกบัญชีจริง | ข้อเสนอรอบนั้นถูกแทนด้วย one-app plan ในบันทึกถัดไป |
| 2026-10-03 | ศึกษาและร่าง one-app plan: UI Sandbox + bundled non-Sandbox XPC, Codex real refresh; ถอน manual collector สำหรับ MVP | อ่าน Apple/OpenAI/Anthropic docs และ reference source; ยังไม่ build/run spike หรือเปลี่ยน source/สิทธิ์; Claude review ยังไม่เกิด | ส่งแผนให้ Claude ตรวจ แล้วปรับก่อน implementation |
| 2026-10-03 | เปลี่ยนเป้าหมาย public source-build; ทดสอบ startup ก่อนแก้แผน แล้วตัด XPC/Sandbox spike/provenance/session splitting | CLI 0.160.0 guarded/canary quota result สำเร็จ, MCP positive control ทำงาน, config เดิมไม่เปลี่ยน; ไม่มี app source/build ใหม่ | ส่ง revised plan ให้ Claude review ก่อน S1 |
| 2026-10-03 | รับ Claude review; แก้เอกสาร B1–B4 และ Application Support bridge/user-level settings | Reviewer verified MCP 7 vs config 3 และ guard/Node/notification findings; ยังไม่แก้แอปหรืออ้าง discovery offline | ปิด discovery checks ก่อน S1; เคาะ C3 ก่อน S2 |
| 2026-10-03 | อ่านภาคผนวก Claude review และปรับ discovery หลักเป็น app-server inventory+quota สองรอบ | เอกสารเท่านั้น ไม่มี CLI/network/source changes; layer completeness/no-spawn proof ยังไม่ทำ | พิสูจน์สองเงื่อนไขก่อน S1; C3 เลือก ก แล้ว; ตรวจสองเงื่อนไขก่อน S1 |
| 2026-10-03 | ใช้ sub-agents พิสูจน์ prerequisites ก่อน S1; C3 เลือก กแล้ว | source audit + inventory 5/all-disabled/no-spawn/positive-control 2 และ independent review ผ่าน supported default scope; ไม่ขอ quotaหรือ build/app source ใหม่ | S1 ใน default context ตามข้อจำกัด/placeholder map ที่ตรวจแล้ว |
| 2026-10-03 | ผู้ใช้รับ CLI-only personal use; จัด execution plan 12 ลำดับและ full handoff สำหรับ session ใหม่ทำ S1–S3 ต่อเนื่อง | ตรวจ Git/build config และอ่าน xcresult เดิม 61/0/0 ไม่รัน tests/build ใหม่; ยังไม่ implementation/bridge/publication | รับช่วงจาก MEMORY แล้วเริ่ม S1 ตามแผน |

เพิ่มบันทึกเมื่อจบชุดงานที่มีผลลัพธ์ ไม่ต้องบันทึกทุกคำสั่งหรือซ้ำรายละเอียด changelog

| 2026-10-03 | ทำ S1–S3 ด้วย sub-agents ขนานจริง: Codex guarded two-child/native discovery, Claude native wrapper/installer/snapshot, real UI/coordinator และเตรียม public source | Final113/0/0, Release build6, independent reviewผ่าน; actualทั้งสองproviders/install-exactrestore-reinstall/CmdQ/CPU0.48%RSS96.75MiBผ่านในMacนี้ ข้อจำกัดในvalidation | ใช้งานส่วนตัว/feedback; publicationเมื่อdestination+คำสั่งชัดเจน |

| 2026-10-03 | Owner ยืนยัน kraiwin/AIUsageBar public และให้เผยแพร่ source0.1.0/build6 | Ship gate113/0/0; redacted unrelated private workflow docs; source snapshot84files; GitHubmain readbackตรง2f0fe19 ไม่มี binary/tag; scaffoldเดิมอยู่local-only branch | ใช้งาน/feedback; optional platform/accessibility checksตามข้อจำกัด |

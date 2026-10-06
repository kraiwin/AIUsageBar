# AIUsageBar — Windows plan corrections + global Claude reviewer handoff

บันทึก 2026-10-05 12:21:20 Asia/Bangkok · `C:/Python/AIUsage` · PowerShell

## สถานะที่ต้องรับช่วง

ผู้ใช้อนุมัติ C#/.NET10/WinForms, native Windows11 x64, tray icon/menuภาษาไทย และ **W1–W2 offline/isolated** ด้วย “go” แล้ว ตาม DECISIONSข้อ24 ไม่ต้องถามเลือก stack/scope เดิมซ้ำ แต่ยังไม่ได้ยืนยัน dispatch gate ที่สอง

จากนั้นผู้ใช้ขอ Claude review จริง และสั่ง **แก้แผนและรีวิวซ้ำ** งานแก้เริ่มแล้วแต่ review ซ้ำยังไม่ได้รัน มีการทำ global Claude reviewer แทรกตามคำสั่งผู้ใช้จนติดตั้งและตรวจเสร็จ รอบล่าสุดขอ handoff เพื่อเปิด session ใหม่ ไม่ใช่คำสั่งเริ่ม source/บัญชีจริง

**งานถัดไปคือทำ draft plan ให้สอดคล้องกัน แล้วรีวิวซ้ำผ่าน global claude-reviewer** ไม่ใช่ทำ global launcher ใหม่ หรือเริ่ม Windows implementation ทันที

ยังไม่มี `windows/`, Windows source/build/app tests, native quota/account probe หรือ bridge/user settings writes ไม่มี commit/push ในรอบ PC นี้ Mac source/Xcode คงเดิม

## อ่านก่อนเริ่ม

1. AGENTS.md, [PROJECT_BRIEF](PROJECT_BRIEF.md), [DECISIONS](DECISIONS.md) ข้อ22–24, [STATUS](STATUS.md), [DEVELOPMENT](DEVELOPMENT.md)
2. MEMORY.md และ handoff นี้ ซึ่งแทนงานถัดไปใน [handoff 11:37](HANDOFF-2026-10-05-113717-windows-plan-ready.md)
3. [Windows MVP plan](plans/2026-10-05-windows-mvp-plan.md), [actual Claude review + lead triage](plans/2026-10-05-windows-mvp-claude-review.md)
4. [PC evidence](validation/2026-10-05-windows-environment.md)
5. Global `%USERPROFILE%/.codex/agents/claude-reviewer.toml`, launcher/brief และ global validation ที่ระบุด้านล่าง

ตรวจ Git/state จริงก่อนใช้ snapshot ไม่ discard งานเอกสารที่ยังไม่ commit

## Windows plan: review จริงและ draft corrections

Review 11:52 เรียก native Claude CLI2.1.289 จริง โดยไม่มี --model/--effort จึงใช้ default; metadata `claude-opus-5-5`, exit0/success, ~230s ไม่มี tools/MCP/model-generated substitutes Verdict **fix-then-proceed**: C1 blocker, C2–C6 major, C7 minor

Raw result/input นอก repo: `C:/tmp/claude-reviews/result-20261005-115213.json`, `prompt-20261005-115213.txt` รายงานเต็มถูกเก็บใน repo ตามลิงก์ด้านบนแล้ว ไม่ต้องมี raw file จึงจะเข้าใจ findings

Draft แก้ main plan แล้ว:

| Finding | Draft change | สิ่งที่ยังต้องตรวจ/ปิดใน review ซ้ำ |
|---|---|---|
| C1 isolation | clean environment, scratch config, file-only backend candidate, startup source audit ก่อนรัน; proxyไม่อ้างเป็นnetwork sandbox | ยืนยัน exact CLI key/source/auth/known-folder paths ก่อน native W2; ถ้าพิสูจน์ไม่ได้ให้blockedเฉพาะnative probe |
| C2 positive controls | เพิ่มtest-only mcpServerStatus/list, แยกfake job-limit control/MCP control/hooks source coverage | แก้ broad impossibility claimของClaudeด้วยMacevidence; Windowsjob/accountingยังต้องทดลอง |
| C3 W2 vs W3 | W2ไม่ขอquota/auth; W3ต้องreassertguardsในauthcontextก่อนเชื่อquota | ไม่อ้างfixtureproofครอบenterprise/managed/liveทั้งหมด |
| C4 executable change | bindtuple/version/policy; developer/operatorรันW2ใหม่และreconfirm; unknownblocked | record lifecycle/importต้องgroundในmanifestและreview; ไม่เพิ่มwarn-and-allow |
| C5 bridge | tee-split capture-onlyเป็นW2experimentแทนhandshakeที่ยังไม่พิสูจน์ | backpressure/streams/exit/top-levelBashdeathเป็นblockinggates; productionbridgeยังdeferredถ้าไม่ผ่าน |
| C6 files | concreteReplaceFileW/tempclose/recoverybackup/race/ACLcontract; readerแยกsharing | verifiedAPIsemanticsไม่ใช่runtimeproof; fault/lock/reparse/raceยังต้องW2/W4 |
| C7 cleanup | boundedcleanup-pending/manifestretained/retryเมื่อapphostDLLล็อก | ยังไม่ทดสอบactualWindowsinstaller |

**Draft ยังไม่เก็บ disposition ให้สอดคล้องทั้งไฟล์:** ท้ายแผนยังเป็น pending table, Codex M3 historical row/file manifestยังพูดถึง native-wrapper readiness/fallback ขณะที่ main designเลือกtee experiment อย่าอ้าง findings closed จากการเห็น paragraphใหม่ ต้อง reconcile tasks/file responsibilities/acceptance/history/dispositionsก่อนส่ง review ซ้ำ พร้อมคง originalreviewเป็นประวัติ

อ่าน [Mac inventory evidence](validation/2026-10-03-codex-inventory-prerequisites.md) และ [startup evidence](validation/2026-10-03-codex-startup-side-effects.md) ประกอบ C2: เคยใช้ mcpServerStatus/list ทำ positive controlได้โดยไม่มีmodel/thread จึงไม่รับข้ออ้างว่าMCPcontrolต้องมีturnโดยอัตโนมัติ ไม่เอาผลMacแทนWindows

Microsoft docsถูกเปิดตรวจ Job basic limits/accounting, CreateFileW และ ReplaceFileW แล้วใน sessionนี้ แต่ยังไม่มีWindowsspike Sourcefetchจาก pinned rust-v0.160.0 path `codex-rs/core/src/auth/storage.rs` คืน404 จึง **ยังไม่มี pinnedWindowsbootstrap/authsourceauditที่ตรวจครบ** อย่าอ้างว่าsourceauditผ่าน หาตำแหน่ง/tagที่ถูกต้องจากofficialsourceก่อนnativeprobe ไม่ลดguardsเพื่อให้ดูผ่าน

## Global Claude reviewer — ทำเสร็จและตรวจแล้ว

สร้างนอก repo:

- `%USERPROFILE%/bin/claude-run.py`
- `%USERPROFILE%/bin/claude-review-brief.md`
- `%USERPROFILE%/.codex/agents/claude-reviewer.toml`

แก้ `/team` ที่ `%USERPROFILE%/.agents/skills/source-command-team/SKILL.md` และแก้เฉพาะข้อความที่ผิดใน `%USERPROFILE%/.codex/agents/codex-reviewer.toml` สำรองก่อนแก้เป็น `.bak-20261005` ทั้งสองไฟล์ ไม่แก้config.tomlหรืออ่าน/แก้ `~/.claude` โดยตรง; CLIยังเป็นเจ้าของauth/stateของตัวเอง

`/team` ตอนนี้แยก nativeGPTroles, Waluigi=GPTรีวิวGPTในบริบทแยก และ ClaudeCLIจริง cross-vendor review ไม่มีmodel/effortปลอมในnative roster

Launcher pin **opus/high**, อนุญาต xhigh ตามscopeเสี่ยงที่อนุมัติ, ห้ามmax/fallback ส่งUTF8stdin ปิดtools/MCP/customizations/sessionpersistence timeout20min ผลเก็บ `C:/tmp/claude-reviews/` ตรวจprocess/success/is_error/modelfamily/result/lastVERDICTก่อนคืน0 stdoutบรรทัดเดียวเป็นpath `exit.txt` ซึ่งเขียนท้ายสุดแม้ล้ม

ใช้ workflowเดิมทุกrepo ไม่ประกอบคำสั่งสด:

```powershell
python %USERPROFILE%/bin/claude-run.py --prompt-file <absolute-UTF8-input> --slug windows-plan-delta
```

Promptต้องมีcontextและsourceที่จำเป็นพร้อมpath/เลขบรรทัด เพราะ toolsปิด ไม่ส่งcredential/account/rawconfig unrelated content Launcherใส่standardbriefและnumberedinputให้เอง อ่านexit/resultตามpathก่อนสรุป findingsทุกข้อต้องtraceถึงClaudeจริง; failureรายงานแล้วหยุด ไม่ลดรุ่น/ไม่ให้GPTแต่งแทน Leadตรวจfactsก่อนรับ เพราะClaudeอาจผิด

ผลตรวจจริง:

- Smoke code `first(xs){return xs[1];}` ผ่าน launcher0/Claude0/is_errorfalse; model_used `claude-opus-5-5`, requestedopus/high, finalVERDICT fix-then-proceed
- Smoke exit: `C:/tmp/claude-reviews/20261005-121633-000231-global-smoke-38148-exit.txt`
- Invalidmodelจริง: launcher1/Claude1/is_errortrue/API404/modelunknown; exitยังเขียน และไม่fallback
- Failure exit: `C:/tmp/claude-reviews/20261005-121652-240319-invalid-model-24768-exit.txt`
- Behavioralforwardtest: native defaultGPTtestagentอ่านTOMLใหม่และfailureartifactsแล้วรายงานfailure ไม่มีfindings/verdictที่แต่ง ไม่มีretry/เปลี่ยนmodel นี่ทดสอบinstructions ไม่ใช่claimว่าregistryโหลดcustomroleในsessionเดิม
- Ruffผ่าน; pytest **22ผ่าน** ที่ `C:/tmp/claude-reviews/launcher-tests/`; tomllib/YAML/skillvalidatorผ่าน (validatorใช้ `python -X utf8` เพื่ออ่านไทย); house-rule suffixใน/teamเทียบbackupตรงเดิม
- Fullvalidation: `C:/tmp/claude-reviews/global-install-validation-20261005.md`

เปิดchatCodexใหม่เพื่อโหลดglobalskill/agent ถ้ายังไม่เห็นrole ห้ามแกล้งว่าโหลดแล้ว ให้mainอ่านTOMLแล้วเรียกlauncherตรง พร้อมบอกcallerตามจริง

## Capacity incident: corrected provenance

ผู้ใช้นำผลตรวจClaudeมาระบุว่าCodexlog12:01เป็น `codex_error_info: server_overloaded`, turnจบก่อนtoolcalls จึงเป็นฝั่งCodex/OpenAI ไม่ใช่Claudequota ผมเคยเสนอจะลองClaudeรุ่นอื่นก่อนตรวจที่มา ซึ่งไม่ถูกต้อง ยึดprovenanceที่แก้แล้ว ไม่สืบซ้ำโดยไม่มีเหตุผลหรือโทษClaudeจากตัวแดงCodex Review11:52สำเร็จจริง และglobalsmokeก็สำเร็จอีกครั้ง

## งานต่อและขอบเขตอนุญาต

1. รับช่วงdraft ตรวจความสอดคล้องและบันทึกC1–C7dispositionsตามจริงก่อนรีวิวซ้ำ งานเอกสารได้รับอนุมัติแล้ว ไม่ต้องถามทำต่อซ้ำ
2. ใช้actualclaude-reviewer/global launcherรีวิวdeltaพร้อมC2evidenceและข้อจำกัดsourceaudit เก็บreportในdocs/plans ไม่ยกsmoketestlauncherเป็นplanreview
3. ถ้ามีfindings แก้ในscopeแผนและตรวจซ้ำจนได้ผลที่ใช้ตัดสินใจได้ หรือsurfaceblockerจริง ไม่fabricateverdict ถ้าClaudefailให้รายงานexitrecordและหยุดreview-dependentwork
4. เมื่อแผน/reviewพร้อม เสนอbackend-dev/core-process-probes, worker/tray-build-icon, qa-engineer/testharness แล้วรอownerdispatchgateตามplan-gateก่อนsource
5. W3livequota/W4userbridge/settings/install, autostart/release/commit/pushยังไม่อนุมัติรอบนี้ แม้มีpublicMacpublicationauthorizationเดิม ห้ามอนุมานข้ามscope
6. Self-gateทุกartifact: reread/relevantvalidation/reference/requirements; เอกสารผลแยกglobaltooltests, futureWindowsproof และMac126testsเดิมชัดเจน

## Git snapshot

Branchmain tracksorigin/main; HEAD `ca2eb2b`; currentchangesยังไม่commit/push ไม่reset/cleanเพื่อย้อนsnapshotเก่า

Modified: `.gitignore`, `CHANGELOG.md`, `MEMORY.md`, `docs/DECISIONS.md`, `docs/STATUS.md`

Untrackedก่อนhandoffนี้: handoff11:37, WindowsMVPplan, actualClaudereview, Windowsenvironmentvalidation Handoffนี้เพิ่มuntrackedอีกไฟล์

Localonlyspec `docs/specs/spec-2026-10-05-112624-windows-system-tray-mvp.md` ถูกignoreและบันทึกagreedW1–W2แล้ว ไม่force-add Globaltoolingอยู่home/Ctmp ไม่เข้าGitapprepo

## Prompt สำหรับ session ใหม่

```text
อ่าน AGENTS.md, PROJECT_BRIEF, DECISIONS, STATUS, DEVELOPMENT, MEMORY.md และ
docs/HANDOFF-2026-10-05-122120-windows-claude-review.md แล้วตรวจGit/stateจริง
รับช่วง draft Windows plan และทำงานเอกสาร/reviewซ้ำที่อนุมัติไว้:
reconcile docs/plans/2026-10-05-windows-mvp-plan.md กับ findings C1–C7
ให้ tasks/manifest/acceptance/dispositions ตรงกับ design ที่แก้ และส่งให้
claude-reviewer จริงผ่าน %USERPROFILE%/bin/claude-run.py (opus/high)
รวม Mac MCP-status positive-control evidence และแยก unverifiedWindowsgates
ห้ามประกอบClaudeCLIสด/ลดmodelเงียบ/แต่งreviewแทน ใช้exit/resultจริง
แก้findingsในscopeแผนและreviewซ้ำก่อนเสนอdispatch ไม่ถามทำต่อซ้ำทีละขั้น
Ownerอนุมัติstack/native/tray/W1–W2แล้ว แต่dispatchgateยังรอเคาะ
ยังไม่เริ่มWindows source/livequota/bridgeinstall/commit/pushในคำสั่งนี้
จบแล้วบอกผลreviewและขั้นต่อไปที่ผมต้องตัดสินใจให้ชัด
```

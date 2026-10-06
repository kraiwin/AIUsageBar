# AIUsageBar — รับช่วง Windows C6 plan พร้อม review

2026-10-05 16:22:48 Asia/Bangkok · C:/Python/AIUsage · PowerShell

## จุดที่หยุดและงานถัดไป

ผู้ใช้ขอเขียน handoff เพื่อเปิด session ใหม่ หลังเสนอแผน C6 ที่ review ผ่าน
**คำขอ handoff ไม่ใช่การอนุมัติแผน C6 หรือ dispatch ใหม่**.

งานถัดไป: รับ owner approval ของ [แผน C6](plans/2026-10-05-windows-blocker-amendment.plan.md)
แล้วเสนอ backend-dev(Core/โครงการ) + qa-engineer(ClaudeTests) และรอ dispatch
gate ที่สองตาม plan-gate ก่อนแก้ source. ไม่ต้องเริ่ม grill/เลือก stack/W1 ใหม่.
หากข้อความใหม่อนุมัติหรือเปลี่ยน gate ให้ยึดคำสั่งผู้ใช้และบันทึกขอบเขตตามจริง.

แผนล่าสุดผ่าน **actual Codex CLI review: PROCEED for PLAN** แล้ว ทุก finding
มี disposition ครบ. ยังไม่ implement C6 seam และไม่มี build/test ใหม่หลัง W1.
C1/native isolation และ C5/production bridge ยัง blocked ไม่ได้ปิดพร้อม C6.

## อ่านก่อนทำงาน

1. AGENTS.md, PROJECT_BRIEF.md, DECISIONS.md, STATUS.md, DEVELOPMENT.md, MEMORY.md
2. Handoff นี้ ซึ่งแทนงานถัดไปของ handoff12:21 และ planning checkpoints เก่า
3. [C6 amendment plan](plans/2026-10-05-windows-blocker-amendment.plan.md)
4. [Actual Codex review และ verbatim results](plans/2026-10-05-windows-blocker-amendment-review.md)
5. [W1/W2a validation + debug ledger](validation/windows-mvp-validation.md)
6. [C1 pinned source audit](validation/2026-10-05-windows-codex-audit.md)
7. windows/AGENTS.md และ [Windows build/run](../windows/README.md)

ตรวจ Git/state จริงก่อนใช้ snapshot; รักษางานเดิม ห้าม reset/clean/discard.

## W1/W2a ที่ทำและตรวจแล้ว

- Windows11 Pro x64/build26200, SDK10.0.401/runtime10.0.12; C#/NET10/WinForms.
- Source แยก windows/; Version0.1.0 development, tray assembly0.1.0.0.
- Lead Release build exit0, **0warnings/0errors**, elapsed1.47s.
- Lead mandatory offline **80passed/0failed/0skipped**, elapsed9391ms;
  QA final self-gate80/0/0 elapsed8708ms. ข้อมูล/child RPC เป็น synthetic เท่านั้น.
- Final native own-window tray smoke exit0: menu/refresh/UI thread/icon re-add/
  quit. TaskbarCreated เป็นข้อความจำลองเข้า HWND ของเรา ไม่ restart Explorer.
- Single-instance testกับ own pre-held mutex exit3/no smoke startup.
- Normal tray แสดง unavailable/—; ไม่มี quota จำลองหรือ actual account data.
- No third-party NuGet: sources clear; assets ทั้ง4โครงการไม่มี package entries.
- Independent code review เป็น **native GPT แยก context**, ไม่ใช่ Claude หรือ
  Codex CLI. ปิด source findings เรื่อง RPC Finish/drain/exit/accounting,
  trailing frames/EOF, displaced backup comparison และ cleanup failure paths.
- Mac source/tests/Config/Xcode ไม่เปลี่ยน; ไม่รัน Mac tests บน PC.

หลักฐาน local ที่ถูก ignore:
`build/WindowsValidation/lead-build.txt`, `lead-offline.txt`, `lead-exits.json`,
`tray/tray-smoke.json`; `build/WindowsTraySecondInstance/second-instance.json`.
รายงาน repo ข้างต้นมีผล/ข้อจำกัดพอรับช่วงได้โดยไม่ต้องมี raw logs.

## C6 แผนที่รออนุมัติ

แก้ source เพียง3ไฟล์:

| Owner ที่เสนอ | File | งาน |
|---|---|---|
| backend-dev | windows/src/AIUsageBar.Core/PrivateFiles.cs | internal readonly per-instance callback หลังปิด old reader ก่อน actual ReplaceFileW; public path default no-op |
| backend-dev | windows/src/AIUsageBar.Core/AIUsageBar.Core.csproj | SDK InternalsVisibleTo สำหรับ AIUsageBar.Tests, ไม่เพิ่ม package |
| qa-engineer | windows/tests/AIUsageBar.Tests/ClaudeTests.cs | deterministic actual-Win32 race + controls/security/recovery survival |

Lead ดูแล integration/review/docs; ทุกคนรักษางานคนอื่นและขอบเขตไฟล์.

Test oracle ที่สำคัญ:

- แยก original A/editor E/proposed N และ identity/full bytes ที่รู้ล่วงหน้า.
- External edit ใช้ MoveFileExW จริง ไม่ใช้ AtomicWrite ซ้อน writer.lock.
- Same-ID edit ใช้ direct handle overwrite/flush, equal length, identityเดิม.
- ต้องได้ typed publish-conflict; controlled replacement ทิ้ง targetเป็น N.
  นี่คือ detection + retained evidence ไม่ใช่ CAS/rollback.
- Recovery ใหม่ต้องมี1pathnameจาก set-difference; initial ID/bytes ต้องตรง E
  หรือ preserved A ID + edited bytes ตามแต่ละกรณี.
- **ปิด inspection handles ทั้งหมด** ก่อน later public publication แล้วเปิด
  **pathnameเดิม** ตรวจ ID/full bytes/security ซ้ำ ห้ามถือ orphan handle เป็น proof.
- Public create/replace, internal counting no-op create0/replace1 และ instance
  isolation ต้องมี controls; ตรวจ semantic protected DACL/owner/user+SYSTEM,
  regular file/single-link/no reparse ทั้ง target/recovery และหลัง reopen.
- หลัง approval/dispatch: focused files/deterministic ต้องเลือก case ไม่ว่าง,
  build0warnings/errors + full mandatory suite ไม่มี skip; บันทึก countsจริง.

## Actual plan review provenance

Architect ทำ read-only grounded plan; lead เป็นผู้เซฟไฟล์.
Codex reviewer เรียก **CLI จริงผ่าน global wrapper**, self-contained stdin,
workdir C:/tmp; Codex0.160.0/model gpt-6.1-sol/providerOpenAI/reasoningmedium.
เป็น GPT รีวิว GPT ไม่ใช่ cross-vendor. No reviewer source edits/test/probe.

| รอบ | ผล | Evidence stamp |
|---|---|---|
| First | exit0; fix-then-proceed;1major/3minor/2info | 20261005-160331 |
| Delta | exit0; ปิด6ข้อเดิมใน PLAN; เหลือ1minor/1info | 20261005-161125 |
| Focused closure | exit0; **PROCEED**; no remaining material finding | 20261005-161541 |

Prompt/raw/summary อยู่ `C:/tmp/codex-reviews/` ตามชื่อ stamp; full results และ
paths อยู่ใน report repo. Findingsทุกข้อแก้แล้วตาม actual output ไม่แต่ง verdict.
Review นี้เป็น model-review workflow แยกจาก prohibited native quota/account
compatibility probe; ห้ามใช้มันเป็นข้ออ้างปลด native gate.

Spec local-only: `docs/specs/spec-2026-10-05-154712-windows-blocker-amendment.md`.
ไม่ force-add. Source baseline31files hashตรวจ unchanged ตอนปิดแผน อยู่ที่
`C:/tmp/aiusagebar-windows-blocker-source-baseline.json` (ตรวจ stateใหม่อีกครั้ง).

## Blockers และขอบเขตที่ยังไม่อนุญาต

- C1: pinned official source commit a956835d020762cb2b570053af06f643a11c0ecc
  มี real ProgramData known-folder/system config ก่อน RPC; scratch env ไม่ isolate.
  Auth storage pathที่ถูกคือ login/src/auth/storage.rs; auditยังไม่ครบ/ไม่มี
  passing real-executable record. NativeCompatibility.cs ยังไม่เขียน/รัน.
- C5: tee experiment **34pass/9fail/43**, exit1; pipeline/substitution unsupported.
  Top TerminateProcess baseline4/candidate6 survivors, MSYS TERM3/5หลัง2s;
  slow runtime caller EOFเพิ่มประมาณ3.1s และ pipelineค้างเมื่อstdinยังเปิด.
  นับก่อน harness cleanup. Native supervisor/warm brokerเป็น study ไม่ source-ready.
- C6 stressรอบสุดท้าย inconclusive/exit1; เคยเห็น editor recovery2ชุดในรอบก่อน
  แต่ยังไม่ใช่ deterministic pass. แผนใหม่แก้ช่อง proof นี้ ไม่ใช่ยืนยันผ่านแล้ว.
- Job limit1 ปฏิเสธ CreateProcess1816 แต่ total1/terminated0 อาจไม่เปลี่ยน;
  failed explicit associationเป็นคนละ control. ห้ามอ้าง countersตรวจทุก attempt.
- Optionalถาม strict2s/500ms หรือ baseline-equivalence ยังไม่มีคำตอบ;
  **คง strictเดิม** ไม่ตีความ silence ว่าอนุญาตลดเกณฑ์. ไม่ขวาง C6-only.
- WindowsSandbox.exe lookupไม่พบ; VC toolchain component queryไม่พบ.
  vswhereทั่วไปพบ SSMS ไม่ใช่ VC compiler. ไม่มีการ install/enableอะไร.
- W3 actual quota/import, W4 production bridge/settings/install, native CLI probe,
  OS/account/firewall/registry/toolchain changes, autostart/release/commit/push
  ยังไม่อนุมัติในรอบนี้. ไม่มี binary/tag/release.
- SDKfirst-useเคยติดตั้ง ASP.NET devcertificateอัตโนมัติใน buildแรก แจ้งผู้ใช้แล้ว;
  ไม่ trust/remove. ทุกdotnetคำสั่งต่อไปตั้งtelemetryoptout/certgenerationfalse.

คำสั่งbuild/testsจาก windows/ ดูREADME/แผน; อย่ารันซ้ำเพียงเพื่ออ้างผลใหม่ก่อน
source permission. Narrator/DPI/Explorerrestartจริง/sleepwake/resource60s/liveusage
ยังไม่ตรวจ. GlobalClaude launcherที่ทำก่อนหน้านี้ยังอยู่ ไม่สร้างใหม่; ถ้าต้อง
Claude reviewใช้ global claude-run.py opus/high ตาม handoff12:21.

## Git snapshot และ self-gate

Branchmain tracksorigin/main; HEAD ca2eb2b. งาน PC ทั้งชุดยัง uncommitted/unpushed.
Modified: .gitignore,CHANGELOG,MEMORY,README,DECISIONS,STATUS.
Untracked: windows/ และ PC handoff/plan/review/validation หลายไฟล์ รวม handoffนี้.
build/bin/obj/specsถูกignore; rawreviewอยู่นอกrepo. ห้าม commit credential/log/account dumps.

Handoffเป็นงานเอกสารเท่านั้น ไม่ใช่ approval/implementation. Lead reread brief,
ตรวจ Markdown links/tables/Git whitespace และ latest review/stateก่อนส่งมอบ.

## Prompt สำหรับ session ใหม่

```text
อ่าน AGENTS, PROJECT_BRIEF, DECISIONS, STATUS, DEVELOPMENT, MEMORY และ
docs/HANDOFF-2026-10-05-162248-windows-c6-plan-ready.md ตรวจ Git/stateจริง
รับช่วง C6-only amendment ที่ actual Codex CLI reviewให้ PROCEED for PLAN แล้ว
รอ ownerยืนยันแผนC6 จากนั้นเสนอ backend-dev(Core/csproj)+QA(ClaudeTests)
และรอ second dispatch gate ก่อนsource ตามplan-gate ไม่เริ่มW1/grill/stackใหม่
คงC1/C5strictgates ไม่nativequota/accounts/settings/install/dependency/commit/push
อ่านแผน+reviewพร้อมdispositions อย่าเปลี่ยนhistorical80testsเป็นผลรอบใหม่
คำขอhandoffล่าสุดไม่ใช่อนุมัติC6 ให้บอกจุดตัดสินใจที่เหลือสั้นๆ
```

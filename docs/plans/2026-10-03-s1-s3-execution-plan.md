# แผนลงมือ S1–S3 ต่อเนื่อง — AIUsageBar ใช้งานส่วนตัวผ่าน CLI

> สถานะหลังลงมือ: ดู [STATUS](../STATUS.md) และ [DECISIONS ข้อ 20](../DECISIONS.md) เอกสารนี้เก็บ design/baseline ก่อน implementation ไม่ใช่สถานะปัจจุบัน

2026-10-03 · สำหรับ session ใหม่ทำงานต่อเป็นชุดเดียวตาม prompt ท้ายไฟล์

## เป้าหมายและสิทธิ์

ทำแอป Menu Bar ที่ **เจ้าของใช้เองกับ Codex CLI และ Claude Code บน Mac นี้** ให้ใช้งานได้จริงครบสองค่าย พร้อมเอกสาร build จาก source และเตรียม source สำหรับ public repo ตามเป้าหมายเดิม
ยอมรับข้อจำกัดที่ต้องใช้ CLI แล้ว ไม่ต้องขยายไปสู่การ login ผ่าน browser, quota บนเว็บ, enterprise profiles หรือ API billing ก่อนปิด MVP

ผู้ใช้ต้องการให้ session ใหม่ทำงานต่อเนื่องครบทุกลำดับ โดยไม่ต้องสั่งทีละขั้นหรือถามซ้ำว่าจะทำต่อไหม แต่ยังต้องตรวจสอบและขอสิทธิ์ตามจริง ห้ามข้าม failure หรืออ้างว่าผ่านโดยไม่มีหลักฐาน
การสร้างแผนนี้ยังไม่ได้เริ่ม S1, ยังไม่สร้าง remote, push หรือติดตั้ง bridge งานเริ่มเมื่อผู้ใช้เปิด session ใหม่และส่ง prompt ด้านล่าง

ข้อสรุปหลักยึด DECISIONS ข้อ 14, 17–19 และ [integration design](2026-10-03-one-app-integration-plan.md):

- แอปเดียวแบบไม่ใช้ Sandbox พร้อม Hardened Runtime และ ad-hoc signing; ไม่ใช้ XPC, notarization, Apple Developer Program หรือ dependency ภายนอก
- Codex ใช้ native CLI path ที่ค้นพบแล้วให้ผู้ใช้ยืนยันหรือเลือกเอง; ไม่ใช้ Node, custom shim หรือระบบตรวจ provenance
- Codex ใช้ child process สองรอบสำหรับ inventory และ quota; ตรวจ registry/config ก่อนอ่าน quota และไม่ใช้ `mcp list` เป็น fallback อัตโนมัติ
- Claude ใช้ snapshot ล่าสุดโดยไม่แยก sessions; เมื่อ payload ล่าสุด valid แต่ไม่มี quota ให้เขียนสถานะ no-data ทับ (เจ้าของเลือกข้อ ก แล้ว) และ UI แสดง —
- Claude ใช้ native wrapper/helper ใน Application Support เพื่อคง statusline เดิม พร้อม installer ระดับผู้ใช้ที่มี preview, backup และ rollback
- UI แสดงชื่อเต็มเป็นข้อความบรรทัดเดียว; no-data แสดง —, ค่า 0 จริงแสดง 0%; ใช้ข้อมูลสมมติเฉพาะเมื่อเปิด `--demo`; ข้อมูล stale/snapshot ต้องมีป้ายกำกับที่ไม่ทำให้เข้าใจว่าเป็นยอดปัจจุบัน
- polling ทุก 5 นาทีและ throttle ไม่น้อยกว่า 1 นาที พร้อมจัดการ clock, generation และ cancellation; แอปไม่มี updater หรือ telemetry
- โค้ดของเราไม่อ่านหรือเก็บ token/cookies; อนุญาตให้ official CLI จัดการ auth/refresh ของตัวเอง ห้ามบันทึก raw account/config/header/stdin ลง log

## รับช่วงก่อนลงมือ

อ่าน AGENTS.md → PROJECT_BRIEF → DECISIONS → STATUS → MEMORY และ dated handoff ล่าสุด จากนั้นอ่านแผนนี้, integration design, review และ validation
ตรวจ Git และ native executable/toolchain อีกครั้งโดยไม่เปิดเผย secrets; ตรวจงานที่ค้างใน working tree และห้าม reset, clean หรือเขียนทับงานเดิม

Baseline จริง: build 5 / offline core+RPC 61 tests ผ่านในรอบก่อน; app target ยัง Sandbox และยังไม่มี production transport/bridge
Prerequisites ผ่านสำหรับ CLI0.160 default direct app-server context: [หลักฐาน](../validation/2026-10-03-codex-inventory-prerequisites.md)
ไม่ต้องทำ probe เดิมซ้ำเพียงเพราะ session เปลี่ยน รันใหม่เมื่อ CLI/config/โค้ด/ข้อกังวลเปลี่ยนจริง หรือหลักฐานใช้ไม่ได้

## ตารางงานตามลำดับ

| ลำดับ | ขั้น | ผลงาน | เกณฑ์ก่อนเดินต่อ | ผู้รับผิดชอบหลัก |
|---:|---|---|---|---|
| 1 | S1.1 | ปรับ target ให้ไม่ใช้ Sandbox คง Hardened Runtime/ad-hoc และแยก Codex module | Release build และ entitlements ตรงแผน ไม่มี dependency ใหม่ | Codex + Diddy Kong |
| 2 | S1.2 | ค้นหา native executable, ตั้งค่า และ UI ยืนยัน | npm/nvm resolve native executable จาก package ได้; ไม่ต้องใช้ Node/login shell; หาก path หายให้แจ้งเพื่อเลือกใหม่ | Codex + Diddy Kong |
| 3 | S1.3 | transport สอง child, inventory/registry guards และ quota result | guard assertions กับ placeholder map ถูกต้อง อ่าน weekly window 10080 นาทีและเวลารีเซ็ตจริงได้ ไม่มี model/thread/login RPC | Codex + Diddy Kong |
| 4 | S1.4 | synthetic tests ที่มีความหมายและ real account smoke | fragmentation/EOF/timeout/cancel/child cleanup/failure ผ่าน; เทียบ quota/reset บนเครื่อง; ปิด findings จาก independent review | Codex + Kamek/Waluigi |
| 5 | S2.1 | native Claude wrapper/helper และ atomic latest schema | stdin bytes/output/exit/cancel ของ command เดิมคงอยู่; เราไม่เพิ่ม dependency จาก jq/python | Codex + bounded worker |
| 6 | S2.2 | installer ระดับผู้ใช้: preview/backup/apply/restore | preserve padding/keys, compare-before-write, จัดการ conflict และใช้ Application Support path ที่เสถียร | Codex + bounded worker |
| 7 | S2.3 | latest snapshot, no-data policy ข้อ ก และ Claude smoke | quota ที่หายไปอย่างถูกต้องล้างค่าเดิมและแสดง —; ข้อมูล invalid ไม่กลายเป็น 0 และไม่เลื่อน freshness; concurrent writes/repair/rollback ผ่าน | Codex + Kamek/Waluigi |
| 8 | S3.1 | เชื่อม menu/status title/coordinator เข้ากับข้อมูลจริง | Codex refresh จริง, Claude อ่าน latest snapshot, แสดง freshness และแยก failure ของแต่ละ provider ได้ | Codex + Diddy Kong |
| 9 | S3.2 | combined Mac smoke และตรวจ resource | offline/wake/clock/reset/quit/keyboard/VoiceOver; ไม่มี process ค้าง; บันทึก CPU/memory/request count ตามเกณฑ์ที่กำหนดก่อนวัด | Codex + Kamek |
| 10 | S3.3 | README สำหรับ Xcode build/use/remove, LICENSE/credits/limitations | เจ้าของใช้งานเองได้; source build ไม่ต้องใช้ paid account; เปิดเผยโปรแกรม/ไฟล์/CLI side effects | Codex + Toadette |
| 11 | S3.4 | final review และเตรียม source | ไม่มี secrets/account/raw fixtures; docs ตรงกับโค้ด; build/tests/smoke ของ final bundle ผ่าน; Git scope พร้อม | Codex + Waluigi |
| 12 | เผยแพร่ source | GitHub public repo ตามปลายทางที่ยืนยัน | ทำหลังขั้น 11 และมีคำสั่ง/ปลายทาง publication ชัดเจน; ไม่เดา GitHub owner หรือ push ไป global backup repo | Codex; เจ้าของระบุ/ยืนยันปลายทางเมื่อพร้อม |

ขั้น 12 อาจรอข้อมูลหรือการยืนยันปลายทาง แต่ต้องเตรียม source/bundle/docs/review ให้ครบก่อน ไม่หยุดขั้น 1–11 เพียงเพราะยังไม่รู้ remote
ไม่มี public binary release/notarization/App Store หรือสมัคร Apple Developer Program ในแผนนี้

## S1 contract ที่ห้ามหลุด

1. ใช้ Modular API ที่แคบ: `fetchQuota`, `cancel` และ typed state; ไม่เปิด `runCommand/readFile/fetchURL/sendRPC` แบบทั่วไปให้ UI
2. child แรก: ใช้ static guards ที่กำหนดไว้, `initialize/initialized`, อ่าน `experimentalFeature/list` แบบแบ่งหน้าและยืนยันว่า active known guards เป็น false, อ่าน `config/read` ภายใน memory limit เพื่อดึง MCP names และ validated transport kind; ปิดและ reap child โดยยังไม่อ่าน quota
3. child ที่สอง: ใช้ guards และ disable map ซึ่งมี **disabled transport placeholder** (`stdio=/usr/bin/false` หรือ `HTTP=https://example.invalid/`) เพื่อ bootstrap project-only entries; ยืนยัน registry, `notify[]` และ MCP ทั้งหมด disabled ก่อน `account/rateLimits/read`
4. ห้ามคัดลอก command/env/header/args หรือ secrets จาก user config ลง process argv เพื่อสร้าง placeholder; หาก kind ไม่รู้จัก/ขัดกัน หรือ policy ไม่ตรง ให้รายงาน unsupported และหยุดโดยไม่ bypass
5. หลักฐาน source/runtime จำกัดเฉพาะ default context; named v2 `--profile` ไม่ถูก forward ใน app-server 0.160; `remote_control StageRemoved` ไม่ใช่ functional guard ห้าม assert ว่าเป็น false แล้วสรุปว่า remote control ถูกปิด
6. valid unknown no-id notification ให้ discard; malformed/disguised token request ให้ fail; ตรวจ response id/server requests อย่าง strict; ปรับ tests ของ core ปัจจุบันตาม contract ใหม่
7. จำกัดขนาด response ของ `config/read`/registry แยกจาก quota; ทำ typed extraction ใน memory และห้าม log raw JSON หรือ `error.localizedDescription` ที่อาจเปิดเผย config/accounts
8. จัดการครั้งละ one request พร้อม generation, timeout/cancel/exit/reap; ต้อง drain stdout และ stderr; ห้ามใช้ `killall` หรือหยุด session CLI เดิม
9. ใช้ default CLI account จริงของเจ้าของ; ทดสอบด้วย native candidate เดิมที่ผู้ใช้ยืนยันและใช้ใน probe; path อาจเปลี่ยนเมื่อ nvm อัปเดต จึงต้องตรวจใหม่
10. การรับ successful RPC ไม่ทำให้ cache เป็น fresh โดยอัตโนมัติ ต้อง verify semantics ของ fetch method ก่อนตั้ง `providerObservedAt`; quota window metadata ต้อง strict และห้ามแปลงข้อมูลที่หายไปเป็น 0

synthetic tests ไม่ต้องใช้บัญชี; account smoke ใช้เฉพาะ official CLI quota path ที่อนุมัติแล้ว เทียบตัวเลขบนเครื่องและห้ามใส่ลง Git/tool logs
หาก CLI guard assertion ไม่ตรง ให้หยุด provider และรายงานสิ่งที่ต้องแก้ ห้ามใช้ `mcp list` HTTP/keyring fallback โดยไม่ได้รับอนุมัติจากเจ้าของ

## S2 contract

- ใช้ Application Support directory สำหรับ native helper/wrapper/owned metadata/private snapshot/backup เพื่อไม่ให้ย้ายตาม app bundle
- native Swift wrapper ส่ง original stdin bytes ให้ original command ก่อน และคง stdout/stderr/exit/cancel; ห้ามสร้าง raw temporary files; หาก ingest ล้มเหลว statusline เดิมต้องทำงานต่อได้
- แก้เฉพาะ user-level `statusLine.command`; preview/backup/compare original bytes ก่อน write, preserve padding และ other keys; ห้ามแก้ project/local/managed overrides
- quota-only schema แยก valid no-data tombstone จาก invalid snapshot; newest received snapshot ไม่ใช่ account binding; label “จาก Claude Code ล่าสุด เวลา X” หมายถึง ingest time
- เมื่อ latest snapshot ไม่มี quota ให้เขียน no-data ทับและซ่อนตัวเลขเก่าทุกจุด; ไม่แบ่ง sessions/raw session ID/pseudonyms
- ตรวจ atomic private file, modes, size, owner, symlink และ conflicts; clear/restore เฉพาะไฟล์ที่เราสร้าง ห้ามลบทั้ง Application Support folder โดยไม่ตรวจงานผู้ใช้
- ติดตั้ง global config ด้วย exact reviewable patch พร้อม backup/restore หลัง synthetic tests; ขอ filesystem escalation ผ่าน tools เมื่อถึงขั้นนั้น ไม่ขออนุมัติแนวทาง CLI ใหม่ซ้ำ
- การลบหรือย้าย app ต้องไม่ทำให้ statusline เดิมพัง; explicit disconnect ต้อง restore original; ทดสอบ fallback เมื่อ helper หาย และห้ามรับรองว่า wrapper ยังรันได้เมื่อไฟล์ wrapper ทั้งชุดถูกลบ

## ทำงานต่อเนื่องและขนานอย่างไร

Codex หลักรับผิดชอบ requirements/integration/Git/docs/verification ตาม Mario global instructions และอ่าน `~/.codex/MARIO_TEAM.md`
ใช้ sub-agents จริง และไม่รายงานว่าทำงานขนานหากยังไม่ได้มอบหมายงาน; ใช้ไม่เกิน 3 children และไม่มี recursive delegation

- ระบุ ownership ก่อน concurrent writes เช่น Diddy ดูแล Providers/Services ใหม่, bounded worker ดูแล ClaudeBridge/Installer หลัง contract นิ่ง, Codex หลักดูแล `project.pbxproj`/Config/STATUS/DECISIONS
- QA/reviewer อ่านและตรวจคนละ scope โดยไม่แก้ไฟล์ที่ worker กำลังแก้; หากต้องแก้ tests ให้กำหนดเจ้าของให้ชัด
- serialize การแก้ shared models, UI และ project files; ใช้ agent เดิมต่อเมื่องานยังอยู่ใน scope เดิม ไม่จำเป็นต้องใช้ครบทุก role
- งาน auth/data integrity/cross-module ที่มีผลสำคัญต้องมี independent review และปิด findings ก่อนจบ; Codex หลักตรวจ actual diff สำคัญ และไม่นับคำยืนยันของ agent เป็นผล test
- รายงานความคืบหน้าเป็นภาษาไทยแบบผู้บริหาร: เริ่มจากผล/สถานะ บอกว่าทำอะไรเพื่ออะไร สิ่งที่ได้/ยังไม่รู้ ข้อแนะนำ และใครทำต่อ; อัปเดตประมาณทุก 60 วินาที ไม่ปล่อยให้เจ้าของต้องเดาว่าต้องทำอะไร
- build/test ตามความเสี่ยงหลังแต่ละชุดงาน; รันซ้ำเมื่อมี changes/failures/new concern เท่านั้น; อัปเดต STATUS/CHANGELOG ตามจริง; ใช้ Config เป็นแหล่งเดียวของ version/build number

## จุดที่อาจหยุดจริง และวิธีจัดการ

| เหตุ | ผู้รับผิดชอบ/การตอบสนอง |
|---|---|
| CLI/schema/config ต่างจน guards ไม่ผ่าน | Codex ตรวจหาสาเหตุก่อน; ไม่ลด guard เพื่อให้เขียว; ทำงานอิสระส่วนอื่นต่อได้ แต่ห้ามอ้างว่า provider ใช้งานได้แล้ว |
| login/Keychain permission หรือข้อมูลจริงไม่มา | Codex แจ้งข้อผิดพลาดโดยไม่เปิดเผย secret; เจ้าของอาจต้อง login ผ่าน CLI หรืออนุญาตระบบ; เราไม่คัดลอก token หรือแก้ credential |
| filesystem/tool approval | เตรียม exact change/backup ให้ตรวจได้ก่อน แล้วขอ escalation ผ่าน tool; เวลาที่ผ่านไปไม่ใช่ approval และห้ามเขียนนอก roots เพื่อเลี่ยง sandbox |
| source/docs/review/test fail | Codex แก้และ retest ตามความเสี่ยงเอง ไม่ให้เจ้าของแก้ routine code หรือเลือกวิธีทดสอบแทน |
| GitHub owner/repo/credentials ของ gh ยังขาด | ทำขั้น 1–11 ให้พร้อม แล้วถามข้อมูลปลายทางที่ขาดครั้งเดียว; ห้ามเดาหรือ push ไปผิด repo |
| session ถูกปิดหรือ context ต้อง handoff | อัปเดต STATUS/dated handoff/MEMORY ตามหลักฐาน; ไม่มีงานทำต่อเบื้องหลังหลังปิด session เว้นแต่ runtime รองรับและมีการตั้งงานนั้นไว้ |

สิทธิ์ที่คงอยู่: ไม่แตะ browser cookies/credential values, ไม่รัน reference scripts/build, ไม่เพิ่ม dependency หรือ analytics; Computer Use ต้องใช้ Mac เครื่องเดิม ห้ามใช้ PC/Atlas

## เกณฑ์จบงานรอบใหญ่

เจ้าของเปิดแอปเดียวบน Mac นี้ได้ เห็น Codex weekly จริงและ Claude latest snapshot ตามข้อจำกัดที่ยอมรับ; state/refresh/reset ถูกต้อง; Quit/Disconnect ทำงาน; ไม่เหลือ process ของเราค้างตามผล smoke; README ครบทั้ง build/use/remove; tests และ final review ผ่าน; source พร้อมโดยไม่มี secret และระบุสิ่งที่ยังไม่ได้ตรวจตามจริง

ห้ามอ้างว่า MVP เสร็จก่อนทั้งสองค่ายใช้งานจริงตาม scope; ห้ามอ้างว่าเผยแพร่แล้ว หากยังไม่มี remote/write outcome และการอ่านผลกลับมายืนยัน

## Prompt สำหรับ session ใหม่

> อ่าน AGENTS.md, MEMORY.md และ handoff ล่าสุด จากนั้นอ่านแผนนี้กับ DECISIONS ล่าสุด ตรวจ Git/state ใหม่ แล้วทำ S1–S3 ตามลำดับจนแอปพร้อมใช้ส่วนตัวบน Mac นี้ผ่าน Codex CLI/Claude Code ใช้ sub-agents ทำงานขนานจริงเมื่อแบ่ง scope ได้ โดยไม่ถามว่าจะทำต่อไหมทุกขั้น ดำเนิน implementation/tests/real CLI smoke/review/docs ตามแผน; ผู้ใช้เลือกข้อ ก สำหรับ no-data แล้ว และ prerequisites ของ default CLI 0.160 ผ่านตามหลักฐาน หากพบ blocker ให้แก้เองก่อน หรือถามเฉพาะข้อมูล/สิทธิ์ที่จำเป็น เตรียม public source ให้ครบ แต่ยังไม่ publish/push จนปลายทางและคำสั่งเผยแพร่ชัดเจน อัปเดต STATUS/CHANGELOG/handoff ตามผลจริง ห้ามพิมพ์หรือเก็บ credential/account payload และห้ามอ้าง build/test/production ที่ไม่มีผลตรวจ

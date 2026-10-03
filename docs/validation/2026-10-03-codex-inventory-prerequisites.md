# Codex inventory prerequisites — ผลตรวจสองเงื่อนไขก่อน S1

2026-10-03 · CLI 0.160.0, macOS 27.0.1 arm64 · ไม่ขอ quota/thread/model

## ข้อสรุป

**ผ่านใน default direct app-server context ที่ทดสอบ**: inventory เห็น user/project/CLI MCP ที่คาดไว้; registry/config RPC ไม่เปิด MCP canary; รอบสอง disabled ทุกตัวจาก inventory
ไม่รับรอง named v2 profiles, enterprise/cloud/MDM policy ที่ไม่ได้ runtime-test, ทุก CLI version, macOS14 หรือ Intel

| กรณี final | Registry | Inventory | Canary events | Config ผู้ใช้ |
|---|---|---|---|---|
| child แรก inventory | 4 pages; hooks/plugins/code_mode_host registered+false | 5 ตัว = user 3 + project fixture 1 + CLI fixture 1; fixture เปิดอยู่ | 0 | hash ไม่เปลี่ยน |
| child สอง guarded | active guard registry false; notify ว่าง | 5 ตัวเดิม ทุกตัว enabled=false | 0 | hash ไม่เปลี่ยน |
| positive control ขอ MCP status | guards เหมือน child แรก | 5 ตัว; เปิดเฉพาะ fixtures | 2 ครั้ง (MCP จำลอง) | hash ไม่เปลี่ยน |

ทุกกรณี final exit code 0 ไม่มี output-limit/non-JSON faults layer source tags ที่เห็น: sessionFlags, project, user, system
Registry `remote_control` เป็น stage=removed/default=false และ runtime ignore key นี้; ไม่ใช่ functional guard หรือหลักฐานว่า remote-control service ถูกปิด

## วิธีตรวจ runtime

- รัน native binary เดิมด้วย static feature/notify/plugin/telemetry overrides และ --strict-config
- สร้าง fixture `.codex/config.toml` ใน `build/InventoryProbe/project/` และ trust override เฉพาะ process ไม่แก้ global trust/user config
- ปิด MCP จริง 3 ตัวใน child แต่ปล่อย fixture stdio สองตัว enabled เพื่อสังเกต no-spawn path
- initialize/initialized → experimentalFeature/list pagination → config/read พร้อม cwd เดียวกัน/includeLayers=true → รอ 8 วินาที → close/wait
- รอบสองเพิ่ม disabled map แล้ว registry/config assertions แบบเดิม; ไม่เรียก account/rateLimits/read ในชุดนี้
- Positive control mcpServerStatus/list เรียก fixture ทั้งสองได้จริง จึงพิสูจน์ detector ไม่ใช่เพียงดู marker ที่ไม่มีวันเกิด
- อ่าน raw config/RPC/stderr ใน bounded memory แล้วทิ้ง; result มีเพียง flags/counts/fixture presence/source tags/event labels ไม่เก็บ transport/env/header/account/token
- Artifacts: `build/InventoryProbe/probe.py`, `results.jsonl`, project/canary fixtures (gitignored); Python standard library เฉพาะงานตรวจ ไม่เป็น dependency แอป
- รัน CLI ต้อง escalation เพราะ tool sandbox บล็อก state/home access; ไม่มี HOME/CODEX_HOME redirection หรือ auth copy CLI อาจใช้/เขียน state/log/auth ของตัวเองตามปกติ

## ปัญหาที่พบและแก้ใน harness

1. tool sandbox บล็อก startup (operation not permitted) ไม่ใช้รอบนั้นเป็นหลักฐาน no-spawn
2. มี excluded setup attempts ที่ child exit 1 รวมรอบที่เคยใส่ unknown guard fixture; ไม่เก็บ precise error/argument fingerprint พอวินิจฉัย cause จึงไม่อ้างว่า strict-config reject unknown เป็นผลพิสูจน์ Final pair ไม่ inject fake feature: unknown_guard fields=false ไม่ใช่ negative-control proof; unknown-feature echo/recognition แยกเป็น source audit และรายการทดสอบที่ต้องทำใน S1
3. child สองแบบ enabled=false อย่างเดียวล้มเหลวสำหรับ project-only MCP เพราะ bootstrap ยังไม่เห็น transport ของ layer นั้น
4. เพิ่ม **disabled transport placeholders** เฉพาะ CLI override ให้ typed bootstrap parse ได้: stdio command=/usr/bin/false, HTTP URL=https://example.invalid/ พร้อม enabled=false โดยเลือก kind จาก validated inventory ไม่คัดลอก env/headers/args secrets ลง CLI arguments ไม่เปลี่ยน user settings แล้ว final pair ผ่าน

placeholder ถือว่าเปลี่ยน transport เฉพาะ child ที่ปิดไว้ ไม่ใช่ transport ที่ผู้ใช้ตั้งจริง; production ต้องรองรับเพียง recognized transport kinds, reject unknown/conflicting transport และยืนยัน disabled ทุกตัวก่อน quota
การทดสอบนี้ยังไม่ใช้ MCP name collision ที่ชน placeholder หรือนโยบาย managed บังคับ transport; หาก assertions ไม่ผ่านให้ unsupported config ไม่พยายามสั่งงาน server

## หลักฐาน source ของ layer completeness

ตรวจ official tag rust-v0.160.0 แบบอ่านอย่างเดียว:

- [config_manager_service.rs](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/app-server/src/config_manager_service.rs): read(cwd) ใช้ effective enabled layers แล้ว apply exact requirements ก่อน serialize config
- [config manager](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/app-server/src/config_manager.rs), [loader](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/config/src/loader/mod.rs), [state](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/config/src/loader/state.rs): รวม system/cloud/user/project/runtime/managed ตาม state ที่ใช้ และไม่รวม disabled/untrusted project layers
- [CLI main](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/cli/src/main.rs): direct app-server ใช้ LoaderOverrides::default และไม่ forward --profile-v2 entrypoint จึงไม่ทดสอบหรืออ้างว่า named v2 profile ถูกเลือก
- [MCP manager](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/core/src/mcp.rs), [plugin manager](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/core-plugins/src/manager.rs): plugin-derived MCP แยกจาก config/read; plugins=false ทำให้ plugin resolver คืน empty จึงต้อง registry-verify flag จริง
- [feature catalog](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/app-server/src/request_processors/catalog_processor.rs) คืน names/runtime enabled จาก registry; [features](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/features/src/lib.rs) ระบุ remote_control=Removed/ignored

runtime fixture ยืนยัน user/project/session overrides และเห็น system metadata; ไม่ได้สร้าง synthetic enterprise/cloud/MDM config จึง source evidence แยกจาก runtime coverage ชัดเจน

## ผลต่อ S1

ทำ S1 ใน default app-server context นี้ได้โดยใช้ guard assertions, cwd/overrides เดิมทั้งสองรอบ และ disabled placeholder map ที่ทดสอบแล้ว หาก CLI/config/policy ต่างจน invariants ไม่ตรงให้หยุดก่อน quota
ไม่ย้ายไป mcp list fallback, ไม่รองรับ v2 profile โดยเดา และไม่อ้าง OS/CLI versions ที่ไม่ได้ตรวจ
ยังไม่แก้ app target/Swift, ไม่มี build/test แอปใหม่หรือ bridge install; 61 XCTest/build 5 เป็นผลรอบก่อน ไม่ใช่ผลของ integration

## Independent review

Waluigi ตรวจ harness และ sanitized results แบบ read-only ไม่รัน CLI/build; final ไม่พบ blocker เพิ่มใน supported-default scope หลังระบุ transport placeholders, removed remote_control และขอบเขต unknown-feature evidence ตามจริง

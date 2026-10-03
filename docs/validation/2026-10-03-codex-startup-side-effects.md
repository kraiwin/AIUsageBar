# Codex app-server startup — MCP/hooks/notify probe

วันที่ 2026-10-03 · ตรวจเป็นงานแรกก่อนแก้แผน ตามคำขอผู้ใช้
CLI ที่รัน: native binary ของ official installation `codex-cli 0.160.0` บน macOS 27.0.1 arm64

## ข้อสรุปและขอบเขต

ใน controlled quota-only path **ไม่พบการเรียก MCP canary, hooks canary หรือ notify canary**; initialize และ account/rateLimits/read ได้ result จริงจาก CLI
รอบปิด integrations ด้วย CLI overrides ยังคงอ่าน rate limits ได้; config.toml เดิมไม่เปลี่ยน (เทียบ SHA-256 ก่อน/หลังใน memory)

นี่ไม่ใช่การรัน integrations เดิมของผู้ใช้: config เครื่องนี้มี MCP 3 รายการและ notify พร้อม hook trust state; ทั้ง 3 MCP เดิมถูก disable เฉพาะ child และใช้ MCP canary แทน เพื่อไม่เปิดบริการจริง/คำสั่งที่มีผลข้างเคียง hooks/notify ของ probe เป็นคำสั่งที่บันทึกเฉพาะชื่อเหตุการณ์ ไม่เก็บ payload
Claude review เพิ่มหลักฐานว่า config ไฟล์เดียวมี 3 MCP แต่ CLI effective list มี 7 (อีก 4 จาก plugins); features.plugins=false ทำให้เหลือ 3 ตาม config ดังนั้น guard นี้เป็นส่วนสำคัญของผล probe ไม่ใช่พิสูจน์จากการปิด 3 รายการเพียงอย่างเดียว ผล list เป็นการตรวจของ Claude แยกจาก quota probe นี้ ดู [review B1/D2](../plans/2026-10-03-one-app-integration-plan-review.md)

plugins, remote control, Code Mode host และ telemetry ถูกปิดใน child ทุกกรณี จึงไม่ครอบคลุมการเริ่ม app-server ด้วย config เดิมทุกอย่างแบบไม่มี guard

| กรณี final | Effective hooks / notify / MCP canary | ผล RPC | Canary events | Config เดิม |
|---|---|---|---|---|
| quota-only, canary integrations เปิด | true / argv length 4 / enabled=true | initialize=result; rateLimits=result | ไม่มี | ไม่เปลี่ยน |
| positive control: mcpServerStatus/list | true / argv length 4 / enabled=true | initialize=result; status=list result | mcp 1 ครั้ง | ไม่เปลี่ยน |
| guarded quota-only | false / argv length 0 / enabled=false | initialize=result; rateLimits=result | ไม่มี | ไม่เปลี่ยน |

รอบ final child exit code 0 และไม่มี output-limit/non-JSON faults ไม่มี thread/start หรือ turn/start/model request
Positive control พิสูจน์ MCP detector; ไม่ได้ทดสอบ hook/notify ด้วย model turn หรือเปิด thread เพื่อกระตุ้น event ความเชื่อถือของ synthetic hooks ไม่ใช่หลักฐานว่า hooks ทุกชนิดพร้อม execute จึงต้องใช้ source event-dispatch ประกอบ และไม่กล่าวว่า absent marker เพียงอย่างเดียวรับรอง hook safety ทุก config

## วิธีทดสอบและข้อมูลที่ไม่เก็บ

- Probe และ canary ที่เขียนขึ้นใน `build/StartupProbe/` (gitignored) ใช้ Python standard library; ไม่เป็น production dependency หรือ source integration ของแอป
- ส่ง initialize → initialized → account/rateLimits/read ผ่าน stdio; รอ 8 วินาทีเพื่อสังเกต background startup แล้วปิด stdin/wait; ไม่ start daemon/listener
- Test-only config/read ตรวจเฉพาะ effective hooks flag, notify length และ MCP canary enabled; result ทั้งก้อนอยู่ใน memory ไม่พิมพ์/บันทึก
- บันทึกเพียง result/error class, canary labels, notification method names, child exit และ config unchanged; **ไม่เก็บตัวเลข quota, account ID/email, raw RPC/stdin/stderr, token หรือ auth cache**
- Source fetch กับ CLI runtime ต้อง escalation เพราะ network และ CLI ใช้ state/auth home นอก workspace; ไม่แก้ HOME/CODEX_HOME หรือ copy credentials
- CLI อาจเขียน logs/state หรือ refresh credential ของตัวเองตามพฤติกรรมปกติ; ตรวจ config unchanged ไม่ใช่รับรองว่า `.codex` ทั้งโฟลเดอร์ไม่มี mutation
- Final quota probes เว้น request อย่างน้อย 60 วินาทีผ่าน guard ใน harness; positive control ไม่เรียก provider quota

ข้อมูลที่เห็นเพิ่ม: notification `account/updated` และ `remoteControl/status/changed` แม้ปิด remote control จึงต้องทำ fixture/allowlist regression ก่อนต่อ transport จริง core ปัจจุบันยังไม่รองรับ remoteControl notification นี้ ผล probe ไม่ใช่ผลทดสอบ CodexRPCSession production integration

## กลไก guard ที่มีหลักฐานจริง

จาก source official tag `rust-v0.160.0` และ CLI --help:

1. `-c features.hooks=false` ปิด lifecycle hooks; canonical key คือ hooks
2. `-c 'notify=[]'` ปิด legacy notification command โดย array override แทนค่าฐาน
3. `-c 'mcp_servers={ "name"={enabled=false}, ... }'` merge enabled=false สำหรับทุกชื่อที่ค้นพบ โดยรักษา config ดิสก์เดิม หรือใช้ dotted override เมื่อชื่อไม่มี dot/quote ambiguities
4. `mcp_servers={}` **ไม่ล้างรายการเดิม** เพราะ merge table แบบ recursive
5. `features.plugins=false`, `features.remote_control=false`, `features.code_mode_host=false` และ `analytics.enabled=false`/otel exporters none ใช้ในการทดสอบ; production ต้องตรวจ effective config/version/layering ที่รองรับก่อนอ้าง guard ครบ

Dotted override implementation split ที่จุดโดยตรง ไม่ parse quoted TOML key: การใช้ `mcp_servers."name".enabled=false` สร้างชื่อที่รวม quote และทำ config ผิดได้ ใช้ inline map ที่มี explicit keys ในกรณีนี้ ไม่ copy syntax โดยเดา

แหล่งหลัก:

- [CLI overrides implementation](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/config/src/overrides.rs), [recursive merge](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/config/src/merge.rs)
- [MCP enabled field](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/config/src/mcp_types.rs), [feature keys](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/features/src/lib.rs)
- [legacy notify: AfterAgent event/spawn](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/hooks/src/legacy_notify.rs), [session hooks config](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/core/src/session/mod.rs)
- [app-server bootstrap](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/app-server/src/lib.rs), [quota vs thread dispatch](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/app-server/src/message_processor.rs)
- [Official hooks docs](https://learn.chatgpt.com/docs/hooks), [Config reference](https://learn.chatgpt.com/docs/config-file/config-reference)

ดาวน์โหลด source เข้า build/StartupProbe/source แบบอ่านอย่างเดียว ไม่ build/execute reference; hash prefixes: overrides dbac7a6f68de31e7, merge d119f72f5bb56b29, features 124c76d1fbad7b05, legacy notify e710e4c7d4806bbe, message processor f1a1bb0029e7c2f4

## รอบที่ใช้ไม่ได้และข้อจำกัด

- ก่อน final มี child-exit เพราะ format override ที่ไม่ตรง parser และ sandbox restriction; ไม่ใช้เป็นหลักฐานว่า integration ไม่ทำงาน
- รอบ quota แรกหลังแก้ override ได้ rateLimits result แต่ test-only config/read ใหญ่กว่า buffer; fault ทำให้ยืนยัน effective config ไม่ครบ จึงแก้เฉพาะ probe limit และรัน final ใหม่
- ไม่ทดสอบทุก CLI version, managed/system/cloud/project config, plugin injection หรือ OS; production discovery/guard ต้องทดสอบแยก ไม่ถือว่า MCP 3 ตัวใน config นี้แทนทุก configuration
- ไม่ได้เรียกคำสั่ง MCP/hooks/notify ของผู้ใช้จริงหรือออก model turn; ยังไม่ได้ติดตั้ง transport/bridge ของแอป และไม่ได้แก้ entitlements

## เกณฑ์ก่อน implementation/account integration

ใช้ผลนี้เป็น baseline สำหรับ source/guard fixtures และ transport tests ไม่ใช้เป็นใบรับรองไม่มี side effects
ต้องค้นหารายการ MCP/effective layers ด้วยวิธีที่ตรวจแล้ว, disable ครบก่อน startup, เก็บ original auth context, ไม่แก้ user config, และหาก config/version เปลี่ยนหรือ guard ไม่ครบให้แจ้ง unsupported configuration แทนการเปิด integrations โดยอัตโนมัติ

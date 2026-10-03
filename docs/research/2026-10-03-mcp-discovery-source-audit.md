# MCP discovery / guard recognition — source audit หลัง Claude review

2026-10-03 · อ่าน source ของ official Codex tag `rust-v0.160.0` แบบ read-only
ไม่มีการรัน mcp list/app-server/quota ในรอบ audit นี้ ไม่มี credential/global settings/source edits
หลักฐาน runtime 4 รอบที่ Claude รันอยู่ใน [review](../plans/2026-10-03-one-app-integration-plan-review.md) แยกจาก source audit นี้

## 1. mcp list ไม่ใช่ local-only inventory

[cli/src/mcp_cmd.rs](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/cli/src/mcp_cmd.rs): McpCli::run โหลด cloud config แล้ว run_list โหลด MCP manager/AuthManager, configured/effective servers และ compute_auth_statuses ก่อน serialize output

[core/src/mcp.rs](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/core/src/mcp.rs): config.to_mcp_config(plugins_manager) และ runtime_config อธิบายว่ารายการ effective ไม่ใช่แค่ user TOML file
[core-plugins/src/manager.rs](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/core-plugins/src/manager.rs): plugin manager คืนรายการว่างเมื่อ plugins_enabled=false สอดคล้องกับผล Claude ว่า 7 เหลือ 3

[codex-mcp/src/mcp/auth.rs](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/codex-mcp/src/mcp/auth.rs):

- stdio auth branch คืน Unsupported ไม่ spawn command จากการตรวจ auth นี้
- enabled streamable HTTP branch เรียก determine_streamable_http_auth_status พร้อม credential-store/keyring backend และ HTTP client อาจทำ OAuth discovery ไป configured endpoint
- http_headers_helper มี branch ที่เลี่ยง execute helper แต่ยังตรวจ credential store ไม่ใช่รับรองว่าไม่มี keyring/network

นอกจากนี้ cloud-config/plugin loading อาจมีงาน remote ตาม context; การไม่ spawn stdio MCP ไม่ได้เท่ากับ process นี้ไม่ต่อ network ทั้งหมด
source-level finding นี้พิสูจน์ว่าคำกล่าว local-only/no-credential-access ไม่ถูกต้อง ไม่ใช่ผล network trace ของเครื่องนี้ และยังไม่ได้ทำ mcp-list canary รอบใหม่

JSON stdout ของ run_list มี env/transport/header details ตรง ๆ ข้อมูลอาจมีค่าลับ ต้อง decode ใน bounded memory เลือก name/enabled ไม่บันทึก raw stdout/fixtures/log tool output การที่ Claude เลือกพิมพ์แค่ชื่อ/enabled เป็นวิธีที่ถูกกับข้อจำกัดนี้

## 2. config/read ไม่พิสูจน์ recognized feature จาก echo อย่างเดียว

[app-server/src/config_manager_service.rs](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/app-server/src/config_manager_service.rs) โหลด effective layers แล้ว deserialize เป็น ConfigToml/serialize ส่งกลับ protocol config
[features/src/lib.rs](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/features/src/lib.rs) มี FeaturesToml ที่ flatten map ของ bool entries; arbitrary feature names ยังอยู่ใน map ได้ ส่วน runtime Features::apply_map จึง lookup recognized key และ ignore unknown

ดังนั้น features.fake=false สามารถอยู่ใน config/read แม้ runtime ไม่รู้จัก ห้ามใช้เพียง key present/value=false เป็นใบรับรองว่า features.hooks=false ยังเป็น guard จริงหลัง key rename

ยังควร assert config/read สำหรับ notify/MCP/types/bounds แต่ feature guards ใช้ recognition/runtime-enabled proof เพิ่มจาก experimentalFeature/list

[request_processors/catalog_processor.rs](https://github.com/openai/codex/blob/rust-v0.160.0/codex-rs/app-server/src/request_processors/catalog_processor.rs) iterate known FEATURES registry แล้วคืน enabled จาก config.features.enabled(spec.id) จึงตรวจ canonical hooks/plugins/code_mode_host ที่ present และ false ได้ key ปลอมจะไม่อยู่ใน registry response ไม่ต้องทำ version allowlist เป็นวิธีหลัก ต้องตรวจ stage/no-op ของ entry และ test API/pagination ก่อน quota จริง

## 3. ผลต่อแผนก่อน S1

รับ B1–B4 ในระดับเอกสารโดยไม่รื้อ architecture; แผน discovery list×2/config-read ยังเป็น design ที่รอปิดสองช่องนี้:

1. ListArgs ของ v0.160.0 มีเพียง json bool ไม่พบ skip-auth/offline option; mcp-list discovery ต้องตรวจ network/keyring/cloud/plugin path และ canary แล้วเลือกขอบเขตหรือวิธี inventory อื่นที่ CLI resolve ให้ หรือให้ owner รับทราบ/เลือกขอบเขตการอ่านสถานะ HTTP MCP ของ CLI ก่อนใช้จริง ไม่อ้าง offline discovery และไม่แอบเปลี่ยนไปเขียน TOML parser
2. guard assertion เพิ่ม experimentalFeature/list ที่ใช้ registry/runtime state ร่วม config/read; unknown/missing/renamed guard ให้ fail closed ก่อน quota ต้องมี tests สำหรับ registry absent/true/unknown keys

ยังไม่ประกาศ S1 ready จากผล negative app-server quota probe เดิม เพราะ probe นั้นไม่ได้เรียก mcp list และไม่ได้ทดสอบ key rename/runtime recognition

## 4. ข้อเสนอปัจจุบันหลัง Claude ตรวจ source ซ้ำ

[ภาคผนวก review](../plans/2026-10-03-one-app-integration-plan-review.md) ยืนยัน mcp list ไม่ local-only และ config/read ไม่เป็น feature-recognition proof แล้ว เสนอ app-server สอง child แทน list discovery: inventory จาก guarded config/read รอบแรก แล้วรอบสอง disable map + registry/config assertions ก่อน quota

แผนล่าสุดใช้วิธีนี้เป็นข้อเสนอทางหลัก แต่ต้องพิสูจน์ config/read ครบทุก layer และ no-spawn canary ของ registry/config RPC ใน child แรกก่อนใช้จริง ยังไม่มี runtime test ใหม่ในรอบนี้ mcp list เหลือ fallback ที่ต้องให้ owner รับขอบเขต network/keyring หากวิธีหลักไม่ผ่าน ไม่ถือว่า fallback อนุมัติแล้ว

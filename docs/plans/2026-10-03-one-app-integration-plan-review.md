# รีวิวแผน one-app integration (ฉบับ source-build)

รีวิว 2026-10-03 โดย Claude · แผนที่รีวิว: [2026-10-03-one-app-integration-plan.md](2026-10-03-one-app-integration-plan.md)
อ่านประกอบ: DECISIONS ข้อ 14, STATUS, [startup probe](../validation/2026-10-03-codex-startup-side-effects.md), `AIUsageBar/Providers/CodexRPCSession.swift`, `Config/App.xcconfig`

## สรุป

**ทิศทางแผนถูกแล้ว และเริ่ม S1 ได้หลังแก้ 4 ข้อ (B1–B4)** ทั้ง 4 ข้อแก้แค่ในแผน ไม่ต้องย้อนไปเปลี่ยน architecture
ข้อที่ช่วยได้มากที่สุดคือ B1: ไม่ต้องเขียนตัวอ่าน TOML หรือไล่ config layers เอง ใช้ `codex mcp list --json` ของ CLI แทนได้ แผนจะเล็กลงด้วย

ไม่ได้ build/test หรือแก้ source/settings ใดๆ ในรอบนี้

### สิ่งที่รันระหว่างรีวิว (เปิดเผยตามจริง)

คำสั่งที่รันเป็นแบบอ่านอย่างเดียว ไม่เรียก quota และไม่ส่ง RPC:

- `codex --version` จาก 2 path, ดู `codex mcp list --help` / `codex app-server --help`
- `codex mcp list --json` 4 รอบ (ปกติ / `features.plugins=false` / ใส่ disable map / feature key ปลอม) **พิมพ์ออกมาแค่ชื่อ server กับค่า enabled** ไม่พิมพ์ส่วน transport/env
- อ่าน `/usr/local/bin/codex` (shim) และ `bin/codex.js` ของ npm package รวมถึงนับหัวข้อ `[mcp_servers.*]` ใน `~/.codex/config.toml` แบบดูชื่ออย่างเดียว

⚠️ ยังไม่ได้อ่าน source ว่า `codex mcp list` ต่อ network หรือ keyring ตอนเติมช่อง `auth_status` หรือเปล่า [เดา: น่าจะแค่ดูข้อมูลในเครื่อง] ต้องตรวจก่อนนำไปใช้จริง (ดู B1)

## Findings เรียงตามความรุนแรง

### 🔴 Blocker ก่อน S1

**B1 — รายชื่อ MCP ที่ต้องปิดไม่ได้มาจาก config.toml ไฟล์เดียว** (แผน §4 ข้อ 4–5, probe "MCP 3 รายการ")

- หลักฐาน [verified]: ใน `config.toml` มี MCP 3 ตัว แต่ `codex mcp list --json` ให้ผล **7 ตัว เปิดอยู่ 4 ตัว** (อีก 4 ตัวมาจาก plugins) พอใส่ `-c features.plugins=false` จะเหลือ 3 ตัวตาม config.toml
- ผลกระทบ: ถ้าทำตามแผน §4 ข้อ 4 (เขียนตัวอ่าน TOML + layering เอง) ต้องเลียนแบบการโหลด plugin, profile, project และ managed config ของ Codex ทั้งหมด ซึ่งใหญ่และพังทุกครั้งที่ CLI เปลี่ยน และ probe ไม่ได้บอกว่า `features.plugins=false` เป็นตัวที่ทำให้ไม่มี MCP เพิ่มอีก 4 ตัว
- แก้แบบเล็กที่สุด: ให้ CLI หาค่า effective ให้เอง
  1. รัน `codex mcp list --json` + static guards ชุดเดียวกับที่จะใช้ตอน start (hooks/notify/plugins/remote_control/code_mode_host/analytics/otel)
  2. เอาชื่อทุกตัวที่ `enabled=true` ไปสร้าง inline map `mcp_servers={ "ชื่อ"={enabled=false}, ... }` ซึ่ง escape ชื่อเป็น TOML basic string
  3. รัน `mcp list --json` ซ้ำด้วย argument ชุดเต็ม แล้ว**ยืนยันว่าทุกตัวเป็น `enabled=false`** ไม่ผ่านให้แจ้งว่า "config นี้ยังไม่รองรับ" (ทดสอบในเครื่องนี้แล้วว่าวิธีนี้ใช้ได้ [verified])
  4. แล้วจึง start `app-server` ด้วย argument ชุดเดียวกัน
- ต้องตรวจก่อนนำไปใช้: (ก) อ่าน source `mcp list` ที่ tag เดียวกันว่าไม่ spawn server/ต่อ network (ข) ใช้ MCP canary แบบ probe เดิมรันยืนยัน 1 รอบ (ค) output มีช่อง `transport` ซึ่งอาจมี URL/env/header ให้ decode ใน memory เอาแค่ `name`/`enabled` ห้าม log หรือเก็บเป็น fixture ดิบ
- ได้อะไร: ตัด TOML parser และ fixture layering ออกจาก S1 ทั้งก้อน ตรงกับเป้าหมาย "ไม่เพิ่ม dependency/ไม่กลายเป็น framework ใหญ่" ในแผน §4

**B2 — guard ที่ CLI ไม่รู้จักจะถูกข้ามเงียบๆ** (แผน §2 รายการ guards)

- หลักฐาน [verified]: `codex mcp list -c features.nonexistent_xyz=false` ได้ exit 0 และไม่มี warning
- เหตุการณ์ที่จะเจอ: CLI รุ่นหน้าเปลี่ยนชื่อ `features.hooks` (ในแผนก็จดว่า "canonical key คือ hooks" แปลว่าเคยมีชื่อ alias) → guard หายแต่แอปยังทำงานปกติ และผู้ใช้ไม่รู้
- แก้แบบเล็ก: หลัง `initialize` และก่อนส่ง `account/rateLimits/read` ให้เรียก `config/read` (probe ทำแล้วในโหมดทดสอบ) แล้วยืนยันค่า effective ว่า hooks=false, notify ยาว 0 และ MCP ทุกตัว disabled ไม่ตรงให้ปิด child แล้วแจ้งว่า "เวอร์ชัน/config นี้ยังไม่รองรับ"
- ข้อควรระวัง: probe เคยเจอว่า `config/read` ใหญ่เกิน buffer ต้องตั้งเพดานแยกสำหรับ response นี้ และ payload มีค่าลับของ config ได้ ให้ decode ใน memory เอาแค่ 3 ค่านี้
- ทางเลือกที่ไม่แนะนำ: ทำ allowlist ของเวอร์ชันที่รองรับ เพราะ Codex ออกรุ่นใหม่บ่อย ผู้ใช้จะเจอ "ไม่รองรับ" ทุกอาทิตย์ ใช้การ assert ค่า effective แทนดีกว่า แต่บันทึกเวอร์ชันที่ทดสอบแล้วไว้ใน README

**B3 — core ปัจจุบันจะล้มทั้ง fetch เมื่อเจอ notification ที่ไม่รู้จัก** (แผน §4 Protocol bullet 2)

- หลักฐาน [verified]: `CodexRPCSession.swift:156-160` ยอมรับแค่ 3 method ที่เหลือโยน `invalidEnvelope` ส่วน probe เจอ `remoteControl/status/changed` อยู่แล้ว
- แผนเสนอให้เพิ่ม method นี้ลง allowlist แต่ CLI รุ่นหน้าเพิ่ม notification ใหม่เมื่อไหร่ แอปจะขึ้น "โหลดไม่สำเร็จ" ทุกรอบ
- แก้แบบเล็ก: ข้อความที่**ไม่มี id** (เป็น notification) ให้ข้ามไปหมด ยกเว้นตัวที่ปลอมเป็น token refresh ที่เช็คอยู่แล้ว ส่วน response และ server request (มี id) ให้เข้มเหมือนเดิม และเพิ่ม test "notification ชื่อแปลก → ข้าม และยังได้ quota"
- notification ไม่ต้องตอบกลับ การข้ามจึงไม่ทำให้ protocol ค้าง

**B4 — Codex ที่ติดตั้งผ่าน npm/nvm จะรันจากแอป GUI ไม่ได้** (แผน §4 Path ข้อ 1 และ 3)

- หลักฐาน [verified] จากเครื่องนี้:
  - `~/.nvm/.../bin/codex` เป็น symlink ไปที่ `codex.js` ซึ่งขึ้นต้น `#!/usr/bin/env node`
  - แอปที่เปิดจาก Finder ได้ PATH พื้นฐานที่ไม่มี nvm (แผน §4 ข้อ 1 ก็ห้ามโหลด login shell) จึงรันไม่ขึ้นเพราะหา `node` ไม่เจอ
  - `/usr/local/bin/codex` เป็น bash shim ที่ owner เขียนเองและ hardcode path ของ nvm v24.21.0 ถ้าแอปเจอตัวนี้ก่อน จะใช้งานได้จนกว่าจะอัป node
  - ตัว native จริงอยู่ที่ `@openai/codex/node_modules/@openai/codex-darwin-arm64/vendor/aarch64-apple-darwin/bin/codex` และ probe เองก็ใช้ native binary
- แก้แบบเล็ก: ตอนค้นหา ถ้า path ไปจบที่ `codex.js` ของ npm package ให้แปลงเป็น native binary ใน `vendor/<triple>/bin/codex` ของ package นั้น แล้วแสดงให้ผู้ใช้ยืนยันเหมือน candidate อื่น **ไม่รองรับการรันผ่าน Node** จะได้ตัดเรื่อง "environment ของ Node" ใน §4 ข้อ 3 ทิ้งไปทั้งข้อ
- เรื่อง env: launcher ตั้งแค่ `CODEX_MANAGED_BY_NPM=1` กับ `CODEX_MANAGED_PACKAGE_ROOT` [verified] ซึ่งน่าจะใช้แค่กับข้อความแจ้งอัปเดต [เดา] ใส่ 2 ตัวนี้ตามไปก็พอ
- การค้นหาอัตโนมัติไม่เปิด login shell จึงหา nvm ไม่เจอเอง ให้เพิ่ม `~/.nvm/versions/node/*/lib/node_modules/@openai/codex` และ global prefix ของ Homebrew/npm ลงรายการที่จะไล่ดู
- ถ้า nvm เปลี่ยนเวอร์ชันแล้ว path หาย แผนเดิม "แจ้งตรงๆ + ให้เลือกใหม่" ครอบเคสนี้แล้ว

### 🟠 ก่อน S2 (Claude bridge)

**C1 — แผนยังไม่ได้บอกว่า bridge ทำงานด้วยอะไร และวางไว้ที่ไหน** (แผน §5)

- statusline ของ Claude Code เป็น shell command ซึ่งต้อง parse JSON โดยไม่มี jq/python (ห้ามเพิ่ม dependency) และต้องไม่เก็บ stdin ดิบ
- ถ้า wrapper เรียก helper ที่อยู่ใน `AIUsageBar.app` พอผู้ใช้ย้ายแอปหรือลากลงถังขยะ statusline เดิมจะพังทันที นี่คือต้นเหตุของปัญหา "missing-app fallback" ใน §5
- แก้แบบเล็กที่แนะนำ: ตอนติดตั้ง ให้ก๊อป helper (Swift CLI ของเราเอง sign แบบ ad-hoc) และ wrapper ไปไว้ที่ `~/Library/Application Support/AIUsageBar/` ซึ่งเป็น path ที่ไม่ขยับ wrapper อ่าน stdin เข้า buffer → **ส่งให้ statusline เดิมก่อนเสมอ** → ส่งต่อให้ helper เขียน snapshot (atomic rename) → คืน stdout/exit code ของคำสั่งเดิม ถ้า helper หายหรือพัง ให้ข้ามไปเงียบๆ
- ผลคือแอปแค่อ่านไฟล์ snapshot แล้วการย้าย/ลบแอปไม่ทำให้ statusline พัง และ uninstall = กู้คำสั่งเดิม + ลบโฟลเดอร์นี้
- ต้องวัดเวลาเพิ่ม: statusline ถูกเรียกบ่อย helper ต้องเร็ว (เป้าหมาย < 50ms [เดา]) และต้องไม่ทำให้ output ของคำสั่งเดิมช้า

**C2 — statusline อาจถูก override ระดับ project** (แผน §5 bullet 1)

- `statusLine` ตั้งได้ทั้ง user settings, project `.claude/settings.json`, local และ managed settings ถ้าติดตั้งที่ระดับ user แต่ project ไหนตั้งของตัวเองไว้ session ใน project นั้นจะไม่ส่ง snapshot มา
- แก้: preview ของ installer ระบุชัดว่าติดตั้งที่ user-level อย่างเดียว และเขียนใน README ว่า "session ที่ project ตั้ง statusline เองจะไม่อัปเดตตัวเลข" ไม่ต้องไปแก้ project settings ของผู้ใช้
- ตอนเขียน settings ให้แก้เฉพาะ `statusLine.command` ส่วน field อื่นใน object นั้น (เช่น `padding`) และ key อื่นทั้งไฟล์ต้องคงไว้ ทำ compare-before-write กับ bytes ที่อ่านตอน preview

**C3 — ต้องตัดสินใจ: ถ้าข้อมูลรอบล่าสุดไม่มี rate_limits จะทำยังไง** (แผน §5 bullet 3–5) ← **owner ต้องเคาะ**

- เคสจริง: ผู้ใช้สลับไปใช้ Claude Code ด้วย API key หรือบัญชีที่ไม่มี rate_limits ใน JSON ตัวเลข 42% ของบัญชีเดิมยังค้างอยู่ในไฟล์ และป้ายยังขึ้น "จาก Claude Code ล่าสุด เวลา 10:35"
- ทางเลือก ก (แนะนำ): เขียน snapshot แบบ "ไม่มีข้อมูล quota" ทับไปเลย UI ขึ้น `—` ตรงกับความหมาย "ล่าสุด" ที่ตกลงไว้
- ทางเลือก ข: ไม่เขียนทับ เก็บตัวเลขเดิมไว้ ตัวเลขดูต่อเนื่องกว่า แต่เสี่ยงโชว์ตัวเลขของบัญชีอื่น ขัดกับกฎ "ห้ามแสดงตัวเลขเก่าเหมือนเป็นค่าปัจจุบัน"

### 🟡 ก่อน account smoke / ก่อน public source

**D1 — README และตารางไฟล์/โปรแกรมต้องเพิ่ม** (แผน §6 README acceptance)

- แอปรัน `codex` 3 ครั้งต่อรอบ (mcp list ×2 + app-server) ทุก 5 นาที = ประมาณ 860 process ต่อวัน ควรเขียนบอกตรงๆ
- MCP ที่มาจาก plugins ถูกปิดด้วย ไม่ใช่แค่ใน config.toml
- โฟลเดอร์ `~/Library/Application Support/AIUsageBar/` ที่มี wrapper/helper/snapshot (ถ้ารับ C1)

**D2 — เอกสาร probe ควรระบุว่า `features.plugins=false` เป็นตัวหลัก** (validation probe "MCP 3 รายการ")

- หลักฐานจาก B1: ถ้าไม่มี guard นี้ effective MCP เป็น 7 ตัว ควรเติม 1 บรรทัดใน validation doc เพื่อไม่ให้คนอ่านเข้าใจว่ามีแค่ 3 ตัว

**D3 — DECISIONS ข้อ 13 ยังเขียนว่าแผนนี้เป็นฉบับ XPC**

- ข้อ 13 bullet 3 ลิงก์ไปที่ไฟล์เดียวกันแต่อธิบายว่า "UI คง Sandbox + XPC service" ซึ่งข้อ 14 แทนแล้ว ควรเติมว่า "(ถูกแทนด้วยข้อ 14)" จะได้ไม่สับสน

**D4 — การ sign แบบ ad-hoc สำหรับ source build** [verified จาก `Config/App.xcconfig`]

- ตั้งไว้ถูกแล้ว: `CODE_SIGN_STYLE=Manual`, `CODE_SIGN_IDENTITY=-` และไม่มี `DEVELOPMENT_TEAM` คนที่ clone ไปจึง build ได้โดยไม่ต้องมี Apple account
- ตอน S1 ต้องแก้: `ENABLE_APP_SANDBOX=YES` และไฟล์ entitlements ปัจจุบันยังเป็น Sandbox
- Hardened Runtime ไม่ต้องใช้ entitlement เพิ่มเพื่อ spawn child process [เดา จากความรู้ทั่วไปเรื่อง HR ควรยืนยันด้วย build จริงใน S1]
- แอปที่ build เองในเครื่องจะไม่มี quarantine flag Gatekeeper จึงไม่เตือน ข้อความ README ในแผนใช้ได้

### ⚪ Optional

- **O1 — server request**: ตอนนี้เจอ request ที่มี id แล้วปิด child ทันทีโดยไม่ตอบ ใช้ได้เพราะปิด process อยู่แล้ว ไม่ต้องเพิ่ม JSON-RPC error response
- **O2 — throttle ข้าม relaunch** (แผน §4 bullet single-flight): ต้องเก็บเวลาที่ request ล่าสุดลง UserDefaults ไม่งั้นเปิด-ปิดแอปรัวๆ แล้วจะเกิน 1 ครั้ง/นาที เป็นแค่การระบุให้ชัดว่าเก็บที่ไหน
- **O3 — cache รายชื่อ MCP**: ไม่แนะนำในรอบแรก รัน discovery ทุกรอบง่ายกว่าและไม่ต้องกังวลว่าข้อมูลเก่า (ราคาแค่ 2 process ต่อ 5 นาที)

## ผล probe รองรับ guard แค่ไหน

- **รองรับได้จริง:** CLI 0.160.0 กับ config นี้ path ที่ขอแค่ quota ไม่ start MCP (MCP เริ่มเมื่อมีคำขอ status หรือเปิด thread ตาม positive control) และ hooks/notify ผูกกับ thread/turn ซึ่งเราไม่เปิด เพราะฉะนั้น guard ทั้งหมดเป็นการ**ป้องกันซ้ำชั้นสอง** ไม่ใช่สิ่งที่ทำให้ path นี้ปลอดภัยตั้งแต่แรก
- **ยังไม่รองรับ:** CLI รุ่นอื่น, config ที่มี managed/project layer, และ plugin ที่ทำงานตอน startup ได้ ซึ่งเป็นเหตุผลของ B1 (ให้ CLI หาค่า effective) และ B2 (assert หลัง start) แทนการเชื่อ probe เครื่องเดียว
- การเขียน dotted key เทียบกับ inline map ใน probe ถูกต้อง และทดสอบซ้ำแล้วว่า inline map ใช้ได้ [verified ผ่าน `mcp list`]

## สิ่งที่แนะนำให้แก้ในแผนก่อนเริ่ม S1

1. §4 ข้อ 4–5: เปลี่ยนเป็นขั้นตอน B1 (`mcp list --json` → map → `mcp list` ยืนยัน → start) และลบเรื่อง TOML reader/fixture layering
2. §4 Protocol: เพิ่ม `config/read` assert ตาม B2 และเปลี่ยนวิธีรับ notification ตาม B3
3. §4 ข้อ 3: เปลี่ยน "รองรับ npm/Node launcher" เป็น "แปลง npm package เป็น native binary" ตาม B4
4. §5: ระบุ wrapper + helper ใน Application Support ตาม C1 และเพิ่ม C2 ลงใน acceptance
5. owner เคาะ C3

## ภาคผนวก — หลังอ่าน MCP discovery source audit (2026-10-03)

อ่าน [source audit](../research/2026-10-03-mcp-discovery-source-audit.md) แล้วตรวจซ้ำ 3 จุดจาก source tag `rust-v0.160.0` [verified]:
`mcp_cmd.rs` เรียก `cloud_config::load_config` + `compute_auth_statuses` ก่อนพิมพ์ และ `ListArgs` มีแค่ `--json`;
`catalog_processor.rs` มี `experimentalFeature/list` ที่วนตาม registry `FEATURES` คืน `enabled` จาก runtime และรองรับ cursor/limit;
`features/src/lib.rs` `apply_map` แค่ `warn!` เมื่อเจอ key ที่ไม่รู้จัก

**ยอมรับว่ารีวิวเดิมผิด 2 จุด:**

- B1: ที่เดาว่า `mcp list` อ่านแค่ไฟล์ในเครื่อง **ผิด** — ถ้ามี HTTP MCP ที่เปิดอยู่ มันอ่าน keyring และอาจยิง OAuth discovery ออก network
- B2: `config/read` เช็ค feature guard ไม่ได้ เพราะ key ปลอมก็ยังส่งกลับมาใน config — เห็นด้วยกับ audit ให้ใช้ `experimentalFeature/list` แทน

**ข้อเสนอใหม่แทน B1 (ยังต้องพิสูจน์ก่อนใช้):** ไม่ใช้ `mcp list` แล้วใช้ app-server 2 รอบแทน

1. child รอบแรก: static guards (รวม `features.plugins=false` ซึ่งตัด MCP จาก plugins ออก) → `initialize` → `config/read` เอาแค่ชื่อใน `mcp_servers` → ปิด ไม่ขอ quota
2. child รอบสอง: static guards + disable map → `config/read` ยืนยัน notify ว่าง/MCP disabled ทุกตัว + `experimentalFeature/list` ยืนยัน hooks/plugins/remote_control/code_mode_host อยู่ใน registry และเป็น false (ไม่เจอ = fail closed) → แล้วจึงขอ quota

เหตุผล: ใช้ path เดียวกับที่ probe ทดสอบแล้ว ไม่มี auth-status computation และไม่ต้องเขียน TOML parser
ต้องพิสูจน์ 2 อย่างก่อน [เดา ยังไม่ได้ตรวจ]: (ก) `config/read` คืน `mcp_servers` ครบทุก layer (project/profile/managed) (ข) canary แบบ probe เดิมยืนยันว่า `config/read` + `experimentalFeature/list` ไม่ start MCP ในรอบแรกที่ยังไม่มี disable map
ถ้าข้อ (ก) หรือ (ข) ไม่ผ่าน → ทางที่เหลือคือใช้ `mcp list` และให้ owner รับทราบเรื่อง keyring/network ของ HTTP MCP

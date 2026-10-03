# แหล่งข้อมูล usage — ข้อเสนอสำหรับ MVP

ตรวจ 2026-10-03 · สถานะ: **อนุมัติ official CLI lifecycle และ Claude snapshot แล้ว** ตาม DECISIONS ข้อ 12; ยังไม่เชื่อม provider ของแอป; controlled CLI startup probe ได้ rateLimits result จริงแล้ว และรอ Claude รีวิว [one-app integration plan](../plans/2026-10-03-one-app-integration-plan.md)

## ข้อค้นพบที่เปลี่ยนจากสมมติฐานเดิม

มีช่องทางที่มีเอกสารรองรับผ่านเครื่องมือของผู้ให้บริการ จึงไม่จำเป็นต้องเริ่มด้วยการให้แอปเราอ่าน OAuth token แล้วเรียก HTTP endpoint ภายในเอง
ข้อมูล quota รายสัปดาห์ต่างจาก API billing, token counts, context window หรือ credit balance; ห้ามใช้สิ่งเหล่านั้นแทน weekly usage

### Codex: app-server JSON-RPC

เอกสาร [Codex App Server](https://learn.chatgpt.com/docs/app-server) รองรับ `account/rateLimits/read` ผ่าน local stdio JSONL และ notification `account/rateLimits/updated`
ผลมี `rateLimits` และอาจมี `rateLimitsByLimitId`; window ใช้ `usedPercent`, `windowDurationMins`, `resetsAt` (Unix seconds)
อ่าน window ที่ระบุ 7 วันจริง (`10080` นาที); ไม่เหมาว่า `secondary` เป็นรายสัปดาห์เสมอ หาก metadata ขาดหรือไม่มี window นี้ ให้ไม่มีข้อมูล weekly

[Authentication](https://learn.chatgpt.com/docs/auth) ระบุ auth cache แบบไฟล์หรือ OS credential store และ CLI-managed automatic token refresh
แอปเราใช้ protocol โดยไม่รับ token เองได้ แต่ไม่อาจรับรองว่า CLI จะไม่เขียน/refresh credential ของตัวเอง เงื่อนไข CLI-managed lifecycle นี้ผู้ใช้อนุมัติแล้วตาม DECISIONS ข้อ 12
Schema จาก CLI 0.160.0 ที่ตรวจในรอบก่อนรองรับ method นี้; duration/reset เป็น optional และไม่มี `noRefresh` ใน params ของ rate-limits read
`account/read` มี `refreshToken=false` แต่ไม่ใช่หลักฐานว่าการอ่าน rate limits จะไม่มี refresh ทางอ้อม

### Claude: Claude Code status-line snapshot

[Status line docs](https://code.claude.com/docs/en/statusline) ระบุ JSON stdin ที่มี `rate_limits.seven_day` และ `five_hour` พร้อม `used_percentage` / `resets_at`
quota windows อาจไม่มี; subscription snapshot ปรากฏหลัง response แรกและ window ที่ reset ผ่านไปอาจถูกตัดออก
ข้อเสนอ: helper ของเราเลือกรับเฉพาะ quota และส่ง snapshot ให้แอป; ไม่บันทึก stdin ทั้งก้อนซึ่งมี session/workspace metadata
ต้องรักษา status line เดิมของผู้ใช้ไว้และตรวจ script เดิมก่อนเตรียม wrapper; รอบนี้ยังไม่ได้เปลี่ยน settings หรือ script

เอกสาร [Authentication and credential use](https://code.claude.com/docs/en/legal-and-compliance#authentication-and-credential-use) จำกัดการนำ subscription credential ไปเป็นตัวกลางของ third-party applications
เอกสารไม่ได้ทำให้ endpoint ที่ reference ใช้เป็น public usage API; สำหรับ MVP เราจึงเสนอ status-line bridge ที่ไม่รับ credential
นี่เป็นการเลือกออกแบบอย่างระมัดระวัง ไม่ใช่ข้อสรุปทางกฎหมายว่าการอ่าน usage ส่วนตัวทุกแบบถูกห้าม

## เปรียบเทียบทางเลือก

| วิธี | แอปเราแตะ token หรือไม่ | ได้ข้อมูลใหม่เมื่อใด | ข้อเสนอ |
|---|---|---|---|
| Codex local app-server | ไม่ต้องรับ token; CLI จัดการ auth | เรียก RPC โดยไม่มี model turn | อนุมัติ CLI-managed lifecycle แล้ว; non-Sandbox module ตาม DECISIONS ข้อ 14 |
| Claude status-line bridge | ไม่รับ token | เมื่อ Claude Code ส่ง snapshot | อนุมัติข้อจำกัด snapshot แล้ว; รอเตรียม bridge |
| HTTP OAuth endpoint ของ reference | ต้องอ่าน token โดยตรง | ตอนเรียก endpoint | ไม่เลือกในข้อเสนอ MVP นี้ |
| browser cookies / scraping หน้าเว็บ | ต้องเพิ่มสิทธิ์เข้าถึง browser/session | ขึ้นกับหน้าเว็บและ session | นอกขอบเขตระยะแรกตาม DECISIONS |

## ข้อเสนอวิธีที่อนุมัติแล้ว (DECISIONS ข้อ 12)

1. **Codex:** ใช้ CLI app-server ที่ติดตั้งอยู่; แอปเราไม่อ่าน เก็บ หรือ refresh token เอง แต่ยอมให้ official CLI จัดการ credential lifecycle ของตัวเองตามปกติ ผู้ใช้อนุมัติข้อยกเว้นเฉพาะ CLI-managed lifecycle แล้ว
2. **Claude:** ยอมรับโหมด snapshot จาก Claude Code; รีเฟรชในแอปหมายถึงอ่าน snapshot ล่าสุด ไม่ใช่บังคับ Claude ดึง quota ใหม่จากผู้ให้บริการ
3. รีเฟรชของแอปคงทุก 5 นาทีและกดเองได้; สำหรับ Claude ต้องระบุแหล่งข้อมูล/เวลารับ snapshot และสถานะข้อมูลเก่า แยกจากเวลาที่ provider fetch สำเร็จ
4. ก่อนติดตั้ง bridge ต้องทำแพตช์ที่รักษา status-line output เดิมพร้อม backup และ preview; การเขียนนอก workspace ขอ escalation แยกเมื่อพร้อมติดตั้ง

หากยังคงข้อห้าม refresh ของ CLI ด้วยอย่างเคร่งครัด จะยังไม่เลือก Codex app-server จนมีวิธีที่พิสูจน์ว่าไม่เกิด credential mutation
หากต้องการ Claude fresh quota ทุก 5 นาทีแม้ไม่เปิด Claude Code ช่องทาง snapshot นี้ยังตอบโจทย์ไม่ครบ; ต้องศึกษาทางเลือกเพิ่มเติม ไม่เพิ่มการเรียก model เพื่อกระตุ้นข้อมูล

## ข้อกำหนด implementation หากอนุมัติ

- แยก `ClaudeSnapshotProvider` กับ `CodexAppServerProvider` จาก UI และ model; ไม่มี dependency library ภายนอก
- รับเฉพาะเปอร์เซ็นต์ finite ในช่วงที่รองรับและ timestamp ที่ valid; field หาย/type ผิดไม่กลายเป็น 0; weekly window หายไม่ใช้ window อื่นแทน
- เก็บ snapshot แบบ private และ atomic; ห้ามเก็บ credential, email, transcript หรือ raw RPC/stdin ลง cache/log/fixtures
- เวลา receive/read ของ bridge ไม่ใช่เวลายืนยันข้อมูลจาก provider; การอ่านไฟล์เดิมหรือ rerun status-line timer ห้ามทำให้ข้อมูลเก่าดูใหม่
- สร้างนโยบายข้อมูลเก่าและการเปลี่ยนบัญชี/หลาย Claude sessions ก่อน integration; ห้ามรับค่า session หนึ่งแล้วอ้างว่าเป็นบัญชีปัจจุบันโดยไม่มีหลักฐาน
- RPC allowlist เฉพาะ initialize และ quota read/notification ที่จำเป็น; ไม่เรียก login/logout, token refresh, credit reset, email หรือ model turns
- ก่อนใช้งานจริงตรวจ executable ที่จะเรียก, CLI version, config/network destinations และการโหลด plugin/MCP; protocol ที่ documented ไม่ได้แปลว่า subprocess จะไม่มี side effects
- network ของแอปยังยึด HTTPS/ปลายทางตรวจสอบได้ตาม DECISIONS; stdio เป็น local IPC ไม่ใช่การอนุญาต remote listener หรือ endpoint ที่ผู้ใช้ config ไว้โดยอัตโนมัติ
- ทดสอบ parser/timeout/process lifecycle ด้วยข้อมูลสังเคราะห์ ก่อน smoke บัญชีจริง; ไม่ต้องมี credential ใน unit tests

## สิ่งที่ reference ทำ (ไม่ใช่วิธีที่อนุมัติให้เรา)

CodexBar checkout `a53a6fe19e62cbeded0bc06a316c2ff1a80cf228` · MIT · อ่านอย่างเดียว ไม่รัน build/script และไม่คัดลอกโค้ด

| เส้นทาง reference | ข้อมูลและพฤติกรรมที่พบ | หลักฐาน source |
|---|---|---|
| Claude HTTP OAuth | `GET https://api.anthropic.com/api/oauth/usage`; Bearer auth + `anthropic-beta`; `five_hour`, `seven_day`, `utilization`, `resets_at` | [Usage fetcher](../../reference/CodexBar/Sources/CodexBarCore/Providers/Claude/ClaudeOAuth/ClaudeOAuthUsageFetcher.swift) |
| Claude credential handling | มีการอ่าน credential file, cache ของตัวเองและ refresh/delegated refresh; ไม่ตรงกับข้อจำกัด read-only ทั้งหมดของเรา | [Credentials](../../reference/CodexBar/Sources/CodexBarCore/Providers/Claude/ClaudeOAuth/ClaudeOAuthCredentials.swift) |
| Codex HTTP OAuth | `GET https://chatgpt.com/backend-api/wham/usage`; Bearer auth; primary/secondary windows ใช้ `used_percent`, `reset_at`, `limit_window_seconds` | [Usage fetcher](../../reference/CodexBar/Sources/CodexBarCore/Providers/Codex/CodexOAuth/CodexOAuthUsageFetcher.swift) |
| Codex CLI | เริ่ม app-server แล้วอ่าน rate limits ผ่าน JSON-RPC; auth lifecycle เป็นของ CLI | [UsageFetcher](../../reference/CodexBar/Sources/CodexBarCore/UsageFetcher.swift) |

source links ใช้ local clone ที่ gitignored; ผู้รับ repo ใหม่ต้อง clone reference ที่ commit เดียวกันเอง
การมีโค้ดใน reference ไม่ใช่การยืนยันว่า HTTP endpoint ได้รับการรองรับอย่างเป็นทางการ

## หลักฐานและงานที่ยังไม่ตรวจ

- เครื่องมือและ metadata ของ credential files: [environment validation](../validation/2026-10-03-environment.md)
- guarded/canary CLI startup probe ได้ rateLimits result จริงแล้วตาม validation ล่าสุด โดยไม่เก็บ raw quota/account; ยังไม่ตรวจ Keychain value/access หรือพิสูจน์ token refresh behavior และแอปยังไม่เชื่อม
- ยังไม่พิสูจน์ timestamp ความสดของ Claude snapshot, ผลหลังสลับบัญชี และความเข้ากันได้ของ wrapper กับ status line เดิม
- มี source/offline core และ build/tests แล้วตาม STATUS; ยังไม่ทดสอบ macOS 14 และยังไม่เชื่อมบัญชีจริง

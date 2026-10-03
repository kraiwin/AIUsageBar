# HANDOFF — AIUsageBar S1–S3 (2026-10-03 22:58:51 +07 +0700)

สร้างด้วย codex-handoff แบบ full สำหรับ session ใหม่ลงมือ implementation ต่อเนื่อง

ฉบับ public ตัดรายละเอียด workflow ส่วนตัวที่ไม่เกี่ยวกับแอปออก; สถานะในไฟล์เป็นประวัติ ณ เวลานั้น

## TL;DR

เจ้าของยอมรับ **CLI-only ใช้ส่วนตัวบน Mac นี้** ต้องมี Codex CLI/Claude Code ไม่ต้องขยาย browser-only/enterprise profiles ก่อน MVP
มี UI text-only/offline core/RPC แล้ว build 5; **production Codex transport และ Claude bridge ยังไม่ทำ**
สอง prerequisites ผ่านใน CLI0.160 default context; C3 ownerเลือก ก (valid latest ไม่มี quota →no-data/—)
เริ่ม [execution plan S1–S3](plans/2026-10-03-s1-s3-execution-plan.md) ใน session ใหม่หลังตรวจสด ใช้ sub-agents จริงและทำต่อเนื่องไม่ถามทำต่อทุกเฟส

## ข้อสรุปที่ต้องรักษา

- DECISIONS ข้อ14: single non-Sandbox + Hardened Runtime/ad-hoc ไม่มี XPC/App Store/notarization/Apple Developer Program/third-party dependency
- ข้อ17: Claude missing quota ล่าสุดต้องล้างค่าเดิม ไม่โชว์ตัวเลขบัญชีเก่า; invalid payloadแยก error ไม่ใช่0
- ข้อ18: passเฉพาะ default direct app-server context source/runtimetestsจริง ไม่รับรอง named v2 profileหรือenterprise/MDM/cloud runtimeทุกแบบ
- ข้อ19: งานหลักคือ personal CLI app; session ใหม่ทำ S1→S2→S3 ต่อเนื่องตาม execution plan รวม prepare publicsourceเดิม การยอมรับใช้ส่วนตัวไม่ยกเลิก publicsourcegoal
- GitHub destination/คำสั่งpublicationยังไม่ชัด: เตรียมsourceให้ครบก่อนถามปลายทาง ไม่create/pushในhandoffรอบนี้ ไม่ push repository อื่น
- UIชื่อเต็มบรรทัดเดียว ไม่มีiconซ้าย; normalไม่ใส่sampleตัวเลข demoเฉพาะ--demo; stale/snapshotต้องแสดงตามจริง
- ไม่มี token/cookie reader/log/cacheของเรา; officialCLIจัดการauth/refreshของตัวเองได้รับอนุมัติแล้ว ไม่ถามนโยบายนี้ซ้ำ

## State ที่ตรวจสดในรอบ handoff

- Repo `~/Documents/AIUsageBar`, branch main, HEAD `cf11c03`; `git remote` ว่าง ไม่มีpublication/push
- ไม่มี stagedchanges; modified tracked: .gitignore, AGENTS.md, README.md, docs/DECISIONS.md, docs/PROJECT_BRIEF.md
- Source/project/tests/Config/CHANGELOG/MEMORY/docsส่วนใหญ่ untracked งานหลังinitialscaffoldยังไม่commit **ห้าม reset/cleanทิ้ง**
- `git diff --stat` แสดง tracked5ไฟล์เท่านั้น ไม่รวมappsource untracked อย่าใช้statนั้นสรุปscopeทั้งหมด
- Config/App.xcconfig:0.1.0/build5, macOS14, Swift6 strictconcurrency, ad-hoc Manual, HardenedRuntime YES แต่ **ENABLE_APP_SANDBOXยังYES** และentitlements app-sandbox=true ต้องเปลี่ยนในS1
- อ่าน xcresult เดิม `build/TestResults/CodexRPC-003.xcresult` ในรอบนี้: Passed61 / failed0 / skipped0, runtimeWarningsว่าง บนmacOS27.0.1arm64 ไม่ได้รันtests/buildใหม่เพื่อhandoff
- Releasebuild5ผ่านจากรอบก่อน validation; appsourceไม่มีproductionProcess/URLSession/credentialreader/refresh timer ตอนนี้ UI normalยังempty/disconnected
- Probe child finalแต่ละตัว exit0ตามsanitizedresults ไม่มีquota/thread/modelใน inventoryชุดใหม่; รอบhandoffไม่ได้inspect liveapp/demoPIDหรือbrowserstate **อย่าใช้PIDs/portsในhandoffเก่าโดยไม่ตรวจสด**

## หลักฐานและ gotchas ที่สำคัญ

อ่านตามลำดับ:

1. [integration design](plans/2026-10-03-one-app-integration-plan.md) — currenttwo-childdesign
2. [Claude review + appendix](plans/2026-10-03-one-app-integration-plan-review.md) — B1–B4แก้ในเอกสารแล้วและแก้ข้อเดาเดิม
3. [discovery source audit](research/2026-10-03-mcp-discovery-source-audit.md)
4. [inventory prerequisites](validation/2026-10-03-codex-inventory-prerequisites.md) — finalruntimeevidence/scope
5. [offline RPC validation](validation/2026-10-03-codex-rpc.md) — baseline61tests

- **ไม่ใช้ mcp list --json เป็นทางหลัก**: source0.160มีHTTP OAuth/keyring auth-statusและJSONenv/transportอาจมีsecrets ไม่มีoffline/skip-authswitch fallbackต้องownerรับขอบเขตใหม่ก่อน
- config/read echo unknownboolfeatureได้ ใช้ experimentalFeature/list paginatedregistry/runtimeenabledสำหรับhooks/plugins/code_mode_host + config/readสำหรับnotify/MCP; ค่าfalseที่echoไม่เป็นproof
- remote_controlเป็นRemoved/defaultfalseและruntimeignore ไม่ใช่functionalguard อย่าอ้างปิดremotecontrolจากค่านี้
- directappserver0.160ไม่ได้forward --profile-v2; ใช้defaultcontext ไม่คัดลอกcredentials/redirectHOME/CODEX_HOMEเพื่อจำลองprofile
- config/readแสดง explicit enabled layersที่ใช้กับcwd ไม่ใช่pluginresolvedMCPทั้งหมด; plugins=falseต้องregistryverified ข้อมูลที่เคยเห็น7effectiveMCPมาจากuser3+plugins4 เมื่อปิดpluginsเหลือ3
- runtimefixtureล่าสุดเห็นuser3+project1+CLI1=5, layers sessionFlags/project/user/system firstและsecondregistry4pages firstMCPfixturesเปิดอยู่แต่0markers secondปิด5ทั้งหมด0markers positivecontrolเรียกMCPstatusทำ2markers confighashเดิมไม่เปลี่ยน
- **disablemap enabled=falseอย่างเดียวไม่พอ**สำหรับproject-only entry เพราะbootstrapก่อนprojectmergeขาดtransport: ต้องvalidatekindแล้วใช้disabledplaceholder stdio=/usr/bin/false หรือHTTP=https://example.invalid ไม่copyargs/env/header/commandเดิมลงargv unknownkindให้unsupported
- Finalprobeไม่injectfakeguard อย่าอ้างunknown_guard=falseเป็นnegativecontrol ผ่าน S1ต้องมีtestsสำหรับunknown/renamed/true/missing/registrypagination
- configreadอาจresponseใหญ่ ใช้ceilingแยกเสนอ2MiBและboundedmemory ไม่ส่งrawผลขึ้นtool/log/fixture
- RPCcoreปัจจุบันยังhandshake→quotaและallowlistnotificationเดิม ต้องเปลี่ยนในS1เป็นguardqueriesก่อนquotaและdiscardvalidunknownnotification(noid) serverrequest/response/disguisedtokenยังstrict
- SwiftCLI wrapper/helperอยู่นอกbundleในApplicationSupportคงstatuslineเดิม user-levelcommandfieldonly preservingpadding/keys และ no-data最新เลือกกแล้ว latestsessionไม่accountbinding

## ตำแหน่งโค้ด/อุปกรณ์

- Providers/CodexRPCSession.swift pureofflineprotocol; UsagePayloadParser.swift strictquota parser
- Models/UsageFreshness.swift, UsageState.swift; UI/MenuBarText.swift ยังเป็นrenderer ไม่ได้เชื่อมnormalstatus titleกับrealstate
- UI/StatusItemController.swift เป็นNSStatusItem/popover/tooltip; App/AIUsageBarApp.swift lifecycle
- nativecandidateที่probeใช้ (ต้องตรวจสดเมื่อเริ่ม):
  `~/.nvm/versions/node/v24.21.0/lib/node_modules/@openai/codex/node_modules/@openai/codex-darwin-arm64/vendor/aarch64-apple-darwin/bin/codex`
- Claudeoriginalstatuslineที่เคยตรวจ `~/.claude/statusline.sh`; ต้องreadsafeก่อนexactpreview/checkrevision ตอนติดตั้งไม่ใช้snapshothashเดิมเป็นapproval
- Source/probeharnessอยู่build/StartupProbe, build/InventoryProbe gitignored; ไม่ย้ายrawconfig/auth/accountpayloadเข้าGitหรือใช้harnessแทนproductionmodule
- reference/CodexBar commit a53a6fe19e62cbeded0bc06a316c2ff1a80cf228 read-only ไม่build/scriptsไม่copycodeทั้งแอป
- Xcode27.0/Swift6.4/macOS27.0.1arm64ที่เคยตรวจ READMEมีbuild/testcommands deploy14แต่ไม่runtimeclaim14/Intel

## รับช่วงแล้วทำอะไร

1. อ่าน AGENTS/brief/decisions/status/memory/handoff; ตรวจ Git/version/nativepath/toolchain ก่อนtrustsnapshot รักษางานuntrackedทั้งหมด
2. อ่าน executionplan แล้วลงมือS1.1–S1.4 ตั้งownershipก่อนsubagentwrites mainเป็นintegration/docs/projectfileowner
3. ทำmeaningfultestsและaccountsmoke/reviewตามdesignที่ยอมรับ ไม่มีquota/rawpayloadในoutputs แก้failureเองก่อนต้องถามowner
4. หลังCodexจริงทำS2helper/installerและlatestClaudequota; globalinstallเตรียมpreview/backupก่อนขอfilesystemescalation ใช้MacComputerUseเดิมสำหรับUItests ไม่PC/Atlas
5. S3รวมUI/refresh/resource/smoke/README/LICENSE/finalreview/sourceprep อัปเดตSTATUS/CHANGELOGตามหลักฐาน
6. เมื่อพร้อมpublicถามเฉพาะdestination/คำสั่งที่ขาด ไม่รอข้อ12เพื่อหยุดS1–S3 localwork ไม่publish/signnotarize/privatebackuprepoเอง

Toolpermissionหรือaccountloginสามารถหยุดรอได้ตามจริง ไม่ถือว่าเวลาที่ผ่านเป็นapproval ถ้าแก้routinecodeได้ให้ทำเองไม่ถามtechnicalchoiceซ้ำทุกเฟส

## Prompt ให้ user paste ใน session ใหม่

> อ่าน AGENTS.md, MEMORY.md และ handoffล่าสุด แล้วทำ docs/plans/2026-10-03-s1-s3-execution-plan.md ตามลำดับ S1–S3จนพร้อมใช้งานส่วนตัวบนMacนี้ผ่านCodexCLI/ClaudeCode ใช้sub-agentsขนานจริงเมื่อแบ่งscopeได้ ไม่ถามทำต่อทุกเฟส ข้อกเลือกแล้วและprerequisitesผ่านdefaultCLI0.160ตามหลักฐาน ตรวจstateใหม่ รันimplementation/tests/realCLI smoke/review/docsตามแผน ถ้าเจอblockerแก้เองก่อนหรือถามเฉพาะข้อมูล/สิทธิ์จำเป็น เตรียมpublicsourceให้ครบแต่ไม่publish/pushจนปลายทางและคำสั่งเผยแพร่ชัด อัปเดตSTATUS/CHANGELOG/handoffตามผลจริง ห้ามเปิดเผยcredential/accountpayloadและห้ามclaimผลที่ไม่ได้ตรวจ

# แนวทางพัฒนาและส่งมอบ

ใช้กับงานในเครื่องตั้งแต่ต้นจนเตรียม release; ขอบเขตและข้ออนุญาตยึด DECISIONS

## AIUsageBar Engineering Baseline v1

ชื่อแนวทางคุณภาพของโปรเจกต์นี้ ใช้ตรวจโค้ดและผลลัพธ์ทุกชุดงาน ไม่ใช่คำรับรองว่าได้ certification หรือสอดคล้องครบทุกข้อของมาตรฐาน
อ้างอิงคำอธิบายสาธารณะของมาตรฐาน; ยังไม่ได้ทำ formal conformity assessment

| แหล่งอ้างอิง | ใช้กับโปรเจกต์อย่างไร |
|---|---|
| [ISO/IEC 25010:2023 — Product quality model](https://www.iso.org/standard/78176.html) | กำหนดคุณภาพที่ต้องวัดและตรวจตลอดอายุผลิตภัณฑ์; ตารางด้านล่างเป็นเกณฑ์ของเรา ไม่ใช่การคัดลอกข้อกำหนด ISO ทั้งฉบับ |
| [NIST SSDF v1.1 — SP 800-218](https://csrc.nist.gov/pubs/sp/800/218/final) | ใช้แนวทางพัฒนาที่ปลอดภัย: กำหนดข้อจำกัด, ปกป้อง source/credential, ตรวจงานก่อนส่งมอบ และแก้สาเหตุของช่องโหว่ |
| [Swift API Design Guidelines](https://www.swift.org/documentation/api-design-guidelines/) | ตั้งชื่อและออกแบบ API ให้ชัดเจนเมื่อเรียกใช้; เขียนคำอธิบาย contract ของ API |

[ISO/IEC 27001:2022](https://www.iso.org/standard/27001) เป็นมาตรฐานระบบบริหารความมั่นคงปลอดภัยสารสนเทศขององค์กร จึงไม่ใช้เป็นชื่อมาตรฐานโค้ดหรืออ้างว่าแอปนี้ได้รับรอง ISO 27001

### กฎโค้ดที่ใช้ review

- ใช้ชื่อ type แบบ `UpperCamelCase`, member แบบ `lowerCamelCase`; ชื่อสื่อความหมายและหน่วย เช่น percent กับ timestamp ต้องแยกชัดเจน
- ใช้ strong types, `struct` และ `let` เมื่อเหมาะสม; แยก payload ของบริการออกจาก model ที่ UI ใช้; ตรวจค่าที่ขอบเขต provider
- ห้าม `try!`, force unwrap หรือกลืน error ในเส้นทาง credential/network/payload; ใช้ error ที่มีชนิดและแปลงเป็นข้อความ UI ที่ไม่เปิดเผยข้อมูลลับ
- ใช้ `async/await` สำหรับ I/O; ไม่ block main thread; state ของ UI อยู่บน main actor; จัดการ cancellation และป้องกันผลจาก request เก่าทับ request ใหม่
- แยก credential reader, transport, parser และ UI ตามหน้าที่; ฉีด transport/clock สำหรับทดสอบได้ โดยไม่สร้าง framework ทั่วไปเกินความจำเป็น
- จำกัด access level; อธิบาย API contract, side effects, error และเหตุผลของทางเลือกที่ไม่ชัดเจน; comment ไม่เพียงทวนโค้ด
- ใช้ indent 4 spaces และรูปแบบสม่ำเสมอ; ไม่เพิ่ม formatter/linter ภายนอกโดยฝ่าฝืนข้อจำกัด dependency
- compiler warnings ที่เกิดจากโค้ดเราใหม่ต้องแก้ก่อนปิดงาน; ข้อยกเว้นต้องบันทึกเหตุผล ผลกระทบ และงานแก้ต่อ

### หลักฐานคุณภาพของโปรเจกต์

| เป้าหมาย | เกณฑ์ตรวจ / หลักฐาน |
|---|---|
| ข้อมูลถูกต้อง | parser tests ครอบคลุม field/type/range และ reset time; เทียบข้อมูลจริงโดยไม่เก็บข้อมูลบัญชี |
| ทนต่อความผิดพลาด | error/recovery tests; แสดงข้อมูลเก่าพร้อมสถานะตามจริง; refresh ไม่เกิด request ซ้อนโดยไร้การควบคุม |
| ปลอดภัย | ตรวจ credential access, host/redirect, logs และ diff; ไม่มีสำเนา token หรือ dependency ที่ไม่อนุมัติ |
| ตอบสนองและใช้ทรัพยากรเหมาะสม | smoke test เมนูระหว่าง fetch; วัด CPU/memory และจำนวน request ก่อน MVP; บันทึกสภาวะทดสอบและเกณฑ์ยอมรับก่อนอ้างว่าผ่าน |
| ใช้งานได้ | ตรวจภาษาไทย, keyboard navigation, VoiceOver และสถานะ loading/error บน Mac จริง |
| ดูแลและเปลี่ยน provider ได้ | unit tests ที่ไม่ต้องมีบัญชีจริง; provider แยกจาก UI; คำสั่ง build/test ทำซ้ำได้ |
| รองรับระบบเป้าหมาย | บันทึก OS/architecture ที่ทดสอบจริง; ไม่อ้างว่าทดสอบ macOS 14 หากตรวจบนเวอร์ชันอื่นเท่านั้น |

ข้อยกเว้นจาก baseline ต้องมีเหตุผล ความเสี่ยง วิธีตรวจหรือชดเชย และผู้อนุมัติใน DECISIONS ก่อนปิดงาน; agent อนุมัติข้อยกเว้นให้กันเองไม่ได้
การผ่านเกณฑ์ของโปรเจกต์ไม่เท่ากับได้รับ certification จาก ISO/NIST

## เอกสารที่เป็นแหล่งข้อมูลหลัก

| เอกสาร | หน้าที่ | อัปเดตเมื่อ |
|---|---|---|
| PROJECT_BRIEF | เป้าหมายและขอบเขต | เป้าหมายเปลี่ยน |
| DECISIONS | ข้อสรุปและเหตุผลที่ตกลงแล้ว | มีข้อสรุปใหม่หรือเปลี่ยนข้อสรุป |
| STATUS | สถานะปัจจุบัน, milestone, blockers และงานถัดไป | จบชุดงานหรือสถานะเปลี่ยน |
| CHANGELOG | การเปลี่ยนแปลงสำคัญที่ยังไม่ release และที่ release แล้ว | มีผลต่อผู้ใช้หรือการดูแลโปรเจกต์ |
| research/usage-sources.md | หลักฐานและข้อจำกัดของวิธีเชื่อมบริการ | ตรวจพบหรือเปลี่ยนวิธีเชื่อม |

ไม่สร้าง PROGRESS แยกจาก STATUS เพราะจะซ้ำและคลาดเคลื่อนง่าย
เมื่อมีผลตรวจสำคัญ ให้บันทึกคำสั่ง/ขั้นตอน วันที่ ผล และข้อจำกัดใน `docs/validation/` และลิงก์จาก STATUS
เก็บเฉพาะหลักฐานที่ตัดข้อมูลลับแล้ว; ไม่แนบ raw auth/network dumps

## วงจรทำงาน

1. อ่าน brief, decisions และ status; กำหนดผลลัพธ์และเกณฑ์เสร็จของงานที่กำลังทำ
2. ตรวจ working tree ก่อนแก้; รักษางานเดิมของผู้ใช้; ทำงานเป็นชุดเล็กที่ review และย้อนกลับได้
3. แยก model, provider และ UI; ไม่ให้ UI อ่าน credential หรือรู้รายละเอียด endpoint
4. ตรวจ diff และทดสอบตามความเสี่ยง; แก้ข้อผิดพลาดก่อนอ้างว่าเสร็จ
5. อัปเดต STATUS และ CHANGELOG ตามผลจริง; ข้อสรุปใหม่ลง DECISIONS
6. หากสร้าง commit ให้หนึ่ง commit มีจุดประสงค์ชัดเจน เช่น `feat:`, `fix:`, `docs:`, `test:` หรือ `build:`; commit เฉพาะไฟล์ของชุดงาน

งาน authentication, ความถูกต้องของ usage หรือการเปลี่ยนข้ามโมดูลที่มีผลสำคัญต้องมี independent review ตาม AGENTS ก่อนปิดงาน
ไม่เพิ่ม dependency, Git hooks หรือ CI ที่รันโค้ดจากภายนอกโดยไม่ได้ตรวจและอธิบายเหตุผล
เมื่อมี source ให้กำหนดคำสั่ง build/test ที่ทำซ้ำได้ใน README รวมเครื่องมือที่ใช้และ target macOS

## เกณฑ์เสร็จของงานโค้ด

- พฤติกรรมตรงกับเกณฑ์ของงาน; ไม่มี TODO ที่ทำให้เส้นทางหลักใช้งานไม่ได้โดยไม่เปิดเผย
- build ผ่านด้วยเครื่องมือที่บันทึกไว้ และตรวจการใช้งานจริงบน Mac สำหรับการเปลี่ยน UI/app lifecycle
- ทดสอบ logic ที่เสี่ยง: parse, สถานะ error, เวลา reset และการรีเฟรช; ไม่เพิ่ม test ที่ตรวจเพียงว่ามีข้อความหรือไฟล์
- ระบุผลตรวจจริงและสิ่งที่ยังไม่ได้ตรวจ; build ผ่านไม่ได้แปลว่า integration ผ่าน
- ไม่มี credential, cookie, authorization header หรือข้อมูลบัญชีใน diff/log/fixture
- เอกสารสถานะและข้อจำกัดตรงกับโค้ดที่ส่งมอบ

## เกณฑ์ตรวจ MVP

| ส่วน | กรณีสำคัญ |
|---|---|
| การอ่านข้อมูล | payload ถูกต้อง, field หาย, type ผิด, ค่าไม่สมเหตุผล และบริการไม่รองรับ weekly usage |
| สถานะบัญชี | ไม่มี credential, ปฏิเสธ Keychain, credential หมดอายุ; ไม่แก้หรือ refresh token ของแอปอื่น |
| เครือข่าย | offline, timeout, 401/403, 429 และ 5xx; ไม่ retry รัว; เคารพ polling ที่ตกลงไว้ |
| ความสดของข้อมูล | โหลดไม่สำเร็จต้องระบุสถานะและเวลาสำเร็จล่าสุด; ไม่ทำให้ค่าเก่าดูเป็นค่าปัจจุบัน |
| เวลาและ UI | timezone ของเครื่อง, reset time, เปิดเมนู, refresh, quit, sleep/wake และไม่มี Dock icon |
| ความลับและปลายทาง | HTTPS ปลายทางคงที่; ไม่ส่ง credential ข้าม host ผ่าน redirect; ไม่ log body/header ที่มีข้อมูลลับ |

ใช้ข้อมูลสังเคราะห์ใน unit tests ได้เมื่อระบุชัดว่าเป็น fixture; ห้ามนำไปแสดงเป็น usage จริงในแอป

## เวอร์ชันและ release

- ใช้รูปแบบ `MAJOR.MINOR.PATCH`; เริ่ม MVP ที่ `0.1.0` และถือช่วง `0.x` ว่ายังพัฒนาอยู่
- PATCH สำหรับการแก้บั๊กที่คงพฤติกรรมที่รองรับ; MINOR สำหรับความสามารถใหม่; หลัง `1.0` การเปลี่ยนที่เข้ากันไม่ได้เพิ่ม MAJOR
- เมื่อมี app target ให้เก็บ app version และ build number ใน build configuration แห่งเดียว (`CFBundleShortVersionString` / `CFBundleVersion`); ไม่สร้าง VERSION ที่ต้องแก้ซ้ำ
- บันทึกเวอร์ชัน Swift/Xcode ที่ตรวจจริงเพื่อทำซ้ำ build; ไม่อ้าง reproducible binary จนกว่าจะทดสอบ
- ก่อน release: ตรวจ clean diff, ผล build/test, smoke test, secrets, license และเครดิต; ย้าย Unreleased เป็นเวอร์ชันพร้อมวันที่จริง
- เตรียม MIT LICENSE ของเราและ third-party notices ก่อนแจกจ่ายตาม DECISIONS; MIT เป็นเงื่อนไขการใช้โค้ด ไม่ใช่การรับรองคุณภาพหรือความปลอดภัย
- การ tag release, publish, push หรือ signing/notarization เพื่อแจกจ่ายต้องได้รับอนุญาตตามขอบเขตที่ตกลงไว้

## เมื่อพบปัญหา credential หรือ provider

หยุดส่ง request ที่เกี่ยวข้องและแก้เส้นทางที่รั่วก่อนใช้งานต่อ; ไม่คัดลอก secret ลง issue หรือเอกสาร
ถ้าพบการเปิดเผย credential ให้แจ้งผู้ใช้เพื่อเพิกถอนผ่านผู้ให้บริการ โดยไม่แก้ credential ของแอปอื่นเอง
หาก endpoint/schema เปลี่ยน ให้แสดงโหลดไม่สำเร็จ เก็บเวลาสำเร็จล่าสุด และยืนยัน contract ใหม่ก่อนปรับ provider

## Source-build distribution ตาม DECISIONS ข้อ 14

เป้าหมาย public source repo ให้ผู้ใช้ build จาก Xcode ด้วย local ad-hoc โดยไม่ต้อง Developer ID/Apple Developer Program; ไม่เข้า App Store ไม่ notarize งาน signing/notarization เพื่อแจก binary ในข้อความ baseline ก่อนหน้านี้อยู่นอกขอบเขตปัจจุบัน ยังต้อง source/secret/license/credits checks ก่อน public และรอบแผนส่ง revised plan ให้ Claude รีวิวแล้ว; ปัจจุบันผู้ใช้สั่ง S1–S3/local personal install ตาม DECISIONS ข้อ 20 ไม่มี publish/push

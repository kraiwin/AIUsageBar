import Foundation

/// Safe display data. Retained readings always keep a visible freshness/failure label.
struct UsagePresentation {
    enum Tone { case neutral, success, warning, failure }

    let title: String
    let detail: String?
    let quota: QuotaWindow?
    let tone: Tone

    static var configurationUnavailable: Self {
        Self(title: "ข้อมูลไม่พร้อม", detail: nil, quota: nil, tone: .failure)
    }

    init(state: UsageState, now: Date, policy: UsageFreshnessPolicy) {
        switch state {
        case .notConnected:
            self.init(title: "ยังไม่เชื่อมบริการ", detail: nil, quota: nil, tone: .neutral)
        case .loading(let previous):
            self.init(title: "กำลังโหลด…", detail: previous == nil ? nil : "ข้อมูลครั้งก่อน",
                      quota: Self.retainedQuota(previous, now: now, policy: policy), tone: .neutral)
        case .unavailable:
            self.init(title: "ไม่มีข้อมูลรายสัปดาห์", detail: nil, quota: nil, tone: .neutral)
        case .requiresLogin:
            self.init(title: "ต้องเชื่อมบัญชี", detail: "ล็อกอินผ่านโปรแกรมของบริการก่อน",
                      quota: nil, tone: .warning)
        case .lastFailure(let previous, let lastSuccessfulAt):
            let detail = lastSuccessfulAt.map { "สำเร็จล่าสุด \(Self.format($0))" }
                ?? (previous == nil ? nil : "ข้อมูลครั้งก่อน · ยังยืนยันความสดไม่ได้")
            self.init(title: "โหลดไม่สำเร็จ", detail: detail,
                      quota: Self.retainedQuota(previous, now: now, policy: policy), tone: .failure)
        case .available(let reading):
            switch UsageFreshness.evaluate(reading, now: now, policy: policy) {
            case .fresh:
                self.init(title: "ข้อมูลล่าสุด", detail: nil, quota: reading.weekly, tone: .success)
            case .stale:
                self.init(title: "ข้อมูลเก่า", detail: "รอการอัปเดตจากบริการ",
                          quota: reading.weekly, tone: .warning)
            case .snapshot:
                self.init(title: "ข้อมูลที่ได้รับล่าสุด", detail: reading.source == .claudeStatusLine
                          ? "จาก Claude Code ล่าสุด เวลา \(Self.format(reading.receivedAt))"
                          : "ยังยืนยันความสดไม่ได้",
                          quota: reading.weekly, tone: .warning)
            case .expired:
                self.init(title: "ถึงเวลารีเซ็ตแล้ว", detail: "รอข้อมูลรอบใหม่", quota: nil, tone: .warning)
            case .invalidTimestamp:
                self.init(title: "ข้อมูลเวลาไม่ถูกต้อง", detail: nil, quota: nil, tone: .failure)
            }
        }
    }

    private init(title: String, detail: String?, quota: QuotaWindow?, tone: Tone) {
        self.title = title
        self.detail = detail
        self.quota = quota
        self.tone = tone
    }

    private static func retainedQuota(_ reading: UsageReading?, now: Date,
                                      policy: UsageFreshnessPolicy) -> QuotaWindow? {
        guard let reading else { return nil }
        switch UsageFreshness.evaluate(reading, now: now, policy: policy) {
        case .fresh, .stale, .snapshot:
            return reading.weekly
        case .expired, .invalidTimestamp:
            return nil
        }
    }

    static func format(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "th_TH")
        formatter.timeZone = .autoupdatingCurrent
        formatter.dateFormat = "EEE d MMM HH:mm"
        return formatter.string(from: date)
    }
}

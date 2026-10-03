import CoreFoundation
import Foundation

/// Payload-free failures: never preserve a server message, account object or raw JSON.
enum CodexRPCError: Error, Equatable, Sendable {
    case invalidLimits, invalidState, invalidTimestamp
    case byteLimit, lineLimit, malformedJSON, invalidEnvelope, unexpectedResponse
    case invalidHandshake, invalidQuota, serverRequest, serverError(code: Int)
    case unexpectedEOF, alreadyFinished
}

/// One offline, single-use stdio handshake and quota request. The transport owns
/// process lifetime, timeout/cancellation and writing every outbound line in order.
/// This type performs no I/O and never asks for or responds with credentials.
struct CodexRPCSession: Sendable {
    struct Limits: Equatable, Sendable {
        let lineBytes: Int
        let totalBytes: Int
        let lineCount: Int

        init(lineBytes: Int = 65_536, totalBytes: Int = 262_144,
             lineCount: Int = 128) throws {
            guard (1...65_536).contains(lineBytes),
                  (1...262_144).contains(totalBytes),
                  (1...128).contains(lineCount), lineBytes <= totalBytes else {
                throw CodexRPCError.invalidLimits
            }
            self.lineBytes = lineBytes
            self.totalBytes = totalBytes
            self.lineCount = lineCount
        }
    }

    enum QuotaFreshness: Sendable {
        /// A response alone does not prove a fresh upstream fetch by a CLI version.
        case unverified
        /// Only use after verifying that this transport/CLI response is a fresh fetch.
        case verifiedFreshFetch
    }

    struct Completion: Equatable, Sendable {
        /// nil means the response has no supported Codex weekly quota.
        let reading: UsageReading?
        let receivedAt: Date
    }

    struct Update: Equatable, Sendable {
        let outbound: [Data]
        let completion: Completion?
    }

    private enum Phase: Sendable {
        case idle, initializing, readingQuota, completed, failed
    }

    private let limits: Limits
    private var phase: Phase = .idle
    private var buffer = Data()
    private var totalBytes = 0
    private var lineCount = 0
    private var startedAt: Date?
    private var quotaRequestedAt: Date?
    private var lastReceivedAt: Date?

    init(limits: Limits) { self.limits = limits }

    init() {
        // These fixed defaults satisfy Limits' bounds; no fallible forced initializer.
        self.limits = Limits.defaultLimits
    }

    mutating func start(at date: Date) throws -> Data {
        guard phase == .idle else { throw CodexRPCError.invalidState }
        guard Self.valid(date) else { throw fail(.invalidTimestamp) }
        startedAt = date
        phase = .initializing
        return Data(#"{"id":1,"method":"initialize","params":{"clientInfo":{"name":"aiusagebar","title":"AIUsageBar","version":"0.1.0"}}}"#.utf8) + Data([10])
    }

    /// Each receipt time describes the chunk's arrival, not the time a cached file
    /// was read. A verified fetch uses the request start as a conservative observation.
    /// Errors terminate the session. Calls after completion fail without undoing success.
    mutating func receive(_ chunk: Data, receivedAt: Date,
                          quotaFreshness: QuotaFreshness = .unverified) throws -> Update {
        if phase == .completed { throw CodexRPCError.alreadyFinished }
        guard phase == .initializing || phase == .readingQuota else {
            throw CodexRPCError.invalidState
        }
        do {
            guard Self.valid(receivedAt), let startedAt, startedAt <= receivedAt,
                  lastReceivedAt.map({ $0 <= receivedAt }) ?? true else {
                throw CodexRPCError.invalidTimestamp
            }
            // Check before copying even a single byte into our retained buffer.
            guard chunk.count <= limits.totalBytes - totalBytes else {
                throw CodexRPCError.byteLimit
            }
            totalBytes += chunk.count
            lastReceivedAt = receivedAt
            var outbound: [Data] = []
            let quotaRequestWasPending = phase == .readingQuota
            for byte in chunk {
                if byte == 10 {
                    lineCount += 1
                    guard lineCount <= limits.lineCount else { throw CodexRPCError.lineLimit }
                    let line = buffer
                    buffer.removeAll(keepingCapacity: false)
                    let update = try process(line, receivedAt: receivedAt,
                                             quotaFreshness: quotaFreshness,
                                             quotaRequestWasPending: quotaRequestWasPending)
                    outbound.append(contentsOf: update.outbound)
                    if let completion = update.completion {
                        // One-shot success is final. Close transport now and discard
                        // bounded remainder without interpreting trailing payloads.
                        return Update(outbound: outbound, completion: completion)
                    }
                } else {
                    guard buffer.count < limits.lineBytes else { throw CodexRPCError.byteLimit }
                    buffer.append(byte)
                }
            }
            return Update(outbound: outbound, completion: nil)
        } catch let error as CodexRPCError {
            throw fail(error)
        } catch {
            throw fail(.invalidEnvelope)
        }
    }

    /// JSONL requires a final newline. EOF during a handshake/request is failure,
    /// even if the last unterminated bytes look like a complete JSON object.
    mutating func finish() throws {
        if phase == .completed { return }
        guard phase == .initializing || phase == .readingQuota else {
            throw CodexRPCError.invalidState
        }
        throw fail(.unexpectedEOF)
    }

    private mutating func process(_ line: Data, receivedAt: Date,
                                  quotaFreshness: QuotaFreshness,
                                  quotaRequestWasPending: Bool) throws -> Update {
        let value: Any
        do { value = try JSONSerialization.jsonObject(with: line) }
        catch { throw CodexRPCError.malformedJSON }
        guard let envelope = value as? [String: Any] else { throw CodexRPCError.invalidEnvelope }
        if let method = envelope["method"] {
            guard let method = method as? String, !method.isEmpty,
                  envelope["result"] == nil, envelope["error"] == nil else {
                throw CodexRPCError.invalidEnvelope
            }
            // All server requests, including token-refresh and approvals, terminate
            // without a response. A refresh disguised as a notification also fails.
            guard envelope["id"] == nil, method != "account/chatgptAuthTokens/refresh" else {
                throw CodexRPCError.serverRequest
            }
            guard envelope["params"] == nil || envelope["params"] as? [String: Any] != nil else {
                throw CodexRPCError.invalidEnvelope
            }
            return Update(outbound: [], completion: nil)
        }
        guard phase != .completed else { throw CodexRPCError.unexpectedResponse }
        // A response coalesced with initialize cannot answer a quota request whose
        // outbound bytes have not yet been returned for the transport to write.
        guard phase != .readingQuota || quotaRequestWasPending else {
            throw CodexRPCError.unexpectedResponse
        }
        guard let id = Self.integer(envelope["id"]),
              id == (phase == .initializing ? 1 : 2) else {
            throw CodexRPCError.unexpectedResponse
        }
        guard (envelope["result"] != nil) != (envelope["error"] != nil),
              envelope["params"] == nil else { throw CodexRPCError.invalidEnvelope }
        if let error = envelope["error"] {
            guard let error = error as? [String: Any],
                  let code = Self.integer(error["code"]),
                  error["message"] is String else { throw CodexRPCError.invalidEnvelope }
            throw CodexRPCError.serverError(code: code)
        }
        guard let result = envelope["result"] as? [String: Any] else {
            throw CodexRPCError.invalidEnvelope
        }
        if phase == .initializing {
            guard let agent = result["userAgent"] as? String, !agent.isEmpty else {
                throw CodexRPCError.invalidHandshake
            }
            phase = .readingQuota
            quotaRequestedAt = receivedAt
            return Update(outbound: [
                Data(#"{"method":"initialized","params":{}}"#.utf8) + Data([10]),
                Data(#"{"id":2,"method":"account/rateLimits/read"}"#.utf8) + Data([10])
            ], completion: nil)
        }
        guard let quotaRequestedAt, quotaRequestedAt <= receivedAt else {
            throw CodexRPCError.invalidTimestamp
        }
        guard result["rateLimits"] != nil || result["rateLimitsByLimitId"] != nil else {
            throw CodexRPCError.invalidQuota
        }
        // The parser sees quota keys only; account fields never leave this function.
        let quota = result.filter { ["rateLimits", "rateLimitsByLimitId"].contains($0.key) }
        let parsed: UsageReading?
        do {
            let data = try JSONSerialization.data(withJSONObject: quota)
            parsed = try UsagePayloadParser.parseCodex(data, receivedAt: receivedAt)
        } catch { throw CodexRPCError.invalidQuota }
        let reading = parsed.map {
            UsageReading(provider: $0.provider, weekly: $0.weekly, session: $0.session,
                         source: $0.source, receivedAt: receivedAt,
                         providerObservedAt: quotaFreshness == .verifiedFreshFetch ? quotaRequestedAt : nil)
        }
        phase = .completed
        return Update(outbound: [], completion: Completion(reading: reading, receivedAt: receivedAt))
    }

    private mutating func fail(_ error: CodexRPCError) -> CodexRPCError {
        phase = .failed
        buffer.removeAll(keepingCapacity: false)
        return error
    }

    private static func valid(_ date: Date) -> Bool {
        let seconds = date.timeIntervalSince1970
        return seconds.isFinite && seconds > 0 && seconds <= 253_402_300_799
    }

    /// JSON booleans bridge to NSNumber; never accept true as request ID 1.
    private static func integer(_ value: Any?) -> Int? {
        guard let number = value as? NSNumber,
              CFGetTypeID(number) != CFBooleanGetTypeID() else { return nil }
        let numeric = number.doubleValue
        guard numeric.isFinite, numeric.rounded() == numeric,
              (-2_147_483_648...2_147_483_647).contains(numeric) else { return nil }
        return Int(numeric)
    }
}

private extension CodexRPCSession.Limits {
    static var defaultLimits: Self {
        Self(lineBytes: 65_536, totalBytes: 262_144, lineCount: 128, validated: ())
    }

    init(lineBytes: Int, totalBytes: Int, lineCount: Int, validated: Void) {
        self.lineBytes = lineBytes
        self.totalBytes = totalBytes
        self.lineCount = lineCount
    }
}

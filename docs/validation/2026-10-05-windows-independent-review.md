# Windows W1/W2a independent source review

Date: 2026-10-05. Reviewer: native GPT agent `windows_review`, separate context,
read-only. This is neither Claude nor Codex CLI review. No reviewer build,
process execution, native CLI/account/settings access or runtime validation.

Intent: offline Thai tray and test-only guarded process/storage primitives.
Simpler approach retained: unavailable statuses, no fixture quota in normal UI,
disabled live connection/installation. Scope is the approved Windows plan;
unsupported tee experiments are runtime gates, not presumed code findings.

## Findings and dispositions

| ID | Finding | Owner fix / source closure | Required regression |
|---|---|---|---|
| R1 major | RPC success returned without graceful exit/drain/trailing-frame/accounting validation | Core added mandatory ICodexTransport.FinishAsync; A must finish before B, B before quota accepted. JsonRpcTransport closes stdin, bounds exit/drains, rejects trailing envelopes/stream failures/nonzero exit or accounting other than active0/total1/terminated0 | Valid and no-LF final response; late bad response/server request/stderr overflow; bad exit/blocked spawn; cleanup on every failure |
| R2 major | Replace race discarded displaced backup without comparing pre-replace identity/bytes | PrivateFiles captures old identity/bytes, compares actual recovery after ReplaceFileW, retains recovery and reports conflict on mismatch | Open reader, ACL, race/retained displaced bytes and partial failure under scratch |
| R3 medium | Valid final JSONL response without LF rejected at EOF | DrainOutput queues the bounded final frame for normal strict-envelope validation | EOF/no-LF and malformed final frames |

Current source references: [provider completion](../../windows/src/AIUsageBar.Core/CodexProvider.cs),
[process cleanup](../../windows/src/AIUsageBar.Core/WindowsProcess.cs),
[private recovery comparison](../../windows/src/AIUsageBar.Core/PrivateFiles.cs).
FinishAsync is line154 in the final source snapshot; PrivateFiles.WriteLocked
is line124 and WindowsProcess.DisposeAsync line111. Review statements refer
to those paths/functions; runtime source changes are described below.

Lead reread each changed path and confirmed the source closure reported by the
reviewer. Access timestamps were excluded from stable-file checks so reads do
not self-invalidate. Transport completion is a required interface method; no
default no-op can accidentally bypass acceptance in an injected transport.
Coordinator now offers bounded ShutdownAsync to await provider cleanup; the
current disconnected tray has no active native provider to join.

Reviewer traced suspended launch/job/handle ownership, parse/config/RPC limits,
snapshot/error-marker flow, sink EOF-before-write ordering, cooldown persistence
and invalidated completions, and tray UI-thread/shutdown. Final source verdict:
**proceed with authorized offline verification**, with no remaining blocking
source finding in the reviewed delta. This does not certify Windows runtime.

Actual offline test/tee/tray evidence and any later source delta are recorded
in [Windows validation](windows-mvp-validation.md), once available. Native C1
remains blocked as documented in [source audit](2026-10-05-windows-codex-audit.md).

## Final runtime-debug source delta

Reviewer performed another read-only pass after runtime debugging: no remaining
material source finding. Checked Finish streamFailed/exit-task race and observed
task cleanup; internal cleanup deadline normalized while external cancellation
remains cancellation; stdin-dispose exceptions cannot skip job cleanup or any
stream disposal; reader retains strict guards with one bounded missing-path
retry; LocalState writes share reader16KiB bound. Lead additionally re-read the
small stderr4096line guard and QA regression. No reviewed guard was relaxed to
make a blocked-create detector claim pass. Final runtime evidence belongs to the
lead/QA report linked above; external race and tee production gates are not closed.

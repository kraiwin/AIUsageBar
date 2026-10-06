# Bounded host plan — actual Codex CLI review

2026-10-05. Owner authorized planning/model review only (DECISIONS28), without
host experiments, provider/native compatibility invocation or real accounts.

## First review: H0/H1 draft, fix-then-proceed

Actual Codex CLI0.160.0, model gpt-6.1-sol/providerOpenAI/reasoningmedium,
exit0; raw68483bytes. Reviewer used the installed global wrapper, self-contained
stdin in C:/tmp, only five named labels: plan, Program.cs, focused CodexTests.cs,
WindowsProcess.cs and C1 study. Three native skeptic clarifications were provided
as an explicitly labeled addendum before launch; no reviewer source/test/probe
edits/actions. This is GPT reviewing GPT via actual CLI, not Claude.

- Prompt: C:/tmp/codex-reviews/prompt-20261005-173203.txt
- Raw: C:/tmp/codex-reviews/raw-20261005-173203.md
- Summary: C:/tmp/codex-reviews/summary-20261005-173203.md

Raw688–718 contains the result (duplicated in raw729–759). References in that
result bind the supplied draft snapshot, not later plan line numbers.

Actual verdict: **fix-then-proceed**. Biggest reason: the pre-SDK bootstrap and
pre-managed ownership protocol are not concrete enough to establish bounded
startup, failure observation and cleanup. Six Major, one Minor, one Info.

Lead assigns IDs below to the actual ordered findings, for disposition tracking.

| ID | Actual finding | Lead disposition |
|---|---|---|
| M1 | Pre-SDK protection is still a proposed dependency; bootstrap/invocation/environment/failure stop missing | Accepted: narrow next phase to H0 metadata only, no SDK/build dependency; concrete existing-PowerShell token/query and scratch contract required |
| M2 | Pre-managed stages/sole keeper lack executable protocol, transfer/registration failure and stage signaling | Accepted: H1 excluded from next approval; not resolved for future ownership implementation |
| M3 | Executable hash/dependencies not bound to role/arguments/cwd/env/handles/lifetime | Accepted: no actor/native executable launch in H0; future actor manifest remains deferred |
| M4 | Environment allowlist/trusted paths/private scratch/preparation/rejection incomplete | Accepted: H0-only parameter/schema/output ownership and native-query error behavior must be concrete; no SDK/observer/actor env inherited contract claimed |
| M5 | Missing deadlines/failure cleanup for stage/registration/READY/held stream drains | Accepted: no actors/stream matrix/jobs in H0; do not invent hard timing guarantees for synchronous host metadata APIs; future H1 requirements deferred |
| M6 | Project/registration/PrivateFiles/paired stream integration and update-read-test pairs unverified | Accepted: no app/test source/project/runner integration in H0; future two-file harness design is deferred, not certified |
| m1 | External-access addendum must be in saved plan; no whole-system observation | Accepted: metadata observations only, no global absence/no-egress/external-access certification |
| i1 | H2 exclusion and authorization sound; update review provenance | Retain H2 exclusion and owner gates. Runtime metadata establishes this invocation is actual CLI; the review's request to distinguish an inline native review is a wording error, not evidence that another CLI review must substitute |

## Revision direction

Lead asked architect for a smaller H0-only proposal: future reviewed PowerShell
metadata script in ignored workspace build/HostPreflight, no SDK or app/harness
source changes, no provider/actor invocation or settings/setup. H1 and H2 remain
blocked and excluded. Original H1 concerns are deferred, not reported as fixed
or passing. The revised saved plan requires actual CLI delta review.

No reviewed-plan owner approval, script implementation, test/build/probe or
account/system action follows from this report. Plan/review preparation only.

## Invalid delta input — 17:47:52

Attempt174752 used relative repository reads from C:/tmp; reads failed and
prompt contained labels without bodies. Reviewer stopped the invocation and
kept it INVALID/no accepted plan verdict. Outer abort exit1; raw contained no
exec/tool/functions/MCP/command-action markers, only the missing-input response.
Any rework wording there refers to missing input, not the H0 plan. Corrected
attempt used absolute paths, fail-fast reads and full-prompt checks.

## Actual H0 delta — 17:48:44

CLI0.160.0/gpt-6.1-sol/providerOpenAI/reasoningmedium, exit0, raw36614bytes.
Self-contained23015characters with complete H0 plan, first review report and
C1 source-study excerpts. Three distinct file labels; no reviewer source edits,
test/build/probe or host metadata query. Result raw303–327, duplicated349–373.

- Prompt: C:/tmp/codex-reviews/prompt-20261005-174844.txt
- Raw: C:/tmp/codex-reviews/raw-20261005-174844.md
- Summary: C:/tmp/codex-reviews/summary-20261005-174844.md

Actual verdict **fix-then-proceed**,2Major/2Minor. Biggest reason: artifact-root
preparation remains underspecified as the first filesystem mutation. The review
explicitly requires source reread/parser/self-gate before separately approved
execution and distinguishes H1 deferral from closure.

| ID | Actual finding | Accepted plan correction |
|---|---|---|
| D-M1 major | Concrete artifact parent/drive/exclusive creation/revalidation contract missing | Literal C: fixedNTFS; workspace/build must exist; only HostPreflight/new run may be created; exclusive native creation, parent/leaf handle validation and exact cleanup |
| D-M2 major | Schema names/types/enums/error/precedence/budget/early failures unspecified | Exact complete schema, not-checked/unknown/absent-by-parent distinctions, terminal/cleanup precedence, suppressed diagnostics, soft10000ms/max64queries/max32components/max240chars, H1 thresholds unchanged |
| D-m1 minor | Process versus effective impersonation token and integrity SID structure unclear | Reject any impersonation/unknown OpenThreadToken result; validate integrity SID/authority/RID before threshold |
| D-m2 minor | Immediate reviewed-hash equality and compiler side-effect qualification missing | Rehash with regular/single-link read-only pinned leaf immediately before call; noOutputAssembly/owned outputs not no-temp/process guarantee |

All accepted corrections are in the saved plan. Original M4 is narrowed, not
blanket elimination of prep/compiler effects. Final focused actual CLI closure
is pending; no implementation/runtime success or owner gates inferred.

## Focused review — 17:58:16, additional closure required

Actual CLI0.160.0/gpt-6.1-sol/providerOpenAI/reasoningmedium, exit0,
raw31619bytes; self-contained24044characters/two labels. Result raw322–345,
duplicated354–377. Actual verdict **fix-then-proceed**; biggest reason: artifact
lifecycle still lacks a complete concurrency-safe ownership/cleanup contract.

- Prompt: C:/tmp/codex-reviews/prompt-20261005-175816.txt
- Raw: C:/tmp/codex-reviews/raw-20261005-175816.md
- Summary: C:/tmp/codex-reviews/summary-20261005-175816.md

D-m1/D-m2 closed IN PLAN. D-M1 not closed: directory-create-to-pin substitution
window, retention of handles through invocation/cleanup and identity-preserving
deletion were incomplete. D-M2 not closed: cleanup precedence for already-
inconclusive outcomes and cleanup apiError needed definition. Reviewer also
corrected lead briefing's17-field count: saved schema has14; lead corrected the
briefing, did not invent3newfields or attribute17to the prior actual result.

Architect then supplied a smaller amendment: REMOVE H0 filesystem mutation
entirely. Future execution is one immutable reviewed/UTF8-hashed command string,
ParseInput/AST/static inspection, same-string ScriptBlock.Create/invoke once in
the same tool session. No code file/dir creation/read/hash/delete/pin remains.
D-M1 is eliminated from this scope, NOT proven safe for future file workflows.
D-M2 now requires every cleanup release, first cleanup failure overriding ALL
prior outcomes/APIerror, one complete14-field record emitted AFTER cleanup when
control returns. H1/H2 stay deferred; actual closure review of this scope pending.

## Final in-memory H0 closure — 18:06:38, PROCEED for PLAN

Actual CLI0.160.0/gpt-6.1-sol/providerOpenAI/reasoningmedium, exit0,
raw28077bytes; self-contained23000characters/two labels. Result raw315–327,
duplicated332–344. No source edits, host queries, tests/probes or account actions.

- Prompt: C:/tmp/codex-reviews/prompt-20261005-180638.txt
- Raw: C:/tmp/codex-reviews/raw-20261005-180638.md
- Summary: C:/tmp/codex-reviews/summary-20261005-180638.md

Exact verdict: **proceed**. Exact single biggest reason:
“Removing filesystem artifacts eliminates D-M1’s lifecycle issue from H0 scope,
while the revised output contract fully resolves D-M2’s schema and cleanup
precedence gaps.” No remaining material gap apparent in supplied H0 text.

| Finding | Actual final closure |
|---|---|
| D-M1 | Eliminated from scope; no filesystem artifact/mutation lifecycle, NOT proof of concurrency-safe file workflows |
| D-M2 | Closed IN PLAN;14 fields, every release attempted, first cleanup failure overrides ALL outcomes/APIerrors, one final record after cleanup when control returns |
| D-m1 | Closed IN PLAN; token/effective-context/impersonation/SID checks, not runtime-verified |
| D-m2 | Closed IN PLAN; exact immutable source/ParseInput/AST/UTF8hash/immediate equality/same-text invocation plus compiler-effect caveat |

Reviewer did not verify external links. Lead read raw result, reread saved plan,
used primary API documentation, checked Markdown/scope/source hashes. This
closes H0 planning review only; no owner plan/dispatch or runtime approval.
H1/H2 remain unresolved/excluded and C1/C5 blocked. No command has been authored
or run; future source/parser/self-gate precedes separately approved execution.

# Windows blocker amendment — actual Codex plan review

Date: 2026-10-05. Reviewer: actual OpenAI Codex CLI0.160.0 via global wrapper,
model gpt-6.1-sol/providerOpenAI/reasoningmedium as reported. This is GPT reviewing
GPT in a distinct CLI context, not Claude. CLIexit0; raw90065bytes. Self-contained
stdin in C:/tmp; four-file cap; no reviewer source edits/test/runtime probe.
Native model review was routed for plan-gate, not authorization for app-server,
quota/account/statusline/native compatibility or settings operations.

- Prompt: `C:/tmp/codex-reviews/prompt-20261005-160331.txt`
- Raw: `C:/tmp/codex-reviews/raw-20261005-160331.md`
- Summary: `C:/tmp/codex-reviews/summary-20261005-160331.md`

Input: full amendment plan, PrivateFiles.cs, Core.csproj, ClaudeTests.cs lines1–236
and native MoveFileExW declaration at418, with exact numbered source labels.
Current runner/helpers/SDK source were not all supplied; findings explicitly
retain those as integration dependencies. Line citations bind input snapshot.
Raw also includes echoed prompt/runtime headers/hook status; the review block
below is preserved verbatim from raw656–683 region. No raw auth/config is copied.

Verdict **fix-then-proceed**,1major/3minor/2info/human-verify0. Lead accepted all
findings and corrected the plan; code/tests remain unchanged pending gates.

## Verbatim reviewer result

The intent is to prove C6 conflict detection and recovery retention deterministically; the smaller alternative is to leave production unchanged and retain the stress experiment, but that cannot satisfy the stated deterministic acceptance requirement.

**Major — must fix before implementation/merge**

1. **Specify a recovery oracle that proves the original pathname survives, not merely an open handle.** Record the newly created recovery’s exact path, identity and complete bytes; close inspection handles, perform the later public publication, then reopen that exact path and repeat identity, bytes and security checks. Require exactly one new recovery for each controlled race. This prevents a false pass from reading an already-open file after its directory entry was deleted or replaced.  
   **Evidence:** `docs/plans/2026-10-05-windows-blocker-amendment.plan.md:109–111` requires retention but leaves the handle lifecycle and recovery selection unspecified; `windows/src/AIUsageBar.Core/PrivateFiles.cs:70` opens readers with sharing value `7`, including delete sharing; `windows/src/AIUsageBar.Core/PrivateFiles.cs:156` performs pathname-based cleanup. The existing stress test enumerates recoveries before its later publication and does not recheck them afterward (`windows/tests/AIUsageBar.Tests/ClaudeTests.cs:220–229`).

**Minor**

2. **Make negative controls cover both publication branches and instance isolation.** Specify:
   - Public construction: first creation, then existing-target replacement, both successful.
   - Internal construction with a counting no-op callback: zero calls on first creation, exactly one on replacement.
   - A separate public instance: no effect from the hooked instance.

   These distinguish race detection from a seam that fires too broadly or itself causes conflicts. No serialization CRUD field is introduced, but creation, replacement, reading and recovery deletion still need paired checks.  
   **Evidence:** `docs/plans/2026-10-05-windows-blocker-amendment.plan.md:81–87,104–105`; the actual branches split at `windows/src/AIUsageBar.Core/PrivateFiles.cs:132–140`, with verification and deletion at `:149–156`.

3. **Turn the same-ID and post-conflict expectations into explicit assertions.** Require A/E/N to be distinguishable, successful overwrite and flush, equal length, and unchanged volume/file-index identity before versus after the edit. For both successful controlled replacement traces, require target bytes N after `publish-conflict`; do not accept arbitrary target contents under “may already contain N.” This demonstrates the byte comparator independently and records the actual non-rollback outcome.  
   **Evidence:** `docs/plans/2026-10-05-windows-blocker-amendment.plan.md:97–114`; `windows/src/AIUsageBar.Core/PrivateFiles.cs:140–147` replaces first, then rejects displaced identity or bytes, before reaching published-output verification.

4. **Make security assertions explicit for the new evidence.** Check the retained recovery and published target through validated handles, including protected DACL and expected owner/access rules; repeat for the retained recovery after later publication. Reusing the protected staging pattern is appropriate, but “mirror” should become a concrete acceptance condition.  
   **Evidence:** `docs/plans/2026-10-05-windows-blocker-amendment.plan.md:106–111`; handle validation checks reparse/directory attributes, single-link status and security at `windows/src/AIUsageBar.Core/PrivateFiles.cs:75–79`. The existing semantic DACL comparison covers a normal replacement, not these new race recoveries (`windows/tests/AIUsageBar.Tests/ClaudeTests.cs:79–93`).

**Info — evidence and scope limitations**

5. **Label integration dependencies as unverified and require build/run evidence before claiming closure.** Inline source confirms the current public constructor, reader-disposal boundary, actual Win32 calls and comparison order. It does not confirm the future callback, friend declaration, test assembly name, runner filtering or safe failure output. The core project currently contains only its target framework; SDK support is an external lead fact. Require the proposed build and nonzero selected-case count to resolve integration uncertainty.  
   **Evidence:** `windows/src/AIUsageBar.Core/AIUsageBar.Core.csproj:1–3`; `docs/plans/2026-10-05-windows-blocker-amendment.plan.md:26–29,88–89,128–135`. The supplied `ClaudeTests.cs` is explicitly partial; unseen runner/helpers/native declarations cannot be certified here.

6. **Preserve the bounded claim and authorization gates.** Describe C6 as detection plus retained evidence, without CAS, rollback, comprehensive partial-failure or power-loss guarantees. Keep C1/C5 as unresolved choices, with every descendant ≤2s and caller exit/stdout/stderr EOF ≤500ms unchanged. Treat referenced native sources, links and environment observations as unverified in this audit; retain 80/0/0 as historical. No tests were run here, and this verdict grants no source authorization.  
   **Evidence:** `docs/plans/2026-10-05-windows-blocker-amendment.plan.md:65–70,91–92,113–115,133–136,169–188,207–209`.

**Verdict: fix-then-proceed — the retention oracle must explicitly prove survival of the original recovery pathname after later publication.**

## Lead dispositions

See [plan dispositions](2026-10-05-windows-blocker-amendment.plan.md). Main gap:
close all inspection handles and reopen exactly the same recovery pathname
AFTER later ordinary publication. Other corrections make controls/identity/
post-conflict target/security/nonempty focused selection explicit. C1/C5 remain
choices, not source-ready solutions. Actual delta review is pending; do not turn
plan fixes into new runtime success or unconditional reviewer proceed.

## Delta review — 16:11 input

Actual Codex CLI0.160.0/gpt-6.1-sol, exit0, raw50081bytes. Closed all six prior
findings **in PLAN**, verdict still fix-then-proceed:0major/1minor/1info.
No source/test/runtime/native/account authorization or evidence.

- Prompt: `C:/tmp/codex-reviews/prompt-20261005-161125.txt`
- Raw: `C:/tmp/codex-reviews/raw-20261005-161125.md`
- Summary: `C:/tmp/codex-reviews/summary-20261005-161125.md`

Scope: corrected plan Task2–4/dispositions/header, prior actual review, current
PrivateFiles.cs64–174 and Core.csproj, exact numbered labels. Verbatim final
review result below; citations refer to that snapshot.

The intent is deterministic proof of C6 conflict detection and retention of displaced evidence. The smaller alternative—keeping production unchanged and relying on stress tests—cannot meet that deterministic requirement. The per-instance boundary callback remains a suitably narrow change.

References below use the supplied original line numbers; **plan** means `2026-10-05-windows-blocker-amendment.plan.md`.

| Prior finding | Closure assessment |
|---|---|
| **M1** | **Closed in PLAN.** Exactly one new recovery pathname must come from set difference; all inspection handles must close before later publication; that identical pathname must then reopen with matching identity, complete bytes and security (**plan:111–122,230**). This addresses orphan-handle false positives. |
| **m1** | **Closed in PLAN.** Public create/replace, counting no-op create=0/replace=1, separate public-instance isolation, successful operations and cleanup are required (**plan:104–108,231**). |
| **m2** | **Closed in PLAN.** Distinct A/E/N, equal-length mutation, successful flush, unchanged identity, and target N after both controlled conflicts are explicit (**plan:97–103,111–117,124–127,232**). |
| **m3** | **Closed in PLAN.** Recovery/target checks explicitly cover protected DACL, current owner, expected user+SYSTEM FullControl and regular/single-link semantics, with recovery security repeated after reopening (**plan:118–122,233**). |
| **i1** | **Closed in PLAN.** Future callback/friend/test/filter integration remains unverified; focused selection must be nonempty and execute all deterministic cases, followed by mandatory-suite evidence. Historical 80/0/0 is not current proof (**plan:145–152,234**). SDK support remains a supplied lead fact. |
| **i2** | **Closed in PLAN.** Detection and retention are distinguished from CAS/rollback and comprehensive failure/durability guarantees (**plan:91–92,124–127**). Every descendant ≤2s and caller exit/stdout/stderr EOF ≤500ms remain strict (**plan:63–70**); source authorization remains pending (**plan:237–238**). |

**Major:** None identified within this delta.

**Minor — complete the displaced-evidence oracle.** Require the *initial* recovery identity and complete bytes to match the known adversary result:

- External replacement: recovery identity must equal recorded E identity, and recovery bytes must equal E.
- Same-ID edit: recovery identity must equal the preserved A identity, and recovery bytes must equal the known edited bytes.

**Why:** The correction proves that one newly observed recovery survives unchanged, but does not explicitly prove that it contains the displaced adversary version. Recording its identity/bytes and comparing those observations after publication can preserve an initially wrong baseline.

**Exact evidence:** **plan:98** records A/E identities; **plan:115** records recovery identity/bytes; **plan:121–122** compares the reopened recovery against that recorded baseline. None explicitly connects the recovery to expected E. The production comparison checks displaced identity against original A and can throw before reading displaced bytes (**PrivateFiles.cs:144–147**), so the external-replacement test needs its own complete-content assertion. Add these equalities alongside **plan:115–117**.

**Info:** All six closures above concern acceptance requirements only. Callback, friend declaration and test integration remain unverified until implementation. Native model review is separate from any native quota/account compatibility probe; this review supplies neither compatibility evidence nor source authorization.

**Verdict: fix-then-proceed — explicitly tie the retained recovery to the expected displaced adversary identity and bytes before using it as the survival baseline.**

## Delta lead disposition

m4 accepted: initial recovery must independently equal known displaced adversary:
external E identity/full bytes; same-ID preserved A identity/full edited bytes.
Plan amended explicitly BEFORE recording survival baseline. This prevents a
wrong initial baseline from merely remaining wrong-but-unchanged.
I3 retained: closure is plan acceptance, future implementation remains unverified.
A final focused actualCLI closure check is pending for only this corrected oracle.

## Final focused closure — PROCEED for PLAN

Actual CodexCLI0.160.0/gpt-6.1-sol, providerOpenAI/reasoningmedium, exit0,
raw14878bytes.79-line self-contained input: corrected C6 plan94–134, prior actual
minor334–345, PrivateFiles140–156. No remaining material finding; m4 closed in PLAN.

- Prompt: `C:/tmp/codex-reviews/prompt-20261005-161541.txt`
- Raw: `C:/tmp/codex-reviews/raw-20261005-161541.md`
- Summary: `C:/tmp/codex-reviews/summary-20261005-161541.md`

Verbatim final reviewer result:

**1. m4 closed in PLAN.** `docs/plans/2026-10-05-windows-blocker-amendment.plan.md:116–120` explicitly requires initial recovery equality **before** adopting a survival baseline:

- External replacement: recorded staged E volume/file-index identity **and complete E bytes**.
- Same-ID edit: preserved A volume/file-index identity **and complete edited bytes**.
- Expected identity/content captured independently beforehand; arbitrary observed recovery values cannot serve as the oracle.

This directly satisfies the previous CLI minor at `C:/tmp/codex-reviews/raw-20261005-161125.md:334–341`.

**2. No material remaining gap introduced by this correction.** The complete-content requirement supplies the independent test assertion needed because `windows/src/AIUsageBar.Core/PrivateFiles.cs:145` can throw on identity mismatch before reading displaced bytes. Plan lines 125–127 then separately require reopening the exact recovery pathname and verifying its identity, full bytes and security after later publication. The initial correctness oracle and subsequent survival check are now explicitly connected.

**3. Verdict: proceed — m4’s missing initial displaced-evidence oracle is explicit and independently grounded.** This is plan closure only; callback, friend declaration and test integration remain future implementation/runtime evidence. The prior six closures and strict C1/C5 gates remain unchanged. C6 remains detection plus retention, with target N required for the controlled successful replacements only. This verdict grants no source authorization.

Lead reread the exact expected-adversary equality, closed-handle pathname survival,
negative controls, targetN and semantic security assertions against all findings.
Plan review is complete; owner source-plan and second dispatch gates remain.
No source/test/runtime proof or C1/C5 closure follows from this model verdict.

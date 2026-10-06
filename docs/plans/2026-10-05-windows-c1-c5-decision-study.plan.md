# Plan: C1/C5 decision study — environment and caller ownership

2026-10-05. Architect supplied the grounded decision design; lead saves the
combined plan. Owner authorized read-only team study (DECISIONS27), not setup,
native execution or source implementation. This is a decision proposal, not an
implementation-ready plan or an actual Codex CLI-reviewed source dispatch.

## Summary

Recommend assessing availability of an owner-provisioned disposable Windows 11
x64 guest for C1 while retaining the working offline edition. Keep C5 production
bridge disabled: neither an inner native supervisor nor a warm broker currently
establishes ownership before the outer shell creates its children. No compiler,
VM, native supervisor, broker or prototype implementation is selected here.

## Patterns to Mirror

| Category | Source | Pattern and limitation |
|---|---|---|
| Source binding | ../validation/2026-10-05-windows-codex-audit.md | Pin source/binary/policy; a source commit is not executable identity |
| C1 effects | ../validation/2026-10-05-windows-c1-isolation-study.md | Pre-RPC auth/cloud/telemetry/disk map; unresolved transitives remain explicit |
| Recorded failures | ../validation/windows-mvp-validation.md | Preserve failed tee evidence and observe descendants before harness cleanup |
| Owned-child launch | ../../windows/src/AIUsageBar.Core/WindowsProcess.cs:64 | Suspended launch/assignment/handles; does not control upstream creator |
| Outer/inner caller | ../../windows/tests/AIUsageBar.Tests/ClaudeTests.cs:552 | Distinct native redirector and MSYS host/cancellation endpoints |
| C5 limits | ../validation/2026-10-05-windows-c5-bridge-study.md | Separate architecture inference from runtime proof |

## Files to Change

Current phase is documentation only. Future app/test/shell source manifests
must be defined and reviewed after the environment/caller branch is concrete.

| File | Action | Why |
|---|---|---|
| docs/plans/2026-10-05-windows-c1-c5-decision-study.plan.md | CREATE | Combined options, recommendation and stop rules; lead owns |
| docs/validation/2026-10-05-windows-c1-isolation-study.md | CREATE | C1 author's source/isolation study |
| docs/validation/2026-10-05-windows-c5-bridge-study.md | CREATE | Architect's caller/bridge study |
| docs/STATUS.md | UPDATE | Actual research result and remaining gates |
| MEMORY.md | UPDATE | Latest checkpoint |
| CHANGELOG.md | UPDATE | Material evidence/decision work |
| docs/DECISIONS.md | UPDATE | Received owner instructions only |

No lab configuration, context probe, harness source or installation artifact
is created by this decision proposal. A future context-contract record belongs
to a separately selected phase, not to this phase's completed-file claim.

## Tasks

### Task 1: Record completed read-only research

- Action: save C1 effects/isolation and C5 admission/stream/keeper findings,
  including independent skeptic dispositions.
- Mirror: pinned source and historical validation rather than new runtime claims.
- Validate: re-read both reports and source citations, check links and preserve
  source hashes. C6's84/0/0 remains its prior result, not a new research test run.

### Task 2: Select the C1 environment branch

- Action: recommend assessing an already-available disposable Win11x64 guest.
  Alternative: keep offline support and defer native verification. Guest
  availability remains unknown until the owner supplies it; no silence implies
  VM provisioning approval.
- Mirror: source-grounded known-folder effects. Use synthetic actual ProgramData,
  user/profile/project ancestry, local policy and auth absence; no owner data.
- Validate before any future guest launch: exact virtualization product and
  guest reset/image, sanitized transfer bundle, binary provenance/hash, network
  disabled at VM boundary, no owner shares, clipboard/device redirections off,
  and explicit observation/fixture/RPC manifest. Setup is a separate approval.

### Task 3: Preserve RPC and evidence boundaries

- Action: guest planning inherits W2b's no quota/account/auth RPC restriction.
  Default future inventory is limited to authorized config/registry reads;
  test-only MCP canary and its launch contract require the prescribed separate
  prerequisites. An expected unauthenticated quota error is still a quota RPC
  and is not authorized. No model turns/login/logout/email mutations.
- Mirror: config/registry assertion guards cannot undo bootstrap effects.
- Validate: requests explicitly allowlisted before dispatch; attempts and denied
  egress distinguished from successful traffic; guest proof cannot certify host
  known folders, credentials or authenticated usage fields.

### Task 4: Establish C5 upstream caller contract before choosing code

- Action: identify the exact creator, executable paths, creation flags, root/MSYS
  cancellation targets, inherited handles, stdin/EOF behavior and normal
  background-output semantics for the pinned context. Current official docs
  cannot fill all these cells; actual-context observation is separately scoped.
- Mirror: known synthetic R:bin/bash -> S:usr/bash -> later admission chain.
- Validate: ownership that starts after R creates S is insufficient for strict
  pre-connect cleanup. Keep bridge disabled if this gap cannot be closed.

### Task 5: Bound any later synthetic proof

- Action: only after a concrete reviewed source plan and owner gates, use
  controlled pre-connect/handshake/pre-keeper/pre-READY/pre-resume/CLR-stall
  stages; positive upstream-owned and negative inner-only controls.
- Mirror: existing suspended creation and safe observation, not a harness job
  incorrectly credited as production root-death control.
- Validate: all supported descendants <=2s; paired caller exit/stdout/stderr EOF
  overhead <=500ms; exact streams/exit/single original execution. Measure before
  observer-job cleanup. Stop if a synthetic pass depends on upstream controls
  actual Claude does not supply. No arbitrary WMI/service/task launch guarantee.

### Task 6: Defer toolchain and deployment selection

- Action: existing .NET/PInvoke may model a synthetic creator; native C/Win32 or
  NativeAOT require separate verified setup/publish/deployment decisions.
- Mirror: no third-party dependency/toolchain addition in the current app.
- Validate: a compiler or already-running broker does not fix missing upstream
  ownership. Preserve strict gates without silently restricting originals.

## Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Guest evidence treated as host/account certification | High | Bind exact guest/binary/policy and separate later authenticated decision |
| Inner cleanup mistaken for whole-tree ownership | High | Explicit upstream admission stages and both cancellation endpoints |
| Background output truncated or exit delayed | High | Define normal-completion semantics before claiming compatibility |
| Keeper leak or premature job closure | Medium | Full handle ledger, exact held root and per-invocation lifecycle |
| Fallback consumes input or runs original twice | Medium | Paired input/streams/exit/single-execution proof after admission |
| Research interpreted as source/setup permission | High | DECISIONS27 scope; future concrete manifests and owner gates |

## Independent skeptic disposition

Native GPT in separate context reviewed the two bounded studies read-only; this
is not actual Codex CLI or Claude review. C1 finding accepted: expected no-auth
usage-error probing exceeds W2b; quota/auth/account routes stay source-only with
separate future authorization. C5 review found no actionable unsupported claim
in the bounded study. Final readback of the updated C1 report and this combined
decision plan closed that finding; no remaining actionable findings. Lead
re-read all three documents, validated links and verified26source/project/config/
shell hashes unchanged. C1/C5 remain blocked; no source dispatch follows. Owner
guest availability remains unknown; an optional question was sent without
assuming an answer.

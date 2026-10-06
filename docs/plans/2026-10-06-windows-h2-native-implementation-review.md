# Native H2 implementation plan — actual CLI review, fixes required

2026-10-06 · DECISIONS37 authorized one actual model-review invocation and
reporting, without another research/fix segment. No source/build/H0/native H2
execution was authorized. The reviewed plan is not ready for source dispatch.

## Actual result

**fix-then-proceed — 5 Major, 1 Minor, 1 Info.** The tool completed successfully
(exit0); that does not mean the PLAN passed. The reviewer recommends retaining
fake-only operation until these lifecycle/cleanup contracts are corrected.

| ID | Severity / canonical raw line | Actual finding | Accepted disposition |
|---|---|---|---|
| M1 | Major2191 | FinishAsync shares one2s deadline between exit and drain and links its caller token; planned independent budgets do not match unchanged transport | Accept; define budgets consistent with actual existing transport or explicitly amend source scope. NOT fixed in this review-only segment |
| M2 | Major2193 | ONE snapshot wording conflicts with live accounting in Finish/Dispose | Accept; separate one copied terminal-evidence snapshot from ongoing live queries; exact success/failure capture points required. NOT fixed |
| M3 | Major2195 | Prepare/run fixture creation, frozen identities, handle lifetime and stale fixture recovery unclear | Accept; choose and specify complete producer/retention/reacquisition/cleanup lifecycle, distinguishing frozen versus runtime IDs. NOT fixed |
| M4 | Major2197 | Manifest hashes recorded but executing consumer/code/source not compared before admission | Accept; exact artifact identities/hash comparisons at consume, mismatch consumes attempt and prevents creation. NOT fixed |
| M5 | Major2199 | Existing startup cleanup short-circuits original-handle wait if TerminateProcess fails | Accept; independent signal wait even on termination failure, separate outcomes and available pre-close accounting; required fake case. NOT fixed |
| m1 | Minor2201 | Acceptable unknown access coverage and execution inconclusive both use inconclusive label/exit semantics | Accept; separate status fields/precedence/serializer and exit cases. NOT fixed |
| i1 | Info2203 | Supported namespace assumptions, narrow-rights results and reviewer acceptance need a durable acceptance record before manifest preparation | Accept; explicit limited-proof record and bypass STOP gate in later amendment. Not an all-tag/ABA/section/isolation certificate |

The recorded verdict's reason is that the frozen lifecycle and cleanup/accounting
contracts do not yet map consistently to the stated source scope. These are PLAN
gaps, not evidence that a native attempt occurred or failed.

## Immutable input and scope

`C:/tmp/aiusagebar-h2-native-implementation-input.md`:136744UTF8bytes,
2167physical lines/five labels, SHA256
`a935095d6dfa8beab564219e5992781098b74a0f0f6098baa8a8b5e1f45b22c3`.
Split-line count2168 includes a trailing empty line. Companion input .json binds:

- Plan snapshot SHA256
  `bf0c4f7e8faa115e82c04fb48d125b13b34c7d4f8abe1a7617e6fb0aeaa2ced0`.
- Fixture contract SHA256
  `058865fe4c2fa621d012b11454883bb1ad6e6bdd6af0395ca1c7659814c62f65`.
- Identity contract SHA256
  `6a43a1388ac9dffb886932088c182a930cfe1b15bbb52a564b6c43b1615709b0`.
- Selected current launch/discovery/transport/fake/runner excerpts.
- Selected pinned fingerprint/arg0/dotenv/installation/state producers and the
  historical H0 reference as READ-ONLY TEXT, never invoked.

Prompt requested outsider intent/simpler alternative, actual-code assumptions,
producer/consumer/cleanup/test gaps and a calibrated verdict. No tools/files/
browsing/commands/repository exploration/edits were requested. The earlier
identity-report two-argument API sketch was explicitly superseded by the plan's
required checkpoint callback. Unexcerpted claims and22 reported source hashes
were not independently verified by this CLI review; lead/source readers checked
them separately. Historical110/110 is fake preparation evidence only.

## Actual CLI provenance

ONE invocation: `20261006-134939-h2-native-implementation-13644`.
Installed Git Bash/global codex-run.sh task with pinned bin/codex route;
CLI0.160.0, model gpt-6.1-sol, providerOpenAI, effortmedium, timeout600.
Execution cwd C:/tmp, no --repo, snapshot_repo=none, exit0.
Approvalnever/sandboxdanger-full-access is the recorded model-review header;
no-tools restrictions are prompt instructions, not mechanical isolation.

The router contributed identity design and is NOT an independent native GPT
reviewer. Findings/verdict above come from the actual CLI's separate model
context; no contributor-generated verdict substituted. Wrapper version metadata
and codex exec belong to the authorized MODEL REVIEW route, not a native H2 or
provider quota experiment. No credential values were put into the packet;
the existing model workflow can use its configured credentials.

External evidence in `C:/tmp/codex-tasks/`, same basename:
`raw-`, `prompt-`, `last-`, `summary-` .md and `exit-` .txt.
Wrapper log: `C:/tmp/codex-reviews/wrapper-h2-native-implementation-20261006.log`.

| Evidence | Bytes / SHA256 |
|---|---|
| Generated prompt |136808 /2498330d07dd2e1bd1e8da2d2d1979d57eba403624676ae9ef577e32a6987bb7 |
| Raw |147832 /039f9c02f64b841801bc2c3b2f408a7c67e54993fcf487e0fd4fc8adc894922b |
| Last response |5319 /b061f5454f8eef8b425c4fe501a66f6c5e2ed1b367ecec8e9b73b6f458b03e50 |

Lead read the actual header, full last response, exit record and canonical
raw2187–2207. Counts2189; findings2191/2193/2195/2197/2199/2201/2203;
verdict2207. Duplicate output2212–2232 is not another invocation.
Input SHA remained unchanged; strict anchored tool/action marker check0.

## Checkpoint and next bounded work

This review/report segment is complete. No delta invocation, source changes,
SDK/build/tests, H0, native provider, new study, install/settings/commit/push.
The26-entry application-source baseline remains unchanged. Native admission
and source dispatch stay STOP; earlier readiness PROCEED does not approve this
implementation draft.

Next segment: correct these named plan contracts using the already supplied
code/evidence, self-gate the amendment, then actual delta review. Do not reopen
unrelated source research or start implementation from this report.

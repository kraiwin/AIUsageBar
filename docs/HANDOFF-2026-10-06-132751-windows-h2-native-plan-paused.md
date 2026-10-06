# H2 native implementation planning — owner paused

Owner explicitly requested a break after noticing more than50 minutes elapsed.
Stop planning/research/model review/implementation until a new resume instruction.
All three subagents are completed; no live team work remains. No unified exec
or model-review invocation was left running by this planning turn.

## Completed in this round

- Actual architect returned concrete eight-source-file plan; lead saved draft at
  docs/plans/2026-10-06-windows-h2-native-implementation.plan.md.
- Source fixture contract: docs/validation/2026-10-06-windows-h2-native-fixture-contract.md,
  latestSHA058865fe4c2fa621d012b11454883bb1ad6e6bdd6af0395ca1c7659814c62f65.
  Source author verified22 records; lead previously checked21 and golden hashes;
  final22-record lead readback remains pending.
- Identity contract: docs/validation/2026-10-06-windows-h2-native-identity-contract.md,
  SHA6a43a1388ac9dffb886932088c182a930cfe1b15bbb52a564b6c43b1615709b0.
- Findings incorporated: dotenv before app-server; nonempty comment-only inputs;
  fixed DB/alias classes; attribute-sharing limitations; oldest metadata query
  freshness; handle-based cleanup; no atomic image/namespace/isolation claim.
- Recovered historical H0 reference text C:/tmp/aiusagebar-h2-h0-reference.txt
  SHA3a9c9f0b7fbe35a372bfebbfc14c13123b0bb1141cdf54b2065395219768c496.
  Not invoked. Initial export quoting failed parser before execution; safe
  literal export matched old hash. No .ps1 artifact was created.

## Remaining before plan delivery

1. Merge architect's final required callback API into draft:
   enum NativeLaunchCheckpoint { BeforeCreate, BeforeResume };
   internal WindowsProcess.Start(ProcessLaunch,NativeLaunchBinding,
   Action<NativeLaunchCheckpoint> validateAdmission).
   Required synchronous per-call callback captures immutable H0 ticket/linked
   token, checks oldest observation age/thread/cancellation at both checkpoints;
   throw uses exact-owned cleanup, count0 beforeCreate or suspended count1 beforeResume.
2. Finalize known sentinel layout/case-query/deletion protocol and review all
   latest contract changes. Janitor has NO prefix filter: regular sentinel is
   skipped because path.is_dir()==false before lock/open/delete.
3. Lead self-gate full draft/contracts/22source hashes/links, then assemble frozen
   bounded actual Codex CLI PLAN-review packet. No CLI implementation-plan review
   has run yet. Earlier readiness PROCEED does not approve this draft.
4. Resolve actual review findings and present concrete plan for owner PLAN AND
   dispatch approval; native H0/one-child run approval remains separate.

## Scope and next-session discipline

No app-source/build/test/freshH0/native invocation/account-contents/install/
settings/commit/push in this turn. Historical fake110/110 remains the prior
preparation result, not new evidence. Draft is NOT implementation-ready or
review-approved. C1/C5 remain blocked. Do not implement from this handoff.

On resume, use a short bounded segment: merge final draft corrections and
self-gate only, then report before starting another lengthy review segment.
The owner has not supplied an exact new time budget; do not invent one as an
approved constraint. Avoid reopening completed source studies without a named
remaining issue. Existing work is uncommitted; preserve all edits.

At pause: application baseline 26 entries; changed=0.

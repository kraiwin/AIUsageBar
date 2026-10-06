# Windows H0 preflight — stopped inconclusive

Historical first attempt. Later owner-authorized [guard fix and replay](2026-10-06-windows-h0-guard-fix.md)
identified TokenElevation capacity and reached metadata-clear. Preserve this
original failed result; it is not current replay evidence.

2026-10-06. Owner instructed “ลุย h0” (DECISIONS29), authorizing the reviewed
[H0-only plan](../plans/2026-10-05-windows-host-bounded-test.plan.md).
Exactly one H0 invocation was performed. No retry, elevation or workaround.

## Source and self-gate

QA authored one ASCII in-memory PowerShell block with inline C# metadata
interop. Independent native GPT reviewed it read-only: no actionable static
ABI/resource/scope findings; this was not actual CLI or Claude code review.
Lead reread the complete command, checked PowerShell AST/parser and the
14-field whitelist before execution.

- Source:19268 characters,411 lines, no code file saved.
- UTF-8 SHA256:`31bcb21714c37984ac90a05382a61784b33d50d3ea56fdab8734b9752ee54650`.
- Parser:0 errors;3 command AST nodes (outer scope, imported-command lookup,
  invocation of the already-imported Add-Type cmdlet).
- Source transported as Base64 data in memory; exact UTF-8 digest recomputed
  immediately before same-text ScriptBlock.Create/invocation.
- Tool-provided PowerShell context used login=false; no nested shell/profile
  load, SDK/build or provider CLI invocation in the block.
- Cooperative10s budget includes inline compilation; it is not a watchdog.
  Synchronous compiler/OS calls and unobserved host internal effects are not
  certified absent or hard-bounded.

## Actual result

Execution tool exit0; wall time1.5037918s. Exit0 means the command returned its
structured record, **not that the metadata gate passed**. One JSON record,
exactly14 fields, validated by the lead:

```json
{
  "schemaVersion": 1,
  "phase": "H0",
  "privilegeClear": false,
  "knownFolderResolved": false,
  "localDriveClear": false,
  "ancestryClear": false,
  "configState": "not-checked",
  "requirementsState": "not-checked",
  "outcome": "inconclusive",
  "category": "privilege",
  "apiError": 24,
  "nativeAuthorized": false,
  "c1Status": "blocked",
  "c5Status": "blocked"
}
```

The native privilege-query guard failed before the known-folder/target-attribute
phase. The record does not identify the exact failing native call, and it does
not establish whether the process was elevated. Both target states remain
not-checked: no assertion about machine-config presence or absence is possible.

Microsoft defines24 as ERROR_BAD_LENGTH. That identifies an error category,
not a demonstrated root cause of this particular call. No conclusion about
Windows damage, account state or a specific buffer/call fault is inferred.
[Microsoft system error codes](https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-).

No cleanup failure was reported; the reviewed code attempts every acquired
handle/allocation release in finally before emitting the record. No global
resource/access monitoring or broader host-health proof is claimed.

## Scope and next step

No config/auth contents were opened, hashed or copied; no provider CLI/account/
quota request, application build/test, module bootstrap, installer, settings,
firewall/registry change, commit or push was performed. No app source changed.
Lead rechecks the26 source/project/config/shell hashes against the existing
baseline before closing this evidence packet.

H0 attempt is complete as an **inconclusive stopped probe**, not a passing
host preflight. C1/C5 and H1/H2 remain blocked/excluded. Next work is read-only
inspection of the privilege-guard API contract before proposing a corrected,
reviewed command and separately authorized H0 replay. The successful planning
[CLI review](../plans/2026-10-05-windows-host-bounded-test-review.md) is not runtime
success evidence. No second invocation was made.

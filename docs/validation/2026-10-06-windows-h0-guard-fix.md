# H0 guard fix — TokenElevation capacity, metadata-clear replay

2026-10-06. Owner authorized root-cause investigation, guard correction, team
review and ONE corrected H0 replay in the existing metadata-only scope.
The [original attempt](2026-10-06-windows-h0-preflight.md) remains historical
inconclusive/privilege/error24, not relabeled as passing.

## Root cause and differential

Original `Info(kind)` passed capacity4096 to every GetTokenInformation class.
The actual fault on this Windows host was TokenElevation(class20): capacity4096
fails with ERROR_BAD_LENGTH24; capacity4 succeeds on the same token handle.
This is not a blanket rule that oversized buffers fail for every class.

One guard-only diagnostic opened the current process token with TOKEN_QUERY,
rejected thread impersonation, and compared capacities without reading or
printing token-buffer contents. It called no folder/drive/file/provider API.
Diagnostic exit0, stage=matrix/error0, tool wall0.9296578s; no cleanup failure
reported. No existing native-debugger attachment workflow was available in the
ephemeral tool context; this task installed no debugger. Source tracing preceded this
bounded in-process diagnostic.

| Class | Capacity | Success | Required bytes | Error |
|---|---|---|---|---|
| Statistics10 |4096|true|56|0|
| Statistics10 |56|true|56|0|
| Elevation20 |4096|false|4|24|
| Elevation20 |4|true|4|0|
| Integrity25 |4096|true|28|0|
| Integrity25 |28|true|28|0|

This reproduces the failed guard call and tries to disprove the capacity
hypothesis first. Success of the large-buffer controls disproves blanket4096
incompatibility; successful token opening and same-ABI class20 success at4 rule
out failed opening/general ABI or access denial as the explanation for this
observed differential. The corrected full replay below completes the trace.

Microsoft documents TokenElevation as a single DWORD, and its own helpers use
typed sizes. The general API documentation alone did not establish this host's
oversized-capacity behavior; the matrix supplies that evidence.
[TOKEN_ELEVATION](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-token_elevation),
[GetTokenInformation](https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-gettokeninformation),
[Microsoft WIL helpers](https://github.com/microsoft/wil/blob/master/include/wil/token_helpers.h).

## Surgical in-memory correction and review

Only query capacity changes:

```csharp
uint capacity = kind == 20 ? 4u : 4096u;
bool ok = GetTokenInformation(token, kind, buffer, capacity, out length);
```

Allocation remains4096; Statistics/Integrity capacities, ABI, bounds, elevation
consumer length==4, token/context checks, cleanup, budgets and14-field schema
are unchanged. Three type-name occurrences were renamed before hashing to
avoid existing type reuse. No app source or code file was written.

QA author reread the delta and native GPT reviewer independently approved this
specific change for the owner-authorized replay. Neither reviewer executed
code; this was separate-context GPT review, not actual Codex CLI/Claude review.
Lead reread the modified source and compared the unchanged flow.

- Corrected source19325characters/412lines, ASCII in memory only.
- PowerShell parser0errors.
- UTF-8 SHA256:`3a9c9f0b7fbe35a372bfebbfc14c13123b0bb1141cdf54b2065395219768c496`.
- Same-source parser/hash equality immediately before one invocation;
  login=false, no module bootstrap/nested shell/SDK/provider CLI.

The static review before the initial run checked ABI/bounds/cleanup, but did
not experimentally validate the capacity contract of each information class.
No test or successful runtime proof for that contract had existed then.

## Corrected replay result

Exactly ONE corrected full H0 invocation this session, separate from the single
guard-only diagnostic batch. Tool exit0/wall1.1662652s. Lead validated one JSON
record with exactly14 fields:

```json
{
  "schemaVersion": 1,
  "phase": "H0",
  "privilegeClear": true,
  "knownFolderResolved": true,
  "localDriveClear": true,
  "ancestryClear": true,
  "configState": "absent-by-parent",
  "requirementsState": "absent-by-parent",
  "outcome": "metadata-clear",
  "category": "none",
  "apiError": 0,
  "nativeAuthorized": false,
  "c1Status": "blocked",
  "c5Status": "blocked"
}
```

Both targets are absent-by-parent: a parent in the inspected machine-config
namespace was missing, so dependent target attributes were not probed further.
The schema does not disclose which parent or raw path. No configuration file
was opened. This is point-in-time prerequisite metadata, not account/native
isolation, no-egress or production bridge certification.

## Breadcrumb ledger and closure

| Breadcrumb | Change/result | What it establishes |
|---|---|---|
| Original H0 | All classes4096; privilege/error24 before namespace | Repro input and safe stopped record |
| Source/official-doc trace | Fixed and variable token structures have different sizing patterns | Capacity hypothesis; not yet runtime proof |
| Guard-only matrix | Same handle, one capacity axis; class20 fails4096/passes4, controls pass | Concrete class20 root cause and falsification of blanket/ABI/access explanations |
| Team delta review/parser | Class20only4, other guards unchanged; parser0/hash bound | Scope/syntax/source gate, not runtime pass |
| Corrected H0 | metadata-clear/error0, four flags true; targets absent-by-parent | One corrected metadata replay succeeds, earlier observations consistent |

All26 app/source/project/config/shell baseline hashes remain unchanged. No
provider/account/quota call, config/auth contents, application build/test,
installer/settings/firewall/registry changes, commit or push. Runtime has no
explicit file output; compiler/host internal effects remain uncertified.
H1/H2/C1/C5 remain gated. Stop after the single successful replay; no native
launch follows from H0. Cooperative10s budget is not a hard watchdog.

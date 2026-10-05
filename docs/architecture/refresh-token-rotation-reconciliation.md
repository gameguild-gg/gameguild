# Refresh-token reconciliation — #263

## Requirement provenance

[#263](https://github.com/gameguild-gg/gameguild/issues/263) retains all 19 original
criteria and the approved consolidation of duplicate #264. This record separates
the executed repository correction from the remaining integrated token lifecycle.
It does not change the issue's original checkboxes or certify its full completion.

## Executed baseline and repository correction

Production source `8b4cc4eac530d3476b103a8e7f28616144d3984b` was exercised against
actual PostgreSQL with the application's migrations applied. Seven new cases
yielded **two failures and five passing controls**, with zero skips. Both
`GetActiveByUserIdAsync` and `RevokeAllForUserAsync` referenced computed `IsActive`,
which EF explicitly ignores. Both threw SQL translation exceptions before any
listing or user-wide revocation could complete. This confirms the earlier static
review's persistence concern by execution.

Both predicates now use mapped `UserId`, `IsRevoked` and `ExpiresAt`, with a UTC
timestamp captured once per operation. Evaluation remains in PostgreSQL. Active
rows still mean unrevoked and strictly unexpired; listing remains newest first.
User-wide revocation retains its previous active-row behavior, metadata update
and single `SaveChangesAsync`. Other users, expired rows and already revoked rows
are left intact. This correction introduces no API, schema or token-format change.

The five positive controls use the existing actual conditional rotation update:
expired/revoked/already replaced/wrong-hash tokens cannot be claimed, and two
independent contexts produce exactly one committed winner with the winner's hash
stored as the replacement. This is a repository claim test; it does not establish
atomicity of the complete service's token/session issuance and rollback.

The test fixture generates synthetic hash values without credentials or providers.
Seed timestamps are aligned to PostgreSQL's microsecond precision so historical
revocation timestamps can be compared exactly after storage. No authentication,
repository or database operation in these seven cases is mocked.

Executed definitions, initial failures, logs, TRX files and hashes are retained
under `artifacts/test-results/issue-263-refresh-20261005/`.
[Public baseline evidence](https://github.com/gameguild-gg/gameguild/issues/263#issuecomment-5989397406).
The baseline and intervening failures are retained separately from the passing
implementation receipts below.

## Replay containment and command transactions

After the predicate correction, actual HTTP execution exposed a second defect:
the endpoint returned 401 for revoked/replaced tokens, but the command transaction
rolled back its earlier token revocation, session termination and token-version
updates when the service threw the denial. Two replay cases failed their persisted
state assertions; expired and unknown tokens were valid negative controls. An
earlier HTTP fixture attempt also failed three cases because it reused the empty
default username; seed users now have unique usernames. All attempts are retained.

An internal `RefreshTokenContainmentDenial` is now returned only after all required
containment writes complete. It has `Success=false`, a generic message and no
identity/tokens. Its explicit server-only `ICommitOnFailureOutcome` contract tells
the transaction owner to retain those completed mutations. The handler preserves
this internal outcome without profile mapping; the controller still emits the
existing generic 401 Problem Details after the command transaction finishes.
It is not a bindable request flag or a new public API model.

`CommandOutcome.IsFailure` remains unchanged: denial is still business failure,
including for quota accounting. `ShouldRollback` adds an explicit typed commit
decision only for trusted outcomes. Ordinary failed responses, null/false results,
exceptions and cancellation retain their rollback behavior. A similarly named
public flag or a nested marked result cannot grant its wrapper commit authority.
Nested commands still depend on their outer transaction owner; this acceptance
exercises the actual top-level refresh endpoint and its transaction.

All **15** new PostgreSQL cases now pass: the original seven repository cases,
four actual refresh HTTP scenarios and four direct actual transaction scenarios
(ordinary success, ordinary denial, committed denial and thrown failure). The
HTTP replay cases verify stored revocation, terminated sessions and increased user
token version through independent contexts, with no new replacement token or
change to another user's token. Expired/unknown requests do not contain an unrelated
session. Direct transaction cases verify both business outcome and committed
state/outbox count, preserving rollback of ordinary failures and exceptions.

Fresh Authentication **2,145**, SharedKernel **1,377**, Authorization **1,667** and
API architecture/security/eventing **108** pass with zero failures/skips. Full
Release solution is warning/error clean. This is **5,312 focused/core cases**,
including 22 new definitions; those focused cases are not added again to a full
suite receipt. Whole API integration now passes **238** cases; its 15 focused cases
are not counted again. Whole API unit execution is in progress at publication.
The complete exported OpenAPI is identical to accepted #258: **1,297 paths and
1,656 schemas**, with the internal containment result/contract absent. EF reports
no model changes since the last migration; no client regeneration is required.
Matching-head gates and merge remain required. **#263 stays OPEN**.

## Remaining acceptance, in implementation order

| Work | Required acceptance |
|---|---|
| Persisted lineage and family identity | Parent/child lineage and family membership survive restart, isolate independent sessions and support existing records through a documented migration. |
| Atomic service rotation | Exactly one complete replacement token/session commits; losing, cancelled or partially failed requests cannot leave a usable orphan replacement. Old tokens are immediately invalid and expiry/TTL remain enforced. |
| Reuse containment | Replaying any replaced token revokes its family, rejects descendants and produces the required security event. Concurrent replay/rotation has an explicit committed outcome. |
| Revocation entry points and bearer consumption | Explicit and user-wide revocation have actor checks and observable effect on real signed JWT requests through the active host pipeline, including persisted token versions and sessions. |
| Alerts and audit | Token generation, rotation, revocation and reuse write durable, credential-free audit events. Reuse alerts reach the chosen user/security surface with retained delivery evidence. |
| Cleanup and metrics | A registered scheduled caller cleans eligible expired/revoked state with retention boundaries, preserves required lineage/replay evidence and exports bounded-label lifecycle counters. |
| Documentation and whole-flow acceptance | Document hashed storage, cryptographic generation, configurable TTL, race/replay behavior, failure recovery and client refresh coordination. Exercise actual endpoints/storage plus unit contracts. Optional sliding expiration is decided and documented explicitly. |

Existing code may satisfy parts of these requirements. Each must be mapped to its
original criterion and demonstrated before acceptance; a passing repository test
does not certify the whole flow. **#263 remains OPEN.**

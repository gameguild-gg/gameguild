# Economy Consolidation Cross-Reference — issue #1092 safety net

> Purpose: the consolidation refactor (#1092 — Economy family 108 tables, tiered to 67) needs a documented behavioral baseline. This file maps **each consolidation opportunity to the flows, state machines and event rails that exercise it**, so a refactor can be verified flow-by-flow against this package. Table/line citations for the merge targets come from `D:/repositories/gg-dbanalysis/db-analysis.md` (analyzed at `develop` `d5d558e0`) and were re-verified against this branch's module configs where cited.

## Tier 1 — mechanical twins (16 tables → 6)

| #1092 merge | Tables absorbed | Exercised by (this package) | Invariants the refactor must preserve |
|---|---|---|---|
| **T1.1 `economy_outbox`** (5→1, discriminator `OutboxKind`) | `economy_outbox_messages`, `economy_marketplace_outbox`, `economy_payout_dispatch_outbox`, `economy_admin_withdrawal_dispatch_outbox`, `economy_compliance_outbox` | [`04-event-inventory.md`](04-event-inventory.md) §2.1–2.5; flows §5 (payouts), §6 (treasury), §7 (marketplace), §11 (KYC) | **Two of the five queues have no reader today** (N1/N2) — the merge is the moment to either wire a dispatcher or drop the write side. The three live queues have *different* claim disciplines (marketplace: EF+Serializable lease 1 min; payout/treasury: `FOR UPDATE SKIP LOCKED` lease 2 min + `AvailableAt` retry) and different completion markers (`PublishedAt` vs `CompletedAt`); the payout payload is a security-relevant signed command whose hash and fencing token must survive the move. Enqueue must remain **inside** the posting transaction (marketplace writer fns do this today). |
| **T1.2 `economy_inbox`** (2→1) | `economy_compliance_inbox`, `economy_ad_reward_provider_proof_inbox` | [`04-event-inventory.md`](04-event-inventory.md) §2.6; flows §9 (ad proofs), §11 (KYC dedupe) | both dedupe by `ProviderEventId` + `PayloadHash` with `ProcessedAt/ProcessingError`; keep FK-free (they must accept events for aggregates under rebuild — #1092 §3.3 guardrail). |
| **T1.3 `economy_approvals`** (3→1) | `economy_capability_policy_approvals`, `economy_kill_switch_release_approvals`, `economy_projection_generation_approvals` | flows §11 + control-plane endpoints ([`02-cqrs-wiring-matrix.md`](02-cqrs-wiring-matrix.md) EconomyControlPlaneAdministrationController); dual-control semantics | identical 5-column shape with `ReauthenticationHash`; the payout-request dual control is **not** in this family (it lives in `PayoutRequestReviewAuditEvent` rows + `FirstApprovalActorId`) and stays untouched. |
| **T1.4 `economy_provider_events`** (2→1) | `economy_payout_provider_events`, `economy_admin_withdrawal_provider_events` | state machines §4–5; flows §5 edge 7-8, §6; [`05-cross-module-sequences.md`](05-cross-module-sequences.md) §6 | `(EventId PK, EventHash, OperationRef, ResultingState, RecordedAt)`; these are the **replay-dedupe anchors** for provider webhooks (`FindProviderEvent`, `DurablePayoutSettlementWorkflow.cs:165-169,179-183`) — the merged table needs the operation discriminator in every lookup. |
| **T1.5 `economy_ad_reward_session_timeline`** (2→1) | `economy_ad_reward_session_events`, `economy_ad_reward_playback_milestones` | flows §9; state machine §8 | both `(SessionId, Sequence, EvidenceHash, OccurredAt)` unique on `(SessionId, Sequence)`; milestone ordering is verified by `AdPlaybackVerification` (`DurableAdRewardCompletionService.cs:556`) — sequence continuity must survive. |
| **T1.6 `economy_marketplace_value_legs`** (2→1) | `economy_marketplace_settlement_legs`, `economy_marketplace_refund_legs` | flows §7 (settlement legs, refund legs); state machine §6 | leg counters drive the cumulative refund math (`DurableMarketplaceRefundService.cs:174-191`) — the `RefundedUnits` column is settlement-side and must stay queryable per `(SettlementId, Currency)`. |

## Tier 2 — shared spines (98 → 81)

| #1092 merge | Exercised by | Invariants | Status |
|---|---|---|---|
| **T2.1 unified `economy_events`** (10 streams→1, +bounty expiry = 11) | [`04-event-inventory.md`](04-event-inventory.md) §3 (all ten rows + the bounty stream); audit reads in flows §6 (treasury chain), §11 (holds), risk-review audit | unique `(Parent, Sequence)` per stream already exists; **`economy_admin_withdrawal_audit_events` is a hash chain** (`PreviousHash/Hash`) — either keep chain columns on the unified table or keep the table separate (#1092's own exception). Append-only writes happen inside writer functions — the merge must not widen those transactions. | **Delivered** (migration `20261126143143_ConsolidateEconomyEventLedger`): the chain columns stayed on the ledger row (`StreamKind = 11`, unique per `(RunId, Sequence)` and on `Hash`); `economy_provider_dispute_events` kept `ProviderEventId` as a kind column with its own kind-filtered unique index; every SECURITY DEFINER append site was enumerated from the live catalog and retargeted inside the migration with exactly-once rewrites — no writer gained a statement or a lock. See the Tier 2.1 delivery note below. |
| **T2.2 unified `economy_unit_consumptions`** (4→1) | flows §9 (ad budget + cap consumptions — `AdRewardsModelConfiguration.cs:346-359,398-408`); capability receipt consumptions verified inside writer fns (`post_ad_reward_issuance_v1` joins `economy_capability_receipt_consumptions` — `Migration:`); risk decision consumptions (`Core:/Risk/PostgreSqlRiskControlPlane.cs`) | the writer functions **SQL-join** the capability-consumption table to authorize postings; any rename/merge must update the SECURITY DEFINER bodies in lockstep with the EF model. | **Delivered** (migration `20261118113006_ConsolidateEconomyTier2`): the posting writers' capability-receipt joins and risk-decision burn INSERTs were retargeted in the same migration; the insert-validation and immutability triggers moved over kind-scoped (`ConsumptionKind = 2`). |
| **T2.3 unified `economy_policy_versions`** (2→1) | flows §7 — the marketplace dual-control signed currency policy (`DurableMarketplacePolicyReader`, `GetEffectiveAsync`/`GetVersionAsync` — settlement uses effective, refund uses **historical** version `DurableMarketplaceRefundService.cs:155-162`); ad network policy versions (`AdRewardsModelConfiguration.cs:250-268`) | historical-version reads are correctness-critical (a refund must price with the policy that priced the settlement); a `PolicyDomain` discriminator must not break the `(TenantId, …)` effective-window indexes. | **Delivered** (same migration): per-domain partial uniques preserve `(TenantId, subject, Version)`; the settlement/refund writers' policy verification gained the `PolicyDomain` predicate plus the `ProductId`→`SubjectId` rename. |
| **T2.4 unified `economy_operations`** (payouts + treasury runs + dispatch snapshots + authorization evidence) | state machines §4–5 (15 shared spine columns incl. `FencingToken`, `ReserveVersion`, `ReserveAuthorizationEpoch`); flows §5–6; fencing verification in §4 audit items 1-3 | **the fencing-token discipline is the crown jewel**: exact-version + token + epoch match at dispatch (`DurablePayoutSettlementWorkflow.cs:316-323`), monotonic SQL token allocator (`PostgreSqlPayoutFencingTokenAllocator.cs:24-27`), evidence rows keyed `(OperationId, Phase)` (`Payouts:/PayoutAuthorizationEvidence.cs:5`). A discriminator-based merge must keep the two independent fencing allocators monotonic **per kind** (or unify the sequence — but then re-verify no cross-kind token reuse assumptions). Treasury's `ExecutionEpoch` has no payout twin — the merged table keeps both columns. | **Delivered, narrowed** (same migration): payout operations and admin-withdrawal runs merged behind `OperationKind` with both fencing allocators kept independent per kind; `economy_dispatch_snapshots` (keyed by posting group) and `economy_payout_authorization_evidence` (keyed `(OperationId, Phase)`) remain separate 1:N child stores re-bound to `economy_operations` — folding them into the operations row would break their key semantics, so "#1092's dispatch snapshots + authorization evidence" is delivered as retargeted bindings rather than table drops. |
| **T2.5 bounty expiration events → T2.1** | flows §8; worker `Bounties:/DurableBountyExpirationWorkflow.cs:72-79` | version-stamped rows (`BountyVersion`) — keep the version link in the payload. | **Delivered with T2.1** (same migration): `economy_bounty_expiration_events` is `StreamKind = 12`, one row per bounty; `RecordedAt` is the row's `OccurredAt` and `BountyVersion` stays in the payload. |

### Tier 2 delivery note (2026-11-18)

`20261118113006_ConsolidateEconomyTier2` delivers T2.2 + T2.3 + T2.4: eight
legacy tables retire into three unified spines (`economy_unit_consumptions`,
`economy_policy_versions`, `economy_operations`), every SECURITY DEFINER body
that addressed a retired table was rewritten in lockstep (including the ten
table-typed reader functions, which now return the unified row with a kind
predicate), and the risk-consumption triggers moved over kind-scoped.

T2.1/T2.5 (the unified `economy_events` ledger) is deferred rather than
forced: #1092 itself flags `economy_admin_withdrawal_audit_events` as the
possible exception (its `PreviousHash`/`Hash` chain is verified row-by-row by
`DurableAdminWithdrawalApplicationService`), the streams are heterogeneous
(`economy_provider_dispute_events` is keyed by a provider event-id string,
not a `(Parent, Sequence)` pair), and each stream's append happens inside a
posting writer function that must not widen its transaction — a safe merge
needs the writers' insert sites enumerated with the same ground-truth
discipline used for T2.2–T2.4, which is a follow-up of the same size as this
one. Tier 3 builds on T2.1 (the legacy-cutover stream and
`economy_risk_audit_evidence` both fold into it), so the sequencing note in
#1092 is preserved by deferring rather than half-merging.

### Tier 2.1 delivery note (2026-11-26)

`20261126143143_ConsolidateEconomyEventLedger` delivers T2.1 + T2.5: eleven
event and audit tables retire into `economy_events`, a flat row discriminated
by `EconomyEventStreamKind` (twelve kinds — the ad-reward timeline's two
independently numbered streams became kinds 9 and 10, each keeping its own
`(SessionId, Sequence)` index). The three reasons recorded above for deferring
it are each closed:

- **The audit hash chain** — `PreviousHash`/`Hash` are columns of the ledger
  row, unique per `(RunId, Sequence)` and on `Hash` for kind 11. The
  row-by-row verification (`IAdminWithdrawalAuditTrail`,
  `Treasury:/PostgreSqlAdminWithdrawalStore.cs`) reads the same rows through
  the kind predicate.
- **Heterogeneous keys** — `ProviderEventId` is a required column of kind 2
  with its own kind-filtered unique index. The two tables that were not keyed
  by `Id` (provider dispute events; audit entries keyed `(RunId, Sequence)`)
  received fresh ledger ids — neither is ever read back through `Id` — and
  `Down` carries them back losslessly.
- **Writer transaction scope** — the twelve functions in `economy_private`
  that named a legacy table were enumerated from the live catalog. Ten writers
  were retargeted inside the migration: every rewrite must match its function
  text exactly once or the migration aborts, the sequence lookups gained the
  kind predicate so they keep using their partial unique index, and no function
  gained a statement, a lock or a round trip. The table-typed audit reader was
  dropped and recreated against the ledger with its owner and grants.
  `post_provider_reversal_v1` is the one body left untouched: it is retired
  dead code (EXECUTE revoked by `HardenEconomyProviderReversalWriter`, and its
  body already addressed columns and a function that no longer exist), and the
  migration's final guard pins it as the only function naming a retired table.

The five per-table immutability triggers became one kind-scoped trigger
(`deny_immutable_event_mutation`, `StreamKind IN (1, 4, 5, 7, 11)`) and a
kind-shape check forces every column of every other stream to NULL. `Down`
restores the pre-migration state exactly: the eleven tables as the migration
chain built them (physical column order and constraint names included), every
row — including rows written through the ledger after the upgrade — and the
original function text byte for byte.

## Tier 3 — structural merges (81 → 67)

| #1092 merge | Exercised by | Invariants |
|---|---|---|
| legacy family retirement (4 tables) | flows §11 legacy-migration surface ([`02`](02-cqrs-wiring-matrix.md) EconomyLegacyMigrationAdministrationController); step-up cutover commands | time-boxed archive; the cutover audit stream is already in `economy_events` (`StreamKind = 7`), so the retirement has no audit table of its own left to preserve. |
| capability registries merge | control-plane flows (`EconomyModelConfiguration.cs:755-767,1191-1223` per #1092) | unique-name indexes; writer fns resolve capabilities by id — body updates required. |
| `economy_risk_audit_evidence` → events | risk-review audit flow (state machine §10); registered posting and self-service hard-to-soft conversion inside the writer functions | payload jsonb shape. **Delivered** (migration `20261128112714_ConsolidateEconomyTier3RiskAuditEvidence`, on top of the ledger): the record is `StreamKind = 15` and keeps its id; `EventKind` is the row's `KindText`, `RecordedAt` its `OccurredAt`, and `OperationFingerprint`, `EvidenceHash` and the jsonb `Payload` stay columns, unique per `(RiskDecisionId, EvidenceHash)`. The five functions that appended to the table were enumerated from the live catalog and retargeted in place — three fragments each (target, column list, head of the value list), so every body keeps its own layout and `Down` restores it byte for byte — and the migration's final guard pins that nothing in `economy_private` names the retired table. Kind 15 joins `deny_immutable_event_mutation`, as its table's own trigger made it immutable. |
| `economy_anchor_verifications` → events | ledger anchor flows (`Core:/Ledger/PostgreSqlAnchorVerificationService.cs`) | **Not 1:1** — the table is a history: one record per verification run (the first at publication), and the ledger administration endpoint lists every record of an anchor. **Delivered** (migration `20261127094447_ConsolidateEconomyTier3AnchorVerifications`, on top of the ledger): the record is `StreamKind = 14`, ordered by `OccurredAt` with no sequence; `RetainUntil` is the row's `ExpiresAt` and `ObjectHash` its `EvidenceHash`. Columns on the anchor were rejected: they would have dropped the history. The stream does not join `deny_immutable_event_mutation` — its table had no such trigger. Anchor workers are default-off — a safe window for the merge. |
| `economy_projection_reconciliation_events` → events | control-plane projection rebuild flows | generation linkage; its rows are immutable today (`deny_projection_reconciliation_event_mutation`), so the folded kind must join `deny_immutable_event_mutation`. **Delivered** (migration `20261126205120_ConsolidateEconomyTier3ProjectionEvents`, on top of the ledger): the record is `StreamKind = 13`; the rebuild function's single insert is retargeted in place, and kind 13 joins `deny_immutable_event_mutation`, so the record stays immutable. |
| AdRewards pipeline collapse (pending-claims → completion state; reconciliations 3→1) | flows §9; `AdRewardCompletionState` already distinguishes `PendingProviderReport` — the merge formalizes it | `(TenantId, Network, ReportId, Version)` unique prefixes already align (#1092 §2.3-T3.6). |
| `economy_marketplace_refund_debts` + `economy_wallet_debts` | flows §7 edge 4 (seller shortfall debt), provider-reversal debt events | `DebtSource` discriminator; wallet FK is 1:1 on the core side — keep the wallet-side table's key shape. **The plan's shape for this row is wrong, and half of it was delivered instead.** The two tables are different aggregates. `economy_wallet_debts` is a wallet's *current* hard-coin debt: one row per wallet, raised by a provider reversal, read to block cash-out, with the ledger's kind-3 movements pointing at it by foreign key — it stays as it is. `economy_marketplace_refund_debts` was an append-only *record* — one row per refund, responsible wallet and currency — that cannot share a per-wallet key. **Delivered** (migration `ConsolidateEconomyTier3RefundShortfalls`, on top of the ledger): the record is `StreamKind = 16` and keeps its id; the refund function's single insert is retargeted in place, three fragments, each matching once, and the durable refund service reads the shortfalls from the ledger. The stream does not join `deny_immutable_event_mutation` — its table had no such trigger. |
| compliance evidence absorbs custody tables | custody observation endpoints ([`02`](02-cqrs-wiring-matrix.md) reserve query controller) | evidence-kind discriminator. **Assessed 2026-11-27; not merged — the owner's decision is pending.** The three tables are different aggregates: a custody observation is a provider's signed report (asset, eligible amount); a custody reconciliation is a user's record against a reserve version (a jsonb list of observations, no provider and no signature); and compliance evidence has seven `NOT NULL` columns that neither custody row can fill honestly — environment, provider event id, tenant, subject hash, result, policy version and raw object reference. The merge would relax those to nullable plus a shape check, for a two-table reduction, and make the domain harder rather than easier to reason about. Recommendation: keep the custody tables; if the count matters, only observations are a plausible evidence kind. |

## Flow → merge coverage matrix

Every user flow in [`01-user-flows.md`](01-user-flows.md) against the ten consolidation opportunities (T1.1–T1.6, T2.1–T2.4 — T2.5 and Tier 3 are covered above):

| Flow | T1.1 outbox | T1.2 inbox | T1.3 approvals | T1.4 provider events | T1.5 ad timeline | T1.6 value legs | T2.1 events | T2.2 consumptions | T2.3 policy versions | T2.4 operations |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 wallet reads | — | — | — | — | — | — | — | — | — | — |
| 2 top-up mint | writes core outbox | — | — | — | — | — | source stamp events | receipt consumptions | — | — |
| 3 conversion | writes core outbox | — | — | — | — | — | hold/debt events | receipt + risk consumptions | — | — |
| 4 transfer | writes core outbox | — | — | — | — | — | events | consumptions | — | — |
| 5 payout | payout outbox | — | — | provider events | — | — | — | risk consumptions | signed payout policy | **spine** |
| 6 treasury | withdrawal outbox | — | — | provider events | — | — | audit hash chain | — | signed policy | **spine** |
| 7 marketplace | marketplace outbox | — | currency policy approvals | — | — | **both leg tables** | marketplace events | — | **policy versions** | — |
| 8 bounty | — | — | — | — | — | — | bounty expiry events | — | — | — |
| 9 ad rewards | — | provider proof inbox | — | — | **both timeline tables** | — | session events | budget + cap consumptions | ad policy versions | — |
| 10 AI credits | — | — | — | — | — | — | — | — | — | — |
| 11 KYC/compliance | compliance outbox | compliance inbox | hold approvals (T3) | — | — | — | hold + review events | — | — | — |

Reading: every merge target is exercised by at least one documented flow; the densest overlap is the outbox merge (T1.1), which touches five flows — reinforcing #1092's sequencing note that outbox consolidation must be dual-written behind the dispatcher switch first.

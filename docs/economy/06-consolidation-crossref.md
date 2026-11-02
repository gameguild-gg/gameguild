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

| #1092 merge | Exercised by | Invariants |
|---|---|---|
| **T2.1 unified `economy_events`** (10 streams→1, +bounty expiry = 11) | [`04-event-inventory.md`](04-event-inventory.md) §3 (all ten rows + the bounty stream); audit reads in flows §6 (treasury chain), §11 (holds), risk-review audit | unique `(Parent, Sequence)` per stream already exists; **`economy_admin_withdrawal_audit_events` is a hash chain** (`PreviousHash/Hash`) — either keep chain columns on the unified table or keep the table separate (#1092's own exception). Append-only writes happen inside writer functions — the merge must not widen those transactions. |
| **T2.2 unified `economy_unit_consumptions`** (4→1) | flows §9 (ad budget + cap consumptions — `AdRewardsModelConfiguration.cs:346-359,398-408`); capability receipt consumptions verified inside writer fns (`post_ad_reward_issuance_v1` joins `economy_capability_receipt_consumptions` — `Migration:`); risk decision consumptions (`Core:/Risk/PostgreSqlRiskControlPlane.cs`) | the writer functions **SQL-join** the capability-consumption table to authorize postings; any rename/merge must update the SECURITY DEFINER bodies in lockstep with the EF model. |
| **T2.3 unified `economy_policy_versions`** (2→1) | flows §7 — the marketplace dual-control signed currency policy (`DurableMarketplacePolicyReader`, `GetEffectiveAsync`/`GetVersionAsync` — settlement uses effective, refund uses **historical** version `DurableMarketplaceRefundService.cs:155-162`); ad network policy versions (`AdRewardsModelConfiguration.cs:250-268`) | historical-version reads are correctness-critical (a refund must price with the policy that priced the settlement); a `PolicyDomain` discriminator must not break the `(TenantId, …)` effective-window indexes. |
| **T2.4 unified `economy_operations`** (payouts + treasury runs + dispatch snapshots + authorization evidence) | state machines §4–5 (15 shared spine columns incl. `FencingToken`, `ReserveVersion`, `ReserveAuthorizationEpoch`); flows §5–6; fencing verification in §4 audit items 1-3 | **the fencing-token discipline is the crown jewel**: exact-version + token + epoch match at dispatch (`DurablePayoutSettlementWorkflow.cs:316-323`), monotonic SQL token allocator (`PostgreSqlPayoutFencingTokenAllocator.cs:24-27`), evidence rows keyed `(OperationId, Phase)` (`Payouts:/PayoutAuthorizationEvidence.cs:5`). A discriminator-based merge must keep the two independent fencing allocators monotonic **per kind** (or unify the sequence — but then re-verify no cross-kind token reuse assumptions). Treasury's `ExecutionEpoch` has no payout twin — the merged table keeps both columns. |
| **T2.5 bounty expiration events → T2.1** | flows §8; worker `Bounties:/DurableBountyExpirationWorkflow.cs:72-79` | version-stamped rows (`BountyVersion`) — keep the version link in the payload. |

## Tier 3 — structural merges (81 → 67)

| #1092 merge | Exercised by | Invariants |
|---|---|---|
| legacy family retirement (4 tables) | flows §11 legacy-migration surface ([`02`](02-cqrs-wiring-matrix.md) EconomyLegacyMigrationAdministrationController); step-up cutover commands | time-boxed archive; `economy_legacy_cutover_audit` is one of the 10 streams (T2.1 candidate first). |
| capability registries merge | control-plane flows (`EconomyModelConfiguration.cs:755-767,1191-1223` per #1092) | unique-name indexes; writer fns resolve capabilities by id — body updates required. |
| `economy_risk_audit_evidence` → events | risk-review audit flow (state machine §10) | payload jsonb shape. |
| `economy_anchor_verifications` → columns on anchors | ledger anchor flows (`Core:/Ledger/PostgreSqlAnchorVerificationService.cs`) | 1:1 on `ExternalAnchorId`; anchor workers are default-off — safe window for the merge. |
| `economy_projection_reconciliation_events` → events | control-plane projection rebuild flows | generation linkage. |
| AdRewards pipeline collapse (pending-claims → completion state; reconciliations 3→1) | flows §9; `AdRewardCompletionState` already distinguishes `PendingProviderReport` — the merge formalizes it | `(TenantId, Network, ReportId, Version)` unique prefixes already align (#1092 §2.3-T3.6). |
| `economy_marketplace_refund_debts` + `economy_wallet_debts` | flows §7 edge 4 (seller shortfall debt), provider-reversal debt events | `DebtSource` discriminator; wallet FK is 1:1 on the core side — keep the wallet-side table's key shape. |
| compliance evidence absorbs custody tables | custody observation endpoints ([`02`](02-cqrs-wiring-matrix.md) reserve query controller) | evidence-kind discriminator. |

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

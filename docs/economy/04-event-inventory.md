# Economy Event Inventory — 5 outboxes, 10 event streams

> Economy family `GameGuild.Finance.Economy*` · Documented against `develop` commit `ac959fd8f`.
> Citation shorthand: see [`README.md`](README.md). `Migration:` = `API:/Database/Migrations/20260824180312_AddEconomyProductionPlatform.cs`.
> This inventory is the event-side reference for consolidation issue #1092 (§2.3 Tier 1 outbox merge and Tier 2.1 unified event ledger).

## 1. Event machinery overview

The Economy family does **not** use the platform integration-outbox rail (`gameguild.integration.outbox_messages` + `OutboxDispatcher`, used by Billing — `docs/commerce/payments/04-event-flow-inventory.md` §1). It runs its own five lease-based outbox tables plus ten per-aggregate event/audit streams, all inside the `economy_*` table family. There is no publisher-subscription bus: delivery is worker-poll or nothing.

| Rail | Mechanism | Worker | Default |
|------|-----------|--------|---------|
| Payout dispatch outbox | `economy_payout_dispatch_outbox`, lease + `AvailableAt` retry | `EconomyDurableWorker.RunDispatchCycleAsync` (`API:/Core/HostedServices/EconomyDurableWorker.cs:108-127`) → `IPayoutDispatchOutboxProcessor` | **OFF** — `DispatchWorkersEnabled` defaults false (`:17`) |
| Admin-withdrawal dispatch outbox | `economy_admin_withdrawal_dispatch_outbox`, same shape | same cycle → `IAdminWithdrawalDispatchOutboxProcessor` (`:116-117`) | **OFF** (same switch) |
| Marketplace outbox | `economy_marketplace_outbox`, lease + `PublishedAt` | `EconomyDurableWorker.RunSafetyCycleAsync` (`:76-88`) → `IMarketplaceOutboxProcessor.ProcessNextAsync` in a batch loop | ON — `InternalOutboxWorkersEnabled` (`:18`), `SafetyWorkersEnabled` (`:15`) |
| Core posting outbox | `economy_outbox_messages` | **none** | write-only spool |
| Compliance outbox | `economy_compliance_outbox` | **none** | write-only spool |

## 2. The 5 outboxes — writers, readers, retry, DLQ, replay

### 2.1 `economy_outbox_messages` (core posting outbox)

- **Writers**: the PostgreSQL writer functions insert `economy.posting.accepted.v1` rows after every accepted posting — verified in `economy_private.post_ad_reward_issuance_v1` (`Migration:`, `INSERT INTO public.economy_outbox_messages ... 'economy.posting.accepted.v1'`; the same insert block exists in the registered-posting writer path) — plus the in-memory kernel's `AddOutbox` (`Core:/Ledger/InMemoryLedgerKernelStore.cs`, payloads `economy.payout.reserved.v1` / `economy.payout.dispatch.v1` in `Payouts:/PayoutCoordinator.cs:218-221,294-297`, test-only composition).
- **Readers**: **none.** No processor, dispatcher or consumer reads this table anywhere in `apps/api/Source` (verified by search — the only references are the EF row/config: `Core:/Persistence/EconomyPersistenceRows.cs:356`, `EconomyModelConfiguration.cs:641-654`). There is not even a `PublishedAt` column — only `Id, PostingGroupId, Type, Payload, PayloadHash, OccurredAt` with a `PayloadHash` unique.
- **Retry/DLQ/replay**: n/a — never delivered. Rows accumulate unboundedly (NEW gap N1; also #1094 §4.3 #8 noted the missing dispatcher-scan index on the assumption a dispatcher exists).

### 2.2 `economy_marketplace_outbox`

- **Writers**: inside the posting transactions — `economy_private.post_marketplace_settlement_v1` inserts `marketplace.entitlement.grant.v1` and `post_marketplace_refund_v1` inserts `marketplace.entitlement.revoke.v1` on full refund (`Migration:`, ids derived via `derive_economy_uuid_v1(settlement,'entitlement-grant-outbox')` / `(refund,'entitlement-revoke-outbox')`). Message shape `(Id, TenantId, SettlementId, MessageType, Payload, PayloadHash, OccurredAt, PublishedAt?, LeaseOwner?, LeaseExpiresAt?, AttemptCount, LastError?)`.
- **Reader/processor**: `PostgreSqlMarketplaceOutboxProcessor.ProcessNextAsync` (`Marketplace:/PostgreSqlMarketplaceOutboxProcessor.cs:67-163`) — Serializable claim of the oldest unpublished, unleased row (`:118-137`, lease 1 min), payload-hash + settlement-binding validation (`:140-163`), then `CommerceMarketplaceOutboxHandler` (`:166-307`): grant → `IEntitlementService.GrantEntitlementAsync` + order `MarkAsPaid`/`MarkAsFulfilled` (`:221-255`); revoke → `RevokeEntitlementAsync` + order `ProcessRefund` (`:257-279`); order-line snapshot re-verification (`:281-296`).
- **Retry**: failure records `LastError` (1,000-char clamp) and releases the lease (`:96-110`) — the next cycle re-claims and increments `AttemptCount`. **No attempt cap, no dead-letter** (NEW gap N3). Operator visibility: `GET admin/economy/marketplace/outbox` (`EconomyMarketplaceQueryAdministrationController.cs:87`).
- **Replay**: safe — handler operations are idempotent on their deterministic ids (entitlement keyed by settlement; order transitions status-guarded).

### 2.3 `economy_payout_dispatch_outbox`

- **Writer**: `DurablePayoutSettlementWorkflow.BeginDispatchAsync` inside the Serializable dispatch transaction (`Payouts:/DurablePayoutSettlementWorkflow.cs:141-151`) — payload = serialized `PayoutDispatchCommand` (idempotency key `{operation}:dispatch`, fencing token, snapshot hash), `AvailableAt = now`.
- **Reader**: `PostgreSqlPayoutDispatchOutboxProcessor.ProcessNextAsync` (`Payouts:/PostgreSqlPayoutDispatchOutbox.cs:73-223`) — `FOR UPDATE SKIP LOCKED` claim (`:170-190`, lease 2 min), payload-hash + operation-binding validation (`:93-104`), provider dispatch + evidence-signature verification (`:106-112`), completion transaction re-checking lease + operation state (`:114-153`). Gated by `DispatchWorkersEnabled` (default **off**) — fail-closed provider dispatch (`EconomyDurableWorker.cs:16,49-52`).
- **Retry**: any failure releases the lease and pushes `AvailableAt` 1 minute out with `LastErrorCode = provider-error` (`:193-207`) — infinite retry, **no attempt cap/DLQ** (N3). Terminal provider events bypass this queue via webhook/reconcile (`ApplyProviderEventAsync`, `:157-257`).
- **Replay**: idempotent — `CompletedAt` short-circuit (`:119-123`) and operation-state guard (`:132-144`).

### 2.4 `economy_admin_withdrawal_dispatch_outbox`

- **Writer**: `Treasury:/DurableAdminWithdrawalWorkflow.cs` `BeginDispatchAsync` (same pattern as payouts; run id + fencing token + execution epoch in payload).
- **Reader**: `PostgreSqlAdminWithdrawalDispatchOutboxProcessor` (`Treasury:/PostgreSqlAdminWithdrawalDispatchOutbox.cs`) called from the same dispatch cycle (`EconomyDurableWorker.cs:116-117`), provider `StripePlatformAdminWithdrawalProvider` (`Treasury:/StripePlatformAdminWithdrawalProvider.cs`).
- **Retry/DLQ**: same lease + `AvailableAt` backoff, same **no cap** (N3). Index redundancy noted by #1094 (§4 close: unique `RunId` + unique `(TenantId, RunId)` — the second subsumed).

### 2.5 `economy_compliance_outbox`

- **Writer**: `PostgreSqlComplianceEvidenceStore.IngestAsync` (`Core:/Risk/PostgreSqlComplianceEvidenceStore.cs:288-300`) — `economy.compliance-evidence.published.v1` with evidence envelope digest, written transactionally with the `economy_compliance_inbox` dedupe row (`:193-230`).
- **Readers**: **none found** (verified by search — only the EF row/config references). KYC status reads query the evidence tables directly (`Payouts:/ComplianceKycPayoutEligibilitySource.cs`), not the outbox (NEW gap N2 — same family as N1).

### 2.6 The 2 inboxes (for completeness)

- `economy_compliance_inbox` — provider-event dedupe for compliance/KYC evidence (`PostgreSqlComplianceEvidenceStore.cs:193-230`; status `Published/Duplicate` mapping into KYC at `API:/Core/Integration/EconomyKycEvidenceStore.cs:22-27`).
- `economy_ad_reward_provider_proof_inbox` — ad-network provider proof dedupe (`AdRewards:/Persistence/AdRewardsModelConfiguration.cs:318-329`), consumed by the report/proof pipeline (`AdRewards:/AdRewardProviderAdapters.cs`).

## 3. The 10 per-aggregate event/audit streams

The streams #1092 §2.3-Tier-2.1 proposes folding into one `economy_events` table, each already declaring the target unique `(Parent, Sequence)` shape:

| # | Table | Parent aggregate | Written at | Read at |
|---|-------|------------------|-----------|---------|
| 1 | `economy_source_stamp_events` | source stamp | every funding transition (`Core:/Funding/HardCoinFundingLifecycle.cs:254-273`; writer fns insert the first event, `Migration:`) | funding claim re-hydration, ledger queries |
| 2 | `economy_hold_events` | hold | hold lifecycle changes — e.g. refund-drain release inside `post_marketplace_refund_v1` (`Migration:`) | hold audit endpoint (`EconomyComplianceHoldAdministrationController.cs:68`) |
| 3 | `economy_wallet_debt_events` | wallet debt | provider-reversal debt accrual (`Core:/Funding/PostgreSqlProviderReversalGateway.cs`) | debt queries |
| 4 | `economy_provider_dispute_events` | provider dispute | dispute lifecycle (`Core:/Funding/ProviderDisputeWorkflow.cs`) | dispute views |
| 5 | `economy_risk_review_events` | risk review case | case transitions (`Core:/Risk/RiskReviewLedger.cs`) | `GET risk-reviews/{id}/audit` (`EconomyRiskReviewAdministrationController.cs:64`) |
| 6 | `economy_compliance_hold_events` | compliance hold | hold placement/release (`Core:/Risk/PostgreSqlComplianceHoldStore.cs`) | hold audit |
| 7 | `economy_legacy_cutover_audit` | legacy cutover batch | cutover propose/approve/rollback (`Core:/Operations/LegacyEconomyShadowMigration.cs`) | migration batch views |
| 8 | `economy_marketplace_events` | marketplace settlement | inside the settlement/refund writer functions (`Migration:`; unique `(SettlementId, Sequence)` index) | settlement audit |
| 9 | `economy_ad_reward_session_events` + `economy_ad_reward_playback_milestones` | ad session | milestone recorder (`AdRewards:/DurableAdRewardCompletionService.cs:244`) + lifecycle | session status reads (`DurableAdRewardSessionReader.cs`) |
| 10 | `economy_admin_withdrawal_audit_events` | treasury run | every run transition (`Treasury:/DurableAdminWithdrawalWorkflow.cs` `audit.Append` calls) | hash-chain verified `Audit` view (`DurableAdminWithdrawalApplicationService.cs:312-317`) — **the one stream with chain columns (`PreviousHash/Hash`)**, flagged by #1092 as the possible exception to the unified table |

Plus the bounty expiry stream `economy_bounty_expiration_events` (worker, `Bounties:/DurableBountyExpirationWorkflow.cs:72-79`) — the "+1" #1092 Tier-2.5 folds into the same unified ledger, and `economy_risk_audit_evidence` (Tier-3.3) which carries the same shape.

## 4. Delivery posture summary

| Queue/stream | Delivery guarantee | Retry | Dead-letter | Replay-safe |
|---|---|---|---|---|
| marketplace outbox | at-least-once (worker) | infinite 1-min lease cycles | **none** | yes (deterministic ids) |
| payout dispatch outbox | at-least-once (worker, **disabled by default**) | infinite, `AvailableAt + 1 min` | **none** | yes (`CompletedAt` + state guards) |
| admin-withdrawal dispatch outbox | at-least-once (worker, **disabled by default**) | same | **none** | yes |
| core posting outbox | **never delivered** | n/a | n/a | n/a |
| compliance outbox | **never delivered** | n/a | n/a | n/a |
| 10 event streams | local append-only audit — consumed by direct reads, not dispatched | n/a | n/a | append-only by construction |

Contrast with the platform outbox rail (Billing): per-consumer delivery receipts, retry schedule and `DeadLetteredAtUtc` exist there (`API:/Core/Eventing/OutboxDispatcher.cs`) — none of the five Economy queues inherit that machinery, which is the operational argument behind #1092's Tier-1 single-queue merge.

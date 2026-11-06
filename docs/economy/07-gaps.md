# Economy Gaps — known issues vs NEW findings

> Economy family `GameGuild.Finance.Economy*` · Documented against `develop` commit `ac959fd8f`.
> Citation shorthand: see [`README.md`](README.md). `Migration:` = `API:/Database/Migrations/20260824180312_AddEconomyProductionPlatform.cs`.

## Known issues (already tracked, deliberate, or cross-referenced from sibling packages)

| # | Issue | Evidence | Status |
|---|-------|----------|--------|
| K1 | **108-table family with 5 outboxes and 10 event streams** — the consolidation target itself | issue #1092; family inventory and tiered plan in `D:/repositories/gg-dbanalysis/db-analysis.md` §2; cross-referenced per-flow in [`06-consolidation-crossref.md`](06-consolidation-crossref.md) | tracked (#1092); PR-able increment 108 → 82 (Tier 1 + Tier 2.1-2.4) |
| K2 | **No database FKs from sub-module tables to `economy_wallets`** (AdReward sessions/completions, marketplace buyer/seller/platform wallets, bounty poster/escrow, payout operations) nor the commerce seam (`settlements.OrderId → orders`, `top_up_intents.PaymentId → payments`) | issue #1093 §3.2-B/P1; e.g. `AdRewards:/Persistence/AdRewardsModelConfiguration.cs:47,135`, `Marketplace:/Persistence/MarketplaceModelConfiguration.cs:44-47`, `Bounties:/Persistence/BountiesModelConfiguration.cs:11-12`, `Payouts:/PayoutPersistenceModels.cs:13`, `Core:/Persistence/EconomyModelConfiguration.cs:1035-1037` | tracked (#1093, P0 financial + P1 seam) |
| K3 | **Index gaps**: `economy_marketplace_settlements (Status)` for the operational sweep; duplicate unique `HasIndex(RunId).IsUnique()` + `HasIndex(TenantId, RunId).IsUnique()` on the admin-withdrawal outbox; core outbox lacks a dispatcher-scan index | issue #1094 §4.3 items 1, 8 and §4 close; query `Marketplace:/PostgreSqlMarketplaceOperationalQueryReader.cs:34`; duplicate at `Treasury:/PostgreSqlAdminWithdrawalStore.cs:159-160` | tracked (#1094 PR-1) — the outbox-scan index is moot while N1 stands |
| K4 | ~~**Top-up refunds never reverse the Economy mint**~~ **FIXED by #1266** (verified at develop 21b58ac6a7) — `charge.refunded` now drives `TryReverseRefundedTopUpAsync` through the persisted provider-reversal writer (`Core:/Integrations/StripeEconomyTopUpEventConsumer.cs:33,43-44,158-266`): protected operation `ReverseProviderFunding` with `economy:stripe:reverse` idempotency, cumulative-amount conversion with a whole-HardCoin requirement (`:268-278`), already-spent units partitioned into wallet debt (`ProviderReversalDisposition.ResponsibleDebt`, `:246-251`), and the event released afterwards for the Commerce lifecycle consumer (`:261-266`). `MapStatus` itself still maps `payment_intent.*` only (`:150-157`) — the reversal rides a dedicated path, not a status mapping. The funding state machine's `Reversed` is now reachable via webhook ([`03-state-machines.md`](03-state-machines.md) §2) | fixed (#1266 / #1202 N4 family); residual: no rate policy on the webhook host route (N9) |
| K5 | Dormant configured tables (`Finance.Ledgers`, `Social.Ratings`, `Gamification.Achievements`) inflate the "table count" denominator | #1092 §1 caveat 1 | tracked (resolve in consolidation PRs) |
| K6 | **AI-credit usage lives outside the double-entry journal** — reservations/settlements subtract from the wallet view via a contributor instead of posting `Burn` | `Core:/Queries/EconomyWalletQueries.cs:120-133` (interface doc comment), `AiCredits:/Integrations/AiCreditWallet.cs` | deliberate, documented — flagged for Tier-3 judgment when #1092 reaches the ledger collapse |
| K7 | **Provider dispatch and anchor workers are default-off** (fail-closed switches) | `API:/Core/HostedServices/EconomyDurableWorker.cs:16-18` with doc comment `:25-29` | deliberate — but see N6/N7 for the unprotected in-flight window this creates |
| K8 | Marketplace settlement has no pending/in-progress state — it is born `Settled` inside the writer transaction | `Marketplace:/MarketplaceContracts.cs:23-28,189`; writer function atomicity | deliberate (atomic posting), noted for state-machine completeness |

## NEW gaps found by this audit

### Event delivery / operations

**N1 — The core posting outbox has no consumer; it is a write-only spool.**
`economy_outbox_messages` rows (`economy.posting.accepted.v1`) are inserted by the SECURITY DEFINER writer functions after every accepted posting (verified in `economy_private.post_ad_reward_issuance_v1`, `Migration:`, and the registered-posting path — `INSERT INTO public.economy_outbox_messages`), yet no processor, dispatcher or reader exists anywhere in `apps/api/Source` (only references are the EF row `Core:/Persistence/EconomyPersistenceRows.cs:356` and config `EconomyModelConfiguration.cs:641-654`; `EconomyDurableWorker` never touches it; the table has no `PublishedAt`). Every posting grows the table forever and the events are never delivered. Either wire a dispatcher or stop writing — and the #1092 T1.1 merge should decide which before unifying.

**N2 — The compliance outbox has no consumer either.**
`PostgreSqlComplianceEvidenceStore.IngestAsync` writes `economy.compliance-evidence.published.v1` (`Core:/Risk/PostgreSqlComplianceEvidenceStore.cs:288-300`) transactionally with the inbox row, but no reader consumes the queue (verified by search at 21b58ac6a7). Correction to the original audit: payout KYC eligibility does **not** read the evidence tables — `ComplianceKycPayoutEligibilitySource` calls the legacy `KycService` ledger (`Payouts:/ComplianceKycPayoutEligibilitySource.cs:22,36-37`), which sharpens compliance-package gap N7 (two ledgers, no bridge): the signed evidence tier is *written but never consulted* by either gate. Same unbounded-growth shape as N1.

**N3 — None of the five Economy outboxes has an attempt cap or dead-letter path.**
Marketplace: failure records `LastError` and releases the lease, next cycle re-claims, `AttemptCount` grows unbounded (`Marketplace:/PostgreSqlMarketplaceOutboxProcessor.cs:96-110` — no cap branch). Payout: any failure pushes `AvailableAt = now + 1 min` with `LastErrorCode = provider-error` and retries forever (`Payouts:/PostgreSqlPayoutDispatchOutbox.cs:193-207`). Treasury: same shape (`Treasury:/PostgreSqlAdminWithdrawalDispatchOutbox.cs`). A poison payload (hash mismatch, unsupported message type) retries every minute indefinitely; the only visibility is the admin outbox list for marketplace ([`02-cqrs-wiring-matrix.md`](02-cqrs-wiring-matrix.md)). The platform outbox rail the Billing module uses has `DeadLetteredAtUtc` semantics (`API:/Core/Eventing/OutboxDispatcher.cs`) — the consolidation merge (T1.1) should bring the cap with it.

### State-machine dead ends

**N4 — `PayoutOperationState.Cancelled` is unreachable in the production path; a reserved-but-undispatched payout pins FIFO fragments forever.**
The transition table allows `Reserved → Cancelled` (`Payouts:/PayoutLifecycleContracts.cs:185`), but the only producer is the in-memory `PayoutCoordinator.DispatchAsync` (`Payouts:/PayoutCoordinator.cs:252-256`) — and neither `PayoutCoordinator` nor `InMemoryPayoutOperationStore` is composed (API registers only the durable services: `Payouts:/PayoutsModule.cs:14-31`, `API:/Core/Setup/ApiProductComposition.cs:133`). No controller or command cancels a payout operation. An operation reserved then abandoned (operator never dispatches) keeps its fragment reservations in `Reserved`, excluding those funds from every other flow with no exit.

**N5 — Treasury runs reserve funds at proposal and have no cancel path — `PendingApproval` runs hold value indefinitely and block overlapping withdrawals.**
`ProposeAsync` creates the run in `PendingApproval` **and immediately** runs the reservation workflow (FIFO fragments + `AdminWithdrawalReservation` posting) (`Treasury:/DurableAdminWithdrawalApplicationService.cs:82-168` → `DurableAdminWithdrawalWorkflow.ReserveAsync`). `AdminWithdrawalRunState.Cancelled` (`AdminWithdrawalContracts.cs:14`) has no producer in the durable path (verified — only the overlap guard *reads* it), and the controller exposes no cancel (`API:/Core/Controllers/EconomyTreasuryAdministrationController.cs:44-206`). The overlap guard treats every non-`Failed`/non-`Cancelled` run as blocking (`Treasury:/PostgreSqlAdminWithdrawalStore.cs:238`), so a proposed-but-never-approved run both pins escrow fragments and vetoes the next period's withdrawal until someone dispatches-and-fails or manually fixes the row.

**N6 — The execution gates are dead code in the composed path, and the dispatch worker does not re-check the kill switch before calling the provider.**
`PayoutExecutionGate`/`AdminWithdrawalExecutionGate` (`PayoutLifecycleContracts.cs:267-305`, `AdminWithdrawalContracts.cs:125-176`) — including `Stop()`, the epoch bump meant to fence in-flight work — have **zero callers** in the durable services or module registrations (verified by search; only the in-memory coordinators use them). Kill-switch enforcement in the durable path happens at *authorization* time (receipt epoch equality `DurablePayoutSettlementWorkflow.cs:405`; active-switch checks inside the writer functions, `Migration:`), but once a dispatch is enqueued, `PostgreSqlPayoutDispatchOutboxProcessor.ProcessNextAsync` (`Payouts:/PostgreSqlPayoutDispatchOutbox.cs:73-160`) dispatches to the provider without consulting any gate or current epoch — activating a kill switch does not stop already-authorized outbox rows from being sent.

**N7 — `Ambiguous` payout/withdrawal states have no automated exit.**
Exit requires a terminal provider webhook or an operator `reconcile` (`Payouts:/DurablePayoutApplicationService.cs:279-310`; treasury twin `:273-305`). `EconomyDurableWorker` runs no ambiguity sweep (safety cycle: capability activation, journal integrity, bounty expiry, marketplace outbox — `API:/Core/HostedServices/EconomyDurableWorker.cs:58-106`). A missed webhook leaves six-figure-value operations ambiguous until a human notices. (Partially deliberate — provider truth is authoritative — but nothing scheduled even *surfaces* stale ambiguity.)

**N8 — AI-credit reservations never expire.**
`AiCreditReservation` has `Reserved → Settled/Released` only (`AiCredits:/Integrations/AiCreditWallet.cs:105-165`); no hosted service sweeps stale reservations (nothing in `AiCredits:/AiCreditsModule.cs` or the durable worker). An AI run that crashes between `ReserveAsync` and `SettleAsync`/`ReleaseAsync` permanently subtracts its `ReservedSoftUnits` from the wallet's available balance (`:250-264`).

### API surface

**N9 — No Economy controller applies rate limiting.**
All 19 controllers lack `EnableRateLimiting` (verified by search across `API:/Core/Controllers/Economy*.cs` and `AdReward*.cs`). Money-moving self-service endpoints (`POST economy/transfers`, `top-ups`, `payout-requests`, `ad-rewards/sessions`, `marketplace ... :settle/:refund`) run unthrottled — unlike the Payments family where mutations carry `RateLimitPolicies.ExpensiveOperations` (`docs/commerce/payments/02-cqrs-wiring-matrix.md` §Auth model). The protected-operation risk rail throttles by policy, but not by request rate.

**N10 — Read paths bypass the CQRS pipeline (auth-behavior and logging parity).**
All `GET` surfaces on the execution/treasury/marketplace/bounty/ad-reward/compliance/ledger/policy/reserve controllers call stores or readers directly instead of `ISender` (flagged per-route in [`02-cqrs-wiring-matrix.md`](02-cqrs-wiring-matrix.md)). Permission checks still run in-controller, but these reads skip the shared pipeline behaviors (validation, authorization-behavior, audit) that the command paths get — an inconsistency the payments package treated as a convention violation.

### Severity summary

| Gap | Money impact | Class |
|---|---|---|
| N1, N2 | indirect (storage growth, missing delivery) | event plumbing |
| N3 | indirect (poison-message livelock on money queues) | event plumbing |
| N4, N5 | **direct** — funds/escrow pinned with no exit | state-machine dead end |
| N6 | **direct** — kill switch cannot stop in-flight provider sends | concurrency/control |
| N7 | direct (value stuck in Ambiguous until manual action) | operations |
| N8 | direct to affected user (spendable Soft reduced) | lifecycle |
| N9, N10 | hygiene | API surface |

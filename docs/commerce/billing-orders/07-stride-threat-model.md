# Billing / Orders / Subscriptions STRIDE Threat Model

> Modules `GameGuild.Commerce.Billing` + `GameGuild.Commerce.Orders` + `GameGuild.Commerce.Subscriptions` (+ `GameGuild.Commerce.Payments` charge/refund authority, `GameGuild.Finance.Economy.Marketplace` settlement, `GameGuild.Notifications` dunning channel) · Documented against `develop` commit `21b58ac6a7`.
> Companion documents: flows (`subscription-billing-cycle-flow.md`, `order-lifecycle-flow.md`), wiring matrix (`cqrs-wiring-matrix.md`), state machines (`state-machines.md`), event inventory (`event-flow-inventory.md`), sequences (`cross-module-sequences.md`), gaps (`gaps.md`), DFDs (`08-dfds.md`), architecture (`../09-security-architecture.md`).

## 0. Scope, assets and trust boundaries

**Crown-jewel assets** — (B1) the authoritative subscription price chain (plan → subscription → invoice → payment), (B2) per-cycle billing idempotency (no double billing), (B3) the plan-change saga's money compensation, (B4) order line-item pricing snapshots, (B5) entitlement/inventory grants tied to settled payments, (B6) the provider webhook inbox (billing truth), (B7) dunning customer communications.

**Trust boundaries** (full DFDs in `08-dfds.md`):

| # | Boundary | Crossing | Primary control |
|---|----------|----------|-----------------|
| TB1 | Internet user → API | authenticated HTTP | `[Authorize]` + `RequirePermission` / `ValidateTenantAccess` per surface |
| TB2 | Provider (Stripe et al.) → webhook inbox | anonymous POST, signed | per-provider signature verifiers + `WebhookSourceSecurityFilter` + `Webhook` rate limit + durable inbox (`WebhookProcessorBase.cs:177-298`) |
| TB3 | Billing → Payments (in-process port) | `IPaymentSubscriptionSyncService`, `IPaymentDunningLifecycleService`, `IOrderPaymentProcessor`/`IOrderPaymentAuthority` | Deny-default ports when unregistered (`GameGuild.Commerce.Orders/Abstractions/IOrderPaymentAuthority.cs:9-31`) |
| TB4 | Orders → Economy.Marketplace (in-process port) | `IOrderMarketplaceSettlementAuthority` | fail-closed Deny default (`IOrderMarketplaceSettlementAuthority.cs:47-55`), idempotent settle |
| TB5 | API → durable outbox → consumers | integration events | per-consumer receipts ×5 → dead-letter (`OutboxDispatcher.cs:263-292`); saga events published `required: true` |
| TB6 | Subscriptions → Notifications (dunning email) | domain events + durable notification queue | attempt ceiling + dead-letter (`GameGuild.Notifications/Entities/Notification.cs:292-310`) |

**Method** — STRIDE per flow family. Every threat carries a verified `file:line` mitigation or a tracking reference. Gap ids G-1…G-14 refer to `gaps.md`; **PR #1229 (billing money-integrity, issue #1205) has since fixed G-1, G-2 (PastDue half), G-4, G-5, G-8, G-9, G-10, G-11, G-13 and most of G-6/G-7** — those fixes are cited as mitigations; residual open items are marked **OPEN**. The #1202 payments-side currency/concurrency class fixed by #1266 is covered in `../payments/07-stride-threat-model.md` and cross-referenced here where the billing path shares it.

---

## 1. Flow family — subscription creation (plan-derived amounts, trial, promo)

Entry: `POST api/v{v}/subscriptions` (`GameGuild.Commerce.Subscriptions/Controllers/SubscriptionsController.cs:35-75`) → `CreateSubscriptionCommandHandler`.

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 1.1 | **S** — cross-tenant subscription creation | post with another tenant's id | controller tenant validation from auth context 403 (`SubscriptionsController.cs:41-42`); integration-pinned `AuthenticationAndTenantIsolationTests` |
| 1.2 | **T** — **client-supplied amount becomes the billing amount** (price tampering — the G-13 class) | direct API call creates a 0.01 subscription on a 99 plan | **fixed via #1229 (was G-13)**: the handler now loads the plan, resolves the authoritative interval price (`ResolvePriceForCycleAsync`, `Commands/CreateSubscription/CreateSubscriptionCommandHandler.cs:30-48`), treats the client amount as *advisory only* — a disagreement is logged as a price-integrity signal (`:50-59`) — and persists `effectiveAmount = promoQuote?.FinalAmount ?? authoritativeAmount` (`:110`); fraud assessment uses the plan price (`:65-66`) |
| 1.3 | **T** — custom-quote sentinel bypass | create against an enterprise plan carrying the -1 price | fail-closed `SubscriptionPlanCustomQuotePendingException` (`CreateSubscriptionCommandHandler.cs:30-36`, issue #416) |
| 1.4 | **R** — promo abuse (invalid code, stacking, re-use) | supply promo codes to discount forever | codes quoted server-side fail-closed — none applicable refuses the whole creation with per-code reasons (`:78-106`); quote applies against the plan's authoritative price (issue #418); promo usage rows stamped on invoice materialization (`GameGuild.Commerce.Billing/Services/InvoiceGenerationService.cs:203`), so reuse is visible downstream |
| 1.5 | **I** — trial extension abuse | apply a trial-extension promo to push billing forever | `ExtendTrial` pushes `NextBillingDate` past the new trial end (`Entities/Subscription.cs:399-404`); trial grants are journaled events (`TrialExtendedEvent :406`); repeated extension requires repeated valid promo codes — server-quoted each time |
| 1.6 | **D** — repudiate subscription creation ("fraud evidence vanished") | pause/suspend overwrites metadata | **fixed via #1229 (was G-10)**: `Suspend` merges suspension bookkeeping into metadata instead of replacing it — creation-time fraud-review evidence survives (`Entities/Subscription.cs:452-461`, `MergeSuspensionMetadata`); `Reactivate` removes only suspension keys (`:467-480`) |
| 1.7 | **S** — fraud gate bypass at creation | create while gate disabled | tenant-scoped `IsEnabled` (`GameGuild.Commerce.Payments/Services/FraudDetection/FraudDetectionService.cs:72-89`); Block refuses creation outright (`SubscriptionFraudBlockedException`), Review records evidence in metadata (`CreateSubscriptionCommandHandler.cs:134-148` post-drift) |
| 1.8 | **E** — subscription-create flooding | scripted creates | `[EnableRateLimiting(ExpensiveOperations)]` on the controller (`SubscriptionsController.cs:23`) |

## 2. Flow family — billing period, renewal quote and invoice materialization

Entry: `SubscriptionBillingService.ProcessRenewalAsync` (quote-only), `InvoiceGenerationService.MaterializeForConfirmedCycleAsync` (`GameGuild.Commerce.Billing/Services/InvoiceGenerationService.cs`), provider-billed path `invoice.payment_succeeded` (`StripeBillingWebhookService.cs:486-497`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 2.1 | **R** — **double billing for one cycle** (the crown-jewel billing threat) | replay a payment confirmation / race two materializers | closed three ways: per-cycle idempotency key `subscription:{id}:cycle:{n}:invoice` (`InvoiceGenerationService.cs:47-49`) with unique index `IX_invoices_IdempotencyKey` race backstop — loser detaches and re-fetches the winner (`:179-191`); `Subscription.RecordPayment` cycle monotonicity + `LastPaymentIdempotencyKey` — same cycle same key idempotent (`:685-694`), same cycle different key `RejectedOutOfOrder` (`:690-703` post-#1229 line drift); the single-payment-per-invoice `PaymentId` unique index (`Entities/Invoice.cs:20`) |
| 2.2 | **T** — invoice amount tampering after issue | mutate a persisted invoice | entity immutability: amounts frozen once `Status != Draft` (`Entities/Invoice.cs:185`, `EnsureMutable :458-464`); drafts only exist inside the materializer/saga which issue immediately; `Void` refuses `Paid` (`:370-372`) |
| 2.3 | **I** — provider invoice impersonation | forged `invoice.payment_succeeded` with a chosen amount | signature verification (TB2) + provider-object binding; `RecordPaymentAsync` enforces amount+currency equality against the subscription's authoritative money (`BillingWebhookService.cs:191` handler; entity guard `Subscription.cs:760+` `RejectedMoney` — cannot over/under-record) |
| 2.4 | **D** — pricing decisions repudiated | "the renewal price was arbitrary" | every renewal quote writes an idempotent pricing-decision ledger entry per cycle (`GameGuild.Commerce.Subscriptions/Services/SubscriptionBillingService.cs:48-88`); drift validation surface `POST pricing-discounts-ledger/:validate-drift` (SystemAdmin) |
| 2.5 | **E** — invoice materialization failure loses the cycle | write fails after payment synced | never-throws adapter with idempotent replay healing (`GameGuild.Commerce.Billing/Services/SubscriptionInvoiceMaterializer.cs:52-59`); re-attempt on replay (`PaymentSubscriptionSyncService.cs:103-121`) |
| 2.6 | **R** — wallet credit paid twice per invoice | outbox redelivery | `BillingInvoicePaidV1` consumer idempotent per provider event id (issue #406, `API:/Core/Billing/BillingInvoicePaidWalletCreditHandler.cs:30-34`); per-consumer receipts ×5 → DLQ |
| 2.7 | **E** — invoices that can never be paid age invisibly | account-credit shortfall leaves an Open invoice | **fixed via #1229 (was G-2, PastDue half)**: `InvoiceAgingService.MarkOverdueOpenInvoicesPastDueAsync` sweeps overdue Open invoices to `PastDue` (`GameGuild.Commerce.Billing/Services/InvoiceAgingService.cs:19-67`); **residual OPEN**: `Invoice.MarkUncollectible` (`Entities/Invoice.cs:424`) still has no caller — the bad-debt write-off half remains dead code |
| 2.8 | **T** — account-credit tender against the wrong invoice state | apply credits to a Paid/Void invoice | `ApplyAccountCredit` guards refuse illegal states (`Entities/Invoice.cs:394-410`) |

## 3. Flow family — payment failure, retry, dunning

Entry: `invoice.payment_failed` webhook → `RecordPaymentFailureAsync` (`SubscriptionBillingService.cs:111+`); retry queue `PaymentRetryQueueWorker` (opt-in); suspension `PaymentDunningLifecycleService.SuspendForRetryExhaustionAsync` (`:22-72`); recovery `TryReactivateAfterRecoveryAsync` (`:74-109`); email ladder `PaymentDunningOptions` (`API:/Core/Integration/PaymentDunningOptions.cs:20-43`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 3.1 | **S** — forged payment-failure event drives suspension | crafted `invoice.payment_failed` | signature verification (TB2) + inbox idempotency (`WebhookProcessorBase.cs:187-198`); only Active transitions to PastDue (`Entities/Subscription.cs:840-844` post-#1229 drift) |
| 3.2 | **E** — **dunning harassment** (the money-family communication threat) | retry storm emails the customer on every failure | bounded retry budget on the payment row (default 4, backoff 1m×5ⁿ, `GameGuild.Commerce.Payments/Entities/Payment.cs:20-22`); dunning ladder is two rungs — PaymentFailed day-0 High, FinalNotice day-3 Urgent (`PaymentDunningOptions.cs:24-41`) with the final notice fired once at suspension (`PaymentDunningLifecycleService.cs:57-68`); notifications queue enforces quiet hours + attempt ceiling + dead-letter (`Notification.cs:99-108, 292-310`); misconfigured ladder fails closed to the built-in default step, never to silence and never to spam (`PaymentDunningOptions.cs:91-112,129-152`) |
| 3.3 | **T** — retry charges the wrong instrument mid-swap | customer swaps card while a retry is in flight | checkout pauses the retry queue during method swap and resumes after (`GameGuild.Commerce.Payments/Controllers/PaymentsController.cs:294-305`); expired methods are marked and skipped (`PaymentMethodExpiryMonitorBackgroundService`) |
| 3.4 | **R** — double suspension / suspension of the wrong subscription | concurrent retry exhaustion | `SuspendForRetryExhaustionAsync` skips already-suspended/cancelled and only accepts Active/PastDue (`PaymentDunningLifecycleService.cs:39-55`); idempotent by reason check |
| 3.5 | **I** — a paying customer stays suspended (denial of service after payment) | recovery payment lands but nothing reactivates | **fixed via #1229 (was G-11)**: a confirmed cycle payment heals `PastDue → Active` inside `RecordPayment` (`Entities/Subscription.cs:814-818`); dunning-suspended subscriptions reactivate only on `RetryExhausted` reason recovery (`PaymentDunningLifecycleService.cs:74-109`, reason guard `:127`) |
| 3.6 | **D** — dunning communication repudiation | "they never warned me before suspension" | every dunning send is a durable notification row with attempts/evidence; suspension events journaled (`SubscriptionRenewalFailedEvent :68`) |
| 3.7 | **E** — retry queue silently disabled (operational) | `Payments:RetryQueue:Enabled` off | accepted known posture (K-1 in `gaps.md`) — dunning then depends on provider-side retries; documented in `docs/billing/payment-retry-and-dunning.md` |
| 3.8 | **T/E** — manual invoice-retry endpoint is a no-op ack | customer told "retry scheduled", nothing happens | **fixed via #1229 (was G-1)**: `RetryInvoicePaymentHandler` is now a real handler — tenant-scoped read (`i.TenantId == actor.TenantId.Value`, `Commands/RetryInvoicePayment/RetryInvoicePaymentHandler.cs:37-38`), dispatches into the Payments retry boundary, transitions Draft/Open/PastDue per contract (issue #1205 G-1) |

## 4. Flow family — cancellation, pause/resume, reactivate

Entry: `POST {id}:cancel` / `:suspend` / `:pause` / `:resume` / `:reactivate` (`Controllers/SubscriptionLifecycleController.cs:96,116,136,155,173`); provider cancel webhook (`BillingWebhookService.cs:124-150`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 4.1 | **S** — actor cancels/suspends another user's subscription | route-id guessing | lifecycle mutations are `[Authorize]` + `ExpensiveOperations`; queries are tenant-filtered; destructive PATCH/PUT/DELETE on `SubscriptionsController` now gated by `RequirePermission(MonetizationPermission.Keys.Configure)` (`SubscriptionsController.cs:222,249,276`) — **G-9 fixed via #1229** |
| 4.2 | **T** — cancel then get billed anyway | race cancel vs renewal charge | `RecordPayment` rejects payments for cancelled subscriptions — `RejectedCancelled`, message says refund required (`Entities/Subscription.cs:747-755`); concurrent renewal vs cancel guarded by optimistic `RowVersion` (`:229-235`) |
| 4.3 | **R** — pause used as billing-abuse toggle | pause → extend trial-like free use → resume | pause aliases to `Suspend` (acknowledged aliasing, K-3) but now: suspension records `autoRenewBeforeSuspension` and turns `AutoRenew` off (`Entities/Subscription.cs:452-461`); resume restores the recorded preference instead of force-enabling billing (`:467-480`) — **G-10 fixed**; suspended subscriptions cannot record payments while suspended (RecordPayment guards) |
| 4.4 | **I** — future-dated cancel treated as immediate | service cut before the paid period ends | `Cancel` records `EndDate = effectiveDate ?? now` (`:434-447`) — the state machine's terminal Cancelled plus honored EndDate; residual semantics documented (mid-cycle vs end-of-cycle is a date, state-machines.md note) |
| 4.5 | **D** — provider-initiated cancel repudiated | "we never cancelled at Stripe" | provider deleted webhook cancels with Custom reason + provider EndDate (`BillingWebhookService.cs:137-150`) and publishes `BillingSubscriptionCancelledV1` outbox event (`:150-154`) |
| 4.6 | **E** — cancel spam | scripted cancels | `ExpensiveOperations` rate limit on the lifecycle controller |

## 5. Flow family — plan change saga (proration → invoice → payment → activation)

Entry: `POST billing/subscriptions/{id}/plan-change:orchestrate` (`GameGuild.Commerce.Billing/Controllers/SubscriptionChangeOrchestrationController.cs:32-45`) → `SubscriptionChangeSagaOrchestrator`.

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 5.1 | **R** — double plan-change charge (replayed intent) | re-POST the same orchestration | saga idempotency key `subscription:{id}:change:{plan}:{effectiveDate:O}` unique (`Services/SubscriptionChangeSagaOrchestrator.cs:540-542`); replay returns the recorded saga (`:93-101`); the gateway charge carries `{key}:charge` (`:246`) |
| 5.2 | **T** — proration currency/amount manipulation | cross-currency downgrade to mint credit | currency mismatch between plans refuses the change (`:143-147`); proration is daily-rated over remaining days server-side (`:123-171`); net-positive adjustments produce a real invoice (Open → paid), credits are recorded on the saga — no wallet minting (`:171-197`) |
| 5.3 | **I** — activation on an unsettled charge | gateway outcome unknown → activate anyway | uncertain outcomes throw, local rollback, same intent retried after reconciliation — **fail-closed for activation** (`:260-266`); settled amount/currency mismatch blocks activation (`:292-296`) |
| 5.4 | **D** — compensation refund denied | saga fails after the charge, money stuck | compensation issues the gateway refund + voids the invoice + terminal Failed state with `CompensationsApplied` evidence (`:366-440`); step events published `required: true` so the journal cannot silently lose a step (`GameGuild.Commerce.Billing/Events/BillingIntegrationEventPublisher.cs:158-213`) |
| 5.5 | **E** — stuck saga after crash | worker dies mid-saga | journaled steps + terminal Failed with evidence; replays return the recorded outcome until a new effective date (K-4 by design); no partial-activation window — activation is the last step |
| 5.6 | **S** — unauthorized orchestration | non-owner triggers plan change | controller `[Authorize]` + `Api` limiter; authorization tests pin the surface (`SubscriptionChangeAuthorizationTests`); residual noted: the route is any-authenticated (see wiring matrix) |

## 6. Flow family — order placement, capture, fulfillment (post-#1229 semantics)

Entry: `POST v{v}/orders` → items → `POST {id}:capture` → `POST {id}:complete` (`GameGuild.Commerce.Orders/Controllers/OrdersController.cs`), marketplace cart checkout (`MarketplaceCartController.cs:55`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 6.1 | **R** — duplicate order creation (double purchase) | replay the create with the same key | unique `IdempotencyKey` index; same-actor replay returns the original order (`Commands/CreateOrder/CreateOrderCommandHandler.cs:24-36`); **key owned by another actor → Forbidden** (`:29-33`) — collision-based squatting refused |
| 6.2 | **T** — line-item price tampering at add time | add an item with a client-supplied price | items carry an **authoritative pricing snapshot** resolved from the published price version (`Entities/Order.cs:218-296`): non-positive price rejected (`:245-253`), whole-line discount rejected (`:260-263`), mixed currencies rejected (`:270-277`); cart checkout re-snapshots every line and 409s on drift (`Commands/MarketplaceCartCommands.cs:149-156`) |
| 6.3 | **R** — double charge on capture (the order money threat) | two concurrent captures | persisted Pending→Processing claim before the gateway call; loser gets 409 `ConcurrentModification` and was not charged (`Commands/CaptureOrder/CaptureOrderCommandHandler.cs:78-93`); Payments-side idempotency `order:{tenant:N}:{order:N}:charge` (`GameGuild.Commerce.Payments/Services/OrderPaymentService.cs:399`); duplicate capture of an already-Paid order returns the existing payment (`CaptureOrderCommandHandler.cs:30-33`) |
| 6.4 | **I** — fulfillment without settlement (entitlement theft) | complete an order whose payment did not settle | `IsSettledAsync` demands payment Succeeded **and** order/tenant/amount/currency equality (`OrderPaymentService.cs:317-325`) — fail-closed; unregistered Payments port → Deny (`Abstractions/IOrderPaymentAuthority.cs:9-31`) |
| 6.5 | **T** — subscription lines smuggled through the generic order flow | charge a subscription via orders:capture | authority guard 403 (`CaptureOrderCommandHandler.cs:63-69`) |
| 6.6 | **E** — outcome-unknown payment re-charged blindly | capture re-run on a Processing payment | `SafeProviderReplayWindow` 23h (`OrderPaymentService.cs:17`); past the window → `RequiresReconciliation`, no blind re-charge (`:106-110`, `:311`) |
| 6.7 | **I** — entitlement/inventory divergence | inventory decremented without entitlement, or vice versa | complete runs inventory commit + per-line entitlement grant + `MarkAsFulfilled` in **one transaction** — rollback on any failure (`Commands/CompleteOrder/CompleteOrderCommandHandler.cs:125-176`); out-of-stock rolls back with 409 (`:129-145`) |
| 6.8 | **S** — actor captures/completes another user's order | route-id guessing | `OrderActorContext.Authorize` in capture and complete handlers (`CaptureOrderCommandHandler.cs:26`, `CompleteOrderCommandHandler.cs:30`); **refund and update now enforce it too** (#1229 — `Commands/RefundOrder/RefundOrderCommandHandler.cs:34-37`, verified); **OPEN residual (G-6)**: cancel, delete, hold, release still lack the ownership check (verified — no `OrderActorContext` reference in those handlers) |
| 6.9 | **E** — order-surface flooding | scripted creates/captures | **fixed via #1229 (was G-7)**: `OrdersController` now carries `[EnableRateLimiting(RateLimitPolicies.Api)]` (`Controllers/OrdersController.cs:22`, verified) |
| 6.10 | **I** — reservation release invisible to audit | capture decline path skips the audit log | **fixed via #1229 (was G-5)**: `ReleasePaymentReservation` now goes through `TransitionToWithReason` so `OrderStateChangedEvent` + `order_audit_logs` record it (`Entities/Order.cs:361-373`) |
| 6.11 | **T** — Economy settlement mixed with fiat references (double settlement) | complete with both bodies | mixed authority 400 (`CompleteOrderCommandHandler.cs:41-47`); Economy port unregistered → Deny (`IOrderMarketplaceSettlementAuthority.cs:47-55`); settle idempotent by caller key, `IsDuplicate` propagated |

## 7. Flow family — order refund (post-#1229 semantics)

Entry: `POST v{v}/orders/{id}:refund` → `RefundOrderCommandHandler`.

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 7.1 | **T** — refund without money moving (local status flip only) — the pre-#1229 G-4 class | refund endpoint flips status; customer never gets money | **fixed via #1229 (was G-4)**: the handler now routes through the Payments refund boundary (`IOrderPaymentRefunder.RefundAsync` with `AuthoritativeOrderRefund` carrying order/tenant/payment/amount/currency/reason/actor) **before any local mutation** (`Commands/RefundOrder/RefundOrderCommandHandler.cs:73-90`) — approval-queued refunds move no money and leave the order untouched until completion; gateway refund + refund accounting happen in Payments |
| 7.2 | **R** — refund the modern happy path repeatedly | refund a `Fulfilled` order twice | status guard accepts `Fulfilled`/`Completed`/`PartiallyRefunded` (`:39-43`); already-`PartiallyRefunded` orders may only be refunded the **full remaining total** (FSM has no second-partial edge) (`:45-52`); the Payments side enforces its own single-open-refund + cumulative guards (`../payments/07-stride-threat-model.md` §3) |
| 7.3 | **T** — amount out of bounds | refund 0 or > total | bound validation 0 < amount ≤ total (`:54-60`) |
| 7.4 | **S** — actor refunds another user's order | route-id guessing | ownership enforced (`:34-37`, #1229) + `RequirePermission(OrdersPermission.Keys.Refund)` on the controller |
| 7.5 | **I** — refund an order never captured through Payments | legacy unbound order | explicit validation — only orders with a Payments binding can be refunded here (`:62-68`) |
| 7.6 | **D** — entitlement revocation / inventory restore divergence on full refund | money back but entitlement kept | full refund revokes entitlements and restores tracked inventory as part of the flow (`:38-50` pre-drift refs now at handler bottom; `Entities/Order.cs:457+` `ProcessRefund` picks Refunded/PartiallyRefunded by cumulative) |
| 7.7 | **E** — Economy-settled orders have no refund path | complete via MarketplaceSettlement then refund | **OPEN (G-4 adjacent)**: the Economy settlement path has no refund leg — `RefundOrderCommandHandler` requires a Payments `PaymentId` (`:62-68`); Economy-hard-currency purchases cannot be reversed through this endpoint |

## 8. Flow family — provider webhook ingress (billing truth)

Entry: `POST api/v{v}/billing/webhooks/{provider}` (`BillingWebhooksController.cs:60,126,186,253,319`) → `WebhookProcessorBase` pipeline → durable inbox.

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 8.1 | **S** — forged provider event (renewal never happened / cancel injected) | sign a fake `invoice.payment_succeeded` | per-provider signature verification (Stripe HMAC `StripeWebhookVerifier.cs:15`; PayPal/Braintree/Apple JWS/Google verifiers), `WebhookSourceSecurityFilter` + IP allowlist (`Security/WebhookSourceIpAllowlist.cs`), `Webhook` rate limit, payload contract pinning |
| 8.2 | **R** — duplicate delivery double-applies money state | provider redelivery | duplicate check by `(ExternalEventId, Provider)` before processing (`WebhookProcessorBase.cs:187-198`); inbox row stored **before** routing (`:212-215`); Stripe path replay → `AlreadyProcessed` + security event (`StripeBillingWebhookService.cs:97-114`) |
| 8.3 | **I** — event applied to the wrong subscription | crafted external ids | provider events resolve through the subscription's registered external ids; unknown subscription → logged, 200, inbox processed — no guessed target (`BillingWebhookService.cs:137-140`, `StripeTrialWillEndConsumer.cs:53-62`) — prevents infinite replay of unfixable events without misattribution |
| 8.4 | **E** — poison event loops forever | unparseable/unknown payload | per-attempt timeout (`:254-257`); exponential backoff 1→360 min, terminal 1440 min, then dead-letter + **critical security event** (`Services/BillingWebhookRetryWorker.cs:181-207`, `WebhookSettings.cs:158,164`, `BillingWebhookDeadLetterService.cs:28-35`); SystemAdmin replay endpoint (`BillingWebhooksController.cs:494`) |
| 8.5 | **D** — webhook trail repudiated | "that renewal event was never ours" | raw delivery + headers retained under opt-in redacted policy (`Entities/BillingWebhookRawDelivery.cs`, `Security/WebhookRawDeliveryRedactor.cs`); processed/failed outbox events queryable (`BillingEventsController.cs:89-125`) |
| 8.6 | **S** — secret rotation window attack | replay with an old secret | dual-secret rotation window (Stripe) + `POST secrets:rotate` SystemAdmin with Compliance.Audit logging (`Commands/RotateWebhookSecret/RotateWebhookSecretCommand.cs:126-141`) |
| 8.7 | **I** — dead command accepts webhooks without a handler | `ProcessBillingWebhookCommand` | inert: no handler, no caller — cannot be invoked through any route (G-14 register; assembly-scan registration means Send-time failure, not an open surface) |

## 9. Flow family — subscription state integrity (FSM-level)

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 9.1 | **T** — out-of-order cycle recording (skip-ahead billing) | record cycle n+2 before n+1 | expected = last+1; out-of-order rejected (`Entities/Subscription.cs` RecordPayment monotonicity, post-#1229 at `:734+`) |
| 9.2 | **I** — payment recorded for a cancelled/expired subscription | pay after cancelling | economic invariant guard — `RejectedCancelled` "Refund required" (`:747-755`) |
| 9.3 | **T** — refund does not stop the subscription (pay for nothing) | full refund, subscription keeps running | `AdjustForRefundAsync` suspends active/past-due subscriptions with `RefundIssued` reason on full refunds; partial leaves running (`GameGuild.Commerce.Subscriptions/Services/PaymentSubscriptionSyncService.cs:212-257`) |
| 9.4 | **I** — unreachable states confuse consumers | `Expired` has no writer | **OPEN (G-3)**: `SubscriptionStatus.Expired` declared, guarded against, but never written — consumers branch on a state that cannot occur (`Services/SubscriptionPaymentMethodCoordinationService.cs:35`); `OrderStatus.Disputed` likewise unreachable (G-12) |
| 9.5 | **E** — query endpoints 500 at runtime | five queries had no handlers | **fixed via #1229 (was G-8)**: `GetSubscriptionUsageHandler`, `GetSubscriptionBillingHistoryHandler`, `GetSubscriptionPlanUsageStatisticsHandler`, `SuggestSubscriptionPlanUpgradesHandler`, `ValidateSubscriptionPlanLimitsHandler` all exist now (verified in `Queries/` directories) |

## 10. Residual risk register (this model's outputs)

| Id | Item | Class | Status |
|----|------|-------|--------|
| R-B1 | Invoice `Uncollectible` still has no writer — no bad-debt write-off | E/I | OPEN (G-2 half) |
| R-B2 | Order cancel/delete/hold/release lack actor-ownership checks | S | OPEN (G-6 residual, verified) |
| R-B3 | Economy-settled orders have no refund path | T | OPEN (G-4 adjacent) |
| R-B4 | `SubscriptionStatus.Expired` + `OrderStatus.Disputed` unreachable | I | OPEN (G-3, G-12) |
| R-B5 | Retry queue opt-in/disabled default leaves dunning to provider | E | Accepted (K-1) |
| R-B6 | Plan-change route is any-authenticated (no RequirePermission) | S | Noted in wiring matrix; hardening candidate |
| R-B7 | Provider-billed renewals trust Stripe's clock (no local monotonicity on that path) | R | Accepted (K-6, architectural) |

## 11. Verification notes (audit of this package's functional docs)

Audited the seven package documents against `develop 21b58ac6a7` (written against `74e9bba4c`). 14+ citations spot-checked per family; results:

**Confirmed fixed by #1229 (issue #1205) — `gaps.md` rows now stale:**
- G-1 retry stub → real handler, tenant-scoped (`Commands/RetryInvoicePayment/RetryInvoicePaymentHandler.cs:22-46` verified: invoice read filters `i.TenantId == actor.TenantId.Value`).
- G-2 PastDue half → `InvoiceAgingService` exists and marks overdue Open invoices (`Services/InvoiceAgingService.cs:19-67`). `MarkUncollectible` still callerless (verified: only `Entities/Invoice.cs:424` definition) — register updated.
- G-4 → `RefundOrderCommandHandler` fully rebuilt: Payments-boundary refund first, Fulfilled accepted, ownership enforced, PartiallyRefunded→full-remaining rule (`Commands/RefundOrder/RefundOrderCommandHandler.cs:1-90`).
- G-5 → `ReleasePaymentReservation` via `TransitionToWithReason` (`Entities/Order.cs:361-373`).
- G-7 → `OrdersController.cs:22` `[EnableRateLimiting(RateLimitPolicies.Api)]` (verified).
- G-8 → all five handlers exist (verified directory listings).
- G-9 → `RequirePermission(MonetizationPermission.Keys.Configure)` on PATCH/PUT/DELETE (`Controllers/SubscriptionsController.cs:222,249,276`).
- G-10 → `MergeSuspensionMetadata`/`RemoveSuspensionMetadata` preserve evidence and restore AutoRenew (`Entities/Subscription.cs:452-495`).
- G-11 → PastDue heals on confirmed cycle payment (`Entities/Subscription.cs:814-818`).
- G-13 → plan-authoritative amounts (`CreateSubscriptionCommandHandler.cs:30-66` verified: `authoritativeAmount` from `ResolvePriceForCycleAsync`; client amount advisory, disagreement logged).
- G-6 → partially fixed: `UpdateOrderCommandHandler` and `CreateOrderCommandHandler` now reference `OrderActorContext` (verified); cancel/delete/hold/release do not (residual registered as R-B2).

**Citation drift (line-number-only):**
- `Entities/Subscription.cs` — #1229 added ~112 lines: `RecordPayment` 641→`734`, PastDue transition 740→`844`, heal block new at `:814-818`, Suspend 452→`452-461` (same start, new body). State-machine doc's line refs for RecordPayment internals are stale by +93.
- `CreateSubscriptionCommandHandler.cs` — fraud gate 41→`66`, promo quote 54→`78`, amount usage 84→`110`.
- Verified exact: `SubscriptionChangeSagaOrchestrator.cs:542` (saga key), `:246` (`:charge`), `OrderPaymentService.cs:17` (23h window), `:106-110`, `:318` (IsSettledAsync), `:399` (charge key), `InvoiceGenerationService.cs:23-24,47-49` (invoice key), `PaymentDunningLifecycleService.cs:22,57,74,127`, `PaymentDunningOptions.cs:24-41`, `WebhookProcessorBase.cs:187-198,212-215`, `BillingWebhookRetryWorker.cs` backoff refs.

**Issue material for the orchestrator (do not file here):** R-B1 (Uncollectible dead), R-B2 (orders ownership asymmetry), R-B3 (Economy refund path), R-B4 (dead states Expired/Disputed), R-B6 (plan-change permission), plus the payments-package residuals R-P1..R-P6 in `../payments/07-stride-threat-model.md`.

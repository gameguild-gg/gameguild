# Payments STRIDE Threat Model

> Module `GameGuild.Commerce.Payments` (+ Stripe ingress in `GameGuild.Commerce.Billing`, Economy top-up in `GameGuild.Finance.Economy`) · Documented against `develop` commit `21b58ac6a7`.
> Citation shorthand: `Payments:` = `apps/api/Source/Modules/GameGuild.Commerce.Payments/`, `Billing:` = `apps/api/Source/Modules/GameGuild.Commerce.Billing/`, `Economy:` = `apps/api/Source/Modules/GameGuild.Finance.Economy/`, `API:` = `apps/api/Source/GameGuild.API/`.
> Companion documents: flows (`01`), wiring matrix (`02`), state machines (`03`), event inventory (`04`), sequences (`05`), gaps (`06`), DFDs (`08`), architecture (`../09-security-architecture.md`).

## 0. Scope, assets and trust boundaries

**Crown-jewel assets** — (A1) money at the Stripe gateway and its local mirror (`Payment` aggregate), (A2) refund authority (gateway + local `Refund` rows), (A3) dispute outcomes and evidence, (A4) wallet/account-credit balances, (A5) gateway API credentials, (A6) webhook signing secrets, (A7) fraud blacklist / review decisions, (A8) FX rate overrides and currency configuration.

**Trust boundaries crossed by the money path** (full DFDs in `08-dfds.md`):

| # | Boundary | Crossing | Primary control |
|---|----------|----------|-----------------|
| TB1 | Internet user → Web/API | authenticated HTTP, bearer token | `[Authorize]` + CQRS `AuthorizationBehavior` + controller `ValidateTenantAccess` |
| TB2 | API → Stripe (outbound) | TLS to `api.stripe.com`, keyed by gateway credentials | credential store under DataProtection (`Payments:/Services/GatewayCredentialStore.cs:93-94`), per-request `IdempotencyKey` |
| TB3 | Stripe → API webhook ingress (inbound, anonymous) | `POST billing/webhooks/stripe` | HMAC signature (`StripeWebhookVerifier.cs:15`), livemode/api-version/account pinning, source filter, `Webhook` rate limit (`BillingWebhooksController.cs:186-189`) |
| TB4 | Webhook ingress → aggregates | verified inbox row → consumer chain | signature-verified inbox rows only (`StripeBillingWebhookService.cs:235-245`), single-worker claim (`:170`) |
| TB5 | API → durable outbox → Economy wallet credit | integration events | per-consumer receipts, idempotent by event id (`API:/Core/Eventing/OutboxDispatcher.cs:263-292`) |
| TB6 | Stripe verified event → Economy mint/reversal | `IStripeVerifiedEventConsumer` chain | settlement-row binding + posted-idempotency (`Economy:/Integrations/StripeEconomyTopUpEventConsumer.cs`) |

**Method** — STRIDE per flow family. Every threat carries either a mitigating `file:line` (verified against `21b58ac6a7`) or a tracking reference. Gap ids N1-N15 refer to `06-gaps.md`; PRs #1266 (refund currency, concurrency, webhook transitions — issue #1202) and the security-batch follow-ups have since **fixed N1-N6, N9, N11, N13**; those fixes are cited here as *mitigations*, and the residual open items (N7, N8, N10, N12, N14, N15) are marked **OPEN**.

---

## 1. Flow family — payment initiation and Stripe exchange

Entry: `POST api/v1/payments` (`Payments:/Controllers/PaymentsController.cs:130-184`) → `ProcessPaymentCommandHandler` (`Payments:/Commands/ProcessPayment/ProcessPaymentCommandHandler.cs:36-389`) → `IPaymentGateway` failover/circuit stack → `StripePaymentService.PaymentIntent create Confirm true` (`Payments:/Services/StripePaymentService.cs:593-642`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 1.1 | **S** — spoofed payer submits a charge for another tenant | actor A posts `TenantId` of tenant B | controller `ValidateTenantAccess` 403 (`PaymentsController.cs:138-139`); handler re-checks subscription tenant (`ProcessPaymentCommandHandler.cs:45-48` throws `UnauthorizedAccessException`) — belt and braces |
| 1.2 | **T** — amount tampering; client submits an amount ≠ subscription price | modify request body amount | handler equality guard against the authoritative subscription amount (`ProcessPaymentCommandHandler.cs:50-53`); subscription-side `RecordPayment` re-verifies money (`GameGuild.Commerce.Subscriptions/Entities/Subscription.cs:760` amount/currency match) |
| 1.3 | **T** — currency confusion, the #1202 class — minor-unit exponent applied for the wrong currency | pay 100 JPY, be credited as 100 USD-cents scaled | charge leg converts with `request.Currency` (`StripePaymentService.cs:610`); refund leg now uses the **payment's** currency (`:754-768`, comment names the JPY 100× over-refund) — **fixed via #1266** (was gaps N1); webhook amounts cross-checked on refund completion (`RefundProcessingService.cs:644-653`) |
| 1.4 | **R** — replayed charge for the same billing cycle | re-POST the same subscription payment | idempotency key derived from tenant+subscription+cycle (`ProcessPaymentCommandHandler.cs:60`); replay returns the recorded row, no second charge (`:70-90`) |
| 1.5 | **R** — idempotency-key collision across subscriptions | craft two requests that derive the same key | key includes tenant id + subscription id + billing cycle (`SubscriptionPaymentIdentity`), so a collision requires the same subscription and cycle — that is 1.4, answered by the same guard; storage lookup `GetByIdempotencyKeyAsync` (`:70`) |
| 1.6 | **I** — gateway response tampering in transit | MITM alters the PaymentIntent response | TLS to Stripe; responses are bound to the request through the provider-mapping binder before any state change (`Payments:/Services/GatewayPaymentResultBinder.cs:24+`); reconciliation only trusts signature-verified webhooks (TB3) |
| 1.7 | **I** — outcome-unknown timeout hides a real charge (denial of truth) | Stripe 5xx/timeout after money moved | deliberate fail-open-to-`Processing` (`StripePaymentService.cs:701-715`; handler keeps claim `:293-299`) — never retried blind; webhook reconciliation settles it (`Billing:/Services/StripePaymentLifecycleEventConsumer.cs:88-108`), and aged unmapped Pending rows now exit via `PendingPaymentAgingService` (`Payments:/Services/PendingPaymentAgingService.cs`, registered `PaymentsModule.cs:169-174`) — **#1266 closed the Pending limbo (was N5/N6)** |
| 1.8 | **D** — repudiation of a charge by the payer | "I never authorized this" | fraud attempt context persisted pre-gateway (IP, device fingerprint, BIN, timezone — `ProcessPaymentCommandHandler.cs:95-110` `RecordFraudAttemptContext`); `PaymentRetryAttempt`/audit rows; receipt email (`:353`); Stripe-side PaymentIntent evidence |
| 1.9 | **E** — DoS the charge endpoint / gateway flooding | scripted charge attempts | `[EnableRateLimiting(ExpensiveOperations)]` on the controller (`PaymentsController.cs:25`); fraud gate throttles risky flows (§4); Stripe-side rate limits are the provider's own |
| 1.10 | **E** — cross-tenant resource exhaustion via unthrottled aux controllers | hammer `currencies`, `pricing`, `taxes`… | **OPEN — N10**: ten controllers carry no `[EnableRateLimiting]` (verified: 0 attributes in `CurrenciesController.cs`, `PricingController.cs`, `GiftsController.cs`); the anonymous `currencies/suggestion` endpoint is reachable pre-auth |
| 1.11 | **S** — account-credit tender abuse; spend credits that were revoked | race a credit revocation against a charge | credits are *planned* pre-gateway but *consumed only on success* (`ProcessPaymentCommandHandler.cs:323`); consumption failure flags manual settlement instead of double-taking (`:496-508`); credit-only settlement is atomic (`SettleWithCreditsOnlyAsync :397-430`) |

## 2. Flow family — payment methods and SetupIntent checkout

Entry: `POST payments/setup-intents` (`PaymentsController.cs:208`), `POST payments/subscription-checkouts:complete` (`:248-311`), `PaymentMethodsController` CRUD (`api/v1/billing/payment-methods`), Stripe `payment_method.updated` mirror (`Billing:/Services/StripePaymentMethodUpdatedConsumer.cs`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 2.1 | **S** — actor registers a method for another user | guess another user's route id | per-action ownership checks (`PaymentMethodsController.cs` ownership column of the wiring matrix); typed registration with method-type hint (fix commit `358fa8290`) |
| 2.2 | **T/E** — method-swap mid-dunning charges a dead card | retry queue fires while the customer changes instrument | checkout completion pauses queued retries before the Stripe default-method call and resumes after (`PaymentsController.cs:294-305`); on swap failure retries stay paused (no automated charge of the failed instrument) |
| 2.3 | **I** — local method registry drifts from gateway truth | registry says Active, Stripe detached | gateway is authoritative; mirror upsert is additive best-effort and never fails the charge (`PaymentsController.cs:902-925`); `payment_method.updated`/detachment events reconcile (`StripePaymentMethodUpdatedConsumer.cs`); expiry monitor marks stale rows (`PaymentMethodExpiryMonitorBackgroundService`, `PaymentsModule.cs:271`) |
| 2.4 | **D** — repudiate a method registration | "someone added that card" | registry rows carry actor/audit columns; single-default invariant is test-pinned (`PaymentMethodSingleDefaultInvariantTests`); removal of an in-use method requires explicit `Confirm` (`PaymentMethodsController.cs:187-208`) |
| 2.5 | **I** — gateway payment-method id leakage enables cross-account attacks | expose `pm_…` ids at the API boundary | `GatewayPaymentMethodIdProtector` protects ids at the API boundary (`Payments:/Services/GatewayPaymentMethodIdProtector.cs`) |
| 2.6 | **E** — bank-verification micro-deposit brute force | guess the two deposit amounts | attempts bounded by the `BankAccountVerification` flow (stays unverified on mismatch, retries allowed — `PaymentMethodsController.cs:233-256` handlers); Plaid unregistered fails closed to the challenge (`PaymentsModule.cs:48-53`) |

## 3. Flow family — refunds (request, approval, gateway legs, webhook completion)

Entries: `POST payments/{id}:refund` (`PaymentsController.cs:445`), workflow `POST refunds` (`RefundsController.cs:36`), approval `POST refunds/{id}:approve|reject` (`RefundsController.cs:165,189`), execution `RefundProcessingService.ExecuteAsync` (`:336`), webhook completion `CompleteFromGatewayAsync` (`:607+`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 3.1 | **S** — unauthorized refund initiation (money movement) | non-owner requests a refund | controller tenant guard on both entries (`PaymentsController.cs:450`, `RefundsController.cs:51-55`); approvals gated by `payments:refunds:approve` permission on command + queue query (`02-cqrs-wiring-matrix.md` RefundsController rows) |
| 3.2 | **T** — refund more than was collected | request amount > remaining refundable | remainder bound checks (`RefundProcessingService.cs:185-192` exceeds-maximum failure); entity cumulative guard `RefundedAmount + refundAmount > Amount` (`Payments:/Entities/Payment.cs:700-703`); refund-kind proration caps at remaining (`:745+`) |
| 3.3 | **R** — partial-refund race corrupts the cumulative counter (concurrent gateway leg vs webhook leg) | fire two partial refunds concurrently | **fixed via #1266 (was N2)**: `Payment` now saves under the entity `Version` optimistic token (`API`-wide `EntityBase.Version`, `GameGuild.SharedKernel/Entities/EntityBase.cs:62`); `ApplyRefundToPaymentAsync` reloads fresh state on stale-version conflicts and re-applies, bounded 3 attempts (`RefundProcessingService.cs:539-600`); storage-level single-open-refund guard (`Payments:/Repositories/RefundRepository.cs:108`) still backs it |
| 3.4 | **R** — duplicate refund via idempotency-key replay | re-send the same client key | recorded row returned for the same key (`RefundProcessingService.cs:204-210`); open-refund guard rejects a second concurrent row (`:234-247` + storage `OpenRefundAlreadyExistsException`) |
| 3.5 | **I** — webhook claims a refund different from what Stripe moved | forged/edited refund event says "succeeded 50" for a 100 refund | signature verification first (TB3); **fixed via #1266 (was N3)**: the verified event's amount is checked against the local row and mismatches fail closed to reconciliation (`RefundProcessingService.cs:644-653`, wired from `StripePaymentLifecycleEventConsumer.cs` `gatewayReportedAmount: verifiedEvent.Amount`) |
| 3.6 | **T** — currency confusion on refunds (the N1 #1202 class) | refund JPY amount scaled by USD exponent → 100× over-refund | **fixed via #1266 (was N1)**: minor-unit conversion uses the payment's currency (`StripePaymentService.cs:754-768`); regression-pinned by `StripeRefundCurrencyConversionTests` |
| 3.7 | **D** — approver repudiates an approved refund | "I never clicked approve" | approval state transitions on the `Refund` entity (`Payments:/Entities/Refund.cs:200-214`); approver notifications recorded (`RefundProcessingService.cs:1016`); revenue event + ledger entry + audit trail written in-transaction with completion (`:893-941`) |
| 3.8 | **E** — refund workflow stuck after worker crash | refund left `Processing` forever | `RecoverInterruptedAsync` + single-flight gates (`RefundProcessingService.cs:433-434`, `:151`); `RefundWorkflowBackgroundService` retries up to `MaxExecutionAttempts` (default 3) then leaves operator-visible `Failed` (`PaymentsModule.cs:70`) |
| 3.9 | **T** — refund a payment that never succeeded | refund-id on a Failed payment | fail-closed status guard (`RefundProcessingService.cs:665-672` payment must be Succeeded/Disputed/PartiallyRefunded; refusal instead of illegal transition) |
| 3.10 | **S/I** — chargeback already accepted, then a second gateway refund | double money-back | accept-dispute path records already-moved money with `ExecuteAtGateway=false` (`RefundProcessingService.cs:316`, `:712`) — gateway refund id recorded as chargeback-settled, no second gateway call |
| 3.11 | **E** — alias endpoint refunds 0 | `billing/charges/{id}:refund` with omitted amount | **OPEN — N12**: `BillingChargesController.cs:114` still passes `body.Amount ?? 0` where the payments surface coalesces to the remaining balance (`PaymentsController.cs:460-464` post-drift) — domain rejects, but the alias is a UX/consistency hole |
| 3.12 | **I** — dead cancel-refund branch carries a live hazard | `CancelPaymentCommandHandler` refund-on-succeeded branch | **OPEN — N7**: pre-check `CanTransitionTo(Cancelled)` (`Commands/CancelPayment/CancelPaymentCommandHandler.cs:37`) makes the `Status == Succeeded` refund branch (`:55-80`) unreachable (Succeeded→Cancelled is not in the table); the branch's idempotency key embeds a timestamp (`cancel_refund_{id}_{yyyyMMddHHmmss}`, `:58`) — non-idempotent if ever revived. Dead-code trap, not a live path |

## 4. Flow family — fraud gate, review and blacklist

Entry: pre-gateway `AssessPaymentAsync` (`Payments:/Services/FraudDetection/FraudDetectionService.cs:36-39` → `AssessAsync :62-174`), review queue `FraudReviewsController`, blacklist `FraudBlacklistController`, background re-score `FraudBackgroundScoringWorker` (`PaymentsModule.cs:147`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 4.1 | **S** — anonymous/internal reading of fraud statistics | unauthenticated metrics scrape | `[Authorize]` + `[AuthorizeRequest(PaymentsPermission.Keys.Admin)]` on `GetFraudStatisticsQuery` (`Queries/GetFraudStatistics/GetFraudStatisticsQuery.cs:36`); gateway analytics/metrics queries now also carry the Admin permission (verified `Queries/GetGatewayMetrics/GetGatewayMetricsQuery.cs:14`, `GetGatewayAnalyticsQuery.cs:15`) — **N11 closed** by the security batch |
| 4.2 | **T** — attacker suppresses fraud signals (client supplies its own AVS/CVV) | forge "AVS pass" in the payload | controller ignores client AVS/CVV entirely — server-known facts only (`PaymentsController.cs:159-162` comment); malformed BIN is dropped, not trusted (`:191-196`) |
| 4.3 | **R** — background re-score loosens a hold after the fact | late provider says "fine" | escalation is tightening-only (`Payments:/Entities/Payment.cs:818-823` refuses Allow); settled payments get a review flag, never a retroactive money reversal (`:840-845`); no-op escalation skips the write (fix commit `ad6607e9f`) |
| 4.4 | **I** — review resolution race silently overwrites the first decision | two admins resolve concurrently | guard requires `PendingReview` (`Payment.cs:877-881`), but the entity had no concurrency token historically — **now serialized by `EntityBase.Version`** (`EntityBase.cs:62`) with EF conflict exceptions; residual: last-writer-wins on reviewer identity is prevented by the version check failing the save |
| 4.5 | **D** — blacklist / review decisions repudiated | "why was my card blacklisted" | declined review auto-blacklists with expiry recorded (`FraudReviewAutoBlacklister.cs:30-73`); review evidence JSON persisted on the payment (`ProcessPaymentCommandHandler.cs:134-148` analog on subscription side) |
| 4.6 | **E** — deferred re-scoring jobs dropped on restart | queue volatility | **OPEN — N14**: `FraudBackgroundScoringQueue` is an in-process bounded `Channel` (`Services/FraudDetection/FraudBackgroundScoringQueue.cs:47-49`, capacity now configurable) — a full queue or restart silently drops deferred re-scores; the tightening pass never runs |
| 4.7 | **S** — blacklist poisoning across tenants | tenant A blacklists tenant B's instrument | blacklist entries are tenant-scoped (`FraudBlacklistTenantTests` pins the boundary) |

## 5. Flow family — disputes and chargebacks

Entry: Stripe `charge.dispute.*` through the verified inbox (`Billing:/Services/StripeDisputeEventConsumer.cs:25-80`), operator surface `DisputesController` (`api/v1/disputes`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 5.1 | **S** — forged dispute webhook moves a payment to Disputed | craft `charge.dispute.created` | HMAC signature + endpoint pinning (TB3, `StripeWebhookVerifier.cs:15`); inbox idempotency by provider scope (`StripeBillingWebhookService.cs:97-114`); unknown payment fails closed (`StripeDisputeEventConsumer.cs:50` throws `InvalidWebhookPayloadException` → inbox retry, never a guessed target) |
| 5.2 | **R** — duplicate dispute deliveries double the fee/rows | Stripe redelivery | `GatewayDisputeId` lookup reconciles instead of duplicating (`StripeDisputeEventConsumer.cs:54-77`); fee recorded before status so terminal resolution freezes it (`:66-70`, issue #419) |
| 5.3 | **T** — dispute evidence tampering | edit submitted evidence | evidence flows through `AddDisputeEvidenceCommand` + validator and Stripe-side upload receipts (`DisputesController.cs:136+`, `StripePaymentService.cs:452+`); automatic evidence collection is server-composed (`StripeDisputeEventConsumer.cs:117-139`) |
| 5.4 | **I** — operator pushes a dispute to a terminal status outside the FSM | `POST disputes/{id}:respond` with `Won` | **OPEN — N8**: `DisputeService` default branch assigns `dispute.Status = newStatus` for any non-guarded status (`Payments:/Services/DisputeService.cs:81`), bypassing `MarkAsWon`/`MarkAsLost` (`Entities/PaymentDispute.cs:270-277`) and freezing the row out of gateway reconciliation (terminal states ignored at `:211`); no dispute transition table exists |
| 5.5 | **D/E** — dispute deadline missed | reminder worker off | `DisputeDeadlineReminderBackgroundService` is opt-in (`PaymentsModule.cs:79`); missed deadlines are an accepted operational risk, surfaced by the reminder config |
| 5.6 | **T** — accept-dispute then refund anyway (double payout) | operator accepts, then the refund queue also pays | accept routes through the chargeback-settled recording path (`ExecuteAtGateway=false`, §3.10), so the refund workflow never re-calls the gateway for the same money |

## 6. Flow family — webhook ingress (Stripe → platform)

Entry: `POST api/v1/billing/webhooks/stripe` (`BillingWebhooksController.cs:186-189`) → verifier → inbox → consumer chain (`StripeBillingWebhookService.cs:70+`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 6.1 | **S** — webhook forgery (the crown-jewel ingress threat) | sign a fake `payment_intent.succeeded` for a pending payment | HMAC-SHA256 over the raw payload with dual-secret rotation window (`StripeWebhookVerifier.cs:15-177`, tolerance `:165`); payload contract pins livemode, endpoint api-version, connected account (`:59-76`); `WebhookSourceSecurityFilter` + `RateLimitPolicies.Webhook` (`BillingWebhooksController.cs:188-189`); missing signature → 400 without processing (`:204-210`) |
| 6.2 | **R** — webhook replay of an already-processed event | re-send a captured valid delivery | idempotent inbox by `(ExternalEventId, Provider)` — replay returns `AlreadyProcessed` **and raises a replay security event** (`StripeBillingWebhookService.cs:97-114`); legacy signature-unverified rows are never replayed (`:235-245`, issue #408 fail-closed) |
| 6.3 | **T** — event routed to the wrong aggregate (payload swapping) | craft event whose object id collides with another payment | monetary events require the canonical `payment_intent` identity (`StripeWebhookVerifier.cs:224-243`) and provider-object binding validation (`StripeBillingWebhookService.cs:117-122`); disputes resolve only through payment_intent capture leg or legacy id (`StripeDisputeEventConsumer.cs` resolution) |
| 6.4 | **I/D** — processed inbox rows repudiated or lost | "we never got that event" | every delivery is a durable inbox row with attempts; `BillingWebhookProcessedV1`/`FailedV1` outbox events (`StripeBillingWebhookService.cs:310-328`); SystemAdmin replay/inspect endpoints (`BillingWebhooksController.cs:494`, `:462`); raw retention opt-in with redaction (`WebhookRawDeliveryRedactor.cs`) |
| 6.5 | **E** — inbox poison-loop | a consumer throws forever | failed deliveries 500 to Stripe → provider redelivery on Stripe's schedule; dead-letter after the attempt ceiling with a **critical security event** (`Billing:/Services/BillingWebhookDeadLetterService.cs:28-35`); `PaymentSucceeded` reconciliation stays inside the state machine post-#1266 (`StripePaymentLifecycleEventConsumer.cs:88-108` routes Pending via Processing — was N5) so a resolvable Pending row no longer poison-loops |
| 6.6 | **E** — slow consumer blocks the provider window | processing exceeds Stripe's timeout | response-bound CTS acknowledges "deferred" and the inbox retry worker completes (`BillingWebhooksController.cs:394-416`); per-attempt `ProcessingTimeoutSeconds` (`WebhookProcessorBase.cs:254-257`) |

## 7. Flow family — Economy interface (top-up mint and reversal)

Entry: `StripeEconomyTopUpEventConsumer` (`Economy:/Integrations/StripeEconomyTopUpEventConsumer.cs`) in the verified-consumer chain; wallet credit via `BillingInvoicePaidV1` outbox (`API:/Core/Billing/BillingInvoicePaidWalletCreditHandler.cs:30-34`).

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 7.1 | **T** — mint HardCoins without money (fake top-up) | trigger the mint on an unpaid intent | consumer only fires on **verified** `payment_intent.*` success events and requires an existing `EconomyTopUpProviderIdentity` settlement row (`StripeEconomyTopUpEventConsumer.cs:40-44` resolution gate); mint goes through `ConfirmHardCoinFunding` protected-operation authorization; posted-idempotency prevents double mint (`:55-58`) |
| 7.2 | **T/I** — refund a top-up and keep the coins (drain) | pay top-up → receive coins → chargeback | **fixed via #1266 (was N4)**: `charge.refunded` on a top-up intent now drives a provider-reversal of the mint (reversal capability `economy.reverse-provider-funding.v1`, `StripeEconomyTopUpEventConsumer.cs:24-46`, `:161+`), using the cumulative-authoritative `amount_refunded`; previously refund events were released with the mint intact |
| 7.3 | **R** — duplicate wallet credit for one invoice | outbox event redelivered | wallet credit is idempotent per provider event id (`BillingInvoicePaidWalletCreditHandler.cs:30-34`, completed-event rejection `API:/Database/ApplicationDbContext.cs:366-381`); per-consumer inbox receipts ×5 then dead-letter (`OutboxDispatcher.cs:263-292`, `:223-231`) |
| 7.4 | **E** — mint failure stalls the webhook | gateway error mid-mint | failure marks the top-up `Held` and rethrows → inbox retry redelivers the mint attempt (`StripeEconomyTopUpEventConsumer.cs:119-125`) — fail-closed with bounded retry |

## 8. Flow family — configuration surfaces (currencies, FX, pricing, taxes, wallets, gifts, referrals, credits)

Controllers per `02-cqrs-wiring-matrix.md` remaining rows.

| # | Threat (class) | Attack sketch | Mitigation / status |
|---|----------------|---------------|---------------------|
| 8.1 | **S** — tenant user flips base currency / disables currencies | privilege escalation on `currencies/{code}/*` | role gates `Roles = "SystemAdmin,Owner,TenantAdmin,Admin"` (`CurrenciesController.cs:66,85,104`) |
| 8.2 | **T** — FX rate override manipulation to skew conversion | operator pushes a bogus rate | FX operator permission on override/provider-key commands (`FxRatesController` matrix rows, `FxSecurityTests`); variance report exposes drift (`fx-rates/variance`) |
| 8.3 | **I** — anonymous currency suggestion leaks tenant config | scrape suggestions pre-auth | endpoint is intentionally public and data-minimized (country code only, nothing persisted — `Queries/Currencies/GetCurrencySuggestion.cs:25-33`); **the anonymous-500 bug (N9) is fixed** — the `[AuthorizeRequest("")]` attribute was removed (#1266, verified in the query file comment), so the endpoint no longer throws for anonymous callers |
| 8.4 | **T** — wallet balance tampering via endpoints | direct `AddFunds`/`TransferFunds` abuse | ownership/tenant guards + validators on every wallet mutation (wallet matrix rows); wallet audit log queryable; reconciliation report + metrics workers (`WalletsController` matrix) |
| 8.5 | **R** — promo/gift code abuse | reuse a gift code across accounts | `RedeemGiftCommand` is idempotent/owned (`GiftsController` matrix); subscription-side promo codes are quoted server-side fail-closed (see billing-orders STRIDE §1); account-credit grants are admin-gated (`AccountCreditsController` matrix) |
| 8.6 | **D/E** — revenue auditing blind to money events | anomaly alerts unacknowledged forever | `RevenueAuditingController` admin-gated with ack trail (`RunRevenueReconciliation`, `AcknowledgeRevenueAnomalyAlert` matrix rows) |
| 8.7 | **I** — payment money events invisible to central compliance | audit split across module-local tables | **OPEN — N15**: `RevenueAuditService` writes module-local `revenue_events`/`financial_ledger_entries`/`audit_trails` only; no projection into `GameGuild.Compliance.Audit` (only Billing's webhook-secret rotation reaches `IAuditService`, `Billing:/Commands/RotateWebhookSecret/RotateWebhookSecretCommand.cs:126-141`) |

## 9. Residual risk register (this model's outputs)

| Id | Item | Class | Status |
|----|------|-------|--------|
| R-P1 | Dispute status settable arbitrarily via respond (no transition table) | T/I | OPEN (N8) — `DisputeService.cs:81` |
| R-P2 | Ten controllers without rate-limit policy, incl. anonymous suggestion | E | OPEN (N10) |
| R-P3 | BillingCharges refund alias defaults amount to 0 | T/UX | OPEN (N12) — `BillingChargesController.cs:114` |
| R-P4 | Dead cancel-refund branch with timestamped idempotency key | T (latent) | OPEN (N7) |
| R-P5 | Fraud background re-score queue volatile | E | OPEN (N14) |
| R-P6 | Payment money events not projected into Compliance.Audit | I/D | OPEN (N15) |
| R-P7 | Outcome-unknown payments depend on webhook or aging sweep for exit | E | Mitigated (aging sweep, #1266); residual = window before sweep timeout |
| R-P8 | Fail-open posture on outcome-unknown is deliberate | D | Accepted (K5 in `06-gaps.md`) — double-charge avoidance outranks fast failure |

## 10. Verification notes (audit of this package's functional docs)

Audited `01`–`06` against `develop 21b58ac6a7` (docs were written against `74e9bba4c`). 20+ citations spot-checked; results:

**Confirmed fixed by #1266 (docs 06-gaps.md now stale where it lists them as open):**
- N1 refund currency — `StripePaymentService.cs:754-768` now uses the payment currency; the doc's "hardcodes USD" claim at `:741-743` is obsolete.
- N2 refund concurrency — `RefundProcessingService.cs:539-600` `ApplyRefundToPaymentAsync` with `EntityBase.Version` (`GameGuild.SharedKernel/Entities/EntityBase.cs:62`); doc's "no concurrency token" claim obsolete.
- N3 webhook amount verification — `RefundProcessingService.cs:644-653` fail-closed mismatch; consumer passes `gatewayReportedAmount: verifiedEvent.Amount`.
- N4 top-up refund mint reversal — `Economy:/Integrations/StripeEconomyTopUpEventConsumer.cs:24-46,161+`.
- N5 illegal Pending→Succeeded — `StripePaymentLifecycleEventConsumer.cs:88-108` routes via `MarkAsProcessing`.
- N6 Pending limbo — new `PendingPaymentAgingService.cs` + `PaymentsModule.cs:169-174`.
- N9 anonymous currency 500 — attribute removed (`Queries/Currencies/GetCurrencySuggestion.cs` issue comment).
- N13 dead `PaymentSucceededEvent` — file deleted; `Events/` contains only `PaymentMethodEvents.cs`.
- N11 gateway metrics permission — `AuthorizeRequest(PaymentsPermission.Keys.Admin)` now present on both queries (`GetGatewayMetricsQuery.cs:14`, `GetGatewayAnalyticsQuery.cs:15`) — fixed by the security batch (#1269).

**Still open (verified in current code):** N7 (`CancelPaymentCommandHandler.cs:37` pre-check + dead branch `:55-80`, timestamped key `:58`), N8 (`DisputeService.cs:81` default branch), N10 (0 rate-limit attributes on Currencies/Pricing/Gifts controllers), N12 (`BillingChargesController.cs:114` `body.Amount ?? 0`), N14 (in-process `Channel` at `FraudBackgroundScoringQueue.cs:47-49`, capacity now configurable), N15 (no `IAuditService` in `RevenueAuditService.cs`).

**Citation drift (line-number-only, content still correct):**
- `PaymentsController.cs` citations drifted ~+8 lines (POST payments 122→130, cancel 390→398, refund 437→445); tenant guard `:138`, `NormalizeCardBin :191` verified exact.
- `RefundProcessingService.cs` drifted ~+11 (RequestRefundAsync 142→153, ExecuteAsync 325→336); single-open-refund `:246`, approver notify `:297`, gateway-settled `:316` verified at new lines.
- `RefundsController.cs:36,137,165,189`, `StripeDisputeEventConsumer.cs:31,50,69-70,103,109`, `FraudDetectionService.cs:36,146-147`, `StripeWebhookVerifier.cs:15`, `StripeBillingWebhookService.cs:97-114`, `BillingWebhooksController.cs:186-189`, `GatewayPaymentResultBinder.cs:10-22`, `GatewayCredentialStore.cs:93-94`, `Payment.cs:26-38` all verified exact.

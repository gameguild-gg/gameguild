# Payments Gaps — known issues vs NEW findings

> Module `GameGuild.Commerce.Payments` · Documented against `develop` commit `74e9bba4c`.
> Citation shorthand: `Payments:` = `apps/api/Source/Modules/GameGuild.Commerce.Payments/`, `Billing:` = `apps/api/Source/Modules/GameGuild.Commerce.Billing/`, `Economy:` = `apps/api/Source/Modules/GameGuild.Finance.Economy/`.

## Known issues (already tracked, fixed, or deliberately accepted)

| # | Issue | Evidence | Status |
|---|-------|----------|--------|
| K1 | **Fraud-statistics date bug** — weekly trend buckets dropped future-dated attempts when the test seeded the current ISO week; window filter cuts anything created after "now" (`Queries/GetFraudStatistics/GetFraudStatisticsQuery.cs:251` with `BucketStart :495-508`) | test anchored to previous ISO week in commits `94aab401e` and `c40b4c80b` (`tests/.../FraudDetection/GetFraudStatisticsQueryHandlerTests.cs:235-247`) | test-side fixed 2026-11-02; handler still silently swaps reversed windows instead of rejecting (`:238-241`) — accepted behavior |
| K2 | **CodeQL info-alerts on Stripe services** — `cs/exposure-of-sensitive-information` on email logging | suppression annotations with redaction proof at `Services/StripeCustomerService.cs:45,78,90`; batch-fixed in PR #1163 (commit `eb7847752`, "49 medium MaskEmail") | annotated; remaining alerts are info-level with `LogRedaction.MaskEmail` applied |
| K3 | Zero-amount "full refund" historical bug — null amount produced a 0 refund rejected by the domain | fixed by remainder coalescing (`Controllers/PaymentsController.cs:460-464`) | fixed (issue #419) |
| K4 | Background fraud re-score performed a redundant no-op persistence write | escalation now returns false and callers skip the write (`Entities/Payment.cs:796-798`; fix commit `ad6607e9f`) | fixed |
| K5 | Gateway outcome unknown (timeout/5xx) deliberately fails **open** — payment stays `Processing` to avoid double-charging | `Services/StripePaymentService.cs:701-715`; handler keeps claim `Commands/ProcessPayment/ProcessPaymentCommandHandler.cs:293-299` | deliberate; reconciliation by webhook inbox |
| K6 | Fraud background re-score can only tighten an Allow, never loosen | `Entities/Payment.cs:813-823` | deliberate (issue #409) |
| K7 | Webhook raw-payload retention (PII risk) | opt-in, default off (`BillingWebhooksController.cs:429-443`; `docs/billing/webhook-raw-payload-retention.md`) | deliberate |
| K8 | Legacy webhook inbox rows without verification proof are never replayed | fail-closed guard (`Billing:/Services/StripeBillingWebhookService.cs:235-245`) | deliberate (issue #408) |

## NEW gaps found by this audit

### Money-critical

**N1 — Refund amount conversion hardcodes USD for every currency.**
`Services/StripePaymentService.cs:741-743`: `createOptions.Amount = StripeAmountConverter.ToStripeAmount(request.Amount.Value, "USD");` — while `GatewayRefundRequest` carries the payment's real currency (`Abstractions/IPaymentGateway.cs:246`, populated at `Services/RefundProcessingService.cs:547`). `StripeAmountConverter` scales by the currency's minor-unit exponent (`Services/StripeHelpers.cs:16-24`): for a zero-decimal currency (JPY) a ¥9,000 refund is sent to Stripe as 900,000 (100× over-refund); for a three-decimal currency (KWD) it under-refunds. The charge leg does this correctly (`StripePaymentService.cs:601`). Tests only exercise USD (`tests/.../StripePaymentSafetyTests.cs`, refund suites).

**N2 — Concurrent refund legs can corrupt the cumulative refund counter (no concurrency token).**
`Payment` has no row version / concurrency token (`Data/Configurations/PaymentConfiguration.cs`; entity `Entities/Payment.cs` — none). The gateway leg (`Services/RefundProcessingService.cs:384`) and the webhook-completion leg (`:615`) both read-modify-write `RefundedAmount += amount` through `ProcessRefund` (`Payment.cs:705`). Two concurrent partial refunds each pass the `RefundedAmount + refundAmount > Amount` guard (`:700-703`) on stale copies; last write wins and the recorded cumulative undercounts what the gateway actually refunded. The refund *rows* are protected by the storage-level single-open-refund guard (`Repositories/RefundRepository.cs:108`); the payment aggregate is not.

**N3 — Webhook completion does not verify the refunded amount.**
`RefundProcessingService.CompleteFromGatewayAsync` (`:552-631`) completes the local refund row with `refund.Amount` regardless of the amount Stripe reports; the verified event carries `CumulativeRefundedAmount` (`Billing:/Services/StripeWebhookVerifier.cs:121`) but no consumer applies it. A partial refund executed at the gateway for less than requested (rare but possible after currency/fee adjustments) is recorded locally at full value.

**N4 — Top-up refunds never reverse the Economy mint.**
`Economy:/Integrations/StripeEconomyTopUpEventConsumer.cs:128-136` maps only forward `payment_intent.*` statuses; `refund.*` events for a top-up payment intent return `false` (released) and the minted HardCoins stay in the wallet after Stripe refunded the money. Combined with N1's currency scaling this is a drain path on refunded top-ups.

### Correctness / state-machine

**N5 — The webhook's `Pending → Succeeded` reconciliation branch is illegal per the entity's own transition table.**
`Billing:/Services/StripePaymentLifecycleEventConsumer.cs:89-93` calls `MarkAsSucceeded` for payments in `Pending`, `Processing` or `RequiresAction` — but `Payment.ValidTransitions` (`Entities/Payment.cs:26`) permits `Pending → {Processing, Cancelled, Failed}` only, so a resolvable Pending payment would throw `InvalidOperationException` (`Payment.cs:421-431`), fail the inbox delivery and poison-loop its retries. The branch is currently dead only because an unmapped Pending payment is also unresolvable (no provider mapping and no `ExternalPaymentId`, `StripePaymentLifecycleEventConsumer.cs:317-339`) — which itself means:

**N6 — Outcome-unknown payments left `Pending` (unmapped Stripe) have no automated exit.**
`GatewayPaymentResultBinder.PrepareProcessingClaim` (`Services/GatewayPaymentResultBinder.cs:10-22`) deliberately leaves unmapped Stripe payments `Pending`; the retry queue only selects `Failed` rows (`Queries/GetRetryingPaymentCandidates/GetRetryingPaymentCandidatesQuery.cs`); the webhook cannot resolve them (N5); `Cancel` requires an operator. They need a manual sweep.

**N7 — `CancelPaymentCommandHandler`'s refund-on-succeeded branch is dead code.**
The `CanTransitionTo(Cancelled)` pre-check (`Commands/CancelPayment/CancelPaymentCommandHandler.cs:37-51`) returns failure for `Succeeded` payments (Succeeded may only go to Refunded/PartiallyRefunded/Disputed, `Payment.cs:29`), so the gateway-refund-then-`ProcessRefund` path at `:53-99` can never execute. Behavior is correct (matches the controller doc "use refund instead"); the 47 dead lines are a trap for future edits. Also: cancelling a `Processing` payment (`Payment.cs:27` allows it) never calls `IPaymentGateway.CancelPaymentAsync` — a Stripe intent that later settles conflicts with the terminal local row.

**N8 — Dispute status can be set arbitrarily through the respond action.**
`Services/DisputeService.cs:75-85`: the `default` branch assigns `dispute.Status = newStatus` for any status other than the three guarded ones — an operator can push a dispute to `Won`/`Lost`/`Resolved`/`Cancelled` via `POST disputes/{id}:respond` (`Controllers/DisputesController.cs:226`), bypassing `MarkAsWon`/`MarkAsLost` (`Entities/PaymentDispute.cs:270-277`) and freezing it out of later gateway reconciliation (terminal states ignored at `:211`). There is no dispute transition table equivalent to `Payment.ValidTransitions`.

### API surface

**N9 — Anonymous currency suggestion returns 500 (the Session-state log failure).**
`GetCurrencySuggestionQuery` carries `[AuthorizeRequest("")]` (`Queries/Currencies/GetCurrencySuggestion.cs:32`) — an empty permission meaning "no permission required" — but `AuthorizationBehavior` throws for unauthenticated actors **before** the empty-permission skip (`Identity.Authorization/Behaviors/AuthorizationBehavior.cs:66-69` vs `:105-108`). The endpoint is `[AllowAnonymous]` (`Controllers/CurrenciesController.cs:43`) and registered for anonymous access (`GameGuild.API/Security/AnonymousEndpointRegistry.cs:112`), its handler explicitly supports anonymous callers (`GetCurrencySuggestion.cs:51-69`), and the controller has no catch — the exception surfaces as a 500 to every anonymous visitor. Unit tests invoke the handler directly (`tests/.../FxSecurityTests.cs:284-313`) so the pipeline break is invisible to the suite. Fix candidates: remove the attribute, or move the empty-permission skip above the authentication check.

**N10 — Twelve controllers ship with no rate-limit policy.**
`Currencies` (incl. the anonymous `suggestion` endpoint), `CurrencyConversion`, `FxRates`, `Pricing`, `PriceLocalization`, `Taxes`, `TaxJurisdictions`, `TaxRules`, `Gifts`, `Referrals`, `AccountCredits`, `RevenueAuditing` — no `[EnableRateLimiting]` at controller or action level (see matrix, `02-cqrs-wiring-matrix.md`), against the module's own stated convention ("Rate limiting is applied to prevent DoS attacks and abuse", `Controllers/PaymentsController.cs:15-19`).

**N11 — Gateway metrics/analytics document `payments:admin` but do not enforce it.**
Endpoint docs claim "Requires the payments:admin permission" (`Controllers/PaymentsController.cs:652-654, 684-691`) while `GetGatewayMetricsQuery`/`GetGatewayAnalyticsQuery` carry no `[AuthorizeRequest]` — any authenticated tenant user can read per-gateway success rates, error-code distributions and cost analyses.

**N12 — BillingCharges refund alias defaults the amount to 0.**
`Controllers/BillingChargesController.cs:114` passes `body.Amount ?? 0` where the payments surface coalesces null to the remaining balance (`PaymentsController.cs:460-464`) — the alias endpoint always fails a "refund everything" request with a domain rejection instead.

### Events / integration

**N13 — `PaymentSucceededEvent` is raised and then discarded.**
`Entities/Payment.cs:475-481` raises it; `ApplicationDbContext` collects domain-event entities (`API:/Database/ApplicationDbContext.cs:179-182`) and only clears them (`:348-351`) — no publisher, no consumer anywhere in the tree. Downstream reactions run as direct service calls instead (`ProcessPaymentCommandHandler.cs:340-356`). Either route the event through the outbox (giving Economy/Compliance a durable hook) or delete the dead event type.

**N14 — Fraud background re-scoring queue is volatile.**
`Services/FraudDetection/FraudBackgroundScoringQueue.cs:23-40` is a bounded in-process channel: a full queue or a process restart silently drops deferred re-scoring jobs, so an allowed-but-suspicious payment never gets its tightening pass — no durability, no replay, no metric on drops beyond a log warning.

**N15 — Payment money events are not projected into Compliance.Audit.**
Refund/chargeback/success audits go to the module-local `audit_trails` table via `RevenueAuditService.RecordAuditTrailAsync` (`Services/RevenueAuditService.cs:131`); the central compliance module's `IAuditService.LogAsync` is invoked by Billing only for webhook-secret rotation (`Billing:/Commands/RotateWebhookSecret/RotateWebhookSecretCommand.cs:126-141`). Retention, chain-of-custody and security aggregation of `GameGuild.Compliance.Audit` do not cover payment money movement.

## Priority suggestion

1. N1 (wrong refund amounts at Stripe for non-USD) — immediate, small fix, money loss.
2. N9 (anonymous 500 on a public pricing endpoint) — immediate, breaks first-run UX.
3. N2 + N3 (refund counter integrity) — add a concurrency token and amount verification.
4. N4 (top-up refund mint reversal) — pairs with N1 for a combined drain.
5. N5/N6 (Pending limbo + illegal transition) — align the transition table with the consumer's intent, add a sweep for unmapped Pending payments.
6. N8, N11, N12 (authorization/alias inconsistencies) — quick hardening.
7. N13/N14/N15 (event architecture) — design decisions for the outbox rollout.

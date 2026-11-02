# Gap Register — Billing / Orders / Subscriptions (known vs NEW)

> Documented against `develop` commit `74e9bba4c`. Every entry carries file:line. "Known" = already documented in the repo's docs or explicitly acknowledged in code comments before this audit; "NEW" = surfaced by this documentation pass. Nothing here is a behavior change — this is a register, not a fix proposal.

## NEW gaps

### G-1 — Manual invoice-payment retry endpoint schedules nothing (stub)
`GameGuild.Commerce.Billing/Commands/RetryInvoicePayment/RetryInvoicePaymentHandler.cs:27-37` returns `Accepted: true` with message "accepted for local scheduling. External gateway capture requires provider configuration" — but no scheduler, queue entry, outbox event or Payments call exists anywhere for that "local scheduling" (`RetryScheduledAt` is just `UtcNow`, `:36`). The endpoint `POST api/v{v}/billing/invoices/{invoiceId}/retry` (`Controllers/BillingInvoicesController.cs:18-25`) therefore acknowledges retries that never happen. Also: no validator, no ownership/tenant check on the invoice id.

### G-2 — Invoice `PastDue` and `Uncollectible` are unreachable in production code
`Invoice.MarkPastDue` (`GameGuild.Commerce.Billing/Entities/Invoice.cs:438-447`) and `Invoice.MarkUncollectible` (`:424-433`) have **zero callers** across the codebase (grep: only definitions and `Entities/InvoiceTests.cs`). No aging job sweeps `DueDate`, no bad-debt write-off path exists. The invoice FSM's overdue/write-off half (`InvoiceStatus.PastDue`/`Uncollectible`, `:497,500`) is dead code; dunning in practice lives entirely on the subscription (`PastDue`) and Payments retry states, so an invoice that is opened but never paid (e.g., account-credit path leaves it `Open` when credits fall short, `:411-416`) can age forever with no terminal outcome.

### G-3 — `SubscriptionStatus.Expired` is unreachable
The transition table allows `Trialing → Expired` (`GameGuild.Commerce.Subscriptions/Entities/Subscription.cs:32`) and `RecordPayment` guards against it (`:654-656`), but no method writes `Expired` and no caller sets it (grep: only guards/specs). Trials that end unconverted go to `Cancelled` instead (`EndTrial(false)` `:356-358`). Consumers keying on Expired (`Services/SubscriptionPaymentMethodCoordinationService.cs:35`, `Services/CurrencySubscriptionUsageProbe.cs:30`) branch on a state that cannot occur.

### G-4 — Order refund handler: wrong status set, no gateway refund, no ownership check
`RefundOrderCommandHandler` (`GameGuild.Commerce.Orders/Commands/RefundOrder/RefundOrderCommandHandler.cs:24-27`) accepts only `Completed`/`PartiallyRefunded` orders, yet the FSM allows refunds from `Fulfilled` (`Entities/Order.cs:49`) — every order that went through the modern `Pending→Processing→Paid→Fulfilled` flow (the documented happy path) **cannot be refunded via the API**. The handler never calls Payments to issue a gateway refund — it revokes entitlements/restores inventory locally and flips local status (`:36-52`); the customer's money is not returned by this endpoint. It also skips the `OrderActorContext.Authorize` ownership check that capture/complete enforce (`Commands/CaptureOrder/CaptureOrderCommandHandler.cs:26`, `Commands/CompleteOrder/CompleteOrderCommandHandler.cs:30`).

### G-5 — `Order.ReleasePaymentReservation` bypasses the state machine and the audit trail
`Entities/Order.cs:358-367` assigns `Status = OrderStatus.Pending` directly instead of `TransitionTo(...)`: no FSM validation, and — unlike every other transition (`TransitionToWithReason :187-200`, `OnStatusChanged :206-215`) — **no `OrderStateChangedEvent` is raised**, so `order_audit_logs` has no record that a payment reservation was released (triggered on capture decline, `Commands/CaptureOrder/CaptureOrderCommandHandler.cs:129-143`).

### G-6 — Ownership-check asymmetry in Orders handlers
Capture and complete call `OrderActorContext.Authorize` (`Commands/CaptureOrder/CaptureOrderCommandHandler.cs:26-28`, `Commands/CompleteOrder/CompleteOrderCommandHandler.cs:30-32`); cancel, update, delete, refund, hold and release do not (`Commands/CancelOrder/CancelOrderCommandHandler.cs`, `Commands/UpdateOrder/UpdateOrderCommandHandler.cs`, `Commands/DeleteOrder/DeleteOrderCommandHandler.cs`, `Commands/RefundOrder/RefundOrderCommandHandler.cs`, `Commands/HoldOrder/HoldOrderCommandHandler.cs`, `Commands/ReleaseOrder/ReleaseOrderCommandHandler.cs` — none reference the actor context). Controller permissions (`RequirePermission(OrdersPermission.Keys.*)`, `Controllers/OrdersController.cs`) gate by permission, not ownership; a principal holding the Create permission can mutate other users' pending orders within tenant scope.

### G-7 — Orders controller has no rate-limit policy
`Controllers/OrdersController.cs:16-18` carries `[Authorize]` only — no `[EnableRateLimiting]` at controller or action level, unlike the sibling commerce surfaces (Billing invoices `Api`, subscription lifecycle `ExpensiveOperations`, webhooks `Webhook`). Order creation/capture (charge-attempting endpoints) are the least rate-limited commerce surface.

### G-8 — Five subscription query endpoints have no handler anywhere (runtime 500s)
These queries are 5-line record definitions with **no handler class**, and handlers are only registered by assembly scan (`GameGuild.API/Core/Setup/ApplicationLayerExtensions.cs:133-145`) — so `sender.Send` fails at runtime:
- `GetSubscriptionUsageQuery` (`Queries/GetSubscriptionUsage/GetSubscriptionUsageQuery.cs:5`) ← GET `{id}/usage` (`Controllers/SubscriptionBillingController.cs:92`)
- `GetSubscriptionBillingHistoryQuery` (`Queries/GetSubscriptionBillingHistory/GetSubscriptionBillingHistoryQuery.cs`) ← GET `{id}/billing-history` (`Controllers/SubscriptionBillingController.cs:109`)
- `GetSubscriptionPlanUsageStatisticsQuery` (`Queries/GetSubscriptionPlanUsageStatistics/GetSubscriptionPlanUsageStatisticsQuery.cs`) ← GET `{planId}/usage` (`Controllers/SubscriptionPlanOperationsController.cs:29`)
- `SuggestSubscriptionPlanUpgradesQuery` (`Queries/SuggestSubscriptionPlanUpgrades/SuggestSubscriptionPlanUpgradesQuery.cs`) ← GET `{planId}/suggest-upgrades` (`Controllers/SubscriptionPlanOperationsController.cs:47`)
- `ValidateSubscriptionPlanLimitsQuery` (`Queries/ValidateSubscriptionPlanLimits/ValidateSubscriptionPlanLimitsQuery.cs`) ← POST `{planId}:validate-limits` (`Controllers/SubscriptionPlanOperationsController.cs:81`)
(For contrast, `CalculatePricingQuery` used by the same controller lives in Payments with a real handler, `GameGuild.Commerce.Payments/Queries/CalculatePricing/CalculatePricingQueryHandler.cs:15`.)

### G-9 — Destructive subscription endpoints guarded by bare `[Authorize]`
`DELETE v{v}/subscriptions/{id}` (`Controllers/SubscriptionsController.cs:270`) and PUT/PATCH (`:218,244`) require only authentication — no `RequirePermission` (compare plan CRUD which requires `MonetizationPermission.Configure`, `Controllers/SubscriptionPlansCrudController.cs:30,224,243`), no validators on the delete/patch/update commands. Any authenticated principal can hard-delete any subscription they can address (tenant scoping depends on query-level filters, not the handler).

### G-10 — Pause clobbers fraud evidence; resume force-enables auto-renew
Pause aliases to `Suspend()` (`Commands/PauseSubscription/PauseSubscriptionHandler.cs:20-21`) and `Subscription.Suspend` **replaces** `Metadata` with `{"suspensionReason":...}` (`Entities/Subscription.cs:457`). Creation-time fraud-review evidence is stored in that same `Metadata` field (`Commands/CreateSubscription/CreateSubscriptionCommandHandler.cs:134-140`), so pausing (or any suspension) destroys the review evidence. Resume aliases to `Reactivate()` (`Commands/ResumeSubscription/ResumeSubscriptionHandler.cs:20`) which force-sets `AutoRenew = true` (`Entities/Subscription.cs:468`) — a user "resuming" silently re-enables billing even if they had auto-renew off.

### G-11 — Successful payment does not heal `PastDue` subscriptions
`Subscription.RecordPayment` (`Entities/Subscription.cs:641-721`) records the payment and advances the period regardless of `PastDue` status but never transitions back to `Active`. Automatic reactivation exists only for dunning **suspended** subscriptions (`Services/PaymentDunningLifecycleService.cs:74-109`), and the webhook payment-success path (`Services/BillingWebhookService.cs:168-225`) calls no reactivation. A subscription that failed once, then paid, stays `PastDue` (with a fresh paid period) until an operator calls `:reactivate` — `HasActiveSubscription`/`IsActive`-keyed features (`:287`) treat it as inactive the whole time.

### G-12 — `OrderStatus.Disputed` is unreachable (disputes never reach the order)
The FSM declares `Fulfilled/Completed/PartiallyRefunded → Disputed` and `Disputed → Fulfilled/Completed/Refunded` (`Entities/Order.cs:48-55`), but no code path writes `OrderStatus.Disputed`: the Stripe dispute consumer marks the **Payment** disputed (`GameGuild.Commerce.Billing/Services/StripeDisputeEventConsumer.cs:109` `payment.MarkAsDisputed()`), never the Order. Orders-side dispute handling is dead FSM scaffolding.

### G-13 — Client-supplied subscription amount is authoritative on the create endpoints
Both create surfaces pass the request-body `Amount` straight into the entity: `Controllers/SubscriptionsController.cs:56-64` and `Controllers/BillingSubscriptionsController.cs:86-94` (`body.Amount`), the validator only enforces `Amount >= 0` (`Commands/CreateSubscription/CreateSubscriptionCommandValidator.cs:18`), and the handler uses `promoQuote?.FinalAmount ?? request.Amount` (`Commands/CreateSubscription/CreateSubscriptionCommandHandler.cs:84`) — the plan's authoritative interval price (`Services/SubscriptionLifecycleService.cs:37-38`) is never resolved or compared at creation. A direct API caller can create a subscription at 0.00 or any amount; renewal billing then uses that locked `Amount` (`Entities/Subscription.cs:565-569`). Legitimate checkout flows may compute the amount server-side upstream, but the endpoint contract itself has no price-integrity guard (contrast: the pricing-decision ledger records the *decision*, not a plan-price check, `Services/SubscriptionBillingService.cs:48-88`).

### G-14 — Dead command pair in Billing
`ProcessBillingWebhookCommand` + `ProcessBillingWebhookCommandValidator` (`GameGuild.Commerce.Billing/Commands/ProcessBillingWebhookCommand/`) have no handler and no controller caller (each provider has its own dedicated command). Inert code retained in the module surface.

## Known / documented-by-design items (for completeness)

| # | Item | Evidence | Status |
|---|------|----------|--------|
| K-1 | Automated payment-retry queue is opt-in and **disabled by default** — subscription dunning depends on an operator flipping `Payments:RetryQueue:Enabled` | `GameGuild.Commerce.Payments/Services/PaymentRetryQueueWorker.cs:113-130`; documented in `docs/billing/payment-retry-and-dunning.md` ("Automated retry queue", lines 64-87) | known |
| K-2 | Webhook raw-payload retention is opt-in; expired deliveries purged per cycle | `Services/BillingWebhookRetryWorker.cs:150-174`; `docs/billing/webhook-raw-payload-retention.md` | known |
| K-3 | Pause/resume alias to suspend/reactivate ("entity doesn't have dedicated pause properties") | code comments `Commands/PauseSubscription/PauseSubscriptionHandler.cs:8,20`, `Commands/ResumeSubscription/ResumeSubscriptionHandler.cs:8` — the aliasing is acknowledged; its **consequences** are the NEW G-10 | partially known |
| K-4 | Failed plan-change sagas are not auto-resumed; replays return the recorded terminal outcome until a new effective date is used | `Services/SubscriptionChangeSagaOrchestrator.cs:93-101` + design comment `:14-16` ("resumable-by-evidence") | known by design |
| K-5 | Subscriptions without `FulfilledOrderId` only log a warning (legacy/migration path) | `Commands/CreateSubscription/CreateSubscriptionCommandHandler.cs:119-126` | known (explicit log message) |
| K-6 | Provider-billed renewals trust the provider's subscription clock; only first-party payments enforce `LastProcessedBillingCycle` monotonicity | `Services/BillingWebhookService.cs:168-225` vs `Entities/Subscription.cs:696-703` | architectural, documented here for the record |
| K-7 | Outbox events for webhook processed/failed/renewed/cancelled have no registered consumers (observability-only streams) | `GameGuild.API/Core/Eventing/EventTransportServiceCollectionExtensions.cs:18-26` registers only the wallet-credit consumer | known (events exist deliberately, saga events carry `required: true`) |

## Suggested follow-up issue slices

1. G-1 + G-2 together: either wire the retry endpoint to the Payments retry queue + add an invoice aging worker (`PastDue`/`Uncollectible`), or mark the endpoint clearly as a no-op ack and remove the dead states.
2. G-4: extend the refund handler to `Fulfilled`, route through Payments `ProcessRefundAsync` (the saga already demonstrates the compensated-refund pattern, `Services/SubscriptionChangeSagaOrchestrator.cs:377-400`), and add the ownership check.
3. G-8: implement the five missing query handlers (read-model queries; `GetSubscriptionInvoicesHandler` is the pattern, `Queries/GetSubscriptionInvoices/GetSubscriptionInvoicesHandler.cs`).
4. G-10/G-11: stop overwriting `Metadata` on suspend (merge instead), and auto-heal `PastDue → Active` on confirmed cycle payment inside `RecordPayment`.
5. G-13: resolve the plan's interval price server-side at creation (or assert equality with the supplied amount) before persisting the subscription.

# Products — event inventory

> Module `GameGuild.Commerce.Products` · Documented against `develop` commit `60100459e6`.
> Citation shorthand: `Products:` = `apps/api/Source/Modules/GameGuild.Commerce.Products/`, `API:` = `apps/api/Source/GameGuild.API/`.

## 1. Use-case event contracts (45 commands)

The module's entire observable contract is the platform's **generic durable operation events**: every command carries a `UseCaseEventContractAttribute` (`Products:/Architecture/UseCaseEventContracts.g.cs:1-46`, plus `PricingRuleUseCaseEventContracts` entries `:1-6` and `PricingExperimentUseCaseEventContracts.cs`). **No named cross-module domain event is declared by any of them** — every row's reason is "the durable generic operation event remains the observable contract".

Quota-impacting commands (surface for the platform quota system):

| Command | Use case | Quota impact |
|---|---|---|
| `CreateProductCommand` | `commerce.products.create-product` | `+1 Products` (`UseCaseEventContracts.g.cs:17`) |
| `CreatePromoCodeCommand` | `commerce.products.create-promo-code` | `+1 PromoCodes` (`:19`) |
| `GenerateBulkPromoCodesCommand` | `commerce.products.generate-bulk-promo-codes` | `+N PromoCodes` (`:31`) |

Full contract families (45 total): product CRUD/lifecycle (create/update/patch/batch/activate/deactivate/archive/delete), pricing (set-product-pricing, set-product-inventory), categories (create/update/delete), promo codes (create/update/patch/delete/activate/deactivate/validate/apply/bulk-generate), campaigns, stacking rules (create/update/delete), entitlements (grant/revoke product access, grant/revoke entitlement, check-multiple), support tickets (create/start/assign/priority/resolve/close/reopen/message), pricing rules (create/update/delete/activate/deactivate, calculate-pricing), pricing experiments (via `PricingExperimentUseCaseEventContracts.cs`).

Transport: the platform `OutboxDispatcher` rail (`API:/Core/Eventing/OutboxDispatcher.cs`) — 5-attempt/≤900s backoff, `DeadLetteredAtUtc`, tombstones, replay (the rail the Economy module notably does not use).

## 2. What the module does NOT publish

| Absent stream | Why it matters |
|---|---|
| No `ProductCreated/Updated/Published` domain events | sibling modules learn about catalog changes only by querying (`CheckProductAccessQuery` is pulled by Orders/Assets, not pushed) |
| No price-change event | a `SetProductPricing` change never notifies carts/checkouts; they must re-read (see `05-cross-module-sequences.md` §5 for the billing-consistency consequence) |
| No promo-usage event onto the platform outbox | campaign performance is computed from tables (`GetCampaignPerformanceQuery`), not from events |

## 3. Cross-module contracts implemented by the module (consumed by siblings)

| Contract | Implementation | Consumer | Registration reality |
|---|---|---|---|
| `GameGuild.Commerce.ISubscriptionPromoCoordinator` | `Products:/Services/SubscriptionPromoCoordinator.cs:16` (issue #418 — quote + record usage at subscription creation/invoicing) | `Subs:/Commands/CreateSubscription/CreateSubscriptionCommandHandler.cs:22` (+ invoice materialization) | **never registered in host** — consumer null → hard failure when promo codes supplied (P2) |
| `GameGuild.Commerce.Payments.IPromoDiscountCalculator` | `Products:/Services/PromoDiscountCalculatorAdapter.cs` (issue #417 — the Products promo engine backs the Payments-owned discount abstraction) | Payments pricing surfaces | **explicitly registered** (`API:/Core/Setup/InfrastructureLayerExtensions.cs:281-282`) — live |
| `GameGuild.Commerce.IFraudReviewTicketCoordinator` | `Products:/Services/FraudReviewSupportTicketCoordinator.cs:19` (issue #409 — fraud manual reviews raise support tickets) | `Payments:/Commands/ProcessPayment/ProcessPaymentCommandHandler.cs:27`; `Subs:/Commands/SubscriptionChangeFraudGate.cs:66` | **never registered** — consumers null → reviews silently skip ticket creation (P3) |

## 4. Security-event emission (promo anomaly path)

`ValidatePromoCodeCommandHandler` documents anomaly publication through the central pipeline when validation velocity crosses thresholds (`Commands/ValidatePromoCode/ValidatePromoCodeCommandHandler.cs:14-18`), implemented via `IPromoUsageAnomalyEvaluator` — which is null in the composed host (P4), so **no promo anomaly security event can currently be emitted**. This is a Compliance.Audit sink that silently starves (the audit package's event inventory should be read with this producer missing).

## 5. Retry / DLQ posture

| Stream | Attempts | Dead-letter | Replay |
|---|---|---|---|
| Generic operation events (45 commands) | platform rail 5×/≤900s | `DeadLetteredAtUtc` + tombstone | dispatcher replay |
| Promo usage recording (apply path) | in-transaction (`ApplyPromoCodesCommandHandler` atomic recorder) | n/a | `IdempotencyKey` unique index (`ProductsModule.cs:232-236`) |
| Anomaly security events | n/a — producer dormant (P4) | n/a | n/a |

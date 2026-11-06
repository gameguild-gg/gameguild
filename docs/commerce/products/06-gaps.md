# Products — gaps

> Module `GameGuild.Commerce.Products` · Documented against `develop` commit `60100459e6`.
> KNOWN = already tracked elsewhere · NEW = found by this audit. Every entry carries file:line at the documented commit.
>
> **Status update (issue #1290 fix)**: P1–P4 are FIXED — the host now composes `AddProductsModule`
> (`API:/Core/Setup/InfrastructureLayerExtensions.cs` block 10c, right after the Subscriptions module)
> with all four options classes `.Bind(...)`-wired to their declared sections inside the module method
> (`Products:/ProductsModule.cs`). The composed-host regression coverage lives in
> `apps/api/tests/GameGuild.Commerce.Subscriptions.IntegrationTests/SubscriptionPromoWiringIntegrationTests.cs`
> (coordinator seams, hosted scheduler, promo checkout happy path, 422-not-500 rejections, fraud-review ticket).

## NEW (found by this audit)

### P1 — `AddProductsModule` is never invoked by the host; the pricing scheduler is dead and four options classes are config-inert. **(headline)** — FIXED by #1290

The method registers the hosted scheduler, options, and five promo/defense services (`Products:/ProductsModule.cs:16-66`). The only caller in the entire tree is its own unit test (`apps/api/tests/GameGuild.Commerce.Products.UnitTests/ModuleAndInfrastructureTests.cs:19-22`). The composed host gets handlers from module scan and repositories/`*Service` from the convention scanner (`API:/Core/Setup/InfrastructureLayerExtensions.cs:482-560`), so most endpoints work — which is exactly why the missing remainder went unnoticed:

- **`PricingSchedulingBackgroundService` never hosted** (`:54`): scheduled pricing rules never activate/deactivate and date-bounded experiments never start/complete automatically (`BackgroundServices/PricingSchedulingBackgroundService.cs:11-18` documents the intended behavior; the sweep lives in `IPricingExperimentService.RunSchedulingSweepAsync`).
- **No configuration binding for any Products options**: `PricingSchedulingOptions` (`PricingScheduling:*`), `PromoEngineOptions` (`PromoCodes:*`, e.g. `PromoCodes:DefaultTimeZone`), `PromoAbuseDetectionOptions` (`PromoAbuseDetection:*`), `PromoRedemptionCaptchaOptions` — declared `SectionName`s (`Configuration/PromoEngineOptions.cs:9-20`, `Configuration/PricingSchedulingOptions.cs:7-15`) with no `Bind` anywhere; even inside `AddProductsModule` they are `AddOptions<T>()` without `.Bind` (`:53-60`). Operators setting these sections get silently ignored defaults (UTC timezone, default thresholds, worker "enabled" that never runs). G5-shaped blind spot: the binding guard passes because `AddOptions<T>()` appears in product sources.
- Same class as Consent N1 (dead registration method) — fix is one composition call in `ApiProductComposition`/`InfrastructureLayerExtensions` plus `.Bind(...)` on the four options.

### P2 — Subscription creation with promo codes fails hard: coordinator never registered. — FIXED by #1290 (wiring) + the total-rejection throw is now `SubscriptionPromoCodesRejectedException` (a `DomainException` → 422, not 500; `Subs:/Entities/SubscriptionPromoCodesRejectedException.cs`)

`CreateSubscriptionCommandHandler` correctly refuses to drop promo codes when `ISubscriptionPromoCoordinator` is null — `InvalidOperationException("Promo codes were supplied but no promo coordinator is registered...")` (`Subs:/Commands/CreateSubscription/CreateSubscriptionCommandHandler.cs:83-87`). The implementation (`Products:/Services/SubscriptionPromoCoordinator.cs:16`, issue #418) is only registered by the never-called `AddProductsModule` (`ProductsModule.cs:36`). Net effect: every subscription-creation request carrying promo codes 500s in the composed host; promo-less creation is unaffected. The same coordinator is the seam for plan-change/upgrade promo paths in the billing-orders package.

### P3 — Fraud-review tickets silently skipped: coordinator never registered. — FIXED by #1290 (coordinator registered by the now-composed module)

`ProcessPaymentCommandHandler` takes `IFraudReviewTicketCoordinator? = null` (`Payments:/Commands/ProcessPayment/ProcessPaymentCommandHandler.cs:27`) and only opens a ticket when non-null (`:155-162`); `SubscriptionChangeFraudGate` mirrors it (`Subs:/Commands/SubscriptionChangeFraudGate.cs:66`). The implementation (`Products:/Services/FraudReviewSupportTicketCoordinator.cs:19`, issue #409) is only registered at `ProductsModule.cs:39`. Net effect: fraud `Review`/`Block` outcomes produce **no support ticket** — the issue #409 "reviews surface in the support queue staff already work" behavior never happens, with no log of the skip beyond the fraud assessment itself.

### P4 — Promo anti-farming and anti-abuse defenses are off (null-optional degradation). — FIXED by #1290 (all three services registered by the now-composed module)

Post-fix defaults (issue #1290): the **usage-anomaly evaluator** and the **customer-state resolver** are active in the composed host with their built-in thresholds (velocity security events now reach Compliance.Audit; customer-type-restricted codes resolve instead of failing closed as `CustomerNotEligible` for everyone). The **CAPTCHA gate** is registered but stays **disabled by default** (`PromoRedemptionCaptcha:Enabled=false`) — the sibling Payments precedent (`PaymentMethodSecurityOptions`, issue #414) likewise fails to the historic behavior when unset, and enabling the gate requires the web side to complete a CAPTCHA challenge for anonymous redemptions. When enabled it fails closed (missing secret or unverifiable token ⇒ rejection). Operators opt in via `PromoRedemptionCaptcha:Enabled=true` + `PromoRedemptionCaptcha:VerificationSecret`.

Original finding (`60100459e6`) — `IPromoRedemptionCaptchaGate`, `IPromoUsageAnomalyEvaluator` (`ProductsModule.cs:42-43`) and `IPromoCustomerStateResolver` (`:41`) were unregistered; their consumers take them as optional:

- CAPTCHA gate null → anonymous validation/apply runs with **no gate** (`Commands/ValidatePromoCode/ValidatePromoCodeCommandHandler.cs:30-36` — the denial branch simply can't fire);
- anomaly evaluator null → **no velocity security events** reach Compliance.Audit (`:25`, `04-event-inventory.md` §4);
- customer-state resolver null → any promo code with `EligibleCustomerType` set fails closed as `CustomerNotEligible` for **everyone** (`Services/PricingEngineService.cs:312-318`) — first-time/win-back codes are unusable in the composed host.

### P5 — `ProductSubscriptionPlan` is an orphan third plan table. **(deprecated-by-documentation)**

Configured and indexed (`ProductsModule.cs:258-266`) with no reader outside the module (verified by search at `60100459e6`: only migration designers reference it besides the module). With `SubscriptionPlan` (billing truth) and `ProductPricing` (display) already coexisting (P6), this is a third plan concept that can only accumulate dead rows — **deprecated as of #1290: do not read, write, or extend `ProductSubscriptionPlan`; subscription billing truth is `SubscriptionPlan.MonthlyPriceInCents` (`05-cross-module-sequences.md` §5) and catalog display is `ProductPricing`. Removal (table drop + migration) is a follow-up for the billing-orders package; until then the entity stays mapped only so existing rows remain inspectable.**

### P6 — No display/billing price reconciliation for subscription products.

`ProductPricing` (display) and `SubscriptionPlan.MonthlyPriceInCents` (billing truth, `Subs:/Services/SubscriptionPlanPricingResolver.cs:30-62`) have no enforced link; the payments amount guard protects the charge (`Payments:/Commands/ProcessPayment/ProcessPaymentCommandHandler.cs:50-53`) but nothing reconciles the storefront display with the plan. Document as a consistency contract (`05-cross-module-sequences.md` §5) and add either a reconciliation check or a derived-display rule.

### P7 — Catalog search is `ToLower().Contains` over three text columns.

`Repositories/ProductRepository.cs:57-59` — functional but non-indexed (no `EF.Functions.ILike`/tsvector); scale characteristics worth tracking as the catalog grows.

### P8 — Archived (soft-deleted) products have no restore command.

Archive is one-way: `ArchiveProductHandler` soft-deletes (`Commands/ProductStatusCommands.cs:68-81`) and no handler un-deletes; the state machine's restore edge is missing (`03-state-machines.md` §1).

### P9 — Dead enum states: `ProductAccessStatus.Suspended` and `.Cancelled` have no writer.

`Enums/ProductEnums.cs:124-136` declares six access states; only `Active` and `Revoked` are ever assigned (`Entities/UserProduct.cs:115, :127, :153`); `Expired`/`Pending` are computed at read (`:98-108`). `Suspended`/`Cancelled` are unreachable — declared-but-dead states that mislead readers of the access model.

## KNOWN (tracked elsewhere, cross-referenced)

| # | Gap | Tracking | Relationship |
|---|-----|----------|--------------|
| K1 | The Consent module's dead registration (same class as P1) | compliance package N1 (`docs/compliance/07-gaps.md`), PR #1268 open | pattern precedent — P1 argues for a G-guard extension: "module Add*Module methods must have a host caller or an explicit exclusion" |
| K2 | Payments amount/subscription guard (post-#1266) | payments package edge 2 (`docs/commerce/payments/01-user-flows.md` §1a) | the enforcement side of the P6 contract |
| K3 | Plan-level interval pricing incl. annual discount and custom-quote sentinel | billing-orders package (`SubscriptionPlanPricingResolverTests`, issue #416) | the resolver both sides of P6 depend on |
| K4 | Economy top-up refund/reversal capability (#1266 follow-ups in `20261103141023_AddEconomyTopUpRefundReversalCapability`) | economy package K-family | money-path neighbors; none read Products pricing |

## Fix sketch (ordering)

1. **P1** — one composition call (`AddProductsModule`) + `.Bind` on the four options; keeps everything else in this list intact. Smallest change, largest repair.
2. **P2/P3/P4** — all dissolve once P1 lands (the module method registers all five missing services); P2's failure mode is the reason to treat P1 as user-facing, not cosmetic.
3. **P5** — deprecate `ProductSubscriptionPlan` or wire it to plan sync; do not leave three plan tables.
4. **P6** — define the display contract (derive display from plan, or add a reconciliation alert).

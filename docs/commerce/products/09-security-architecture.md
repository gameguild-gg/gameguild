# Products — security architecture

> Module `GameGuild.Commerce.Products` · Documented against `develop` commit `60100459e6`.
> Consolidates `07-stride-threat-model.md` and `08-dfds.md`: component view, the three-mechanism DI reality, permission map, pricing integrity posture, fail-verdicts.

## 1. Components and the wiring that keeps them up

```mermaid
flowchart TD
    subgraph HOST["composed host"]
        SCAN1["module scan - handlers and validators<br/>ApplicationLayerExtensions L110-145"]
        SCAN2["convention scan - repositories and star-Services<br/>InfrastructureLayerExtensions L482-560"]
        EXPL["explicit six - recorder auditlogger discountcalc<br/>validator catalogaudit bundlecalc L275-290"]
        DEAD["AddProductsModule - NEVER CALLED P1<br/>scheduler options coordinators defenses"]
    end
    subgraph MOD["module"]
        CTRL["11 controllers 92 endpoints"]
        ENG["PricingEngineService + promo gates"]
        ENT["EntitlementService UserProductService"]
        TICK["SupportTicket surface"]
        COORD1["SubscriptionPromoCoordinator - dead reg P2"]
        COORD2["FraudReviewTicketCoordinator - dead reg P3"]
    end
    subgraph STORES[("catalog stores")]
        S1["Products Pricing Versions Categories<br/>Inventory Promos Campaigns UserProducts Tickets"]
    end
    CTRL --> ENG
    CTRL --> ENT
    CTRL --> TICK
    SCAN1 --> CTRL
    SCAN2 --> ENG
    EXPL --> ENG
    DEAD -.->|"absent"| COORD1
    DEAD -.->|"absent"| COORD2
    ENG --> S1
    ENT --> S1
    TICK --> S1
```

The architectural fact: the module runs on **three registration mechanisms**, and the fourth (its own composition method) is the one that would have made it whole (`02-cqrs-wiring-matrix.md` §0).

## 2. Permission map (who can do what)

| Capability | Gate | Evidence |
|---|---|---|
| Anonymous catalog browse/detail/pricing/availability | `AllowAnonymous` + published-only clamp | `Products:/Controllers/ProductsController.cs:27-121, 160-196` |
| Include drafts (own) | authenticated creator | `:177-179` |
| Include drafts (tenant) / mutate any product | `ProductsPermission.Keys.Manage` | `:178, :387-393` |
| Create/update products | `Create`/`Update` + owner check | `:204, :251, :282` |
| Set pricing | `PricingManage` **+** `monetization:monetize` (command layer) | `:89`; proven `MonetizationPermissionHttpTests.cs:222-277` |
| Delete/archive | `Delete` + owner | `:342, :359` |
| Entitlement reads (all users) | `EntitlementsPermission.Keys.ReadAll` | `Controllers/EntitlementsController.cs:36` |
| Support ticket admin | typed support permissions | `Controllers/SupportTicketsController.cs` |

## 3. Pricing integrity posture

- **Display side** (this module): immutable `ProductPricingVersion` rows — every price change supersedes + increments (`Entities/ProductPricing.cs:111-131`); historical lookup `GetVersionAt` (`:146-152`); rule→experiment→promo ordering fixed in the engine (`Services/PricingEngineService.cs:48-103`).
- **Billing side** (Subscriptions): plan cents authoritative, resolved per cycle (`Subs:/Services/SubscriptionPlanPricingResolver.cs:30-62`), enforced at charge time (`Payments:/Commands/ProcessPayment/ProcessPaymentCommandHandler.cs:50-53`).
- **The seam**: nothing reconciles the two (P6) and `ProductSubscriptionPlan` is a third orphaned surface (P5). The integrity property that *does* hold end-to-end: **no client-supplied amount ever reaches a charge** without the plan-equality guard.

## 4. Fail-open/fail-closed verdicts

| Dependency | Failure | Verdict |
|---|---|---|
| Promo coordinator at subscription creation | null (host reality) | **fail-closed** — hard error when promo codes supplied (P2) |
| Fraud ticket coordinator | null | **fail-open (silent)** — reviews proceed without tickets (P3) |
| CAPTCHA gate / anomaly evaluator / customer-state resolver | null | **fail-open** for probing (gate off), **fail-closed** for restricted codes (everyone `CustomerNotEligible`) (P4) |
| Pricing scheduler | never hosted | neutral — rules stay in manual state (P1) |
| Options binding | absent | silent defaults (UTC, stock thresholds) (P1) |
| Pricing engine on missing pricing | no default option | returns zero-price result (`Services/PricingEngineService.cs:33-43`) — callers must treat 0 as "no price", not free |

## 5. Deployment invariants

1. If `AddProductsModule` is ever wired in, verify no double registration of `IPromoDiscountCalculator` (explicit host registration exists — `InfrastructureLayerExtensions.cs:281-282`; module registers the adapter again `ProductsModule.cs:32` — last-wins, benign but noisy).
2. Keep the monetize command-layer gate on pricing mutations — the endpoint permission alone was deliberately insufficient (`MonetizationPermissionHttpTests` pins it).
3. Never surface `ProductPricing` as the price of a subscription checkout without the plan-derived amount alongside — the display/billing seam (P6) is a UI discipline until reconciled.
4. The convention scanner's unmatched-type warning (`InfrastructureLayerExtensions.cs:562+`) is the early-warning system for renamed services — treat those logs as build failures during refactors.

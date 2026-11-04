# Products — cross-module sequences

> Module `GameGuild.Commerce.Products` · Documented against `develop` commit `60100459e6`.
> Citation shorthand: `Products:` = `apps/api/Source/Modules/GameGuild.Commerce.Products/`, `Subs:` = `apps/api/Source/Modules/GameGuild.Commerce.Subscriptions/`, `Payments:` = `apps/api/Source/Modules/GameGuild.Commerce.Payments/`, `Orders:` = `apps/api/Source/Modules/GameGuild.Commerce.Orders/`.

## 1. Checkout pricing — Orders asks Products for the effective price

```mermaid
sequenceDiagram
    participant Cart as Cart or checkout
    participant Orders as AddOrderItem flow
    participant Repo as IProductRepository
    participant Engine as PricingEngineService
    Cart->>Orders: add product with promo codes
    Orders->>Repo: GetByIdAsync includePricing
    Orders->>Engine: CalculatePriceAsync product pricing promos user L10-120
    Engine->>Engine: effective price sale-or-base L40-45
    Engine->>Engine: pricing rule window match - ruleUnitDiscount L48-62
    Engine->>Engine: experiment arm if bucket key - sticky variant L70-88
    Engine->>Engine: promo codes - customer state stacking caps L91-103
    Engine-->>Orders: PricingCalculationResult finalPrice
    Orders-->>Cart: order line at engine price
```

Note the order of adjustments (rule → experiment → promo) is fixed in the engine (`Products:/Services/PricingEngineService.cs:48-103`); the same engine serves `CalculatePricingCommand` for preview surfaces.

## 2. Subscription creation with promo codes — the broken seam (P2)

```mermaid
sequenceDiagram
    participant User as Buyer
    participant SubsCtrl as Subscriptions controller
    participant Handler as CreateSubscriptionCommandHandler
    participant Coord as ISubscriptionPromoCoordinator
    participant Engine as Products promo engine
    User->>SubsCtrl: create subscription with promo codes
    SubsCtrl->>Handler: CreateSubscriptionCommand promoCodes
    Handler->>Handler: authoritativeAmount from plan L77-80
    alt coordinator registered - NOT the case in host ProductsModule L36 never called
        Handler->>Coord: QuoteForSubscriptionCreationAsync L89-95
        Coord->>Engine: validate codes against plan price
        Engine-->>Coord: applied codes and discount
        Coord-->>Handler: quote - create at discounted first invoice
        Handler->>Coord: RecordSubscriptionUsageAsync L178-181
    else coordinator null - HOST reality
        Handler-->>User: InvalidOperationException promo codes supplied but no coordinator<br/>Subs Commands CreateSubscription CreateSubscriptionCommandHandler.cs L83-87 - 500
    end
```

Fail verdict: **fail-closed as an unplanned 500** — the guard is correct (codes must not be silently dropped), but the registration hole turns every promo-coded subscription creation into an error (P2).

## 3. Fraud manual review → support ticket — the silent seam (P3)

```mermaid
sequenceDiagram
    participant Pay as ProcessPaymentCommandHandler
    participant Fraud as Fraud gate assessment
    participant Coord as IFraudReviewTicketCoordinator
    participant Tickets as SupportTicket store
    Pay->>Fraud: AssessPaymentAsync
    Fraud-->>Pay: Review or Block with urgency
    alt coordinator registered - NOT the case in host ProductsModule L39 never called
        Pay->>Coord: create ticket best-effort L155-162
        Coord->>Tickets: CreateSupportTicket
        Tickets-->>Coord: ticket id
    else coordinator null - HOST reality Payments L27 optional
        Pay->>Pay: skip ticket creation - log only
        Note over Pay,Tickets: issue 409 surface reviews in support queue - never happens P3
    end
```

Same shape in `Subs:/Commands/SubscriptionChangeFraudGate.cs:66` — optional injection, silent skip.

## 4. Entitlement check — sibling modules pull access

```mermaid
sequenceDiagram
    participant Sib as Orders or Assets
    participant Q as CheckProductAccessQuery
    participant H as CheckProductAccessQueryHandler
    participant UP as UserProduct rows
    Sib->>Q: user product
    Q->>H: handle
    H->>UP: GetByUserIdAndProduct
    UP-->>H: row or none
    H->>H: HasActiveAccess status window L98-108
    H-->>Sib: bool
```

Assets' paid-download windows ride this seam via `CommerceOrderValidationService` (`API:/Core/Setup/InfrastructureLayerExtensions.cs:295-297`), which itself consults order/payment state — the entitlement answer is one of two inputs.

## 5. Pricing display vs. billing truth — the consistency contract post-#1266

Two independent price surfaces must agree for a subscription product:

```mermaid
flowchart TD
    CATALOG["Catalog display price<br/>Products ProductPricing BasePrice SalePrice<br/>decimal 10-2 currency<br/>versioned ProductPricingVersion"]
    BILLING["Billing truth<br/>Subs SubscriptionPlan MonthlyPriceInCents long<br/>resolved per cycle by SubscriptionPlanPricingResolver<br/>Subs Services SubscriptionPlanPricingResolver.cs L30-62"]
    PAYGUARD["Payment amount guard<br/>subscription Amount not equal request Amount - reject<br/>Payments ProcessPaymentCommandHandler.cs L50-53"]
    ORPHAN["ProductSubscriptionPlan<br/>Products table - legacy<br/>no reader outside Products - P5"]

    CATALOG -.->|"no enforced link - display discipline only P6"| BILLING
    BILLING --> PAYGUARD
    ORPHAN -.->|"not consulted"| BILLING
```

The contract that holds: **the money path is closed** — plan price (cents) is authoritative, resolved through `IPlanPricingResolver` for preview/checkout/renewal alike (issue #416 note in `SubscriptionPlanPricingResolver.cs:10-14`), and the payments handler rejects any requested amount that diverges (`:50-53`), the #1266-era hardening documented in `docs/commerce/payments/01-user-flows.md` edge 2.

The contract that does NOT hold: nothing reconciles `ProductPricing` (what the product page shows) with `SubscriptionPlan.MonthlyPriceInCents` (what is billed). A merchandiser editing one without the other produces an honest-but-wrong price tag; the guard downstream only protects the charge, not the display. `ProductSubscriptionPlan` (`ProductsModule.cs:258-266`) is a third, orphaned plan table with no reader outside the module (`06-gaps.md` P5).

## 6. Shared surfaces with the payments/billing matrices

Cross-referenced against `docs/commerce/payments/02-cqrs-wiring-matrix.md` and `docs/commerce/billing-orders/cqrs-wiring-matrix.md`:

| Shared surface | Owner | Where the other matrix references it |
|---|---|---|
| `IPromoDiscountCalculator` | implemented here (adapter), abstraction in Payments | payments matrix Pricing/Promo rows (issue #417 seam) |
| Pricing preview at checkout | engine here, consumed by Orders/Subscriptions | billing-orders matrix `pricing:preview` / `breakdown` rows (plan side) |
| Subscription promo codes | quotes/usage recorded here via `ISubscriptionPromoCoordinator` | billing-orders matrix plan-change/upgrade rows apply promos through the same coordinator (registration hole P2 applies there too — any Subs path taking promo codes) |
| Fraud review tickets | `IFraudReviewTicketCoordinator` implemented here | payments matrix fraud family (reviews surface as tickets — silent skip P3) |

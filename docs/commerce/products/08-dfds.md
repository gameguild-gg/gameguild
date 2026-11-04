# Products — data-flow diagrams

> Module `GameGuild.Commerce.Products` · Documented against `develop` commit `60100459e6`.
> PII marked per flow. The module stores no direct personal identifiers beyond user/creator ids and support-ticket contact fields (customer/reporter names and emails — flagged below).

## Level 0 — context

```mermaid
flowchart TD
    ANON(("anonymous visitor"))
    CREATOR(("creator or admin"))
    BUYER(("authenticated buyer"))
    SIB["sibling commerce modules<br/>Orders Subscriptions Payments Assets"]
    MOD["GameGuild.Commerce.Products<br/>catalog pricing promo entitlements tickets"]
    STORES[("Products Pricing Versions<br/>Categories Inventory Promos<br/>UserProducts Tickets")]

    ANON -->|"browse detail pricing availability"| MOD
    CREATOR -->|"admin catalog pricing promos categories"| MOD
    BUYER -->|"validate apply promo codes<br/>self entitlements tickets"| MOD
    SIB -->|"price at checkout access check<br/>promo quote fraud tickets"| MOD
    MOD --> STORES
```

Trust boundaries: TB1 anonymous/visitor → API; TB2 authenticated buyer → API; TB3 creator/admin (typed permissions) → API; TB4 API → stores (in-process); TB5 sibling modules → module seams (in-process, coordinator interfaces).

## C1 — Catalog reads (level 1)

```mermaid
flowchart TD
    subgraph TB1["TB1 anonymous"]
        A1(("visitor"))
    end
    subgraph API["API process"]
        R1["products controller AllowAnonymous<br/>ProductsController.cs L27-196"] --> R2["visibility clamp<br/>published only unless owner L34-38 L177-179"]
        R2 --> R3["GetProductsPaged / GetProductById<br/>GetProductPricing handlers"]
    end
    R3 --> S1[("Products<br/>ProductPricing<br/>ProductInventory")]
    A1 --> R1
```

PII: none crosses TB1.

## C2 — Catalog administration (level 1)

```mermaid
flowchart TD
    subgraph TB3["TB3 creator admin"]
        C1(("actor with Products permissions"))
    end
    subgraph API["API process"]
        M1["RequirePermission Create Update Delete<br/>PricingManage Manage"] --> M2{"owner check<br/>CanMutateAsync L395-399"}
        M2 --> M3["command + validator + handler<br/>ExpectedVersion concurrency"]
        M3 --> M4["audit via generic operation events"]
    end
    C1 --> M1
    M3 --> S1[("Products<br/>ProductCategory<br/>ProductCommissionConfig<br/>ProductBundleItem")]
    M3 --> S2[("ProductPricing + ProductPricingVersion<br/>ProductInventory")]
```

PII: none.

## C3 — Pricing engine (level 1)

```mermaid
flowchart TD
    subgraph USERS["pricing inputs"]
        U1(("checkout caller"))
        U2(("admin"))
    end
    subgraph ENGINE["PricingEngineService L10-120"]
        E1["effective price sale-or-base"] --> E2["pricing rule window<br/>usage metric quantity segment"]
        E2 --> E3["experiment arm sticky bucket"]
        E3 --> E4["promo codes customer state<br/>stacking caps"]
        E4 --> E5["final price floor 0"]
    end
    U1 --> E1
    U2 -->|"rules experiments admin"| S3[("PricingRule + Tiers<br/>PricingExperiment variants assignments outcomes")]
    E2 --> S3
    E3 --> S3
    E4 --> S4[("PromoCode + StackingRules")]
    E5 --> U1
```

Dormant leg: scheduler worker (P1) would drive rule/experiment window transitions inside S3 — edge absent in the composed host.

## C4 — Promo engine (level 1)

```mermaid
flowchart TD
    subgraph TB2["TB2 buyer or anonymous validator"]
        P1(("promo caller"))
    end
    subgraph API["API process"]
        G1["validate or apply command"] --> G2{"captcha gate - NULL in host P4"}
        G2 --> G3["engine eligibility<br/>resolver - NULL in host P4"]
        G3 --> G4["apply - atomic usage record<br/>idempotency unique"]
        G1 --> G5["attempt row always persisted"]
        G3 --> G6{"anomaly evaluator - NULL in host P4"}
        G6 -.->|"no security event emitted P4"| AUD[("Compliance.Audit sinks")]
    end
    P1 --> G1
    G4 --> S4[("PromoCodeUse<br/>UserUsageCounter<br/>ValidationAttempt<br/>CampaignAttribution")]
    G5 --> S4
```

PII: usage rows carry user ids (pseudonymous references), no contact data.

## C5 — Entitlements + tickets (level 1)

```mermaid
flowchart TD
    subgraph TB2b["TB2 buyer or admin"]
        T1(("requester"))
    end
    subgraph API["API process"]
        E1["grant revoke check<br/>window computed at read"] --> S5[("UserProduct")]
        T2["support ticket create message resolve<br/>entity state guards SupportTicket.cs L139-186"] --> S6[("SupportTicket + Messages<br/>PII - customer reporter names emails")]
    end
    T1 --> E1
    T1 --> T2
    SIB2["Orders Assets pull access"] --> E1
```

PII: `SupportTicket.CustomerName/ReporterName/ReporterEmail` (`ProductsModule.cs:376-378`) cross TB2 — the module's only direct PII columns; exposure governed by typed read permissions and self-scope on `MySupportTicketsController`.

## C6 — Cross-module seams (level 1)

```mermaid
flowchart TD
    subgraph SIBS["sibling modules - TB5"]
        O["Orders"]
        SB["Subscriptions"]
        PM["Payments"]
    end
    subgraph MOD["Products seams"]
        SE1["IProductRepository access<br/>Orders AddProductToOrderCommandHandler.cs L19"]
        SE2["ISubscriptionPromoCoordinator<br/>NEVER REGISTERED P2"]
        SE3["IFraudReviewTicketCoordinator<br/>NEVER REGISTERED P3"]
        SE4["IPromoDiscountCalculator adapter<br/>registered InfrastructureLayerExtensions L281-282"]
    end
    O --> SE1
    SB -->|"quote + record usage - fails closed 500"| SE2
    PM -->|"review ticket - silent skip"| SE3
    PM -->|"discount calculation - live"| SE4
```

## Boundary inventory

| Boundary | Crossers | Control |
|---|---|---|
| TB1 anonymous → API | catalog reads | published-only clamp; drafts 404; no rate limit (residual C1.6) |
| TB2 buyer → API | promo validate/apply, self entitlements, own tickets | `[Authorize]`; idempotent apply; **captcha off (P4)** |
| TB3 creator/admin → API | catalog + pricing + promo admin | typed permissions + owner checks + monetize command gate (pricing) |
| TB4 API ↔ stores | 20+ tables | in-process EF Core; unique indexes for idempotency/eligibility floors |
| TB5 siblings ↔ module seams | 4 coordinator/repository seams | two live, two unregistered (P2/P3) — the seams' consumers fail closed or silent per optionality |

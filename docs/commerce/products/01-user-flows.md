# Products user flows — happy path and every edge case

> Module `GameGuild.Commerce.Products` · Documented against `develop` commit `60100459e6`.
> Citation shorthand: `Products:` = `apps/api/Source/Modules/GameGuild.Commerce.Products/`.

## 1. Catalog browse / search (anonymous)

`GET v{v}/products` (`Products:/Controllers/ProductsController.cs:160-196`) → `GetProductsPagedQuery` (`Queries/GetProducts/GetProductsPagedQuery.cs`) → handler (`GetProductsPagedQueryHandler.cs:20-90`) → repository paged filter (`Repositories/ProductRepository.cs`).

```mermaid
flowchart TD
    subgraph BROWSE["Catalog browse - GET products ProductsController.cs L160"]
        B1["Client GET v1 products<br/>type creatorId searchTerm isBundle<br/>categoryId inStock includeUnpublished skip take sortBy"] --> B2{"includeUnpublished and TryActor<br/>L177"}
        B2 -- "anonymous or unauthenticated" --> B3["includeDrafts false<br/>isPublished true filter"]
        B2 -- "authenticated actor" --> B4{"Actor has Products Manage<br/>L178"}
        B4 -- "no" --> B5["creatorId forced to own id L179<br/>drafts only own"]
        B4 -- "yes" --> B6["includeDrafts with tenant scope L191"]
        B3 --> B7["GetProductsPagedQueryHandler"]
        B5 --> B7
        B6 --> B7
        B7 --> B8{"categoryId supplied L21-32"}
        B8 -- "unknown category" --> B9["deterministic empty page L27-31"]
        B8 -- "known or absent" --> B10["productRepository GetPagedAsync<br/>search term name description shortDescription Contains<br/>ProductRepository.cs L57-59"]
        B10 --> B11{"inStock filter"}
        B11 -- "true" --> B12["join ProductInventory<br/>stock minus reserved greater than 0 or untracked"]
        B11 -- "absent" --> B13["no inventory filter"]
        B12 --> B14["200 PagedResult ProductDto"]
        B13 --> B14
    end
```

### 1a. Edge-case matrix — browse/search

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Anonymous caller asks for drafts | `includeUnpublished && TryActor` (`ProductsController.cs:177`) | drafts flag collapses to false — anonymous catalog is published-only |
| 2 | Non-manager creator asks for drafts | creator pin (`:178-179`) | sees only **own** drafts (no permission escalation via includeUnpublished) |
| 3 | Unknown `categoryId` | empty-determinism guard (`GetProductsPagedQueryHandler.cs:26-31`) | empty page, not an unfiltered listing (classical filter-injection guard) |
| 4 | Search term casing | `ToLower().Contains` (`Repositories/ProductRepository.cs:57-59`) | case-insensitive by lowercasing both sides (note: not `EF.Functions.ILike` — full scan semantics on non-indexed text) |
| 5 | `inStock=true` on untracked product | inventory join semantics (`ProductRepository.cs` inventory predicates) | untracked products report always-in-stock (`ProductsController.cs:113-115` documents the contract) |
| 6 | Pagination abuse (`take=100000`) | `GetProductsPagedQueryValidator` (default take 50; validator clamps) | bounded page |

## 2. Product detail / pricing / availability (anonymous + drafts for owners)

```mermaid
flowchart TD
    subgraph DETAIL["Product detail - GET products productId ProductsController.cs L27"]
        D1["GET v1 products id<br/>includePricing default true"] --> D2["GetProductByIdQuery L34"]
        D2 --> D3{"null or not published<br/>L37"}
        D3 -- "not published" --> D4{"CanAccessUnpublished<br/>same tenant and creator or Manage L387-393"}
        D4 -- "no" --> D5["404 - drafts invisible"]
        D4 -- "yes" --> D6["200 ProductDto"]
        D3 -- "published" --> D6
        D7["GET products id pricing L69"] --> D8["GetProductPricingQuery<br/>unpublished include only when owner L75-84"]
        D8 --> D9["200 pricing list"]
        D10["HEAD products id L49"] --> D11["ProductExistsQuery or owner draft check L53-60"]
        D10 --> D12["200 or 404 - existence oracle"]
        D13["GET products id availability L115"] --> D14["GetProductAvailabilityQuery L119"]
        D14 --> D15["stock state or always-in-stock L113-115"]
    end
```

### 2a. Edge-case matrix — detail

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Draft product, anonymous | 404 (not 403) (`ProductsController.cs:37-38`) | existence of drafts not leaked |
| 2 | Cross-tenant draft | tenant equality in `CanAccessUnpublished` (`:389`) | 404 |
| 3 | `HEAD` as an existence oracle for drafts | owner-gated branch (`:53-57`) | anonymous HEAD only proves published existence |
| 4 | Pricing of a draft | include-unpublished re-checks product access first (`:75-81`) | no draft price leak |

## 3. Admin create → lifecycle (create / patch / activate / deactivate / archive / delete)

```mermaid
flowchart TD
    subgraph ADMIN["Product administration - ProductsController.cs"]
        A1["POST products L203<br/>RequirePermission Products Create"] --> A2["CreateProductCommand L208-222<br/>creator from actor tenant from actor"]
        A2 --> A3["CreateProductCommandHandler<br/>Products Commands CreateProduct L8"]
        A3 --> A4["201 ProductDto"]
        A5["POST products batch-create L233"] --> A6["BatchCreateProductsCommand L238-240<br/>each item creator pinned to actor"]
        A7["PUT products id L250 PATCH L281<br/>RequirePermission Products Update"] --> A8{"CanMutateAsync<br/>L395-399 owner or Manage in tenant"}
        A8 -- "no" --> AX1["403"]
        A8 -- "yes" --> A9["Update or Patch command<br/>ExpectedVersion optimistic concurrency L267 299"]
        A10["POST id activate L311"] --> A11["ActivateProductHandler<br/>IsPublished true ProductStatusCommands.cs L20-22"]
        A12["POST id deactivate L326"] --> A13["DeactivateProductHandler<br/>IsPublished false L45-47"]
        A14["POST id archive L341<br/>RequirePermission Products Delete"] --> A15["ArchiveProductHandler<br/>SoftDelete L68-73"]
        A16["DELETE products id L358"] --> A17{"softDelete query default true L362"}
        A17 -- "true" --> A18["soft delete with reason"]
        A17 -- "false" --> A19["hard delete - check no purchases first"]
        A20["PUT id pricing L88<br/>RequirePermission PricingManage"] --> A21["SetProductPricingCommand L96-106"]
        A21 --> A22["SetProductPricingCommandHandler L9-118<br/>immutable versioning see flow 4"]
        A23["PUT id inventory L129<br/>RequirePermission Manage"] --> A24["SetProductInventoryCommand L135-141"]
    end
```

### 3a. Edge-case matrix — administration

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Creator id spoofing via body | actor-pinned `creatorId` (`ProductsController.cs:207, 222, 238`) | creator always the authenticated actor |
| 2 | Optimistic-concurrency race on update | `ExpectedVersion` plumbed (`:267, :299`) | stale writers rejected by entity version |
| 3 | Mutating another creator's product | `CanMutateAsync` → `CanAccessUnpublished` (`:395-399, :387-393`) | 403 without Manage |
| 4 | Two-layer permission on pricing | controller `PricingManage` + command-layer `monetization:monetize` (AuthorizationBehavior; proven by `MonetizationPermissionHttpTests.cs:222-277`) | both must pass — endpoint permission alone is not enough |
| 5 | Archive vs delete | archive = soft (`ProductStatusCommands.cs:68-73`); delete defaults soft, supports hard | hard delete gated behind explicit query flag |
| 6 | Deactivate a product with live entitlements | no guard in `DeactivateProductHandler` (`:41-52`) | unpublished but entitlements untouched (display-only change) — see `03-state-machines.md` §1 note |

## 4. Pricing set — immutable versioning (the product-side price source of truth)

`PUT products/{id}/pricing` → `SetProductPricingCommandHandler` (`Products:/Commands/SetProductPricing/SetProductPricingCommandHandler.cs:9-118`).

```mermaid
flowchart TD
    subgraph PRICING["SetProductPricing - immutable price history"]
        P1["SetProductPricingCommand<br/>pricingId null create or update"] --> P2{"Product exists L21-26"}
        P2 -- "no" --> PX1["ProductNotFoundException 404"]
        P2 -- "yes" --> P3{"pricingId supplied L32"}
        P3 -- "update" --> P4["locate pricing on product L35-36"]
        P4 -- "absent" --> PX2["InvalidOperation 400"]
        P4 -- "found" --> P5{"base or sale changed L44"}
        P5 -- "yes" --> P6["pricing UpdatePrices<br/>supersedes prior version CurrentVersion plus plus<br/>Entities ProductPricing.cs L111-131"]
        P5 -- "no" --> P7["metadata only name currency sale window"]
        P3 -- "create" --> P8["ProductPricing CreateWithVersion<br/>initial version v1 L59-75"]
        P6 --> P9{"IsDefault L80-92"}
        P8 --> P9
        P9 -- "true" --> P10["unset other defaults then set"]
        P9 -- "false" --> P11["leave defaults"]
        P10 --> P12["SaveChanges - version row persisted in same transaction L53 75 94"]
        P11 --> P12
        P12 --> P13["200 ProductPricingDto with currentPrice and isSaleActive L96-117"]
    end
```

### 4a. Edge-case matrix — pricing

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Negative price | `UpdatePrices` guards (`Entities/ProductPricing.cs:115-120`) | rejected — price cannot be negative |
| 2 | Sale price ≥ base price | guard (`:119-120`) | rejected — sale must be a real discount |
| 3 | Price change without version bump | impossible — `UpdatePrices` always supersedes + increments (`:122-130`) | every change is auditable via `ProductPricingVersion` |
| 4 | Historical price at date D | `GetVersionAt` (`:146-152`) | order reconciliation can resolve what the price was |
| 5 | Effective price with windowed sale | `IsSaleActive` window check (`:169-182`), handler duplicates the check (`SetProductPricingCommandHandler.cs:96-102`) | sale applies strictly inside `[SaleStartDate, SaleEndDate)` |
| 6 | Setting default on a second option | first-unset-then-set (`:80-92`) | exactly one default per product |

## 5. Promo validate → apply (the promo engine)

`ValidatePromoCodeCommandHandler` (`Products:/Commands/ValidatePromoCode/ValidatePromoCodeCommandHandler.cs:22+`) and `ApplyPromoCodesCommandHandler` (`Commands/ApplyPromoCode/ApplyPromoCodesCommandHandler.cs:20+`), engine in `Services/PricingEngineService.cs:10-120, 300-330`.

```mermaid
flowchart TD
    subgraph PROMO["Promo validation and application"]
        V1["POST promo-codes validate<br/>PromoCodesController"] --> V2{"captchaGate enabled and untrusted caller<br/>ValidatePromoCodeCommandHandler L30-36"}
        V2 -- "gate denies" --> VX1["CaptchaRequired outcome recorded as attempt<br/>no code lookup L31-36"]
        V2 -- "pass or gate null - HOST reality P4" --> V3["PricingEngineService ValidateCodeAsync"]
        V3 --> V4{"code active and window open<br/>PromoValidityWindow tenant default timezone<br/>PromoEngineOptions DefaultTimeZoneId"}
        V4 -- "no" --> VX2["invalid or expired"]
        V4 -- "yes" --> V5{"EligibleCustomerType set<br/>L312"}
        V5 -- "yes but resolver null - HOST reality P4" --> VX3["CustomerNotEligible fail closed L313-318"]
        V5 -- "resolver present" --> V6["PromoCustomerStateResolver<br/>New Existing Churned Services PromoCustomerStateResolver.cs L29-56"]
        V6 --> V7{"match"}
        V7 -- "no" --> VX3
        V7 -- "yes" --> V8["stacking rules and caps<br/>PromoStackingResolver"]
        V8 --> V9["valid - attempt row persisted PromoCodeValidationAttempt"]
        A1["POST promo-codes apply or checkout path"] --> A2["ApplyPromoCodesCommandHandler"]
        A2 --> A3["atomic usage record PromoCodeUse<br/>IdempotencyKey unique L232-236 of ProductsModule config"]
        A3 --> A4["per-user counter increment<br/>PromoCodeUserUsageCounter"]
        A4 --> A5["first-purchase attribution once per user<br/>PromoCampaignAttribution unique UserId"]
    end
```

### 5a. Edge-case matrix — promo engine

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Anonymous code farming | CAPTCHA gate design (`ValidatePromoCodeCommandHandler.cs:30-36`) | **in the composed host the gate is null (P1/P4) — probing is ungated**, but denied attempts still record for anomaly velocity (which is also off — P4) |
| 2 | Customer-type-restricted code (first-time only, win-back) | resolver (`PricingEngineService.cs:312-321`) | **host reality: resolver null → every restricted code fails `CustomerNotEligible`** (P4) |
| 3 | Replay of an apply request | `PromoCodeUse.IdempotencyKey` unique (`ProductsModule.cs:232-236`) | second application is a replay, never a second use |
| 4 | Per-user cap races | `PromoCodeUserUsageCounter` composite key (`:252-256`) | counter row per (code,user) |
| 5 | Multi-code stacking abuse | stacking rules + max caps (`Services/PromoStackingResolver.cs`) | total discount clamped |
| 6 | Attempt-tracking failure | best-effort persistence (`ValidatePromoCodeCommandHandler.cs:10-13` doc comment) | validation succeeds; tracking failure logged only |
| 7 | Campaign timezone | `PromoEngineOptions.DefaultTimeZoneId` (`Configuration/PromoEngineOptions.cs:20`) | **host reality: never bound to config — always UTC fallback (P1)** |

## 6. Entitlements — grant / revoke / check

`GrantProductAccessCommand`/`RevokeProductAccessCommand` (`Commands/GrantProductAccess/`, `Commands/RevokeProductAccess/`), `CheckProductAccessQuery` (`Queries/CheckProductAccess/`), `EntitlementsController` (`Controllers/EntitlementsController.cs:19+`).

```mermaid
flowchart TD
    subgraph ENT["Entitlement lifecycle"]
        E1["POST user access grant<br/>GrantProductAccessCommandHandler"] --> E2["UserProduct Create<br/>Active acquisition type window Entities UserProduct.cs L153"]
        E3["POST revoke"] --> E4["AccessStatus Revoked L127"]
        E5["GET entitlements me expiring active<br/>EntitlementsController L38-58"] --> E6["EntitlementService reads UserProduct"]
        E7["CheckProductAccessQuery from sibling modules"] --> E8{"HasActiveAccess<br/>L98-108"}
        E8 -- "status Active and window open" --> E9["true"]
        E8 -- "status not Active or outside window" --> E10["false"]
    end
```

### 6a. Edge-case matrix — entitlements

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Duplicate grant | unique `(UserId,ProductId)` (`ProductsModule.cs:163`) | one access row per user/product |
| 2 | Future-dated window | `AccessStartDate > now` fails `HasActiveAccess` (`Entities/UserProduct.cs:101-103`) | entitlement exists but not yet active |
| 3 | Expired window | `AccessEndDate <= now` (`:105-107`) | access lapses without a job — time-based, computed at read |
| 4 | Expiring-soon listing | `GetExpiringEntitlementsAsync` (`EntitlementsController.cs:44-50`) | N-day horizon query |
| 5 | Reading all users' entitlements | `RequirePermission(EntitlementsPermission.Keys.ReadAll)` (`:36`) | typed permission, not just `[Authorize]` |

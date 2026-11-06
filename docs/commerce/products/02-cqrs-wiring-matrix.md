# Products — CQRS / wiring matrix

> Module `GameGuild.Commerce.Products` · Documented against `develop` commit `60100459e6`.
> Citation shorthand: `Products:` = `apps/api/Source/Modules/GameGuild.Commerce.Products/`, `API:` = `apps/api/Source/GameGuild.API/`.

## 0. DI wiring — the load-bearing section

Four mechanisms compose this module. **The module method is now composed by the host (issue #1290 fix; at the documented commit `60100459e6` it was dead — see the status column):**

| Mechanism | What it registers | Evidence | Status |
|---|---|---|---|
| Module scan (CQRS) | all command/query handlers + FluentValidation validators from the assembly | `API:/Core/Setup/ApplicationLayerExtensions.cs:110-145`; `Commerce.Products` in `ModuleConfiguration.CommonEnabledModules` (`API:/Core/Setup/ModuleConfiguration.cs:20`) | **live** — the G1 guard's empty baseline section confirms no handler is unregistered |
| Convention scan (repositories/services) | `I{Name}Repository → {Name}Repository` and `I{Name}Service → {Name}Service` (+`Reader`) from enabled module assemblies, scoped | `API:/Core/Setup/InfrastructureLayerExtensions.cs:482-560` (patterns at `:533-557`); unmatched-type warning at `:562+` | **live** — covers the 10 repositories and 8 `*Service` interfaces |
| Module method (`AddProductsModule`) | hosted `PricingSchedulingBackgroundService`; `.Bind(...)` options for `PricingScheduling`/`PromoCodes`/`PromoAbuseDetection`/`PromoRedemptionCaptcha`; `ISubscriptionPromoCoordinator`, `IFraudReviewTicketCoordinator`, `IPromoCustomerStateResolver`, `IPromoRedemptionCaptchaGate`, `IPromoUsageAnomalyEvaluator`; the six non-convention promo/pricing/catalog services formerly inlined at `:275-290` (`IPromoCodeUsageRecorder`, `IPromoCodeAuditLogger`, `IPromoDiscountCalculator` adapter, `IProductCategoryValidator`, `IProductCatalogAuditLogger`, `IBundleSavingsCalculator`) | composed by the host at `API:/Core/Setup/InfrastructureLayerExtensions.cs` block 10c (right after `AddSubscriptionsModule`, issue #1290); method body `Products:/ProductsModule.cs` | **live since #1290** — at `60100459e6` this row was **DEAD** (gap P1): never invoked, options unbound, five services absent |
| EF model | `ProductsModelConfiguration` (IModelConfiguration) incl. `ProductsModule.ConfigureProductsModel` (`Products:/Configuration/ProductsModelConfiguration.cs:20`) | assembly-scanned in `API:/Database/ApplicationDbContext.cs:866-883` | **live — tables materialized** |

Runtime consequences **before** the #1290 fix (all traced in `06-gaps.md`, now resolved):

- `PricingSchedulingBackgroundService` never hosted — scheduled pricing rules/experiments never transitioned on time (`ProductsModule.cs:54`). Now hosted by the composed API.
- `IOptions<PricingSchedulingOptions/PromoEngineOptions/PromoAbuseDetectionOptions/PromoRedemptionCaptchaOptions>` resolved framework defaults only; no configuration section was bound anywhere. Now `.Bind(...)`-wired inside the module method, so `PricingScheduling:*`, `PromoCodes:*`, `PromoAbuseDetection:*` and `PromoRedemptionCaptcha:*` take effect.
- `ISubscriptionPromoCoordinator`, `IFraudReviewTicketCoordinator`, `IPromoCustomerStateResolver`, `IPromoRedemptionCaptchaGate`, `IPromoUsageAnomalyEvaluator` were unregistered → P2 (hard failure, fixed), P3/P4 (silent null degradation, fixed; CAPTCHA gate registered but opt-in by configuration).

## 1. Endpoint matrix (11 controllers, ~92 endpoints)

**Auth legend** — `[Authorize]` = any authenticated; `RequirePermission(X)` = typed ABAC permission enforced at the endpoint; command-layer `AuthorizationBehavior` adds `monetization:monetize` on monetization mutations (proven end-to-end by `MonetizationPermissionHttpTests.cs:222-277`). `AllowAnonymous` on catalog reads. **Rate limit legend** — `—` = none on the action (global limiter only).

### ProductsController (`v{v}/products`, `Products:/Controllers/ProductsController.cs`)

| Route | Request | Validator | Handler | Auth | Rate limit | Unit tests | Integration tests | HOLE? |
|---|---|---|---|---|---|---|---|---|
| GET `{productId}` | `GetProductByIdQuery` | — | `GetProductByIdQueryHandler` | AllowAnonymous (drafts owner-gated `:37-38`) | — | `Queries/GetProductById/*` in unit project | — | no |
| HEAD `{productId}` | `ProductExistsQuery` / `GetProductByIdQuery` | — | `ProductExistsHandler` (`Commands/ProductStatusCommands.cs:224`) | AllowAnonymous | — | — | — | no |
| GET `{productId}/pricing` | `GetProductPricingQuery` | — | `GetProductPricingQueryHandler` | AllowAnonymous | — | — | — | no |
| PUT `{productId}/pricing` | `SetProductPricingCommand` | `SetProductPricingCommandValidator` | `SetProductPricingCommandHandler` | `RequirePermission(ProductsPermission.Keys.PricingManage)` + monetize command gate | — | `Commands/SetProductPricing/*` | `MonetizationPermissionHttpTests.cs` | no rate limit |
| GET `{productId}/availability` | `GetProductAvailabilityQuery` | — | handler (`:119`) | AllowAnonymous | — | — | — | no |
| PUT `{productId}/inventory` | `SetProductInventoryCommand` | — | inventory handler | `RequirePermission(Manage)` | — | — | — | no validator file |
| GET (list) | `GetProductsPagedQuery` | `GetProductsPagedQueryValidator` | `GetProductsPagedQueryHandler` | AllowAnonymous | — | — | — | no |
| POST | `CreateProductCommand` | `CreateProductCommandValidator` | `CreateProductCommandHandler` | `RequirePermission(Create)` | — | `Commands/CreateProduct/*` | — | no |
| POST `:batch-create` | `BatchCreateProductsCommand` | — (item validation in create validator reuse) | `BatchCreateProductsHandler` (`Commands/ProductStatusCommands.cs:187`) | `RequirePermission(Create)` | — | — | — | no dedicated validator/test |
| PUT `{productId}` | `UpdateProductCommand` | `UpdateProductCommandValidator` | `UpdateProductCommandHandler` | `RequirePermission(Update)` + owner | — | `Commands/UpdateProduct/*` | — | no |
| PATCH `{productId}` | `PatchProductCommand` | — | `PatchProductHandler` (`Commands/ProductStatusCommands.cs:121`) | `RequirePermission(Update)` + owner | — | — | — | no validator |
| POST `{productId}:activate` | `ActivateProductCommand` | — | `ActivateProductHandler` (`:14`) | `RequirePermission(Update)` + owner | — | — | — | no validator |
| POST `{productId}:deactivate` | `DeactivateProductCommand` | — | `DeactivateProductHandler` (`:41`) | `RequirePermission(Update)` + owner | — | — | — | no validator |
| POST `{productId}:archive` | `ArchiveProductCommand` | — | `ArchiveProductHandler` (`:68`) | `RequirePermission(Delete)` + owner | — | — | — | no validator |
| DELETE `{productId}` | `DeleteProductCommand` | `DeleteProductCommandValidator` | `DeleteProductCommandHandler` | `RequirePermission(Delete)` + owner | — | `Commands/DeleteProduct/*` | — | no |

### PromoCodesController (15), PromoCampaignsController (11), PromoStackingRulesController (5)

`Products:/Controllers/PromoCodesController.cs`, `PromoCampaignsController.cs`, `PromoStackingRulesController.cs`. Representative wiring (all follow the same shape — command + validator + handler, admin permissions for mutations, `AllowAnonymous`/authenticated reads per controller):

| Route family | Requests | Validator | Handlers | Auth | HOLE? |
|---|---|---|---|---|---|
| promo codes CRUD + `:activate`/`:deactivate` + bulk-generate + validate + apply | `CreatePromoCodeCommand`, `UpdatePromoCodeCommand`/`PatchPromoCodeCommand`, `DeletePromoCodeCommand`, `GenerateBulkPromoCodesCommand`, `ValidatePromoCodeCommand`, `ApplyPromoCodesCommand` (+status commands) | per-command validators in `Commands/*/{name}Validator.cs` | `Commands/*/{name}CommandHandler.cs` | typed promo permissions; validate/apply reachable by users | **apply/validate degrade in host (P4): captcha/anomaly/customer-state services null**; `IPromoCodeUsageRecorder`/`IPromoCodeAuditLogger` explicitly registered — apply itself works |
| campaigns CRUD + `:activate`/`:pause`/`:complete`/`:cancel` + performance | `PromoCampaignCommands.cs` records | inline/none | `PromoCampaignCommands.cs` handlers | admin | no validators on lifecycle actions |
| stacking rules CRUD | `PromoStackingRuleCommands.cs` | — | same-file handlers | admin | no validators |

### PricingRulesController (9) / PricingExperimentsController (9)

`Products:/Controllers/PricingRulesController.cs`, `PricingExperimentsController.cs`; entities in `Commerce:/Entities/PricingRule.cs`, `PricingExperiment.cs`; use-case contracts in `Products:/Architecture/PricingRuleUseCaseEventContracts` (via `UseCaseEventContracts.g.cs:1-6`) and `PricingExperimentUseCaseEventContracts.cs`.

| Route family | Requests | Validator | Handlers | Auth | HOLE? |
|---|---|---|---|---|---|
| pricing rules CRUD + activate/deactivate | `CreatePricingRuleCommand`, `UpdatePricingRuleCommand`, `DeletePricingRuleCommand`, `PricingRuleStatusCommands.cs` | validators per command | `Commands/CreatePricingRule/…` + `PricingRuleMutationHelpers.cs` | admin | **scheduled activation never runs — worker dead (P1)** |
| experiments CRUD + start/pause/complete + assignments/outcomes | `PricingExperimentCommands.cs`, `PricingExperimentQueries.cs` | — | same-file handlers + `Services/PricingExperimentService.cs` | admin | scheduling sweep dead (P1); date-bounded auto start/complete inert |

### EntitlementsController (5) / UserEntitlementsController (2) / SupportTicketsController (12) / MySupportTicketsController (4) / ProductCategoriesController (5)

| Controller | Requests | Auth | HOLE? |
|---|---|---|---|
| EntitlementsController (`v{v}/entitlements`) | reads via `IEntitlementService` direct; `EntitlementMutationCommands.cs` (grant/revoke) | `RequirePermission(EntitlementsPermission.Keys.ReadAll)` on reads; typed on mutations | no rate limit |
| UserEntitlementsController | self-scoped reads | `[Authorize]` self | none |
| SupportTicketsController | `SupportTicketCommands.cs` (create/start/assign/priority/resolve/close/reopen/message) | typed support permissions | no validators on transitions (state guards live in entity, `Entities/SupportTicket.cs:139-186`) |
| MySupportTicketsController | customer self-service reads + message | `[Authorize]` self | none |
| ProductCategoriesController | `ProductCategoryCommands.cs` + `ProductCategoryQueries.cs` | admin for mutations | delete guarded by product references (`Services/ProductCategoryValidator.cs`) |

## 2. Verdict

Every endpoint has a route→request→handler chain (module scan guarantees handlers; G1 baseline clean). The holes are **service-level, not endpoint-level**: the promo/defenses/scheduler/coordinator registrations that only `AddProductsModule` makes (P1-P4). This is the same class as Consent N1 (dead registration method) but with a convention scan masking most of the blast radius — only the non-convention services fail, and three of those degrade silently because their consumers accept null.

# Products — CQRS / wiring matrix

> Module `GameGuild.Commerce.Products` · Documented against `develop` commit `60100459e6`.
> Citation shorthand: `Products:` = `apps/api/Source/Modules/GameGuild.Commerce.Products/`, `API:` = `apps/api/Source/GameGuild.API/`.

## 0. DI wiring — the load-bearing section

Three different mechanisms compose this module, and **one registration method is dead**:

| Mechanism | What it registers | Evidence | Status |
|---|---|---|---|
| Module scan (CQRS) | all command/query handlers + FluentValidation validators from the assembly | `API:/Core/Setup/ApplicationLayerExtensions.cs:110-145`; `Commerce.Products` in `ModuleConfiguration.CommonEnabledModules` (`API:/Core/Setup/ModuleConfiguration.cs:20`) | **live** — the G1 guard's empty baseline section confirms no handler is unregistered |
| Convention scan (repositories/services) | `I{Name}Repository → {Name}Repository` and `I{Name}Service → {Name}Service` (+`Reader`) from enabled module assemblies, scoped | `API:/Core/Setup/InfrastructureLayerExtensions.cs:482-560` (patterns at `:533-557`); unmatched-type warning at `:562+` | **live** — covers the 10 repositories and 8 `*Service` interfaces |
| Explicit host registrations | `IPromoCodeUsageRecorder`, `IPromoCodeAuditLogger`, `IPromoDiscountCalculator` (adapter), `IProductCategoryValidator`, `IProductCatalogAuditLogger`, `IBundleSavingsCalculator` — none match the convention | `API:/Core/Setup/InfrastructureLayerExtensions.cs:275-290` | **live** (issues #418/#417/#401 called out in comments) |
| EF model | `ProductsModelConfiguration` (IModelConfiguration) incl. `ProductsModule.ConfigureProductsModel` (`Products:/Configuration/ProductsModelConfiguration.cs:20`) | assembly-scanned in `API:/Database/ApplicationDbContext.cs:866-883` | **live — tables materialized** |
| `AddProductsModule` | the six above-named registrations PLUS: `AddHostedService<PricingSchedulingBackgroundService>`, `AddOptions` for 4 options classes, `ISubscriptionPromoCoordinator`, `IFraudReviewTicketCoordinator`, `IPromoCustomerStateResolver`, `IPromoRedemptionCaptchaGate`, `IPromoUsageAnomalyEvaluator`, `IPromoDiscountCalculator` (dup) | `Products:/ProductsModule.cs:16-66` — **the only caller in the tree is its own unit test** (`apps/api/tests/GameGuild.Commerce.Products.UnitTests/ModuleAndInfrastructureTests.cs:22`) | **DEAD — never invoked by the host** (gap P1) |

Runtime consequences of the dead method (all traced in `06-gaps.md`):

- `PricingSchedulingBackgroundService` never hosted — scheduled pricing rules/experiments never transition on time (`ProductsModule.cs:54`).
- `IOptions<PricingSchedulingOptions/PromoEngineOptions/PromoAbuseDetectionOptions/PromoRedemptionCaptchaOptions>` resolve framework defaults only; **no configuration section is bound anywhere** (not even inside `AddProductsModule` — `AddOptions<T>()` without `.Bind` at `:53-60`; sections `PricingScheduling:*`, `PromoCodes:*`, `PromoAbuseDetection:*` are inert).
- `ISubscriptionPromoCoordinator`, `IFraudReviewTicketCoordinator`, `IPromoCustomerStateResolver`, `IPromoRedemptionCaptchaGate`, `IPromoUsageAnomalyEvaluator` unregistered → P2 (hard failure), P3/P4 (silent null degradation).

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

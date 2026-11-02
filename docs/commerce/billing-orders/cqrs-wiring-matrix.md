# CQRS Wiring Matrix — Billing, Orders, Subscriptions Controllers

> Modules `GameGuild.Commerce.Billing` + `GameGuild.Commerce.Orders` + `GameGuild.Commerce.Subscriptions` · Documented against `develop` commit `74e9bba4c`.
> Every controller action → command/query → validator → handler → auth → rate limit → tests → holes. Handlers are registered by assembly scanning (`services.AddCqrs(assemblies)` in `GameGuild.API/Core/Setup/ApplicationLayerExtensions.cs:133-145`); the Subscriptions module additionally registers a subset explicitly (`GameGuild.Commerce.Subscriptions/Extensions/DependencyInjection.cs:73+`) — both styles resolve. FluentValidation validators auto-run through `ValidationBehavior` (`GameGuild.SharedKernel/CQRS/Behaviors/ValidationBehavior.cs:13`); a missing validator means no pre-handler validation, not a failure.

**Auth legend** — `[Authorize]` = any authenticated principal; `Policies.SystemAdmin` / `SystemConfigurationManage` / `SystemDataMigration` = admin policies; `RequirePermission(X)` = ABAC permission; `AllowAnonymous` = anonymous ingress (webhooks/plan catalog). **Rate limit legend** — `Api` / `ExpensiveOperations` / `Webhook` from `RateLimitPolicies` (`GameGuild.SharedKernel/Configuration/PresentationLayer/RateLimiting/RateLimitPolicies.cs`); “—” = no policy on the action or controller (global limiter only, if any).

Test paths are relative to `apps/api/tests/`.

## 1. GameGuild.Commerce.Billing — controllers

### BillingEventsController (`api/v{v}/billing/events`, controller `[Authorize(Policy = Policies.SystemAdmin)]`, `Controllers/BillingEventsController.cs:20-22`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` | `ListBillingEventsQuery` | `ListBillingEventsQueryValidator` (inline) | `ListBillingEventsQueryHandler` (inline) | SystemAdmin | — | `Queries/ListBillingEventsQueryHandlerTests.cs` | — | no |
| GET `{eventId:guid}` | `GetWebhookEventQuery` | `GetWebhookEventQueryValidator` | `GetWebhookEventHandler` | SystemAdmin | — | `Queries/GetWebhookEventHandlerTests.cs` | — | no |
| GET `outbox` | `ListBillingOutboxEventsQuery` | inline validator | `ListBillingOutboxEventsQueryHandler` | SystemAdmin | — | — (covered via `Queries/PricingCalculationValidationTests` infra) | — | test gap only |
| GET `outbox/{eventId:guid}` | `GetBillingOutboxEventQuery` | inline validator | `GetBillingOutboxEventQueryHandler` | SystemAdmin | — | — | — | test gap only |
| GET `subscriptions/{subscriptionId:guid}/projection` | `GetSubscriptionEventProjectionQuery` | — | `GetSubscriptionEventProjectionQueryHandler` | SystemAdmin | — | `Services/SubscriptionEventStateProjectionServiceTests.cs` | — | no |

### BillingExternalProvidersController (`api/v{v}/billing/external-providers`, controller `[EnableRateLimiting(Api)]`, `Controllers/BillingExternalProvidersController.cs:27-29`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` | `GetExternalBillingProvidersQuery` | — | registry-backed handler | SystemConfigurationManage | Api | `Services/ExternalBillingProviderRegistryTests.cs` | `ExternalBillingProviderEndpointsIntegrationTests.cs` | no |
| GET `{providerKey}` | `GetExternalBillingProviderQuery` | — | registry-backed handler | SystemConfigurationManage | Api | same | same | no |
| POST `{providerKey}:enable` | `SetExternalBillingProviderEnabledCommand` | inline (`...Command.cs:23`) | inline (`:38`) | SystemConfigurationManage | ExpensiveOperations | `Commands/SetExternalBillingProviderEnabledCommandHandlerTests.cs` | same integration file | no |
| POST `{providerKey}:disable` | same command | inline | inline | SystemConfigurationManage | ExpensiveOperations | same | same | no |
| POST `migration:dry-run` | `MigrateBillingProviderCommand` | inline (`...Command.cs:25`) | inline (`:54`) | SystemDataMigration | ExpensiveOperations | `Commands/MigrateBillingProviderCommandHandlerTests.cs` | same integration file | no |

### BillingInvoicesController (`api/v{v}/billing/invoices`, controller `[Authorize]` + `[EnableRateLimiting(Api)]`, `Controllers/BillingInvoicesController.cs:12-15`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `{invoiceId:guid}/retry` | `RetryInvoicePaymentCommand` | **none** | `RetryInvoicePaymentHandler` | any authenticated | Api | — | — | **YES — G-1** handler is a stub that schedules nothing; no validator; no ownership/tenant check beyond route id |
| POST `{invoiceId:guid}/account-credits` | `ApplyInvoiceAccountCreditsCommand` | **none** | `ApplyInvoiceAccountCreditsHandler` | any authenticated | Api | `Commands/ApplyInvoiceAccountCreditsHandlerTests.cs` | — | validator missing (test gap minor) |

### BillingWebhooksController (`api/v{v}/billing/webhooks`, `Controllers/BillingWebhooksController.cs:28`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `google-pay` | `ProcessGooglePayWebhookCommand` | `ProcessGooglePayWebhookCommandValidator` | `ProcessGooglePayWebhookCommandHandler` | AllowAnonymous | Webhook | `Commands/ProcessGooglePayWebhookCommandHandlerTests.cs`, `Services/GooglePayBillingWebhookServiceTests.cs` | `BillingWebhookEndpointsIntegrationTests.cs` | no |
| POST `apple-pay` | `ProcessApplePayWebhookCommand` | validator | `ProcessApplePayWebhookCommandHandler` | AllowAnonymous | Webhook | `Commands/ProcessApplePayWebhookCommandHandlerTests.cs`, `Services/ApplePayBillingWebhookServiceTests.cs` | same | no |
| POST `stripe` | `ProcessStripeWebhookCommand` | validator | `ProcessStripeWebhookCommandHandler` | AllowAnonymous | Webhook | `Commands/ProcessStripeWebhookCommandHandlerTests.cs`, `Services/StripeBillingWebhookServiceTests.cs` | same + `StripeDisputeWebhookTests.cs`, `StripePaymentLifecycleWebhookTests.cs` | no |
| POST `paypal` | `ProcessPayPalWebhookCommand` | validator | `ProcessPayPalWebhookCommandHandler` | AllowAnonymous | Webhook | `Commands/ProcessPayPalWebhookCommandHandlerTests.cs`, `Services/PayPalBillingWebhookServiceTests.cs` | same | no |
| POST `braintree` | `ProcessBraintreeWebhookCommand` | validator | `ProcessBraintreeWebhookCommandHandler` | AllowAnonymous | Webhook | `Commands/ProcessBraintreeWebhookCommandValidatorTests.cs`, `Services/BraintreeBillingWebhookServiceTests.cs` | same | no |
| GET `webhook-events/{eventId}` | `GetWebhookEventQuery` | `GetWebhookEventQueryValidator` | `GetWebhookEventHandler` | SystemAdmin | — | `Queries/GetWebhookEventHandlerTests.cs` | same | no |
| POST `webhook-events/{eventId}:retry` | `RetryWebhookEventCommand` | `RetryWebhookEventCommandValidator` | `RetryWebhookEventHandler` | SystemAdmin | — | `Commands/RetryWebhookEventHandlerTests.cs` | same | no |
| GET `security` | `GetBillingWebhookSecuritySummaryQuery` | — | `GetBillingWebhookSecuritySummaryHandler` | SystemAdmin | — | `Queries/GetBillingWebhookSecuritySummaryHandlerTests.cs` (+caching) | same | no |
| GET `stats` | `GetBillingWebhookStatsQuery` | — | inline `GetBillingWebhookStatsQueryHandler` (`Queries/GetBillingWebhookStats/GetBillingWebhookStatsQuery.cs:80`) | SystemAdmin | Api | `Queries/GetBillingWebhookStatsQueryHandlerTests.cs` | same | no |
| POST `secrets:rotate` | `RotateWebhookSecretCommand` | inline (`...Command.cs:45`) | inline (`:63`) | SystemAdmin | Api | `Security/WebhookSecretRotationTests.cs` | same | no |

Dead wiring: `ProcessBillingWebhookCommand` + validator (`Commands/ProcessBillingWebhookCommand/`) have **no handler and no controller caller** — inert code, listed for completeness.

### PricingCalculationsController (`api/v{v}/pricing/calculations`, controller `[Authorize(Policy = Policies.SystemAdmin)]`, `Controllers/PricingCalculationsController.cs:20-22`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `{calculationId:guid}` | `GetPricingDecisionLedgerEntryQuery` | — | ledger repository read | SystemAdmin | — | `Queries/PricingCalculationValidationTests.cs` | — | no |
| POST `~/api/v{v}/pricing/validate/{calculationId:guid}` | `ValidatePricingCalculationQuery` | — | validation read | SystemAdmin | — | same | — | no |

### PricingDecisionLedgerController (`api/v{v}/billing/pricing-discounts-ledger`, controller SystemAdmin, `Controllers/PricingDecisionLedgerController.cs:22-24`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` | `ListPricingDecisionLedgerEntriesQuery` | — | repository read | SystemAdmin | — | `Repositories/PricingDecisionLedgerRepositoryTests.cs` | — | no |
| GET `{entryId:guid}` | `GetPricingDecisionLedgerEntryQuery` | — | repository read | SystemAdmin | — | same | — | no |
| GET `analytics/effectiveness` | `GetPricingLedgerEffectivenessQuery` | — | repository read | SystemAdmin | — | same | — | no |
| POST `:validate-drift` | `ValidatePricingLedgerDriftCommand` | inline (`...Command.cs:58`) | inline (`:71`) | SystemAdmin | — | `Commands/ValidatePricingLedgerDriftCommandHandlerTests.cs` | — | no |

### SubscriptionChangeOrchestrationController (`api/v{v}/billing/subscriptions/{subscriptionId:guid}`, controller `[Authorize]` + `[EnableRateLimiting(Api)]`, `Controllers/SubscriptionChangeOrchestrationController.cs:19-22`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `plan-change:orchestrate` | `OrchestrateSubscriptionPlanChangeCommand` | inline validator (`...Command.cs:30`) | inline handler (`:54`) → `SubscriptionChangeSagaOrchestrator` | any authenticated | Api | `Services/SubscriptionChangeSagaOrchestratorTests.cs`, `Services/SubscriptionChangeAuthorizationTests.cs` | — | no dedicated endpoint integration test |
| GET `plan-change:saga` | `GetSubscriptionChangeSagaQuery` | — | `GetSubscriptionChangeSagaQueryHandler` | any authenticated | Api | `Queries/GetSubscriptionChangeSagaQueryHandlerTests.cs` | — | no |

## 2. GameGuild.Commerce.Orders — controllers

### OrdersController (`v{v}/orders`, controller `[Authorize]`, no rate-limit policy, `Controllers/OrdersController.cs:16-18`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `` | `CreateOrderCommand` | `CreateOrderCommandValidator` | `CreateOrderCommandHandler` | RequirePermission Create | — | `AuthoritativeOrderCommandTests.cs` | `OrderWorkflowIntegrationTests.cs` | no |
| POST `{orderId}/items` | `AddProductToOrderCommand` | validator | `AddProductToOrderCommandHandler` | RequirePermission Create | — | same + `ProductCatalogIntegrationTests.cs` | same | no |
| POST `{orderId}:complete` | `CompleteOrderCommand` | `CompleteOrderCommandValidator` | `CompleteOrderCommandHandler` | RequirePermission Create | — | `AuthoritativeOrderCommandTests.cs` | same | no |
| POST `{orderId}:cancel` | `CancelOrderCommand` | `CancelOrderCommandValidator` | `CancelOrderCommandHandler` | RequirePermission Create | — | same | same | ownership check absent in handler (G-6) |
| POST `{orderId}:refund` | `RefundOrderCommand` | `RefundOrderCommandValidator` | `RefundOrderCommandHandler` | RequirePermission Refund | — | `HandlersAndQueriesTests.cs` | same | handler excludes Fulfilled orders + no gateway refund (G-4) |
| GET `{orderId}` | `GetOrderQuery` | — | `GetOrderQueryHandler` | RequirePermission Read (+controller ownership `:189-193`) | — | `HandlersAndQueriesTests.cs` | same | no |
| GET `` | `GetAllOrdersQuery` / `GetUserOrdersQuery` | — | handlers | RequirePermission Read (`owner=me` vs ReadAll `:217-224`) | — | same | same | no |
| HEAD `{orderId}` | `OrderExistsQuery` | — | `OrderExistsQueryHandler` | RequirePermission Read | — | same | — | no |
| PATCH `{orderId}` | `UpdateOrderCommand` | validator | `UpdateOrderCommandHandler` | RequirePermission Create | — | `Validators/OrderCommandValidatorsTests.cs` | — | ownership check absent (G-6) |
| DELETE `{orderId}` | `DeleteOrderCommand` | validator | `DeleteOrderCommandHandler` | RequirePermission Delete | — | same | — | ownership check absent (G-6) |
| POST `{orderId}:capture` | `CaptureOrderCommand` | **none** | `CaptureOrderCommandHandler` | RequirePermission Create + in-handler `OrderActorContext.Authorize` | — | `AuthoritativeOrderCommandTests.cs` | same | validator missing (minor) |
| POST `{orderId}:payment-intent` | `PrepareOrderPaymentIntentCommand` | **none** | inline `PrepareOrderPaymentIntentCommandHandler` (`Commands/PrepareOrderPaymentIntent/PrepareOrderPaymentIntentCommand.cs:8`) | RequirePermission Create | — | `PrepareOrderPaymentIntentCommandTests.cs` | same | validator missing (minor) |
| POST `{orderId}:hold` | `HoldOrderCommand` | **none** | `HoldOrderCommandHandler` | RequirePermission Create | — | `HandlersAndQueriesTests.cs` | — | validator missing; ownership absent (G-6) |
| POST `{orderId}:release` | `ReleaseOrderCommand` | **none** | `ReleaseOrderCommandHandler` | RequirePermission Create | — | same | — | same as hold |

### MarketplaceCartController (`v{v}/marketplace/cart`, controller `[Authorize]`, `Controllers/MarketplaceCartController.cs:13-15`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` | `GetMarketplaceCartQuery` | — | handler in `Commands/MarketplaceCartCommands.cs` | any authenticated | — | `MarketplaceCartTests.cs` | — | no |
| POST `items` | `AddMarketplaceCartItemCommand` | inline input checks | multi-handler class (`MarketplaceCartCommands.cs:21-24`) | any authenticated | — | same | — | no |
| PATCH `items/{itemId}` | `SetMarketplaceCartItemQuantityCommand` | inline | same | any authenticated | — | same | — | no |
| DELETE `items/{itemId}` | `RemoveMarketplaceCartItemCommand` | inline | same | any authenticated | — | same | — | no |
| POST `checkout` | `CheckoutMarketplaceCartCommand` | inline (idempotency key `:146`) | same | any authenticated | — | same | `OrderWorkflowIntegrationTests.cs` | no |

### ProductRecommendationsController (`v{v}/products`, controller `[Authorize]`, `Controllers/ProductRecommendationsController.cs:15-17`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `{productId}/recommendations` | `GetProductRecommendationsQuery` | — | product-catalog handler | **AllowAnonymous** | — | `ProductCatalogIntegrationTests.cs` | — | no |

## 3. GameGuild.Commerce.Subscriptions — controllers

### SubscriptionsController (`api`, controller `[Authorize]` + `[EnableRateLimiting(ExpensiveOperations)]`, `Controllers/SubscriptionsController.cs:21-24`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `v{v}/subscriptions` | `CreateSubscriptionCommand` | `CreateSubscriptionCommandValidator` | `CreateSubscriptionCommandHandler` | any authenticated (+in-controller tenant validation `:41`) | ExpensiveOperations | `Commands/CreateSubscriptionCommandHandlerTests.cs` (+Fraud/Promo variants) | `SubscriptionEndpointIntegrationTests.cs`, `Security/AuthenticationAndTenantIsolationTests.cs` | no |
| GET `v{v}/subscriptions` | `GetPagedSubscriptionsQuery` (+`GetExpiringSubscriptionsQuery` when `expiring`) | — | `GetPagedSubscriptionsQueryHandler` | any authenticated | Api (action) | `Controllers/SubscriptionsControllerTests.cs` | `SubscriptionEndpointsIntegrationTests.cs` | no |
| GET `{subscriptionId}` | `GetSubscriptionByIdQuery` | `GetSubscriptionByIdQueryValidator` | `GetSubscriptionByIdQueryHandler` | any authenticated | Api | `Queries/…` validator tests | same | no |
| PATCH `{subscriptionId}` | `PatchSubscriptionCommand` | **none** | `PatchSubscriptionHandler` | any authenticated | ExpensiveOperations | `Commands/PatchSubscriptionHandlerTests.cs` | — | validator missing; tenant check is in-command only |
| PUT `{subscriptionId}` | `UpdateSubscriptionCommand` | **none** | `UpdateSubscriptionHandler` | any authenticated | ExpensiveOperations | `Commands/UpdateSubscriptionCommandTests.cs` | — | validator missing |
| DELETE `{subscriptionId}` | `DeleteSubscriptionCommand` | **none** | `DeleteSubscriptionHandler` | any authenticated | ExpensiveOperations | `Commands/DeleteSubscriptionCommandTests.cs` | — | validator missing; destructive op guarded only by `[Authorize]` (G-9) |

### BillingSubscriptionsController (`api/v{v}/billing/subscriptions`, controller `[Authorize]` + `[EnableRateLimiting(ExpensiveOperations)]`, `Controllers/BillingSubscriptionsController.cs:13-16`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` | `GetPagedSubscriptionsQuery` | — | handler | any authenticated | Api | `Controllers/BillingSubscriptionsAndNotificationsControllerTests.cs` | `SubscriptionEndpointsIntegrationTests.cs` | no |
| GET `{subscriptionId}` | `GetSubscriptionByIdQuery` | validator | handler | any authenticated | Api | same | same | no |
| POST `` | `CreateSubscriptionCommand` | validator | `CreateSubscriptionCommandHandler` | any authenticated | ExpensiveOperations | `Commands/CreateSubscriptionCommandHandlerTests.cs` | same | no |
| POST `{subscriptionId}:renew` | `ProcessSubscriptionRenewalCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/ProcessSubscriptionRenewalCommandHandlerTests.cs` | — | no |
| POST `{subscriptionId}:cancel` | `CancelSubscriptionCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/CancelSubscriptionCommandHandlerTests.cs` | — | no |

### SubscriptionLifecycleController (`api`, controller `[Authorize]` + `[EnableRateLimiting(ExpensiveOperations)]`, `Controllers/SubscriptionLifecycleController.cs:19-22`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `{id}:activate` | `ActivateSubscriptionCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/ActivateSubscriptionCommandHandlerTests.cs` | `SubscriptionEndpointIntegrationTests.cs` | no |
| POST `{id}:start-trial` | `StartSubscriptionTrialCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/StartSubscriptionTrialCommandTests.cs` | same | no |
| POST `{id}:end-trial` | `EndSubscriptionTrialCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/EndSubscriptionTrialCommandTests.cs` | same | no |
| POST `{id}:cancel` | `CancelSubscriptionCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/CancelSubscriptionCommandHandlerTests.cs` | same | no |
| POST `{id}:suspend` | `SuspendSubscriptionCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/SuspendSubscriptionCommandHandlerTests.cs` | same | no |
| POST `{id}:pause` | `PauseSubscriptionCommand` | **none** | `PauseSubscriptionHandler` (suspends, `Commands/PauseSubscription/PauseSubscriptionHandler.cs:20-21`) | any authenticated | ExpensiveOperations | `Commands/PauseSubscriptionHandlerTests.cs` | same | pause ≡ suspend — clobbers metadata (G-10) |
| POST `{id}:resume` | `ResumeSubscriptionCommand` | **none** | `ResumeSubscriptionHandler` (reactivates `:20`) | any authenticated | ExpensiveOperations | `Commands/ResumeSubscriptionHandlerTests.cs` | same | resume force-enables AutoRenew (G-10) |
| POST `{id}:reactivate` | `ReactivateSubscriptionCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/ReactivateSubscriptionCommandTests.cs` | same | no |
| POST `{id}:upgrade` | `UpgradeSubscriptionPlanCommand` | validator | handler (+fraud gate) | any authenticated | ExpensiveOperations | `Commands/UpgradeSubscriptionPlanCommandTests.cs` (+Promo) | same | no |
| POST `{id}:downgrade` | `DowngradeSubscriptionPlanCommand` | validator | handler (+fraud gate) | any authenticated | ExpensiveOperations | `Commands/DowngradeSubscriptionPlanCommandTests.cs` | same | no |
| POST `{id}:renew` | `ProcessSubscriptionRenewalCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/ProcessSubscriptionRenewalCommandTests.cs` | same | no |
| POST `{id}:auto-renew` | `SetSubscriptionAutoRenewCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/SetSubscriptionAutoRenewCommandHandlerTests.cs` | same | no |
| POST `{id}:external-ids` | `SetSubscriptionExternalIdsCommand` | validator | handler | any authenticated | ExpensiveOperations | `Commands/SetSubscriptionExternalIdsCommandTests.cs` | same | no |

### SubscriptionBillingController (`api`, controller `[Authorize]` + `[EnableRateLimiting(Api)]`, `Controllers/SubscriptionBillingController.cs:18-21`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `subscriptions:get-metrics` | `GetSubscriptionStatusCountsQuery` | — | `GetSubscriptionStatusCountsQueryHandler` | any authenticated | Api | `Services/SubscriptionServiceTests.cs` | — | no |
| GET `{id}/invoices` | `GetSubscriptionInvoicesQuery` | — | `GetSubscriptionInvoicesHandler` | any authenticated | Api | `Queries/GetSubscriptionInvoicesHandlerTests.cs` | — | no |
| GET `{id}/usage` | `GetSubscriptionUsageQuery` | — | **NO HANDLER EXISTS** | any authenticated | Api | — | — | **YES — G-8** runtime failure, no handler registered |
| GET `{id}/billing-history` | `GetSubscriptionBillingHistoryQuery` | — | **NO HANDLER EXISTS** | any authenticated | Api | — | — | **YES — G-8** |
| GET `{id}/breakdown` | `GetSubscriptionPricingBreakdownQuery` | — | `GetSubscriptionPricingBreakdownQueryHandler` (`Queries/GetSubscriptionPricingBreakdown/GetSubscriptionPricingBreakdownQueries.cs:28`) | any authenticated | Api | `Services/SubscriptionPlanPricingResolverTests.cs` | `SubscriptionPlanPricingResolverIntegrationTests.cs` | no |
| POST `{id}/pricing:preview` | `PreviewSubscriptionPlanChangeQuery` | — | `PreviewSubscriptionPlanChangeQueryHandler` (`:84`) | any authenticated | Api | same | same | no |

### SubscriptionPlansCrudController (`api`, controller `[Authorize]`, `Controllers/SubscriptionPlansCrudController.cs:18`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `v{v}/subscription-plans` | `CreateSubscriptionPlanCommand` | validator | `CreateSubscriptionPlanCommandHandler` | RequirePermission Monetization Configure | — | `Commands/CreateSubscriptionPlanCommandHandlerTests.cs` | `SubscriptionEndpointsIntegrationTests.cs` | no |
| GET `v{v}/subscription-plans` | slug/price/active/featured/search/paged query fan-out (`:103-156`) | — | respective handlers | **AllowAnonymous** | — | `Controllers/SubscriptionPlansCrudControllerTests.cs` | same | catalog intentionally public |
| POST `v{v}/subscription-plans:compare` | `CompareSubscriptionPlansQuery` | — | `CompareSubscriptionPlansQueryHandler` | any authenticated | — | `Queries/CompareSubscriptionPlansQueryHandlerTests.cs` | — | no |
| GET `{planId}` | `GetSubscriptionPlanByIdQuery` | validator | handler | any authenticated | — | `Queries/GetSubscriptionPlanByIdQueryValidatorTests.cs` | — | no |
| DELETE `{planId}` | `DeleteSubscriptionPlanCommand` | validator | shared handler (`Commands/SubscriptionPlanCommandHandlers.cs:131`) | RequirePermission Configure | — | `Commands/DeleteSubscriptionPlanCommandHandlerTests.cs` | — | no |
| PUT `{planId}` | `FullUpdateSubscriptionPlanCommand` | validator | `FullUpdateSubscriptionPlanHandler` | RequirePermission Configure | — | `Commands/FullUpdateSubscriptionPlanHandlerTests.cs` | — | no |

### SubscriptionPlanOperationsController (`api`, controller `[Authorize]`, `Controllers/SubscriptionPlanOperationsController.cs:16-18`)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `{planId}/usage` | `GetSubscriptionPlanUsageStatisticsQuery` | — | **NO HANDLER EXISTS** | any authenticated | — | — | — | **YES — G-8** |
| GET `{planId}/suggest-upgrades` | `SuggestSubscriptionPlanUpgradesQuery` | — | **NO HANDLER EXISTS** | any authenticated | — | — | — | **YES — G-8** |
| GET `{planId}/pricing` | `CalculatePricingQuery` (Payments module) | — | `CalculatePricingQueryHandler` (`GameGuild.Commerce.Payments/Queries/CalculatePricing/`) | any authenticated | — | `Services/SubscriptionPlanPricingRulesTests.cs` | — | no |
| POST `{planId}:validate-limits` | `ValidateSubscriptionPlanLimitsQuery` | — | **NO HANDLER EXISTS** | any authenticated | — | — | — | **YES — G-8** |
| PATCH `{planId}/details` | `UpdateSubscriptionPlanCommand` | validator | shared handler (`SubscriptionPlanCommandHandlers.cs:1`) | RequirePermission Configure | — | `Commands/UpdateSubscriptionPlanCommandTests.cs` | — | no |
| PATCH `{planId}/pricing` | `UpdateSubscriptionPlanPricingCommand` | validator | shared (`:24`) | RequirePermission Configure | — | `Commands/UpdateSubscriptionPlanPricingCommandTests.cs` | — | no |
| PATCH `{planId}/limits` | `UpdateSubscriptionPlanLimitsCommand` | validator | shared (`:42`) | RequirePermission Configure | — | `Commands/UpdateSubscriptionPlanLimitsCommandTests.cs` | — | no |
| PATCH `{planId}/features` | `UpdateSubscriptionPlanFeaturesCommand` | validator | shared (`:60`) | RequirePermission Configure | — | `Commands/UpdateSubscriptionPlanFeaturesCommandTests.cs` | — | no |
| POST `{planId}:activate` | `ActivateSubscriptionPlanCommand` | validator | shared (`:78`) | RequirePermission Configure | — | `Commands/ActivateSubscriptionPlanCommandTests.cs` | — | no |
| POST `{planId}:deactivate` | `DeactivateSubscriptionPlanCommand` | validator | shared (`:96`) | RequirePermission Configure | — | `Commands/DeactivateSubscriptionPlanCommandTests.cs` | — | no |
| POST `{planId}:archive` | `ArchiveSubscriptionPlanCommand` | **none** | `ArchiveSubscriptionPlanHandler` | RequirePermission Configure | — | `Commands/ArchiveSubscriptionPlanCommandTests.cs` | — | validator missing (minor) |
| POST `{planId}:clone` | `CloneSubscriptionPlanCommand` | **none** | `CloneSubscriptionPlanHandler` | RequirePermission Configure | — | `Commands/CloneSubscriptionPlanCommandTests.cs` | — | validator missing (minor) |
| POST `{planId}:featured` | `SetSubscriptionPlanFeaturedCommand` | validator | shared (`:114`) | RequirePermission Configure | — | `Commands/SetSubscriptionPlanFeaturedCommandTests.cs` | — | no |
| POST `{planId}:external-id` | `SetSubscriptionPlanExternalIdCommand` | validator | shared (`:132`) | RequirePermission Configure | — | `Commands/SetSubscriptionPlanExternalIdCommandTests.cs` | — | no |

### SubscriptionNotificationsController + SubscriptionReportsController + ClientsController (adjacent surfaces)

| Route | Command/Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `api/v{v}/notifications/subscriptions` | `GetSubscriptionNotificationsQuery` | — | `Queries/SubscriptionNotificationQueries.cs` handler | any authenticated | Api | `Controllers/BillingSubscriptionsAndNotificationsControllerTests.cs` | — | no |
| POST `api/v{v}/notifications/subscriptions/{id}:resend` | `ResendSubscriptionNotificationCommand` | — | same file | any authenticated | Api | same | — | no |
| GET `api/v{v}/reports/churn` | `GetSubscriptionChurnReportQuery` | — | `Queries/SubscriptionChurnReportQueries.cs` | any authenticated | Api | — | — | test gap only |
| `v{v}/clients` CRUD + modules (`Controllers/ClientsController.cs:19-137`) | `CreateTenantCommand` / `GetTenantsPageQuery` / `UpdateTenantCommand` / `ArchiveTenantCommand` / `GetTenantFeatureFlagsQuery` / `UpdateTenantFeatureFlagsCommand` (Identity.Tenants commands) | per Identity.Tenants | per Identity.Tenants | any authenticated | — | `Controllers/SubscriptionsControllerTests.cs` partially | `Security/CommerceSecurityIntegrationTests.cs` | out of module core — documented for completeness |

## 4. Cross-cutting wiring facts

1. **Handler discovery is assembly-scan based** — `AddCqrs(cqrsAssemblies)` (`GameGuild.API/Core/Setup/ApplicationLayerExtensions.cs:133-145`) registers every `ICommandHandler<,>`/`IQueryHandler<,>`/`INotificationHandler<>` in enabled module assemblies. A command/query type with no handler class fails **at Send time**, not at startup — that is why G-8 endpoints compile and 500 in production.
2. **Validators are optional per-type** — `ValidationBehavior` (`GameGuild.SharedKernel/CQRS/Behaviors/ValidationBehavior.cs:13`) short-circuits only when a validator is registered; the matrix marks every absent validator. Billing tests pin this behavior (`tests/GameGuild.Commerce.Billing.UnitTests/Cqrs/ValidationPipelineShortCircuitTests.cs`).
3. **Rate-limit posture is asymmetric** — Billing webhooks get the dedicated `Webhook` policy; subscription lifecycle mutators use `ExpensiveOperations` with reads on `Api`; the entire Orders controller has **no** policy (G-7).
4. **Ownership enforcement lives in handlers, inconsistently** — capture/complete call `OrderActorContext.Authorize` (`Commands/CaptureOrder/CaptureOrderCommandHandler.cs:26`, `Commands/CompleteOrder/CompleteOrderCommandHandler.cs:30`); cancel/update/delete/refund/hold/release do not (G-6). Subscription reads rely on query-level tenant filters; destructive `DELETE v{v}/subscriptions/{id}` has only `[Authorize]` (G-9).

# Payments CQRS Wiring Matrix

> Module `GameGuild.Commerce.Payments` · Documented against `develop` commit `74e9bba4c`.
> Columns: route → command/query → validator → handler → auth → rate limit → unit test → integration test → HOLE?
> Citation shorthand: `Payments:` = `apps/api/Source/Modules/GameGuild.Commerce.Payments/`, `Billing:` = `apps/api/Source/Modules/GameGuild.Commerce.Billing/`. Test paths are abbreviated to the Payments test projects.

## Auth model (applies to the whole matrix)

Three authorization layers, in execution order:

1. **Controller surface**: `[Authorize]` on every controller except webhook ingress; per-endpoint overrides `[Authorize(Roles = "SystemAdmin,Owner,TenantAdmin,Admin")]` (e.g. `CurrenciesController.cs:66,85,104`), `[AllowAnonymous]` for `currencies/suggestion` (`CurrenciesController.cs:43`) and webhook endpoints (`Billing:/Controllers/BillingWebhooksController.cs:61,127,187,254,320`), `[Authorize(Policy = Policies.SystemAdmin)]` for webhook admin surfaces (`BillingWebhooksController.cs:463,495,521,547,581`).
2. **CQRS pipeline behavior**: `[AuthorizeRequest(permission)]` on the request type, enforced by `AuthorizationBehavior` (`Identity.Authorization/Behaviors/AuthorizationBehavior.cs:49-156`) — registered globally at `Identity.Authorization/Extensions/AuthorizationModuleExtensions.cs:352`. Permission-bearing requests in this module: `GetFraudStatisticsQuery` (`payments:admin`, `Queries/GetFraudStatistics/GetFraudStatisticsQuery.cs:36`), refund decision commands (`payments:refunds:approve`), fraud review commands, gateway credential/test commands, FX operator commands, revenue-auditing commands.
3. **Controller tenant guard**: `ValidateTenantAccess` / `ResolveTenantScope` (`PaymentsController.cs:931-934`, `RefundsController.cs:248-275`) — non-system-admin actors are pinned to their authenticated tenant.

Rate limiting: controllers default to `RateLimitPolicies.ExpensiveOperations` (mutations), query endpoints override to `RateLimitPolicies.Api` (`SharedKernel/Configuration/PresentationLayer/RateLimiting/RateLimitPolicies.cs`), webhook ingress uses `RateLimitPolicies.Webhook` plus the `WebhookSourceSecurityFilter` (`BillingWebhooksController.cs:62-63`). `Currencies`, `Pricing`, `Taxes`, `TaxRules`, `TaxJurisdictions`, `Referrals`, `Gifts`, `AccountCredits`, `CurrencyConversion`, `PriceLocalization` controllers carry no `EnableRateLimiting` attribute — they run unthrottled (see gaps, `06-gaps.md` N5).

Validation: validators registered through the shared CQRS `ValidationBehavior` (`SharedKernel/CQRS/Behaviors/ValidationBehavior.cs`) run automatically when a validator class exists; "—" means no validator class exists and the guards live in the controller action or the handler/entity (HOLE only when neither exists).

## PaymentsController — `api/v1/payments` (`Payments:/Controllers/PaymentsController.cs`, controller `[Authorize]` L24 + `ExpensiveOperations` L25)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L67) | `GetAllPaymentsQuery` | — | `GetAllPaymentsQueryHandler` (`Queries/GetAllPayments/`) | `[Authorize]` + tenant scoping L86-94 | Api (L68) | `PaymentTests.cs`, `PaymentRecoveryQueriesTests.cs` | `PaymentEndpointsIntegrationTests.cs` | no validator — controller clamps paging L83-84 |
| POST `` (L122) | `ProcessPaymentCommand` (L169) | `ProcessPaymentCommandValidator` | `ProcessPaymentCommandHandler` (`Commands/ProcessPayment/ProcessPaymentCommandHandler.cs:14`) | `[Authorize]` + `ValidateTenantAccess` L138 | ExpensiveOperations | `ProcessPaymentCommandHandlerTests.cs`, `ProcessPaymentFraudGateTests.cs`, `ProcessPaymentCreditAutoApplyTests.cs`, `ProcessPaymentFraudAdminNoticeTests.cs` | `PaymentEndpointsIntegrationTests.cs` | covered |
| POST `setup-intents` (L201) | `CreateSetupIntentCommand` (L223) | — (inline guards L210-221) | inline in `Commands/CreateSetupIntent/CreateSetupIntentCommand.cs` | `[Authorize]` + tenant L220 | ExpensiveOperations | `StripePaymentIntentCreationTests.cs` | via checkout E2E | **no dedicated validator** (controller guards) |
| POST `subscription-checkouts:complete` (L241) | `SetDefaultPaymentMethod` service call (L296) + `UpsertCheckoutPaymentMethodCommand` (L907) + `ProcessPaymentCommand` (L311) | ProcessPayment validator on final charge | `StripeCustomerService.SetDefaultPaymentMethodAsync`; `ProcessPaymentCommandHandler` | `[Authorize]` + tenant L270 | ExpensiveOperations | `PaymentMethodRegistryServiceTests.cs`, `PaymentRetryMethodChangeServiceTests.cs` | — | **no integration test found for this composite action** |
| GET `{paymentId}` (L336) | `GetPaymentByIdQuery` (L346) | `GetPaymentByIdQueryValidator` | `GetPaymentByIdQueryHandler` | `[Authorize]` + tenant-after-read L350 | Api (L337) | `PaymentTests.cs` | `PaymentEndpointsIntegrationTests.cs` | covered |
| POST `{paymentId}:cancel` (L390) | `CancelPaymentCommand` (L406) | — | `CancelPaymentCommandHandler` | `[Authorize]` + tenant L403 | ExpensiveOperations | `EntitiesAndServicesTests.cs` | `PaymentEndpointsIntegrationTests.cs` | **no validator**; reason unbounded (see 06-gaps.md N7) |
| POST `{paymentId}:refund` (L437) | `ProcessRefundCommand` (L470) | `ProcessRefundCommandValidator` | `ProcessRefundCommandHandler` → `RefundProcessingService.ExecuteAsync` | `[Authorize]` + tenant L450 | ExpensiveOperations | `RefundLifecycleTests.cs`, `RefundDuplicateAndProrationTests.cs` | `PaymentEndpointsIntegrationTests.cs` | covered |
| POST `{paymentId}:retry` (L499) | `RetryPaymentCommand` (L515) | `RetryPaymentCommandValidator` | `RetryPaymentCommandHandler` | `[Authorize]` + tenant L512 | ExpensiveOperations | `RetryPaymentRecoveryLifecycleTests.cs`, `PaymentRetryQueueWorkerTests.cs` | — | **no integration test on retry action** |
| GET `recovery-metrics` (L536) | `GetPaymentRecoveryMetricsQuery` (L566) | — | `GetPaymentRecoveryMetricsQueryHandler` | `[Authorize]` + tenant scoping L556-564 | Api (L537) | `PaymentRecoveryQueriesTests.cs` | — | controller validates window L551 |
| GET `retrying-candidates` (L585) | `GetRetryingPaymentCandidatesQuery` (L608) | — | `GetRetryingPaymentCandidatesQueryHandler` | `[Authorize]` + tenant scoping | Api (L586) | `PaymentRetryQueueWorkerTests.cs` | — | covered |
| GET `{paymentId}:retry-attempts` (L626) | `GetPaymentRetryAttemptsQuery` (L643) | — | `GetPaymentRetryAttemptsQueryHandler` | `[Authorize]` + tenant L640 | Api (L627) | `PaymentRetryAttemptQueriesTests.cs` | — | covered |
| GET `gateways/metrics` (L654) | `GetGatewayMetricsQuery` (L679) | — | `GetGatewayMetricsQueryHandler` | `[Authorize]`, tenant required L666-677 | Api (L655) | `GetGatewayMetricsQueryTests.cs`, `GatewayMetricsServiceTests.cs` | — | docs claim `payments:admin` (L652) but **no `AuthorizeRequest` on the query** — gap N11 |
| GET `gateways/analytics` (L692) | `GetGatewayAnalyticsQuery` (L717) | — | `GetGatewayAnalyticsQueryHandler` | `[Authorize]`, tenant required | Api (L693) | `GatewayAnalyticsTests.cs` | — | same doc/attribute mismatch as above — gap N11 |
| POST `gateways/{providerId}/test-connection` (L733) | `TestGatewayConnectionCommand` (L746) | — | inline handler in `Commands/TestGatewayConnection/TestGatewayConnectionCommand.cs` | `[Authorize]` + admin claim in doc; query-level `AuthorizeRequest` present on command | ExpensiveOperations (L734) | `GatewayConnectionTests.cs` | — | covered |
| PUT `gateways/{providerId}/credentials/{secretName}` (L767) | `ProvisionGatewayCredentialCommand` (L789) | — | inline in `Commands/ManageGatewayCredentials/GatewayCredentialCommands.cs` | `[Authorize]` + `AuthorizeRequest` on command | ExpensiveOperations (L768) | `GatewayCredentialStoreTests.cs` | — | covered |
| DELETE `gateways/{providerId}/credentials/{secretName}` (L808) | `DeactivateGatewayCredentialCommand` (L821) | — | inline (same file) | `[Authorize]` + `AuthorizeRequest` | ExpensiveOperations (L809) | `GatewayCredentialStoreTests.cs` | — | covered |
| GET `refunds/metrics` (L835) | `GetRefundAnalyticsQuery` (L869) | — | `GetRefundAnalyticsQueryHandler` | `[Authorize]`, tenant required L856-867 | Api (L836) | `GetRefundAnalyticsQueryTests.cs`, `RefundAndDisputeStatisticsTests.cs` | — | covered |

## BillingChargesController — `api/v1/billing/charges` (aliases of the payments surface)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L20) | `GetAllPaymentsQuery` (L46) | — | shared handler | `[Authorize]` + tenant scoping | default | shared | `PaymentEndpointsIntegrationTests.cs` | covered |
| GET `{chargeId}` (L50) | `GetPaymentByIdQuery` (L57) | shared | shared | `[Authorize]` + tenant | default | shared | shared | covered |
| POST `` (L64) | order charge flow (`Services/OrderPaymentService.cs`) | — | `OrderPaymentService` | `[Authorize]` | default | `OrderPaymentServiceTests.cs`, `OrderPaymentIntentServiceTests.cs` | — | **POST creates charge without validator class** |
| POST `{chargeId}:retry` (L87) | `RetryPaymentCommand` (L98) | shared | shared | `[Authorize]` + tenant | default | shared | — | covered |
| POST `{chargeId}:refund` (L102) | `ProcessRefundCommand` (L114) — amount defaults `0` when body omits it (L114) | shared | shared | `[Authorize]` + tenant | default | shared | — | **default 0 refund leg rejected by domain — UX hole vs payments controller which coalesces null to remainder** (gap N12) |
| POST `{chargeId}:cancel` (L118) | `CancelPaymentCommand` (L130) | — | shared | `[Authorize]` + tenant | default | shared | — | covered |

## RefundsController — `api/v1/refunds` (`Payments:/Controllers/RefundsController.cs`, `[Authorize]` L23 + ExpensiveOperations L24)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `` (L36) | `RequestRefundCommand` (L57) | — (reason guard in controller L45) | `RequestRefundCommandHandler` → `RefundProcessingService.RequestRefundAsync` (`Services/RefundProcessingService.cs:142`) | `[Authorize]` + tenant via payment L51-55 | ExpensiveOperations | `RefundApprovalWorkflowTests.cs`, `RefundDuplicateAndProrationTests.cs` | — | **no validator class; no integration test on workflow queue** |
| GET `` (L77) | `ListRefundsQuery` (L98) | — | inline handler (`Queries/ListRefunds/ListRefundsQuery.cs`) | `[Authorize]` + tenant scoping L94-96 | Api (L78) | `RefundLifecycleTests.cs` | — | covered |
| GET `{refundId}` (L112) | `GetRefundByIdQuery` (L121) | — | inline (same file) | `[Authorize]` + tenant L124 | Api (L113) | `RefundLifecycleTests.cs` | — | covered |
| GET `pending-approvals` (L137) | `GetPendingRefundApprovalsQuery` (L155) | — | inline (`Queries/GetPendingRefundApprovals/`) | `[Authorize]` + `payments:refunds:approve` on query | Api (L138) | `RefundApprovalWorkflowTests.cs` | — | covered |
| POST `{refundId}:approve` (L165) | `ApproveRefundCommand` (L177) | — | `ApproveRefundCommandHandler` | `[Authorize]` + tenant L174 + `payments:refunds:approve` | ExpensiveOperations | `RefundApprovalWorkflowTests.cs` | — | covered |
| POST `{refundId}:reject` (L189) | `RejectRefundCommand` (L201) | — | `RejectRefundCommandHandler` | `[Authorize]` + tenant + permission | ExpensiveOperations | `RefundApprovalWorkflowTests.cs` | — | covered |
| GET `statistics` (L212) | `GetRefundStatisticsQuery` (L233) | — | inline (`Queries/GetRefundStatistics/`) | `[Authorize]` + tenant scoping | Api (L213) | `RefundAndDisputeStatisticsTests.cs` | — | covered |

## DisputesController — `api/v1/disputes` (`Payments:/Controllers/DisputesController.cs`, `[Authorize]` L22 + ExpensiveOperations L23)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L37) | `ListDisputesQuery` | — | inline (`Queries/ListDisputes/`) | `[Authorize]` | Api (L38) | `DisputesControllerTests.cs` | — | covered |
| GET `{disputeId}` (L89) | `GetDisputeByIdQuery` | — | `GetDisputeByIdQueryHandler` | `[Authorize]` | Api (L90) | `DisputesControllerTests.cs` | — | covered |
| GET `{disputeId}/evidence` (L112) | `GetDisputeEvidenceQuery` | — | `GetDisputeEvidenceQueryHandler` | `[Authorize]` | Api (L113) | `DisputesControllerTests.cs` | — | covered |
| POST `{disputeId}/evidence` (L136) | `AddDisputeEvidenceCommand` | `AddDisputeEvidenceCommandValidator` | `AddDisputeEvidenceCommandHandler` | `[Authorize]` | ExpensiveOperations | `SubmitDisputeEvidenceBundleTests.cs` | — | covered |
| POST `` (L174) | `CreateDisputeCommand` | `CreateDisputeCommandValidator` | `CreateDisputeCommandHandler` | `[Authorize]` | ExpensiveOperations | `DisputesControllerTests.cs` | — | covered |
| POST `{disputeId}:respond` (L213) | `UpdateDisputeStatusCommand` (L226) | `UpdateDisputeStatusCommandValidator` | `UpdateDisputeStatusCommandHandler` | `[Authorize]` | ExpensiveOperations | `DisputesControllerTests.cs` | — | **arbitrary status settable via respond — validator unverified against transition table** (gap N8) |
| POST `{disputeId}:accept` (L238) | `AcceptDisputeCommand` | — | `AcceptDisputeCommandHandler` (refund chain) | `[Authorize]` | ExpensiveOperations | `AcceptDisputeRefundChainTests.cs` | — | covered |
| POST `{disputeId}:challenge` (L263) | challenge via `ChallengeDisputeCommandHandler` | — | `ChallengeDisputeCommandHandler` (`Commands/AcceptDispute/`) | `[Authorize]` | ExpensiveOperations | `DisputeNotificationAndFeeTests.cs` | — | covered |
| POST `{disputeId}:submit-evidence` (L289) | `SubmitDisputeEvidenceCommand` | — | `SubmitDisputeEvidenceCommandHandler` | `[Authorize]` | ExpensiveOperations | `SubmitDisputeEvidenceBundleTests.cs` | — | covered |
| POST `{disputeId}:cancel` (L313) | `CancelDisputeCommand` | `CancelDisputeCommandValidator` | `CancelDisputeCommandHandler` | `[Authorize]` | ExpensiveOperations | `DisputesControllerTests.cs` | — | covered |
| POST `{disputeId}:resolve` (L342) | `ResolveDisputeCommand` | `ResolveDisputeCommandValidator` | `ResolveDisputeCommandHandler` | `[Authorize]` | ExpensiveOperations | `DisputeNotificationAndFeeTests.cs` | — | covered |
| GET `statistics` (L368) | dispute statistics query | — | inline | `[Authorize]` | Api (L369) | `RefundAndDisputeStatisticsTests.cs` | — | covered |

## PaymentMethodsController — `api/v1/billing/payment-methods` (`Payments:/Controllers/PaymentMethodsController.cs`, `[Authorize]` L19 + ExpensiveOperations L20)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L32) | list query | — | inline | `[Authorize]` + tenant | Api (L33) | `PaymentMethodsControllerTests.cs` | `PaymentMethodMigrationTests.cs` | covered |
| GET `{paymentMethodId}` (L66) | by-id query | — | inline | `[Authorize]` + ownership | Api (L67) | `PaymentMethodsControllerTests.cs` | — | covered |
| POST `` (L84) | `RegisterPaymentMethodCommand` (`Commands/ManagePaymentMethods/PaymentMethodCommands.cs:47`) | inline | inline handler same file | `[Authorize]` + tenant | ExpensiveOperations | `PaymentMethodRegistryServiceTests.cs` | — | covered |
| PUT `{paymentMethodId}` (L139) | `UpdatePaymentMethodCommand` (`PaymentMethodCommands.cs:91`) | — | inline | `[Authorize]` + ownership | Api (L140) | `PaymentMethodsControllerTests.cs` | — | covered |
| POST `{paymentMethodId}:set-default` (L164) | `SetDefaultPaymentMethodCommand` (`PaymentMethodCommands.cs:124`) | — | inline | `[Authorize]` + ownership | ExpensiveOperations | `PaymentMethodSingleDefaultInvariantTests.cs` | — | covered |
| DELETE `{paymentMethodId}` (L187) | `RemovePaymentMethodCommand` (`PaymentMethodCommands.cs:151`) | — | inline | `[Authorize]` + ownership | ExpensiveOperations | `PaymentMethodsControllerTests.cs` | — | covered |
| GET `{id}/bank-verification` (L216) | `GetBankAccountVerificationQuery` (L225) | — | inline | `[Authorize]` | Api (L217) | `BankAccountVerificationTests.cs` | — | covered |
| POST `{id}/bank-verification/initiate` (L233) | `InitiateBankAccountVerificationCommand` (L249) | — | `Commands/BankAccountVerification/` | `[Authorize]` | ExpensiveOperations | `BankAccountVerificationTests.cs` | — | covered |
| POST `{id}/bank-verification/verify` (L256) | verify command | — | same file | `[Authorize]` | ExpensiveOperations | `BankAccountVerificationTests.cs` | — | covered |

## Fraud controllers (`api/v1/fraud/*`)

### FraudStatisticsController (`Controllers/FraudStatisticsController.cs`, `[Authorize]` L23 + ExpensiveOperations L24)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L37) | `GetFraudStatisticsQuery` (L57) | — | `GetFraudStatisticsQueryHandler` (`Queries/GetFraudStatistics/GetFraudStatisticsQuery.cs:221`) | `[Authorize]` + `AuthorizeRequest(payments:admin)` on query L36; tenant pinned L50-54 | Api (L38) | `GetFraudStatisticsQueryHandlerTests.cs` | — | covered |
| GET `export` (L71) | same query | — | same + `FraudStatisticsCsvComposer` (L146) | same | ExpensiveOperations (L72) | `FraudBlacklistCsvTests.cs` (composer pattern) | — | covered |
| GET `export/pdf` (L111) | same query | — | same + `FraudStatisticsPdfComposer` | same | ExpensiveOperations (L112) | `FraudStatisticsPdfComposerTests.cs` | — | covered |

### FraudReviewsController (`Controllers/FraudReviewsController.cs`, `[Authorize]` + ExpensiveOperations)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L40) | `GetFraudReviewsQuery` | — | `GetFraudReviewsQueryHandler` | `[Authorize]` + admin permission on query | Api (L41) | `FraudReviewQueueAndAuthorizationTests.cs` | — | covered |
| GET `{paymentId}/detail` (L82) | `GetFraudReviewDetailQuery` | — | inline | `[Authorize]` | Api (L83) | `GetFraudReviewDetailQueryTests.cs` | — | covered |
| POST `{paymentId}:approve` (L113) | `ResolveFraudReviewCommand` approved | `ResolveFraudReviewCommandValidator` | `ResolveFraudReviewCommandHandler` | `[Authorize]` + permission | ExpensiveOperations | `ResolveFraudReviewCommandHandlerTests.cs` | — | covered |
| POST `{paymentId}:deny` (L132) | same, declined | shared | shared | shared | ExpensiveOperations | shared + `FraudBlacklistServiceTests.cs` (auto-blacklist) | — | covered |
| POST `bulk-approve` (L150) | `ResolveFraudReviewsBulkCommand` | `ResolveFraudReviewsBulkCommandValidator` | `ResolveFraudReviewsBulkCommandHandler` | `[Authorize]` | ExpensiveOperations (L151) | `ResolveFraudReviewsBulkCommandHandlerTests.cs` | — | covered |
| POST `bulk-deny` (L169) | same | shared | shared | shared | ExpensiveOperations (L170) | shared | — | covered |

### FraudBlacklistController (`Controllers/FraudBlacklistController.cs`, `[Authorize]` + ExpensiveOperations)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L36) | `ListFraudBlacklistQuery` | — | inline | `[Authorize]` | Api (L37) | `FraudBlacklistServiceTests.cs` | — | covered |
| GET `check` (L65) | `CheckFraudBlacklistQuery` | — | inline | `[Authorize]` | Api (L66) | `FraudBlacklistServiceTests.cs` | — | covered |
| POST `` (L89) | `AddFraudBlacklistEntryCommand` | — | inline in command file | `[Authorize]` + permission | ExpensiveOperations (L90) | `FraudBlacklistTenantTests.cs` | — | covered |
| DELETE `{entryId}` (L115) | `RemoveFraudBlacklistEntryCommand` | — | inline | `[Authorize]` + permission | ExpensiveOperations (L116) | `FraudBlacklistTenantTests.cs` | — | covered |
| GET `export` (L135) | export query | — | inline CSV composer | `[Authorize]` | Api (L136) | `FraudBlacklistCsvTests.cs` | — | covered |
| POST `import` (L175) | import command | — | inline | `[Authorize]` | ExpensiveOperations (L176) | `FraudBlacklistCsvTests.cs` | — | covered |

### FraudDevicesController (`Controllers/FraudDevicesController.cs`, `[Authorize]` + ExpensiveOperations)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L36) | `GetFraudDeviceInsightsQuery` | — | inline | `[Authorize]` | Api (L37) | `GetFraudDeviceInsightsQueryTests.cs` | — | covered |

## CurrenciesController — `api/v1/currencies` (`Controllers/CurrenciesController.cs`, `[Authorize]` L17)

| Route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| GET `` (L25) | `GetTenantCurrenciesQuery` | — | inline (`Queries/Currencies/GetTenantCurrencies.cs`) | `[Authorize]` | **none** | `CurrencyConfigurationTests.cs` | — | **no rate limit** (gap N10) |
| GET `suggestion` (L42) | `GetCurrencySuggestionQuery` (L55) | — | `GetCurrencySuggestionQueryHandler` (`Queries/Currencies/GetCurrencySuggestion.cs:36`) | `[AllowAnonymous]` L43 + registry L112 + `AuthorizeRequest("")` L32 — **behavior throws for anonymous** (gap N9) | **none** | `FxSecurityTests.cs:284-313` handler-direct only | — | **HOLE — anonymous path broken; no pipeline-level test** |
| POST `{currencyCode}/enable` (L65) | `EnableCurrencyCommand` | — | inline (`Commands/Currencies/CurrencyConfigurationCommands.cs`) | Roles L66 | **none** | `CurrencyConfigurationTests.cs` | — | covered |
| POST `{currencyCode}/disable` (L84) | `DisableCurrencyCommand` | — | inline | Roles L85 | **none** | `CurrencyConfigurationTests.cs` | — | covered |
| POST `{currencyCode}/set-base` (L103) | `SetBaseCurrencyCommand` | — | inline | Roles L104 | **none** | `CurrencyConfigurationTests.cs` | — | covered |

## Remaining commerce surfaces

| Controller / route | Command / Query | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|--------------------|-----------------|-----------|---------|------|------------|-----------|------------------|-------|
| CurrencyConversionController POST `currency/convert` (L28) | `ConvertCurrency` command (`Commands/CurrencyConversion/ConvertCurrency.cs`) | inline | inline handler same file | `[Authorize]` + FX permission | **none** | `CurrencyConversionCommandTests.cs` | — | covered |
| FxRatesController GET `fx-rates` (L30) | `GetFxRatesQuery` | — | inline | `[Authorize]` | **none** | `FxRateSyncTests.cs` | — | covered |
| FxRatesController GET `fx-rates/sync-status` (L54) | `GetFxSyncStatusQuery` (L60) | — | inline | `[Authorize]` | **none** | `FxRateSyncTests.cs` | — | covered |
| FxRatesController POST `fx-rates/sync` (L69) | `RunFxRateSyncCommand` (L76) | — | inline | `[Authorize]` + operator permission | **none** | `FxRateSyncTests.cs`, `FxOperatorAlertTests.cs` | — | covered |
| FxRatesController GET `fx-rates/variance` (L86) | `GetFxRateVarianceReportQuery` (L92) | — | inline | `[Authorize]` | **none** | `FxProviderContractTests.cs` | — | covered |
| FxRatesController POST `fx-rates/overrides` (L102) | override command (`Commands/FxRates/FxRateCommands.cs`) | inline | inline | `[Authorize]` + operator | **none** | `FxOverrideLifetimeTests.cs` | — | covered |
| FxRatesController PUT/DELETE `fx-rates/provider-keys/{provider}` (L130, L150) | provider-key commands | inline | inline | `[Authorize]` + operator | **none** | `FxSecurityTests.cs` | — | covered |
| PricingController POST `pricing/:calculate` (L28) | `CalculatePriceCommand` (L39) | inline in command file | inline | `[Authorize]` | **none** | `PricingEngineTests.cs` | — | covered |
| PricingController POST `pricing/:preview` (L50) | `PreviewPriceChangeCommand` (L62) | — | inline | `[Authorize]` | **none** | `PricingEngineTests.cs` | — | covered |
| PriceLocalizationController GET `` (L27) | `GetPlanLocalizedPricesQuery` | — | inline (`Queries/PriceLocalization/GetPriceLocalization.cs`) | `[Authorize]` | **none** | `BaseCurrencyReportTests.cs` | — | covered |
| PriceLocalizationController GET `recalculate/preview` (L44) | `PreviewLocalizedPricesRecalculationQuery` | — | inline | `[Authorize]` | **none** | `BaseCurrencyReportTests.cs` | — | covered |
| PriceLocalizationController POST `recalculate` (L61) | recalculate command | inline | inline (`Commands/PriceLocalization/`) | `[Authorize]` | **none** | `BaseCurrencyReportTests.cs` | — | covered |
| PriceLocalizationController GET `recalculations` (L80) | `GetPriceRecalculationRunsQuery` | — | inline | `[Authorize]` | **none** | `BaseCurrencyReportTests.cs` | — | covered |
| PriceLocalizationController POST `recalculations/{runId}/rollback` (L98) | rollback command | inline | inline | `[Authorize]` | **none** | `BaseCurrencyReportTests.cs` | — | covered |
| PriceLocalizationController POST/DELETE `plans/{planId}/currencies/{code}` (L121, L150) | currency set/unset commands | inline | inline | `[Authorize]` | **none** | `BaseCurrencyReportTests.cs` | — | covered |
| TaxesController POST `taxes/calculate` (L24) | `CalculateTaxCommand` | `CalculateTaxCommandValidator` | `CalculateTaxCommandHandler` | `[Authorize]` | **none** | `TaxCalculationTests.cs` | `TaxCalculationIntegrationTests.cs` | covered |
| TaxesController POST `taxes/validate-vat` / `validate-exemption` (L54-55) | `ValidateTaxExemptionCommand` (L64) | — | `ValidateTaxExemptionHandler` | `[Authorize]` | **none** | `TaxCalculationTests.cs` | — | covered |
| TaxJurisdictionsController GET/POST/PATCH/DELETE (L24, L57, L83, L107) | `GetTaxJurisdictionsQuery`, `CreateTaxJurisdictionCommand`, `PatchTaxJurisdictionCommand`, `DeleteTaxJurisdictionCommand` | — | handlers in `Commands/` + `Queries/` folders | `[Authorize]` (+ admin on mutations) | **none** | `EfConfigAndServicesTests.cs`, `TaxRateAndRevenueEventTests.cs` | — | covered |
| TaxRulesController GET/POST/PATCH/DELETE (L27, L76, L103, L128) | `GetApplicableTaxRulesQuery`, `CreateTaxRuleCommand`, `PatchTaxRuleCommand`, `DeleteTaxRuleCommand` | — | handlers in folders | `[Authorize]` (+ admin) | **none** | `TaxCalculationTests.cs` | `TaxCalculationIntegrationTests.cs` | covered |
| WalletsController POST/GET `wallet*` (L28-283, 21 actions) | `CreateMyWalletCommand`, `GetMyWalletQuery`, `GetMyWalletBalanceQuery`, `LockMyWalletCommand`, `UnlockMyWalletCommand`, `ListWalletsQuery`, `GetWalletByIdQuery`, `PatchWalletCommand`, `CloseWalletCommand`, `FreezeWalletCommand`, `UnfreezeWalletCommand`, `GetWalletAuditLogQuery`, `AddFundsCommand`, `DeductFundsCommand`, `TransferFundsCommand`, `GetWalletReconciliationReportQuery`, `GetWalletMetricsQuery` | validators for Create/Lock/Unlock/AddFunds/DeductFunds/Transfer (`Commands/*/…Validator.cs`) | handlers per folder (`Commands/` + `Queries/`) | `[Authorize]` + ownership/tenant guards | ExpensiveOperations default, Api on reads | `UserWalletTests.cs`, `WalletOperationsServiceTests.cs`, `WalletAuthorizationContractTests.cs`, `WalletBalanceAlertMonitorTests.cs`, `WalletReconciliationAndMetricsTests.cs`, `WalletTransactionAndDisputeTests.cs` | `WalletEndpointsIntegrationTests.cs` | covered |
| GiftsController POST/GET `gifts*` (L22-110) | `CreateGiftCommand`, `GetGiftByCodeQuery`, `RedeemGiftCommand`, `GetMySentGiftsQuery`, `GetMyReceivedGiftsQuery` (`Commands/Gifts/GiftRequests.cs`) | inline | inline | `[Authorize]` | **none** | `GiftServiceTests.cs` | — | covered |
| ReferralsController GET/POST `referrals*` (L22-101) | `GetMyReferralCodeQuery`, `GetMyReferralStatsQuery`, `GetMyReferralsQuery`, `TrackReferralClickCommand`, `TrackReferralSignupCommand` (`Commands/Referrals/ReferralRequests.cs`) | inline | inline | `[Authorize]` | **none** | `ReferralServiceTests.cs` | — | covered |
| AccountCreditsController GET/POST `account-credits*` (L23-121) | `GetMyCreditBalancesQuery`, `GetMyCreditHistoryQuery`, `GetAccountCreditMetricsQuery`, `GrantAccountCreditCommand`, `RevokeAccountCreditCommand` (`Commands/AccountCredits/AccountCreditRequests.cs`) | inline | inline | `[Authorize]` (+ admin on grants) | **none** | `AccountCreditsControllerTests.cs`, `AccountCreditServicePersistenceTests.cs`, `AccountCreditWorkerAndMetricsTests.cs` | — | covered |
| RevenueAuditingController `billing/revenue/auditing*` (L31-359, 12 actions) | `RunRevenueReconciliationCommand`, `Queries.RevenueAuditing.*` queries, `RunRevenueAnomalyDetectionCommand`, `AcknowledgeRevenueAnomalyAlertCommand` (`Queries/RevenueAuditingQueries.cs`) | inline for run commands | `RevenueReconciliationService`, `RevenueAnomalyService`, handlers | `[Authorize]` + admin + tenant scoping | **none** | `RevenueAuditingControllerTests.cs`, `RevenueAuditingCommandHandlerTests.cs`, `RevenueAuditingPersistenceTests.cs`, `RevenueAuditingServiceTests.cs` | — | covered |

## Webhook ingress (Billing module, anonymous surface)

| Route | Command | Validator | Handler | Auth | Rate limit | Unit test | Integration test | HOLE? |
|-------|---------|-----------|---------|------|------------|-----------|------------------|-------|
| POST `billing/webhooks/stripe` (`Billing:/Controllers/BillingWebhooksController.cs:186`) | `ProcessStripeWebhookCommand` (L213) | `ProcessStripeWebhookCommandValidator` | `ProcessStripeWebhookCommandHandler` → `StripeBillingWebhookService.ProcessStripeWebhookAsync` (`Billing:/Services/StripeBillingWebhookService.cs:70`) | `[AllowAnonymous]` L187 + HMAC signature (`StripeWebhookVerifier.cs:15-136`) + `WebhookSourceSecurityFilter` L189 | Webhook (L188) | `StripePaymentLifecycleWebhookTests.cs`, `StripeDisputeWebhookTests.cs` (integration project) | `BillingWebhookEndpointsIntegrationTests.cs` | covered |
| POST `billing/webhooks/paypal`, `google-pay`, `apple-pay`, `braintree` (L60-383) | provider webhook commands | per-command validators | `BillingWebhookService` family | same pattern | Webhook | per-provider suites | `ExternalBillingProviderEndpointsIntegrationTests.cs` | covered |
| GET `billing/webhooks/webhook-events/{id}` (L462), `:retry` (L494), `security` (L520), `stats` (L546), `secrets:rotate` (L580) | admin queries/commands | — | handlers in Billing | `[Authorize(Policy = SystemAdmin)]` | Api on stats | `BillingWebhookEndpointsIntegrationTests.cs` | same | covered |

**Matrix totals**: 22 controllers, ~120 endpoints. Systematic holes: no validator class on the refund-request workflow commands, cancel-payment, and the currency/gift/referral/pricing command families (controller guards only); ten controllers run with no rate-limit policy; two analytics endpoints document `payments:admin` without enforcing it at the query layer. Integration coverage is concentrated on the payments/wallet/tax/webhook happy paths — the refund-approval workflow, retry action, subscription-checkout completion and every fraud admin surface have unit tests only.

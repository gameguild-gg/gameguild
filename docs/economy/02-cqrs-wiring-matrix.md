# Economy CQRS Wiring Matrix

> Economy family `GameGuild.Finance.Economy*` · Documented against `develop` commit `ac959fd8f`.
> Columns: route → command/query → validator → handler → auth → rate limit → unit test → HOLE?
> Citation shorthand: see [`README.md`](README.md). Controllers live in `API:/Core/Controllers/`; endpoint command/queries in `API:/Core/Commands/Economy*EndpointCommands.cs` (handlers in the same files) unless the request type lives in a module.

## Auth model (applies to the whole matrix)

Three authorization layers, in execution order:

1. **Controller surface**: `[Authorize]` on every controller except the two provider webhooks — Stripe Connect (`EconomyPayoutExecutionController.cs:331-338`, `[ApiController]` without `[Authorize]`, `[AllowAnonymous]` on the action) and SumSub (`EconomyKycController.cs:114-125`). No role-based overrides anywhere in the family.
2. **Actor-context guards**: every action resolves `IActorContextAccessor` and requires an authenticated user **and** tenant id (self-service, e.g. `EconomyWalletController.cs:370-386`); admin surfaces additionally require a typed `economy:*` permission via `actor.HasPermission(...)` (`Identity.Authorization/Models/TypedPermissions.cs:931-944`): `ReviewPayouts` (payout review), `OperatePayouts` (payout execution), `OperateTreasury`, `OperateMarketplace`, `OperateBounties`, `OperateAdRewards`, `OperateCompliance` (compliance cases/holds + risk reviews), `ManagePolicies`, `ManageReserves`, `OperateLedger`, `ManageKillSwitches`, `ManageLegacyMigration`, `ReadOperations`.
3. **Step-up reauthentication**: every money-moving admin execution (payout reserve/dispatch, treasury propose/approve/dispatch, control-plane dual-control actions, legacy cutover) runs inside `IEconomyStepUpExecutor` (`API:/Core/Authorization/EconomyStepUpExecutor.cs:49-81`) — Serializable transaction consuming a fresh step-up receipt bound to (operation type, target reference, SHA-256 payload hash) before the protected action (`API:/Core/Commands/EconomyFundsEndpointCommands.cs:101-211`, `EconomyControlPlaneEndpointCommands.cs`, `EconomyComplianceEndpointCommands.cs`).

Rate limiting: **no Economy controller carries `EnableRateLimiting`** — all 19 controllers run unthrottled (verified by search; contrast with `docs/commerce/payments/02` where every controller is throttled). See [`07-gaps.md`](07-gaps.md) N9.

Validation: validators registered through the shared `ValidationBehavior` run automatically when a validator class exists; "—" means no validator class exists and guards live in the handler/service (HOLE only when neither exists).

## EconomyWalletController — `api/v1/economy` (`API:/Core/Controllers/EconomyWalletController.cs`, `[Authorize]` L37)

| Route | Command / Query | Validator | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|-----------|---------|------|-----------|-------|
| GET `wallet` (L43) | `GetMyEconomyWalletQuery` (L52) | — | `GetMyEconomyWalletQueryHandler` (`Core:/Queries/EconomyWalletQueries.cs:14`) | self-service guard L49 | `GameGuild.Finance.Economy.UnitTests/Queries/` | covered |
| GET `wallet/transactions` (L56) | `ListMyEconomyWalletTransactionsQuery` (L64) | — | same file `:72` | self-service L61 | same | covered |
| POST `conversions/hard-to-soft` (L67) | `ConvertMyHardToSoftCommand` (L80) | `ConvertMyHardToSoftCommandValidator` | `ConvertMyHardToSoftCommandHandler` (`Core:/Commands/HardToSoftConversionCommands.cs:13`) → `HardToSoftConversionWorkflow` | self-service L76 | `Finance.Economy.UnitTests/Commands/` + `Funding/` | covered |
| POST `transfers` (L84) | `CreateMyEconomyTransferCommand` (L101) | `SelfServiceEconomyTransferCommandValidator` | `CreateMyEconomyTransferCommandHandler` (`Core:/Commands/SelfServiceEconomyTransferCommands.cs:9`) → `SelfServiceEconomyTransferService` | self-service L95 | `Finance.Economy.UnitTests/Transfers/` | covered |
| POST `top-ups` (L131) | `CreateMyHardCoinTopUpCommand` (L147) | `HardCoinTopUpCommandValidator` | `CreateMyHardCoinTopUpCommandHandler` (`Core:/Commands/HardCoinTopUpCommands.cs:20`) → `SelfServiceHardCoinTopUpService` | self-service L141 | `Finance.Economy.UnitTests/Funding/`, `Integrations/` | covered |
| GET `top-ups` (L178) | `ListMyHardCoinTopUpsQuery` (L190) | — controller clamps take L187 | in top-up service/query path | self-service L185 | `Funding/` tests | covered |
| GET `top-ups/{topUpId}` (L195) | `GetMyHardCoinTopUpQuery` (L208) | — | same | self-service L203 | same | covered |
| GET `capabilities` (L213) | — (no CQRS; direct `IEconomyProviderCapabilityReadiness.Assess`, L238) | — | `Core:/Risk/EconomyCapabilityEvaluator.cs` | self-service L219 | capability tests in `Risk/` | **bypasses CQRS pipeline — no validator/auth behavior chain** |
| GET `payouts` (L248) | `ListMyPayoutOperationsQuery` (L262) | — | `Payouts:/Queries/SelfServicePayoutQueries.cs` | self-service actor L257 | `Finance.Economy.Payouts.UnitTests/Queries/` | covered |
| POST `payout-requests` (L267) | `CreateMyPayoutRequestCommand` (L284) | validator in `SelfServicePayoutRequestCommands.cs` | `CreateMyPayoutRequestCommandHandler` (`Payouts:/Commands/SelfServicePayoutRequestCommands.cs:33`) | self-service L278 | `Finance.Economy.Payouts.UnitTests/` | covered |
| GET `payout-requests` (L302) | `ListMyPayoutRequestsQuery` (L316) | — | `SelfServicePayoutQueries.cs` | self-service L311 | same | covered |
| POST `payout-requests/{id}/cancel` (L321) | `CancelMyPayoutRequestCommand` (L335) | validator in same command file | `CancelMyPayoutRequestCommandHandler` (`:100`) | self-service L328 | same | covered |
| GET `payouts/{operationId}` (L353) | `GetMyPayoutOperationQuery` (L365) | — | `SelfServicePayoutQueries.cs` | self-service L362 | same | covered |

## EconomyPayoutAccountController — `api/v1/economy/payouts` (`EconomyPayoutExecutionController.cs:63-137`, `[Authorize]` L66)

| Route | Command / Query | Validator | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|-----------|---------|------|-----------|-------|
| POST `onboarding` (L72) | `CreateOrRefreshPayoutOnboardingEndpointCommand` (L87) | — | `EconomyFundsEndpointCommandHandler` (`API:/Core/Commands/EconomyFundsEndpointCommands.cs:177`) → `DurablePayoutApplicationService.CreateOrRefreshAccountAsync` (`Payouts:/DurablePayoutApplicationService.cs:77`) | self-service L79 | `Finance.Economy.Payouts.UnitTests/` | covered |
| GET `account` (L96) | — direct service `payouts.GetAccountAsync` (L111) | — | `DurablePayoutApplicationService.GetAccountAsync :89` | self-service L104 | same | **bypasses CQRS** |

## EconomyPayoutExecutionAdministrationController — `api/v1/admin/economy/payout-requests` (`EconomyPayoutExecutionController.cs:139-326`, `[Authorize]` L142, permission `OperatePayouts` L316)

| Route | Command / Query | Validator | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|-----------|---------|------|-----------|-------|
| POST `{requestId}/reserve` (L149) | `ReservePayoutExecutionEndpointCommand` (L172) | — (guards in service) | step-up + `DurablePayoutApplicationService.ReserveApprovedAsync` (`EconomyFundsEndpointCommands.cs:180-191` → `Payouts:/DurablePayoutApplicationService.cs:101` → `DurablePayoutReservationWorkflow.cs:36`) | `OperatePayouts` + step-up `economy.payout.reserve` (L168) | `Finance.Economy.Payouts.UnitTests/` reservation suites | covered |
| POST `operations/{operationId}/dispatch` (L182) | `DispatchPayoutExecutionEndpointCommand` (L205) | — | step-up + `DispatchAsync` (`EconomyFundsEndpointCommands.cs:193-205` → `DurablePayoutApplicationService.cs:203` → `DurablePayoutSettlementWorkflow.cs:53`) | `OperatePayouts` + step-up `economy.payout.dispatch` binding version (L199) | dispatch suites | covered |
| POST `operations/{operationId}/reconcile` (L216) | `ReconcilePayoutExecutionEndpointCommand` (L229) | — | `ReconcileAsync` (`EconomyFundsEndpointCommands.cs:207` → `DurablePayoutApplicationService.cs:279`) | `OperatePayouts` (no step-up — read-only provider call + terminal apply) | reconcile suites | covered |
| GET `operations` (L234) | — direct `payouts.List` (L251) | — | store read | `OperatePayouts` | — | **bypasses CQRS** |
| GET `operations/{operationId}` (L254) | — direct `payouts.Get` (L268) | — | store read | `OperatePayouts` | — | **bypasses CQRS** |

## EconomyStripeConnectWebhookController — `api/v1/integrations/economy/stripe-connect` (`EconomyPayoutExecutionController.cs:328-359`)

| Route | Command / Query | Validator | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|-----------|---------|------|-----------|-------|
| POST `webhook` (L337) | `ApplyStripePayoutProviderEventEndpointCommand` (L354) | payload normalized + HMAC verified first (`StripeConnectPayoutProvider.cs:222-232`) | `DurablePayoutApplicationService.ApplyProviderEventAsync` (`EconomyFundsEndpointCommands.cs:210`) | `[AllowAnonymous]`; signature = the auth (L343-346 requires header) | `Finance.Economy.Payouts.UnitTests/` evidence suites | covered — provider-facing ingress |

## EconomyPayoutAdministrationController — `api/v1/admin/economy/payout-requests` review surface (`EconomyPayoutAdministrationController.cs:16-129`)

| Route | Command / Query | Validator | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|-----------|---------|------|-----------|-------|
| GET `` (L23) | list-for-review (L28) | — | store `ListForReview` | `ReviewPayouts` | `Finance.Economy.Payouts.UnitTests/Commands/` | covered |
| GET `{requestId}/audit` (L46) | review-audit read (L50) | — | store `ListReviewAudit` | `ReviewPayouts` | review audit tests | covered |

| POST `{requestId}/approve` (L62) | `ReviewPayoutRequestCommand` (approve) | validator in `ReviewPayoutRequestCommands.cs` | `ReviewPayoutRequestCommandHandler` (`Payouts:/Commands/ReviewPayoutRequestCommands.cs:34-67`) | `ReviewPayouts` (`:60-66`) | dual-control tests | covered |
| POST `{requestId}/reject` (L76) | `ReviewPayoutRequestCommand` (reject) | same | same | `ReviewPayouts` | same | covered |

## EconomyTreasuryAdministrationController — `api/v1/admin/economy/treasury/withdrawals` (`EconomyTreasuryAdministrationController.cs:34-287`, permission `OperateTreasury` L263-270)

| Route | Command / Query | Validator | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|-----------|---------|------|-----------|-------|
| GET `` (L44) | — direct `withdrawals.List` (L60) | — | store read | `OperateTreasury` | `Finance.Economy.Treasury.UnitTests/` | **bypasses CQRS** |
| GET `{runId}` (L63) | — direct `Get` (L74) | — | store read | `OperateTreasury` | same | **bypasses CQRS** |
| GET `{runId}/audit` (L78) | — direct `Audit` (L89) | — | audit chain verify | `OperateTreasury` | audit tests | **bypasses CQRS** |
| POST `` (L93) | `ProposeTreasuryWithdrawalEndpointCommand` (L115) | — (guards in `ValidateProposal`, `Treasury:/DurableAdminWithdrawalApplicationService.cs:454`) | step-up `economy.treasury.propose` → `ProposeAsync :82` → reserve workflow | `OperateTreasury` + step-up | `Finance.Economy.Treasury.UnitTests/` | covered |
| POST `{runId}/approve` (L127) | `ApproveTreasuryWithdrawalEndpointCommand` (L149) | — | step-up `economy.treasury.approve` → `ApproveAsync :170` → workflow approve | `OperateTreasury` + step-up | approve tests | covered |
| POST `{runId}/dispatch` (L156) | `DispatchTreasuryWithdrawalEndpointCommand` (L180) | — | step-up `economy.treasury.dispatch` → `DispatchAsync :195` → `BeginDispatchAsync` | `OperateTreasury` + step-up | dispatch tests | covered |
| POST `{runId}/reconcile` (L190) | `ReconcileTreasuryWithdrawalEndpointCommand` (L203) | — | `ReconcileAsync :273` | `OperateTreasury` | reconcile tests | covered |

## EconomyMarketplaceController — `api/v1/economy/marketplace` (`EconomyMarketplaceController.cs:28-90`)

| Route | Command / Query | Validator | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|-----------|---------|------|-----------|-------|
| POST `orders/{orderId}:settle` (L37) | `SettleMarketplaceOrderEndpointCommand` (L50) | — (guards in service) | `EconomyPublicEndpointCommandHandler` (`API:/Core/Commands/EconomyPublicEndpointCommands.cs`) → `Marketplace:/DurableMarketplaceSettlementService.cs:92` | self-service L47 | `Finance.Economy.Marketplace.UnitTests/` | covered |
| POST `settlements/{settlementId}:refund` (L57) | `RefundMarketplaceOrderEndpointCommand` (L70, `SelfService` authority) | — | → `Marketplace:/DurableMarketplaceRefundService.cs:95` | self-service L67 (buyer enforced in service `:134-139`) | refund suites | covered |

## EconomyMarketplaceAdministrationController — `api/v1/admin/economy/marketplace` (`EconomyMarketplaceController.cs:92-125`, permission `OperateMarketplace` L113)

| Route | Command / Query | Validator | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|-----------|---------|------|-----------|-------|
| POST `settlements/{settlementId}:refund` (L101) | `RefundMarketplaceOrderAdministrationEndpointCommand` (L117, `Operations` authority) | — | same refund service | `OperateMarketplace` | refund authority tests | covered |

## EconomyMarketplaceQueryAdministrationController — `api/v1/admin/economy/marketplace` reads (`EconomyMarketplaceQueryAdministrationController.cs`, permission `OperateMarketplace` L111)

| Route | Handler | HOLE? |
|-------|---------|-------|
| GET `settlements` (L20), `settlements/{id}` (L39), `refunds` (L54), `refunds/{id}` (L72) | `Marketplace:/MarketplaceOperationalQueries.cs` / `PostgreSqlMarketplaceOperationalQueryReader.cs` | **bypasses CQRS** (direct reader calls) |
| GET `outbox` (L87) | outbox reader (dead-letter visibility for the marketplace queue) | **bypasses CQRS** |

## EconomyBountiesController — `api/v1/economy/bounties` (`EconomyBountiesController.cs:32-198`)

| Route | Command / Query | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|---------|------|-----------|-------|
| POST `` (L41) | `CreateBountyEndpointCommand` (L54) | `Bounties:/DurableBountyApplicationService.cs` → escrow posting | self-service | `Finance.Economy.Bounties.UnitTests/` | covered |
| GET `` (L67) / GET `{bountyId}` (L81) | — reader | `DurableBountyApplicationService` views | self-service (owner-scoped) | same | **bypasses CQRS** |
| POST `{bountyId}:claim` (L95) | `ClaimBountyEndpointCommand` (L109) | `DurableBountyClaimWorkflow.ClaimAsync :352` | self-service claimant | claim suites | covered |
| POST `{bountyId}:reclaim` (L116) | `ReclaimBountyEndpointCommand` (L130) | `DurableBountyClaimWorkflow.ReclaimAsync :229` (+ `PrepareForReclaimAsync` flip) | self-service poster | reclaim suites | covered |
| GET `admin/economy/bounties/expired` (L183, admin controller in same file) | — reader | expiration views | `OperateBounties` L189 | expiration tests | **bypasses CQRS** |

## EconomyAdRewardsController — `api/v1/economy/ad-rewards` (`EconomyAdRewardsController.cs:41-154`)

| Route | Command / Query | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|---------|------|-----------|-------|
| POST `sessions` (L53) | `StartAdRewardSessionEndpointCommand` (L77) | `AdRewards:/DurableAdRewardSessionService.cs:49` | self-service + server-side risk context resolver (L63-76) | `Finance.Economy.AdRewards.UnitTests/` | covered |
| POST `sessions/{id}/complete` (L92) | `CompleteAdRewardSessionEndpointCommand` (L108) | `DurableAdRewardCompletionService.cs` | self-service (token bound, L104-116) | completion + milestone suites | covered |
| GET `sessions/{id}` (L132) | — `IDurableAdRewardSessionReader` (L138) | `DurableAdRewardSessionReader.cs` | self-service owner | reader tests | **bypasses CQRS** |

## EconomyAdRewardsAdministrationController — `api/v1/admin/economy/ad-rewards` (`EconomyAdRewardsController.cs:156-198`, permission `OperateAdRewards`)

| Route | Command | Handler | Unit test | HOLE? |
|-------|---------|---------|-----------|-------|
| POST `reports` (L166) | `ImportAdRewardReportEndpointCommand` (L176) | `DurableAdRewardReportService.cs` | `Finance.Economy.AdRewards.UnitTests/` reconciliation suites | covered |
| GET `reports` (L184) | — `IDurableAdRewardReportReader` (L195) | `DurableAdRewardReportReader.cs` | reader tests | **bypasses CQRS** |

## EconomyAdRewardQueryAdministrationController — `api/v1/admin/economy/ad-rewards` reads (`EconomyAdRewardQueryAdministrationController.cs`, permission `OperateAdRewards` L98)

| Route | Handler | HOLE? |
|-------|---------|-------|
| GET `sessions` (L20), `sessions/{id}` (L40) | `AdRewardOperationalQueries.cs` / `PostgreSqlAdRewardOperationalQueryReader.cs` | **bypasses CQRS** |
| GET `pending-claims` (L55), `reconciliations` (L74) | deferred-claim and reconciliation readers | **bypasses CQRS** |

## EconomyKycController — `api/v1/economy/kyc` + `api/v1/integrations/economy/sumsub` (`EconomyKycController.cs:24-166`)

| Route | Command | Handler | Auth | Unit test | HOLE? |
|-------|---------|---------|------|-----------|-------|
| POST `onboarding` (L33) | `StartKycOnboardingEndpointCommand` (L42) | `API:/Core/Compliance/` KYC service (SumSub) | self-service | KYC tests | covered |
| POST `access-token` (L52) | `CreateKycAccessTokenEndpointCommand` (L64) | same | self-service | same | covered |
| GET `status` (L77) | status query | applicant status read | self-service | same | covered |
| POST `integrations/.../webhook` (L121) | `IngestSumSubWebhookEndpointCommand` | `EconomyKycEvidenceStore` → `Core:/Risk/PostgreSqlComplianceEvidenceStore.cs` (inbox dedupe + outbox) | provider ingress (HMAC in provider adapter) | evidence store tests | covered |

## EconomyControlPlaneAdministrationController — `api/v1/admin/economy` (`EconomyControlPlaneAdministrationController.cs:58-454`)

18 endpoints, all through `EconomyControlPlaneEndpointCommandHandler` (`API:/Core/Commands/EconomyControlPlaneEndpointCommands.cs:58-69+`) with per-route permission checks at the controller (`:445` generic guard):

| Route (L) | Command family | Permission | Step-up |
|-----------|----------------|------------|---------|
| POST `policies` (69) / `policies/{id}/approve` (97) | `ProposeEconomyPolicyEndpointCommand` / `Approve...` | `ManagePolicies` | approve: yes |
| POST `capabilities/readiness` (122), GET `capabilities/configuration` (150) | readiness inspection | `ManagePolicies` | no |
| POST `kill-switches` (170), `.../release-proposals` (195), `.../release-approvals` (219), `.../release` (243) | kill-switch lifecycle (dual-control release) | `ManageKillSwitches` | proposals/approvals/release: yes |
| POST `ledger/verification-runs` (258), GET `ledger/health` (275), POST `ledger/anchors` (288), `ledger/anchors/verification-runs` (308) | journal integrity + WORM anchors | `OperateLedger` | no |
| POST `ledger/projection-generations` (321) / `{generation}/approvals` (335) | projection rebuild (dual-control) | `OperateLedger` | approvals: yes |
| POST `custody/observations` (359) | custody evidence ingest | `ManageReserves` | no |
| GET `reserves/liabilities` (375), POST `reserves/proposals` (387) / `{id}/approve` (415) | reserve head proposals (dual-control) | `ManageReserves` | approve: yes |

## EconomyComplianceAdministrationController — `api/v1/admin/economy/compliance` (`EconomyComplianceAdministrationController.cs:36-244`, permission `OperateCompliance` L238)

| Route (L) | Command | Step-up |
|-----------|---------|---------|
| GET `financial-crime/cases` (46), `cases/{id}` (58) | reads via `Core:/Risk/` compliance stores | no |
| POST `cases/{id}/assignment` (77), `cases/{id}/decisions` (103), `cases/{id}/regulatory-references` (146) | `AssignFinancialCrimeCaseEndpointCommand`, `DecideFinancialCrimeCaseEndpointCommand`, `RecordRegulatoryReferenceEndpointCommand` (`API:/Core/Commands/EconomyComplianceEndpointCommands.cs:10-22`) | decisions: yes |
| GET `trust-safety/appeals` (168) | read | no |
| POST `appeals/{id}/assignment` (180), `appeals/{id}/decisions` (206) | `AssignTrustSafetyAppealEndpointCommand`, `DecideTrustSafetyAppealEndpointCommand` | decisions: yes |

## EconomyComplianceHoldAdministrationController — `api/v1/admin/economy/compliance/holds` (`EconomyComplianceHoldAdministrationController.cs:14-170`, permission `OperateCompliance` L161)

| Route (L) | Command | Notes |
|-----------|---------|-------|
| GET `` (23), GET `{id}` (47), GET `{id}/audit` (68) | hold reads via `Core:/Risk/PostgreSqlComplianceHoldAdministrationStore.cs` | bypasses CQRS |
| POST `{id}/release-proposals` (89) / `{id}/release-approvals` (113) | `ProposeComplianceHoldReleaseEndpointCommand` / `Approve...` (`EconomyComplianceEndpointCommands.cs:26-31`) | dual-control release, step-up on approvals |

## EconomyRiskReviewAdministrationController — `api/v1/admin/economy/risk-reviews` (`EconomyRiskReviewAdministrationController.cs:17-157`, permission `OperateCompliance` L148)

| Route (L) | Command | Notes |
|-----------|---------|-------|
| GET `` (26), GET `{id}` (43), GET `{id}/audit` (64) | `Core:/Risk/RiskReviewLedger.cs` reads | bypasses CQRS |
| POST `{id}:approve` (85) / `{id}:reject` (105) | `ApproveEconomyRiskReviewEndpointCommand` / `Reject...` (`EconomyComplianceEndpointCommands.cs:33-38`) | releases held protected operations |

## EconomyLegacyMigrationAdministrationController — `api/v1/admin/economy/legacy-migration/batches` (`EconomyLegacyMigrationAdministrationController.cs:29-203`, permission `ManageLegacyMigration` L201)

| Route (L) | Command | Step-up |
|-----------|---------|---------|
| GET `` (39), GET `{batchId}` (55) | `Core:/Operations/LegacyEconomyShadowMigration.cs` reads | no |
| POST `` (66) `Capture`, `{id}/wallets:backfill` (84), `{id}:reconcile` (105) | `CaptureLegacyEconomyEndpointCommand` etc. (`EconomyFundsEndpointCommands.cs:37-42`) | no |
| POST `{id}/cutover:propose` (116) / `cutover:approve` (135) / `cutover:rollback` (153) | cutover commands (`EconomyFundsEndpointCommands.cs:43-51`) | all three: yes |

## Query-administration controllers (read-only, direct readers — all bypass CQRS)

| Controller | Route base | Endpoints (L) | Permission |
|------------|-----------|---------------|------------|
| `EconomyLedgerQueryAdministrationController.cs` | `admin/economy/ledger` | verification-runs (19), `{id}` (33), anchors (46), `{id}` (60), anchor verifications (71), projection-generations (84), `{gen}` (98), `{gen}/audit` (109) | `OperateLedger` (L127) |
| `EconomyPolicyQueryAdministrationController.cs` | `admin/economy/policies` | list (21), `{id}` (41), `{id}/audit` (52) | `ManagePolicies` (L66) |
| `EconomyReserveQueryAdministrationController.cs` | `admin/economy` | custody/observations (19), `{id}` (37), reserves/proposals (52), `{id}` (70), reserves/active (85) | `ManageReserves` (L104) |

## Integration-test posture

There is **no** `GameGuild.Finance.Economy.*.IntegrationTests` project. The seven Economy unit-test projects (`apps/api/tests/GameGuild.Finance.Economy{,AdRewards,AiCredits,Bounties,Marketplace,Payouts,Treasury}.UnitTests/` + `GameGuild.TestSupport.Finance.Economy/`) run against real PostgreSQL fixtures through the durable stores; HTTP-surface integration coverage exists only insofar as `GameGuild.API.IntegrationTests/` exercises economy routes — none of the Economy endpoints appear in that project's economy-named suites (verified by search), so the wiring above is validated at the command/service level, not through the controllers.

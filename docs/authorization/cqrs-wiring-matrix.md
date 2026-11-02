# Authorization CQRS Wiring Matrix — every controller, every endpoint

> Module `GameGuild.Identity.Authorization` + its authorization-surface controllers in `GameGuild.Identity.Authentication` · Documented against `develop` commit `74e9bba4c`; the 15 handler-less 500 holes were fixed by issue **#1203** (rows marked *fixed in #1203*; handlers in `GameGuild.Identity.Authentication/Handlers/AbacPolicyQueryHandlers.cs` and `.../ConditionalPolicyQueryHandlers.cs`, pipeline coverage in `GameGuild.API.IntegrationTests/PolicyManagementQueriesPostgreSqlHttpTests`).
> Scope: **24 controllers, 177 endpoints**. Columns follow the auth-package convention: route · command/query · validator · handler · auth attribute · rate limit · unit test · integration test · HOLE.
> The task framing said "11 controllers, ~110 endpoints" (echoing #1180's count of the hidden policy surface); the real authorization surface at this commit is larger — 19 controller classes live in the Authorization module and 5 authorization-surface controllers live in the Authentication module. All are listed.

Legend:
- **Validator**: `Y` = `AbstractValidator<T>` exists (FluentValidation, runs via the CQRS `ValidationBehavior`); `—` = none.
- **Handler**: `file:line` of the `ICommandHandler`/`IQueryHandler`; `NONE` = no handler registered anywhere in `apps/api/Source` (see HOLE).
- **UNAV** = no real handler, but an `UnavailableReason` contract exists so the throwing `UnavailableCommandHandler<T>` answers (`GameGuild.SharedKernel/CQRS/Extensions/ServiceCollectionExtensions.cs:93-109`) → HTTP 422. `NONE` without a contract → `MediatorSender` throws handler-not-found (`CQRS/Implementation/MediatorSender.cs:271-275`) → HTTP 500.
- **Auth**: class-level `[Authorize]` unless noted. **Rate limit**: `—` unless the controller carries `[EnableRateLimiting(RateLimitPolicies.Authorization)]` (`Modules/GameGuild.SharedKernel/Configuration/PresentationLayer/RateLimiting/RateLimitPolicies.cs:25`).
- **Unit test**: mapping/controller or handler test file naming this request type. Controller tests mock `IMediator` — a green controller test does NOT prove the handler exists. **Integration**: file exercising the real HTTP route.

## 1. Authorization module controllers

### 1.1 AccessReviewsController — `v{v}/access-reviews` (`Controllers/AccessReviewsController.cs:19`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `campaigns` :28 | `CreateAccessReviewCampaignCommand` | Y | `Commands/AccessReviewCommands.cs:38` | `[Authorize]` | — | AccessReviewsControllerStatusTranslationTests | AccessReviewWorkflowPostgreSqlHttpTests | revoke decision performs no remediation (flow doc §8) |
| GET `campaigns/{id}` :40 | `GetAccessReviewCampaignByIdQuery` | — | `Queries/AccessReviewQueries.cs:15` | `[Authorize]` | — | CoverageBoostTests5 | AccessReviewWorkflowPostgreSqlHttpTests | — |
| GET `campaigns/active` :59 | `GetActiveAccessReviewCampaignsQuery` | — | `:32` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| POST `campaigns/{id}:start` :75 | `StartAccessReviewCampaignCommand` | Y | `Commands/AccessReviewCommands.cs:74` | `[Authorize]` | — | AccessReviewControllerTests | AccessReviewWorkflowPostgreSqlHttpTests | — |
| POST `campaigns/{id}:complete` :95 | `CompleteAccessReviewCampaignCommand` | Y | `:103` | `[Authorize]` | — | CoverageBoostTests5 | AccessReviewWorkflowPostgreSqlHttpTests | completion allowed after EndDate (derived expiry not checked) |
| POST `campaigns/{id}:cancel` :120 | `CancelAccessReviewCampaignCommand` | Y | `:132` | `[Authorize]` | — | CoverageBoostTests5 | — | cancel maps to Expired status |
| POST `campaigns/{id}:send-reminders` :139 | `SendAccessReviewRemindersCommand` | — | `:221` | `[Authorize]` | — | CoverageBoostTests5 | — | reminder notification has no consumer |
| GET `items/pending` :158 | `GetPendingReviewItemsQuery` | — | `Queries/AccessReviewQueries.cs:49` | `[Authorize]` | — | AccessReviewsControllerStatusTranslationTests | AccessReviewWorkflowPostgreSqlHttpTests | — |
| POST `items/{id}:approve` :175 | `ApproveAccessReviewItemCommand` | Y | `Commands/AccessReviewCommands.cs:163` | `[Authorize]` | — | AccessReviewsControllerStatusTranslationTests | AccessReviewWorkflowPostgreSqlHttpTests | no Pending-state guard |
| POST `items/{id}:revoke` :193 | `RevokeAccessReviewItemCommand` | Y | `:199` | `[Authorize]` | — | AccessReviewsControllerStatusTranslationTests | — | no Pending guard; no permission revocation |
| POST `campaigns:process-expired` :212 | `ProcessExpiredCampaignsCommand` | — | `:238` | `[Authorize(Roles="SystemAdmin")]` :211 | — | CoverageBoostTests5 | — | AutoRevokeOnNoResponse dead |

### 1.2 ContentTypePermissionsController — `api/v{v}/authorization/content-types` (`Controllers/ContentTypePermissionsController.cs:26`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `grants` :41 | `GrantContentTypePermissionGrantCommand` | — | `Commands/ContentTypePermissionCommands.cs:47` | `[Authorize]` | — | ContentTypePermissionGrantServiceTests | — | — |
| POST `denies` :71 | `DenyContentTypePermissionGrantCommand` | — | `:193` | `[Authorize]` | — | — | — | no direct unit test |
| POST `grants:revoke` :102 | `RevokeContentTypePermissionGrantCommand` | — | `:101` | `[Authorize]` | — | — | — | no direct unit test |
| DELETE `{tenantId}/grants/{grantId}` :133 | `RevokeContentTypePermissionGrantByIdCommand` | — | `:137` | `[Authorize]` | — | ContentTypePermissionGrantServiceTests | — | — |
| GET `{tenantId}/grants` :164 | `GetContentTypePermissionGrantsQuery` | — | `:274` | `[Authorize]` | — | ContentTypePermissionGrantServiceTests | — | — |
| GET `{tenantId}/has-permission` :197 | `DacPermissionQuery` | — | none — direct `IDacPermissionResolver` call (:216) | `[Authorize]` | — | DacPermissionResolverTests | — | bypasses CQRS pipeline (no validation/behaviors) |

### 1.3 DelegatedAdminController — `v{v}/delegated-admin` (`Controllers/DelegatedAdminController.cs:19`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `` :24 | `GrantDelegatedAdminCommand` | Y | `Commands/DelegatedAdminCommands.cs:45` | `[Authorize]` | — | DelegatedAdminScopeGuardTests | — | — |
| DELETE `{id}` :39 | `RevokeDelegatedAdminCommand` | Y | `:133` | `[Authorize]` | — | DelegatedAdminScopeGuardTests | — | — |
| GET `{id}` :58 | `GetDelegatedAdminScopeByIdQuery` | — | `Queries/DelegatedAdminQueries.cs:15` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `user/{id}/scopes` :77 | `GetAdminScopesQuery` | — | `:32` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `user/{id}/managed-users` :93 | `GetManagedUsersQuery` | — | `:49` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `user/{id}/managed-resources` :109 | `GetManagedResourceTypesQuery` | — | `:66` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `user/{id}/can-manage-user/{target}` :125 | `CanManageUserQuery` | — | `:83` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `user/{id}/can-manage-resource` :142 | `CanManageResourceQuery` | — | `:105` | `[Authorize]` | — | CoverageBoostTests5 | — | — |

### 1.4 JitElevationsController — `v{v}/jit-elevations` (`Controllers/JitElevationsController.cs:19`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `` :24 | `RequestJitElevationCommand` | Y | `Commands/JitElevationCommands.cs:50` | `[Authorize]` | — | JitElevationActorIdentityGuardTests; JitElevationEnforcementTests | — | — |
| POST `{id}:approve` :39 | `ApproveJitElevationCommand` | Y | `:109` | `[Authorize]` | — | JitElevationActorIdentityGuardTests | — | user token version not bumped |
| POST `{id}:deny` :56 | `DenyJitElevationCommand` | Y | `:182` | `[Authorize]` | — | JitElevationActorIdentityGuardTests | — | — |
| POST `{id}:revoke` :73 | `RevokeJitElevationCommand` | Y | `:237` | `[Authorize]` | — | JitElevationActorIdentityGuardTests | — | — |
| GET `{id}` :90 | `GetJitElevationByIdQuery` | — | `Queries/JitElevationQueries.cs:15` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `pending` :109 | `GetPendingJitElevationsQuery` | — | `:32` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `user/{userId}` :124 | `GetUserJitElevationsQuery` | — | `:49` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `user/{userId}/active` :140 | `GetActiveJitElevationsQuery` | — | `:66` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `user/{userId}/check` :156 | `HasActiveJitElevationQuery` | — | `:88` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| POST `:cleanup` :174 | `CleanupExpiredElevationsCommand` | — | `Commands/JitElevationCommands.cs:275` | `[Authorize]` | — | CoverageBoostTests5 | — | — |

### 1.5 PermissionAnalyticsController — `v{v}/permission-analytics` (`Controllers/PermissionAnalyticsController.cs:18`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| GET `usage` :23 | `GetPermissionUsageQuery` | — | `Queries/PermissionAnalyticsQueries.cs:19` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `user-activity` :40 | `GetUserActivityQuery` | — | `:46` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `resource-patterns` :58 | `GetResourceAccessPatternsQuery` | — | `:74` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `trends` :76 | `GetPermissionTrendsQuery` | — | `:101` | `[Authorize]` | — | PermissionAnalyticsServiceTrendTests | — | — |
| GET `anomalies` :93 | `DetectPermissionAnomaliesQuery` | — | `:126` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `report` :109 | `GeneratePermissionReportQuery` | — | `:151` | `[Authorize]` | — | CoverageBoostTests5 | — | — |

### 1.6 PermissionApprovalsController — `api/v{v}/authorization/permission-approvals` (`Controllers/PermissionApprovalsController.cs:20`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `` :30 | `CreatePermissionApprovalCommand` | — | `Commands/PermissionApprovalCommands.cs:47` | `[Authorize]` | — | PermissionApprovalServiceTests | — | — |
| GET `{id}` :49 | `GetPermissionApprovalQuery` | — | `:129` | `[Authorize]` | — | — | — | no direct unit test |
| POST `{id}/decisions` :70 | `DecidePermissionApprovalCommand` | — | `:99` | `[Authorize]` | — | PermissionApprovalServiceTests | — | — |

### 1.7 PermissionDelegationsController — `v{v}/permission-delegations` (`Controllers/PermissionDelegationsController.cs:19`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `` :24 | `DelegatePermissionsCommand` | Y | `Commands/PermissionDelegationCommands.cs:44` | `[Authorize]` | — | StaticsAndFacadeTests | — | — |
| DELETE `{id}` :39 | `RevokeDelegationCommand` | Y | `:80` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `{id}` :58 | `GetDelegationByIdQuery` | — | `Queries/PermissionDelegationQueries.cs:15` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `delegate/{id}` :77 | `GetActiveDelegationsQuery` | — | `:32` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `delegator/{id}` :93 | `GetDelegationsByDelegatorQuery` | — | `:49` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `check` :109 | `CheckDelegatedPermissionQuery` | — | `:71` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| POST `:cleanup` :127 | `CleanupExpiredDelegationsCommand` | — | `Commands/PermissionDelegationCommands.cs:114` | `[Authorize]` | — | CoverageBoostTests5 | — | — |

### 1.8 PermissionEngineControllers.cs — 3 controllers

**PermissionSyncController** — `api/v{v}/authorization/sync` (`Controllers/PermissionEngineControllers.cs:21`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| GET `export` :33 | `ExportPermissionSyncCommand` | — | `Commands/PermissionEngineCommands.cs:144` | `[Authorize]` | — | PermissionEngineOperationsTests | — | — |
| POST `import` :54 | `ImportPermissionSyncCommand` | — | `:171` | `[Authorize]` | — | PermissionEngineOperationsTests | — | — |

**PermissionRestorationController** — `api/v{v}/authorization/restorations` (`:99`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `deleted/{permissionId}` :109 | `RestoreDeletedPermissionCommand` | — | `Commands/PermissionEngineCommands.cs:203` | `[Authorize]` | — | PermissionEngineOperationsTests | — | — |
| POST `undo/{auditLogId}` :130 | `UndoPermissionChangeCommand` | — | `:224` | `[Authorize]` | — | PermissionEngineOperationsTests | — | — |

**PermissionComplianceController** — `api/v{v}/authorization/compliance` (`:153`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| GET `report` :166 | `GetPermissionComplianceReportQuery` | — | `Commands/PermissionEngineCommands.cs:246` | `[Authorize]` | — | PermissionEngineOperationsTests | — | — |

### 1.9 PermissionGraphController — `api/v{v}/authorization/permission-graph` (`Controllers/PermissionGraphController.cs:22`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| GET `` :34 | `GetPermissionGraphQuery` | — | `Queries/PermissionGraphQueries.cs:16` | `[Authorize]` | — | PermissionGraphControllerTests; PermissionGraphQueryHandlerTests | — | — |
| GET `roles/{roleId}/deletion-impact` :59 | `AnalyzeRoleDeletionQuery` | — | `:33` | `[Authorize]` | — | PermissionGraphQueryHandlerTests | — | advisory only — delete never consults |
| GET `roles/{roleId}/permission-removal-impact/{key}` :77 | `AnalyzePermissionRemovalQuery` | — | `:50` | `[Authorize]` | — | PermissionGraphQueryHandlerTests | — | — |

### 1.10 PermissionHistoryController — `api/v{v}/authorization/permissions` + policy versions (`Controllers/PermissionHistoryController.cs:21`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| GET `history/at` :35 | `GetPermissionHistoryAtQuery` | — | `Queries/PermissionHistoryQueries.cs:36` | `[Authorize]` | — | PermissionHistoryReconstructionServiceTests (service level) | PermissionLifecycleContractPostgreSqlTests | — |
| GET `history/inheritance` :81 | `GetInheritanceHistoryQuery` | — | `:112` | `[Authorize]` | — | TenantPermissionHistoryTests | — | — |
| GET `api/v{v}/authorization/permission-policies/{policyId}/versions` :106 | `GetPolicyVersionHistoryQuery` | — | `:161` | `[Authorize]` | — | PolicyVersionSnapshotServiceTests (snapshot level) | — | reads a table whose only writers are bundle deploy + seeds |

### 1.11 PermissionSimulationController — `api/v{v}/authorization` (`Controllers/PermissionSimulationController.cs:21`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `permissions:simulate` :35 | `SimulatePermissionChangeCommand` | — | `Commands/PermissionSimulationCommands.cs:88` | `[Authorize]` | — | PermissionTemplateLifecycleTests | — | — |

### 1.12 PermissionTemplatesController — `api/v{v}/authorization/permission-templates` (`Controllers/PermissionTemplatesController.cs:20`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| GET `` :31 | `ListPermissionTemplatesQuery` | — | `Queries/PermissionTemplateQueries.cs:26` | `[Authorize]` | — | — | — | no direct unit test |
| POST `` :57 | `CreatePermissionTemplateCommand` | — | `Commands/PermissionTemplateCommands.cs:73` | `[Authorize]` | — | PermissionTemplateLifecycleTests | — | — |
| PUT `{templateId}` :83 | `UpdatePermissionTemplateCommand` | — | `:211` | `[Authorize]` | — | PermissionTemplateLifecycleTests | — | — |
| DELETE `{templateId}` :115 | `DeletePermissionTemplateCommand` | — | `:380` | `[Authorize]` | — | PermissionTemplateLifecycleTests | — | — |
| GET `{templateId}/versions` :141 | `GetPermissionTemplateVersionsQuery` | — | `Queries/PermissionTemplateQueries.cs:99` | `[Authorize]` | — | — | — | no direct unit test |
| POST `{templateId}:instantiate-preview` :168 | `PreviewPermissionTemplateInstantiationQuery` | — | `:161` | `[Authorize]` | — | — | — | no direct unit test |
| POST `{templateId}:instantiate` :203 | `InstantiatePermissionTemplateCommand` | — | `Commands/PermissionTemplateCommands.cs:501` | `[Authorize]` | — | PermissionTemplateLifecycleTests | — | — |
| POST `{templateId}/migrations` :239 | `MigratePermissionTemplateCommand` | — | `:810` | `[Authorize]` | — | PermissionTemplateLifecycleTests | — | — |
| GET `{templateId}/migrations/{migrationId}` :274 | `GetPermissionTemplateMigrationQuery` | — | `Queries/PermissionTemplateQueries.cs:194` | `[Authorize]` | — | — | — | no direct unit test |
| POST `{templateId}/migrations/{migrationId}:rollback` :303 | `RollbackPermissionTemplateMigrationCommand` | — | `Commands/PermissionTemplateCommands.cs:1219` | `[Authorize]` | — | PermissionTemplateLifecycleTests | — | — |

### 1.13 PolicyBundlesController — `api/v{v}/authorization/policy-bundles` (`Controllers/PolicyBundlesController.cs:34`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `` :51 | `CreatePolicyBundleCommand` | — | `Commands/PolicyBundleCommands.cs:54` | `[Authorize]` | **Authorization** :32 | SignPolicyBundleCommandHandlerTests | — | — |
| GET `` :73 | `ListPolicyBundlesQuery` | — | `:527` | `[Authorize]` | Authorization | SignPolicyBundleCommandHandlerTests | — | — |
| POST `{bundleId}:sign` :95 | `SignPolicyBundleCommand` | — | `:148` | `[Authorize]` (system admin in handler :158) | Authorization | SignPolicyBundleCommandHandlerTests | — | — |
| POST `{bundleId}:approve` :120 | `ApprovePolicyBundleCommand` | — | `:220` | `[Authorize]` | Authorization | SignPolicyBundleCommandHandlerTests | — | — |
| POST `{bundleId}:deploy` :146 | `DeployPolicyBundleCommand` | — | `:297` | `[Authorize]` | Authorization | PolicyBundleRepositorySignatureTests | — | — |
| POST `deployments/{deploymentId}:rollback` :181 | `RollbackPolicyBundleDeploymentCommand` | — | `:405` | `[Authorize]` | Authorization | — | — | no direct unit test |

### 1.14 ResourcePermissionsController — `api/v{v}/authorization/resources` (`Controllers/ResourcePermissionsController.cs:21`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| GET `invitations/{id}` :26 | `GetResourceInvitationQuery` | — | `Queries/ResourceInvitationQueries.cs:45` | `[Authorize]` | — | ResourcePermissionsControllerInvitationTests | — | — |
| GET `invitations/pending` :40 | `GetPendingResourceInvitationsQuery` | — | `:85` | `[Authorize]` | — | ResourcePermissionsControllerInvitationTests | — | — |
| POST `invitations/{id}/accept` :52 | `AcceptResourceInvitationCommand` | — | `Commands/ResourcePermissionCommands.cs:259` | `[Authorize]` | — | ResourcePermissionsControllerInvitationTests | — | — |
| POST `invitations/{id}/decline` :65 | `DeclineResourceInvitationCommand` | — | `:322` | `[Authorize]` | — | ResourcePermissionsControllerInvitationTests | — | — |
| DELETE `invitations/{id}` :84 | `RevokeResourceInvitationCommand` | — | `:384` | `[Authorize]` | — | ResourcePermissionsControllerInvitationTests | — | — |
| POST `share` :104 | `ShareResourceCommand` | — | `:181` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| PUT `users/permissions` :132 | `UpdateUserPermissionsCommand` | — | `:59` | `[Authorize]` | — | CoverageBoostTests5 | PermissionUpdateContractPostgreSqlTests | — |
| DELETE `users/access` :160 | `RemoveUserAccessCommand` | — | `:469` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `{type}/{id}/permissions` :191 | `GetEffectivePermissionsQuery` | — | `GameGuild.Identity.Authentication/Handlers/PermissionEvaluationQueryHandlers.cs:332` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `{type}/{id}/has-permission` :229 | `HasPermissionQuery` | — | `Queries/TenantPermissionQueries.cs:593` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `{type}/{id}/users` :269 | `GetResourceUsersQuery` | — | `:708` | `[Authorize]` | — | CoverageBoostTests5 | — | — |

### 1.15 SoDController — `v{v}/sod` (`Controllers/SoDController.cs:19`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `rules` :28 | `CreateSoDRuleCommand` | Y | `Commands/SoDCommands.cs:35` | `[Authorize]` | — | SoDServiceTests | — | — |
| PUT `rules/{id}` :43 | `UpdateSoDRuleCommand` | Y | `:82` | `[Authorize]` | — | SoDServiceTests | — | — |
| DELETE `rules/{id}` :73 | `DeleteSoDRuleCommand` | Y | `:119` | `[Authorize]` | — | SoDServiceTests | — | — |
| GET `rules/{id}` :92 | `GetSoDRuleByIdQuery` | — | `Queries/SoDQueries.cs:15` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `rules` :111 | `GetActiveSoDRulesQuery` | — | `:49` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `violations/detect/{userId}` :138 | `DetectSoDViolationsQuery` | — | `:66` | `[Authorize]` | — | CoverageBoostTests5 | — | detection is on-demand only — not enforced at grant time |
| GET `violations/user/{userId}` :154 | `GetUserSoDViolationsQuery` | — | `:83` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| GET `violations/active` :170 | `GetActiveSoDViolationsQuery` | — | `:100` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| POST `violations/{id}:resolve` :185 | `ResolveSoDViolationCommand` | Y | `Commands/SoDCommands.cs:152` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| POST `violations/{id}:exception` :202 | `GrantSoDExceptionCommand` | Y | `:189` | `[Authorize]` | — | CoverageBoostTests5 | — | — |
| POST `violations:scan` :219 | `ScanSoDViolationsCommand` | Y | `:211` | `[Authorize]` | — | CoverageBoostTests5 | — | — |

### 1.16 TenantPermissionsController — `api/v{v}/authorization/tenants` (`Controllers/TenantPermissionsController.cs:23`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `grant` :35 | `GrantTenantPermissionCommand` | — | `Commands/TenantPermissionCommands.cs:52` | `[Authorize]` | — | GrantTenantPermissionCommandTests | PermissionLifecycleContractPostgreSqlTests | — |
| POST `revoke` :63 | `RevokeTenantPermissionCommand` | — | `:150` | `[Authorize]` | — | TenantPermissionCommandSecurityTests | PermissionLifecycleContractPostgreSqlTests | — |
| GET `{tenantId}/permissions` :99 | `GetTenantPermissionsQuery` | — | `Queries/TenantPermissionQueries.cs:118` | `[Authorize]` | — | GetTenantPermissionsQueryHandlerTests | PermissionAuditTenantQueryPostgreSqlTests | — |
| GET `~/api/v{v}/authorization/permissions/global:check` :140 | `HasGlobalPermissionQuery` | — | `:295` | `[Authorize]` | — | TenantPermissionQueryPagingAndGlobalTests | — | — |
| GET `{tenantId}/has-permission` :171 | `GetTenantPermissionsQuery` + client contains | — | `:118` | `[RequireDacPermission]` :172 | — | CoverageBoostTests5 | — | fetch-then-contains idiom |
| POST `global/defaults` :210 | `SetGlobalDefaultPermissionsCommand` | — | `Commands/TenantPermissionCommands.cs:248` | `[Authorize]` | — | TenantPermissionCommandSecurityTests | — | — |
| POST `defaults` :238 | `SetTenantDefaultPermissionsCommand` | — | `:316` | `[Authorize]` | — | TenantPermissionCommandIsolationTests | — | — |
| POST `deny` :271 | `DenyTenantPermissionCommand` | — | `:398` | `[Authorize]` | — | TenantPermissionCommandSecurityTests | — | — |
| POST `deny/remove` :301 | `RemoveDenyPermissionsCommand` | — | `:495` | `[Authorize]` | — | TenantPermissionCommandIsolationTests | — | — |
| POST `permissions:set-expiration` :335 | `SetTenantPermissionExpirationCommand` | — | `:594` | `[Authorize]` | — | PermissionExpirationCommandHandlerTests | PermissionExpirationAdministrationPostgreSqlHttpTests | — |
| POST `permissions:extend-expiration` :365 | `ExtendTenantPermissionExpirationCommand` | — | `:658` | `[Authorize]` | — | PermissionExpirationCommandHandlerTests | PermissionExpirationAdministrationPostgreSqlHttpTests | — |
| GET `permissions:expiring` :396 | `GetExpiringTenantPermissionsQuery` | — | `Queries/TenantPermissionQueries.cs:849` | `[Authorize]` | — | PermissionExpirationServiceTests (service) | PermissionExpirationRecoveryPostgreSqlTests | — |
| POST `permissions:process-expired` :437 | `ProcessExpiredPermissionsCommand` | — | `Commands/TenantPermissionCommands.cs:700` | `[Authorize]` | — | PermissionExpirationCommandHandlerTests | PermissionExpirationAdministrationPostgreSqlHttpTests | — |
| POST `permissions:send-expiration-reminders` :457 | `SendExpirationRemindersCommand` | — | `:737` | `[Authorize]` | — | PermissionExpirationCommandHandlerTests | — | — |
| GET `permissions:expiration-rules` :484 | `GetEffectiveExpirationRuleQuery` | — | `Queries/TenantPermissionQueries.cs:959` | `[Authorize]` | — | PermissionExpirationAnalyticsQueryHandlerTests | — | — |
| GET `permissions:expiration-summary` :528 | `GetExpirationSummaryQuery` | — | `Queries/PermissionExpirationAnalyticsQueries.cs:33` | `[Authorize]` | — | PermissionExpirationAnalyticsQueryHandlerTests | — | — |
| GET `permissions:expiration-trends` :569 | `GetExpirationTrendsQuery` | — | `:78` | `[Authorize]` | — | PermissionExpirationAnalyticsQueryHandlerTests | — | — |
| GET `permissions:expiration-report` :618 | `GetExpirationReportQuery` | — | `:127` | `[Authorize]` | — | PermissionExpirationAnalyticsQueryHandlerTests | — | — |

## 2. Authentication module — authorization-surface controllers (hidden from OpenAPI unless noted)

### 2.1 AbacPolicyController — `v{v}/abac-policies` (`GameGuild.Identity.Authentication/Controllers/AbacPolicyController.cs:21`, `IgnoreApi` :19, `PLANNED` comment :11)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `` :32 | `CreateAbacPolicyCommand` | — | **UNAV** | `[Authorize]` | — | AbacPolicyControllerTests (mapping-only) | — | **YES** 422 unavailable |
| GET `{policyId}` :43 | `GetAbacPolicyQuery` | — | `GameGuild.Identity.Authentication/Handlers/AbacPolicyQueryHandlers.cs:61` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| PUT `{policyId}` :55 | `UpdateAbacPolicyCommand` | — | **UNAV** | `[Authorize]` | — | AbacPolicyControllerTests | — | **YES** 422 |
| DELETE `{policyId}` :67 | `DeleteAbacPolicyCommand` | — | **UNAV** | `[Authorize]` | — | AbacPolicyControllerTests | — | **YES** 422 |
| GET `` :79 | `GetAbacPoliciesQuery` | — | `Handlers/AbacPolicyQueryHandlers.cs:87` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| POST `:evaluate` :101 | `EvaluateAbacPoliciesCommand` | — | `GameGuild.Identity.Authentication/Handlers/PolicyEvaluationCommandHandlers.cs:74` | `[Authorize]` | — | PolicyEvaluationCommandHandlersTests | AccessControlIntegrationTests | — |
| POST `:evaluate-bulk` :112 | `BulkEvaluateAbacPoliciesCommand` | — | `:123` | `[Authorize]` | — | PolicyEvaluationCommandHandlersTests | AccessControlIntegrationTests | — |
| POST `:test-expression` :123 | `TestAbacExpressionCommand` | — | `:200` | `[Authorize]` | — | TestAbacExpressionCommandHandlerTests | — | — |
| POST `{policyId}:activate` :138 | `ActivateAbacPolicyCommand` | — | **UNAV** | `[Authorize]` | — | AbacPolicyControllerTests | — | **YES** 422 |
| POST `{policyId}:deactivate` :150 | `DeactivateAbacPolicyCommand` | — | **UNAV** | `[Authorize]` | — | AbacPolicyControllerTests | — | **YES** 422 |
| POST `{policyId}:clone` :162 | `CloneAbacPolicyCommand` | — | **UNAV** | `[Authorize]` | — | AbacPolicyControllerTests | — | **YES** 422 |
| GET `statistics` :178 | `GetAbacPolicyStatisticsQuery` | — | `Handlers/AbacPolicyQueryHandlers.cs:128` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| GET `{policyId}/usage` :190 | `GetAbacPolicyUsageQuery` | — | `Handlers/AbacPolicyQueryHandlers.cs:166` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| GET `{policyId}/audit-trail` :202 | `GetAbacPolicyAuditTrailQuery` | — | `Handlers/AbacPolicyQueryHandlers.cs:198` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| POST `:validate` :218 | `ValidateAbacPolicyCommand` | — | **UNAV** | `[Authorize]` | — | AbacPolicyControllerTests | — | **YES** 422 |
| GET `conflicts` :229 | `GetAbacPolicyConflictsQuery` | — | `Handlers/AbacPolicyQueryHandlers.cs:265` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| GET `templates` :245 | `GetAbacPolicyTemplatesQuery` | — | `Handlers/AbacPolicyQueryHandlers.cs:347` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| POST `templates/{templateId}:instantiate` :257 | `CreateAbacPolicyFromTemplateCommand` | — | **UNAV** | `[Authorize]` | — | AbacPolicyControllerTests | — | **YES** 422 |

### 2.2 ConditionalPolicyCrudController — `v{v}/conditional-policies` (`GameGuild.Identity.Authentication/Controllers/ConditionalPolicyCrudController.cs:20`, `IgnoreApi` :18) — **8 of 11 endpoints dead (422); the 3 reads fixed in #1203**

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `` :31 | `CreateConditionalPolicyCommand` | — | **UNAV** | `[Authorize]` | — | ConditionalPolicyCrudControllerTests | — | **YES** 422 |
| GET `{policyId}` :42 | `GetConditionalPolicyQuery` | — | `GameGuild.Identity.Authentication/Handlers/ConditionalPolicyQueryHandlers.cs:64` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| PUT `{policyId}` :54 | `UpdateConditionalPolicyCommand` | — | **UNAV** | `[Authorize]` | — | ConditionalPolicyCrudControllerTests | — | **YES** 422 |
| DELETE `{policyId}` :66 | `DeleteConditionalPolicyCommand` | — | **UNAV** | `[Authorize]` | — | ConditionalPolicyCrudControllerTests | — | **YES** 422 |
| GET `` :78 | `GetConditionalPoliciesQuery` | — | `Handlers/ConditionalPolicyQueryHandlers.cs:90` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| POST `{policyId}:activate` :100 | `ActivateConditionalPolicyCommand` | — | **UNAV** | `[Authorize]` | — | ConditionalPolicyCrudControllerTests | — | **YES** 422 |
| POST `{policyId}:deactivate` :112 | `DeactivateConditionalPolicyCommand` | — | **UNAV** | `[Authorize]` | — | ConditionalPolicyCrudControllerTests | — | **YES** 422 |
| POST `{policyId}:clone` :124 | `CloneConditionalPolicyCommand` | — | **UNAV** | `[Authorize]` | — | ConditionalPolicyCrudControllerTests | — | **YES** 422 |
| PUT `{policyId}/priority` :136 | `UpdateConditionalPolicyPriorityCommand` | — | **UNAV** | `[Authorize]` | — | ConditionalPolicyCrudControllerTests | — | **YES** 422 |
| GET `templates` :152 | `GetConditionalPolicyTemplatesQuery` | — | `Handlers/ConditionalPolicyQueryHandlers.cs:433` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| POST `templates/{templateId}:instantiate` :164 | `CreateConditionalPolicyFromTemplateCommand` | — | **UNAV** | `[Authorize]` | — | ConditionalPolicyCrudControllerTests | — | **YES** 422 |

### 2.3 ConditionalPolicyEvaluationController — `v{v}/conditional-policies` (`:20`, `IgnoreApi` :18)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `:evaluate` :31 | `EvaluateConditionalPoliciesCommand` | — | `PolicyEvaluationCommandHandlers.cs:280` | `[Authorize]` | — | PolicyEvaluationCommandHandlersTests | AccessControlIntegrationTests | — |
| POST `:evaluate-bulk` :42 | `BulkEvaluateConditionalPoliciesCommand` | — | `:323` | `[Authorize]` | — | ConditionalPolicyEvaluationControllerTests | — | — |
| POST `:test-rule` :53 | `TestConditionalPolicyRuleCommand` | — | `:403` | `[Authorize]` | — | PolicyEvaluationCommandHandlersTests | — | — |
| GET `statistics` :68 | `GetConditionalPolicyStatisticsQuery` | — | `GameGuild.Identity.Authentication/Handlers/ConditionalPolicyQueryHandlers.cs:129` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| GET `{policyId}/usage` :80 | `GetConditionalPolicyUsageQuery` | — | `Handlers/ConditionalPolicyQueryHandlers.cs:166` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| GET `{policyId}/evaluation-history` :92 | `GetConditionalPolicyEvaluationHistoryQuery` | — | `Handlers/ConditionalPolicyQueryHandlers.cs:198` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| POST `:validate` :108 | `ValidateConditionalPolicyCommand` | — | `:544` | `[Authorize]` | — | ConditionalPolicyEvaluationControllerTests | — | — |
| GET `conflicts` :119 | `GetConditionalPolicyConflictsQuery` | — | `Handlers/ConditionalPolicyQueryHandlers.cs:236` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| POST `:simulate` :131 | `SimulateConditionalPolicyCommand` | — | `:465` | `[Authorize]` | — | PolicyEvaluationCommandHandlersTests | — | — |
| GET `condition-types` :146 | `GetPolicyConditionTypesQuery` | — | `Handlers/ConditionalPolicyQueryHandlers.cs:318` | `[Authorize]` | — | PolicyManagementQueriesPostgreSqlHttpTests (pipeline) | PolicyManagementQueriesPostgreSqlHttpTests | fixed in #1203 |
| POST `:validate-condition` :158 | `ValidateConditionCommand` | — | `:590` | `[Authorize]` | — | ConditionalPolicyEvaluationControllerTests | — | — |

### 2.4 PermissionEvaluationController — `v{v}/permissions` (`:28`, `IgnoreApi` :25, rate-limited :26)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| POST `tenant:check` :39 | `HasTenantPermissionQuery` (body-bound) | — | `Handlers/PermissionEvaluationQueryHandlers.cs:32` | `[Authorize]` | Authorization | — | AccessControlIntegrationTests | — |
| GET `tenant` :53 | `GetTenantPermissionsQuery` | — | `Queries/TenantPermissionQueries.cs:118` (Authorization module) | `[Authorize]` | Authorization | GetTenantPermissionsQueryHandlerTests | — | — |
| POST `content-type:check` :85 | `HasContentTypePermissionQuery` | — | `PermissionEvaluationQueryHandlers.cs:128` | `[Authorize]` | Authorization | — | — | — |
| GET `content-type` :99 | `GetContentTypePermissionsQuery` | — | `:65` | `[Authorize]` | Authorization | — | — | — |
| POST `resource:check` :120 | `HasResourcePermissionQuery` | — | `:211` | `[Authorize]` | Authorization | — | — | — |
| GET `resource` :134 | `GetResourcePermissionsQuery` | — | `:181` | `[Authorize]` | Authorization | — | — | — |
| GET `~/v{v}/users/{userId}/permissions` :154 | `GetUserPermissionsQuery` | — | `:244` | `[Authorize]` | Authorization | — | — | — |
| GET `~/v{v}/users/{userId}/permissions/effective` :168 | `GetEffectivePermissionsQuery` | — | `:332` | `[Authorize]` | Authorization | — | BulkPermissionChecksPostgreSqlTests | — |
| POST `:resolve-hierarchy` :185 | `ResolvePermissionHierarchyQuery` | — | `:388` | `[Authorize]` | Authorization | — | — | — |

### 2.5 RolesController — `v{v}/roles` (`:29`, per-action `Roles.Manage`)

| Route | Command/Query | Val | Handler | Auth | Rate | Unit test | Integration | HOLE |
|---|---|---|---|---|---|---|---|---|
| GET `` :41 | `GetRolesQuery` | — | `Handlers/RoleQueryHandlers.cs:9` | `Roles.Manage` | — | RoleQueryHandlersTests; ApiKeyAndRolesControllerTests | — | — |
| GET `{roleId}` :64 | `GetRoleByIdQuery` | — | `:32` | `Roles.Manage` | — | RoleQueryHandlersTests | — | — |
| POST `` :89 | `CreateRoleCommand` | — | `Handlers/RoleCommandHandlers.cs:13` | `Roles.Manage` | — | RoleCommandHandlersTests | — | no durable audit |
| PUT `{roleId}` :123 | `UpdateRoleCommand` | — | `:52` | `Roles.Manage` | — | RoleCommandHandlersTests | — | no durable audit |
| DELETE `{roleId}` :156 | `DeleteRoleCommand` | — | `:126` | `Roles.Manage` | — | RoleCommandHandlersTests | — | deletes without impact consult |
| GET `user/{userId}` :182 | `GetUserRolesQuery` | — | `RoleQueryHandlers.cs:60` | `Roles.Manage` | — | RoleQueryHandlersTests | — | — |
| POST `:assign` :205 | `AssignRoleToUserCommand` | — | `RoleCommandHandlers.cs:162` | `Roles.Manage` | — | RoleCommandHandlersTests | — | no token-version bump; no IsActive check |
| POST `:bulk-assign` :235 | `BulkAssignRolesCommand` | — | `:241` | `Roles.Manage` | — | BulkAssignRolesCommandHandlerTests | — | — |
| POST `:remove` :259 | `RemoveRoleFromUserCommand` | — | `:335` | `Roles.Manage` | — | RoleCommandHandlersTests | — | no token-version bump |

## 3. Matrix summary

| Metric | Count |
|---|---|
| Controllers (Authorization module) | 19 classes in 16 files |
| Controllers (Authentication module, authorization surface) | 5 |
| Total endpoints | **177** |
| Endpoints with real handlers | 161 (146 + the 15 read queries fixed by #1203) |
| Dead endpoints — 422 unavailable-contract | 16 (ABAC 8, Conditional CRUD 8) — still planned surface, guarded hidden from OpenAPI by `PlannedAuthorizationSurfaceGuardTests` |
| Dead endpoints — 500 handler-not-found | 0 (15 fixed by #1203) |
| Endpoints with FluentValidation validators | 17 (JIT 4, DelegatedAdmin 2, AccessReview 6, SoD 4, Delegation 2 — queries unvalidated by convention) |
| Controllers with rate limiting | 2 (`PolicyBundlesController`, `PermissionEvaluationController`) |
| Controllers hidden from OpenAPI | 4 (Abac, ConditionalPolicyCrud, ConditionalPolicyEvaluation, PermissionEvaluation) |

**The 31 dead endpoints were the headline wiring hole**: every one is routed, `[Authorize]`-guarded, and was covered by mapping-only controller tests that mock `IMediator` — the suite stayed green while the endpoints could not serve traffic. Issue **#1203** closed the 15 handler-less 500s (real query handlers + pipeline-level PG tests replacing the mocked read-side tests) and pinned the remaining 16 as designed-unavailable 422s: they keep the `UnavailableReason` contract and their controllers stay hidden from the generated client (`PlannedAuthorizationSurfaceGuardTests`). Two different failure modes existed (422 designed-unavailable vs 500 forgot-to-implement); the read-side queries never got the `UnavailableReason` contract treatment the write-side got, which is why they failed with a different, uglier status — #1203 resolved that by implementing them instead.

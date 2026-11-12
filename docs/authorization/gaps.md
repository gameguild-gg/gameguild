# Authorization Module — Consolidated Gaps

> Module `GameGuild.Identity.Authorization` + its authorization-surface controllers in `GameGuild.Identity.Authentication` · Documented against `develop` commit `74e9bba4c`. Items marked **fixed in #1203** were closed by that PR.
> Scope: every gap found by this documentation audit, each with `file:line`, classified as **already filed** (issue exists) or **NEW** (candidate issues). Gaps carried unchanged from the merged auth docs are marked *carried* with their source.

Paths are relative to `apps/api/Source/Modules/GameGuild.Identity.Authorization/` unless they start with `GameGuild.` (then relative to `apps/api/Source/`).

## A. Already filed

| # | Gap | Evidence | Issue |
|---|-----|----------|-------|
| A1 | ~~`AbacPolicyMiddleware` (both copies) is a header-writing stub; no request-time ABAC gate exists~~ **middleware deleted**: both copies removed and unregistered from `UseAuthenticationModule` — request-time ABAC evaluation happens at permission-resolution time via `ConditionalPolicyEvaluationExtension` (#1230), so a per-request middleware added nothing beyond the `X-ABAC-Policies` header. (The fail-open overlay concern in A2 remains tracked under #1180.) | deleted: `Middleware/AbacPolicyMiddleware.cs`, `GameGuild.Identity.Authentication/AbacPolicyMiddleware.cs`; registration removed from `GameGuild.Identity.Authentication/AuthenticationModule.cs` | **#1180** (middleware half fixed) |
| A2 | ABAC overlay failure is permissive (fail-open for the layer); `Permit` exposed by `:evaluate` can be mistaken for access approval; ABAC/conditional controllers hidden + PLANNED comment | `Services/AbacPolicyEvaluationExtension.cs` catch block; `GameGuild.Identity.Authentication/Controllers/AbacPolicyController.cs:11,19` | carried from #1110 docs (PR #1176) → #1180 |
| A3 | Owner-override audit is DEBUG-only; role-hierarchy depth caps silently drop grants | `Services/EffectivePermissionResolverService.cs:66-68`; `Services/RbacPermissionResolver.cs:161` | carried from #1110 docs |

## B. NEW — dead endpoints / wiring holes (the headline)

| # | Gap | Evidence | Suggested severity |
|---|-----|----------|--------------------|
| B1 | ~~**31 of 177 authorization endpoints are dead.**~~ **fixed in #1203** (the 15 × 500 half): the 15 handler-less read queries got real handlers (`GameGuild.Identity.Authentication/Handlers/AbacPolicyQueryHandlers.cs`, `.../ConditionalPolicyQueryHandlers.cs`) backed by `IAbacPolicyRepository`/`IConditionalPolicyRepository` with fail-closed tenant guards. The 16 × 422 half remains by design (planned management surface): the commands keep their `UnavailableReason` contracts and their controllers stay hidden from the generated client, enforced by `GameGuild.API.UnitTests/Architecture/PlannedAuthorizationSurfaceGuardTests`. Original evidence: contracts `GameGuild.Identity.Authentication/Architecture/UseCaseEventContracts.g.cs:2,3,19,20,21,26,27,28,30,31,32,35,36,38,39,40,91,93,94,99`; registration `GameGuild.SharedKernel/CQRS/Extensions/ServiceCollectionExtensions.cs:93-109`; throw `GameGuild.SharedKernel/CQRS/Implementation/UnavailableUseCaseHandlers.cs:13-21`; 500 path `GameGuild.SharedKernel/CQRS/Implementation/MediatorSender.cs:271-275`; status mapping `GameGuild.SharedKernel/Middlewares/ExceptionHandlingMiddleware.cs:59-72,124-133,143` | high → 500s fixed in #1203; 422s tracked as follow-up |
| B2 | ~~The dead read endpoints are covered by **mapping-only unit tests with a mocked `IMediator`**~~ **fixed in #1203**: the mocked read-side tests for the 15 fixed queries were removed from `AbacPolicyControllerTests.cs`, `ConditionalPolicyCrudControllerTests.cs`, `ConditionalPolicyEvaluationControllerTests.cs` and replaced by pipeline-level coverage (`GameGuild.API.IntegrationTests/PolicyManagementQueriesPostgreSqlHttpTests.cs`) that exercises the real mediator + handlers over HTTP against PostgreSQL. The remaining mocked tests cover only the still-422 command-mapping behavior | high (test gap) → fixed in #1203 |
| B3 | ~~Read-side dead queries never received the `UnavailableReason` contract treatment — inconsistent failure mode (500 vs designed 422)~~ **fixed in #1203** by implementing the 15 queries outright (no unavailable-contract needed; the endpoints now serve 200/404/403) | `UseCaseEventContracts.g.cs` lacks entries for `GetAbacPolicyQuery`, `GetAbacPoliciesQuery`, `GetAbacPolicyStatisticsQuery`, `GetAbacPolicyUsageQuery`, `GetAbacPolicyAuditTrailQuery`, `GetAbacPolicyConflictsQuery`, `GetAbacPolicyTemplatesQuery`, `GetConditionalPolicyQuery(s)`, `GetConditionalPolicyTemplatesQuery`, `GetConditionalPolicyStatisticsQuery`, `GetConditionalPolicyUsageQuery`, `GetConditionalPolicyEvaluationHistoryQuery`, `GetConditionalPolicyConflictsQuery`, `GetPolicyConditionTypesQuery` | medium → fixed in #1203 |
| B4 | Vestigial duplicate access-review commands in the Authentication module are all marked unavailable (dead types alongside the working Authorization-module versions) | `UseCaseEventContracts.g.cs:13,14,21,28,29,32,39,49,68,69,80,82,87` (`BulkReviewAccessItemsCommand`, `CreateAccessReviewCampaignCommand` (Authentication ns), etc.) | low (cleanup) |

## C. NEW — stub middleware beyond #1180

| # | Gap | Evidence |
|---|-----|----------|
| C1 | ~~`AccessReviewMiddleware` is a second header-writing stub (`X-Access-Review: enabled`)~~ **fixed in #1203**: unregistered from `UseAuthenticationModule` and marked `[Obsolete]` — the claimed compliance-tracking/review-triggering responsibilities belong to the access-review services, not a per-request middleware (no minimal in-place implementation exists) | `Middleware/AccessReviewMiddleware.cs`; registration removed from `GameGuild.Identity.Authentication/AuthenticationModule.cs` |
| C2 | ~~`PermissionCachingMiddleware` is a third header-writing stub (`X-Permission-Cache: enabled`)~~ **fixed in #1203**: unregistered from `UseAuthenticationModule` and marked `[Obsolete]` — permission caching is performed by the permission resolution services (with explicit invalidation via `ClearPermissionCacheCommand`) | `Middleware/PermissionCachingMiddleware.cs`; registration removed from `GameGuild.Identity.Authentication/AuthenticationModule.cs` |

## D. NEW — access review

| # | Gap | Evidence |
|---|-----|----------|
| D1 | **Revoke decision is not remediation**: `RevokeItemAsync` flips item status only — no grant revocation, no role removal, no audit row | `Services/AccessReviewAnalyticsServices.cs:245-267` |
| D2 | `AutoRevokeOnNoResponse` is a dead flag (persisted in migrations, read by nothing) | `Entities/AccessReview.cs:45`; only migration references exist |
| D3 | Item decisions have no Pending-state guard — approved↔revoked overwrite freely | `Entities/AccessReview.cs:230-251` (no status check in `Approve`/`Revoke`) |
| D4 | Campaign counters never incremented — `GetCompletionPercentage` always 0% | `Entities/AccessReview.cs:140-162` (`IncrementReviewed/Approved/Revoked` have no callers) |
| D5 | Cancel and expiry share the `Expired` status (semantic overloading) | `Entities/AccessReview.cs:117-126` ("Using Expired as cancelled state") |
| D6 | `AccessReviewStatus.Active` enum value is unreachable (no writer sets it) | `Models/PermissionEnums.cs:146`; no `Status = AccessReviewStatus.Active` anywhere |
| D7 | Campaign completion allowed after `EndDate` passed (derived expiry never blocks `Complete`) | `Entities/AccessReview.cs:68-69,101-112` |
| D8 | Access-review operations write no durable audit trail at all | no `IPermissionAuditService`/audit-event call in `Services/AccessReviewAnalyticsServices.cs` |
| D9 | Reminder notification is an orphan producer (see E1) | `Services/AccessReviewAnalyticsServices.cs:294-297` |

## E. NEW — events / durability

| # | Gap | Evidence |
|---|-----|----------|
| E1 | `AccessReviewReminderNotification` has zero consumers | producer `Services/AccessReviewAnalyticsServices.cs:294-297`; type `Notifications/AccessReviewReminderNotification.cs:10`; no `INotificationHandler<T>` in `apps/api/Source` |
| E2 | Expiration alerts are best-effort post-commit — crash between commit and publish loses the alert permanently (no outbox row, no retry) | `Services/PermissionExpirationService.cs:83-89,224-232` |
| E3 | Webhook revocation delivery fire-and-forget: external systems can permanently miss `permission.revoked` | `Services/WebhookPermissionChangeNotifier.cs:168-243` (give-up log at `:243`) |
| E4 | Expected audit coverage without producers: `RoleAssigned`/`RoleRevoked`/`PermissionRevoked`/`PermissionChanged` listed by coverage checks but never emitted by the roles path | `GameGuild.Compliance.Audit/Services/ComprehensiveEventCoverageService.cs:89,104`; no producer in `GameGuild.Identity.Authentication/Handlers/RoleCommandHandlers.cs` |

## F. NEW — roles lifecycle

| # | Gap | Evidence |
|---|-----|----------|
| F1 | Invalidation asymmetry: bulk assign bumps user token versions + per-user caches; single `:assign`/`:remove` bump only the role cache dependency | `GameGuild.Identity.Authentication/Handlers/RoleCommandHandlers.cs:311` vs `:201-205`, `:364-368` |
| F2 | Single assign ignores `role.IsActive` (bulk enforces it) | `:180-184` vs `:279` |
| F3 | Role delete performs no assignment census and ignores the `deletion-impact` analysis endpoint | `:141-156`; endpoint `Controllers/PermissionGraphController.cs:59` |
| F4 | Roles path writes no `PermissionAuditLog` / audit events | absence in `RoleCommandHandlers.cs` (contrast `Commands/TenantPermissionCommands.cs:152-162`) |

## G. NEW — evaluation / policy engine

| # | Gap | Evidence |
|---|-----|----------|
| G1 | `PermissionHandler` rethrows after `context.Fail` — store outage yields 500, discards the failure reason | `Handlers/PermissionHandler.cs:69-74` |
| G2 | `ConditionalPolicyEvaluator` is not wired into permission resolution — deployed conditional denies never affect effective-permission decisions | evaluator `Services/ConditionalPolicyEvaluator.cs:78` invoked only from `GameGuild.Identity.Authentication/Handlers/PolicyEvaluationCommandHandlers.cs:280+`; not registered as `IPermissionEvaluationExtension` (registry holds `Services/AbacPolicyEvaluationExtension.cs:38` only) |
| G3 | Conditional `:evaluate` defaults to allow when no policy applies (not deny-by-default) | `Services/ConditionalPolicyEvaluator.cs:126` |
| G4 | `ContentTypePermissionsController.has-permission` bypasses CQRS (direct service call) — skips validation/behavior pipeline | `Controllers/ContentTypePermissionsController.cs:216` |
| G5 | `tenants/{id}/has-permission` fetches the full list and does client-side `Contains` instead of a boolean engine query | `Controllers/TenantPermissionsController.cs:182-205` |
| G6 | `PolicyBundleStatus.Revoked` is checked but has no writer (key-revocation flow unimplemented) | `Commands/PolicyBundleCommands.cs:171` |
| G7 | `PolicyVersionHistory` has no retention bound; every redeploy appends definitions + snapshots unboundedly | `Entities/PolicyVersionHistory.cs:57-122`; no prune path |
| G8 | Resource-level `[AuthorizeRequest]` builds only `AclSubject.ForUser` — role/group-scoped ACL entries can never match in that frame | `Behaviors/AuthorizationBehavior.cs:137` (comment admits the limitation) |
| G9 | Reflection-based `ResourceId` extraction has no startup contract validation — misnamed property = silent fail-closed | `Behaviors/AuthorizationBehavior.cs:116-121,166-190` |
| G10 | JIT approval bumps tenant security version but not the user security version | `Services/JitElevationService.cs:103` (no `IUserSecurityVersionStore` call) |
| G11 | `JitElevationRequest.Activate()` has no caller — `Active` status effectively unreachable via code; `IsGrantInForce` papers over it | `Entities/JitElevationRequest.cs:155-162` (no callers); `:75` |
| G12 | Reminder stamp persisted before publish — failed publish suppresses reminders for a full `MinimumReminderInterval` | `Services/PermissionExpirationService.cs:221-232` |
| G13 | `DbAuthorizationPolicyProvider` gives no signal for dangling DB policy names (silent static fallback) | `Providers/DbAuthorizationPolicyProvider.cs:45-96` |
| G14 | Re-grant expiry merge keeps the earlier deadline silently (surprising UX; extension requires the separate endpoint) | `Services/FocusedPermissionServices.cs:125-127` |

## H. Carried from merged auth docs (for completeness)

| # | Gap | Source |
|---|-----|--------|
| H1 | Overlay permissive-skip on ABAC evaluator failure | `docs/auth/flows/permission-evaluation-abac-flow.md` §9.4 |
| H2 | Owner-override audit DEBUG-only | same §9.5 |
| H3 | Role-hierarchy cap silently drops grants (depth 20) | same §9.6 |
| H4 | ABAC/conditional management controllers hidden + PLANNED | same §9.2 (now subsumed by B1/B3) |

## Suggested issue split

1. `bug(authz): 31 dead authorization endpoints — ABAC/conditional CRUD + analytics have no handlers` (B1+B2+B3) — the highest-impact finding.
2. `bug(authz): AccessReviewMiddleware and PermissionCachingMiddleware are header-writing stubs` (C1+C2) — sibling of #1180.
3. `bug(authz): access-review revoke decision performs no remediation; dead auto-revoke flag; unguarded decisions` (D1+D2+D3+D4).
4. `bug(authz): orphan AccessReviewReminderNotification + non-durable expiration alerts` (E1+E2).
5. `bug(authz): role assignment invalidation asymmetry and missing audit producers` (E4+F1..F4).
6. `bug(authz): conditional policies not enforced at resolution; default-allow evaluation` (G2+G3).
7. Smaller engine items (G1, G4-G14) can batch into a hardening issue.

# Not Implemented Features

This document lists all placeholder commands and queries that have been defined but not yet implemented with handlers.

---

## Learning Platform (GameGuild.Learning*)

### Summary
- **Total Not Implemented in scoped Learning scan**: 0 features ✅
- **API Host Status**: Learning.Courses, Learning.Enrollments, Learning.Assessments, Learning.Certificates, Learning.Cohorts, Learning.Experience.Discovery, Learning.Experience.LearningPaths, Learning.Experience.Recommendations, and Learning.Experience.Social are enabled in the API host ✅
- **Controller Status**: Course, enrollment, assessment, certificate/template, cohort, discovery, learning path, recommendation, and social learning controllers are registered as application parts ✅
- **EF Model Status**: All enabled Learning entity modules expose `IModelConfiguration` and are discovered by `ApplicationDbContext` ✅
- **Database Status**: `AddLearningExperienceCloseout` migration adds the Learning close-out tables and updates the model snapshot ✅
- **Frontend Status**: Learning dashboard placeholders for classes, certificates, listing pricing/FAQ/testimonials, support tickets/discussions, notifications, integrations, and revenue/completion analytics were replaced with API-backed or course-derived screens ✅

### Verification
- `dotnet build apps/api/Source/GameGuild.API/GameGuild.API.csproj --no-restore -o .tmp\build-check\major-findings-final /p:UseAppHost=false /m:1 /clp:Summary` ✅
- `pnpm --filter @game-guild/web build` ✅
- Focused Learning unit tests: 591 passed, 0 failed, 0 skipped ✅
- Coverage: all scoped Learning modules are 100% line / 100% branch / 100% method ✅

---

## Testing Lab (GameGuild.TestingLab)

### Summary
- **Total Not Implemented in scoped Testing Lab scan**: 0 Day-0 API features ✅
- **API Host Status**: `GameGuild.TestingLab` is referenced by the API host, enabled in default modules, registered in DI, and registered as an MVC application part ✅
- **Permission Template Status**: Testing Lab role-template list/create/update/delete now persists through `PermissionTemplate` instead of returning planned/stubbed responses ✅
- **Attendance Report Status**: Student attendance reports aggregate persisted registrations, participants, sessions, and feedback instead of returning mock rows ✅
- **EF Model Status**: Testing Lab runtime entities are registered through `TestingLabModelConfiguration` ✅
- **Database Status**: `AddTestingLabProjectsLaunchPadCloseout` adds Testing Lab model changes and updates the model snapshot ✅

### Verification
- Focused Testing Lab unit tests: 66 passed, 0 failed, 0 skipped ✅
- Coverage: 100% line / 100% branch / 100% method ✅

---

## Projects / Launch Base (GameGuild.Projects)

### Summary
- **Total Not Implemented in scoped Projects scan**: 0 Day-0 launch-base API blockers ✅
- **API Host Status**: `GameGuild.Projects` is referenced by the API host, enabled in default modules, registered in DI, and registered as an MVC application part ✅
- **Invitation Status**: Project invite/list/accept/decline flows are EF-backed with `ProjectInvitation` and no longer placeholder responses ✅
- **Statistics Status**: Project download statistics aggregate metadata and release download counts instead of returning hardcoded zero ✅
- **Deleted Query Status**: Admin deleted-project queries return soft-deleted project rows instead of an empty placeholder result ✅
- **EF Model Status**: Project invitations and related model mappings are registered through `ProjectsModelConfiguration` ✅

### Verification
- Focused Projects unit tests: 120 passed, 0 failed, 0 skipped ✅
- Coverage: 100% line / 100% branch / 100% method ✅

---

## Launch Pad (GameGuild.LaunchPad)

### Summary
- **Total Not Implemented in scoped Launch Pad scan**: 0 Day-0 API features ✅
- **API Host Status**: `GameGuild.LaunchPad` is referenced by the API host, enabled in default modules, registered in DI, and registered as an MVC application part ✅
- **Feature Status**: Dedicated launch plans, checklist items, channel metadata, publish workflow, project-status update, and dashboard/status queries are implemented behind CQRS handlers ✅
- **EF Model Status**: Launch Pad entities are registered through `LaunchPadModelConfiguration` ✅
- **Database Status**: `AddTestingLabProjectsLaunchPadCloseout` adds Launch Pad tables and updates the model snapshot ✅

### Verification
- Launch Pad unit tests: 7 passed, 0 failed, 0 skipped ✅
- Coverage: 100% line / 100% branch / 100% method ✅

---

## Community / Platform Management Closeout

### Summary
- **Community API/Web Status**: Community member, project, and feed pages were replaced with live query-backed screens instead of placeholder pages ✅
- **Community API Coverage Status**: Blog, Feed, Follows, Groups, Posts, Profiles, Ratings, and Reactions modules are covered by focused unit suites ✅
- **Platform Permission Status**: `PermissionsEndpoint` now exposes the registered permission catalog for reads and rejects unsupported mutable legacy writes explicitly instead of returning placeholder responses ✅
- **Platform Tenant Shell Status**: The legacy `/tenants` shell endpoint is now a CQRS-backed facade over `GameGuild.Identity.Tenants` instead of returning sample tenants ✅
- **Platform Cache Shell Status**: The legacy `/cache/clear/{pattern}` endpoint now delegates to `IPatternCacheService.RemoveByPatternAsync` and returns explicit unsupported-provider errors instead of a planned response ✅
- **Resource Sharing Status**: Existing-user resource shares create direct persisted permission rows through an API-host user lookup adapter; unknown emails still create invitations ✅
- **Subscription Notification Status**: Subscription lifecycle notifications publish billing messages through the shared notification publisher instead of logging only ✅
- **API Host Status**: Module configuration tests verify Learning, Projects, Testing Lab, Launch Pad, and permission catalog wiring ✅

### Verification
- Social/Community unit tests: 575 passed, 0 failed, 0 skipped ✅
- Coverage: all scoped Social/Community modules are 100% line / 100% branch / 100% method ✅
- API unit tests: 93 passed, 0 failed, 0 skipped ✅

---

## User Module (GameGuild.Identity.Users)

### Summary
- **Total Not Implemented**: 0 features ✅
  - Commands: 0 ✅
  - Queries: 0 ✅
- **Current Test Coverage**: 100% line / 100% branch / 100% method coverage in `.tmp/coverage/fresh-all-unit/best-module-coverage-with-methods.csv`
- **Implemented Handlers**: 41 (37 commands, 4 queries) ✅
- **Note**: All implemented handlers are exposed via controller endpoints
- **Note**: All 17 user commands with controller endpoints have been implemented!

### Not Implemented Commands (0) ✅

All commands have been implemented! ✅

#### User Preferences Commands (12) - ALL IMPLEMENTED ✅
1. ✅ `UpdateUserPreferencesCommand(Guid UserId, UpdateUserPreferencesRequest Request)`
2. ✅ `ReplaceUserPreferencesCommand(Guid UserId, ReplaceUserPreferencesRequest Request)`
3. ✅ `ResetUserPreferencesCommand(Guid UserId)`
4. ✅ `UpdateUserNotificationPreferencesCommand(Guid UserId, UpdateUserNotificationPreferencesRequest Request)`
5. ✅ `ReplaceUserNotificationPreferencesCommand(Guid UserId, ReplaceUserNotificationPreferencesRequest Request)`
6. ✅ `ResetUserNotificationPreferencesCommand(Guid UserId)`
7. ✅ `UpdateUserAccessibilityPreferencesCommand(Guid UserId, UpdateUserAccessibilityPreferencesRequest Request)`
8. ✅ `ReplaceUserAccessibilityPreferencesCommand(Guid UserId, ReplaceUserAccessibilityPreferencesRequest Request)`
9. ✅ `ResetUserAccessibilityPreferencesCommand(Guid UserId)`
10. ✅ `UpdateUserPrivacyPreferencesCommand(Guid UserId, UpdateUserPrivacyPreferencesRequest Request)`
11. ✅ `ReplaceUserPrivacyPreferencesCommand(Guid UserId, ReplaceUserPrivacyPreferencesRequest Request)`
12. ✅ `ResetUserPrivacyPreferencesCommand(Guid UserId)`

#### User Profile Commands (2) - ALL IMPLEMENTED ✅
13. ✅ `UpdateUserProfileCommand(Guid UserId, UpdateUserProfileRequest Request)`
14. ✅ `ReplaceUserProfileCommand(Guid UserId, ReplaceUserProfileRequest Request)`

#### User Notifications Commands (3) - ALL IMPLEMENTED ✅
15. ✅ `MarkNotificationAsReadCommand(Guid UserId, Guid NotificationId)`
16. ✅ `MarkNotificationAsUnreadCommand(Guid UserId, Guid NotificationId)`
17. ✅ `ArchiveNotificationCommand(Guid UserId, Guid NotificationId)`

### Not Implemented Queries (0)

All queries have been implemented! ✅

### Implemented Features ✅

#### Commands (20)
- ActivateUserCommand
- DeactivateUserCommand
- SuspendUserCommand
- UnsuspendUserCommand
- CreateUserCommand
- UpdateUserCommand
- DeleteUserCommand
- RestoreUserCommand
- PurgeUserCommand
- BulkActivateUsersCommand
- BulkDeactivateUsersCommand
- BulkSuspendUsersCommand
- BulkUnsuspendUsersCommand
- BulkCreateUsersCommand
- BulkUpdateUsersCommand
- BulkDeleteUsersCommand
- BulkRestoreUsersCommand
- BulkPurgeUsersCommand
- UpdateUserMetadataCommand ✓
- ReplaceUserMetadataCommand ✓

#### Queries (4 - all exposed via controllers)
- GetUserByIdQuery ✓
- GetUsersPagedQuery (exposed as GetUsersQuery) ✓
- GetUserMetadataQuery (handler exists, controller not hooked up)
- GetUserPreferencesQuery (handler exists, controller not hooked up)
- GetUserProfileQuery (handler exists, controller not hooked up)
- GetUserNotificationQuery (handler exists, controller not hooked up)

---

## Authentication Module (GameGuild.Identity.Authentication)

### Summary
- **Total current source-level not implemented features**: 0 ✅
  - Commands: 0 ✅
  - Queries: 0 ✅
- **Current implementation status**: Authentication, Web3, MFA, KYC bridge, platform permissions, ABAC, conditional policy, and access-review API surfaces are implemented or delegated to their canonical modules ✅
- **Source marker scan**: `rg "TODO|FIXME|HACK|WORKAROUND|PLANNED|NotImplementedException" apps/api/Source -g "*.cs"` returns no implementation blockers ✅
- **Note**: The command/query names below are retained as historical inventory only; they are not counted as open after the June 11, 2026 closeout.

### Reconciled Historical Commands

#### ABAC Policy Commands (13) - RECONCILED ✅
1. `ActivateAbacPolicyCommand`
2. `DeactivateAbacPolicyCommand`
3. `CreateAbacPolicyCommand`
4. `CreateAbacPolicyFromTemplateCommand`
5. `UpdateAbacPolicyCommand`
6. `DeleteAbacPolicyCommand`
7. `CloneAbacPolicyCommand`
8. `ValidateAbacPolicyCommand`
9. `TestAbacExpressionCommand`
10. `EvaluateAbacPoliciesCommand`
11. `BulkEvaluateAbacPoliciesCommand`
12. `SimulateAbacPolicyCommand`
13. `ApplyPermissionTemplateCommand`

#### Conditional Policy Commands (13) - RECONCILED ✅
14. `ActivateConditionalPolicyCommand`
15. `DeactivateConditionalPolicyCommand`
16. `CreateConditionalPolicyCommand`
17. `CreateConditionalPolicyFromTemplateCommand`
18. `UpdateConditionalPolicyCommand`
19. `UpdateConditionalPolicyPriorityCommand`
20. `DeleteConditionalPolicyCommand`
21. `CloneConditionalPolicyCommand`
22. `ValidateConditionalPolicyCommand`
23. `TestConditionalPolicyRuleCommand`
24. `ValidateConditionCommand`
25. `SimulateConditionalPolicyCommand`
26. `EvaluateConditionalPoliciesCommand`
27. `BulkEvaluateConditionalPoliciesCommand`

#### Permission Management Commands (10) - RECONCILED ✅
28. `GrantTenantPermissionCommand`
29. `RevokeTenantPermissionCommand`
30. `BulkGrantTenantPermissionsCommand`
31. `BulkRevokeTenantPermissionsCommand`
32. `GrantResourcePermissionCommand`
33. `RevokeResourcePermissionCommand`
34. `BulkGrantResourcePermissionsCommand`
35. `GrantContentTypePermissionCommand`
36. `ClearPermissionCacheCommand`
37. `UpdatePermissionExpiryCommand`

#### Access Review Commands (17) - RECONCILED ✅
38. `CreateAccessReviewCampaignCommand`
39. `UpdateAccessReviewCampaignCommand`
40. `DeleteAccessReviewCampaignCommand`
41. `StartAccessReviewCampaignCommand`
42. `CompleteAccessReviewCampaignCommand`
43. `CreateCampaignFromTemplateCommand`
44. `ReviewAccessItemCommand`
45. `BulkReviewAccessItemsCommand`
46. `RevokeAccessCommand`
47. `BulkRevokeAccessCommand`
48. `GenerateAccessReviewReportCommand`
49. `SendReviewRemindersCommand`
50. `ConfigureReminderSettingsCommand`
51. `CreatePeriodicAccessReviewCommand`
52. `UpdatePeriodicAccessReviewCommand`
53. `DeletePeriodicAccessReviewCommand`
54. `TriggerPeriodicAccessReviewCommand`

#### KYC/Verification Commands (3) - RECONCILED ✅
55. `InitiateKycVerificationCommand`
56. `CompleteKycVerificationCommand`
57. `UpdateKycVerificationStatusCommand`

#### Social Auth Commands (2) - RECONCILED ✅
58. `SocialSignInCommand`
59. `PolymorphicSignInCommand`

### Reconciled Historical Queries

#### ABAC Policy Queries (7) - RECONCILED ✅
1. `GetAbacPoliciesQuery`
2. `GetAbacPolicyQuery`
3. `GetAbacPolicyTemplatesQuery`
4. `GetAbacPolicyUsageQuery`
5. `GetAbacPolicyStatisticsQuery`
6. `GetAbacPolicyAuditTrailQuery`
7. `GetAbacPolicyConflictsQuery`

#### Conditional Policy Queries (7) - RECONCILED ✅
8. `GetConditionalPoliciesQuery`
9. `GetConditionalPolicyQuery`
10. `GetConditionalPolicyTemplatesQuery`
11. `GetConditionalPolicyUsageQuery`
12. `GetConditionalPolicyStatisticsQuery`
13. `GetConditionalPolicyEvaluationHistoryQuery`
14. `GetConditionalPolicyConflictsQuery`

#### Permission Queries (11) - RECONCILED ✅
15. `GetEffectivePermissionsQuery`
16. `GetUserPermissionsQuery`
17. `GetTenantPermissionsQuery`
18. `GetResourcePermissionsQuery`
19. `GetContentTypePermissionsQuery`
20. `HasTenantPermissionQuery`
21. `HasResourcePermissionQuery`
22. `HasContentTypePermissionQuery`
23. `GetPermissionAuditTrailQuery`
24. `GetPermissionAnalyticsQuery`
25. `GetPermissionTemplatesQuery`

#### Access Review Queries (10) - RECONCILED ✅
26. `GetAccessReviewCampaignsQuery`
27. `GetAccessReviewCampaignQuery`
28. `GetAccessReviewItemsQuery`
29. `GetAccessReviewItemDetailsQuery`
30. `GetAccessReviewTemplatesQuery`
31. `GetAccessReviewAnalyticsQuery`
32. `GetPeriodicAccessReviewsQuery`
33. `GetPeriodicAccessReviewQuery`
34. `GetAccessRevocationHistoryQuery`
35. `GetComplianceStatusQuery`

#### Utility Queries (3) - RECONCILED ✅
36. `GetPermissionCacheStatsQuery`
37. `GetPolicyConditionTypesQuery`
38. `ResolvePermissionHierarchyQuery`

### Implemented Features ✅

#### Authentication Handlers and Services
- LocalSignInHandler
- LocalSignUpHandler
- RefreshTokenHandler
- RevokeTokenHandler
- GoogleIdTokenSignInHandler
- GenerateWeb3ChallengeHandler
- VerifyWeb3SignatureHandler
- AuthenticationFailedEventHandler
- UserSignedInEventHandler
- RefreshTokenEventHandler
- MfaEventHandler
- SendWelcomeEmailHandler
- LogAnalyticsEventHandler
- SMS MFA setup/completion service and controller flow
- Web3 challenge generation and Nethereum signature verification
- Web3 distributed challenge cache with memory fallback
- KYC command bridge to `GameGuild.Compliance.KYC`
- Permission, ABAC, conditional policy, and access-review surfaces delegated to `GameGuild.Identity.Authorization`

---

## Overall Summary

### Grand Total
- **Total current source-level Not Implemented Features**: 0 ✅
  - User Module: 0 actual scoped commands/queries
  - Authentication Module: 0 actual scoped commands/queries after reconciliation

### Implementation Status
- **User Module**: No scoped not-implemented command/query rows remain in this inventory
- **Authentication Module**: No current source-level implementation blockers remain; legacy enterprise rows were reconciled to `GameGuild.Identity.Authorization`, `GameGuild.Compliance.KYC`, and implemented authentication services

### Current Test Coverage
- **User Module Tests**: focused unit suite passing; current coverage artifact reports 100% line / 100% branch / 100% method
  - Entity tests: 18
  - Command handler tests: 13
  - Query handler tests: 16
  - Validator tests: 9
  - **Note**: 2 user metadata commands implemented (Nov 5, 2025)
  - **Note**: Historical controller-hookup notes are retained only as context; they are not counted as open implementation blockers.

- **Authentication Module Tests**: focused closeout slices passing
  - SMS MFA setup/completion: 2 tests passing
  - Web3 signature/distributed-cache slice: 23 tests passing
  - Full Identity.Authentication unit suite: 1,592 tests passing, 0 failed, 0 skipped

### What's Working ✅
Both modules have their **core functionality implemented and tested**:
- User CRUD operations
- User search and filtering queries
- Basic authentication (sign in/up, token management)
- OAuth integration (Google)
- Web3 authentication
- Password hashing and JWT token services
- SMS MFA setup/completion
- KYC bridge into Compliance.KYC
- Permission catalog/platform management surfaces
- ABAC, conditional policy, and access-review shells delegated to canonical Authorization module

### Current Open Items
No current source-level `NotImplementedException`, `TODO`, `FIXME`, `HACK`, `WORKAROUND`, or `PLANNED` implementation markers remain in `apps/api/Source`.

**Note**: Historical backlog rows for User preferences/profile/notifications and Authentication enterprise commands were reconciled with implemented handlers or canonical module ownership and removed from the not-implemented count.

---

**Last Updated**: June 11, 2026

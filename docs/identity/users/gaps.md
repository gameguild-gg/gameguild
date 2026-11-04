# Identity.Users — Consolidated Gaps

> `develop` 7ae5a28a65 · Every gap with `file:line`, classified NEW (no issue found on develop referencing it). Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Users/` unless stated.

## A. Dead code / wiring

| # | Gap | Evidence | Suggested severity |
|---|-----|----------|--------------------|
| U1 | **11 dead request types** — declared queries with no handler and no route | `Queries/UserQueries.cs:8-32` (`GetUserCustomFieldsQuery`, `GetUserTagsQuery`, `GetUserNotificationPreferencesQuery`, `GetUserPrivacyPreferencesQuery`, `GetUserAvatarQuery`, `GetUserBannerQuery`, `GetUserNotificationsPageQuery`, `GetUserNotificationCountQuery`, `GetUserNotificationDeliverySettingsQuery`, `GetUserAccessibilityPreferencesQuery`), `Queries/GetUsersQuery.cs:20` (`GetUsersMetadataQuery`) | medium — candidate for a `G7` wiring-gap guard entry ("declared request types with no handler"), modeled on `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt` |
| U2 | 14 command types without validators, including both purge paths and the bulk notification verbs | `Commands/PurgeUser/`, `Commands/BulkPurgeUsers/`, `Commands/BulkRestoreUsers/`, `Commands/BulkArchiveNotifications/`, `Commands/BulkMarkNotificationsAsRead/`, `Commands/BulkMarkNotificationsAsUnread/`, `Commands/BulkUnarchiveNotifications/`, `Commands/RestoreUser/`, `Commands/SuspendUser/`, `Commands/UnsuspendUser/`, `Commands/UnarchiveNotification/`, `Commands/{Replace,Update,Reset}UserLocalizationPreferences/` | medium |

## B. Events

| # | Gap | Evidence | Severity |
|---|-----|----------|----------|
| U3 | 4 orphan producers: suspend/unsuspend/restore/purge publish notifications with zero consumers — suspensions and purges are invisible downstream | `Commands/SuspendUser/SuspendUserCommandHandler.cs:20`, `Commands/UnsuspendUser/UnsuspendUserCommandHandler.cs`, `Commands/RestoreUser/RestoreUserCommandHandler.cs:22`, `Commands/BulkRestoreUsers/BulkRestoreUsersCommandHandler.cs:35`, `Commands/PurgeUser/PurgeUserCommandHandler.cs:36`, `Commands/BulkPurgeUsers/BulkPurgeUsersCommandHandler.cs:47`; no `INotificationHandler<T>` anywhere in `apps/api/Source` | high (operational blindness) |
| U4 | 5 dead notification types (no producer, no consumer): Created/Updated/Deleted/Activated/Deactivated | `Events/UserCreatedNotification.cs` et al. | low (cleanup) |

## C. Authorization contract

| # | Gap | Evidence | Severity |
|---|-----|----------|----------|
| U5 | **The self-permission that is never granted** — `SelfOrPermission` rules require `users:read:self`/`users:edit:self` even for self actions, but no seeder, role definition, or sign-up flow grants them: ordinary authenticated users get 403 on their own preferences/profiles/metadata until an admin grants `users:*:self` or `users:manage` | rule params `GameGuild.Identity.Authorization/Services/PolicyDefinitionSeeder.cs:852-853,882-883`; evaluator lookup `GameGuild.Identity.Authorization/Rules/Evaluators/SelfOrPermissionRuleEvaluator.cs:97-107`; grant census: only typed-registry + seeder references exist (`GameGuild.Identity.Authorization/Models/TypedPermissions.cs:138,144`) | high (product-level: the self-service surface is effectively admin-only) — either seed the permissions into default roles or change the rule to a pure self-claim check |
| U6 | `GET /users` (the user directory) has no permission policy — any authenticated principal can page users | `Controllers/UsersController.cs:14-17,58` vs `UserProfilesController.cs:22-27` (`Policies.UsersRead`) | high (information disclosure) |
| U7 | No `[AuthorizeRequest]` on any request — single-gate authorization; a future attribute-less route would be unprotected by the CQRS pipeline | grep-zero across module | design note (consider at least the collection verbs) |

## D. Semantics

| # | Gap | Evidence | Severity |
|---|-----|----------|----------|
| U8 | `ValidatePurge` failures surface as 500 (`InvalidOperationException` → generic handler) instead of 409/422 | `Entities/User.cs:395-409`; mapping `GameGuild.SharedKernel/Middlewares/ExceptionHandlingMiddleware.cs:124-143` | medium |
| U9 | Suspended users can still sign in — `CanSignIn` ignores `IsSuspended` | `ValueObjects/UserStatus.cs:56` | medium (cross-module: relies on Authentication-side containment) |
| U10 | Deprecated users-side notification preferences drift independently of the Notifications module's live copy (split-brain) | `Controllers/UserPreferencesController.cs:92-104` | medium |
| U11 | Preference/metadata writes have no audit trail and no event | absence across `Commands/Replace*Preferences/`, `Commands/Update*Preferences/` | medium (forensics) |
| U12 | Mapping-only unit tests (mocked `ISender`) + CRUD-only integration tests — no pipeline-level coverage of the 403 contract or the validator gaps | `GameGuild.Identity.Users.UnitTests/Controllers/`, `GameGuild.Identity.Users.IntegrationTests/UserCrudIntegrationTests.cs` | medium (test gap) |

## Suggested issue split

1. `bug(identity): seed users read-edit self permissions or make SelfOrPermission pure-claim — self-service endpoints 403 for ordinary users` (U5).
2. `bug(identity): GET /users lacks a collection policy — user directory enumerable by any authenticated principal` (U6).
3. `chore(identity): remove or wire the 11 dead Users query types + add G7 guard entries` (U1).
4. `bug(identity): orphan user lifecycle notifications — suspension and purge invisible downstream` (U3+U4).
5. `chore(identity): validators for the 14 uncovered command types + 4xx for ValidatePurge` (U2+U8).

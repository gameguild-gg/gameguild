# Preferences & Per-User Notifications Flow — including the actual 403 contract

> Families F2 (preferences) + F3 (per-user notification states) of `GameGuild.Identity.Users` · `develop` 7ae5a28a65.
> This document records the **actual contract** behind the users-preferences 403s observed in e2e logs — traced end-to-end below, not hypothesized.

## 1. The preferences surface

Five preference families live in one JSON-per-column entity (`Entities/UserPreferences.cs:42-70` — `GeneralPreferences`, `NotificationPreferences`, `AccessibilityPreferences`, `PrivacyPreferences`, `LocalizationPreferences`, each defaulted `"{}"`). Each family exposes the same verb set on `UserPreferencesController` (473 lines):

| Verb | Route shape | Request |
|---|---|---|
| GET | `v{v}/users/{userId}/preferences[/{family}]` | `GetUserPreferencesQuery` — **all family GETs reuse this one query and project the field** (`Controllers/UserPreferencesController.cs:28,105,122,199,215,289`) |
| PUT (replace) | `…/preferences[/{family}]` | `ReplaceUser*PreferencesCommand` |
| PATCH (partial) | `…/preferences[/{family}]` | `UpdateUser*PreferencesCommand` |
| POST `:reset` | `…/preferences[/{family}]:reset` | `ResetUser*PreferencesCommand` |

Metadata follows the same replace/update pattern on `UserMetadataController` (`v{v}/users/{userId}/metadata`); profiles on `UserProfilesController` (GET list uses `Policies.UsersRead`, the collection-grade permission).

The notification-preferences sub-family is **deprecated** on this controller in favor of the Notifications module's own preferences endpoints (`[Obsolete]` + doc comments at `Controllers/UserPreferencesController.cs:92-104`).

## 2. The 403 contract — why authenticated users cannot read their own preferences

The e2e mystery: an authenticated, non-admin user calls `GET /users/{selfId}/preferences` and receives 403. Chain of custody:

1. **Endpoint** — `[Authorize(Policy = Policies.UsersReadSelf)]` (`Controllers/UserPreferencesController.cs:22`); policy name `Users.ReadSelf` (`GameGuild.Identity.Authorization/Policies.cs:169`).
2. **Policy materialization** — `DbAuthorizationPolicyProvider.GetPolicyAsync` loads the seeded definition; a *missing* definition would synthesize deny (`Providers/DbAuthorizationPolicyProvider.cs:86-93`) — not the case here, the definition exists (`GameGuild.Identity.Authorization/Services/PolicyDefinitionSeeder.cs:830-858`).
3. **Seeded rules** — `TenantMatch(allowNoTenant: true)` then `SelfOrPermission(selfPermission: "users:read:self", anyPermission: "users:manage")` (`PolicyDefinitionSeeder.cs:836-857`).
4. **Rule evaluation** — `SelfOrPermissionRuleEvaluator`:
   - unauthenticated → fail (`Rules/Evaluators/SelfOrPermissionRuleEvaluator.cs:41-45`);
   - **SystemAdmin role short-circuits to success** (`:60-66`);
   - even when `currentUserId == targetUserId` (a self action), it performs `_permissionService.HasPermissionAsync(user, tenant, "users:read:self")` and **fails if the permission is not granted** (`:97-107`);
   - acting on another user additionally requires target-tenant membership + `users:manage` (`:109-133`).
5. **The grant that never happens** — `users:read:self` / `users:edit:self` exist only in the typed registry (`GameGuild.Identity.Authorization/Models/TypedPermissions.cs:138,144,171,177`) and the seeder's rule parameters; **no role definition, no default grant, no sign-up flow grants them** (grep across `apps/api/Source`: only the four hits in §4 of the README). Ordinary users therefore fail rule 2 → the policy fails → **403**.

**Contract statement (actual, as shipped):** the users preferences/profiles/metadata self-service endpoints are usable only by (a) principals carrying the SystemAdmin role, or (b) principals explicitly granted `users:read:self`/`users:edit:self` (self scope) or `users:manage`/`users:read`-family permissions (any-user scope). For everyone else the endpoints answer 403 — including for their own data. Front-ends must treat this as expected behavior until the permission is seeded into default roles or the evaluator is changed to a pure self-claim check.

**Where AuthorizationBehavior does NOT run:** none of these requests carry `[AuthorizeRequest]` (zero in the module), so the CQRS authorization pipeline never re-checks — the controller attribute is the single gate.

## 3. Per-user notification states (F3)

`UserNotificationsController` (241 lines) manages the *copy* of notifications stored per user (`Entities/UserNotification.cs`): `MarkAsRead/MarkAsUnread` (guarded `if (!IsRead)` / `if (IsRead)`, `:124-143`), archive/unarchive (`IsArchived`, `ArchivedAt`, `:74-84`), single and bulk (`:mark-as-read`, `:mark-as-unread`, `:archive`, `:unarchive` verbs, bulk bodies with id lists). State machine in `state-machines.md` §3.

Edge matrix (F3):

| Edge | Guard | Result | Evidence |
|---|---|---|---|
| mark-as-read an already-read notification | `if (!IsRead)` | no-op success | `Entities/UserNotification.cs:124-132` |
| mark-as-unread an unread notification | `if (IsRead)` | no-op success | `:137-143` |
| archive an archived notification | **none** — flag rewrite, `ArchivedAt` refreshed | 200 | absence of guard in the entity |
| act on another user's notification | route policy `UsersEditSelf` (same §2 contract) | 403 for ordinary users | `Controllers/UserNotificationsController.cs:53-54` etc. |
| notification id not found | repository returns null → controller maps | 404 | controller `GetNotification` (`:165-178`) |

## 4. Preferences edge matrix (F2)

| Edge | Guard | Result | Evidence |
|---|---|---|---|
| GET preferences for a user with no row | handler returns null → `NotFound()` | 404 | `Controllers/UserPreferencesController.cs:28-34` |
| reset a family that is already default | no diff check — rewrites defaults | 200 | `Commands/ResetUserPreferences/*` |
| PATCH with invalid JSON payload | validator + `Dictionary<string, object?>` parse | 400/422 | `Entities/UserPreferences.cs:77` |
| replace with empty object | allowed — family wiped to `{}` | 200 | validators allow empty dictionaries |
| deprecated notification-preferences verbs | `[Obsolete]` but still routed and enforced | 200/403 as §2 | `Controllers/UserPreferencesController.cs:92-104` |
| cross-family bleed | columns are independent — no validator crosses families | isolated | `Entities/UserPreferences.cs:42-70` |

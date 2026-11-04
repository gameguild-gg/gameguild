# Identity.Users CQRS Wiring Matrix

> Every live endpoint × request × validator × handler × auth attribute, reconciled against the dead request types · `develop` 7ae5a28a65.
> Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Users/`. V = validator present. H = handler present (all commands have handlers; the dead set is query-side).

## 1. Reconciliation summary — the census the wiring-gap guards would ratchet

| Census bucket | Count | Meaning |
|---|---|---|
| Controller routes (live endpoints) | 53 | 6 controllers; route → request mapping verified per section below |
| Command types with handler + route | 45 | every command is wired both sides |
| Command types **without validator** | 14 | `BulkArchiveNotifications`, `BulkMarkNotificationsAsRead`, `BulkMarkNotificationsAsUnread`, `BulkPurgeUsers`, `BulkRestoreUsers`, `BulkUnarchiveNotifications`, `PurgeUser`, `ReplaceUserLocalizationPreferences`, `ResetUserLocalizationPreferences`, `RestoreUser`, `SuspendUser`, `UnarchiveNotification`, `UnsuspendUser`, `UpdateUserLocalizationPreferences` — no FluentValidation gate |
| Query types with handler + route | 10 | `GetUsersQuery`, `GetUsersPagedQuery`, `GetUserByIdQuery`, `GetUserMetadataQuery`, `GetUserNotificationQuery`, `GetUserNotificationsPagedQuery`, `GetUserPreferencesQuery`, `GetUserProfileQuery`, `GetUserProfilesPagedQuery` (+ count/exists verbs reuse list queries) |
| **Dead request types (no handler, no route)** | **11** | the G7-shaped finding — declared, never wired |

**The 11 dead request types** (all queries, all declared in `Queries/UserQueries.cs` except the last):

| # | Type | Line | Designed purpose (inferred from shape) |
|---|---|---|---|
| 1 | `GetUserCustomFieldsQuery` | `Queries/UserQueries.cs:8` | custom-fields projection of metadata |
| 2 | `GetUserTagsQuery` | `:10` | tag list |
| 3 | `GetUserNotificationPreferencesQuery` | `:14` | notification family read |
| 4 | `GetUserPrivacyPreferencesQuery` | `:18` | privacy family read |
| 5 | `GetUserAvatarQuery` | `:22` | avatar |
| 6 | `GetUserBannerQuery` | `:24` | banner |
| 7 | `GetUserNotificationsPageQuery` | `:26` | legacy paging (superseded by `GetUserNotificationsPagedQuery`, which IS wired) |
| 8 | `GetUserNotificationCountQuery` | `:28` | unread count |
| 9 | `GetUserNotificationDeliverySettingsQuery` | `:32` | delivery settings |
| 10 | `GetUserAccessibilityPreferencesQuery` | (same file, family set) | accessibility family read |
| 11 | `GetUsersMetadataQuery` | `Queries/GetUsersQuery.cs:20` | platform users-metadata rollup |

Runtime impact: **none** (unrouted types cannot 500); they are dead code and a false API-design signal. The live per-family GETs satisfy the same need by projecting fields of `GetUserPreferencesQuery` results (e.g. `Controllers/UserPreferencesController.cs:215-219`). A `G7|GameGuild.Identity.Users|<type>` baseline modeled on `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt` would pin them until deleted or wired.

## 2. UsersController

| Route | Request | V | H | Auth |
|---|---|---|---|---|
| POST `v{v}/users` | `CreateUserCommand` | ✓ | ✓ | `Policies.UsersCreate` |
| GET `v{v}/users` | `GetUsersQuery` | ✓ | ✓ | controller `[Authorize]` only — **no policy** (`UsersController.cs:14-17,58`) |
| GET `v{v}/users/{id}` | `GetUserByIdQuery` | ✓ | ✓ | `Policies.UsersReadSelf` |
| PATCH `v{v}/users/{id}` | `UpdateUserCommand` | ✓ | ✓ | `Policies.UsersEditSelf` |
| PUT `v{v}/users/{id}` | `UpdateUserCommand` (replace) | ✓ | ✓ | `Policies.UsersEditSelf` |
| DELETE `v{v}/users/{id}` | `DeleteUserCommand` | ✓ | ✓ | `Policies.UsersDeleteSelf` |

## 3. UsersBulkController (347 lines)

| Route | Request | V | Auth |
|---|---|---|---|
| POST `users:create` | `BulkCreateUsersCommand` | ✓ | `UsersCreate` |
| POST `users:update` / `users:replace` | `BulkUpdateUsersCommand` | ✓ | `UsersUpdate` |
| POST `users:delete` | `BulkDeleteUsersCommand` | ✓ | `UsersDelete` |
| POST `users:activate`/`deactivate`/`suspend`/`unsuspend`/`undelete` | matching `Bulk*UsersCommand` | ✓ except `BulkRestoreUsers` (undelete) | `UsersAdmin` |
| POST `users:purge` | `BulkPurgeUsersCommand` | ✗ | `UsersPurge` |
| POST `users/{id}:activate`/`deactivate`/`suspend` | single commands | ✗ for `SuspendUser` | `UsersAdmin` |
| POST `users/{id}:unsuspend`/`undelete` | single commands | ✗ for `UnsuspendUser`, `RestoreUser` | `UsersAdmin` |
| POST `users/{id}:purge` | `PurgeUserCommand` | ✗ | `UsersPurge` |

## 4. UserProfilesController / UserMetadataController / UserPreferencesController / UserNotificationsController

| Route family | Requests | V | Auth |
|---|---|---|---|
| `users/profiles` (list) + `users/{id}/profile` | `GetUserProfilesPagedQuery`, `GetUserProfileQuery`, `UpdateUserProfileCommand`, `ReplaceUserProfileCommand` | ✓ | list `UsersRead`; item `UsersReadSelf`/`UsersEditSelf` |
| `users/{id}/metadata` | `GetUserMetadataQuery`, `UpdateUserMetadataCommand`, `ReplaceUserMetadataCommand` | ✓ | `UsersReadSelf`/`UsersEditSelf` |
| `users/{id}/preferences[/family][:reset]` | `GetUserPreferencesQuery` + 5×(`Replace…`/`Update…`/`Reset…`) per family | ✓ except the three Localization commands | `UsersReadSelf`/`UsersEditSelf` |
| `users/{id}/notifications[/{nid}][:verb]` | `GetUserNotificationsPagedQuery`, `GetUserNotificationQuery`, `MarkNotificationAsRead/Unread`, `ArchiveNotification`, `UnarchiveNotification` + bulk twins | ✗ for `UnarchiveNotification` + 3 bulk notification commands | `UsersReadSelf`/`UsersEditSelf` |

## 5. HOLE register

| # | Hole | Evidence | Severity |
|---|---|---|---|
| W1 | 11 dead request types (§1) | `Queries/UserQueries.cs:8-32`, `Queries/GetUsersQuery.cs:20` | medium (dead code / guard ratchet) |
| W2 | `GET /users` has no permission policy — any authenticated principal can enumerate users | `Controllers/UsersController.cs:14-17` vs collection policy on profiles (`UserProfilesController.cs:22-27`) | high (information disclosure; contrast `UsersRead` used by profiles list) |
| W3 | 14 command types without validators (incl. both purge paths) | §1 table | medium |
| W4 | `GET /users` controller-level `[Authorize]` but the endpoint is the platform's user directory — policy asymmetry vs `Users.Read` collection permission | `UsersController.cs:14` | same as W2 |
| W5 | No `[AuthorizeRequest]` on any request — single-gate authorization (controller attribute only) | grep-zero across module | design note |
| W6 | Unit tests mirror controllers with mocked `ISender` (mapping-only) — no pipeline-level coverage of the 403 contract; integration tests cover CRUD only (`GameGuild.Identity.Users.IntegrationTests/UserCrudIntegrationTests.cs`, password/username PG suites) | tests tree | medium (test gap — the same shape gaps.md B2 documented for authz) |

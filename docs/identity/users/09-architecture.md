# Identity.Users — Module Architecture

> `develop` 7ae5a28a65 · Anatomy, storage model, authorization posture, and where the module sits in the identity family. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Users/` unless stated.

## 1. Anatomy

```
Controllers/        6 controllers - Users UsersBulk UserProfiles UserPreferences UserMetadata UserNotifications
Commands/           45 command types in per-command folders (Command + Request + Handler [+ Validator] + Response)
Queries/            10 wired queries + 11 dead types (UserQueries.cs family + GetUsersQuery.cs rollup)
Entities/           User UserPreferences UserProfile UserMetadata UserNotification (+ EF configurations)
ValueObjects/       UserStatus (derived struct) UsernameSlug SignInIdentifierType
Repositories/       IUserRepository + implementation
Events/             9 in-process notification types (census in event-flows.md)
Architecture/       UseCaseEventContracts.g.cs - 45 command contracts
Configuration/      module options
```

## 2. Storage model — the JSON-column pattern

One row per user for each satellite (`UserPreferences`, `UserProfile`, `UserMetadata`) plus per-notification rows. The five preference families are **five JSON text columns on one row** (`Entities/UserPreferences.cs:42-70`), each defaulting `"{}"` and parsed via `System.Text.Json` into `Dictionary<string, object?>` on read (`:77`). Consequences: no per-field indexing, no partial durability (a family rewrite replaces the whole column), no schema migration for new preference keys, and reads of any family load all five columns' host row.

## 3. Authorization posture

- Single gate: controller `[Authorize(Policy=…)]` attributes. No `[AuthorizeRequest]`, no `AuthorizationBehavior` participation, no resource-level ACLs.
- The self-policies (`UsersReadSelf`/`UsersEditSelf`/`UsersDeleteSelf`) resolve through `SelfOrPermission` rules that require an actual granted permission even for self actions — see `flows/preferences-and-notifications-flow.md` §2 for the full contract and the 403 consequence.
- Collection-grade verbs use `UsersRead`/`UsersCreate`/`UsersUpdate`/`UsersDelete`/`UsersAdmin`/`UsersPurge` (`GameGuild.Identity.Authorization/Policies.cs:248`); `GET /users` is the outlier with no policy (gap W2).
- Data masking: `[DataMaskingResourceType("User")]` on controllers engages the masking pipeline for read paths (`Controllers/UsersController.cs:16`).

## 4. Position in the identity family

- **Upstream of**: Authentication (sign-up creates the User record and publishes `UserCreatedEvent` from *both* modules — `GameGuild.Identity.Authentication/Services/LocalAuthService.cs:861` and `Commands/CreateUser/CreateUserCommandHandler.cs:20`); Authorization (permission subjects); Tenants (memberships referenced by `ValidatePurge`).
- **Consumed by**: the platform reads user profiles/preferences through these endpoints; Notifications keeps its own preferences copy (deprecated split-brain on this module's side).
- **Durable events out**: `UserCreatedEvent`, `UserDeletedEvent` (+ generic operation events). In-process notifications out: 4 orphan producers.

## 5. Test topology

`GameGuild.Identity.Users.UnitTests/` (Commands/Controllers/Entities/Queries/Repositories/Validators/ValueObjects trees — largely mapping-shaped), `GameGuild.Identity.Users.IntegrationTests/` (`UserCrudIntegrationTests.cs`, `UserPasswordHistoryPostgreSqlTests.cs`, `UserUsernamePostgreSqlTests.cs` — CRUD + two PG-specific uniqueness suites). No pipeline-level test exercises the 403 contract (gap W6).

## 6. Design decisions worth keeping

1. **Derived status over a state column** — `UserStatus` computed from flags (`ValueObjects/UserStatus.cs`): no illegal-state rows possible, but no transition guards either (STRIDE §2).
2. **Purge census in the entity** — `ValidatePurge` keeps the cross-aggregate rule (tenant memberships) next to the data it protects.
3. **One query serving five families** — the projection pattern avoids five near-identical handlers; the cost was eleven designed-but-dead per-family queries.
4. **Quota declaration on the contract** — `QuotaImpact = "+1 Users"` on `CreateUserCommand` (`Architecture/UseCaseEventContracts.g.cs:17`) makes user creation quota-observable at the contract layer.

# GameGuild.Identity.Users — Documentation Package

> The user-records module (users, profiles, preferences, metadata, per-user notification states) · Documented against `develop` commit `7ae5a28a65`.
> Conventions follow the campaign package shape (see `docs/authorization/README.md` and `docs/auth/`): flows with edge matrices, wiring matrix, states, events, sequences-lite, STRIDE, DFDs, architecture, gaps.

## Scope

`GameGuild.Identity.Users` — 6 controllers (UsersController, UsersBulkController, UserProfilesController, UserPreferencesController, UserMetadataController, UserNotificationsController), 53 live endpoints, 45 command types + 10 query types wired, **11 declared-but-dead query types** (see below).

## Contents

| Document | What it covers |
|---|---|
| [`flows/user-lifecycle-flow.md`](flows/user-lifecycle-flow.md) | Create → update → suspend/deactivate → soft-delete → restore → purge (single + bulk), with the lifecycle edge matrix |
| [`flows/preferences-and-notifications-flow.md`](flows/preferences-and-notifications-flow.md) | The 5 preferences families (replace/update/reset per family) + per-user notification states — and **the 403 contract** (the actual answer to the e2e users-preferences 403 mystery) |
| [`cqrs-wiring-matrix.md`](cqrs-wiring-matrix.md) | Every endpoint × request × validator × handler × auth attribute, reconciled against the **11 dead request types** the G7-shaped census found |
| [`state-machines.md`](state-machines.md) | User record status (derived), soft-delete/restore/purge gate, notification read/archive state |
| [`event-flows.md`](event-flows.md) | The 9 in-module notifications (census: 4 orphan producers, 5 fully dead) + the two durable `UserCreatedEvent`/`UserDeletedEvent` integrations |
| [`sequences-lite.md`](sequences-lite.md) | Cross-module sequences: sign-up (Authentication publishes UserCreatedEvent), tenant purge coupling, notification delivery |
| [`07-stride-threat-model.md`](07-stride-threat-model.md) | STRIDE per family — self-service admin surface, mass assignment, PII in preferences JSON |
| [`08-dfds.md`](08-dfds.md) | Level-0 + level-1 per family |
| [`09-architecture.md`](09-architecture.md) | Module anatomy: JSON-column preferences, repository, no-[AuthorizeRequest] posture |
| [`gaps.md`](gaps.md) | Consolidated gap register with file:line |

## Headline findings

1. **11 dead request types** — 11 query records are declared and never wired: no handler, no controller route (`Queries/UserQueries.cs:8-32`, `Queries/GetUsersQuery.cs:20`). They are the designed-but-abandoned per-family read path (avatar, banner, tags, custom fields, per-family preferences, notification count/delivery-settings/page, users-metadata rollup); every live sub-family GET instead reuses `GetUserPreferencesQuery` and projects a field (e.g. `Controllers/UserPreferencesController.cs:215-219`). Same *shape* as the wiring-gap guard family (G1-G6 baseline, `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt`) but not yet baselined — a G7 "declared request types with no handler" rule would ratchet them.
2. **The users-preferences 403 is the actual contract, not a bug in the evaluator** — endpoints carry `[Authorize(Policy = Policies.UsersReadSelf/UsersEditSelf)]`; the seeded policy is `TenantMatch` + `SelfOrPermission(selfPermission: users:read:self, anyPermission: users:manage)` (`GameGuild.Identity.Authorization/Services/PolicyDefinitionSeeder.cs:833-858`); `SelfOrPermissionRuleEvaluator` performs a real permission lookup even for **self** actions (`GameGuild.Identity.Authorization/Rules/Evaluators/SelfOrPermissionRuleEvaluator.cs:97-107`) — and **no seeder grants `users:read:self` to anyone** (only SystemAdmin role short-circuits, `:60-66`). An ordinary authenticated user reading their own preferences therefore gets **403** until granted `users:read:self` or `users:manage`. Full anatomy in the preferences flow doc.
3. **No `[AuthorizeRequest]` anywhere in the module** — authorization is attribute-policy-only at the controller; the CQRS `AuthorizationBehavior` never runs for these requests (contrast: 112 platform declarations, `docs/authorization/flows/orchestrated-permission-checks-flow.md`).
4. **4 orphan notification producers + 5 dead notification types** — `UserSuspendedNotification`, `UserUnsuspendedNotification`, `UserRestoredNotification`, `UserPurgedNotification` are published with zero consumers; `UserCreated/Updated/Deleted/Activated/Deactivated` notifications have neither producer nor consumer (`Events/*.cs`; producers at `Commands/SuspendUser/SuspendUserCommandHandler.cs:20` etc.).
5. **Unguarded lifecycle transitions** — `Activate/Deactivate/Suspend/Unsuspend` set flags with no state validation (`Entities/User.cs:288-318`); the only lifecycle gate is `ValidatePurge` (`:395-409`).

## How to read the diagrams

Mermaid `flowchart TD` / `stateDiagram-v2`, no parentheses or colons in labels; `File.cs:123` in prose, `File.cs L123` inside labels. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Users/` unless stated otherwise.

# Identity.Users Event Flows

> `develop` 7ae5a28a65 · Producer/consumer census with durability per stream. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Users/` unless stated.

## 1. Durable integration events (outbox-backed)

| Event | Producers | Consumers | Durability |
|---|---|---|---|
| `UserCreatedEvent` | sign-up: `GameGuild.Identity.Authentication/Services/LocalAuthService.cs:861` (`AddIntegrationEvent`); admin create: `Commands/CreateUser/CreateUserCommandHandler.cs:20` | durable outbox (`OutboxMessage`) → `GameGuild.API/Core/Eventing/OutboxDispatcher.cs:153,283` reflection dispatch to `IIntegrationEventHandler<T>` registrants | **durable** — DbContext captures `IHasIntegrationEvents` into `OutboxMessage` in the same transaction (`GameGuild.API/Database/ApplicationDbContext.cs:184-230`) |
| `UserDeletedEvent` | `Commands/DeleteUser/DeleteUserCommandHandler.cs:25` | same pipeline | **durable** |
| generic operation events | every command via the use-case operation context (`ApplicationDbContext.cs:246-262`) | operation telemetry | **durable** |

Contract registry: `Architecture/UseCaseEventContracts.g.cs` — 45 command contracts; `CreateUserCommand` additionally declares `ExpectedEventTypes = [UserCreatedEvent]` + `QuotaImpact = "+1 Users"` (`:17`), `DeleteUserCommand` declares `UserDeletedEvent` (`:19`); the other 43 declare `NoDomainEventReason` (the generic operation event remains the observable contract).

## 2. In-process notifications — the census

All 9 types live in `Events/`. Census method: `INotificationHandler<T>` grep across `apps/api/Source` + producer grep within the module.

| Type | Producer | Consumer | Verdict |
|---|---|---|---|
| `UserCreatedNotification` | none | none | **dead type** |
| `UserUpdatedNotification` | none | none | **dead type** |
| `UserDeletedNotification` | none | none | **dead type** |
| `UserActivatedNotification` | none | none | **dead type** |
| `UserDeactivatedNotification` | none | none | **dead type** |
| `UserSuspendedNotification` | `Commands/SuspendUser/SuspendUserCommandHandler.cs:20` | **none** | **orphan producer** |
| `UserUnsuspendedNotification` | `Commands/UnsuspendUser/UnsuspendUserCommandHandler.cs` | **none** | **orphan producer** |
| `UserRestoredNotification` | `Commands/RestoreUser/RestoreUserCommandHandler.cs:22` + `Commands/BulkRestoreUsers/BulkRestoreUsersCommandHandler.cs:35` | **none** | **orphan producer** |
| `UserPurgedNotification` | `Commands/PurgeUser/PurgeUserCommandHandler.cs:36` + `Commands/BulkPurgeUsers/BulkPurgeUsersCommandHandler.cs:47` | **none** | **orphan producer** |

Publishing mechanism: `IPublisher.Publish` (in-process CQRS notifications) — **not** the durable outbox; a publish with no handler is a silent no-op. Consequences: suspensions and purges of users produce no downstream effect (no email, no audit-by-notification, no cache eviction by event) — consumers must be added in the composition root exactly as #1230 did for `AccessReviewReminderNotification` (see `docs/authorization/cross-module-sequences.md` §3 for the pattern).

Same *shape* as the G4 guard family ("declared, never constructed" — that guard only catches the zero-producer half; the orphan-producer half needs the inverse rule, "constructed, never consumed", i.e. the G4-style check the authorization audit ran as E1).

## 3. What does NOT flow (non-events worth naming)

- Preference changes emit **nothing** (no event, no notification) — downstream systems (Notifications module preferences, Analytics) must poll.
- Soft-delete/restore emit nothing durable; only hard `DeleteUserCommand` writes `UserDeletedEvent`. A restored-then-redeleted user emits `UserDeletedEvent` twice — consumers must be idempotent by `EventId` (the outbox inbox pattern provides this, `GameGuild.API/Core/Eventing/OutboxDispatcher.cs`).

# Identity.Tenants Event Flows

> `develop` 7ae5a28a65 · Producer/consumer census with durability. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Tenants/` unless stated.

## 1. The one live durable event

| Event | Producer | Consumer | Durability |
|---|---|---|---|
| `TenantMemberInviteRequestedV1` | `Commands/AddTenantMember/AddTenantMemberCommandHandler.cs:125-133` + `Commands/UpdateTenantMemberInvite/` (resend) — only when an invitee email exists | `GameGuild.Notifications/NotificationsModule.cs:92` → `TenantInviteRequestedHandler` | **durable** — `AddIntegrationEvent` → `OutboxMessage` in the same transaction (`GameGuild.API/Database/ApplicationDbContext.cs:184-230`), dispatched by `GameGuild.API/Core/Eventing/OutboxDispatcher.cs:153,283` |

Contract: `ConditionalEventTypes = [TenantMemberInviteRequestedV1]` on `AddTenantMemberCommand` and `UpdateTenantMemberInviteCommand` — "an invitation event is emitted only when an email-bearing invite is created or resent" (`Architecture/UseCaseEventContracts.g.cs:3,19`). 29 command contracts total; the rest declare the generic operation event only.

## 2. Seven events published into the void — `AddDomainEvent` is a discard bin

`ApplicationDbContext` collects `IHasDomainEvents.DomainEvents` and then only `ClearDomainEvents()`s them — **nothing dispatches or persists in-memory domain events** (`GameGuild.API/Database/ApplicationDbContext.cs:179-183` collect, `:348-351` clear; no other consumer of the collection exists). Every `AddDomainEvent` call in the product is therefore a silent no-op:

| Event | Producer (into the void) | Intended consumers (none) |
|---|---|---|
| `TenantActivatedEvent` | `Entities/Tenant.cs:142` | — billing/limits re-enable, audit |
| `TenantDeactivatedEvent` | `:154` | — session policy, notifications pause |
| `TenantUpdatedEvent` | `:167` | — cache invalidation, audit |
| `TenantArchivedEvent` | `:181` | — retention, data-isolation |
| `TenantRestoredEvent` | `:195` | — re-enable |
| `TenantMemberAddedEvent` | `Commands/AddTenantMember/AddTenantMemberCommandHandler.cs:81,113` | — welcome, audit |
| `TenantMemberRemovedEvent` | `Commands/RemoveTenantMember/RemoveTenantMemberCommandHandler.cs:33` | — access revocation fan-out, audit |

This is a **distinct failure class from the G4 guard**: G4 catches events never constructed; these seven ARE constructed but no dispatch path exists. The wiring-gap guard cannot see them (its rule is constructor-based, `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt:30-31`). Fix direction: either migrate them to `AddIntegrationEvent` (durable V1 events) or add a post-save dispatcher for `IHasDomainEvents`.

## 3. Dead domain events (never constructed — baselined)

| Event | Baseline entry | Issue |
|---|---|---|
| `TenantCreatedEvent` | `wiring-gap-baseline.txt:39` | #1184 |
| `TenantDeletedEvent` | `:40` | #1184 |
| `TenantSubscriptionPlanChangedEvent` | `:41` | #1184 |

Note the asymmetry worth an issue comment: tenant **creation and deletion** — the highest-impact lifecycle moments — have no event at all, while intermediate transitions have discarded ones.

## 4. Audit streams

`Entities/TenantAuditLog.cs` + `GetTenantAuditLogQuery` serve the per-tenant audit view. Writes to it: none found in the module's command handlers (no `TenantAuditLog` producer) — the table is read-side only from this module's perspective; whether the Compliance.Audit module writes tenant rows is that module's package territory. Flagged as a gap (`gaps.md` T10) pending the compliance package's confirmation.

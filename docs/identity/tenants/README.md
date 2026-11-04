# GameGuild.Identity.Tenants — Documentation Package

> The tenancy module (tenant lifecycle, settings, metadata, memberships, invites, usage tracking) · Documented against `develop` commit `7ae5a28a65`.
> Conventions follow the campaign package shape (see `docs/authorization/README.md`).

## Scope

`GameGuild.Identity.Tenants` — 6 controllers (`TenantsController`, `TenantBulkOperationsController`, `TenantLifecycleController`, `TenantSettingsController`, `TenantMetadataController`, `UserMembershipsController`), 46 live endpoints, ~30 command + 8 query request types, the host's tenant-resolution middleware (`Middleware/TenantMiddleware.cs`).

## Contents

| Document | What it covers |
|---|---|
| [`flows/tenant-lifecycle-flow.md`](flows/tenant-lifecycle-flow.md) | Create → update → activate/deactivate → archive/unarchive → delete → recover/undelete → purge (single + bulk) with edge matrix; the E9 not-found→500 classes |
| [`flows/membership-and-invites-flow.md`](flows/membership-and-invites-flow.md) | Members add/remove/role/status, the 3-action invite lifecycle, tenant resolution middleware |
| [`cqrs-wiring-matrix.md`](cqrs-wiring-matrix.md) | Every endpoint × request × handler × auth — including the **unauthenticated-policy controller set** (CRUD/lifecycle/bulk/metadata carry `[Authorize]` only) |
| [`state-machines.md`](state-machines.md) | Tenant activity+archive derived status, soft-delete gates, membership + invite states |
| [`event-flows.md`](event-flows.md) | The 3 dead domain events (G4 baseline), the **7 events published into the void** (`AddDomainEvent` is discarded by the save pipeline), and the one live durable invite event |
| [`sequences-lite.md`](sequences-lite.md) | Auth↔tenant-resolution, invite delivery, plan-change coupling |
| [`07-stride-threat-model.md`](07-stride-threat-model.md) | STRIDE — cross-tenant takeover focus (the unguarded lifecycle verbs) |
| [`08-dfds.md`](08-dfds.md) | Level-0 + level-1 per family |
| [`09-architecture.md`](09-architecture.md) | Module anatomy, resolution pipeline position, dead `TenantService` |
| [`gaps.md`](gaps.md) | Consolidated gap register with file:line |

## Headline findings

1. **Unguarded tenant lifecycle verbs** — `TenantsController`, `TenantBulkOperationsController`, `TenantLifecycleController`, `TenantMetadataController` carry **class-level `[Authorize]` only**: any authenticated principal can update, deactivate, archive, delete, or purge ANY tenant by id — the handlers perform no ownership or permission checks (`Commands/UpdateTenant/UpdateTenantCommandHandler.cs:16`, `Commands/DeleteTenant/DeleteTenantCommandHandler.cs:11-19`, bulk twins). Only `TenantSettingsController` (`SettingsRead/Manage`) and `UserMembershipsController` (`MembersManage`/`UsersReadSelf`) carry policies. Self-service **create** is intentional (creator becomes Owner, `Commands/CreateTenant/CreateTenantCommandHandler.cs:38-49`); the rest is a takeover class.
2. **E9: not-found → 500** — 3 handler paths + 6 `TenantService` paths signal missing tenants with `InvalidOperationException`/`KeyNotFoundException`, which the shared middleware maps to the generic 500 (`Queries/GetTenantAuditLog/GetTenantAuditLogQueryHandler.cs:20`; `Commands/DeactivateTenant/…Handler.cs:16`, `Commands/UpdateTenant/…Handler.cs:16`; `Services/TenantService.cs:77,91,100,112,127,143`; mapping `GameGuild.SharedKernel/Middlewares/ExceptionHandlingMiddleware.cs:124-143`). Correct pattern exists (`EntityNotFoundException` → 404) but is unused here.
3. **Seven events published into the void** — `Tenant.Activate/Deactivate/Update/Archive/Unarchive` and the member add/remove handlers raise in-memory domain events via `AddDomainEvent` (`Entities/Tenant.cs:142,154,167,181,195`; `Commands/AddTenantMember/…Handler.cs:81,113`; `Commands/RemoveTenantMember/…Handler.cs:33`) — but `ApplicationDbContext` **collects and then only clears** `IHasDomainEvents` (`GameGuild.API/Database/ApplicationDbContext.cs:179-183,348-351`): nothing dispatches them. Only `AddIntegrationEvent` (the V1 invite event) is durable.
4. **3 dead domain events** (never constructed at all) are baselined by the G4 wiring-gap guard: `TenantCreatedEvent`, `TenantDeletedEvent`, `TenantSubscriptionPlanChangedEvent` (`apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt:39-41`, issue #1184).
5. **TenantMiddleware default-tenant guard** — authenticated traffic that fails tenant resolution does NOT fall back to the platform default tenant (only anonymous traffic does), explicitly to avoid cross-tenant authorization risk (`Middleware/TenantMiddleware.cs:360-368`).
6. **`TenantService` is dead code** — 6 not-found-throwing methods with no callers outside the module (`Services/TenantService.cs`; grep across `apps/api/Source` finds zero consumers).

## How to read the diagrams

Mermaid `flowchart TD` / `stateDiagram-v2`, no parentheses or colons in labels. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Tenants/` unless stated otherwise.

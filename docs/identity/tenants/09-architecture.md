# Identity.Tenants — Module Architecture

> `develop` 7ae5a28a65 · Anatomy, position in the request pipeline, storage model, dead weight. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Tenants/` unless stated.

## 1. Anatomy

```
Controllers/        6 - Tenants TenantBulkOperations TenantLifecycle TenantSettings TenantMetadata UserMemberships
Commands/           28 routed command types + orphan PlanChangeRequest (ChangeTenantPlan folder)
Queries/            9 handled queries - page, by-id, by-slug, active, default, search, members, memberships, audit-log
Entities/           Tenant TenantMember (+invite metadata JSON) TenantSettings TenantMetadata TenantStatistics
                    TenantAuditLog UsageTracking TenantDomain (+configurations)
Middleware/         TenantMiddleware - the host's tenant-resolution stage
Services/           TenantService - DEAD (no consumers)
Repositories/       ITenantRepository + member repository
Utilities/ Extensions/ DTOs/ Models/
Architecture/       UseCaseEventContracts.g.cs - 29 command contracts
```

## 2. Position in the request pipeline

`TenantMiddleware` runs between token revocation and actor capture (`GameGuild.API/Core/Setup/PipelineExtensions.cs:105-114`), making tenancy available to the actor context, the authorization rules (`TenantMatch`, `SelfOrPermission` tenant lookups), and the audit sinks. The module is thus **infrastructure for every other tenant-scoped module**, not just a CRUD surface — which is why the unguarded lifecycle verbs (STRIDE T1) are platform-grade, not module-grade.

## 3. Storage model

- Flags-not-states: `IsActive` + `IsArchived` (+ `ArchivedAt`) on `Tenant`; membership `IsActive` + JSON `Metadata` hosting `TenantMemberInviteMetadata` on `TenantMember`.
- Entity validators exist (`ValidateForMemberAddition`, `ValidateConfiguration`, `ValidateForArchive`, `ValidateForDeletion`, `Entities/Tenant.cs:207-300`) but the corresponding commands consult only the member-addition one — the rest are dead validation surface.
- `TenantAuditLog` table + repository read projection (`Repositories/TenantRepository.cs:188`) with **no writer** in this module.

## 4. Event architecture (summary)

One durable outbound (`TenantMemberInviteRequestedV1` → Notifications), seven discarded in-memory events, three never-constructed events (G4/#1184). Full census in `event-flows.md`. The architectural rule this module proves: **`AddIntegrationEvent` is the only event mechanism that reaches consumers**; `AddDomainEvent` is currently a write-only collection.

## 5. Test topology

`GameGuild.Identity.Tenants.UnitTests/` (Commands/Controllers/Entities/Events/Queries/Repositories/Services/…), `GameGuild.Identity.Tenants.IntegrationTests/TenantCrudIntegrationTests.cs`. No test pins the authorization posture of the lifecycle controllers (gap T9) — the platform's `ControllerAuthorizationArchitectureTests` cover `[AllowAnonymous]` registration, not missing-permission-policy.

## 6. Design decisions worth keeping

1. **Authenticated traffic never inherits the default tenant** (`Middleware/TenantMiddleware.cs:360-368`) — the single most load-bearing tenancy safety control in the module.
2. **Invite metadata as JSON on the membership row** — one row per membership, invite lifecycle in-place; costs queryability of pending invites across tenants.
3. **Creator-becomes-Owner create flow** — self-service tenancy with immediate administrative ownership.
4. **Entity validators as a contract surface** — useful for future command wiring, currently half-connected.

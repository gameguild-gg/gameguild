# Identity.Tenants — Consolidated Gaps

> `develop` 7ae5a28a65 · NEW gaps unless noted. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Tenants/` unless stated.

## A. Authorization (critical)

| # | Gap | Evidence | Suggested severity |
|---|-----|----------|--------------------|
| T1 | **Unguarded tenant lifecycle verbs** — TenantsController, TenantBulkOperationsController, TenantLifecycleController, TenantMetadataController carry class-level `[Authorize]` only; update/deactivate/archive/delete/purge on any tenant by any authenticated principal; no ownership checks in handlers | `Controllers/TenantsController.cs:14`, `Controllers/TenantBulkOperationsController.cs:14`, `Controllers/TenantLifecycleController.cs:14`, `Controllers/TenantMetadataController.cs:15`; `Commands/UpdateTenant/UpdateTenantCommandHandler.cs:11-19`, `Commands/DeleteTenant/DeleteTenantCommandHandler.cs:11-19` | **critical** |
| T1a | Entity validators not consulted: delete never calls `ValidateForDeletion`, archive never calls `ValidateForArchive` | `Entities/Tenant.cs:278,293` vs `Commands/DeleteTenant/`, `Commands/ArchiveTenant/` | high |
| T7 | Owner-membership deactivation has no owner-protection guard (tenant can be left ownerless) | `Commands/SetTenantMembershipStatus/` | medium |

## B. Error semantics (the E9 class)

| # | Gap | Evidence | Severity |
|---|-----|----------|----------|
| T2 | Not-found → 500: `GetTenantAuditLogQueryHandler` (`KeyNotFoundException`), `UpdateTenantCommandHandler` and `DeactivateTenantCommandHandler` (`InvalidOperationException`) — correct pattern `EntityNotFoundException`→404 exists in the shared middleware but is unused here | `Queries/GetTenantAuditLog/GetTenantAuditLogQueryHandler.cs:20`; `Commands/UpdateTenant/UpdateTenantCommandHandler.cs:16`; `Commands/DeactivateTenant/DeactivateTenantCommandHandler.cs:16`; mapping `GameGuild.SharedKernel/Middlewares/ExceptionHandlingMiddleware.cs:124-143` | high |
| T3 | Duplicate-slug create → `InvalidOperationException` → 500 (should be 409) | `Commands/CreateTenant/CreateTenantCommandHandler.cs:24-28` | medium |

## C. Events

| # | Gap | Evidence | Severity |
|---|-----|----------|----------|
| T8 | 7 events published via `AddDomainEvent` are collected-then-cleared by `ApplicationDbContext` — never dispatched or persisted; member-remove invisibility is the sharpest case | `Entities/Tenant.cs:142,154,167,181,195`; `Commands/AddTenantMember/AddTenantMemberCommandHandler.cs:81,113`; `Commands/RemoveTenantMember/RemoveTenantCommandHandler.cs:33`; discard site `GameGuild.API/Database/ApplicationDbContext.cs:179-183,348-351` | high |
| T8a | (companion to T8, repo-wide) the `AddDomainEvent` mechanism itself is a discard bin — either wire a post-save dispatcher or migrate to `AddIntegrationEvent`; same finding filed from `docs/identity/users/` | `GameGuild.API/Database/ApplicationDbContext.cs:348-351` | high (cross-module) |
| — | `TenantCreatedEvent` / `TenantDeletedEvent` / `TenantSubscriptionPlanChangedEvent` dead | already baselined `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt:39-41` (#1184) | filed |
| T10 | `TenantAuditLog` table has no writer in this module — the per-tenant audit view reads a table nothing populates | read path `Controllers/TenantLifecycleController.cs:149`, `Repositories/TenantRepository.cs:188`; no `new TenantAuditLog` in product sources | high (audit gap; confirm against Compliance.Audit package) |

## D. Dead code / wiring

| # | Gap | Evidence | Severity |
|---|-----|----------|----------|
| T4 | `TenantService` (6 methods, E9-style throws) has zero consumers | `Services/TenantService.cs:77-143` | low |
| T5 | `ChangeTenantPlan` folder holds only the orphan `PlanChangeRequest` DTO — no command/handler/route; plan changes owned by Commerce.Billing | `Commands/ChangeTenantPlan/PlanChangeRequest.cs` | low |
| T6 | 4 routed commands without validators: `RecoverTenant`, `SetTenantMembershipStatus`, `ValidateTenant` (+ the orphan folder) | `Commands/*/` census | medium |

## E. Tests

| # | Gap | Evidence | Severity |
|---|-----|----------|----------|
| T9 | No test pins the authorization posture of T1 (integration suite covers CRUD only) | `GameGuild.Identity.Tenants.IntegrationTests/TenantCrudIntegrationTests.cs` | medium |

## Suggested issue split

1. `bug(identity): tenant lifecycle verbs require permission policies — any authenticated principal can delete or purge arbitrary tenants` (T1+T1a+T9) — platform-grade severity.
2. `bug(identity): tenants not-found signals as 500 — use EntityNotFoundException for 404` (T2+T3).
3. `bug(platform): AddDomainEvent events are discarded by the save pipeline — 7 tenant events published into the void` (T8+T8a; coordinate with the Authorization/Users packages' identical finding).
4. `chore(identity): remove dead TenantService and orphan PlanChangeRequest + add validators` (T4+T5+T6).
5. `bug(identity): TenantAuditLog has no writer` (T10).

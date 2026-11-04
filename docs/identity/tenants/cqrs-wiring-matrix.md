# Identity.Tenants CQRS Wiring Matrix

> Every endpoint × request × validator × handler × auth · `develop` 7ae5a28a65. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Tenants/`. **Auth column is the story here**: four of six controllers carry class-level `[Authorize]` only.

## 1. Reconciliation summary

| Census bucket | Count | Notes |
|---|---|---|
| Controller routes (live endpoints) | 46 | 6 controllers |
| Command types routed + handled | 28 | all routed commands have handlers |
| Command types **without validators** | 4 | `RecoverTenant`, `SetTenantMembershipStatus`, `ValidateTenant` (+ orphan `PlanChangeRequest` below) |
| Query types handled | 9 | one handler each (`Queries/*/`) |
| **Orphan DTO** | 1 | `Commands/ChangeTenantPlan/PlanChangeRequest.cs` — a request record with no command, no handler, no route; plan changes are orchestrated by `GameGuild.Commerce.Billing/Commands/OrchestrateSubscriptionPlanChange/` |
| Dead domain events (never constructed) | 3 | `TenantCreatedEvent`, `TenantDeletedEvent`, `TenantSubscriptionPlanChangedEvent` — baselined G4, issue #1184 |
| Discarded-publisher events | 7 | see `event-flows.md` §2 (`AddDomainEvent` → collected then cleared, never dispatched) |
| Unavailable-contract commands | 0 | (contrast: the Authorization module's designed 422 set — none here) |

## 2. Controllers

| Controller | Endpoints | Auth | Requests |
|---|---|---|---|
| `TenantsController` (224 lines) | create, list, get, `:validate`, patch, put, delete | **`[Authorize]` class-only** (`:14`) | `CreateTenantCommand`, `GetTenantsPageQuery`, `GetTenantByIdQuery`, `ValidateTenantCommand` (no validator), `UpdateTenantCommand` (E9-500), `DeleteTenantCommand` |
| `TenantLifecycleController` (185) | `:activate/:deactivate/:archive/:undelete/:purge`, audit-log | **`[Authorize]` class-only** (`:14`) | lifecycle commands + `GetTenantAuditLogQuery` (E9-500) |
| `TenantBulkOperationsController` (192) | 9 bulk verbs | **`[Authorize]` class-only** (`:14`) | `Bulk*TenantsCommand` ×9 |
| `TenantSettingsController` (135) | settings, feature-flags, system-limits, integration-settings (R/W) | **`SettingsRead`/`SettingsManage`** per action (`:31,40,54,68,77,91,100,114,123`) | `Commands/TenantSettingsOperations.cs` |
| `TenantMetadataController` (106) | metadata, custom-fields, tags | **`[Authorize]` class-only** (`:15`) | `Commands/TenantMetadataOperations.cs` |
| `UserMembershipsController` (440) | memberships CRUD + invites + list/count/exists | **`MembersManage`** (writes), **`UsersReadSelf`** (reads) (`:35,78,104,130,157,171,235,264,289`) | `AddTenantMember`, `RemoveTenantMember`, `UpdateTenantMemberRole`, `SetTenantMembershipStatus` (no validator), `UpdateTenantMemberInvite`, membership queries |

## 3. HOLE register

| # | Hole | Evidence | Severity |
|---|---|---|---|
| T1 | Lifecycle/CRUD/bulk/metadata controllers have **no permission policies** — update/deactivate/archive/delete/purge reachable by any authenticated principal; handlers add no ownership check | `Controllers/TenantsController.cs:14`, `TenantLifecycleController.cs:14`, `TenantBulkOperationsController.cs:14`, `TenantMetadataController.cs:15`; handler sample `Commands/DeleteTenant/DeleteTenantCommandHandler.cs:11-19` | **critical** (tenant takeover class) |
| T2 | E9 not-found→500: 3 live handler sites + 6 dead-service sites | `flows/tenant-lifecycle-flow.md` §4 table | high |
| T3 | Duplicate-slug create → 500 instead of 409 | `Commands/CreateTenant/CreateTenantCommandHandler.cs:24-28` | medium |
| T4 | `TenantService` (6 methods) has zero consumers | `Services/TenantService.cs:77-143` | low (dead code) |
| T5 | `PlanChangeRequest` orphan DTO; `ChangeTenantPlan` folder has no command | `Commands/ChangeTenantPlan/PlanChangeRequest.cs` | low |
| T6 | 4 routed commands without validators | §1 | medium |
| T7 | Owner-membership deactivation has no owner-protection guard | absence in `Commands/SetTenantMembershipStatus/` | medium |
| T8 | 7 events published via `AddDomainEvent` are discarded by the save pipeline | `event-flows.md` §2 | high (observability/integration loss) |
| T9 | Unit/integration tests: CRUD integration suite exists (`GameGuild.Identity.Tenants.IntegrationTests/TenantCrudIntegrationTests.cs`) but nothing pins the authorization posture of T1 | tests tree | medium |

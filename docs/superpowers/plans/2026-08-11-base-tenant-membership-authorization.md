# Base Tenant Membership and Authorization Fix Plan

> Execucao local e sequencial: o usuario proibiu o uso de agentes para esta tarefa.

**Goal:** Tornar a membership do tenant base permanente, garantir que `SystemAdmin` seja atribuido nesse tenant e corrigir confusoes entre administracao global e administracao de tenant.

**Architecture:** A camada de comandos de Tenants aplica a invariavel usando `Tenant.IsDefault`; a promocao de `SystemAdmin` resolve o tenant base no servidor e cria/reativa a membership quando necessario. `ActorContext` continua sendo a fonte central de privilegios, mas `Admin` permanece tenant-local e somente `SystemAdmin` recebe bypass global. Regras de ownership do Testing Lab usam o contexto central, inclusive nos handlers ainda nao cobertos.

**Tech Stack:** .NET 10, ASP.NET Core authorization, EF Core, xUnit/Moq/FluentAssertions, Next.js/TypeScript/Vitest.

---

### Task 1: Lock the base membership lifecycle

**Files:**
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Tenants/Commands/AddTenantMember/AddTenantMemberCommandHandler.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Tenants/Commands/RemoveTenantMember/RemoveTenantMemberCommandHandler.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Tenants/Commands/UpdateTenantMemberInvite/UpdateTenantMemberInviteCommandHandler.cs`
- Test: matching command handler tests in `apps/api/tests/GameGuild.Identity.Tenants.UnitTests/Commands/`

1. Add failing tests for create/reactivate, cancel, and remove paths on the default tenant.
2. Verify the focused tests fail for the intended reason.
3. Enforce active membership and reject destructive operations for `Tenant.IsDefault`.
4. Re-run the focused tests.

### Task 2: Assign SystemAdmin in the correct tenant

**Files:**
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Tenants/Abstractions/ITenantRepository.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Tenants/Repositories/TenantRepository.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Tenants/Commands/UpdateTenantMemberRole/UpdateTenantMemberRoleCommandHandler.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Tenants/Queries/GetUserMemberships/*`
- Test: matching tenant handler/query tests.

1. Add failing tests showing a promotion sent with another tenant is resolved to the default tenant.
2. Cover missing and inactive default memberships.
3. Implement server-side target resolution and membership repair.
4. Expose the default-tenant marker so the dashboard consistently edits the base membership.

### Task 3: Correct global versus tenant admin semantics

**Files:**
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Context/Actors/ActorContext.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Tenants/Middleware/TenantMiddleware.cs`
- Modify: `apps/api/Source/GameGuild.API/Core/Extensions/SecurityServiceCollectionExtensions.cs`
- Modify: targeted controllers/handlers with inconsistent role checks.
- Modify: `apps/web/src/lib/community/queries/members.ts`
- Test: Identity Context, Tenant Middleware, authorization policy, Testing Lab, Projects, and web query tests.

1. Add failing tests proving tenant `Admin` is not a global system admin.
2. Restrict global bypass to `SystemAdmin`; retain `Admin` as a tenant-admin alias.
3. Fix incorrect/missing role names and the uncovered Testing Lab session-project guard.
4. Re-run focused API and web tests serially.

### Task 4: Verify and promote through branches

1. Run focused tests with .NET parallelism disabled and run targeted web tests/type checks.
2. Run diff checks and inspect every changed authorization path.
3. Commit and push on `develop`.
4. Fast-forward/merge `develop` into `main`, push `main`, and inspect CI/deployment status.

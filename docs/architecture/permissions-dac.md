# Discretionary Access Control (DAC) & Permissions

Consolidated summary (replaces prior DAC strategy & related docs).

> **See Also**: [Permission Evaluation Policy](../security/PERMISSION_EVALUATION_POLICY.md) for complete multi-layer conflict resolution rules.

## Hierarchy

1. Tenant Level – global tenant capabilities
2. Content-Type Level – entity type capabilities (Program, Post, etc.)
3. Resource Level – specific entity overrides

Resolution order: Resource → Content-Type → Tenant (first explicit grant wins).

## Conflict Resolution (DAC Layer)

**Policy: Allow-Wins (Additive)**

DAC permissions are merged from all sources:
- Global defaults + Tenant defaults + Direct grants = Effective permissions

```
EffectivePermissions = GlobalDefaults ∪ TenantDefaults ∪ DirectGrants
```

**No explicit deny**: Revoking a permission removes the grant; there is no "deny" entry.

> ⚠️ **Note**: DAC is evaluated AFTER Rule-Based and ABAC layers. A deny from those layers overrides any DAC grant. See [Permission Evaluation Policy](../security/PERMISSION_EVALUATION_POLICY.md).

## Permission Types

Composable flags (Create, Read, Update, Delete, Review, Moderate, ManageMembers, etc.) combined per scope.

## Generic Attributes

```csharp
[RequireTenantPermission(PermissionType.ManageMembers)]
[RequireContentTypePermission<Program>(PermissionType.Create)]
[RequireResourcePermission<Program>(PermissionType.Update)]
// Legacy compatibility: [RequireResourcePermission<Program>(PermissionType.Read)]
```

## ProgramContent Inheritance

ProgramContent inherits Program permissions; dedicated ProgramContent rows removed to simplify reasoning.

## Seeding Essentials

Initial seed: tenant admin + baseline content-type grants to avoid early 401/403 responses.

## Adding a Secured Entity

1. Define entity + repository
2. Register content-type permission mapping
3. Decorate endpoints with attributes
4. Extend seeding (optional defaults)
5. Add tests (tenant / content-type / resource scenarios)

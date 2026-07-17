# GameGuild.Resources Module

**Purpose:** Tenant/user resource quota enforcement and usage tracking with atomic consumption patterns.

## Overview

The Resources module provides:
- Quota definition with soft/hard limits per tenant and user
- Atomic quota consumption with optimistic concurrency
- CQRS pipeline behavior for automatic quota enforcement (`[RequiresQuota]` attribute)
- Usage tracking with historical records
- Limit checking with fail-closed semantics

## Architecture

```
Controllers (9):
├── ResourcesController          [Admin only - system-wide operations]
├── TenantQuotasController       [Tenant admins - quota management]
├── TenantResourcesController    [Tenant members - resource usage]
├── TenantResourceMetadataController
├── TenantResourceSettingsController
├── UserQuotasController         [Users - personal quota view]
├── UserResourcesController      [Users - personal usage]
├── UserResourceMetadataController
└── UserResourceSettingsController

Services:
├── ResourceQuotaService         [Core quota operations]
├── CachedResourceQuotaService   [Decorator with 30s cache TTL]
└── ResourceUsageTrackingService [Usage recording]

Pipeline Behaviors:
└── ResourceQuotaBehavior        [Automatic quota enforcement for [RequiresQuota] commands]
```

## Security Controls

### Authentication & Authorization

| Controller | Auth | Access Control |
|------------|------|----------------|
| `ResourcesController` | `[Authorize(Policy = "SystemAdmin")]` | System admins only |
| `TenantQuotasController` | `[Authorize]` + `ValidateTenantMembershipAsync()` | Tenant members |
| `TenantResourcesController` | `[Authorize]` + `ValidateTenantMembershipAsync()` | Tenant members |
| `UserQuotasController` | `[Authorize]` + `ValidateUserOwnership()` | User owns resource or admin |
| `UserResourcesController` | `[Authorize]` + `ValidateUserOwnership()` | User owns resource or admin |

### Rate Limiting

All controllers have `[EnableRateLimiting]` with appropriate policies:
- **Tenant controllers:** `RateLimitPolicies.PerTenant` - Rate limit per tenant ID
- **User controllers:** `RateLimitPolicies.PerUser` - Rate limit per user ID
- **Admin controller:** `RateLimitPolicies.Internal` - Internal rate limit

### Fail-Closed Patterns

1. **Quota enforcement:** Hard limits are always enforced; soft limits trigger warnings only
2. **Unknown users:** Return 403 Forbidden (not 404) to prevent enumeration
3. **Missing quotas:** Default to deny if quota record doesn't exist
4. **Rollback on failure:** `ResourceQuotaBehavior` decrements usage if command fails

## Threat Model (STRIDE)

### Spoofing

| Threat | Mitigation | Status |
|--------|------------|--------|
| Attacker impersonates another tenant | `ValidateTenantMembershipAsync()` checks actor's tenant membership | ✅ Mitigated |
| Attacker impersonates another user | `ValidateUserOwnership()` checks actor ID matches or is admin | ✅ Mitigated |
| Anonymous access to quotas | `[Authorize]` attribute on all controllers | ✅ Mitigated |

### Tampering

| Threat | Mitigation | Status |
|--------|------------|--------|
| Modify quota limits without authorization | Admin-only endpoints require `RequireAdminRole` policy | ✅ Mitigated |
| Race condition on quota consumption | Optimistic concurrency with `Version` property | ✅ Mitigated |
| Bypass quota via direct DB access | Application-level enforcement via `ResourceQuotaBehavior` | ✅ Mitigated |

### Repudiation

| Threat | Mitigation | Status |
|--------|------------|--------|
| Deny quota consumption | `UsageRecord` entities with `CreatedAt`, `Source`, `ActorId` fields | ✅ Mitigated |
| Deny quota changes | `ResourceQuotaChangedEvent` published for audit logging | ✅ Mitigated |

### Information Disclosure

| Threat | Mitigation | Status |
|--------|------------|--------|
| IDOR - access other tenant's quotas | Tenant membership validation on all tenant endpoints | ✅ Mitigated |
| IDOR - access other user's quotas | User ownership validation on all user endpoints | ✅ Mitigated |
| Enumerate tenant IDs | Rate limiting + constant-time responses | ✅ Mitigated |

### Denial of Service

| Threat | Mitigation | Status |
|--------|------------|--------|
| Exhaust tenant quotas maliciously | Quota limits enforced; rate limiting in place | ✅ Mitigated |
| N+1 queries causing DB exhaustion | Batch queries via `GetByTenantAndTypesAsync()` | ✅ Mitigated |
| Memory exhaustion from large result sets | Pagination with max 200 items per page | ✅ Mitigated |

### Elevation of Privilege

| Threat | Mitigation | Status |
|--------|------------|--------|
| User escalates to admin | `RequireAdminRole` policy on admin endpoints | ✅ Mitigated |
| Cross-tenant access | `ITenantMembershipChecker` validates membership | ✅ Mitigated |

## Configuration

```json
{
  "Resources": {
    "Quota": {
      "DefaultSoftLimitPercentage": 80,
      "EnableUsageTracking": true,
      "CacheTtlSeconds": 30
    }
  }
}
```

## Dependencies

- `GameGuild.SharedKernel` - Base entities, CQRS, common abstractions
- `GameGuild.Identity.Authorization` - `ITenantMembershipChecker`, `IActorContextAccessor`
- `GameGuild.Identity.Context` - Actor context for current user/tenant

## Testing

Unit tests: `GameGuild.Resources.UnitTests` (82+ tests)
- Behavior tests including rollback scenarios
- Repository tests for atomic operations
- Attribute tests for `[RequiresQuota]`

---

**Last Updated:** 2026-01-15  
**Security Review:** ✅ All critical issues resolved

# Security Cache Invalidation Strategy

**Module:** GameGuild.Identity.Authorization  
**Last Updated:** January 2026  
**Criticality:** ⚠️ P1 - Security-relevant caching

---

## Overview

The authorization system uses a **version-based cache invalidation** strategy to ensure security data (permissions, ACLs, policies) remains consistent while providing high-performance lookups.

---

## Cache Architecture

### Cache Layers

```
┌─────────────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                                │
│                                                                     │
│  ┌──────────────────────┐    ┌──────────────────────┐              │
│  │ CachedAccessControl  │    │ CachedPolicyDefinition│              │
│  │ ListService          │    │ Store                 │              │
│  │                      │    │                       │              │
│  │ - ACL lookups        │    │ - Policy definitions  │              │
│  │ - Resource perms     │    │ - ABAC rules          │              │
│  └──────────┬───────────┘    └───────────┬───────────┘              │
│             │                            │                          │
│             └──────────┬─────────────────┘                          │
│                        ▼                                            │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                    IMemoryCache                               │  │
│  │    Per-tenant cache keys include version number               │  │
│  │    Key format: "acl:{tenantId}:{subjectId}:v{version}"       │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                        ▲                                            │
│                        │                                            │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │              ITenantSecurityVersionStore                      │  │
│  │                                                               │  │
│  │    GetVersionAsync(tenantId) → Returns current version       │  │
│  │    IncrementVersionAsync(tenantId) → Triggers invalidation   │  │
│  │                                                               │  │
│  │    Implementations:                                           │  │
│  │    • InMemoryTenantSecurityVersionStore (single instance)    │  │
│  │    • DatabaseTenantSecurityVersionStore (distributed)        │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## How Version-Based Invalidation Works

### 1. Cache Key Construction

Every cache key includes the current security version for the tenant:

```csharp
private async Task<string> BuildCacheKeyAsync(Guid tenantId, Guid subjectId)
{
    var version = await _versionStore.GetVersionAsync(tenantId);
    return $"acl:{tenantId}:{subjectId}:v{version}";
}
```

### 2. Cache Invalidation

When permissions change, the version is incremented. Old cache entries naturally become stale because new lookups use the new version in the key:

```csharp
// When granting/revoking permissions:
await _permissionRepository.GrantPermissionAsync(userId, permission);
await _versionStore.IncrementVersionAsync(tenantId);  // Triggers invalidation

// Next cache lookup will use new version:
// Old key: "acl:{tenantId}:{subjectId}:v5"
// New key: "acl:{tenantId}:{subjectId}:v6"
// Old cached data is effectively invalidated (never looked up again)
```

### 3. Automatic Cleanup

Old cache entries expire via the cache's TTL policy. The `IMemoryCache` uses sliding/absolute expiration so stale entries are eventually evicted:

```csharp
_cache.Set(cacheKey, permissions, new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
    SlidingExpiration = TimeSpan.FromMinutes(5)
});
```

---

## Services Using This Pattern

### CachedAccessControlListService

**Purpose:** Caches ACL entries for resource-based access checks.

**Cache Key Pattern:** `acl:{tenantId}:{subjectId}:{resourceType}:{resourceId}:v{version}`

**Invalidation Events:**
- ACL entry created
- ACL entry updated
- ACL entry deleted
- User's role changed

### CachedPolicyDefinitionStore

**Purpose:** Caches ABAC policy definitions.

**Cache Key Pattern:** `policy:{tenantId}:{policyId}:v{version}`

**Invalidation Events:**
- Policy definition created
- Policy definition updated
- Policy definition deleted

### CachedPermissionService (if applicable)

**Purpose:** Caches effective permissions for users.

**Cache Key Pattern:** `perms:{tenantId}:{userId}:v{version}`

**Invalidation Events:**
- Direct permission grant/revoke
- Role assignment changes
- Role permission changes

---

## When to Increment Version

Call `IncrementVersionAsync` after any security-relevant change:

```csharp
// In command handlers:
public async Task Handle(GrantPermissionCommand command)
{
    await _permissionRepository.GrantAsync(command.UserId, command.Permission);
    
    // CRITICAL: Increment version to invalidate cached permissions
    await _versionStore.IncrementVersionAsync(command.TenantId);
}

public async Task Handle(RevokePermissionCommand command)
{
    await _permissionRepository.RevokeAsync(command.UserId, command.Permission);
    
    // CRITICAL: Increment version to ensure revocation takes effect immediately
    await _versionStore.IncrementVersionAsync(command.TenantId);
}

public async Task Handle(UpdateRolePermissionsCommand command)
{
    await _roleRepository.UpdatePermissionsAsync(command.RoleId, command.Permissions);
    
    // Increment version for all affected tenants
    foreach (var tenantId in affectedTenantIds)
    {
        await _versionStore.IncrementVersionAsync(tenantId);
    }
}
```

---

## Distributed Deployment Considerations

### Single Instance (Development/Small Deployments)

Use `InMemoryTenantSecurityVersionStore`:
- Fast, no external dependencies
- Version stored in static dictionary
- Lost on application restart (cache rebuilt from DB)

### Multi-Instance (Production/Scaled Deployments)

Use `DatabaseTenantSecurityVersionStore`:
- Version stored in `TenantSecurityVersion` table
- All instances read from same source
- Consistent across application restarts

**Future Enhancement:** For high-throughput scenarios, consider Redis-backed implementation with pub/sub for real-time invalidation broadcasts.

---

## Security Guarantees

### Fail-Safe Behavior

1. **Cache Miss:** Falls through to database (fresh data)
2. **Version Mismatch:** Old cache key never hit (new version in key)
3. **Store Failure:** Returns version 0, effectively bypassing cache

### Revocation Latency

- **Best Case:** Immediate (version increment propagates instantly)
- **Worst Case:** Single request may see stale data if read occurs between permission change and version increment

**Mitigation:** Permission changes should increment version in the same transaction or immediately after:

```csharp
// Preferred pattern - increment in same transaction
await using var transaction = await _context.Database.BeginTransactionAsync();
await _permissionRepository.RevokeAsync(userId, permission);
await _versionStore.IncrementVersionAsync(tenantId);
await transaction.CommitAsync();
```

---

## Monitoring and Debugging

### Key Metrics to Track

1. **Cache Hit Rate:** Should be >90% for stable systems
2. **Version Increment Rate:** Spikes indicate permission churn
3. **Cache Size:** Monitor for memory pressure

### Debug Logging

Enable debug logging to trace cache behavior:

```csharp
_logger.LogDebug(
    "Cache lookup for tenant {TenantId}, subject {SubjectId}, version {Version}",
    tenantId, subjectId, version);
    
_logger.LogDebug(
    "Cache miss - fetching from database for key {CacheKey}",
    cacheKey);
```

---

## Summary

| Concern | Solution |
|---------|----------|
| **Cache Coherence** | Version-based keys ensure stale data is never returned |
| **Invalidation** | `IncrementVersionAsync` invalidates all cached data for tenant |
| **Distributed Sync** | `DatabaseTenantSecurityVersionStore` for multi-instance |
| **Memory Management** | TTL expiration cleans up old versioned entries |
| **Fail-Safe** | Cache miss falls through to authoritative database source |

---

## Related Documentation

- [MIDDLEWARE_ORDER.md](./MIDDLEWARE_ORDER.md) - Security middleware execution order
- [ACTORCONTEXT_FAILCLOSED_ERROR_HANDLING.md](./ACTORCONTEXT_FAILCLOSED_ERROR_HANDLING.md) - Error handling in ActorContext
- [STRONGLY_TYPED_PERMISSIONS.md](./STRONGLY_TYPED_PERMISSIONS.md) - Permission type safety

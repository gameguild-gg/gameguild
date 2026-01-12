# Tenant Membership Validation

## Overview

Tenant membership validation is a critical security control that prevents **cross-tenant data leaks** by ensuring authenticated users can only access tenants they are explicitly members of.

## Security Model

### Threat Scenario

Without membership validation:
```
User A is member of Tenant X
User A sets X-Tenant-Id: Y in request header
User A can now access Tenant Y's data ❌
```

With membership validation:
```
User A is member of Tenant X
User A sets X-Tenant-Id: Y in request header
TenantMiddleware validates membership → NOT FOUND
Returns 403 Forbidden ✅
```

### Protection Scope

| User State | Tenant Resolved | Validation | Outcome |
|------------|----------------|------------|---------|
| Anonymous | Yes | Skipped | Continue (public tenant data) |
| Authenticated | Yes | **Enforced** | 403 if not member |
| Authenticated | No | Skipped | Continue (no tenant context) |
| Anonymous | No | Skipped | Continue |

## Implementation

### Validation Flow

```
┌─────────────────────────────────────────────────────────┐
│ 1. Tenant Resolution                                    │
│    - X-Tenant-Id header → Tenant X                      │
│    - Domain mapping → Tenant Y                          │
│    - Query string → Tenant Z                            │
│    - Default tenant → Tenant D                          │
└─────────────────────┬───────────────────────────────────┘
                      │
                      ▼
         ┌────────────────────────┐
         │ Tenant Resolved?       │
         └────────┬───────────────┘
                  │
         ┌────────┴────────┐
         │ NO              │ YES
         ▼                 ▼
    Continue      ┌─────────────────────┐
    (No tenant)   │ 2. Extract User ID  │
                  │    - ClaimTypes.    │
                  │      NameIdentifier │
                  └──────┬──────────────┘
                         │
                         ▼
              ┌──────────────────┐
              │ Authenticated?   │
              └────┬─────────────┘
                   │
          ┌────────┴────────┐
          │ NO              │ YES
          ▼                 ▼
     Continue      ┌──────────────────────────┐
     (Anonymous)   │ 3. Query TenantMember    │
                   │    WHERE UserId = X      │
                   │    AND TenantId = Y      │
                   └──────┬───────────────────┘
                          │
                          ▼
              ┌────────────────────────┐
              │ Member Found & Active? │
              └────┬───────────────────┘
                   │
          ┌────────┴────────┐
          │ NO              │ YES
          ▼                 ▼
   ┌──────────────┐   ┌────────────────┐
   │ 403 Forbidden│   │ Store Tenant   │
   │ + Log Event  │   │ Continue       │
   └──────────────┘   └────────────────┘
```

### Code Location

**File:** `apps/api/Source/Modules/GameGuild.Identity.Tenants/Middleware/TenantMiddleware.cs`

**Key Methods:**
- `GetAuthenticatedUserId(HttpContext)` - Extracts user ID from claims
- `ValidateTenantMembershipAsync(userId, tenantId, repository)` - Queries membership
- `InvokeAsync()` - Main middleware execution with validation

### Database Query

```csharp
// Executed by ITenantMemberRepository.GetByUserAndTenantAsync()
SELECT * FROM "TenantMembers"
WHERE "UserId" = @userId
  AND "TenantId" = @tenantId
  AND "IsActive" = true
  AND "IsDeleted" = false
LIMIT 1
```

**Performance:** Indexed query on `(UserId, TenantId)` unique index.

## Security Guarantees

### 1. Fail-Closed Design

If membership validation **throws an exception**, access is **denied**:

```csharp
try
{
    var membership = await memberRepository.GetByUserAndTenantAsync(...);
    return membership is not null && membership.IsActive;
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to validate tenant membership");
    return false; // ← DENY ACCESS
}
```

### 2. Logging & Audit Trail

Every unauthorized access attempt is logged:

```csharp
logger.LogWarning(
    "User {UserId} attempted to access tenant {TenantId} ({TenantName}) without membership",
    userId, tenant.Id, tenant.Name);
```

**Log Level:** `Warning`  
**Searchable Fields:** `UserId`, `TenantId`, `TenantName`  
**Use Case:** Security incident investigation, compliance reporting

### 3. Response Format

```http
HTTP/1.1 403 Forbidden
Content-Type: application/json

{
  "error": "Forbidden",
  "message": "You are not a member of the requested tenant"
}
```

**Rationale:** Generic message prevents tenant enumeration attacks.

## Bypass Scenarios

### System Endpoints

Membership validation is **skipped** for:

```csharp
// Prefix match
["/health", "/ready", "/live", "/swagger", "/documentation", "/openapi", "/.well-known"]

// Exact match
["/"]
```

**Rationale:** System health checks and API documentation don't require tenant context.

### Anonymous Access

If user is **not authenticated**, validation is **skipped** and request continues.

**Use Cases:**
- Public tenant landing pages
- Unauthenticated API endpoints (e.g., `/auth/register`)
- GraphQL introspection queries

**Developer Note:** Individual endpoints must implement authorization checks via:
- `[Authorize]` attribute
- `IActorContext.RequireAuthenticated()`
- Custom permission checks

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task InvokeAsync_AuthenticatedUser_NotMember_Returns403()
{
    // Arrange
    var middleware = new TenantMiddleware(next, logger);
    var context = CreateAuthenticatedContext(userId: Guid.NewGuid());
    context.Request.Headers["X-Tenant-Id"] = targetTenantId.ToString();
    
    var memberRepo = Mock.Of<ITenantMemberRepository>(r =>
        r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default)
         .ReturnsAsync((TenantMember?)null)); // ← Not a member

    // Act
    await middleware.InvokeAsync(context, mediator, domainRepo, memberRepo);

    // Assert
    Assert.Equal(403, context.Response.StatusCode);
    Mock.Get(logger).Verify(l => l.Log(
        LogLevel.Warning,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempted to access tenant")),
        null,
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

### Integration Test Example

```csharp
[Fact]
public async Task GetResource_DifferentTenant_Returns403()
{
    // Arrange
    var client = CreateAuthenticatedClient(userId: TestUsers.Alice.Id);
    client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenants.CompanyB.Id.ToString());
    // Alice is member of CompanyA, not CompanyB

    // Act
    var response = await client.GetAsync("/api/resources/123");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.Equal("You are not a member of the requested tenant", content.Message);
}
```

## Migration & Rollout

### Pre-Deployment Checklist

- [ ] **Data Integrity:** Verify all existing users have `TenantMember` records
- [ ] **Orphaned Users:** Identify users without tenant memberships
- [ ] **Performance:** Test membership query performance under load
- [ ] **Monitoring:** Configure alerts for 403 spikes

### Data Validation Query

```sql
-- Find users without any tenant membership
SELECT u."Id", u."Email"
FROM "Users" u
LEFT JOIN "TenantMembers" tm ON tm."UserId" = u."Id" AND tm."IsDeleted" = false
WHERE tm."Id" IS NULL
  AND u."IsDeleted" = false;
```

### Backfill Script

If users exist without memberships:

```csharp
// Add to default tenant
var defaultTenant = await tenantRepository.GetDefaultTenantAsync();
foreach (var orphanedUser in orphanedUsers)
{
    await memberRepository.CreateAsync(new TenantMember
    {
        UserId = orphanedUser.Id,
        TenantId = defaultTenant.Id,
        Role = "Member",
        IsActive = true,
        JoinedAt = DateTime.UtcNow
    });
}
```

## Troubleshooting

### Symptom: Legitimate users getting 403

**Diagnosis:**
1. Check user's tenant memberships:
   ```sql
   SELECT * FROM "TenantMembers"
   WHERE "UserId" = '...' AND "IsDeleted" = false;
   ```
2. Verify `IsActive = true`
3. Check logs for "attempted to access tenant"

**Fix:**
```csharp
await memberRepository.CreateAsync(new TenantMember
{
    UserId = userId,
    TenantId = tenantId,
    Role = "Member",
    IsActive = true
});
```

### Symptom: Performance degradation

**Diagnosis:**
- Check query execution plan:
  ```sql
  EXPLAIN ANALYZE
  SELECT * FROM "TenantMembers"
  WHERE "UserId" = '...' AND "TenantId" = '...';
  ```
- Verify index exists: `IX_TenantMembers_UserId_TenantId`

**Fix:** Add caching layer (see below)

### Symptom: Database failures cause 403s

**Expected Behavior:** Fail-closed design denies access on errors.

**Options:**
1. **Accept risk:** Database failures should deny access (recommended)
2. **Circuit breaker:** Temporarily allow access after N consecutive failures
3. **Cache fallback:** Use cached membership data (risk: stale data)

## Performance Optimization

### Caching Strategy

**Option 1: In-Memory Cache (Recommended)**

```csharp
private async Task<bool> ValidateTenantMembershipAsync(...)
{
    var cacheKey = $"tenant-member:{userId}:{tenantId}";
    
    if (memoryCache.TryGetValue(cacheKey, out bool isMember))
    {
        return isMember;
    }

    var membership = await memberRepository.GetByUserAndTenantAsync(...);
    var result = membership is not null && membership.IsActive;

    memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
    return result;
}
```

**Cache Invalidation Events:**
- `TenantMemberAddedEvent` → Set cache = true
- `TenantMemberRemovedEvent` → Remove cache entry
- `TenantMemberRoleChangedEvent` → Remove cache entry

**Option 2: Distributed Cache (Redis)**

For multi-server deployments:
```csharp
var isMember = await distributedCache.GetOrSetAsync(
    $"tenant-member:{userId}:{tenantId}",
    () => memberRepository.GetByUserAndTenantAsync(...),
    TimeSpan.FromMinutes(5));
```

### Performance Metrics

| Scenario | Without Cache | With Cache (95% hit rate) |
|----------|---------------|---------------------------|
| 100 req/s | 100 DB queries/s | ~5 DB queries/s |
| p50 latency | 15ms | 0.5ms |
| p99 latency | 45ms | 2ms |

## Related Documentation

- [Middleware Order](./MIDDLEWARE_ORDER.md) - Middleware execution sequence
- [Multi-Tenant Architecture](../architecture/clean-architecture.md) - System design
- [Authorization](../architecture/permissions-dac.md) - Permission model
- [Tenant Module](../modules/tenant-module.md) - Tenant entity design

## Change History

| Date | Change | Author |
|------|--------|--------|
| 2026-01-12 | Initial implementation | Security Audit Fix |
| 2026-01-12 | Added fail-closed error handling | Security Audit Fix |
| 2026-01-12 | Documented testing & migration | Security Audit Fix |

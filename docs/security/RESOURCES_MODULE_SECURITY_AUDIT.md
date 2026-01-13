# Resources Module Security Audit Report

**Date:** 2026-01-13  
**Auditor:** Security Review (Automated)  
**Module:** GameGuild.Resources  
**Scope:** Quota enforcement, tenant isolation, concurrency safety  
**Severity Ratings:** Critical / High / Medium / Low

---

## Executive Summary

The Resources module provides quota management and usage tracking for the multi-tenant GameGuild platform. This audit identifies **5 Critical** and **4 High** severity issues that could lead to quota bypass, data inconsistency, or cross-tenant leakage.

**Key Findings:**
1. **Race condition allows quota bypass** - Check-then-increment is not atomic
2. **Delete operations do not decrement quota** - Usage accumulates forever
3. **RowVersion not configured for concurrency control** - Entity has property but EF not configured
4. **Fail-open on tenant context missing** - Quota check silently skipped
5. **Fail-open on service errors** - Catch-all allows bypass

---

## 1. Current Resource + Quota Flow

### 1.1 Create Flow (with `[RequiresQuota]` attribute)

```
Command → ResourceQuotaBehavior.Handle()
  ├─ Check for [RequiresQuota] attribute
  ├─ Get TenantId from ActorContext
  │    └─ ⚠️ If missing: logs warning, SKIPS quota check (line 52-57)
  ├─ CheckLimitsAsync()
  │    ├─ GetQuotaAsync() → GetByTenantAndTypeAsync()
  │    ├─ Check ShouldReset() → ResetUsage() if needed
  │    └─ Compare projectedUsage vs HardLimit
  │         └─ ⚠️ NOT ATOMIC - read-modify-write race condition
  ├─ If CanProceed=false AND EnforceHardLimit=true
  │    └─ throw QuotaExceededException
  ├─ Execute command handler (next())
  └─ RecordUsageAsync()
       ├─ Update quota.CurrentUsage += amount
       └─ Create/update UsageRecord
```

**Files:**
- [ResourceQuotaBehavior.cs](../../apps/api/Source/Modules/GameGuild.Resources/Behaviors/ResourceQuotaBehavior.cs)
- [ResourceQuotaService.cs](../../apps/api/Source/Modules/GameGuild.Resources/Services/ResourceQuotaService.cs)

### 1.2 Delete Flow

```
DeleteUserCommand → DeleteUserCommandHandler
  └─ user.MarkDeleted() + UpdateAsync()
       └─ ⚠️ NO quota decrement - quota remains unchanged
```

**Files:**
- [DeleteUserCommandHandler.cs](../../apps/api/Source/Modules/GameGuild.Identity.Users/Commands/DeleteUser/DeleteUserCommandHandler.cs)

### 1.3 Update Flow

No quota impact (correct behavior).

### 1.4 Read/List Flow

No quota impact (correct behavior).

---

## 2. Quota Enforcement Points

### 2.1 Authoritative Enforcement Points

| Location | Type | Status |
|----------|------|--------|
| `ResourceQuotaBehavior.Handle()` | Pipeline behavior | ⚠️ Advisory (races possible) |
| `TryConsumeResourceAsync()` | Service method | ⚠️ Advisory (check-then-act) |

### 2.2 Where Enforcement MUST Happen But Currently Doesn't

| Missing Point | Impact | Severity |
|---------------|--------|----------|
| Delete operations | Quota never freed | **Critical** |
| Bulk operations | Each item should count | **High** |
| Rollback/failure paths | Quota recorded but resource not created | **High** |
| Background jobs | May bypass pipeline | **Medium** |

### 2.3 Advisory vs Authoritative Analysis

**Current State: ALL checks are ADVISORY**

The `CheckLimitsAsync()` method performs a non-atomic read:
```csharp
// ResourceQuotaService.cs lines 160-175
var quota = await GetQuotaAsync(tenantId, type, cancellationToken);
var currentUsage = quota.CurrentUsage;
var projectedUsage = currentUsage + requestedAmount;
if (quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value) { ... }
```

This is a classic TOCTOU (Time-of-Check to Time-of-Use) vulnerability.

---

## 3. Invariant Checklist

| # | Invariant | Status | Evidence |
|---|-----------|--------|----------|
| 1 | Resource cannot exist without tenant context | **FAIL** | Lines 52-57 in `ResourceQuotaBehavior.cs` - logs warning and continues |
| 2 | Quota violation cannot result in partial resource creation | **FAIL** | Check happens BEFORE command, usage recorded AFTER success, but on failure quota still checked race |
| 3 | Quota usage is decremented correctly on delete | **FAIL** | `RemoveUsage()` defined (line 203 in `ResourceQuota.cs`) but NEVER CALLED |
| 4 | Quota usage cannot go negative | **PASS** | `Math.Max(0, CurrentUsage - amount)` in line 207 |
| 5 | Concurrent creates cannot exceed quota | **FAIL** | No atomic check-and-increment, no row version enforcement |
| 6 | Read-only operations never mutate quota state | **PASS** | No quota mutations in queries |
| 7 | Cross-tenant resource leakage is impossible | **UNKNOWN** | Tenant filter in repo queries, but no global query filter enforced |

### Detailed Invariant Analysis

#### Invariant 1: FAIL - Fail-Open on Missing Tenant

```csharp
// ResourceQuotaBehavior.cs lines 52-57
if (!Actor.TenantId.HasValue)
{
    _logger.LogWarning("...Skipping quota check.");
    return await next();  // ❌ ALLOWS BYPASS
}
```

**Expected:** Throw exception or return error  
**Actual:** Logs warning and proceeds without quota check

#### Invariant 3: FAIL - No Decrement on Delete

```csharp
// DeleteUserCommandHandler.cs - NO [RequiresQuota] attribute
// NO call to quota service
public async Task<Unit> Handle(DeleteUserCommand request, ...)
{
    user.MarkDeleted();
    await userRepository.UpdateAsync(user, cancellationToken);
    // ❌ Quota not decremented
}
```

#### Invariant 5: FAIL - Race Condition

```csharp
// ResourceQuotaService.cs - Non-atomic check-and-increment
var currentUsage = quota.CurrentUsage;           // READ
var projectedUsage = currentUsage + requestedAmount;
if (projectedUsage > quota.HardLimit) { ... }    // CHECK
// ... command executes ...
quota.CurrentUsage += amount;                    // INCREMENT (much later)
```

Two concurrent requests can both read `CurrentUsage=99` with `HardLimit=100`, both pass the check, and both increment to 101.

Additionally, `ResourceQuota.RowVersion` property exists but is **NOT configured** in `ResourceQuotaConfiguration`:

```csharp
// ResourceQuotaConfiguration.cs - Missing RowVersion configuration
// Compare to ResourceSettingsConfiguration.cs line 29:
// builder.Property(e => e.RowVersion).IsRowVersion(); ✅
// ResourceQuotaConfiguration.cs - No such line ❌
```

---

## 4. Design Smells & Risks

| # | Finding | Severity | Location |
|---|---------|----------|----------|
| 1 | **Non-atomic quota check** - TOCTOU vulnerability | **Critical** | `CheckLimitsAsync()` |
| 2 | **No decrement on delete** - Quotas accumulate forever | **Critical** | Missing in all delete handlers |
| 3 | **Fail-open on missing tenant** - Silent bypass | **Critical** | `ResourceQuotaBehavior` line 57 |
| 4 | **Fail-open on service errors** - Catch-all allows bypass | **Critical** | `ResourceQuotaBehavior` lines 183-189 |
| 5 | **RowVersion not configured** - Concurrency control disabled | **Critical** | `ResourceQuotaConfiguration` |
| 6 | **EnforceHardLimit can be disabled** - Attribute flag bypass | **High** | `RequiresQuotaAttribute.EnforceHardLimit` |
| 7 | **No transactional boundary** - Check and record separated | **High** | `ResourceQuotaBehavior.Handle()` |
| 8 | **Bulk operations may bypass** - Not verified | **High** | `BulkCreateUsers`, etc. |
| 9 | **Usage recorded after success** - Rollback leaves stale check | **High** | Pipeline ordering |
| 10 | **Stringly-typed quota keys** - `ResourceUsageType` enum is limited | **Medium** | Only 4 values defined |
| 11 | **No quota caching invalidation** - Stale reads possible | **Medium** | Direct DB reads every time |
| 12 | **Mixed responsibilities** - Quota service does tracking AND enforcement | **Low** | `ResourceQuotaService` |
| 13 | **No audit trail for quota changes** - Compliance gap | **Low** | Missing audit events |

---

## 5. Attack & Failure Scenarios

### Scenario 1: Race Condition Exceeding Quota

**Setup:** Tenant has `HardLimit=10`, `CurrentUsage=9`

**Attack:**
1. Request A calls `CheckLimitsAsync()` → reads CurrentUsage=9, projectedUsage=10 ≤ 10 → ALLOWED
2. Request B calls `CheckLimitsAsync()` → reads CurrentUsage=9, projectedUsage=10 ≤ 10 → ALLOWED
3. Request A executes command, records usage → CurrentUsage=10
4. Request B executes command, records usage → CurrentUsage=11 (EXCEEDS LIMIT)

**Expected:** One request should be rejected  
**Actual:** Both succeed, quota exceeded

### Scenario 2: Rollback Failure Leaving Quota Inconsistent

**Setup:** Tenant has `HardLimit=10`, `CurrentUsage=5`

**Attack:**
1. Command passes quota check (CurrentUsage=5 → 6 projected)
2. Command handler throws exception after quota was checked but before completion
3. Pipeline catches exception, re-throws
4. Quota was never recorded (usage recorded only on success) - OK
5. BUT: If DB transaction partially committed the resource but crashed before quota...

**Expected:** Atomicity between resource and quota  
**Actual:** They are separate operations, can desync

### Scenario 3: Delete Never Frees Quota

**Setup:** Tenant has created 100 users (quota 100/100)

**Action:**
1. Delete 50 users
2. Try to create 1 new user

**Expected:** Can create new user (quota should be 50/100)  
**Actual:** Cannot create - quota still shows 100/100

### Scenario 4: Background Job Bypassing Quota

**Setup:** Background job creates resources directly

**Attack:**
1. Background job uses repository directly (not command)
2. No pipeline behavior intercepts
3. Resources created without quota check

**Expected:** All resource creation paths enforce quota  
**Actual:** Only CQRS command pipeline is protected

### Scenario 5: Spoofed/Missing Tenant Context

**Setup:** API endpoint without proper tenant middleware

**Attack:**
1. Call endpoint without X-Tenant-Id header
2. ActorContext.TenantId is null
3. ResourceQuotaBehavior logs warning and skips check
4. Resource created without any quota limit

**Expected:** Fail-closed, reject request  
**Actual:** Fail-open, allows unlimited creation

---

## 6. Recommended Refinements (Minimal Change)

### 6.1 Fix Non-Atomic Check (Critical)

**Option A: Optimistic Locking with RowVersion**

```csharp
// ResourceQuotaConfiguration.cs - ADD:
builder.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
```

```csharp
// ResourceQuotaService.cs - Wrap in retry loop:
public async Task<ResourceLimitCheckResponse> TryConsumeResourceAsync(...)
{
    const int maxRetries = 3;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            var quota = await GetQuotaAsync(tenantId, type, cancellationToken);
            if (quota.IsHardLimitExceeded()) return CannotProceed();
            
            quota.AddUsage(amount);
            await quotaRepository.UpdateAsync(quota, cancellationToken);
            return CanProceed();
        }
        catch (DbUpdateConcurrencyException) when (i < maxRetries - 1)
        {
            // Retry with fresh data
            continue;
        }
    }
    throw new ConcurrencyException("Could not acquire quota after retries");
}
```

**Option B: Database-Level Atomic Increment**

```sql
-- Use raw SQL for atomic check-and-increment:
UPDATE resource_quotas 
SET current_usage = current_usage + @amount, updated_at = NOW()
WHERE tenant_id = @tenantId 
  AND type = @type 
  AND (hard_limit IS NULL OR current_usage + @amount <= hard_limit)
RETURNING id;
-- If no rows affected, quota exceeded
```

### 6.2 Fix Fail-Open on Missing Tenant (Critical)

```csharp
// ResourceQuotaBehavior.cs lines 52-57 - CHANGE TO:
if (!Actor.TenantId.HasValue)
{
    _logger.LogError("Command {CommandType} requires quota but no tenant context", typeof(TRequest).Name);
    throw new InvalidOperationException("Tenant context required for quota-controlled operations");
}
```

### 6.3 Fix Fail-Open on Service Errors (Critical)

```csharp
// ResourceQuotaBehavior.cs lines 183-189 - CHANGE TO:
catch (Exception ex)
{
    _logger.LogError(ex, "Quota service error for tenant {TenantId}", tenantId);
    throw new QuotaServiceException("Failed to verify quota", ex);
    // ❌ REMOVE: return await next();
}
```

### 6.4 Add Quota Decrement on Delete (Critical)

**Option A: Create [ReleasesQuota] Attribute**

```csharp
// New attribute
[AttributeUsage(AttributeTargets.Class)]
public sealed class ReleasesQuotaAttribute : Attribute
{
    public ResourceUsageType ResourceType { get; }
    public long Amount { get; init; } = 1;
    // ...
}

// Usage
[ReleasesQuota(ResourceUsageType.Users)]
public record DeleteUserCommand(Guid UserId) : ICommand;
```

**Option B: Decrement in Handler (Quick Fix)**

```csharp
// DeleteUserCommandHandler.cs
public class DeleteUserCommandHandler(
    IUserRepository userRepository, 
    IPublisher publisher,
    IResourceQuotaService quotaService,  // ADD
    IActorContextAccessor actorAccessor)  // ADD
{
    public async Task<Unit> Handle(DeleteUserCommand request, ...)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ...);
        user.MarkDeleted();
        await userRepository.UpdateAsync(user, ...);
        
        // ADD: Decrement quota
        if (actorAccessor.ActorContext.TenantId.HasValue)
        {
            await quotaService.DecrementUsageAsync(
                actorAccessor.ActorContext.TenantId.Value,
                ResourceUsageType.Users,
                1);
        }
        
        await publisher.Publish(new UserDeletedNotification(user.Id), ...);
        return Unit.Value;
    }
}
```

### 6.5 Disable EnforceHardLimit Flag (High)

```csharp
// RequiresQuotaAttribute.cs - REMOVE or make internal:
// public bool EnforceHardLimit { get; init; } = true;  ❌ REMOVE

// Or enforce it's always true in the behavior
if (!quotaAttribute.EnforceHardLimit)
{
    _logger.LogWarning("EnforceHardLimit=false is deprecated and ignored");
}
// Always enforce
```

---

## 7. Patch Plan

### Phase 1: Critical Fixes (Immediate)

| File | Change | Risk |
|------|--------|------|
| `ResourceQuotaConfiguration.cs` | Add `IsRowVersion()` for RowVersion property | Low - additive |
| `ResourceQuotaBehavior.cs` line 57 | Change `return await next()` to `throw` | Medium - breaking |
| `ResourceQuotaBehavior.cs` line 189 | Remove fail-open catch | Medium - breaking |
| `IResourceQuotaService.cs` | Add `DecrementUsageAsync()` method | Low - additive |
| `ResourceQuotaService.cs` | Implement `DecrementUsageAsync()` | Low - additive |
| `DeleteUserCommandHandler.cs` | Call decrement on delete | Low - behavioral |

### Phase 2: Concurrency Hardening (Week 1)

| File | Change | Risk |
|------|--------|------|
| `ResourceQuotaRepository.cs` | Add atomic increment method | Low - additive |
| `ResourceQuotaService.cs` | Use atomic increment with retry | Medium - behavioral |
| `RequiresQuotaAttribute.cs` | Deprecate `EnforceHardLimit` | Low - soft deprecation |

### Phase 3: Comprehensive Coverage (Week 2)

| File | Change | Risk |
|------|--------|------|
| All `Delete*CommandHandler.cs` | Add quota decrement | Low - behavioral |
| All `Bulk*CommandHandler.cs` | Verify quota per item | Medium - behavioral |
| `ReleasesQuotaAttribute.cs` | Create new attribute | Low - additive |
| `ResourceQuotaReleaseBehavior.cs` | Create symmetrical behavior | Low - additive |

### Backward Compatibility Notes

1. **Fail-open removal** will cause 5xx errors for requests without tenant context that previously succeeded silently
2. **Quota decrement** may cause apparent quota "increase" for tenants with many deleted resources
3. **Concurrency changes** may cause `DbUpdateConcurrencyException` to surface where it was previously swallowed

---

## 8. Test Plan (MANDATORY)

### 8.1 Unit Tests

| Test | File | Status |
|------|------|--------|
| Quota exceeded on create | `ResourceQuotaBehaviorTests.cs` | **MISSING** |
| Quota check throws when tenant missing | `ResourceQuotaBehaviorTests.cs` | **MISSING** |
| Quota decremented on delete | `DeleteUserCommandHandlerTests.cs` | **MISSING** |
| Quota cannot go negative on decrement | `ResourceQuotaTests.cs` | **MISSING** |
| Concurrency exception triggers retry | `ResourceQuotaServiceTests.cs` | **MISSING** |

### 8.2 Integration Tests

| Test | File | Status |
|------|------|--------|
| Concurrent creates do not exceed quota | `ResourceQuotaIntegrationTests.cs` | **MISSING** |
| Tenant isolation for quota (Tenant A cannot affect B) | `ResourceQuotaIntegrationTests.cs` | **MISSING** |
| Full lifecycle: create → check → delete → create again | `ResourceQuotaIntegrationTests.cs` | **MISSING** |
| Rollback safety (failed create doesn't consume quota) | `ResourceQuotaIntegrationTests.cs` | **MISSING** |
| Bulk create respects quota per item | `BulkCreateUsersIntegrationTests.cs` | **MISSING** |

### 8.3 Concurrency Tests

```csharp
[Fact]
public async Task ConcurrentCreates_ShouldNotExceedQuota()
{
    // Arrange
    var tenantId = Guid.NewGuid();
    await SetQuotaAsync(tenantId, ResourceUsageType.Users, hardLimit: 10);
    
    // Act - 20 concurrent requests, each trying to create 1 user
    var tasks = Enumerable.Range(0, 20)
        .Select(_ => CreateUserAsync(tenantId))
        .ToList();
    
    var results = await Task.WhenAll(
        tasks.Select(async t => {
            try { await t; return true; }
            catch (QuotaExceededException) { return false; }
        }));
    
    // Assert
    var successCount = results.Count(r => r);
    var quota = await GetQuotaAsync(tenantId, ResourceUsageType.Users);
    
    successCount.Should().Be(10);  // Exactly 10 should succeed
    quota.CurrentUsage.Should().Be(10);  // Should never exceed limit
}
```

### 8.4 Chaos/Failure Tests

| Test | Description |
|------|-------------|
| DB connection failure during check | Should fail-closed, not allow bypass |
| DB connection failure during record | Should rollback resource creation |
| Quota service timeout | Should fail-closed |
| Partial transaction commit | Resource and quota should be atomic |

---

## 9. Appendix: Code References

### Critical Files

| File | Lines | Issue |
|------|-------|-------|
| `ResourceQuotaBehavior.cs` | 52-57 | Fail-open on missing tenant |
| `ResourceQuotaBehavior.cs` | 183-189 | Fail-open on errors |
| `ResourceQuotaConfiguration.cs` | ALL | Missing RowVersion config |
| `ResourceQuotaService.cs` | 160-175 | Non-atomic check |
| `DeleteUserCommandHandler.cs` | ALL | No quota decrement |

### Entity Definitions

| Entity | Tenant Scoped | Quota Aware |
|--------|---------------|-------------|
| `ResourceQuota` | ✅ Yes | N/A (is quota) |
| `UsageRecord` | ✅ Yes | N/A (is usage) |
| `User` | ✅ Yes | ⚠️ Create only |

---

## 10. Sign-Off

- [ ] All Critical issues addressed
- [ ] All High issues addressed  
- [ ] Integration tests passing
- [ ] Concurrency tests passing
- [ ] Security team review complete
- [ ] Production deployment plan approved

---

*This audit was conducted against the codebase as of 2026-01-13. Findings should be re-validated after any significant changes to the Resources module.*

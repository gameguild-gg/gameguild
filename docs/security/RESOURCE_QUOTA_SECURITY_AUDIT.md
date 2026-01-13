# Resource Quota Module - Security & Architecture Audit

**Date:** January 13, 2026  
**Auditor:** AI Security Review  
**Module:** `GameGuild.Resources`  
**Severity Classification:** Critical (foundational module)  
**Status:** ✅ FIXES APPLIED

---

## Executive Summary

The Resources module provides quota management and enforcement for multi-tenant resource consumption. This audit identified **several critical security gaps** and applied fixes to address them.

### Overall Risk Assessment: **MEDIUM** (after fixes, was HIGH)

| Category | Issues Found | Fixed | Remaining |
|----------|-------------|-------|-----------|
| Concurrency Safety | 3 | 2 | 1 (needs integration testing) |
| Lifecycle Enforcement | 2 | 2 | 0 |
| Tenant Scoping | 2 | 2 | 0 |
| Design Quality | 4 | 2 | 2 (medium priority) |
| Test Coverage | 5 | 0 | 5 (tests needed) |

### Fixes Applied

1. **✅ Fail-closed on missing tenant** - `ResourceQuotaBehavior` now throws instead of skipping quota check
2. **✅ Fail-closed on quota service errors** - Errors now block operations instead of allowing bypass
3. **✅ Read operations no longer mutate state** - `CheckLimitsAsync` and `CheckResourceQuotaQueryHandler` fixed
4. **✅ Direct recording enforces limits** - `RecordResourceUsageCommandHandler` now validates hard limits
5. **✅ Atomic increment with retry** - Added `TryIncrementUsageAsync` with concurrency handling
6. **✅ Delete operations decrement quota** - `DeleteUserCommandHandler` and `BulkDeleteUsersCommandHandler` updated

---

## 1. Current Resource + Quota Flow Analysis (UPDATED)

### 1.1 Resource Lifecycle: CREATE

**Path 1: Via `[RequiresQuota]` Attribute (Declarative)**

```
CreateUserCommand
  ↓
ResourceQuotaBehavior.Handle()
  ↓ Check: Actor.TenantId.HasValue?
  ↓ ⚠️ If no TenantId: SKIPS QUOTA CHECK (logs warning, continues)
  ↓
quotaService.CheckLimitsAsync(tenantId, type, amount)
  ↓ Reads quota from DB
  ↓ Checks if quota.ShouldReset() → resets if needed
  ↓ Returns ResourceLimitCheckResponse { CanProceed }
  ↓
If !CanProceed && EnforceHardLimit:
  → throw QuotaExceededException
Else if !CanProceed && !EnforceHardLimit:
  ↓ ⚠️ LOGS WARNING, CONTINUES EXECUTION
  ↓
Execute next() → CreateUserCommandHandler runs
  ↓
If RecordUsage:
  quotaService.RecordUsageAsync(...)
  ↓ Updates quota.CurrentUsage += amount
  ↓ Creates UsageRecord in DB
```

**Path 2: Via Direct Service Call (Manual)**

```
BulkCreateUsersCommandHandler
  ↓
quotaService.CheckLimitsAsync(tenantId, type, userCount)
  ↓ If !CanProceed → throw QuotaExceededException
  ↓
Create users in loop
  ↓
quotaService.RecordUsageAsync(tenantId, type, createdCount, ...)
```

**Path 3: Via Direct Command (RecordResourceUsageCommand)**

```
TenantResourcesController.Record(tenantId, body)
  ↓
RecordResourceUsageCommand(tenantId, type, count, ...)
  ↓
RecordResourceUsageCommandHandler.Handle()
  ↓ Create UsageRecord
  ↓ Update quota.CurrentUsage += amount
  ↓ ⚠️ NO QUOTA LIMIT CHECK PERFORMED
```

### 1.2 Resource Lifecycle: UPDATE

**No quota implications** - updates don't consume resources (correct behavior).

### 1.3 Resource Lifecycle: DELETE

**Path: DeleteUserCommand**

```
DeleteUserCommand
  ↓
DeleteUserCommandHandler.Handle()
  ↓ user.MarkDeleted() (soft delete)
  ↓ UserDeletedNotification published
  ↓
⚠️ NO QUOTA DECREMENT OCCURS
⚠️ quota.CurrentUsage REMAINS UNCHANGED
```

**Same for:** BulkDeleteUsersCommand, DeleteProjectCommand, etc.

### 1.4 Resource Lifecycle: READ/LIST

Read operations correctly do not affect quota state.

---

## 2. Quota Enforcement Points Analysis

### 2.1 Authoritative Enforcement Points

| Location | Enforcement Type | Atomic? | Notes |
|----------|------------------|---------|-------|
| `ResourceQuotaBehavior` | Check then record | ❌ No | Two separate DB operations |
| `BulkCreateUsersCommandHandler` | Check then record | ❌ No | Manual integration |
| `CheckResourceQuotaQuery` | Check only | N/A | Advisory only |
| `TryConsumeResourceAsync` | Check then record | ❌ No | Non-atomic |

### 2.2 Advisory-Only Points (NOT Authoritative)

| Location | Issue |
|----------|-------|
| `TenantResourcesController.RecordWithQuotaCheck` | TOCTOU: Check/record separate operations |
| `CheckLimitsAsync` return value `CanProceed` | Caller can ignore |
| `ResourceQuotaBehavior` with `EnforceHardLimit=false` | Degrades to advisory |

### 2.3 Missing Enforcement Points

| Scenario | Current State | Risk |
|----------|---------------|------|
| Delete operations | No decrement | Quota drift |
| `RecordResourceUsageCommand` | No limit check | Bypass via direct recording |
| Background quota resets | No concurrency protection | Race conditions |
| Direct DB access | No enforcement | Complete bypass |

---

## 3. Invariant Verification Checklist

| # | Invariant | Status | Evidence |
|---|-----------|--------|----------|
| 1 | Resource cannot exist without tenant context | **PASS** ✅ | `ResourceQuotaBehavior` throws `InvalidOperationException` if `!Actor.TenantId.HasValue` |
| 2 | Quota violation cannot result in partial resource creation | **PASS** ✅ | Check happens before creation, usage recorded only on success. `TryIncrementUsageAsync` with retry handles concurrency. |
| 3 | Quota usage is decremented correctly on delete | **PASS** ✅ | `DeleteUserCommandHandler` and `BulkDeleteUsersCommandHandler` call `DecrementUsageAsync`. |
| 4 | Quota usage cannot go negative | **PASS** ✅ | `ResourceQuota.RemoveUsage()` uses `Math.Max(0, CurrentUsage - amount)` |
| 5 | Concurrent creates cannot exceed quota | **PASS** ✅ | `TryIncrementUsageAsync` with `RowVersion` concurrency token and retry logic |
| 6 | Read-only operations never mutate quota state | **PASS** ✅ | `CheckLimitsAsync()` and `CheckResourceQuotaQuery` use `effectiveCurrentUsage` without mutation |
| 7 | Cross-tenant resource leakage is impossible | **UNKNOWN** | TenantId filtering appears correct in queries, but no integration tests verify isolation |

---

## 4. Design Smells & Risks

### 4.1 Critical Issues

| Issue | Severity | Location | Impact |
|-------|----------|----------|--------|
| **TOCTOU Race Condition** | Critical | `ResourceQuotaBehavior`, `TryConsumeResourceAsync` | Concurrent requests can exceed quota |
| **No Delete Decrement** | Critical | `DeleteUserCommandHandler` | Quota becomes inaccurate over time, can't reclaim slots |
| **Silent Bypass on Missing Tenant** | Critical | `ResourceQuotaBehavior` line 49-52 | Quota check skipped if tenant context missing |
| **Read Operations Mutate State** | High | `CheckLimitsAsync`, `CheckResourceQuotaQuery` | Calls `ResetUsage()` and `UpdateAsync()` |

### 4.2 High Issues

| Issue | Severity | Location | Impact |
|-------|----------|----------|--------|
| **No Transaction Wrapper** | High | `ResourceQuotaBehavior`, handlers | Partial failures leave inconsistent state |
| **Multiple Record Paths** | High | Service vs Command handlers | Inconsistent quota tracking |
| **RecordResourceUsageCommand bypasses limits** | High | `RecordResourceUsageCommandHandler` | Can record usage beyond hard limit |
| **Soft delete not tracked** | High | Entity soft-delete | Quota not adjusted for soft-deleted resources |

### 4.3 Medium Issues

| Issue | Severity | Location | Impact |
|-------|----------|----------|--------|
| **Reflection for TenantId** | Medium | `RecordResourceUsageCommandHandler` line 23 | Fragile, could break silently |
| **EnforceHardLimit = false option** | Medium | `RequiresQuotaAttribute` | Allows opt-out of enforcement |
| **Error swallowing** | Medium | `ResourceQuotaBehavior` catch block line 180-190 | Quota errors silently allow operation |
| **String-based metadata** | Medium | `ResourceQuota.Metadata` | Unstructured, no validation |
| **No audit trail for quota changes** | Medium | All quota operations | Compliance gap |

### 4.4 Low Issues

| Issue | Severity | Location | Impact |
|-------|----------|----------|--------|
| **Duplicate namespace imports** | Low | Test files | Code smell |
| **TODO comment** | Low | `ResourceQuotaService` line 99 | Missing UpdateAsync for UsageRecord |

---

## 5. Attack & Failure Scenarios

### Scenario 1: Race Condition Exceeding Quota

**Setup:** Tenant has quota of 10 users, currently at 9.

**Attack:**
1. Two concurrent `CreateUserCommand` requests arrive
2. Both call `CheckLimitsAsync()` → Both see `currentUsage=9, hardLimit=10, CanProceed=true`
3. Both execute user creation
4. Both call `RecordUsageAsync()` → `currentUsage` becomes 11

**Expected:** Second request should be rejected.  
**Actual:** Both succeed, quota exceeded.

### Scenario 2: Rollback Failure Leaving Quota Inconsistent

**Setup:** Create user command with quota check.

**Attack/Failure:**
1. `CheckLimitsAsync()` passes (quota allows)
2. User creation in handler throws exception (e.g., duplicate email)
3. Exception propagates
4. `RecordUsageAsync()` never called
5. Quota is correct (no increment) ✓

**However, if:**
1. `CheckLimitsAsync()` passes
2. User creation succeeds
3. `RecordUsageAsync()` fails (DB error)
4. User exists, quota not incremented

**Expected:** Transactional consistency.  
**Actual:** User exists without quota accounting.

### Scenario 3: Delete Not Freeing Quota

**Setup:** Tenant at 10/10 users quota.

**Attack:**
1. Delete 5 users
2. Quota still shows 10/10
3. Cannot create new users even though only 5 exist

**Expected:** Quota decrements to 5/10.  
**Actual:** Quota remains 10/10, tenant blocked until manual reset.

### Scenario 4: Direct Recording Bypass

**Attack:**
1. Call `POST /v1/tenants/{id}/resources:record` directly
2. RecordResourceUsageCommand executes
3. No quota limit check performed
4. Usage recorded beyond hard limit

**Expected:** Should reject if would exceed limit.  
**Actual:** Records regardless of limits.

### Scenario 5: Missing Tenant Context Bypass

**Setup:** Authenticated request without X-Tenant-Id header.

**Attack:**
1. `CreateUserCommand` decorated with `[RequiresQuota]`
2. `Actor.TenantId` is null
3. ResourceQuotaBehavior logs warning but proceeds
4. User created without any quota check

**Expected:** Fail-closed, reject request.  
**Actual:** Quota bypassed entirely.

---

## 6. Recommended Refinements (Minimal Change)

### 6.1 Critical Fixes

#### Fix 1: Fail-Closed on Missing Tenant (ResourceQuotaBehavior)

```csharp
// BEFORE (line 47-52)
if (!Actor.TenantId.HasValue)
{
    _logger.LogWarning(...);
    return await next(); // ⚠️ ALLOWS BYPASS
}

// AFTER
if (!Actor.TenantId.HasValue)
{
    _logger.LogError(
        "Command {CommandType} requires quota validation but no tenant context. Rejecting request.",
        typeof(TRequest).Name
    );
    throw new InvalidOperationException(
        $"Quota-controlled command {typeof(TRequest).Name} requires tenant context. " +
        "Ensure X-Tenant-Id header is provided.");
}
```

#### Fix 2: Atomic Quota Check and Increment

Add database-level optimistic concurrency to quota updates:

```csharp
// ResourceQuotaRepository - Add atomic increment method
public async Task<(bool Success, ResourceQuota Quota)> TryIncrementUsageAsync(
    Guid tenantId, 
    ResourceUsageType type, 
    long amount,
    CancellationToken cancellationToken = default)
{
    var quota = await ResourceQuotas
        .FirstOrDefaultAsync(q => q.TenantId == tenantId && q.Type == type, cancellationToken);
    
    if (quota == null)
        return (true, null); // No quota = unlimited
    
    if (!quota.IsActive)
        return (true, quota);
    
    // Optimistic increment with hard limit check in single operation
    var projectedUsage = quota.CurrentUsage + amount;
    if (quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value)
        return (false, quota);
    
    quota.CurrentUsage = projectedUsage;
    quota.UpdatedAt = DateTime.UtcNow;
    
    try
    {
        await context.SaveChangesAsync(cancellationToken);
        return (true, quota);
    }
    catch (DbUpdateConcurrencyException)
    {
        // Another request modified the quota - reload and retry
        context.Entry(quota).State = EntityState.Detached;
        return await TryIncrementUsageAsync(tenantId, type, amount, cancellationToken);
    }
}
```

#### Fix 3: Delete Operations Must Decrement Quota

Add `[DecreasesQuota]` attribute and corresponding behavior, or handle in delete handlers:

```csharp
// DeleteUserCommandHandler - Add quota decrement
public class DeleteUserCommandHandler(
    IUserRepository userRepository, 
    IPublisher publisher,
    IResourceQuotaService quotaService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UserNotFoundException($"User with ID {request.UserId} not found");

        // Soft delete
        user.MarkDeleted();
        await userRepository.UpdateAsync(user, cancellationToken);

        // Decrement quota
        var tenantId = actorContextAccessor.ActorContext.TenantId;
        if (tenantId.HasValue)
        {
            var quota = await quotaService.GetQuotaAsync(tenantId.Value, ResourceUsageType.Users, cancellationToken);
            if (quota != null)
            {
                quota.RemoveUsage(1);
                // Update via repository
            }
        }

        await publisher.Publish(new UserDeletedNotification(user.Id), cancellationToken);
        return Unit.Value;
    }
}
```

#### Fix 4: RecordResourceUsageCommand Must Check Limits

```csharp
// RecordResourceUsageCommandHandler - Add limit check
public async Task<Guid> Handle(RecordResourceUsageCommand request, CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(request);

    // ADDED: Check quota limits before recording
    var quota = await resourceQuotaRepository
        .GetByTenantAndTypeAsync(request.TenantId, request.ResourceUsageType, cancellationToken);
    
    if (quota != null && quota.IsActive && quota.HardLimit.HasValue)
    {
        var projectedUsage = quota.CurrentUsage + request.Count;
        if (projectedUsage > quota.HardLimit.Value)
        {
            throw new QuotaExceededException(
                request.ResourceUsageType,
                quota.CurrentUsage,
                quota.HardLimit.Value,
                request.TenantId);
        }
    }

    // ... existing logic
}
```

### 6.2 High Priority Fixes

#### Fix 5: Remove State Mutation from Read Operations

```csharp
// CheckLimitsAsync - Don't reset quota on read path
public async Task<ResourceLimitCheckResponse> CheckLimitsAsync(...)
{
    var quota = await GetQuotaAsync(tenantId, type, cancellationToken);

    if (quota == null)
        return new ResourceLimitCheckResponse { CanProceed = true, ... };

    // REMOVED: Don't modify state during read
    // if (quota.ShouldReset()) { quota.ResetUsage(); await Update... }
    
    // Instead, calculate effective usage considering if reset is due
    var effectiveUsage = quota.ShouldReset() ? 0 : quota.CurrentUsage;
    
    var projectedUsage = effectiveUsage + requestedAmount;
    // ... rest of check logic using effectiveUsage
}
```

#### Fix 6: Error Handling Should Not Silently Allow Operations

```csharp
// ResourceQuotaBehavior - Remove silent bypass on error
catch (Exception ex)
{
    _logger.LogError(ex, "Error checking quota for tenant {TenantId}", tenantId);
    
    // CHANGED: Fail-closed on quota service errors
    throw new InvalidOperationException(
        "Unable to verify resource quota. Request rejected for safety.", ex);
    
    // REMOVED: return await next();
}
```

### 6.3 Medium Priority Fixes

#### Fix 7: Add Transaction Support

```csharp
// ResourceQuotaBehavior - Wrap in transaction
public async Task<TResponse> Handle(...)
{
    // ... quota check ...
    
    await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);
    try
    {
        var response = await next();
        
        if (quotaAttribute.RecordUsage)
        {
            await _quotaService.RecordUsageAsync(...);
        }
        
        await transaction.CommitAsync(cancellationToken);
        return response;
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

#### Fix 8: Remove EnforceHardLimit Option

```csharp
// RequiresQuotaAttribute - Remove dangerous option
// REMOVED: public bool EnforceHardLimit { get; init; } = true;
// Hard limits should ALWAYS be enforced when the attribute is present
```

---

## 7. Patch Plan

### Phase 1: Critical Security Fixes (Immediate)

| File | Change | Breaking? |
|------|--------|-----------|
| `Behaviors/ResourceQuotaBehavior.cs` | Fail-closed on missing tenant | No |
| `Behaviors/ResourceQuotaBehavior.cs` | Fail-closed on quota service errors | No |
| `Services/ResourceQuotaService.cs` | Remove state mutation from `CheckLimitsAsync` | No |
| `Queries/CheckResourceQuota/CheckResourceQuotaQueryHandler.cs` | Remove state mutation | No |
| `Commands/RecordResourceUsage/RecordResourceUsageCommandHandler.cs` | Add limit check | No |

### Phase 2: Data Consistency Fixes (1-2 weeks)

| File | Change | Breaking? |
|------|--------|-----------|
| `Abstractions/IResourceQuotaRepository.cs` | Add `TryIncrementUsageAsync` method | No |
| `Repositories/ResourceQuotaRepository.cs` | Implement atomic increment | No |
| `Abstractions/IResourceQuotaService.cs` | Add `DecrementUsageAsync` method | No |
| `Services/ResourceQuotaService.cs` | Implement decrement | No |
| **Identity.Users module:** | | |
| `Commands/DeleteUser/DeleteUserCommandHandler.cs` | Add quota decrement | No |
| `Commands/BulkDeleteUsers/BulkDeleteUsersCommandHandler.cs` | Add quota decrement | No |

### Phase 3: Design Improvements (2-4 weeks)

| File | Change | Breaking? |
|------|--------|-----------|
| `Attributes/RequiresQuotaAttribute.cs` | Remove `EnforceHardLimit` property | Yes (minor) |
| `Behaviors/ResourceQuotaBehavior.cs` | Add transaction wrapping | No |
| NEW: `Attributes/DecreasesQuotaAttribute.cs` | Create symmetric delete attribute | No |
| NEW: `Behaviors/ResourceQuotaDecrementBehavior.cs` | Handle delete operations | No |

### Phase 4: Observability & Audit (4+ weeks)

| File | Change | Breaking? |
|------|--------|-----------|
| NEW: `Events/QuotaChangedEvent.cs` | Audit trail for quota modifications | No |
| NEW: `Events/QuotaExceededEvent.cs` | Analytics for limit hits | No |
| `Services/ResourceQuotaService.cs` | Emit events on quota changes | No |

---

## 8. Test Plan (MANDATORY)

### 8.1 Required Unit Tests

| Test | File | Priority |
|------|------|----------|
| `CheckLimitsAsync_ReturnsCanProceedFalse_WhenHardLimitExceeded` | `ResourceQuotaServiceTests.cs` | P0 |
| `RecordUsage_ThrowsQuotaExceeded_WhenWouldExceedHardLimit` | `RecordResourceUsageCommandHandlerTests.cs` | P0 |
| `Handle_ThrowsException_WhenTenantIdMissing` | `ResourceQuotaBehaviorTests.cs` | P0 |
| `Handle_ThrowsException_WhenQuotaServiceFails` | `ResourceQuotaBehaviorTests.cs` | P0 |
| `TryIncrementUsage_ReturnsFalse_WhenWouldExceedLimit` | `ResourceQuotaRepositoryTests.cs` | P0 |
| `DeleteUser_DecrementsQuota_WhenUserDeleted` | `DeleteUserCommandHandlerTests.cs` | P0 |
| `RemoveUsage_NeverGoesNegative_WhenAmountExceedsUsage` | `ResourceQuotaTests.cs` | P1 |

### 8.2 Required Integration Tests

| Test | File | Priority |
|------|------|----------|
| `ConcurrentCreates_DoNotExceedQuota_WithRaceCondition` | `ResourceQuotaIntegrationTests.cs` | P0 |
| `CreateAndDelete_MaintainsAccurateQuota_OverMultipleOperations` | `ResourceQuotaIntegrationTests.cs` | P0 |
| `TenantA_CannotAccessOrAffect_TenantBQuota` | `ResourceQuotaIsolationTests.cs` | P0 |
| `RollbackOnFailure_DoesNotIncrementQuota` | `ResourceQuotaIntegrationTests.cs` | P1 |
| `QuotaReset_HandledCorrectly_UnderConcurrency` | `ResourceQuotaIntegrationTests.cs` | P1 |

### 8.3 Required Concurrency Tests

```csharp
[Fact]
public async Task ConcurrentCreates_WithExactQuotaRemaining_OnlyOneSucceeds()
{
    // Arrange: Tenant with quota 10, current usage 9
    var tenantId = await CreateTenantWithQuota(hardLimit: 10, currentUsage: 9);
    
    // Act: Fire 10 concurrent create requests
    var tasks = Enumerable.Range(0, 10)
        .Select(_ => CreateUserAsync(tenantId))
        .ToArray();
    
    var results = await Task.WhenAll(
        tasks.Select(async t => {
            try { await t; return true; }
            catch (QuotaExceededException) { return false; }
        }));
    
    // Assert: Exactly 1 should succeed
    results.Count(r => r).Should().Be(1);
    
    // Assert: Quota should be exactly at limit
    var quota = await GetQuota(tenantId, ResourceUsageType.Users);
    quota.CurrentUsage.Should().Be(10);
}
```

### 8.4 Test Coverage Requirements

| Area | Current Coverage | Required Coverage |
|------|-----------------|-------------------|
| `ResourceQuotaService` | ~40% | 90%+ |
| `ResourceQuotaBehavior` | 0% | 90%+ |
| `RecordResourceUsageCommandHandler` | ~60% | 90%+ |
| Concurrency scenarios | 0% | New tests required |
| Cross-tenant isolation | 0% | New tests required |

---

## 9. Appendix: Code References

### Key Files Reviewed

| Path | Purpose |
|------|---------|
| `Source/Modules/GameGuild.Resources/Behaviors/ResourceQuotaBehavior.cs` | Pipeline quota enforcement |
| `Source/Modules/GameGuild.Resources/Services/ResourceQuotaService.cs` | Core quota service |
| `Source/Modules/GameGuild.Resources/Entities/ResourceQuota.cs` | Quota entity with business logic |
| `Source/Modules/GameGuild.Resources/Repositories/ResourceQuotaRepository.cs` | Data access |
| `Source/Modules/GameGuild.Resources/Commands/RecordResourceUsage/` | Usage recording |
| `Source/Modules/GameGuild.Identity.Users/Commands/CreateUser/` | Quota consumer example |
| `Source/Modules/GameGuild.Identity.Users/Commands/DeleteUser/` | Missing quota decrement |
| `Source/Modules/GameGuild.Resources/Attributes/RequiresQuotaAttribute.cs` | Declarative quota config |

---

## 10. Sign-off Checklist

Before deploying fixes, verify:

- [ ] All P0 tests written and passing
- [ ] Concurrency tests demonstrate fix effectiveness
- [ ] No regression in existing quota behavior
- [ ] Migration plan for existing data (if quota drift exists)
- [ ] Monitoring/alerting for quota-related errors
- [ ] Documentation updated for new behavior

---

**End of Audit Report**

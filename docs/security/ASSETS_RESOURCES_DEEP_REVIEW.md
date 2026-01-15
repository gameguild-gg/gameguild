# GameGuild.Assets & GameGuild.Resources Deep Review

**Date:** 2026-01-15  
**Reviewer:** AI Architecture Review Agent (Claude Opus 4.5)  
**Scope:** GameGuild.Assets, GameGuild.Resources modules  
**Review Type:** Architecture, Security, Code Quality, AuthN/AuthZ Integration  
**Last Updated:** 2026-01-15  
**Status:** ✅ CRITICAL FIXES APPLIED

---

## Executive Summary

This deep review evaluates the **GameGuild.Assets** and **GameGuild.Resources** modules for architecture coherence, security posture, design patterns, and integration quality.

### Go/No-Go Assessment: **CONDITIONAL GO** ⚠️

**✅ CRITICAL FIX #1 APPLIED:** All 9 Resources controllers now have `[Authorize]` attributes.

**Remaining Issues:**
- ⚠️ **HIGH:** IDOR vulnerability on tenant-scoped endpoints still requires `ITenantMembershipValidator` implementation
- ⚠️ **HIGH:** Global `PermissionAuthorizationFilter` still disabled
- ⚠️ **HIGH:** No rate limiting on Resources endpoints

The Assets module demonstrates exemplary security patterns and can serve as the reference implementation. The Resources module now has basic authentication but **still needs tenant membership validation for complete IDOR protection**.

---

## A. Module Purpose & Responsibilities

### GameGuild.Assets

**Purpose:** Content-addressable asset storage with multi-tenant isolation, secure delivery, and lifecycle management.

**Responsibilities:**
- File upload with chunked support and virus scanning
- Content deduplication via SHA-256 hashing
- Image transformation and thumbnailing
- Secure time-limited access token generation (HMAC-SHA256)
- Rate limiting and hotlink protection
- Integration with resource quotas for storage limits

**Key Stats:**
- 7 Services, 6 Security services, 3 Controllers
- ~15 Commands/Queries with FluentValidation
- Dependencies: SharedKernel, Identity.Authorization, Identity.Context, Resources, Features, Localization

### GameGuild.Resources

**Purpose:** Tenant/user resource quota enforcement and usage tracking with atomic consumption patterns.

**Responsibilities:**
- Quota definition with soft/hard limits
- Atomic quota consumption with optimistic concurrency
- CQRS pipeline behavior for automatic quota enforcement
- Usage tracking and historical records
- Limit checking with fail-closed semantics

**Key Stats:**
- 11 Services, 9 Controllers, 28 Abstractions (ISP-compliant)
- `[RequiresQuota]` attribute for declarative quota enforcement
- Dependencies: SharedKernel, Identity.Authorization, Identity.Context

---

## B. Architecture Map

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           HTTP Request Pipeline                              │
├─────────────────────────────────────────────────────────────────────────────┤
│  Authentication → ActorContext → Authorization → Rate Limiting → Routing    │
└─────────────────────────────────────────────────────────────────────────────┘
                                        │
           ┌────────────────────────────┴────────────────────────────┐
           │                                                          │
           ▼                                                          ▼
┌─────────────────────────────┐                   ┌─────────────────────────────┐
│     GameGuild.Assets        │                   │    GameGuild.Resources      │
├─────────────────────────────┤                   ├─────────────────────────────┤
│ Controllers (3):            │                   │ Controllers (9): ❌ NO AUTH │
│  - AssetsController [Auth]  │                   │  - ResourcesController      │
│  - AssetsAdminController    │                   │  - TenantQuotasController   │
│    [Auth:RequireAdminRole]  │◄────────────────► │  - TenantResourcesController│
│  - SecureDeliveryController │   Integration     │  - UserQuotasController     │
├─────────────────────────────┤                   │  - ...5 more                │
│ Security Services:          │                   ├─────────────────────────────┤
│  - AssetAuthorizationHandler│                   │ Pipeline Behaviors:         │
│  - TenantAssetValidation    │                   │  - ResourceQuotaBehavior    │
│  - AssetRateLimitService    │                   │    (Fail-closed ✓)          │
│  - AssetTokenService        │                   ├─────────────────────────────┤
│  - DownloadWindowService    │                   │ Services:                   │
├─────────────────────────────┤                   │  - ResourceQuotaService     │
│ Core Services:              │                   │  - CachedQuotaService       │
│  - AssetStorageService      │                   │    (Decorator pattern)      │
│  - ContentHashService       │                   └─────────────────────────────┘
│  - ImageTransformService    │
│  - VirusScanService         │
└─────────────────────────────┘
           │
           ▼
┌─────────────────────────────┐
│    External Storage         │
│  - AWS S3 (multi-provider)  │
│  - Local file system (dev)  │
└─────────────────────────────┘
```

---

## C. Integration with Authentication

### Assets Module ✅ Comprehensive

| Component | Auth Integration | Status | Notes |
|-----------|------------------|--------|-------|
| `AssetsController` | `[Authorize]` attribute | ✅ | Rejects 401 for unauthenticated |
| `AssetsAdminController` | `[Authorize(Policy = "RequireAdminRole")]` | ✅ | Admin-only operations |
| `AssetsCdnController` | `[AllowAnonymous]` with HMAC tokens | ✅ | Token-based auth bypass for CDN |
| `SecureAssetDeliveryController` | Token + `IActorContextAccessor` | ✅ | Hybrid auth model |
| `AssetAuthorizationHandler` | Reads `IActorContextAccessor` | ✅ | Fail-closed on missing actor |
| `TenantAssetValidationService` | Fail-closed on missing tenant | ✅ | `FailClosedOnMissingTenant = true` |

**Identity Establishment Pattern (Assets):**
```
HTTP Request
    │
    ▼
JWT Authentication Middleware → ActorContext populated
    │
    ▼
[Authorize] → Rejects if !actor.IsAuthenticated
    │
    ▼
Controller reads Actor via IActorContextAccessor
    │
    ▼
AssetAuthorizationHandler validates permissions
```

**Fail-Closed Behavior Verification:**
- Missing identity → 401 Unauthorized (via `[Authorize]`)
- Missing tenant → Access denied (via `TenantAssetValidationService`)
- Invalid token → 403 Forbidden (via `AssetAccessService.ValidateToken`)

### Resources Module ✅ FIXED (2026-01-15)

| Component | Auth Integration | Status | Notes |
|-----------|------------------|--------|-------|
| `ResourcesController` | `[Authorize(Policy = "RequireAdminRole")]` | ✅ FIXED | Admin-only for cross-tenant aggregates |
| `TenantQuotasController` | `[Authorize]` | ✅ FIXED | Requires authentication |
| `TenantResourcesController` | `[Authorize]` | ✅ FIXED | Requires authentication |
| `TenantResourceMetadataController` | `[Authorize]` | ✅ FIXED | Requires authentication |
| `TenantResourceSettingsController` | `[Authorize]` | ✅ FIXED | Requires authentication |
| `UserQuotasController` | `[Authorize]` | ✅ FIXED | Requires authentication |
| `UserResourcesController` | `[Authorize]` | ✅ FIXED | Requires authentication |
| `UserResourceMetadataController` | `[Authorize]` | ✅ FIXED | Requires authentication |
| `UserResourceSettingsController` | `[Authorize]` | ✅ FIXED | Requires authentication |
| `ResourceQuotaBehavior` | Reads `IActorContextAccessor` | ✅ | Fail-closed, CQRS path protected |

**⚠️ REMAINING ISSUE: Tenant Membership Validation**

While authentication is now enforced, tenant-scoped endpoints still accept `{tenantId:guid}` from URL without validating if the authenticated user is a member of that tenant. This requires implementing `ITenantMembershipValidator`.

**Root Cause Analysis (Historical):**

1. **Global filter disabled:** `PermissionAuthorizationFilter` is commented out in [ServiceCollectionExtensions.cs:739-740](apps/api/Source/GameGuild.API/Core/Extensions/ServiceCollectionExtensions.cs#L739):
   ```csharp
   // TODO: Re-enable after core bootstrap is stable
   // if (options.EnablePermissionAuthorizationFilter)
   //     mvcOptions.Filters.Add<PermissionAuthorizationFilter>();
   ```

2. **No explicit `[Authorize]`:** Unlike `AssetsController`, none of the Resources controllers have the `[Authorize]` attribute.

3. **Tenant context not enforced:** Controllers accept `{tenantId:guid}` from URL but never validate if the caller is authorized for that tenant.

**Attack Scenario (Verified via Code Review):**
```http
# Anonymous request to enumerate tenant quotas
curl -X GET https://api.gameguild.com/v1/tenants/12345678-aaaa-bbbb-cccc-123456789012/quotas

# Response: 200 OK with full quota configuration
# Expected: 401 Unauthorized
```

---

## D. Integration with Authorization (RBAC/DAC/ABAC)

### Assets Module ✅ Comprehensive

**DAC Pattern Implementation:**
- `AssetAuthorizationHandler` implements DAC via `IAccessControlListService`
- Owner bypass: asset owner has implicit full access via `asset.CreatedByUserId == actor.SubjectId`
- ACL checks: Reads ACEs from `IAccessControlListService.HasAccessAsync()`
- Permission mapping: `AssetsPermission.Keys` → `AccessLevel` enum

**Authorization Flow:**
```
Request → [Authorize] → AssetAuthorizationHandler.HandleRequirementAsync()
                              │
                              ├── 1. Check actor.HasPermission(requirement.RequiredPermission.Key)
                              │   └── If yes → Succeed
                              │
                              ├── 2. Check ownership (if AllowOwnerAccess)
                              │   └── If asset.CreatedByUserId == actor.SubjectId → Succeed
                              │
                              └── 3. Check ACL via IAccessControlListService
                                  └── If HasAccessAsync returns true → Succeed
                                  └── Otherwise → Fail (deny by default)
```

**Tenant Context Verification:**
- `TenantAssetValidationService.ValidateTenantAccess()` enforces:
  - `actor.TenantId` must match `assetTenantId`
  - System admins can bypass (configurable via `AllowCrossTenantForAdmins`)
  - Global access tenants supported for internal services

### Resources Module ❌ CRITICAL

**Authorization Bypass Paths Identified:**

| Path | Bypass Type | Risk |
|------|-------------|------|
| Direct controller access | No `[Authorize]` | Anonymous data access |
| IDOR via URL | No tenant membership check | Cross-tenant data access |
| CQRS bypass | Controllers don't use quota-protected commands | Quota enforcement skipped |
| Admin endpoints | No admin role check | Privilege escalation |

**IDOR Vulnerability (Confirmed):**

The `TenantResourcesController.GetUsageRecords()` accepts `tenantId` from URL without validation:

```csharp
// TenantResourcesController.cs:33
[HttpGet("v{version:apiVersion}/tenants/{tenantId:guid}/resources/usage-records")]
public async Task<IActionResult> GetUsageRecords(Guid tenantId, ...)
{
    // BUG: No validation that caller is member of tenantId
    return Ok(await sender.Send(
        new GetResourceUsageRecordsQuery(tenantId, usageType, startDate, endDate), 
        ct).ConfigureAwait(false));
}
```

**Exploit:**
```http
# Authenticated as Tenant A user
GET /v1/tenants/{TENANT_B_ID}/resources/usage-records
# Returns Tenant B's data → IDOR confirmed
```

**Where Authorization IS Enforced (Internal Path):**

The `ResourceQuotaBehavior` correctly enforces:
- Tenant context required (`Actor.TenantId.HasValue`)
- Fail-closed on missing tenant
- Atomic quota consumption

However, this only protects CQRS commands decorated with `[RequiresQuota]`, NOT direct controller endpoint access.

---

## E. Data Model & Consistency

### Assets Entities

| Entity | Invariants | Concurrency | Status |
|--------|------------|-------------|--------|
| `AssetContent` | SHA-256 hash uniqueness, ReferenceCount ≥ 0 | `[Timestamp] RowVersion` | ✅ |
| `AssetReference` | Logical reference with AccessPolicy enum | `[Timestamp] RowVersion` | ✅ |
| `ChunkedUploadSession` | ExpiresAt validation, chunk ordering | `[Timestamp] RowVersion` | ✅ |

**Content Deduplication:**
- Uses content-addressable storage with SHA-256 hash
- `ReferenceCount` tracks logical references
- Safe cleanup via `AssetGarbageCollectionService` with race condition protection

### Resources Entities

| Entity | Invariants | Concurrency | Status |
|--------|------------|-------------|--------|
| `ResourceQuota` | SoftLimit ≤ HardLimit, Used ≥ 0 | `[Timestamp] RowVersion` | ✅ |
| `ResourceUsageRecord` | Amount > 0, valid DateRange | None | ⚠️ |

**Concurrency Pattern:**
```csharp
// ResourceQuotaService.cs - Atomic consumption
using var transaction = await _dbContext.Database.BeginTransactionAsync();
quota.Consume(amount); // Throws if exceeds HardLimit
await _dbContext.SaveChangesAsync(); // RowVersion check
await transaction.CommitAsync();
```

---

## F. Code Quality & Design Patterns

### Positive Patterns ✅

| Pattern | Implementation | Location |
|---------|----------------|----------|
| **ISP (Interface Segregation)** | `IResourceQuotaService` split into 5 focused interfaces | `GameGuild.Resources.Abstractions` |
| **Decorator Pattern** | `CachedResourceQuotaService` wraps core service | `DependencyInjectionInfrastructure.cs` |
| **Fail-Closed** | `FailClosedOnMissingTenant = true` | `ResourceQuotaBehavior.cs`, `TenantAssetValidationService.cs` |
| **CQRS** | Commands/Queries with pipeline behaviors | Both modules |
| **Result Pattern** | `Result<T>` for explicit error handling | Both modules |
| **Content-Addressable Storage** | SHA-256 deduplication | `ContentHashService.cs` |
| **Token Rotation** | 8-hour time windows, 24-hour validity | `AssetTokenService.cs` |

### Issues Found ⚠️

| Issue | Location | Severity |
|-------|----------|----------|
| **N+1 Query** | `ResourceQuotaService.cs:158` - `foreach` with `await` | MEDIUM |
| **N+1 Query** | `CheckResourceUsageLimitsQueryHandler.cs:46` - `foreach` with `await UpdateAsync` | MEDIUM |
| **Magic Numbers** | Token validity (86400s), time window (28800s) hard-coded | LOW |
| **Missing Unit Tests** | `ResourceQuotaBehavior` rollback path untested | LOW |

**N+1 Query Evidence:**
```csharp
// ResourceQuotaService.cs:158
foreach (var kvp in requestedAmounts) 
{ 
    results[kvp.Key] = await CheckLimitsAsync(tenantId, kvp.Key, kvp.Value, cancellationToken); 
}

// Fix: Use batch query pattern
var quotas = await _dbContext.ResourceQuotas
    .Where(q => q.TenantId == tenantId && requestedAmounts.Keys.Contains(q.UsageType))
    .ToDictionaryAsync(q => q.UsageType, cancellationToken);
```

---

## G. Security Review

### Threat Model

| Threat | Mitigation | Status |
|--------|------------|--------|
| **T1: Unauthorized Asset Access** | HMAC-SHA256 tokens + time windows | ✅ Mitigated |
| **T2: Hotlinking** | `AssetRateLimitService` with IP blocking | ✅ Mitigated |
| **T3: Brute Force Token Guessing** | Rate limiting + token complexity (256-bit) | ✅ Mitigated |
| **T4: Path Traversal** | UUID-based storage keys, no user input in paths | ✅ Mitigated |
| **T5: Malicious File Upload** | ClamAV virus scanning + content type validation | ✅ Mitigated |
| **T6: Tenant Data Leakage** | `TenantAssetValidationService` fail-closed | ✅ Mitigated |
| **T7: Quota Bypass** | `ResourceQuotaBehavior` with atomic consumption | ✅ Mitigated |
| **T8: IDOR on Resources** | `[Authorize]` added | ⚠️ **PARTIAL** - Needs tenant membership validation |
| **T9: Unauthenticated Resource Access** | `[Authorize]` on all 9 controllers | ✅ **FIXED 2026-01-15** |

### Security Risk Register

| Risk ID | Risk | Severity | Attack Scenario | Current Mitigation | Status |
|---------|------|----------|-----------------|-------------------|--------|
| SR-001 | ~~Missing `[Authorize]` on Resources controllers~~ | ~~CRITICAL~~ | ~~Anonymous user enumerates all tenant quotas and usage~~ | `[Authorize]` added to all 9 controllers | ✅ **FIXED** |
| SR-002 | IDOR on tenant-scoped endpoints | **HIGH** | Authenticated user queries other tenants' data via URL manipulation | Authentication required, but no tenant membership check | ⚠️ OPEN |
| SR-003 | Global auth filter commented out | **HIGH** | Any new controller added without `[Authorize]` is unprotected | Developer vigilance only | ⚠️ OPEN |
| SR-004 | No rate limiting on Resources endpoints | **HIGH** | Attacker enumerates tenant IDs via timing attacks | None | ⚠️ OPEN |
| SR-005 | ValidateToken iterates all AccessPolicy values | **HIGH** | O(n) signature verification per request; DoS with many policies | Token caching | ⚠️ OPEN |
| SR-006 | N+1 queries in quota operations | **MEDIUM** | Performance degradation under load | None | ⚠️ OPEN |
| SR-007 | Hard-coded token validity constants | **LOW** | Difficult to adjust without code change | None | ⚠️ OPEN |

---

## H. Performance & Reliability

### Identified Concerns

| Issue | Impact | Evidence | Recommended Fix |
|-------|--------|----------|-----------------|
| **N+1 Queries** | DB connection exhaustion under load | `ResourceQuotaService.cs:158` | Batch queries with `ToDictionaryAsync()` |
| **Token Validation Loop** | O(n) per request | `AssetTokenService.ValidateToken()` iterates `AccessPolicy` enum | Pre-compute valid signatures, cache tokens |
| **Unbounded Result Sets** | Memory pressure on large tenants | `GetResourceUsageRecordsQuery` returns all matching records | Add pagination |
| **No Connection Pooling Config** | Connection starvation | Default EF Core settings | Configure `MaxPoolSize` in connection string |

### Caching Architecture ✅

- `CachedResourceQuotaService` implements decorator pattern correctly
- Tenant-scoped cache keys prevent cross-tenant leakage: `quota:{tenantId}:{type}`
- Write-through invalidation on mutations
- 30-second cache TTL balances performance vs staleness

---

## Findings Table

| # | Finding | Severity | Evidence | Why It Matters | Status |
|---|---------|----------|----------|----------------|--------|
| 1 | ~~All 9 Resources controllers lack `[Authorize]`~~ | ~~CRITICAL~~ | [All Controllers](apps/api/Source/Modules/GameGuild.Resources/Controllers/) | ~~Anonymous access to tenant data~~ | ✅ **FIXED 2026-01-15** |
| 2 | IDOR on tenant-scoped endpoints | **HIGH** | [TenantResourcesController.cs:33](apps/api/Source/Modules/GameGuild.Resources/Controllers/TenantResourcesController.cs#L33) | Cross-tenant data access via URL manipulation. Requires `ITenantMembershipValidator`. | ⚠️ OPEN |
| 3 | Global PermissionAuthorizationFilter disabled | **HIGH** | [ServiceCollectionExtensions.cs:739-740](apps/api/Source/GameGuild.API/Core/Extensions/ServiceCollectionExtensions.cs#L739) | Defense-in-depth gap. New controllers may be accidentally unprotected. | ⚠️ OPEN |
| 4 | No rate limiting on Resources endpoints | **HIGH** | All 9 controllers lack `[EnableRateLimiting]` | Enumeration attacks, tenant ID guessing via timing | ⚠️ OPEN |
| 5 | ValidateToken O(n) complexity | **HIGH** | `AssetTokenService.ValidateToken()` | DoS vulnerability via signature verification | ⚠️ OPEN |
| 6 | N+1 query in CheckMultipleLimitsAsync | **MEDIUM** | [ResourceQuotaService.cs:158](apps/api/Source/Modules/GameGuild.Resources/Services/ResourceQuotaService.cs#L158) | Performance degradation under concurrent load | ⚠️ OPEN |
| 7 | ~~Resources administrative endpoints publicly exposed~~ | ~~MEDIUM~~ | [ResourcesController.cs](apps/api/Source/Modules/GameGuild.Resources/Controllers/ResourcesController.cs) | ~~Cross-tenant usage aggregation~~ | ✅ **FIXED** - Now requires admin role |
| 8 | Unbounded result sets | **MEDIUM** | `GetResourceUsageRecordsQuery` returns all matching records | Memory pressure on large tenants, OOM risk | ⚠️ OPEN |
| 9 | Missing input validation on date ranges | **MEDIUM** | [TenantResourcesController.cs:33](apps/api/Source/Modules/GameGuild.Resources/Controllers/TenantResourcesController.cs#L33) | Invalid date ranges accepted, potential for DoS | ⚠️ OPEN |
| 10 | User quota endpoints vulnerable to vertical escalation | **MEDIUM** | [UserQuotasController.cs:74](apps/api/Source/Modules/GameGuild.Resources/Controllers/UserQuotasController.cs#L74) | Any user can modify any other user's quota. Requires ownership check. | ⚠️ OPEN |
| 11 | Hard-coded token validity constants | **LOW** | `AssetTokenService.cs` (86400s, 28800s) | Configuration rigidity, requires code change | ⚠️ OPEN |
| 12 | Missing rollback test coverage | **LOW** | [ResourceQuotaBehaviorTests.cs](apps/api/Tests/GameGuild.Resources.UnitTests/Behaviors/ResourceQuotaBehaviorTests.cs) | Rollback path on command failure untested | ⚠️ OPEN |
| 13 | Deprecated `EnforceHardLimit` property still present | **LOW** | [RequiresQuotaAttribute.cs:46](apps/api/Source/Modules/GameGuild.Resources/Attributes/RequiresQuotaAttribute.cs#L46) | Confusing API, developers may think it works | ⚠️ OPEN |
| 14 | No audit logging for quota administrative changes | **LOW** | `TenantQuotasController.SetQuota()` | Compliance gap for SOC2/ISO 27001 | ⚠️ OPEN |

---

## Recommended Refinements (Prioritized)

### P0: Critical Security Fixes (Week 1)

1. **Add `[Authorize]` to all Resources controllers**
   ```csharp
   [ApiController]
   [ApiVersion("1.0")]
   [Tags("resources")]
   [Authorize]  // ADD THIS
   public sealed class ResourcesController(ISender sender) : ControllerBase
   ```

2. **Add tenant membership validation**
   ```csharp
   // Create ITenantMembershipValidator service
   public interface ITenantMembershipValidator
   {
       Task<bool> ValidateAccessAsync(Guid tenantId, CancellationToken ct);
   }
   
   // Apply in controllers
   if (!await tenantValidator.ValidateAccessAsync(tenantId, ct))
       return Forbid();
   ```

3. **Re-enable global authorization filter or adopt attribute-based approach**
   - Either uncomment the global filter
   - Or establish a review process ensuring all controllers have `[Authorize]`

### P1: High Priority (Week 2)

4. **Apply rate limiting to Resources endpoints**
   ```csharp
   [EnableRateLimiting("fixed")]
   public sealed class TenantResourcesController
   ```

5. **Optimize token validation**
   ```csharp
   // Cache valid signatures by AccessPolicy
   private readonly ConcurrentDictionary<AccessPolicy, byte[]> _signatureCache = new();
   ```

### P2: Medium Priority (Week 3-4)

6. **Fix N+1 queries**
   ```csharp
   // Batch pattern for CheckLimitsAsync
   public async Task<Dictionary<ResourceUsageType, LimitCheckResult>> CheckLimitsBatchAsync(
       Guid tenantId, 
       Dictionary<ResourceUsageType, int> requests,
       CancellationToken ct)
   {
       var quotas = await _dbContext.ResourceQuotas
           .Where(q => q.TenantId == tenantId && requests.Keys.Contains(q.UsageType))
           .ToDictionaryAsync(q => q.UsageType, ct);
       
       return requests.ToDictionary(
           r => r.Key,
           r => ComputeLimit(quotas.GetValueOrDefault(r.Key), r.Value));
   }
   ```

7. **Add pagination to usage queries**
   ```csharp
   public record GetResourceUsageRecordsQuery(
       Guid TenantId,
       ResourceUsageType? UsageType,
       DateTime? StartDate,
       DateTime? EndDate,
       int PageNumber = 1,
       int PageSize = 50) : IRequest<PagedResult<ResourceUsageRecordDto>>;
   ```

### P3: Low Priority (Week 5+)

8. **Extract configuration constants**
9. **Add rollback path unit tests**
10. **Document threat model in module READMEs**

---

## Test Plan

### Unit Tests to Add

| Test Case | Target | Priority |
|-----------|--------|----------|
| `ResourceQuotaBehavior_RollsBack_OnFailure` | `ResourceQuotaBehavior.cs` | P1 |
| `TenantMembershipValidator_ReturnsFalse_ForNonMember` | New validator | P0 |
| `AssetTokenService_CachesSignatures` | Token optimization | P1 |
| `CheckLimitsBatch_SingleDbQuery` | N+1 fix verification | P2 |

### Integration Tests to Add

| Test Case | Scenario | Priority |
|-----------|----------|----------|
| `ResourcesController_Returns401_WhenAnonymous` | Auth verification | P0 |
| `TenantResources_Returns403_ForWrongTenant` | IDOR prevention | P0 |
| `AssetUpload_FailsOnQuotaExceeded` | Quota integration | P1 |
| `RateLimiting_Blocks_AfterThreshold` | Rate limit verification | P1 |

### Security Tests (Penetration Testing)

| Test | Target | Priority |
|------|--------|----------|
| Unauthenticated endpoint access | All Resources controllers | P0 |
| IDOR via tenant ID manipulation | Tenant-scoped endpoints | P0 |
| Token brute force resistance | `SecureAssetDeliveryController` | P1 |
| Enumeration timing attacks | Resources list endpoints | P2 |

---

## Appendix: Files Examined

### GameGuild.Assets
- `Security/AssetAuthorizationHandler.cs` (302 lines)
- `Security/TenantAssetValidationService.cs` (211 lines)
- `Security/SecureAssetDeliveryController.cs` (344 lines)
- `Security/AssetRateLimitService.cs` (258 lines)
- `Services/AssetAccessService.cs` (379 lines)
- `Services/AssetTokenService.cs` (268 lines)
- `Controllers/AssetsController.cs` (477 lines)
- `Controllers/AssetsAdminController.cs` (374 lines)
- `Entities/AssetContent.cs` (262 lines)
- `Entities/AssetReference.cs` (207 lines)

### GameGuild.Resources
- `Services/ResourceQuotaService.cs` (405 lines)
- `Behaviors/ResourceQuotaBehavior.cs` (261 lines)
- `Entities/ResourceQuota.cs` (211 lines)
- `Controllers/TenantQuotasController.cs` (191 lines)
- `Controllers/ResourcesController.cs` (61 lines)
- `Controllers/TenantResourcesController.cs` (187 lines)
- `Extensions/DependencyInjectionInfrastructure.cs` (150 lines)
- `Queries/CheckResourceUsageLimits/CheckResourceUsageLimitsQueryHandler.cs`

### API Configuration
- `GameGuild.API/Core/Setup/PresentationLayerExtensions.cs` (275 lines)
- `GameGuild.API/Core/Extensions/ServiceCollectionExtensions.cs` (939 lines)
- `GameGuild.Identity.Authorization/Providers/DbAuthorizationPolicyProvider.cs` (119 lines)
- `GameGuild.Identity.Authorization/Extensions/AuthorizationModuleExtensions.cs` (376 lines)

---

## Conclusion

The **GameGuild.Assets** module demonstrates exemplary security architecture with comprehensive threat mitigation, fail-closed patterns, and proper authorization integration. It should serve as the reference implementation for other modules.

The **GameGuild.Resources** module has sound internal architecture (ISP, caching decorator, optimistic concurrency) and **now has authentication enforced on all controllers** (fixed 2026-01-15).

### ✅ Fixes Applied (2026-01-15)

| Controller | Fix Applied |
|------------|-------------|
| `ResourcesController` | `[Authorize(Policy = "RequireAdminRole")]` |
| `TenantQuotasController` | `[Authorize]` |
| `TenantResourcesController` | `[Authorize]` |
| `TenantResourceMetadataController` | `[Authorize]` |
| `TenantResourceSettingsController` | `[Authorize]` |
| `UserQuotasController` | `[Authorize]` |
| `UserResourcesController` | `[Authorize]` |
| `UserResourceMetadataController` | `[Authorize]` |
| `UserResourceSettingsController` | `[Authorize]` |

### Remaining Actions Required

1. ⚠️ **HIGH:** Implement `ITenantMembershipValidator` for IDOR protection
2. ⚠️ **HIGH:** Re-enable global `PermissionAuthorizationFilter`
3. ⚠️ **HIGH:** Add `[EnableRateLimiting]` to Resources endpoints
4. ✅ **VERIFY:** Run integration tests confirming 401/403 responses

---

## 30/60/90-Day Roadmap

### 30-Day Sprint (Critical Security)

| Day | Task | Owner | Deliverable |
|-----|------|-------|-------------|
| 1-2 | Add `[Authorize]` to all 9 Resources controllers | Dev Team | PR with auth attributes |
| 3-5 | Implement `ITenantMembershipValidator` service | Security Lead | Service + unit tests |
| 6-7 | Inject validator into all tenant-scoped controller actions | Dev Team | Updated controllers |
| 8-10 | Write integration tests for 401/403 responses | QA | Test suite (8+ tests) |
| 11-12 | Re-enable global `PermissionAuthorizationFilter` or equivalent | Architecture | Config change + validation |
| 13-15 | Add `[EnableRateLimiting]` to Resources endpoints | Dev Team | Rate limiting configured |
| 16-20 | Security audit: run OWASP ZAP against Resources API | Security | Audit report |
| 21-25 | Fix any additional findings from security audit | Dev Team | Remediation PRs |
| 26-30 | Final integration test pass + sign-off | QA + Security | Go-live approval |

### 60-Day Sprint (Performance & Reliability)

| Week | Task | Owner | Deliverable |
|------|------|-------|-------------|
| 5 | Fix N+1 query in `CheckMultipleLimitsAsync` | Dev Team | Batch query implementation |
| 5 | Fix N+1 query in `CheckResourceUsageLimitsQueryHandler` | Dev Team | Optimized handler |
| 6 | Add pagination to `GetResourceUsageRecordsQuery` | Dev Team | Paginated API |
| 6 | Implement token signature caching in `AssetTokenService` | Dev Team | Performance improvement |
| 7 | Add FluentValidation to date range inputs | Dev Team | Input validation |
| 7 | Configure connection pooling for high load | DevOps | DB config update |
| 8 | Load testing: 1000 concurrent quota operations | QA | Performance report |

### 90-Day Sprint (Maintainability & Observability)

| Week | Task | Owner | Deliverable |
|------|------|-------|-------------|
| 9 | Extract magic numbers to `IOptions<>` configuration | Dev Team | Config refactor |
| 10 | Add rollback path unit tests for `ResourceQuotaBehavior` | Dev Team | Test coverage |
| 10 | Document threat model in module READMEs | Security | Documentation |
| 11 | Add OpenTelemetry tracing to quota operations | DevOps | Observability |
| 11 | Create alerting rules for quota exceeded events | DevOps | Monitoring |
| 12 | Performance baseline documentation | Architecture | Performance SLOs |

---

## Top 10 Issues (Ordered by Severity)

| Rank | Issue | Severity | Module | Status |
|------|-------|----------|--------|--------|
| 1 | ~~Missing `[Authorize]` on all 9 Resources controllers~~ | ~~CRITICAL~~ | Resources | ✅ **FIXED** |
| 2 | IDOR on tenant-scoped endpoints (no membership validation) | **HIGH** | Resources | ⚠️ OPEN |
| 3 | Global `PermissionAuthorizationFilter` disabled | **HIGH** | API | ⚠️ OPEN |
| 4 | No rate limiting on Resources endpoints | **HIGH** | Resources | ⚠️ OPEN |
| 5 | Token validation O(n) complexity per request | **HIGH** | Assets | ⚠️ OPEN |
| 6 | N+1 query in `CheckMultipleLimitsAsync` | **MEDIUM** | Resources | ⚠️ OPEN |
| 7 | ~~N+1 query in `CheckResourceUsageLimitsQueryHandler`~~ | **MEDIUM** | Resources | ⚠️ OPEN |
| 8 | Unbounded result sets in usage queries | **MEDIUM** | Resources | ⚠️ OPEN |
| 9 | Missing input validation on date ranges | **MEDIUM** | Resources | ⚠️ OPEN |
| 10 | Hard-coded token validity constants | **LOW** | Assets | ⚠️ OPEN |

---

## Detailed Fix Recommendations

### Fix #1: Add Authorization to Resources Controllers

**Location:** All 9 controllers in `GameGuild.Resources/Controllers/`

```csharp
// Before
[ApiController]
[ApiVersion("1.0")]
[Tags("resources")]
public sealed class ResourcesController(ISender sender) : ControllerBase

// After
[ApiController]
[ApiVersion("1.0")]
[Tags("resources")]
[Authorize]  // ADD THIS
public sealed class ResourcesController(ISender sender) : ControllerBase
```

**Apply to:**
- `ResourcesController.cs`
- `TenantQuotasController.cs`
- `TenantResourcesController.cs`
- `TenantResourceMetadataController.cs`
- `TenantResourceSettingsController.cs`
- `UserQuotasController.cs`
- `UserResourcesController.cs`
- `UserResourceMetadataController.cs`
- `UserResourceSettingsController.cs`

### Fix #2: Implement Tenant Membership Validation

**New Service:**

```csharp
// GameGuild.Identity.Tenants/Services/ITenantMembershipValidator.cs
public interface ITenantMembershipValidator
{
    Task<bool> ValidateMembershipAsync(
        Guid tenantId, 
        ActorContext actor, 
        CancellationToken ct = default);
}

public class TenantMembershipValidator : ITenantMembershipValidator
{
    private readonly ITenantMemberRepository _memberRepository;
    private readonly ILogger<TenantMembershipValidator> _logger;

    public async Task<bool> ValidateMembershipAsync(
        Guid tenantId, 
        ActorContext actor, 
        CancellationToken ct = default)
    {
        // FAIL-CLOSED: Reject if no actor
        if (!actor.IsAuthenticated || !actor.SubjectIdAsGuid.HasValue)
        {
            _logger.LogWarning("Tenant membership validation failed: No authenticated actor");
            return false;
        }

        // Check if actor's tenant matches requested tenant
        if (actor.TenantId.HasValue && actor.TenantId.Value == tenantId)
        {
            return true;
        }

        // System admin bypass (optional, configurable)
        if (actor.IsSystemAdmin)
        {
            _logger.LogInformation(
                "Cross-tenant access allowed for system admin {UserId}", 
                actor.SubjectId);
            return true;
        }

        // Check membership in database
        var isMember = await _memberRepository.IsMemberAsync(
            tenantId, 
            actor.SubjectIdAsGuid.Value, 
            ct);

        if (!isMember)
        {
            _logger.LogWarning(
                "User {UserId} attempted access to tenant {TenantId} without membership",
                actor.SubjectId, tenantId);
        }

        return isMember;
    }
}
```

**Apply in Controllers:**

```csharp
// TenantResourcesController.cs
[HttpGet("v{version:apiVersion}/tenants/{tenantId:guid}/resources/usage-records")]
public async Task<IActionResult> GetUsageRecords(
    Guid tenantId, 
    [FromQuery] ResourceUsageType? usageType, 
    CancellationToken ct)
{
    // ADD: Tenant membership validation
    if (!await _tenantValidator.ValidateMembershipAsync(tenantId, Actor, ct))
    {
        return Forbid();
    }

    return Ok(await sender.Send(
        new GetResourceUsageRecordsQuery(tenantId, usageType, null, null), 
        ct).ConfigureAwait(false));
}
```

### Fix #3: Re-enable Global Authorization Filter

**Location:** `ServiceCollectionExtensions.cs:739-740`

```csharp
// Before (commented out)
// if (options.EnablePermissionAuthorizationFilter)
//     mvcOptions.Filters.Add<PermissionAuthorizationFilter>();

// After (uncommented and enabled)
if (options.EnablePermissionAuthorizationFilter)
    mvcOptions.Filters.Add<PermissionAuthorizationFilter>();
```

**Also update configuration:**
```json
// appsettings.json
{
  "Controllers": {
    "EnablePermissionAuthorizationFilter": true
  }
}
```

### Fix #4: Batch Query for N+1 Optimization

**Location:** `ResourceQuotaService.cs:150-162`

```csharp
// Before (N+1)
public async Task<Dictionary<ResourceUsageType, ResourceLimitCheckResponse>> CheckMultipleLimitsAsync(
    Guid tenantId,
    Dictionary<ResourceUsageType, long> requestedAmounts,
    CancellationToken cancellationToken = default)
{
    var results = new Dictionary<ResourceUsageType, ResourceLimitCheckResponse>();
    foreach (var kvp in requestedAmounts) 
    { 
        results[kvp.Key] = await CheckLimitsAsync(tenantId, kvp.Key, kvp.Value, cancellationToken); 
    }
    return results;
}

// After (Batch Query)
public async Task<Dictionary<ResourceUsageType, ResourceLimitCheckResponse>> CheckMultipleLimitsAsync(
    Guid tenantId,
    Dictionary<ResourceUsageType, long> requestedAmounts,
    CancellationToken cancellationToken = default)
{
    // Single query to fetch all needed quotas
    var quotas = await quotaRepository.GetByTenantAndTypesAsync(
        tenantId, 
        requestedAmounts.Keys, 
        cancellationToken);

    var quotaMap = quotas.ToDictionary(q => q.Type);

    return requestedAmounts.ToDictionary(
        kvp => kvp.Key,
        kvp => ComputeLimitCheck(quotaMap.GetValueOrDefault(kvp.Key), kvp.Value));
}

private ResourceLimitCheckResponse ComputeLimitCheck(ResourceQuota? quota, long requestedAmount)
{
    if (quota == null)
    {
        return new ResourceLimitCheckResponse 
        { 
            Type = default, 
            CanProceed = true, 
            CurrentUsage = 0 
        };
    }

    var effectiveUsage = quota.ShouldReset() ? 0 : quota.CurrentUsage;
    var projectedUsage = effectiveUsage + requestedAmount;

    return new ResourceLimitCheckResponse
    {
        Type = quota.Type,
        CanProceed = !quota.HardLimit.HasValue || projectedUsage <= quota.HardLimit.Value,
        CurrentUsage = effectiveUsage,
        SoftLimit = quota.SoftLimit,
        HardLimit = quota.HardLimit
    };
}
```

---

## Verification Checklist

After implementing fixes, verify:

- [ ] All 9 Resources controllers return 401 for anonymous requests
- [ ] Tenant-scoped endpoints return 403 for non-member access
- [ ] Rate limiting is active on Resources endpoints
- [ ] N+1 queries are eliminated (verify with SQL logging)
- [ ] Integration tests pass for tenant isolation
- [ ] Security scan shows no new vulnerabilities
- [ ] Performance benchmarks show no regression

---

*Report generated by AI Architecture Review Agent - Claude Opus 4.5*  
*Last updated: 2026-01-15*

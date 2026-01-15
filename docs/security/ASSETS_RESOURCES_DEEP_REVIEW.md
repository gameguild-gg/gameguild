# GameGuild.Assets & GameGuild.Resources Deep Review

**Date:** 2025-01-XX  
**Reviewer:** AI Architecture Review Agent  
**Scope:** GameGuild.Assets, GameGuild.Resources modules  
**Review Type:** Architecture, Security, Code Quality

---

## Executive Summary

This deep review evaluates the **GameGuild.Assets** and **GameGuild.Resources** modules for architecture coherence, security posture, design patterns, and integration quality. The review identified **2 CRITICAL**, **3 HIGH**, **4 MEDIUM**, and **3 LOW** severity findings.

### Go/No-Go Assessment: **CONDITIONAL GO** ⚠️

The modules are production-ready **after** addressing the 2 CRITICAL and 3 HIGH findings. The critical findings relate to missing authorization on all 9 Resources controllers and IDOR vulnerabilities in tenant-scoped endpoints.

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

### Assets Module ✅

| Component | Auth Integration | Status |
|-----------|------------------|--------|
| `AssetsController` | `[Authorize]` attribute | ✅ |
| `AssetsAdminController` | `[Authorize(Policy = "RequireAdminRole")]` | ✅ |
| `SecureAssetDeliveryController` | HMAC token validation (bypass auth) | ✅ |
| `AssetAuthorizationHandler` | Reads `IActorContextAccessor` | ✅ |
| `TenantAssetValidationService` | Fail-closed on missing tenant | ✅ |

### Resources Module ❌ CRITICAL

| Component | Auth Integration | Status |
|-----------|------------------|--------|
| `ResourcesController` | **NONE** | ❌ CRITICAL |
| `TenantQuotasController` | **NONE** | ❌ CRITICAL |
| `TenantResourcesController` | **NONE** | ❌ CRITICAL |
| `UserQuotasController` | **NONE** | ❌ CRITICAL |
| All 9 controllers | **NONE** | ❌ CRITICAL |
| `ResourceQuotaBehavior` | Reads `IActorContextAccessor` | ✅ |

**Root Cause:** Global `PermissionAuthorizationFilter` is commented out in [ServiceCollectionExtensions.cs#L738-740](apps/api/Source/GameGuild.API/Core/Extensions/ServiceCollectionExtensions.cs#L738):

```csharp
// TODO: Re-enable after core bootstrap is stable
// Add permission authorization filter globally to all controllers
// if (options.EnablePermissionAuthorizationFilter)
//     mvcOptions.Filters.Add<PermissionAuthorizationFilter>();
```

---

## D. Integration with Authorization (RBAC/DAC/ABAC)

### Assets Module ✅ Comprehensive

**DAC Pattern Implementation:**
- `AssetAuthorizationHandler` implements DAC via `IAccessControlListService`
- Owner bypass: asset owner has implicit full access
- ACL checks: reads ACEs from `IAccessControlListService.GetAccessControlEntriesAsync()`
- Resource-level permissions with cascading tenant context

**Authorization Flow:**
```
Request → [Authorize] → AssetAuthorizationHandler
                              │
                              ├── Check Owner (bypass if owner)
                              ├── Check ACL (via IAccessControlListService)
                              └── Fail-closed if no match
```

### Resources Module ❌ CRITICAL

**No authorization integration at controller level:**
- Controllers accept `{tenantId:guid}` from URL without validation
- No check if current user is member of specified tenant
- No RBAC/DAC/ABAC enforcement at endpoint level

**IDOR Vulnerability:**
```http
GET /v1/tenants/{ANY-TENANT-ID}/resources/usage-records
```
Any user (or anonymous!) can query any tenant's resource usage.

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
| **T8: IDOR on Resources** | **NONE** | ❌ **CRITICAL** |
| **T9: Unauthenticated Resource Access** | **NONE** | ❌ **CRITICAL** |

### Security Risk Register

| Risk ID | Risk | Severity | Attack Scenario | Current Mitigation | Priority |
|---------|------|----------|-----------------|-------------------|----------|
| SR-001 | Missing `[Authorize]` on Resources controllers | **CRITICAL** | Anonymous user enumerates all tenant quotas and usage | None | P0 |
| SR-002 | IDOR on tenant-scoped endpoints | **CRITICAL** | Authenticated user queries other tenants' data via URL manipulation | None | P0 |
| SR-003 | Global auth filter commented out | **HIGH** | Any new controller added without `[Authorize]` is unprotected | Developer vigilance only | P1 |
| SR-004 | No rate limiting on Resources endpoints | **HIGH** | Attacker enumerates tenant IDs via timing attacks | None | P1 |
| SR-005 | ValidateToken iterates all AccessPolicy values | **HIGH** | O(n) signature verification per request; DoS with many policies | Token caching | P1 |
| SR-006 | N+1 queries in quota operations | **MEDIUM** | Performance degradation under load | None | P2 |
| SR-007 | Hard-coded token validity constants | **LOW** | Difficult to adjust without code change | None | P3 |

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

- `CachedResourceQuotaService` implements decorator pattern
- Hybrid cache (L1: IMemoryCache, L2: optional Redis)
- Version-based invalidation via `ITenantSecurityVersionStore`

---

## Findings Table

| # | Finding | Severity | Evidence | Why It Matters | Recommended Fix |
|---|---------|----------|----------|----------------|-----------------|
| 1 | All 9 Resources controllers lack `[Authorize]` | **CRITICAL** | [ResourcesController.cs:11](apps/api/Source/Modules/GameGuild.Resources/Controllers/ResourcesController.cs#L11) | Anonymous access to tenant data | Add `[Authorize]` to all controllers |
| 2 | IDOR on tenant-scoped endpoints | **CRITICAL** | [TenantResourcesController.cs:27](apps/api/Source/Modules/GameGuild.Resources/Controllers/TenantResourcesController.cs#L27) | Cross-tenant data access | Add tenant membership validation |
| 3 | Global PermissionAuthorizationFilter disabled | **HIGH** | [ServiceCollectionExtensions.cs:738](apps/api/Source/GameGuild.API/Core/Extensions/ServiceCollectionExtensions.cs#L738) | Defense-in-depth gap | Re-enable or use alternative |
| 4 | No rate limiting on Resources endpoints | **HIGH** | All 9 controllers | Enumeration attacks | Apply `[EnableRateLimiting]` |
| 5 | ValidateToken O(n) complexity | **HIGH** | `AssetTokenService.ValidateToken()` | DoS vulnerability | Cache computed signatures |
| 6 | N+1 query in CheckLimitsAsync | **MEDIUM** | [ResourceQuotaService.cs:158](apps/api/Source/Modules/GameGuild.Resources/Services/ResourceQuotaService.cs#L158) | Performance degradation | Batch query pattern |
| 7 | N+1 query in UpdateAsync loop | **MEDIUM** | [CheckResourceUsageLimitsQueryHandler.cs:46](apps/api/Source/Modules/GameGuild.Resources/Queries/CheckResourceUsageLimits/CheckResourceUsageLimitsQueryHandler.cs#L46) | Connection exhaustion | Bulk update pattern |
| 8 | Unbounded result sets | **MEDIUM** | `GetResourceUsageRecordsQuery` | Memory pressure | Add pagination |
| 9 | Missing input validation on date ranges | **MEDIUM** | `TenantResourcesController.GetUsageRecords()` | Invalid queries | Add FluentValidation |
| 10 | Hard-coded token constants | **LOW** | `AssetTokenService.cs` | Configuration rigidity | Move to `IOptions<>` |
| 11 | Missing rollback test coverage | **LOW** | `ResourceQuotaBehavior` | Untested failure paths | Add unit tests |
| 12 | Magic numbers in rate limiting | **LOW** | `AssetRateLimitService.cs` | Maintenance difficulty | Extract to configuration |

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

The **GameGuild.Resources** module has sound internal architecture (ISP, caching decorator, optimistic concurrency) but **lacks essential security controls at the controller layer**. The 2 CRITICAL findings (missing `[Authorize]`, IDOR vulnerability) must be addressed before any production deployment.

### Immediate Actions Required

1. ❌ **STOP:** Do not deploy Resources module without auth fixes
2. 🔧 **FIX:** Add `[Authorize]` to all 9 Resources controllers
3. 🔧 **FIX:** Implement tenant membership validation
4. 🔧 **FIX:** Re-enable or replace global authorization filter
5. ✅ **VERIFY:** Run integration tests confirming 401/403 responses

---

*Report generated by AI Architecture Review Agent*

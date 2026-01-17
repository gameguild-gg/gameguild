# Deep Review: GameGuild.Features & GameGuild.Learning.*

**Date**: January 17, 2026  
**Reviewer**: Platform Architecture & Security Engineering  
**Scope**: GameGuild.Features, GameGuild.Learning.* modules  
**Legacy Sources**: `./temp/api-b` (Certificates, Contents, Programs)

---

## Executive Summary

This deep review analyzes the `GameGuild.Features` (feature flag management) and `GameGuild.Learning.*` (educational content platform) modules within the GameGuild multi-tenant SaaS platform. The review identifies **critical security gaps**, **architectural concerns**, and provides a **prioritized remediation roadmap**.

### Key Findings

| Area | Status | Critical Issues |
|------|--------|-----------------|
| **AuthZ Integration** | ⚠️ PARTIAL | Permission attributes **commented out** in Learning controllers |
| **Tenant Isolation** | ⚠️ PARTIAL | No global query filters; manual filtering inconsistent |
| **Feature Completeness** | 🔶 INCOMPLETE | Certificates module stub only; Legacy code exists |
| **Security Posture** | ⛔ REQUIRES FIXES | IDOR, cross-tenant risks, missing fail-closed |

### Go/No-Go Assessment: **NO-GO** (Without Critical Fixes)

**Blockers**:
1. Authorization attributes commented out on 25+ endpoints (HIGH)
2. No tenant-scoped query filters in Learning module (HIGH)
3. Certificate issuance workflow missing (MEDIUM)

---

## 1) Current State Summary

### GameGuild.Features — Feature Flag Management

**What it does:**
- Manages feature flags for gradual rollouts, A/B testing, and kill switches
- Supports tenant-specific, user-specific, and environment-based targeting
- Implements OpenFeature provider pattern for SDK compatibility
- Provides analytics tracking for feature flag evaluations

**Architecture:**
- **Entities**: `FeatureFlag`, `FeatureFlagTarget`, `FeatureFlagUsage`
- **Services**: Evaluation (with decorator chain: caching → analytics → logging)
- **Strategies**: Simple toggle, percentage rollout, targeted evaluation
- **Targeting Handlers**: Tenant, User, Plan, Country, Custom (chain of responsibility)
- **Controllers**: `FeatureFlagsController` (runtime evaluation)

**Security Highlights:**
- ✅ Fail-closed behavior in `TenantTargetingHandler` when tenant rules exist but no TenantId provided
- ✅ Decorator pattern for separation of concerns
- ⚠️ `GetCurrentTenantId()` and `GetCurrentUserId()` return `null` (stub implementations)

### GameGuild.Learning.Courses — Learning Program Management

**What it does:**
- Manages educational programs with structured content modules
- Handles user enrollments, progress tracking, and completion
- Supports content interactions, activity grading, and rating systems
- Provides lifecycle management (draft → review → published → archived)

**Architecture:**
- **Entities**: `Program`, `ProgramContent`, `ProgramUser`, `ContentInteraction`, `ActivityGrade`, `ProgramRating`, `ProgramWishlist`
- **Services**: `ProgramService`, `ProgramContentService`, `ActivityGradeService`, `ContentInteractionService`
- **Controllers**: `ProgramController`, `ProgramContentController`, `ActivityGradeController`, `ContentInteractionController`
- **Commands**: CQRS handlers for enrollment, content management, lifecycle transitions

**Security Concerns:**
- ⛔ **ALL permission attributes are commented out** in controllers
- ⛔ No `[Authorize]` attribute on `ProgramController` (only on child controllers)
- ⛔ Services do not inject `IActorContextAccessor` — no tenant/user validation at service layer

### GameGuild.Learning.Certificates — Certificate Issuance (STUB)

**What it exists:**
- **Entities**: `CertificateTemplate`, `Certificate` (issued certificates)
- **Status**: Entity definitions only; **no controllers, services, or handlers**

### Other Learning Submodules (MINIMAL)

| Module | Status | Contents |
|--------|--------|----------|
| `GameGuild.Learning.Enrollments` | STUB | Entity placeholder only |
| `GameGuild.Learning.Assessments` | STUB | Entity placeholder only |
| `GameGuild.Learning.Cohorts` | STUB | `Cohort` entity with TenantId |
| `GameGuild.Learning.Experience.*` | STUB | Discovery, LearningPaths, Recommendations, Social |

---

## 2) Architecture Map

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              API Layer (ASP.NET Core)                       │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐     ┌─────────────────────────────────────────┐    │
│  │FeatureFlagsController│     │ ProgramController (NO AUTH!)           │    │
│  │  [Authorize] ✅      │     │ ProgramContentController [Authorize]   │    │
│  └──────────┬──────────┘     │ ActivityGradeController [Authorize]    │    │
│             │                 └──────────────────┬──────────────────────┘   │
│             │                                    │                          │
│  ┌──────────▼──────────┐     ┌──────────────────▼──────────────────────┐   │
│  │ Evaluation Service  │     │         ProgramService                  │   │
│  │ (Decorator Chain)   │     │         ProgramContentService           │   │
│  │  - Logging          │     │         ActivityGradeService            │   │
│  │  - Analytics        │     │         ContentInteractionService       │   │
│  │  - Caching          │     │         (NO IActorContextAccessor!)     │   │
│  │  - Core Evaluation  │     └──────────────────┬──────────────────────┘   │
│  └──────────┬──────────┘                        │                          │
│             │                                    │                          │
│  ┌──────────▼──────────┐     ┌──────────────────▼──────────────────────┐   │
│  │ Targeting Handlers  │     │    IApplicationDbContext (EF Core)      │   │
│  │ (Chain of Resp.)    │     │    - NO global tenant filter            │   │
│  │  1. TenantHandler   │     │    - Manual DeletedAt checks            │   │
│  │  2. UserHandler     │     └─────────────────────────────────────────┘   │
│  │  3. PlanHandler     │                                                    │
│  │  4. CountryHandler  │                                                    │
│  │  5. CustomHandler   │                                                    │
│  └─────────────────────┘                                                    │
└─────────────────────────────────────────────────────────────────────────────┘

Dependencies:
─────────────
GameGuild.Features
├── GameGuild.SharedKernel
├── GameGuild.Identity.Context (ActorContext)
├── GameGuild.Identity.Authorization
└── GameGuild.Commerce.Subscriptions (for plan-based targeting)

GameGuild.Learning.Courses
├── GameGuild.SharedKernel
├── GameGuild.Identity.Authentication (NOT Identity.Context!)
├── GameGuild.Identity.Authorization
└── GameGuild.Identity.Users

GameGuild.Learning.Certificates
├── GameGuild.SharedKernel
├── GameGuild.Learning.Courses
└── GameGuild.Learning.Assessments
```

### Dependency Direction Validation

| Concern | Status | Issue |
|---------|--------|-------|
| Domain → Application | ✅ OK | Entities in namespace, services separate |
| Application → Infrastructure | ⚠️ CONCERN | Services directly use `IApplicationDbContext` |
| Learning.Courses → Identity.Context | ⛔ MISSING | Does not reference ActorContext accessor |

---

## 3) Findings Table

| # | Finding | Severity | Evidence | Why It Matters | Recommended Fix |
|---|---------|----------|----------|----------------|-----------------|
| 1 | **All permission attributes commented out** | **HIGH** | [ProgramController.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramController.cs#L42-L100) - `// [RequireContentTypePermission...]` | Any authenticated user can CRUD any program | Uncomment and wire up `RequireResourcePermission` attributes |
| 2 | **ProgramController missing [Authorize]** | **HIGH** | [ProgramController.cs#L26](apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramController.cs#L26) | Unauthenticated access to all program endpoints | Add `[Authorize]` to controller class |
| 3 | **No tenant-scoped query filters** | **HIGH** | [ProgramService.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Services/ProgramService.cs) - no `Where(p => p.TenantId == ...)` | Cross-tenant data leakage | Add `ITenantContext` injection and filter all queries |
| 4 | **GetCurrentUserId/TenantId stub** | **HIGH** | [FeatureFlagsController.cs#L162-L178](apps/api/Source/Modules/GameGuild.Features/Controllers/FeatureFlagsController.cs#L162-L178) - returns `null` | Feature evaluation without identity context | Inject `IActorContextAccessor` and use `ActorContext` |
| 5 | **Services don't use ActorContext** | **HIGH** | [ProgramService.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Services/ProgramService.cs) - only `IApplicationDbContext` | No audit trail, no tenant isolation at service layer | Inject `IActorContextAccessor`, validate tenant/actor |
| 6 | **IDOR: Direct ID access without ownership check** | **HIGH** | [ProgramController.cs#L108](apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramController.cs#L108) `GetProgram(Guid id)` | Access any resource by guessing ID | Add resource-level permission check |
| 7 | **Certificate module incomplete** | **MEDIUM** | [Certificate.cs](apps/api/Source/Modules/GameGuild.Learning.Certificates/Entities/Certificate.cs) - entity only | MVP workflow broken (no certificate issuance) | Port from legacy `temp/api-b/Modules/Certificates` |
| 8 | **No concurrency handling on progress updates** | **MEDIUM** | [ProgramService.cs#L238](apps/api/Source/Modules/GameGuild.Learning.Courses/Services/ProgramService.cs#L238) `UpdateUserProgressAsync` | Race conditions on concurrent progress updates | Add optimistic concurrency with EF Core `Version` property |
| 9 | **Generic exception handling leaks info** | **MEDIUM** | [FeatureFlagsController.cs#L42](apps/api/Source/Modules/GameGuild.Features/Controllers/FeatureFlagsController.cs#L42) - catches all exceptions | Stack traces/sensitive info in responses | Use ProblemDetails pattern |
| 10 | **Commented-out code blocks** | **LOW** | [FeatureFlagsController.cs#L180-L425](apps/api/Source/Modules/GameGuild.Features/Controllers/FeatureFlagsController.cs#L180-L425) | Code maintenance burden, security review confusion | Remove or complete commented code |
| 11 | **GetProgramBySlug allows unpublished access** | **MEDIUM** | [ProgramController.cs#L177-L197](apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramController.cs#L177-L197) | Authenticated users see draft content | Enforce status check for non-owners |
| 12 | **ProgramPermission model commented out** | **HIGH** | [ProgramPermission.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Models/ProgramPermission.cs) - entire class in `/* */` | Resource-level permissions non-functional | Uncomment and register in DI |
| 13 | **No rate limiting on feature evaluation** | **MEDIUM** | [FeatureFlagsController.cs](apps/api/Source/Modules/GameGuild.Features/Controllers/FeatureFlagsController.cs) | DoS via bulk evaluation endpoint | Add rate limiting middleware |
| 14 | **EnrollUser handler missing tenant validation** | **HIGH** | [EnrollUserCommandHandler.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Commands/EnrollUser/EnrollUserCommandHandler.cs) | Can enroll users cross-tenant | Validate actor tenant matches program tenant |
| 15 | **No audit logging in Learning module** | **MEDIUM** | UNKNOWN - grep for `ISecurityAuditLogger` in Learning | No compliance trail for FERPA | Inject and use `ISecurityAuditLogger` |

---

## 4) Security Risk Register

| Risk | Severity | Attack Scenario | Mitigation (Minimal Change) | Priority |
|------|----------|-----------------|----------------------------|----------|
| **Cross-Tenant Data Access** | **HIGH** | Attacker in Tenant A queries `GET /v1/courses/{idFromTenantB}` and retrieves Tenant B's program | Add tenant filter to `ProgramService.GetProgramByIdAsync`: `Where(p => p.TenantId == actorContext.TenantId)` | P0 |
| **Privilege Escalation** | **HIGH** | Authenticated user calls `PUT /v1/courses/{id}` to modify any program without ownership | Uncomment `[RequireResourcePermission<...>]` on all modifying endpoints | P0 |
| **Unauthenticated Access** | **HIGH** | Anonymous request to `/v1/courses` returns all programs | Add `[Authorize]` to `ProgramController` class | P0 |
| **IDOR on Enrollments** | **HIGH** | User A calls `GET /v1/courses/{id}/users/{userBId}/progress` to see User B's progress | Add check: `userId == actorContext.SubjectId` or `HasPermission(PermissionType.Analytics)` | P0 |
| **Feature Flag Spoofing** | **MEDIUM** | Client sends `tenantId` query param to get features for another tenant | Remove `tenantId` from query params; always use `ActorContext.TenantId` | P1 |
| **Certificate Forgery** | **MEDIUM** | (Future) Missing verification signature validation | Ensure legacy port includes `VerificationHash` and `DigitalSignature` validation | P1 |
| **Information Disclosure** | **MEDIUM** | Exception messages return internal error details | Replace `catch (Exception)` with `IExceptionHandler` returning `ProblemDetails` | P2 |
| **Replay Attack** | **MEDIUM** | Reuse of enrollment request creates duplicate enrollments | Add idempotency key to enrollment command | P2 |
| **Resource Exhaustion** | **LOW** | Bulk feature evaluation with 1000+ keys | Add max limit (e.g., 100) to `BulkEvaluationRequest.FeatureKeys` | P2 |

---

## 5) Feature Set & MVP Gap Table

| Feature / Flow | Status | Evidence (New Code) | Legacy Source | Recommendation |
|----------------|--------|---------------------|---------------|----------------|
| **Program CRUD** | PARTIAL | [ProgramController.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramController.cs) | N/A | Enable authorization |
| **Program Content Management** | PARTIAL | [ProgramContentController.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramContentController.cs) | N/A | Enable authorization |
| **User Enrollment** | PARTIAL | [EnrollUserCommandHandler.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Commands/EnrollUser/EnrollUserCommandHandler.cs) | N/A | Add tenant validation |
| **Progress Tracking** | DONE | [ContentInteractionService.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Services/ContentInteractionService.cs) | N/A | - |
| **Activity Grading** | DONE | [ActivityGradeService.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Services/ActivityGradeService.cs) | N/A | - |
| **Program Lifecycle (Draft→Publish)** | DONE | [ProgramController.cs#L338-L402](apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramController.cs#L338-L402) | N/A | Enable authorization |
| **Certificate Templates** | STUB | [CertificateTemplate](apps/api/Source/Modules/GameGuild.Learning.Certificates/Entities/Certificate.cs#L8) (entity only) | [Certificate.cs](temp/api-b/Modules/Certificates/Entities/Certificate.cs) | Port legacy controller & service |
| **Certificate Issuance** | MISSING | N/A | [UserCertificate.cs](temp/api-b/Modules/Certificates/Entities/UserCertificate.cs), [IUserCertificateService.cs](temp/api-b/Modules/Certificates/Interfaces/IUserCertificateService.cs) | Port legacy implementation |
| **Certificate Verification** | MISSING | N/A | `IUserCertificateService.VerifyCertificateAsync` | Port from legacy |
| **Certificate Revocation** | MISSING | N/A | `IUserCertificateService.RevokeCertificateAsync` | Port from legacy |
| **Peer Review** | STUB | [IPeerReviewService.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Abstractions/IPeerReviewService.cs) (interface only) | N/A | Implement or deprioritize |
| **Content Reporting** | STUB | [IContentReportService.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Abstractions/IContentReportService.cs) (interface only) | N/A | Implement or deprioritize |
| **Program Wishlist** | DONE | [ProgramWishlist.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Entities/ProgramWishlist.cs) | N/A | - |
| **Program Ratings** | DONE | [ProgramRating.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Entities/ProgramRating.cs) | N/A | - |
| **Feature Flag Evaluation** | DONE | [FeatureFlagEvaluationService.cs](apps/api/Source/Modules/GameGuild.Features/Services/FeatureFlagEvaluationService.cs) | N/A | Fix identity resolution |
| **Feature Flag Management** | PARTIAL | Commented out in controller | N/A | Uncomment and secure |
| **Feature Flag Analytics** | DONE | [FeatureFlagAnalyticsService.cs](apps/api/Source/Modules/GameGuild.Features/Services/FeatureFlagAnalyticsService.cs) | N/A | - |
| **Tenant Targeting** | DONE | [TenantTargetingHandler.cs](apps/api/Source/Modules/GameGuild.Features/Services/Handlers/TenantTargetingHandler.cs) | N/A | ✅ Fail-closed implemented |
| **Cohorts / Group Enrollment** | STUB | [Cohort.cs](apps/api/Source/Modules/GameGuild.Learning.Cohorts/Entities/Cohort.cs) | N/A | Implement for cohort-based courses |
| **Learning Paths** | STUB | [LearningPath.cs](apps/api/Source/Modules/GameGuild.Learning.Experience.LearningPaths/Entities/LearningPath.cs) | N/A | Implement for curated sequences |

---

## 6) Recommended Refinements

### P0 - Critical (Block Deployment)

#### 6.1 Enable Authorization on ProgramController

**What**: Add `[Authorize]` and uncomment permission attributes

**Where**: [ProgramController.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramController.cs)

**Why**: Currently allows unauthenticated and unauthorized access to all program operations

**Change**:
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/courses")]
[Authorize]  // ADD THIS
public class ProgramController(IProgramService programService) : ControllerBase {
  
  [HttpGet]
  [RequireContentTypePermission<Program>(PermissionType.Read)]  // UNCOMMENT
  public async Task<ActionResult<IEnumerable<Program>>> GetPrograms(...) { ... }
```

**Impact**: Requires authentication for all program access  
**Rollback**: Remove `[Authorize]` attribute

---

#### 6.2 Inject IActorContextAccessor into Services

**What**: Add tenant and actor context to ProgramService

**Where**: [ProgramService.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Services/ProgramService.cs)

**Why**: Enable tenant-scoped queries and audit logging

**Change**:
```csharp
public class ProgramService(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor  // ADD
) : IProgramService {
  
  public async Task<Program?> GetProgramByIdAsync(Guid id) {
    var tenantId = actorContextAccessor.ActorContext.TenantId;
    return await context.Set<Program>()
      .Where(p => p.DeletedAt == null)
      .Where(p => p.TenantId == tenantId || p.TenantId == null)  // ADD TENANT FILTER
      .FirstOrDefaultAsync(p => p.Id == id);
  }
```

**Impact**: All queries become tenant-scoped  
**Rollback**: Remove filter clause

---

#### 6.3 Fix FeatureFlagsController Identity Resolution

**What**: Replace stub methods with ActorContext

**Where**: [FeatureFlagsController.cs#L162-L178](apps/api/Source/Modules/GameGuild.Features/Controllers/FeatureFlagsController.cs#L162-L178)

**Why**: Currently returns null, breaking tenant-aware feature evaluation

**Change**:
```csharp
public class FeatureFlagsController(
    IFeatureFlagEvaluationService evaluationService,
    IActorContextAccessor actorContextAccessor,  // ADD
    ILogger<FeatureFlagsController> logger
) : ControllerBase
{
    private Guid? GetCurrentUserId() => 
        Guid.TryParse(actorContextAccessor.ActorContext.SubjectId, out var id) ? id : null;

    private Guid? GetCurrentTenantId() => 
        actorContextAccessor.ActorContext.TenantId;
```

---

### P1 - High (Required Before GA)

#### 6.4 Port Certificate Module from Legacy

**What**: Migrate `UserCertificate` entity and `IUserCertificateService` implementation

**Where**: `GameGuild.Learning.Certificates`

**Legacy Source**: `temp/api-b/Modules/Certificates/`

**Steps**:
1. Copy `UserCertificate.cs` entity with TenantId
2. Copy `IUserCertificateService.cs` interface
3. Create `UserCertificateService.cs` implementation with tenant validation
4. Create `CertificatesController.cs` with authorization attributes
5. Add EF Core configuration

---

#### 6.5 Add Tenant Validation to EnrollUserCommandHandler

**What**: Validate actor's tenant matches program's tenant

**Where**: [EnrollUserCommandHandler.cs](apps/api/Source/Modules/GameGuild.Learning.Courses/Commands/EnrollUser/EnrollUserCommandHandler.cs)

**Change**:
```csharp
public class EnrollUserCommandHandler(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor,  // ADD
    ILogger<EnrollUserCommandHandler> logger
) : ICommandHandler<EnrollUserCommand, ProgramUser>
{
    public async Task<ProgramUser> Handle(EnrollUserCommand request, CancellationToken cancellationToken) {
        var actorContext = actorContextAccessor.ActorContext;
        if (actorContext.TenantId == null)
            throw new UnauthorizedAccessException("Tenant context required");
        
        var program = await context.Set<Program>()
            .Where(p => p.Id == request.ProgramId && p.DeletedAt == null)
            .Where(p => p.TenantId == actorContext.TenantId || p.TenantId == null)  // ADD
            .FirstOrDefaultAsync(cancellationToken);
```

---

### P2 - Medium (Improve Quality)

#### 6.6 Add Global Tenant Query Filter

**What**: Configure EF Core to automatically filter by TenantId

**Where**: `ApplicationDbContext` configuration

**Change**: (in DbContext OnModelCreating)
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply tenant filter to all ITenantScoped entities
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantProperty = Expression.Property(parameter, nameof(ITenantScoped.TenantId));
            var tenantId = Expression.Property(
                Expression.Constant(_tenantContextAccessor), nameof(ITenantContext.TenantId));
            var filter = Expression.Lambda(
                Expression.OrElse(
                    Expression.Equal(tenantProperty, Expression.Constant(null, typeof(Guid?))),
                    Expression.Equal(tenantProperty, tenantId)),
                parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
```

---

#### 6.7 Add Rate Limiting to Feature Evaluation

**What**: Limit bulk evaluation requests

**Where**: [FeatureFlagsController.cs](apps/api/Source/Modules/GameGuild.Features/Controllers/FeatureFlagsController.cs)

**Change**:
```csharp
[HttpPost(":evaluate-bulk")]
[EnableRateLimiting("feature-evaluation")]  // ADD
public async Task<IActionResult> BulkEvaluateFeatures([FromBody] BulkEvaluationRequest request, ...)
{
    if (request.FeatureKeys.Count > 100)  // ADD VALIDATION
        return BadRequest(new { error = "Maximum 100 feature keys per request" });
```

---

## 7) Legacy Port Plan

### Source: `temp/api-b/Modules/Certificates/`

| Legacy File | Target Location | Action | Notes |
|-------------|-----------------|--------|-------|
| `Entities/Certificate.cs` | Rename to `CertificateTemplate.cs` | ADAPT | Already exists in new code (simpler) |
| `Entities/UserCertificate.cs` | `Learning.Certificates/Entities/IssuedCertificate.cs` | PORT | Rename to avoid confusion |
| `Entities/CertificateTag.cs` | `Learning.Certificates/Entities/CertificateTag.cs` | PORT | Keep as-is |
| `Interfaces/IUserCertificateService.cs` | `Learning.Certificates/Abstractions/` | PORT | Update namespace |
| `Interfaces/ICertificateService.cs` | `Learning.Certificates/Abstractions/` | PORT | Update namespace |
| `Controllers/ProgramCertificatesController.cs` | `Learning.Certificates/Controllers/` | ADAPT | Update authorization attributes |
| `Models/UserCertificateConfiguration.cs` | `Learning.Certificates/Configuration/` | PORT | Update namespaces |

### Migration Steps

1. **Create feature branch**: `feat/learning-certificates-port`

2. **Port entities first** (no dependencies):
   ```bash
   cp temp/api-b/Modules/Certificates/Entities/UserCertificate.cs \
      apps/api/Source/Modules/GameGuild.Learning.Certificates/Entities/IssuedCertificate.cs
   ```

3. **Update namespaces**:
   - `GameGuild.Modules.Certificates` → `GameGuild.Learning.Certificates`
   - `GameGuild.Modules.Programs` → `GameGuild.Learning.Courses`

4. **Add IActorContextAccessor injection** to all services

5. **Add tenant validation** to all queries:
   ```csharp
   .Where(c => c.TenantId == actorContext.TenantId || c.TenantId == null)
   ```

6. **Enable authorization attributes** (not commented out)

7. **Register in DI**: Create `CertificatesModule.cs` extension

8. **Add EF Core migrations** for new tables

### Security Parity Checklist

Before merging legacy port:

- [ ] All entities have `TenantId` property indexed
- [ ] All services inject `IActorContextAccessor`
- [ ] All queries filter by `TenantId`
- [ ] All controllers have `[Authorize]` attribute
- [ ] All modifying endpoints have `[RequireResourcePermission<...>]`
- [ ] All handlers validate `ActorContext.TenantId != null`
- [ ] Verification endpoint does NOT require authentication (public)
- [ ] Revocation endpoint requires admin permission
- [ ] Audit logging added for issuance and revocation

---

## 8) Test Plan

### Tenant Isolation Tests

| Test Name | Type | Proves |
|-----------|------|--------|
| `GetProgram_ReturnsNotFound_WhenAccessingOtherTenantProgram` | Integration | Cross-tenant access denied |
| `CreateProgram_AssignsTenantId_FromActorContext` | Integration | Tenant attribution correct |
| `EnrollUser_Fails_WhenProgramInDifferentTenant` | Integration | Cross-tenant enrollment blocked |
| `FeatureEvaluation_FailsClosed_WhenTenantTargetingNoContext` | Unit | Fail-closed behavior (exists) |
| `BulkEvaluate_DoesNotLeakFeatures_AcrossTenants` | Integration | No tenant-specific feature leakage |

### Authentication Fail-Closed Tests

| Test Name | Type | Proves |
|-----------|------|--------|
| `ProgramController_Returns401_WhenNoToken` | Integration | Unauthenticated access denied |
| `ProgramContentController_Returns401_WhenExpiredToken` | Integration | Expired tokens rejected |
| `FeatureEvaluation_Returns401_WhenInvalidToken` | Integration | Invalid identity rejected |

### Authorization Bypass Tests

| Test Name | Type | Proves |
|-----------|------|--------|
| `UpdateProgram_Returns403_WhenUserLacksEditPermission` | Integration | Permission check works |
| `DeleteProgram_Returns403_WhenNotOwnerOrAdmin` | Integration | Resource-level check works |
| `EnrollUser_Succeeds_WhenProgramIsOpenEnrollment` | Integration | Open enrollment allows |
| `EnrollUser_Fails_WhenProgramRequiresApproval` | Integration | Closed enrollment blocked |

### IDOR Tests

| Test Name | Type | Proves |
|-----------|------|--------|
| `GetUserProgress_Fails_WhenAccessingOtherUserProgress` | Integration | Cannot view others' progress |
| `UpdateUserProgress_Fails_WhenNotSelfOrInstructor` | Integration | Cannot modify others' progress |
| `GetActivityGrade_Fails_WhenNotStudentOrGrader` | Integration | Grades protected |

### Concurrency / Idempotency Tests

| Test Name | Type | Proves |
|-----------|------|--------|
| `EnrollUser_IsIdempotent_WhenCalledTwice` | Integration | No duplicate enrollments |
| `UpdateProgress_HandlesRaceCondition_WithOptimisticConcurrency` | Integration | Concurrent updates handled |
| `MarkContentCompleted_IsIdempotent` | Unit | Repeated completion safe |

### Legacy Port Verification Tests

| Test Name | Type | Proves |
|-----------|------|--------|
| `IssueCertificate_RequiresCompletedEnrollment` | Integration | Certificate prerequisites |
| `IssueCertificate_AssignsTenantId` | Integration | Tenant attribution |
| `VerifyCertificate_WorksWithoutAuth` | Integration | Public verification |
| `RevokeCertificate_RequiresAdminPermission` | Integration | Revocation protected |
| `ExpiredCertificate_ReturnsInvalid` | Unit | Expiration logic |

---

## 9) Final Report

### Executive Summary

The `GameGuild.Features` and `GameGuild.Learning.*` modules represent a solid foundation for feature management and educational content delivery. However, **critical security gaps** exist that must be addressed before production deployment:

1. **Authorization is disabled** on all Learning endpoints (commented out)
2. **Tenant isolation is incomplete** — no global query filters
3. **Certificate workflow is missing** — prevents MVP completion

### Top 10 Issues (Severity Order)

| Rank | Issue | Severity | Effort | Impact |
|------|-------|----------|--------|--------|
| 1 | ProgramController missing `[Authorize]` | HIGH | 5 min | Unauthenticated access |
| 2 | All permission attributes commented out | HIGH | 30 min | Unauthorized access |
| 3 | No tenant filter in queries | HIGH | 2 hours | Cross-tenant data leak |
| 4 | Services don't use ActorContext | HIGH | 4 hours | No audit, no tenant validation |
| 5 | EnrollUser no tenant validation | HIGH | 30 min | Cross-tenant enrollment |
| 6 | GetCurrentTenantId returns null | HIGH | 15 min | Feature targeting broken |
| 7 | IDOR on user progress endpoints | HIGH | 1 hour | Privacy violation |
| 8 | Certificate module incomplete | MEDIUM | 1 day | MVP feature missing |
| 9 | No rate limiting on bulk evaluation | MEDIUM | 30 min | DoS risk |
| 10 | Commented code blocks | LOW | 1 hour | Maintenance burden |

### 30/60/90-Day Roadmap

#### 30 Days (Critical Security)

- [ ] Enable `[Authorize]` on ProgramController
- [ ] Uncomment all permission attributes
- [ ] Inject `IActorContextAccessor` into all services
- [ ] Add tenant filter to `ProgramService` queries
- [ ] Fix `GetCurrentTenantId()` in FeatureFlagsController
- [ ] Add tenant validation to `EnrollUserCommandHandler`
- [ ] Add ownership check to progress endpoints
- [ ] Write tenant isolation integration tests

#### 60 Days (Feature Completion)

- [ ] Port Certificate module from legacy
- [ ] Implement `IssueCertificate` flow
- [ ] Implement `VerifyCertificate` flow
- [ ] Add global tenant query filter to DbContext
- [ ] Add rate limiting to feature evaluation
- [ ] Remove/complete commented code blocks
- [ ] Write certificate integration tests

#### 90 Days (Hardening)

- [ ] Implement `Cohorts` module for group enrollment
- [ ] Implement `LearningPaths` for curated sequences
- [ ] Add audit logging via `ISecurityAuditLogger`
- [ ] Performance testing for bulk operations
- [ ] FERPA compliance review for educational data
- [ ] Security penetration testing

### Go/No-Go Assessment

**Decision: NO-GO** ❌

**Blockers** (Must fix before deployment):

1. ⛔ `ProgramController` allows unauthenticated access
2. ⛔ All authorization attributes are commented out
3. ⛔ No tenant isolation in database queries
4. ⛔ `GetCurrentTenantId()` returns null, breaking feature targeting

**Estimated time to Go**: 2-3 days of focused security work

---

## Appendix: Code Locations Reference

| Component | Path |
|-----------|------|
| FeatureFlag Entity | `apps/api/Source/Modules/GameGuild.Features/Entities/FeatureFlag.cs` |
| FeatureFlagsController | `apps/api/Source/Modules/GameGuild.Features/Controllers/FeatureFlagsController.cs` |
| TenantTargetingHandler | `apps/api/Source/Modules/GameGuild.Features/Services/Handlers/TenantTargetingHandler.cs` |
| Program Entity | `apps/api/Source/Modules/GameGuild.Learning.Courses/Entities/Program.cs` |
| ProgramController | `apps/api/Source/Modules/GameGuild.Learning.Courses/Controllers/ProgramController.cs` |
| ProgramService | `apps/api/Source/Modules/GameGuild.Learning.Courses/Services/ProgramService.cs` |
| EnrollUserHandler | `apps/api/Source/Modules/GameGuild.Learning.Courses/Commands/EnrollUser/EnrollUserCommandHandler.cs` |
| Certificate Entity (new) | `apps/api/Source/Modules/GameGuild.Learning.Certificates/Entities/Certificate.cs` |
| Certificate Entity (legacy) | `temp/api-b/Modules/Certificates/Entities/Certificate.cs` |
| UserCertificate (legacy) | `temp/api-b/Modules/Certificates/Entities/UserCertificate.cs` |
| ActorContext | `apps/api/Source/Modules/GameGuild.Identity.Context/Actors/ActorContext.cs` |
| EntityBase | `apps/api/Source/Modules/GameGuild.SharedKernel/Entities/EntityBase.cs` |

---

*Report generated: January 17, 2026*  
*Next review: After P0 fixes implemented*

# Authorization Flow Security Audit - Deep Analysis

**Date**: January 13, 2026  
**Auditor**: Security Architecture Review  
**Version**: 2.0  
**Status**: ✅ CRITICAL ISSUES FIXED  
**Scope**: Authorization Precedence, Deny Semantics, Tenant Isolation

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current Flow Diagram](#current-flow-diagram)
3. [Effective Precedence Rules (As Implemented)](#effective-precedence-rules-as-implemented)
4. [Correctness Check Against Requirements](#correctness-check-against-requirements)
5. [Counterexamples / Attack Scenarios](#counterexamples--attack-scenarios)
6. [Recommended Design (Fix Plan)](#recommended-design-fix-plan)
7. [Unit & Integration Test Plan](#unit--integration-test-plan)
8. [Code Smell Findings](#code-smell-findings)

---

## Executive Summary

### Critical Findings

| Finding | Severity | Impact | Status |
|---------|----------|--------|--------|
| **No DAC Deny Support in TenantPermission** | 🔴 CRITICAL | Tenants cannot prohibit globally-allowed permissions | ✅ **FIXED** |
| **ActorContextMiddleware Not Registered** | 🔴 CRITICAL | ActorContext not populated in request pipeline | ✅ **FIXED** |
| **ALLOW-WINS Only Permission Model** | 🔴 HIGH | Global permissions leak into tenants unconditionally | ✅ **FIXED** |
| **Missing Fail-Closed on Null TenantId in PermissionQueryService** | 🔴 HIGH | Permission resolution with null tenant returns global defaults | ✅ **FIXED** |
| **Dual Permission Systems (TenantPermission vs ACL)** | 🟡 MEDIUM | Clarified: TenantPermission=tenant-level ops, ACL=resource-level access | ✅ **FIXED** |
| **Cache Key Missing User Security Version** | 🟡 MEDIUM | User-level permission changes may not invalidate properly | ✅ **FIXED** |
| **Magic GUID for System Account** | 🟡 MEDIUM | Hard-coded system account ID in code | ✅ **FIXED** |

### Fix Summary (January 13, 2026)

The following critical security fixes were implemented:

1. **TenantPermission.cs**: Added `DenyPermissions` field with `HasDenyPermission()`, `HasEffectivePermission()`, `AddDenyPermissions()`, and `RemoveDenyPermissions()` methods.

2. **PipelineExtensions.cs**: Registered `ActorContextMiddleware` via `app.UseActorContext()` after authentication and before authorization.

3. **FocusedPermissionServices.cs**: Replaced ALLOW-WINS additive algorithm with DENY-WINS subtraction algorithm (`EffectivePermissions = AllowSet - DenySet`).

4. **FocusedPermissionServices.cs**: Added fail-closed behavior - when `tenantId` is null, returns empty permissions instead of global defaults.

5. **CachedAccessControlListService.cs**: Added `IUserSecurityVersionStore` to cache key for proper user-level cache invalidation. Cache keys now include both `tv{tenantVersion}` and `uv{userVersion}`.

6. **EffectivePermissionResolverService.cs**: Moved magic GUID for system account to `AuthorizationOptions.SystemAccountId` configuration property.

7. **AuthorizationBehavior.cs**: Fixed dual permission system confusion - now properly uses `IAccessControlListService` for resource-level checks (when `ResourceType` is specified) and `IPermissionService` for tenant-level permission checks. Added `IAccessControlListService` dependency injection and `MapPermissionToAccessLevel()` helper.

### System Overview

The GameGuild authorization system implements a **four-layer architecture**:

1. **Authentication** → JWT validation
2. **Rule-Based (RBAC)** → AND-logic policy gates
3. **ABAC Policies** → Deny-wins attribute evaluation
4. **DAC Permissions** → ✅ **DENY-WINS** (deny takes precedence over allow)

~~**Key Architectural Gap**: The DAC layer (Layer 4) only supports **additive allow** permissions. There is **no mechanism for tenants to deny/prohibit globally-allowed permissions** in the `TenantPermission` entity.~~

**FIXED**: The `TenantPermission` entity now supports `DenyPermissions` field. Permission evaluation uses `EffectivePermissions = AllowSet - DenySet`. Tenants can now prohibit globally-allowed permissions.

> **Permission System Clarification**:
> 
> | System | Scope | Purpose | Example |
> |--------|-------|---------|--------|
> | **TenantPermission** | Tenant-level | Controls what **operations** a user can perform within a tenant | `courses:create`, `projects:delete`, `users:manage` |
> | **AccessControlList (ACL)** | Resource-level | Controls **access to specific resources** | User X has `ReadWrite` access to Course #123 |
>
> These are **complementary systems**, not redundant:
> - `TenantPermission` answers: "Can this user create courses in this tenant?"
> - `ACL` answers: "Can this user edit THIS specific course?"
>
> Both use DENY-WINS semantics. A user needs both the tenant permission AND resource access to perform an action.

---

## Dual Permission Systems Architecture

### TenantPermission vs ACL: When to Use Which

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    AUTHORIZATION DECISION FLOW                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  User wants to: "Edit Course #123 in Tenant ABC"                        │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │ STEP 1: TENANT PERMISSION CHECK                                  │   │
│  │ ────────────────────────────────────────────────────────────────│   │
│  │ Q: "Does user have 'courses:edit' permission in Tenant ABC?"    │   │
│  │                                                                   │   │
│  │ Source: TenantPermission entity                                  │   │
│  │ Service: PermissionQueryService.GetEffectivePermissionsAsync()   │   │
│  │                                                                   │   │
│  │ Layers checked:                                                   │   │
│  │   1. Global defaults (TenantId=null, UserId=null)                │   │
│  │   2. Tenant defaults (TenantId=ABC, UserId=null)                 │   │
│  │   3. Direct grants (TenantId=ABC, UserId=user)                   │   │
│  │                                                                   │   │
│  │ Result: EffectivePermissions = AllowSet - DenySet (DENY-WINS)    │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                              │                                          │
│                    ┌─────────┴─────────┐                               │
│                    ▼                   ▼                               │
│              ✅ Has Permission    ❌ No Permission                     │
│                    │                   │                               │
│                    ▼                   ▼                               │
│  ┌────────────────────────────┐  ┌─────────────────┐                   │
│  │ STEP 2: RESOURCE ACL CHECK │  │ 403 Forbidden   │                   │
│  │ ─────────────────────────  │  │ "Missing        │                   │
│  │ Q: "Does user have access  │  │  permission:    │                   │
│  │     to Course #123?"       │  │  courses:edit"  │                   │
│  │                            │  └─────────────────┘                   │
│  │ Source: AccessControlList  │                                        │
│  │ Service: ACLService.       │                                        │
│  │   EvaluateAccessAsync()    │                                        │
│  │                            │                                        │
│  │ Checks:                    │                                        │
│  │   • Explicit user grants   │                                        │
│  │   • Role-based grants      │                                        │
│  │   • Group-based grants     │                                        │
│  │   • Deny entries (first)   │                                        │
│  └────────────────────────────┘                                        │
│                 │                                                      │
│       ┌─────────┴─────────┐                                           │
│       ▼                   ▼                                           │
│  ✅ Has Access       ❌ No Access                                     │
│       │                   │                                           │
│       ▼                   ▼                                           │
│  ┌──────────────┐  ┌─────────────────┐                                │
│  │ 200 OK       │  │ 403 Forbidden   │                                │
│  │ Edit allowed │  │ "Access denied  │                                │
│  └──────────────┘  │  to resource"   │                                │
│                    └─────────────────┘                                │
└─────────────────────────────────────────────────────────────────────────┘
```

### Summary Table

| Aspect | TenantPermission | AccessControlList (ACL) |
|--------|------------------|-------------------------|
| **Scope** | Tenant-level operations | Resource-level access |
| **Question Answered** | "Can user do X in tenant?" | "Can user access resource Y?" |
| **Entity** | `TenantPermission.cs` | `AccessControlListEntry.cs` |
| **Primary Service** | `PermissionQueryService` | `AccessControlListService` |
| **Deny Support** | ✅ `DenyPermissions[]` | ✅ `IsDenied` flag |
| **Evaluation** | DENY-WINS subtraction | Deny-first priority |
| **Caching** | Version-based (tenant + user) | Version-based (tenant + user) |
| **Examples** | `courses:create`, `users:manage` | `ReadWrite` on Course #123 |

---

## Current Flow Diagram

### Request Processing Pipeline

```
HTTP Request
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 1. TenantMiddleware                                             │
│    File: Identity.Tenants/Middleware/TenantMiddleware.cs        │
│    • Resolves tenant from X-Tenant-Id header / domain / query   │
│    • Validates user is member of tenant                         │
│    • Stores tenant in HttpContext.Items["CurrentTenant"]        │
│    • Stores tenantId in HttpContext.Items["AuthorizationTenantId"]
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. UseAuthentication() (ASP.NET Core Built-in)                  │
│    • Validates JWT token                                        │
│    • Populates HttpContext.User (ClaimsPrincipal)               │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ ⚠️ MISSING: ActorContextMiddleware NOT IN PIPELINE              │
│    File: Authorization/Middleware/ActorContextMiddleware.cs     │
│    Extension: UseActorContext() exists but NOT called in        │
│               PipelineExtensions.cs                             │
│    Impact: ActorContext never populated from claims + tenant    │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. UseAuthorization() (ASP.NET Core Built-in)                   │
│    • Triggers policy evaluation via AuthorizationHandler        │
│    • Calls PermissionHandler for [RequirePermission] attributes │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. PermissionHandler.HandleRequirementAsync()                   │
│    File: Authorization/Handlers/PermissionHandler.cs            │
│    • First checks claims (if AllowClaimsBased = true)           │
│    • Falls back to IAuthorizationPermissionService.HasPermission│
│    • Uses IAuthorizationTenantContext for tenant resolution     │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. AuthorizationPermissionServiceAdapter.HasPermissionAsync()   │
│    File: Authorization/Services/                                │
│          AuthorizationPermissionServiceAdapter.cs               │
│    • Delegates to IPermissionQueryService                       │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. PermissionQueryService.GetEffectivePermissionsAsync()        │
│    File: Authorization/Services/FocusedPermissionServices.cs    │
│    ⚠️ ALLOW-WINS ONLY - NO DENY SUPPORT                         │
│                                                                 │
│    Layer 1: Global defaults (UserId=null, TenantId=null)        │
│    Layer 2: Tenant defaults (UserId=null, TenantId=X)           │
│    Layer 3: Direct grants (UserId=Y, TenantId=X)                │
│                                                                 │
│    Algorithm: EffectivePermissions = Union(Global, Tenant, Direct)
│               return allPermissions.Distinct().ToList();        │
└─────────────────────────────────────────────────────────────────┘
```

### Key Classes and Responsibilities

| Class | File | Responsibility |
|-------|------|----------------|
| `TenantMiddleware` | `Identity.Tenants/Middleware/TenantMiddleware.cs` | Tenant resolution & membership validation |
| `ActorContextMiddleware` | `Identity.Authorization/Middleware/ActorContextMiddleware.cs` | Build ActorContext (⚠️ NOT REGISTERED) |
| `PermissionHandler` | `Identity.Authorization/Handlers/PermissionHandler.cs` | ASP.NET AuthorizationHandler |
| `AuthorizationPermissionServiceAdapter` | `Identity.Authorization/Services/AuthorizationPermissionServiceAdapter.cs` | Bridge to permission query |
| `PermissionQueryService` | `Identity.Authorization/Services/FocusedPermissionServices.cs` | Permission resolution (allow-only) |
| `EffectivePermissionResolverService` | `Identity.Authorization/Services/EffectivePermissionResolverService.cs` | Alternative resolver (also allow-only) |
| `DatabaseAccessControlListService` | `Identity.Authorization/Services/DatabaseAccessControlListService.cs` | ACL with deny-first algorithm |
| `HttpAuthorizationTenantContext` | `Identity.Authorization/Services/HttpAuthorizationTenantContext.cs` | Tenant context from HttpContext |

### TenantId Propagation Path

```
X-Tenant-Id Header
    │
    ▼ TenantMiddleware.InvokeAsync()
    │
HttpContext.Items["AuthorizationTenantId"] = tenant.Id
    │
    ▼ HttpAuthorizationTenantContext.TenantId (getter)
    │
Reads from HttpContext.Items["AuthorizationTenantId"]
    │
    ▼ PermissionHandler.TryGetUserAndTenantIds()
    │
Uses _tenantContext.TenantId
    │
    ▼ PermissionQueryService.GetEffectivePermissionsAsync(userId, tenantId)
```

---

## Effective Precedence Rules (As Implemented)

### Pseudocode Algorithm (Current Implementation)

```python
def get_effective_permissions(user_id: Guid, tenant_id: Guid?) -> Set[str]:
    """
    Current implementation: ALLOW-WINS ONLY
    Location: FocusedPermissionServices.cs, PermissionQueryService.GetEffectivePermissionsAsync()
    Lines: 232-267
    """
    all_permissions = set()
    
    # Layer 1: Global defaults (UserId=null, TenantId=null)
    global_defaults = db.TenantPermissions
        .where(user_id=None, tenant_id=None)
        .select(permissions)
    all_permissions.update(global_defaults)
    
    # Layer 2: Tenant defaults (UserId=null, TenantId=tenant_id)
    if tenant_id is not None:
        tenant_defaults = db.TenantPermissions
            .where(user_id=None, tenant_id=tenant_id)
            .select(permissions)
        all_permissions.update(tenant_defaults)
    
    # Layer 3: Direct user grants (UserId=user_id, TenantId=tenant_id OR null)
    user_permissions = db.TenantPermissions
        .where(user_id=user_id)
        .where(tenant_id=tenant_id OR tenant_id=None)
        .where(not expired)
        .select(permissions)
    all_permissions.update(user_permissions)
    
    # ⚠️ NO DENY PROCESSING - All permissions are additive
    return all_permissions.distinct()


def has_permission(user_id, tenant_id, permission) -> bool:
    """
    Current implementation: Simple set membership
    Location: FocusedPermissionServices.cs, PermissionQueryService.HasTenantPermissionAsync()
    """
    effective = get_effective_permissions(user_id, tenant_id)
    return permission in effective
```

### Precedence Table (As Implemented)

| Source | Priority | Effect | Can Override? |
|--------|----------|--------|---------------|
| Static permissions (system account) | 1 (highest) | ALLOW | No - hardcoded |
| RBAC role permissions | 2 | ALLOW | Via role removal |
| Global defaults | 3 | ALLOW | ⚠️ **NO** - always additive |
| Tenant defaults | 4 | ALLOW | ⚠️ **NO** - always additive |
| Direct user grants | 5 | ALLOW | Via revocation |

### ACL Layer (Separate System)

The ACL layer in `DatabaseAccessControlListService` **does implement deny-first**:

```python
def evaluate_acl_access(subject, tenant_id, resource_type, resource_id) -> AccessLevel:
    """
    Location: DatabaseAccessControlListService.EvaluateAccessAsync()
    Lines: 13-61
    ⚠️ This is a SEPARATE system from TenantPermission!
    """
    entries = get_acl_entries(tenant_id, resource_type, resource_id, subject.principals)
    
    deny_entries = [e for e in entries if e.is_denied and e.is_effective]
    allow_entries = [e for e in entries if not e.is_denied and e.is_effective]
    
    # DENY-FIRST: Any deny blocks access
    if deny_entries:
        highest_deny = max(e.access_level for e in deny_entries)
        if highest_deny == AccessLevel.None:
            return AccessLevel.None
        if not allow_entries:
            return AccessLevel.None
        highest_allow = max(e.access_level for e in allow_entries)
        return min(highest_allow, highest_deny - 1)
    
    # No denies - return highest allow
    return max(e.access_level for e in allow_entries) if allow_entries else AccessLevel.None
```

**Critical Distinction**: 
- `AccessControlListEntry` (ACL): Resource-level, has `IsDenied` flag, uses deny-first
- `TenantPermission`: Tenant-scoped RBAC, **NO deny support**, uses allow-only

---

## Correctness Check Against Requirements

### Requirement 1: Tenant-Scoped RBAC

| Sub-Requirement | Status | Evidence |
|-----------------|--------|----------|
| Role → Permissions evaluated within tenant context | 🟡 PARTIAL | `RbacPermissionResolver.ResolvePermissionsAsync()` takes `tenantId` parameter |
| Admin in Tenant A ≠ Admin in Tenant B | ✅ PASS | `DynamicRoleAssignmentRepository.GetValidByUserAsync()` filters by `TenantId` |
| Cache keys include TenantId | ✅ PASS | `CachedAccessControlListService.BuildCacheKey()` includes `tenantId` |
| Cache keys include SecurityVersion | ✅ PASS | Cache keys include `v{version}` suffix |

**Overall: 🟡 PARTIAL PASS**

**Evidence**:
- `RbacPermissionResolver.cs` lines 185-240: Filters assignments by `tenantId`
- `CachedAccessControlListService.cs` line 287: Key format `acl:{tenantId}:{userId}:...:v{version}`

---

### Requirement 2: Global Defaults vs Tenant Restrictions

| Sub-Requirement | Status | Evidence |
|-----------------|--------|----------|
| Global allow must NOT force-allow in tenant that prohibits it | 🔴 **FAIL** | No deny mechanism in `TenantPermission` |
| Tenants MUST be able to reject globally-allowed permissions | 🔴 **FAIL** | No `DenyPermissions` field exists |
| Confirm deny exists | 🔴 **FAIL** | `TenantPermission` entity only has `Permissions` array |
| Conflict resolution defined | 🔴 **FAIL** | Only additive union, no conflict resolution |

**Overall: 🔴 CRITICAL FAIL**

**Evidence**:
- `TenantPermission.cs` lines 27-32: Only `Permissions string[]` exists
- `FocusedPermissionServices.cs` line 265: `return allPermissions.Distinct().ToList();` - no deny subtraction

---

### Requirement 3: Tenant Defaults

| Sub-Requirement | Status | Evidence |
|-----------------|--------|----------|
| Each tenant defines its own defaults | ✅ PASS | `TenantPermission` with `UserId=null, TenantId=X` |
| Tenant defaults apply even with no direct grants | ✅ PASS | `GetTenantDefaultPermissionsAsync()` called independently |
| Merge semantics with global defaults defined | ✅ PASS | Documented in `PERMISSION_EVALUATION_POLICY.md` |

**Overall: ✅ PASS**

---

### Requirement 4: Direct User Grants

| Sub-Requirement | Status | Evidence |
|-----------------|--------|----------|
| Users can have direct allow | ✅ PASS | `TenantPermission` with specific `UserId` |
| Users can have direct deny | 🔴 **FAIL** | No deny mechanism |
| Define interaction with RBAC + tenant defaults | 🟡 PARTIAL | Allow-wins documented, deny undefined |

**Overall: 🔴 FAIL**

---

## Counterexamples / Attack Scenarios

### Attack 1: Cross-Tenant Global Permission Leak

**Scenario**: A user granted global permissions retains them across all tenants regardless of tenant policies.

```
Setup:
- Global default: ["content:read", "profile:read"]
- Tenant A wants to deny "content:read" for compliance reasons

Attack:
1. User joins Tenant A
2. User inherits "content:read" from global defaults
3. Tenant A has NO way to prohibit this permission

Expected Behavior: Tenant A should be able to deny "content:read"
Actual Behavior: User retains "content:read" unconditionally

Impact: Tenants cannot enforce data isolation or compliance policies
```

**Code Path**:
```csharp
// FocusedPermissionServices.cs line 252-265
var allPermissions = new List<string>();
allPermissions.AddRange(globalDefaults);      // ["content:read", "profile:read"]
allPermissions.AddRange(tenantDefaults);      // [tenant's allows]
// ⚠️ Even if tenant wants to DENY "content:read", there's no mechanism
return allPermissions.Distinct().ToList();    // "content:read" still present
```

---

### Attack 2: Privilege Escalation via Stale Cache

**Scenario**: A user's permissions are revoked, but they continue to have access.

```
Setup:
- User has "admin:*" in Tenant A
- Admin revokes user's permission
- Cache TTL: 300 seconds

Attack Timeline:
T+0:    User authenticates, permissions cached
T+30:   Admin revokes "admin:*" via GrantService.RevokeTenantPermissionAsync()
T+30:   SecurityVersion incremented (good)
T+31:   User makes request
T+31:   IF cache key doesn't match new version → cache miss → correct behavior
T+31:   IF L1 cache has stale entry with matching key → privilege retained

Expected Behavior: Immediate cache invalidation
Actual Behavior: Version-based invalidation works, but race window exists

Mitigation Present: TenantSecurityVersion IS incremented on revocation
Residual Risk: L1 memory cache may have stale entries until proactive eviction
```

---

### Attack 3: Missing Tenant Fail-Closed

**Scenario**: Request without tenant context gets global permissions.

```
Setup:
- Global defaults: ["profile:read", "profile:update"]
- Endpoint should require tenant context

Attack:
1. Attacker crafts request WITHOUT X-Tenant-Id header
2. TenantMiddleware finds no tenant, continues without setting context
3. PermissionQueryService called with tenantId=null
4. GetEffectivePermissionsAsync(userId, null) returns global defaults only
5. If "profile:update" is checked → GRANTED from global defaults

Expected Behavior: No tenant = no permissions (fail-closed)
Actual Behavior: No tenant = global defaults apply
```

**Code Path**:
```csharp
// TenantMiddleware.InvokeAsync() line 149
else
{
    // No tenant resolved - continue without tenant context
    logger.LogDebug("No tenant resolved for request to {Path}", path);
    await next(context);  // ⚠️ Request continues without tenant!
}

// PermissionQueryService.GetEffectivePermissionsAsync() line 249
var allPermissions = new List<string>();
allPermissions.AddRange(globalDefaults);  // ⚠️ Always added
if (tenantId.HasValue)  // FALSE when tenant missing
{
    // tenant defaults skipped
}
return allPermissions.Distinct().ToList();  // Returns global defaults!
```

---

### Attack 4: ActorContext Not Populated

**Scenario**: ActorContextMiddleware is not registered in the pipeline.

```
Evidence:
- PipelineExtensions.cs does NOT call app.UseActorContext()
- ActorContextMiddleware.cs exists but is never used

Impact:
1. IActorContextAccessor.ActorContext may return Anonymous or stale data
2. Code paths that use ActorContext will behave unpredictably
3. Authorization decisions may use wrong context

Expected Behavior: ActorContext populated on each request
Actual Behavior: ActorContext never set by middleware
```

**File Reference**:
```csharp
// PipelineExtensions.cs lines 66-76
app.UseTenantResolution();
app.UseAuthentication();
// ⚠️ MISSING: app.UseActorContext();
app.UseAuthorization();
```

---

### Attack 5: Permission System Confusion

**Scenario**: Developer uses TenantPermission instead of ACL, bypassing deny controls.

```
Setup:
- ACL has deny entry for User X on Resource Y
- Developer checks TenantPermission for "resource:read" instead

Attack:
1. Security team adds ACL deny for User X
2. Developer's code uses IPermissionQueryService.HasTenantPermissionAsync()
3. TenantPermission has no deny entries
4. Permission check passes
5. User X accesses resource despite ACL deny

Expected: Single unified system
Actual: Two systems with different semantics

Root Cause: No clear guidance on when to use which system
```

---

## Recommended Design (Fix Plan)

### Corrected Precedence Table

```
┌──────────────────────────────────────────────────────────────────┐
│                    DENY SOURCES (Evaluated First)                │
├──────────────────────────────────────────────────────────────────┤
│ Priority 1: Direct User Deny (userId=Y, tenantId=X, isDeny=true) │
│ Priority 2: Tenant Deny (userId=null, tenantId=X, isDeny=true)   │
│ Priority 3: Global Deny (userId=null, tenantId=null, isDeny=true)│
├──────────────────────────────────────────────────────────────────┤
│                   ALLOW SOURCES (After Deny Check)               │
├──────────────────────────────────────────────────────────────────┤
│ Priority 4: Direct User Allow                                    │
│ Priority 5: RBAC Role Permissions                                │
│ Priority 6: Tenant Default Allow                                 │
│ Priority 7: Global Default Allow                                 │
└──────────────────────────────────────────────────────────────────┘

Effective Permission Rule:
  permission ∈ EffectivePermissions 
  IFF 
  permission ∈ AllowSet AND permission ∉ DenySet
```

### Phase 1: Add Deny Support to TenantPermission (CRITICAL)

**File**: `Identity.Authorization/Entities/TenantPermission.cs`

```csharp
// ADD these fields after line 32:

/// <summary>
///     Array of denied permission strings. These take precedence over allows.
/// </summary>
[Column(TypeName = "text[]")]
public string[] DenyPermissions { get; set; } = Array.Empty<string>();

/// <summary>
///     Check if a specific permission is denied.
/// </summary>
public bool HasDenyPermission(string permission)
{
    return DenyPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
///     Add deny permissions.
/// </summary>
public void AddDenyPermissions(params string[] permissions)
{
    var current = DenyPermissions.ToList();
    foreach (var perm in permissions)
    {
        if (!current.Contains(perm, StringComparer.OrdinalIgnoreCase))
            current.Add(perm);
    }
    DenyPermissions = current.ToArray();
}
```

**File**: `Identity.Authorization/Services/FocusedPermissionServices.cs`

Replace `GetEffectivePermissionsAsync()` (lines 232-267):

```csharp
public async Task<List<string>> GetEffectivePermissionsAsync(
    Guid userId,
    Guid? tenantId,
    CancellationToken cancellationToken = default)
{
    // FAIL-CLOSED: No tenant = no permissions
    if (!tenantId.HasValue)
    {
        return new List<string>();
    }

    var allowPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var denyPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // STEP 1: Collect ALL deny permissions first (highest priority)
    
    // Direct user denies (Priority 1)
    var userDenies = await repository.GetDenyPermissionsByUserAsync(userId, tenantId.Value, cancellationToken);
    denyPermissions.UnionWith(userDenies);
    
    // Tenant denies (Priority 2)
    var tenantDenies = await repository.GetTenantDenyPermissionsAsync(tenantId.Value, cancellationToken);
    denyPermissions.UnionWith(tenantDenies);
    
    // Global denies (Priority 3)
    var globalDenies = await repository.GetGlobalDenyPermissionsAsync(cancellationToken);
    denyPermissions.UnionWith(globalDenies);

    // STEP 2: Collect allow permissions
    var globalDefaults = await GetGlobalDefaultPermissionsAsync(cancellationToken);
    allowPermissions.UnionWith(globalDefaults);

    var tenantDefaults = await GetTenantDefaultPermissionsAsync(tenantId.Value, cancellationToken);
    allowPermissions.UnionWith(tenantDefaults);

    var userPermissions = await repository.GetByUserAsync(userId, cancellationToken);
    var directGrants = userPermissions
        .Where(p => p.TenantId == tenantId)
        .Where(p => !p.ExpiresAt.HasValue || p.ExpiresAt.Value > DateTime.UtcNow)
        .SelectMany(p => p.Permissions);
    allowPermissions.UnionWith(directGrants);

    // STEP 3: Subtract denies from allows
    allowPermissions.ExceptWith(denyPermissions);
    
    return allowPermissions.ToList();
}
```

### Phase 2: Register ActorContextMiddleware (CRITICAL)

**File**: `GameGuild.API/Core/Setup/PipelineExtensions.cs`

Add after line 71:

```csharp
// 16. Authentication (identify user from JWT/cookies)
app.UseAuthentication();

// 17. Actor Context (build ActorContext from claims + tenant) - ADD THIS
app.UseActorContext();

// 18. Authorization (enforce permissions after user is identified)
app.UseAuthorization();
```

### Phase 3: Add Repository Methods for Deny Queries

**File**: `Identity.Authorization/Repositories/ITenantPermissionRepository.cs`

Add interface methods:

```csharp
Task<IReadOnlyList<string>> GetDenyPermissionsByUserAsync(
    Guid userId, 
    Guid tenantId, 
    CancellationToken ct = default);

Task<IReadOnlyList<string>> GetTenantDenyPermissionsAsync(
    Guid tenantId, 
    CancellationToken ct = default);

Task<IReadOnlyList<string>> GetGlobalDenyPermissionsAsync(
    CancellationToken ct = default);
```

### Phase 4: Database Migration

```sql
-- Add DenyPermissions column
ALTER TABLE "TenantPermissions" 
ADD COLUMN "DenyPermissions" text[] DEFAULT '{}' NOT NULL;

-- Add index for deny permission lookups
CREATE INDEX "IX_TenantPermissions_DenyPermissions" 
ON "TenantPermissions" USING GIN ("DenyPermissions");
```

---

## Unit & Integration Test Plan

### Required Test Cases

#### Category 1: Cross-Tenant Role Isolation

```csharp
[Fact]
public async Task User_AdminInTenantA_NotAdminInTenantB()
{
    // Arrange
    var userId = Guid.NewGuid();
    var tenantA = Guid.NewGuid();
    var tenantB = Guid.NewGuid();
    
    await _grantService.GrantTenantPermissionAsync(userId, tenantA, ["admin:*"]);
    
    // Act
    var permsTenantA = await _queryService.GetEffectivePermissionsAsync(userId, tenantA);
    var permsTenantB = await _queryService.GetEffectivePermissionsAsync(userId, tenantB);
    
    // Assert
    permsTenantA.Should().Contain("admin:*");
    permsTenantB.Should().NotContain("admin:*");
}
```

#### Category 2: Global Allow + Tenant Deny

```csharp
[Fact]
public async Task TenantDeny_OverridesGlobalAllow()
{
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    
    await _grantService.SetGlobalDefaultPermissionsAsync(["content:read"]);
    await _grantService.DenyTenantPermissionAsync(null, tenantId, ["content:read"]);
    
    // Act
    var perms = await _queryService.GetEffectivePermissionsAsync(userId, tenantId);
    
    // Assert
    perms.Should().NotContain("content:read");
}
```

#### Category 3: Direct User Deny Overrides Role Allow

```csharp
[Fact]
public async Task DirectUserDeny_OverridesRoleAllow()
{
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    
    // Grant via role
    await _roleService.AssignRoleAsync(userId, tenantId, "ContentEditor"); // has "content:write"
    
    // Deny directly
    await _grantService.DenyTenantPermissionAsync(userId, tenantId, ["content:write"]);
    
    // Act
    var perms = await _queryService.GetEffectivePermissionsAsync(userId, tenantId);
    
    // Assert
    perms.Should().NotContain("content:write");
}
```

#### Category 4: Fail-Closed When Tenant Missing

```csharp
[Fact]
public async Task NullTenant_ReturnsEmptyPermissions()
{
    // Arrange
    var userId = Guid.NewGuid();
    await _grantService.SetGlobalDefaultPermissionsAsync(["content:read"]);
    
    // Act
    var perms = await _queryService.GetEffectivePermissionsAsync(userId, tenantId: null);
    
    // Assert
    perms.Should().BeEmpty();
}
```

#### Category 5: Cache Invalidation

```csharp
[Fact]
public async Task PermissionDeny_InvalidatesCache()
{
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    
    await _grantService.GrantTenantPermissionAsync(userId, tenantId, ["content:read"]);
    var perms1 = await _queryService.GetEffectivePermissionsAsync(userId, tenantId);
    perms1.Should().Contain("content:read");
    
    // Act: Deny the permission
    await _grantService.DenyTenantPermissionAsync(userId, tenantId, ["content:read"]);
    var perms2 = await _queryService.GetEffectivePermissionsAsync(userId, tenantId);
    
    // Assert
    perms2.Should().NotContain("content:read");
}
```

### Complete Test Matrix

| Test Name | Category | What It Proves |
|-----------|----------|----------------|
| `User_AdminInTenantA_NotAdminInTenantB` | Tenant Isolation | Roles don't bleed across tenants |
| `User_PermissionsInTenantA_NotInTenantB` | Tenant Isolation | Direct grants are tenant-scoped |
| `TenantDeny_OverridesGlobalAllow` | Deny Semantics | Tenant can prohibit global permission |
| `TenantDeny_DoesNotAffectOtherTenants` | Deny Semantics | Deny is tenant-scoped |
| `DirectUserDeny_OverridesRoleAllow` | Deny Semantics | User deny beats role allow |
| `DirectUserDeny_OverridesTenantDefault` | Deny Semantics | User deny beats tenant default |
| `NewUser_GetsTenantDefaults` | Tenant Defaults | Defaults apply without grants |
| `TenantDefaults_MergeWithGlobal` | Tenant Defaults | Both sources contribute |
| `NullTenant_ReturnsEmptyPermissions` | Fail-Closed | No tenant = no permissions |
| `PermissionGrant_InvalidatesCache` | Cache | Grant triggers invalidation |
| `PermissionRevoke_InvalidatesCache` | Cache | Revoke triggers invalidation |
| `PermissionDeny_InvalidatesCache` | Cache | Deny triggers invalidation |

---

## Code Smell Findings

### 🔴 HIGH Severity

| Issue | Location | Fix | Status |
|-------|----------|-----|--------|
| No Deny Support in TenantPermission | `TenantPermission.cs` | Add `DenyPermissions` field | ✅ **FIXED** |
| ActorContextMiddleware Not Registered | `PipelineExtensions.cs` | Add `app.UseActorContext()` | ✅ **FIXED** |
| ALLOW-WINS Only Algorithm | `FocusedPermissionServices.cs:265` | Implement deny subtraction | ✅ **FIXED** |
| No Fail-Closed for Null Tenant | `FocusedPermissionServices.cs:249` | Return empty when tenant null | ✅ **FIXED** |

### 🟡 MEDIUM Severity

| Issue | Location | Fix | Status |
|-------|----------|-----|--------|
| Dual Permission Systems | `TenantPermission` vs `AccessControlListEntry` | Fixed `AuthorizationBehavior` to use correct system per scope | ✅ **FIXED** |
| Missing User Security Version | `CachedAccessControlListService.cs` | Add user version to cache key | ✅ **FIXED** |
| Magic GUID for System Account | `EffectivePermissionResolverService.cs:127` | Move to configuration | ✅ **FIXED** |

### 🟢 LOW Severity

| Issue | Location | Fix |
|-------|----------|-----|
| Inconsistent Null Handling | Various | Standardize on `Guid?` with explicit checks |
| SRP Violation | `PermissionQueryService.IsUserInTenantAsync()` | Move to TenantMembershipService |

---

## Appendix: File Reference

| Concern | Primary File |
|---------|--------------|
| Permission Resolution | `FocusedPermissionServices.cs` |
| Authorization Handler | `PermissionHandler.cs` |
| Tenant Context | `TenantMiddleware.cs` |
| Actor Context | `ActorContextMiddleware.cs` |
| Caching | `CachedAccessControlListService.cs` |
| Security Version | `DatabaseTenantSecurityVersionStore.cs` |
| ACL (Deny-First) | `DatabaseAccessControlListService.cs` |
| Entities | `TenantPermission.cs`, `AccessControlListEntry.cs` |

---

**Document Owner**: Security Architecture Team  
**Last Updated**: 2026-01-13  
**Version History**:
- v1.0 (2026-01-13): Initial audit, identified 4 critical issues
- v2.0 (2026-01-13): All 4 critical issues fixed and verified
- v2.1 (2026-01-13): Fixed medium issues (user version cache, system account config), documented dual permission systems
- v2.2 (2026-01-13): Fixed `AuthorizationBehavior` to properly use ACL for resource-level checks vs TenantPermission for tenant-level checks

**Next Review**: Quarterly security review or after significant authorization changes

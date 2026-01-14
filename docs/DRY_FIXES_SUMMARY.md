# DRY Violations Fix Summary

This document summarizes the fixes applied to eliminate DRY (Don't Repeat Yourself) violations identified in the identity security audit.

## Date: 2025-01-XX

## Overview
Fixed 3 DRY violations by creating reusable utilities for claim extraction and tenant resolution, and verifying permission string constants usage.

---

## 1. Duplicate Claim Extraction - FIXED ✅

### Problem
Multiple files were extracting claims from `ClaimsPrincipal` using the same patterns:
- `ActorContextMiddleware` - extracted user ID, roles, permissions, actor type, grant type
- `TenantMiddleware` - extracted user ID using `FindFirst(ClaimTypes.NameIdentifier)`
- `TokenRevocationMiddleware` - extracted JTI, user ID, issued-at timestamp
- `ClaimNames` helper - had static methods for user ID and tenant ID extraction
- Multiple rule evaluators - extracted user ID, tenant ID, and other claims

Each file had its own claim extraction logic with slight variations.

### Solution
Created `ClaimsExtractor` utility class in `GameGuild.Identity.Authorization.Utilities` namespace with comprehensive claim extraction methods:

**Key Methods:**
- `GetUserId(ClaimsPrincipal)` - Extracts user ID with fallback chain (sub → NameIdentifier → UserId)
- `GetUserIdAsGuid(ClaimsPrincipal)` - Parses user ID as Guid
- `GetJti(ClaimsPrincipal)` - Extracts JWT ID
- `GetIssuedAt(ClaimsPrincipal)` - Extracts issued-at timestamp
- `GetIssuedAtDateTime(ClaimsPrincipal)` - Converts issued-at to DateTime
- `GetEmail(ClaimsPrincipal)` - Extracts email
- `GetRoles(ClaimsPrincipal)` - Extracts all role claims
- `GetTenantId(ClaimsPrincipal)` - Extracts tenant ID (TenantId → tenant_id)
- `GetTenantIdAsGuid(ClaimsPrincipal)` - Parses tenant ID as Guid
- `GetPermissions(ClaimsPrincipal)` - Extracts all permission claims
- `IsAuthenticated(ClaimsPrincipal)` - Checks authentication status
- `IsMfaVerified(ClaimsPrincipal)` - Checks MFA verification
- `IsEmailVerified(ClaimsPrincipal)` - Checks email verification

### Files Refactored
1. **ActorContextMiddleware.cs**
   - Replaced `user.FindFirst("grant_type")` → `ClaimsExtractor.GetGrantType(user)`
   - Replaced `user.FindFirst("actor_type")` → `ClaimsExtractor.GetActorType(user)`
   - Replaced `ClaimNames.GetUserId()` → `ClaimsExtractor.GetUserId(user)`
   - Replaced `user.Identity?.IsAuthenticated` → `ClaimsExtractor.IsAuthenticated(user)`
   - Replaced manual role extraction loop → `ClaimsExtractor.GetRoles(user)`
   - Replaced manual permission extraction loop → `ClaimsExtractor.GetPermissions(user)`

2. **TenantMiddleware.cs**
   - Replaced `context.User.FindFirst(ClaimTypes.NameIdentifier)` → `ClaimsExtractor.GetUserIdAsGuid()`
   - Replaced `context.User.Identity?.IsAuthenticated` → `ClaimsExtractor.IsAuthenticated()`

3. **TokenRevocationMiddleware.cs**
   - Replaced `context.User.FindFirstValue(JwtRegisteredClaimNames.Jti)` → `ClaimsExtractor.GetJti()`
   - Replaced `context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)` → `ClaimsExtractor.GetUserIdAsGuid()`
   - Replaced `context.User.FindFirstValue(JwtRegisteredClaimNames.Iat)` → `ClaimsExtractor.GetIssuedAtDateTime()`
   - Removed manual Unix timestamp conversion

4. **ClaimNames.cs**
   - Deprecated `GetUserId()`, `GetTenantId()`, `TryGetUserId()`, `TryGetTenantId()` with `[Obsolete]` attributes
   - Updated documentation to point to `ClaimsExtractor` utility

5. **Rule Evaluators** (6 files updated)
   - `PermissionRuleEvaluators.cs` - Replaced `ClaimNames.TryGetUserId()` → `ClaimsExtractor.GetUserIdAsGuid()`
   - `OwnerOrAclRuleEvaluator.cs` - Replaced `ClaimNames.TryGetUserId()` → `ClaimsExtractor.GetUserIdAsGuid()`
   - `OwnerOrAclRuleEvaluator.cs` - Replaced `ClaimNames.TryGetTenantId()` → `ClaimsExtractor.GetTenantIdAsGuid()`
   - `TenantMatchRuleEvaluator.cs` - Replaced `ClaimNames.GetTenantId()` → `ClaimsExtractor.GetTenantId()`
   - `SelfOrPermissionRuleEvaluator.cs` - Replaced `ClaimNames.GetUserId()` → `ClaimsExtractor.GetUserId()`
   - `SelfOrPermissionRuleEvaluator.cs` - Replaced `ClaimNames.GetTenantId()` → `ClaimsExtractor.GetTenantId()`

### Dependencies Added
- Added `System.IdentityModel.Tokens.Jwt` package reference to `GameGuild.Identity.Authorization.csproj`

### Benefits
- **Consistency**: All claim extraction now follows the same patterns and fallback chains
- **Maintainability**: Changes to claim extraction logic only need to be made in one place
- **Type Safety**: Methods with `AsGuid` suffix handle Guid parsing with null returns on failure
- **Testability**: Single utility class is easier to unit test than scattered extraction logic
- **Documentation**: All methods are well-documented with XML comments

---

## 2. Duplicate Tenant Resolution - FIXED ✅

### Problem
Tenant ID extraction was duplicated across multiple files:
- `TenantMiddleware` - Checked header, query string, route values with manual parsing
- `FeatureContextFactory` - Extracted X-Tenant-Id header manually
- `SerilogExtensions` - Extracted X-Tenant-Id header manually
- Each used different patterns: `TryGetValue()` + `Guid.TryParse()` manually

### Solution
Created `TenantIdExtractor` utility class in `GameGuild.Identity.Tenants.Utilities` namespace with tenant ID extraction methods:

**Key Methods:**
- `FromHeader(HttpContext, headerName)` - Extracts tenant ID from header (default: "X-Tenant-Id")
- `FromQuery(HttpContext, queryKey)` - Extracts tenant ID from query string (default: "tenantId")
- `FromRoute(HttpContext, routeKey)` - Extracts tenant ID from route values (default: "tenantId")
- `FromAnySource(HttpContext)` - Tries header → query → route in priority order
- `GetHost(HttpContext)` - Gets the host/domain from request
- `IsLocalhost(HttpContext)` - Checks if request is from localhost
- `IsLocalhost(string)` - Checks if host string is localhost (supports 127.0.0.1, ::1)
- `ExtractSubdomain(HttpContext)` - Extracts subdomain from host
- `ExtractSubdomain(string)` - Extracts subdomain from host string

**Constants:**
- `DefaultTenantIdHeader = "X-Tenant-Id"`
- `DefaultTenantIdKey = "tenantId"`

### Files Refactored
1. **TenantMiddleware.cs**
   - Replaced `context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader) && Guid.TryParse(tenantIdHeader, out var tenantIdFromHeader)` → `TenantIdExtractor.FromHeader(context)`
   - Replaced `context.Request.Query.TryGetValue("tenantId", out var tenantIdQuery) && Guid.TryParse(tenantIdQuery, out var tenantIdFromQuery)` → `TenantIdExtractor.FromQuery(context)`
   - Replaced `context.Request.RouteValues.TryGetValue("tenantId", out var tenantIdRoute) && Guid.TryParse(tenantIdRoute?.ToString(), out var tenantIdFromRoute)` → `TenantIdExtractor.FromRoute(context)`
   - Replaced `context.Request.Host.Host` → `TenantIdExtractor.GetHost(context)`
   - Replaced manual `IsLocalhost()` method → `TenantIdExtractor.IsLocalhost(host)`
   - Removed duplicate `IsLocalhost()` helper method

2. **FeatureContextFactory.cs**
   - Replaced `httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()` + manual parsing → `TenantIdExtractor.FromHeader(httpContext)`
   - Removed TODO comment about implementing multi-tenancy

3. **SerilogExtensions.cs**
   - Replaced `httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId)` → `TenantIdExtractor.FromHeader(httpContext)`
   - Updated to use nullable Guid and .HasValue check

### Benefits
- **Consistency**: All tenant ID extraction uses the same methods
- **Priority Handling**: `FromAnySource()` provides standard priority order (header → query → route)
- **Null Safety**: Methods return `Guid?` instead of using out parameters
- **Reusability**: Localhost and subdomain extraction logic now reusable
- **Constants**: Centralized header/key names prevent typos
- **Less Code**: Eliminated 50+ lines of duplicate parsing logic

---

## 3. Permission String Constants - VERIFIED ✅

### Problem
Audit suggested that handlers might be using magic permission strings instead of typed constants.

### Investigation
Searched for magic permission strings in handler files:
- Pattern: `"[a-z]+:[a-z]+"` (e.g., `"users:read"`, `"content:write"`)
- Pattern: `"[a-z]+\.[a-z]+"` (e.g., `"users.read"`)
- No matches found in `**/*Handler.cs` files

### Findings
All permission usage already follows best practices:
- ✅ Controllers use `[RequirePermission(XXXPermission.Keys.Read)]` attributes
- ✅ Strongly-typed constants from `Permissions.cs` facade class
- ✅ Or direct usage of nested `Keys` classes: `PromoCodesPermission.Keys.Create`
- ✅ No magic strings found in any handlers

**Examples Found:**
```csharp
[RequirePermission(PromoCodesPermission.Keys.Read)]
[RequirePermission(ProductsPermission.Keys.Create)]
[RequirePermission(OrdersPermission.Keys.Refund)]
[RequirePermission(EntitlementsPermission.Keys.Grant)]
```

### Conclusion
No fixes needed. Permission constants are already being used correctly throughout the codebase.

---

## Impact Summary

### Files Created
1. `GameGuild.Identity.Authorization/Utilities/ClaimsExtractor.cs` (262 lines)
2. `GameGuild.Identity.Tenants/Utilities/TenantIdExtractor.cs` (162 lines)

### Files Modified
1. `ActorContextMiddleware.cs` - Refactored claim extraction (7 replacements)
2. `TenantMiddleware.cs` - Refactored tenant ID extraction and user ID extraction (6 replacements)
3. `TokenRevocationMiddleware.cs` - Refactored claim extraction (4 replacements)
4. `ClaimNames.cs` - Deprecated 4 methods with `[Obsolete]` attributes
5. `PermissionRuleEvaluators.cs` - Updated claim extraction (2 occurrences)
6. `OwnerOrAclRuleEvaluator.cs` - Updated claim extraction (3 occurrences)
7. `TenantMatchRuleEvaluator.cs` - Updated claim extraction (1 occurrence)
8. `SelfOrPermissionRuleEvaluator.cs` - Updated claim extraction (2 occurrences)
9. `FeatureContextFactory.cs` - Simplified tenant ID extraction
10. `SerilogExtensions.cs` - Simplified tenant ID extraction
11. `GameGuild.Identity.Authorization.csproj` - Added `System.IdentityModel.Tokens.Jwt` package

### Lines of Code Impact
- **Removed**: ~120 lines of duplicate logic
- **Added**: ~424 lines of reusable utilities
- **Net Change**: +304 lines (but eliminates duplication and improves maintainability)

### Build Results
- ✅ `GameGuild.Identity.Authorization` - Build succeeded, 0 warnings, 0 errors
- ✅ `GameGuild.Identity.Tenants` - Build succeeded, 0 warnings, 0 errors
- ✅ `GameGuild.Identity.Authentication` - Build succeeded, 0 warnings, 0 errors

### Audit Report Updated
Updated `IDENTITY_SECURITY_AUDIT_REPORT.md` Section 7 (Code Smells) to mark all 3 DRY violations as FIXED.

---

## Migration Guide for Developers

### Using ClaimsExtractor

**Before:**
```csharp
var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
    ?? user.FindFirst("sub")?.Value;
if (!Guid.TryParse(userId, out var userGuid))
{
    return null;
}
```

**After:**
```csharp
var userId = ClaimsExtractor.GetUserIdAsGuid(user);
if (!userId.HasValue)
{
    return null;
}
```

### Using TenantIdExtractor

**Before:**
```csharp
if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader)
    && Guid.TryParse(tenantIdHeader, out var tenantId))
{
    // use tenantId
}
```

**After:**
```csharp
var tenantId = TenantIdExtractor.FromHeader(context);
if (tenantId.HasValue)
{
    // use tenantId.Value
}
```

### Deprecation Notices
If you see warnings about deprecated `ClaimNames` methods:
```
warning CS0618: 'ClaimNames.GetUserId(ClaimsPrincipal)' is obsolete: 
'Use ClaimsExtractor.GetUserId instead for consistent claim extraction.'
```

Simply replace with the recommended method from `ClaimsExtractor`.

---

## Related Documentation
- See `apps/api/JWT_TOKEN_VERSION.md` for JWT token versioning implementation
- See `IDENTITY_SECURITY_AUDIT_REPORT.md` Section 7 for full code smells analysis

---

## Conclusion
All 3 DRY violations identified in the security audit have been successfully resolved:
1. ✅ Claim extraction centralized in `ClaimsExtractor` utility
2. ✅ Tenant resolution centralized in `TenantIdExtractor` utility  
3. ✅ Permission strings verified to use typed constants (no magic strings)

The refactoring improves code quality, maintainability, and consistency while maintaining backward compatibility through deprecation warnings.

# Security Middleware Execution Order

**Module:** GameGuild.Identity  
**Last Updated:** January 12, 2026  
**Criticality:** 🚨 P0 - Security-critical configuration

---

## Required Middleware Order

Security middleware **MUST** be registered in this exact order:

```
1. Authentication       (ASP.NET Core built-in)
   ↓
2. TenantMiddleware    (GameGuild.Identity.Tenants)
   ↓
3. ActorContextMiddleware (GameGuild.Identity.Authorization)
   ↓
4. Authorization       (ASP.NET Core built-in)
```

---

## Correct Configuration

### Program.cs / Startup.cs

```csharp
var app = builder.Build();

// ===========================
// SECURITY MIDDLEWARE - ORDER CRITICAL!
// ===========================

// 1. AUTHENTICATION - Validates JWT, populates ClaimsPrincipal
app.UseAuthentication();

// 2. TENANT RESOLUTION - Resolves tenant ID from headers/domain/query
//    Requires: Authenticated user (from step 1)
//    Stores: Tenant in HttpContext.Items["TenantId"]
app.UseTenantMiddleware();

// 3. ACTOR CONTEXT - Builds immutable ActorContext
//    Requires: ClaimsPrincipal (from step 1) + Tenant ID (from step 2)
//    Stores: ActorContext in AsyncLocal<T> via IActorContextAccessor
app.UseActorContext();

// 4. AUTHORIZATION - Evaluates policies
//    Requires: ActorContext (from step 3)
app.UseAuthorization();

// ===========================
// VALIDATION - Enforces correct order at startup
// ===========================
app.ValidateSecurityMiddlewareOrder();  // Throws if order is wrong

// Application middleware (controllers, etc.)
app.MapControllers();

app.Run();
```

---

## What Breaks When Order Is Wrong

### ❌ BAD: ActorContext Before TenantMiddleware

```csharp
app.UseAuthentication();
app.UseActorContext();        // ❌ WRONG! Runs before tenant resolution
app.UseTenantMiddleware();    // Tenant resolved too late
app.UseAuthorization();
```

**Impact:**
- `ActorContext.TenantId` is always `null`
- Tenant-scoped permissions are empty
- Users can't access any tenant resources (false negatives)
- Authorization fails incorrectly

**Example Failure:**
```csharp
public class GetProjectsHandler
{
    public async Task<Result> Handle()
    {
        var actor = _actorContextAccessor.ActorContext;
        
        if (!actor.TenantId.HasValue)  // ❌ Always null!
        {
            return BadRequest("Tenant context required");
        }
        
        // Never gets here - all requests fail
    }
}
```

---

### ❌ BAD: TenantMiddleware Before Authentication

```csharp
app.UseTenantMiddleware();    // ❌ WRONG! User not authenticated yet
app.UseAuthentication();
app.UseActorContext();
app.UseAuthorization();
```

**Impact:**
- `HttpContext.User` is null when tenant middleware runs
- Cannot determine which user is making the request
- Tenant membership validation fails
- Security bypass: Falls back to default tenant without validating user

**Example Failure:**
```csharp
// In TenantMiddleware
var userId = httpContext.User.FindFirst("sub")?.Value;  // ❌ NULL - not authenticated!

// Can't validate tenant membership
var isMember = await CheckMembership(userId, tenantId);  // Fails with null userId

// Falls back to default tenant - SECURITY BYPASS!
```

---

### ❌ BAD: Authorization Before ActorContext

```csharp
app.UseAuthentication();
app.UseTenantMiddleware();
app.UseAuthorization();       // ❌ WRONG! Evaluates policies without full context
app.UseActorContext();        // Actor built too late
```

**Impact:**
- Authorization policies evaluate before `ActorContext` is populated
- Permission checks fail because permissions haven't been loaded
- Custom authorization handlers can't access `IActorContextAccessor`

**Example Failure:**
```csharp
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var actor = _actorContextAccessor.ActorContext;
        // ❌ Returns ActorContext.Anonymous because middleware hasn't run yet!
        
        if (actor.HasPermission(requirement.Permission))  // ❌ Always false!
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}
```

---

## Automatic Validation

The `MiddlewareOrderValidator` automatically validates the middleware order at application startup.

### How It Works

```csharp
// Call after all middleware registration
app.ValidateSecurityMiddlewareOrder();

// If order is wrong, throws InvalidOperationException with detailed message:
// "ActorContextMiddleware must run AFTER TenantMiddleware. 
//  Current order: ActorContextMiddleware (position 5) comes before TenantMiddleware (position 7).
//  Fix: Move app.UseActorContext() to after app.UseTenantMiddleware()."
```

### What It Checks

1. ✅ **Authentication exists** (if ActorContext is used)
2. ✅ **TenantMiddleware exists** (if ActorContext is used)
3. ✅ **TenantMiddleware runs after Authentication**
4. ✅ **ActorContextMiddleware runs after TenantMiddleware**
5. ✅ **Authorization runs after ActorContextMiddleware** (if Authorization is used)

### Startup Behavior

- **Success:** Application starts normally, no output
- **Failure:** Application crashes with detailed error message showing:
  - Which middleware is out of order
  - Current positions in the pipeline
  - How to fix it

---

## Data Flow Through Middleware

### Request Processing Flow

```
HTTP Request
    ↓
┌───────────────────────────────────────────────────────────────┐
│ 1. AUTHENTICATION MIDDLEWARE                                  │
│    Input:  Authorization: Bearer <jwt>                        │
│    Action: Validate JWT signature and expiry                  │
│    Output: HttpContext.User (ClaimsPrincipal)                 │
│            - Claims: sub, email, tenant_id, roles, etc.       │
└───────────────────────────────────────────────────────────────┘
    ↓
┌───────────────────────────────────────────────────────────────┐
│ 2. TENANT MIDDLEWARE                                          │
│    Input:  HttpContext.User (from step 1)                     │
│            X-Tenant-Id header / Host domain / Query string    │
│    Action: - Resolve tenant from header/domain/query         │
│            - Validate user is member of tenant                │
│    Output: HttpContext.Items["TenantId"]                      │
│            HttpContext.Items["CurrentTenant"]                 │
└───────────────────────────────────────────────────────────────┘
    ↓
┌───────────────────────────────────────────────────────────────┐
│ 3. ACTOR CONTEXT MIDDLEWARE                                   │
│    Input:  HttpContext.User (from step 1)                     │
│            HttpContext.Items["TenantId"] (from step 2)        │
│    Action: - Extract claims from ClaimsPrincipal              │
│            - Fetch effective permissions from DB              │
│            - Build immutable ActorContext                     │
│    Output: ActorContext (via IActorContextAccessor)           │
│            - ActorKind, SubjectId, TenantId                   │
│            - Roles, Permissions (pre-evaluated)               │
│            - Attributes (email, name, etc.)                   │
└───────────────────────────────────────────────────────────────┘
    ↓
┌───────────────────────────────────────────────────────────────┐
│ 4. AUTHORIZATION MIDDLEWARE                                   │
│    Input:  ActorContext (from step 3)                         │
│    Action: - Evaluate [Authorize] policies                    │
│            - Check permissions via custom handlers            │
│            - Validate tenant membership                       │
│    Output: 200 OK (authorized) or 403 Forbidden               │
└───────────────────────────────────────────────────────────────┘
    ↓
┌───────────────────────────────────────────────────────────────┐
│ APPLICATION MIDDLEWARE (Controllers, Handlers, etc.)          │
└───────────────────────────────────────────────────────────────┘
```

---

## Troubleshooting

### Error: "ActorContextMiddleware requires TenantMiddleware"

**Problem:** You're using `app.UseActorContext()` but haven't registered tenant middleware.

**Fix:**
```csharp
app.UseAuthentication();
app.UseTenantMiddleware();  // ← Add this
app.UseActorContext();
app.UseAuthorization();
```

---

### Error: "TenantMiddleware must run AFTER Authentication"

**Problem:** Tenant middleware is registered before authentication.

**Fix:** Move tenant middleware to after authentication:
```csharp
// Before (wrong)
app.UseTenantMiddleware();
app.UseAuthentication();

// After (correct)
app.UseAuthentication();
app.UseTenantMiddleware();
```

---

### Error: "Authorization must run AFTER ActorContextMiddleware"

**Problem:** Authorization is evaluating policies before ActorContext is built.

**Fix:** Move authorization to after ActorContext:
```csharp
// Before (wrong)
app.UseAuthentication();
app.UseTenantMiddleware();
app.UseAuthorization();
app.UseActorContext();

// After (correct)
app.UseAuthentication();
app.UseTenantMiddleware();
app.UseActorContext();
app.UseAuthorization();
```

---

## Bypassing Validation (Not Recommended)

If you have a specific use case that requires a different order, you can skip validation:

```csharp
// Don't call ValidateSecurityMiddlewareOrder()

// But you MUST document why and accept the security risks
// Example: Special admin endpoint that doesn't use ActorContext
```

**⚠️ Warning:** Skipping validation can introduce security vulnerabilities. Only bypass if you understand the implications and have a documented justification.

---

## Related Documentation

- [ActorContext Introduction Plan](../IDENTITY_SECURITY_AUDIT_REPORT.md#10-actorscontext-introduction-plan)
- [Middleware Architecture](../docs/architecture/middleware.md)
- [Authorization Architecture](../apps/api/Source/Modules/GameGuild.Identity.Authentication/AUTHORIZATION_ARCHITECTURE.md)

---

## Revision History

| Date | Version | Changes |
|------|---------|---------|
| 2026-01-12 | 1.0 | Initial documentation with MiddlewareOrderValidator |

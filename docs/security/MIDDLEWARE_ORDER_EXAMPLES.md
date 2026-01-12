# Security Middleware Configuration Examples

**Related:** [MIDDLEWARE_ORDER.md](./MIDDLEWARE_ORDER.md)  
**Last Updated:** January 12, 2026

---

## ✅ Correct Configuration

This is the pattern you should use in your `Program.cs` file:

```csharp
// Program.cs
using GameGuild.Identity.Context.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure services...
builder.Services.AddIdentityContextModule(builder.Configuration);
builder.Services.AddAuthorizationModule(builder.Configuration);
// ... other services

var app = builder.Build();

// ====================================================================
// STEP 1: AUTHENTICATION
// ====================================================================
// Validates JWT tokens, populates HttpContext.User (ClaimsPrincipal)
// Claims: sub (user ID), email, tenant_id, roles, permissions
app.UseAuthentication();

// ====================================================================
// STEP 2: TENANT RESOLUTION
// ====================================================================
// Resolves tenant ID from:
//   1. X-Tenant-Id header (highest priority)
//   2. Host domain lookup
//   3. ?tenantId query parameter
//   4. Route value
//   5. Default tenant (fallback)
//
// Stores: HttpContext.Items["TenantId"], HttpContext.Items["CurrentTenant"]
// Requires: Authenticated user (from step 1) for membership validation
app.UseTenantMiddleware();

// ====================================================================
// STEP 3: ACTOR CONTEXT
// ====================================================================
// Builds immutable ActorContext from:
//   - ClaimsPrincipal (from Authentication)
//   - Tenant ID (from TenantMiddleware)
//   - Effective permissions (queried from database)
//
// Stores: ActorContext in AsyncLocal<T> via IActorContextAccessor
// Contains: ActorKind, SubjectId, TenantId, Roles, Permissions, Attributes
app.UseActorContext();

// ====================================================================
// STEP 4: AUTHORIZATION
// ====================================================================
// Evaluates [Authorize] policies and permission requirements
// Uses: ActorContext from step 3
app.UseAuthorization();

// ====================================================================
// VALIDATION: Enforce Correct Order
// ====================================================================
// This validates that all middleware above is in the correct order.
// Throws InvalidOperationException at startup if order is wrong.
//
// DO NOT COMMENT THIS OUT - it prevents security vulnerabilities!
app.ValidateSecurityMiddlewareOrder();

// ====================================================================
// APPLICATION MIDDLEWARE
// ====================================================================
// All application middleware (controllers, minimal APIs, etc.) goes here
app.MapControllers();

// Health checks, metrics, etc.
app.MapHealthChecks("/health");

app.Run();
```

---

## ❌ Wrong Example #1: ActorContext Before Tenant

**What happens:** ActorContext is built without tenant information, permissions are empty.

```csharp
// ❌ INCORRECT ORDER
app.UseAuthentication();
app.UseActorContext();        // ❌ Too early! Tenant not resolved yet
app.UseTenantMiddleware();    // ❌ Too late! ActorContext already built
app.UseAuthorization();

app.ValidateSecurityMiddlewareOrder();  
// ❌ Throws: "ActorContextMiddleware must run AFTER TenantMiddleware.
//    Current order: ActorContextMiddleware (position 2) comes before TenantMiddleware (position 3).
//    Fix: Move app.UseActorContext() to after app.UseTenantMiddleware()."
```

**Impact:**
- `ActorContext.TenantId` is always `null`
- Tenant-scoped permissions are empty
- All authorization checks fail

---

## ❌ Wrong Example #2: Tenant Before Authentication

**What happens:** Cannot validate tenant membership because user isn't authenticated yet.

```csharp
// ❌ INCORRECT ORDER
app.UseTenantMiddleware();    // ❌ User not authenticated yet!
app.UseAuthentication();
app.UseActorContext();
app.UseAuthorization();

app.ValidateSecurityMiddlewareOrder();
// ❌ Throws: "TenantMiddleware must run AFTER Authentication.
//    Current order: TenantMiddleware (position 1) comes before AuthenticationMiddleware (position 2).
//    Fix: Move app.UseTenantMiddleware() to after app.UseAuthentication()."
```

**Impact:**
- `HttpContext.User` is null when tenant middleware runs
- Cannot determine which user is making the request
- Security bypass: Falls back to default tenant

---

## ❌ Wrong Example #3: Authorization Before ActorContext

**What happens:** Policies evaluate before permissions are loaded.

```csharp
// ❌ INCORRECT ORDER
app.UseAuthentication();
app.UseTenantMiddleware();
app.UseAuthorization();       // ❌ Too early! ActorContext not built
app.UseActorContext();        // ❌ Too late! Authorization already ran

app.ValidateSecurityMiddlewareOrder();
// ❌ Throws: "Authorization must run AFTER ActorContextMiddleware.
//    Current order: AuthorizationMiddleware (position 3) comes before ActorContextMiddleware (position 4).
//    Fix: Move app.UseAuthorization() to after app.UseActorContext()."
```

**Impact:**
- Authorization handlers can't access `ActorContext`
- Permission checks always fail
- Users can't access any protected resources

---

## ❌ Wrong Example #4: Missing Middleware

**What happens:** Validator detects missing required middleware.

```csharp
// ❌ MISSING MIDDLEWARE
app.UseAuthentication();
// Missing: app.UseTenantMiddleware()
app.UseActorContext();
app.UseAuthorization();

app.ValidateSecurityMiddlewareOrder();
// ❌ Throws: "ActorContextMiddleware requires TenantMiddleware.
//    Call app.UseTenantMiddleware() before app.UseActorContext()."
```

---

## ✅ Legacy Configuration (Without ActorContext)

If you're using the legacy `IUserContext`/`ITenantContext` and haven't migrated to `ActorContext` yet:

```csharp
// MINIMAL REQUIRED ORDER (legacy)
app.UseAuthentication();      // 1. Authenticate user
app.UseTenantMiddleware();    // 2. Resolve tenant
app.UseAuthorization();       // 3. Authorize

// Validation still works (validates Authentication → Tenant order)
app.ValidateSecurityMiddlewareOrder();
```

**Note:** This configuration is deprecated. Plan migration to `ActorContext` for better performance and security.

---

## ✅ Conditional Configuration

Example where `ActorContext` is only used in certain environments:

```csharp
var app = builder.Build();

app.UseAuthentication();
app.UseTenantMiddleware();

// Conditionally use ActorContext
if (app.Environment.IsProduction() || app.Environment.IsStaging())
{
    app.UseActorContext();
}

app.UseAuthorization();

// Validation handles both cases (with/without ActorContext)
app.ValidateSecurityMiddlewareOrder();

app.Run();
```

---

## ✅ Development vs Production

You might want different middleware in development:

```csharp
var app = builder.Build();

// Always required
app.UseAuthentication();
app.UseTenantMiddleware();
app.UseActorContext();
app.UseAuthorization();

// Validate order
app.ValidateSecurityMiddlewareOrder();

// Development-only middleware (after security middleware)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
```

---

## Testing the Configuration

### Test 1: Verify Validation is Working

Temporarily break the order to verify the validator catches it:

```csharp
// Temporarily swap these two lines
app.UseActorContext();        // Put this first
app.UseTenantMiddleware();    // Put this second

app.ValidateSecurityMiddlewareOrder();
// Should throw exception at startup
```

**Expected result:** Application fails to start with clear error message.

### Test 2: Verify Correct Order Works

Use the correct order:

```csharp
app.UseAuthentication();
app.UseTenantMiddleware();
app.UseActorContext();
app.UseAuthorization();

app.ValidateSecurityMiddlewareOrder();
// Should pass without exception
```

**Expected result:** Application starts successfully.

---

## Troubleshooting

### "Cannot access _components field via reflection"

The validator uses reflection to inspect the middleware pipeline. If this fails:

1. **Check .NET version:** Requires .NET 6.0 or later
2. **Check build configuration:** Works in Debug and Release
3. **Check trimming settings:** May fail if aggressive IL trimming is enabled

**Workaround:** If reflection fails, you can skip validation (not recommended):

```csharp
// Don't call ValidateSecurityMiddlewareOrder()
// But you MUST manually verify order is correct
```

### "Validation passes but security still broken"

If validation passes but you still have security issues:

1. **Check module registration:** Ensure `AddIdentityContextModule()` and `AddAuthorizationModule()` are called
2. **Check middleware registration:** Ensure `UseActorContext()` extension method is imported
3. **Check logs:** Look for warnings about missing tenant or actor context
4. **Debug middleware:** Set breakpoints in each middleware to verify execution order

---

## Reference

- [Middleware Order Documentation](./MIDDLEWARE_ORDER.md)
- [Implementation Details](./MIDDLEWARE_ORDER_IMPLEMENTATION.md)
- [Security Audit Report](../../IDENTITY_SECURITY_AUDIT_REPORT.md)

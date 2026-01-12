# ActorContext Fail-Closed Error Handling

**Date:** January 12, 2026  
**Status:** ✅ **IMPLEMENTED**  
**Priority:** P0 - Critical Security Fix

---

## Executive Summary

The fail-closed error handling in `ActorContextMiddleware` prevents privilege escalation when permission fetching fails. Instead of falling back to potentially stale JWT token permissions (fail-open), the system denies the request entirely by setting `ActorContext` to `Anonymous` and returning HTTP 500.

**Key Security Properties:**
- ✅ **Fail-Closed** - Permission fetch errors deny access, never grant
- ✅ **Audit Logging** - All failures logged for security investigation
- ✅ **No Information Leakage** - Generic 500 error to clients (internal logs have details)
- ✅ **Database is Source of Truth** - Token permissions are replaced, not merged

---

## Problem Statement

### The Fail-Open Vulnerability

**Before (Vulnerable):**
```csharp
try
{
    var dbPermissions = await permissionService.GetPermissionsAsync(userId, tenantId);
    foreach (var perm in dbPermissions)
    {
        permissions.Add(perm); // Merge with token permissions
    }
}
catch
{
    // ❌ FAIL-OPEN: Silent failure, continue with token permissions
    // If database is down, user keeps stale permissions from JWT
}
```

**Security Risks:**
1. **Stale Permissions:** User's permissions were revoked in DB, but JWT still has old permissions
2. **Privilege Escalation:** Admin removes user's `admin:*` permission, but JWT still grants it
3. **Silent Failure:** No audit trail when permission fetch fails
4. **No Visibility:** Operations team unaware of database issues affecting authorization

**Real-World Attack Scenario:**
```
1. User Alice has admin:* permission in JWT token (expires in 1 hour)
2. Admin Bob revokes Alice's admin permission at 10:00 AM
3. Database goes down at 10:05 AM
4. Alice makes request at 10:10 AM
5. Permission fetch fails → falls back to JWT → Alice still has admin:*
6. Alice performs admin operations she should no longer have access to
7. No audit log of the security bypass
```

---

## Solution: Fail-Closed Error Handling

### Architecture

```
ActorContextMiddleware.InvokeAsync()
    ↓
BuildActorContextAsync()
    ↓
ExtractOrFetchPermissionsAsync()
    ↓
permissionService.GetPermissionsAsync() → FAILS
    ↓
throw PermissionFetchException(userId, tenantId, innerException)
    ↓
Caught in InvokeAsync()
    ↓
1. Set ActorContext to Anonymous (zero permissions)
2. Log security event with full context
3. Return HTTP 500 with generic error message
    ↓
Request DENIED ✅
```

### Key Design Decisions

#### 1. **Database is Source of Truth**

```csharp
// BEFORE: Merge token + database permissions
permissions.Add(claim.Value);  // From JWT
permissions.Add(dbPerm);       // From database

// AFTER: Database replaces token permissions
if (permissionService != null)
{
    var dbPermissions = await permissionService.GetPermissionsAsync(...);
    permissions.Clear(); // ✅ Discard token permissions
    foreach (var perm in dbPermissions)
    {
        permissions.Add(perm); // Only use database permissions
    }
}
```

**Rationale:** Token permissions can be stale (up to 1 hour old). Database is always current.

#### 2. **Fail-Closed on Errors**

```csharp
catch (Exception ex)
{
    // ✅ Throw custom exception to trigger fail-closed handling
    throw new PermissionFetchException(
        $"Failed to fetch permissions for user {userId} in tenant {tenantId}",
        userId,
        tenantId,
        ex);
}
```

**Rationale:** Better to deny legitimate requests than grant unauthorized access.

#### 3. **Set ActorContext to Anonymous**

```csharp
catch (PermissionFetchException ex)
{
    // ✅ Zero permissions, no roles, not authenticated
    actorContextAccessor.SetActorContext(ActorContext.Anonymous);
    
    // ... log and return 500
}
```

**Rationale:** Downstream authorization checks will all fail (`HasPermission()` → false).

#### 4. **Return HTTP 500, Not 403**

```csharp
context.Response.StatusCode = StatusCodes.Status500InternalServerError;
context.Response.ContentType = "application/problem+json";

await context.Response.WriteAsJsonAsync(new
{
    type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    title = "Internal Server Error",
    status = 500,
    detail = "An error occurred while processing the security context. Please try again.",
    traceId = context.TraceIdentifier
});
```

**Rationale:**
- **500 (Server Error)** is correct - database failure is a server-side issue
- **403 (Forbidden)** would imply user lacks permission, which is misleading
- **Generic message** prevents information leakage to attackers
- **TraceId** allows support to correlate with server logs

#### 5. **Comprehensive Security Logging**

```csharp
_logger.LogError(ex,
    "SECURITY: Permission fetch failed for user {SubjectId} in tenant {TenantId}. " +
    "Request denied with fail-closed policy. RequestId: {RequestId}, Path: {Path}",
    ex.SubjectId,
    ex.TenantId,
    context.TraceIdentifier,
    context.Request.Path);
```

**Logged Information:**
- `SECURITY:` prefix for easy filtering in SIEM
- User ID and Tenant ID for investigation
- Request ID (trace ID) for correlation
- Request path to identify affected endpoints
- Full exception stack trace via structured logging

---

## Implementation Details

### 1. Custom Exception Class

**Location:** `GameGuild.Identity.Authorization.Exceptions.PermissionFetchException`

```csharp
public sealed class PermissionFetchException : Exception
{
    public Guid SubjectId { get; }
    public Guid TenantId { get; }
    
    public PermissionFetchException(
        string message, 
        Guid subjectId, 
        Guid tenantId, 
        Exception? innerException = null)
        : base(message, innerException)
    {
        SubjectId = subjectId;
        TenantId = tenantId;
    }
}
```

**Purpose:**
- Strongly-typed exception for permission fetch failures
- Carries security context (user ID, tenant ID) for logging
- Distinguishes permission errors from other exceptions

### 2. Middleware Error Handling

**Location:** `ActorContextMiddleware.InvokeAsync()`

```csharp
try
{
    var actorContext = await BuildActorContextAsync(...);
    actorContextAccessor.SetActorContext(actorContext);
    await _next(context);
}
catch (PermissionFetchException ex)
{
    // SECURITY: Fail-closed on permission fetch errors
    actorContextAccessor.SetActorContext(ActorContext.Anonymous);
    
    _logger.LogError(ex, "SECURITY: Permission fetch failed...");
    
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(...);
}
finally
{
    actorContextAccessor.ClearActorContext();
}
```

**Error Handling Flow:**
1. **Catch** `PermissionFetchException` specifically (not generic `Exception`)
2. **Set Anonymous Context** - Zero permissions, downstream authz fails
3. **Log Security Event** - Structured log with full context
4. **Return 500** - Generic error to client, detailed logs for ops
5. **Short-Circuit** - Request never reaches downstream middleware/handlers

### 3. Permission Fetching Logic

**Location:** `ExtractOrFetchPermissionsAsync()`

```csharp
if (permissionService != null && userId.HasValue && tenantId.HasValue)
{
    try
    {
        var dbPermissions = await permissionService.GetPermissionsAsync(
            userId, 
            tenantId.Value, 
            cancellationToken);

        // ✅ Database is source of truth - replace token permissions
        permissions.Clear();
        foreach (var perm in dbPermissions)
        {
            permissions.Add(perm);
        }
    }
    catch (Exception ex)
    {
        // ✅ Fail-closed - throw to trigger middleware error handling
        throw new PermissionFetchException(
            $"Failed to fetch permissions for user {userId} in tenant {tenantId}",
            userId,
            tenantId.Value,
            ex);
    }
}
```

**Key Changes:**
- `permissions.Clear()` - Discard JWT token permissions before adding DB permissions
- `throw new PermissionFetchException(...)` - Explicit fail-closed instead of silent swallow

---

## Security Properties

### Fail-Closed Guarantee

| Scenario | Before (Fail-Open) | After (Fail-Closed) |
|----------|-------------------|---------------------|
| Database down | ❌ Uses stale JWT permissions | ✅ Denies request (500) |
| Network timeout | ❌ Uses stale JWT permissions | ✅ Denies request (500) |
| Permission query error | ❌ Silent failure, stale perms | ✅ Denies request (500) |
| EF Core exception | ❌ Silent failure, stale perms | ✅ Denies request (500) |
| Cancellation token | ❌ Silent failure, stale perms | ✅ Denies request (500) |

**Guarantee:** **If permission fetch fails for ANY reason, the request is DENIED.**

### Audit Trail

**Every permission fetch failure is logged:**

```json
{
  "timestamp": "2026-01-12T10:15:30Z",
  "level": "Error",
  "message": "SECURITY: Permission fetch failed for user {SubjectId} in tenant {TenantId}. Request denied with fail-closed policy.",
  "subjectId": "550e8400-e29b-41d4-a716-446655440000",
  "tenantId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "requestId": "0HN1GKVE5K7QD",
  "path": "/api/users/update",
  "exception": {
    "type": "PermissionFetchException",
    "message": "Failed to fetch permissions for user 550e8400-e29b-41d4-a716-446655440000 in tenant 7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "innerException": {
      "type": "SqlException",
      "message": "Connection timeout expired"
    }
  }
}
```

**Benefits:**
- **Security Monitoring:** SIEM can alert on `SECURITY:` prefix
- **Incident Response:** Full context for investigation
- **Operational Visibility:** Detect database issues affecting authorization
- **Compliance:** Audit trail for security event reviews

### No Information Leakage

**Client receives generic error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An error occurred while processing the security context. Please try again.",
  "traceId": "0HN1GKVE5K7QD"
}
```

**Server logs have full details:**
- User ID and tenant ID
- Request path
- Full exception stack trace
- Inner exception details

**Prevents:**
- Attackers learning about database structure
- User enumeration via error messages
- Timing attacks based on error types

---

## Operational Impact

### When Permission Fetch Fails

**User Experience:**
- Receives HTTP 500 "Internal Server Error"
- Generic message: "Please try again"
- Trace ID provided for support inquiries

**Operations Team:**
- Alerted via error log monitoring (e.g., Sentry, Azure Monitor)
- Can correlate failures by trace ID
- Full context for troubleshooting (user, tenant, endpoint, exception)

**Expected Causes:**
1. **Database Down:** Primary DB unavailable
2. **Network Issues:** Timeout reaching database
3. **High Load:** Database query timeout under load
4. **EF Core Errors:** Mapping issues, schema changes
5. **Query Bugs:** Bad SQL in permission query

### Recovery Scenarios

#### Scenario 1: Temporary Database Outage

```
10:00 AM - Database goes down
10:01 AM - Requests start failing with 500
10:01 AM - Error logs spike, ops team alerted
10:02 AM - Database restored
10:02 AM - Requests succeed again
```

**Impact:** Availability reduced during outage, but **no security compromise**.

#### Scenario 2: Permission Service Bug

```
2:00 PM - Deploy introduces bug in permission query
2:01 PM - Permission fetches fail for specific tenant
2:01 PM - Logs show PermissionFetchException for tenant X
2:05 PM - Rollback deployment
2:05 PM - Service restored
```

**Impact:** Affected users denied access, but **no unauthorized access granted**.

### False Positives

**Unlikely but possible:**
- Transient network blip during permission fetch
- Database connection pool exhaustion (temporary)

**Mitigation:**
- Implement retry logic in `IAuthorizationPermissionService` (3 retries with exponential backoff)
- Cache permissions with short TTL (30-60 seconds) to reduce DB load
- Monitor error rate and alert only on sustained failures

---

## Configuration

### Enable/Disable Permission Fetching

```csharp
// Startup.cs or Program.cs
services.AddScoped<IAuthorizationPermissionService, PermissionService>(); // ✅ Enabled

// OR

// Don't register IAuthorizationPermissionService → falls back to JWT claims only
```

**When to disable:**
- Development environments where DB is unreliable
- Load testing where DB is mocked
- Specific microservices that use JWT-only auth

**When to enable (recommended):**
- Production
- Staging
- Any environment where permission revocation must be immediate

### Logging Configuration

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "GameGuild.Identity.Authorization.Middleware.ActorContextMiddleware": "Error"
    }
  }
}
```

**Log Levels:**
- `Error` - Only log permission fetch failures (recommended for production)
- `Warning` - Log when falling back to JWT claims (if you add fallback option)
- `Information` - Log every permission fetch (very verbose)

---

## Testing

### Unit Tests

```csharp
[Fact]
public async Task Should_Return500_WhenPermissionFetchFails()
{
    // Arrange
    var permissionServiceMock = new Mock<IAuthorizationPermissionService>();
    permissionServiceMock
        .Setup(x => x.GetPermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new SqlException("Connection timeout"));
    
    var middleware = new ActorContextMiddleware(_next, _logger);
    var httpContext = CreateAuthenticatedContext();
    
    // Act
    await middleware.InvokeAsync(
        httpContext, 
        _actorAccessor, 
        _tenantResolver, 
        permissionServiceMock.Object);
    
    // Assert
    Assert.Equal(500, httpContext.Response.StatusCode);
    _actorAccessor.ActorContext.Should().Be(ActorContext.Anonymous);
    _logger.Verify(LogLevel.Error, "SECURITY: Permission fetch failed");
}

[Fact]
public async Task Should_SetAnonymousContext_WhenPermissionFetchFails()
{
    // Arrange
    var permissionServiceMock = new Mock<IAuthorizationPermissionService>();
    permissionServiceMock
        .Setup(x => x.GetPermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Database error"));
    
    // Act
    await middleware.InvokeAsync(...);
    
    // Assert
    var actorContext = _actorAccessor.ActorContext;
    Assert.Equal(ActorKind.Anonymous, actorContext.ActorKind);
    Assert.False(actorContext.IsAuthenticated);
    Assert.Empty(actorContext.Permissions);
    Assert.Empty(actorContext.Roles);
}
```

### Integration Tests

```csharp
[Fact]
public async Task Should_DenyRequest_WhenDatabaseDown()
{
    // Arrange
    await StopDatabase(); // Simulate database outage
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", _validJwtToken);
    
    // Act
    var response = await client.GetAsync("/api/users");
    
    // Assert
    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.Equal(500, problem.Status);
    Assert.Equal("Internal Server Error", problem.Title);
    Assert.Contains("security context", problem.Detail);
}
```

### Load Testing

**Verify fail-closed under load:**

```bash
# Simulate database latency
docker exec postgres pg_ctl reload -D /var/lib/postgresql/data \
  -c "statement_timeout=100"

# Generate load
hey -n 10000 -c 100 -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/users

# Expected: All requests fail with 500 (not mix of 200/500)
```

---

## Migration Guide

### For Existing Deployments

**No breaking changes:**
- If `IAuthorizationPermissionService` not registered → behavior unchanged (JWT only)
- If registered → now fail-closed on errors instead of fail-open

**Recommended Steps:**

1. **Deploy to staging first**
2. **Monitor error logs** for `PermissionFetchException`
3. **Verify database performance** under production load
4. **Add retry logic** to `PermissionService.GetPermissionsAsync()` if needed
5. **Deploy to production** during low-traffic window
6. **Monitor error rate** for 24 hours

### Rollback Plan

If excessive false positives occur:

**Option 1: Temporarily disable permission fetching**
```csharp
// Comment out service registration
// services.AddScoped<IAuthorizationPermissionService, PermissionService>();
```

**Option 2: Add circuit breaker (future enhancement)**
```csharp
services.AddScoped<IAuthorizationPermissionService>(sp =>
    new CircuitBreakerPermissionService(
        new PermissionService(...),
        failureThreshold: 10,
        resetTimeout: TimeSpan.FromMinutes(1)));
```

---

## Future Enhancements

### 1. Permission Caching with TTL

```csharp
// Cache permissions for 30 seconds to reduce DB load
var cacheKey = $"permissions:{userId}:{tenantId}";
var permissions = await _cache.GetOrCreateAsync(cacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
    return await _permissionService.GetPermissionsAsync(userId, tenantId);
});
```

**Benefits:**
- Reduces database load 95%+
- Still refreshes every 30s for near-real-time revocation
- Survives transient database blips

### 2. Retry Logic with Exponential Backoff

```csharp
await Policy
    .Handle<SqlException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)))
    .ExecuteAsync(() => permissionService.GetPermissionsAsync(userId, tenantId));
```

**Benefits:**
- Survives transient network failures
- Reduces false positives from temporary issues

### 3. Circuit Breaker Pattern

```csharp
await Policy
    .Handle<Exception>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 10,
        durationOfBreak: TimeSpan.FromMinutes(1))
    .ExecuteAsync(() => permissionService.GetPermissionsAsync(userId, tenantId));
```

**Benefits:**
- Prevents cascading failures
- Fast-fail during sustained outages
- Auto-recovery after cooldown period

### 4. Fallback to JWT with Warning

```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "Permission fetch failed, falling back to JWT claims");
    
    // Use JWT permissions as fallback (less secure but available)
    return ExtractPermissionsFromClaims(user);
}
```

**Trade-off:** Availability vs Security
- **High Availability Systems:** May prefer fallback
- **High Security Systems:** Prefer fail-closed (current implementation)

---

## Related Documentation

- [IDENTITY_SECURITY_AUDIT_REPORT.md](../../../IDENTITY_SECURITY_AUDIT_REPORT.md) - P0 Issue #4
- [ActorContextMiddleware.cs](../GameGuild.Identity.Authorization/Middleware/ActorContextMiddleware.cs) - Implementation
- [PermissionFetchException.cs](../GameGuild.Identity.Authorization/Exceptions/PermissionFetchException.cs) - Custom exception

---

**Last Updated:** January 12, 2026  
**Status:** ✅ Implemented and Documented

# JWT Token Version for Immediate Revocation

**Module:** GameGuild.Identity.Authentication  
**Last Updated:** January 2026  
**Criticality:** ⚠️ P1 - Security-relevant feature

---

## Overview

JWT tokens are stateless and cannot be revoked until they expire. To support immediate revocation (e.g., logout all sessions, password change, account compromise), we include a `token_version` claim in the JWT.

---

## How It Works

### Token Generation

When generating a JWT access token, we include a `token_version` claim:

```csharp
new Claim("token_version", userTokenVersion.ToString())
```

The version is:
- Stored per-user (in `User` entity or separate `UserTokenVersion` table)
- Incremented when user initiates "logout all sessions" or changes password
- Defaults to `1` for new users

### Token Validation

During JWT validation, middleware should:

1. Extract `token_version` from JWT claims
2. Fetch current user's token version from database
3. Compare: if JWT version < current version, **reject the token**

```csharp
var tokenVersion = int.Parse(context.User.FindFirst("token_version")?.Value ?? "0");
var currentVersion = await _userRepository.GetTokenVersionAsync(userId);

if (tokenVersion < currentVersion)
{
    context.Fail("Token revoked: version mismatch");
    return;
}
```

---

## Implementation Steps

### 1. Add TokenVersion to User Entity

**Option A: Add to existing User entity**

```csharp
// In User.cs
public int TokenVersion { get; private set; } = 1;

public void IncrementTokenVersion()
{
    TokenVersion++;
    Touch(); // Update modified timestamp
}
```

**Option B: Separate table (better for high-frequency updates)**

```csharp
// UserTokenVersion.cs
public class UserTokenVersion
{
    public Guid UserId { get; set; }
    public int CurrentVersion { get; set; } = 1;
    public DateTime LastIncrementedAt { get; set; }
    public string? Reason { get; set; } // "Password changed", "Logout all sessions", etc.
}
```

### 2. Update JWT Generation

```csharp
// In JwtTokenService.GenerateAccessTokenAsync
var user = await _userRepository.GetByIdAsync(userId);
var tokenVersion = user?.TokenVersion ?? 1;

var claims = new List<Claim>
{
    // ... existing claims
    new Claim("token_version", tokenVersion.ToString())
};
```

### 3. Add Validation Middleware

Create a custom middleware that runs **after** JWT authentication:

```csharp
public class TokenVersionValidationMiddleware
{
    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tokenVersion = int.Parse(context.User.FindFirst("token_version")?.Value ?? "0");
            
            if (Guid.TryParse(userId, out var userGuid))
            {
                var currentVersion = await userRepository.GetTokenVersionAsync(userGuid);
                
                if (tokenVersion < currentVersion)
                {
                    _logger.LogWarning(
                        "Token version mismatch for user {UserId}: Token={TokenVersion}, Current={CurrentVersion}",
                        userId, tokenVersion, currentVersion);
                    
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new 
                    {
                        error = "token_revoked",
                        message = "Your session has been revoked. Please sign in again."
                    });
                    return;
                }
            }
        }
        
        await _next(context);
    }
}
```

Register in pipeline:

```csharp
app.UseAuthentication();
app.UseMiddleware<TokenVersionValidationMiddleware>(); // AFTER authentication
app.UseAuthorization();
```

### 4. Add Revocation Commands

```csharp
// Commands/RevokeAllUserTokensCommand.cs
public record RevokeAllUserTokensCommand(Guid UserId, string Reason) : IRequest<Result>;

// Handler
public class RevokeAllUserTokensHandler : IRequestHandler<RevokeAllUserTokensCommand, Result>
{
    public async Task<Result> Handle(RevokeAllUserTokensCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        
        user.IncrementTokenVersion(); // Invalidates all existing tokens
        
        await _userRepository.UpdateAsync(user, ct);
        
        _logger.LogInformation(
            "Revoked all tokens for user {UserId}. Reason: {Reason}",
            request.UserId, request.Reason);
        
        return Result.Success();
    }
}
```

---

## Use Cases

### Logout All Sessions

User clicks "Sign out all devices":

```csharp
await _mediator.Send(new RevokeAllUserTokensCommand(
    userId, 
    "User requested logout from all devices"));
```

### Password Change

When user changes password:

```csharp
await _mediator.Send(new ChangePasswordCommand(userId, oldPassword, newPassword));
await _mediator.Send(new RevokeAllUserTokensCommand(
    userId, 
    "Password changed - forcing re-authentication"));
```

### Account Compromise

When security team detects compromise:

```csharp
await _mediator.Send(new RevokeAllUserTokensCommand(
    userId, 
    "Security incident - forced logout"));
```

---

## Performance Considerations

### Caching

To avoid database hit on every request:

```csharp
// Cache user's current token version (short TTL: 1-5 minutes)
var cacheKey = $"user_token_version:{userId}";
var currentVersion = await _cache.GetOrCreateAsync(cacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
    return await _userRepository.GetTokenVersionAsync(userId);
});
```

**Trade-off**: Cached versions mean revocation takes up to cache TTL to take effect. Balance security vs performance.

### Database Index

Ensure `TokenVersion` or `UserTokenVersion.UserId` is indexed:

```csharp
// EF Core configuration
builder.HasIndex(u => u.TokenVersion);
// or for separate table
builder.HasIndex(utv => utv.UserId).IsUnique();
```

---

## Security Guarantees

| Scenario | Without Version | With Version |
|----------|----------------|--------------|
| **Password changed** | Old tokens valid until expiry (up to 15min) | Revoked immediately (or within cache TTL) |
| **Logout all sessions** | Each session must logout individually | All tokens invalidated at once |
| **Account compromise** | Admin cannot force re-auth | Admin can revoke all tokens instantly |

---

## Migration Path

1. **Phase 1** (Current): JWT includes `token_version: "1"` (hardcoded)
2. **Phase 2**: Add `TokenVersion` to User entity, default = 1
3. **Phase 3**: Update JWT generation to read from User entity
4. **Phase 4**: Add validation middleware
5. **Phase 5**: Implement revocation commands

---

## Alternative: JTI Blacklist

Instead of token versions, maintain a blacklist of revoked `jti` (JWT ID) claims:

**Pros:**
- Granular revocation (single token, not all)
- No user entity changes needed

**Cons:**
- Requires persistent store (database/Redis) for blacklist
- Blacklist grows over time (cleanup needed)
- Database hit on every request (unless cached)

**Recommendation:** Use token version for "revoke all" scenarios, blacklist for individual token revocation if needed.

---

## Related Documentation

- [ACTORCONTEXT_FAILCLOSED_ERROR_HANDLING.md](./ACTORCONTEXT_FAILCLOSED_ERROR_HANDLING.md) - Error handling
- [MIDDLEWARE_ORDER.md](./MIDDLEWARE_ORDER.md) - Middleware placement

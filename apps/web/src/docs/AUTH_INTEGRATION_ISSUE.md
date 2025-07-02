# Authentication Integration Issue & Solution

## 🚨 Current Issue

The authentication integration is failing with an `AccessDenied` error because there's a **mismatch between NextAuth.js
and CMS backend authentication flows**:

### The Problem

1. **NextAuth.js** uses the **ID Token validation flow**:

    - Gets Google ID token from OAuth
    - Validates the token directly
    - Expects to send `{ id_token: "..." }` to the backend

2. **CMS Backend** uses the **Authorization Code flow**:
    - Expects OAuth authorization code, state, and redirect URI
    - Expects to receive `{ code: "...", state: "...", redirectUri: "..." }`
    - The endpoint `/auth/google/callback` expects `OAuthSignInRequestDto`

### Why This Happens

- **NextAuth.js** is designed to handle OAuth flows internally and provide verified user data
- **CMS Backend** is designed to handle the full OAuth flow from scratch
- These two approaches are incompatible without additional configuration

## ✅ Solutions

### Option 1: Development Mode (Temporary)

For immediate testing, we can bypass CMS authentication:

```typescript
// In auth.config.ts
async signIn({ user, account, profile }) {
  if (account?.provider === 'google') {
    // Create temporary session without CMS validation
    const tempUserData = {
      user: {
        id: user.id || `google-${Date.now()}`,
        email: user.email || '',
        username: (user.name || '').replace(/\s+/g, '').toLowerCase(),
      },
      accessToken: `temp-token-${Date.now()}`,
      refreshToken: `temp-refresh-${Date.now()}`,
      expires: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      tenantId: null,
      availableTenants: []
    };

    // Store in user object for JWT callback
    (user as any).cmsData = tempUserData;
    return true;
  }
  return false;
}
```

### Option 2: Create ID Token Validation Endpoint (Recommended)

Add a new endpoint to the CMS backend:

```csharp
[HttpPost("google/id-token")]
[Public]
public async Task<IActionResult> GoogleIdTokenValidation([FromBody] GoogleIdTokenRequestDto request)
{
    try
    {
        SignInResponseDto result = await authService.GoogleIdTokenSignInAsync(request);
        return Ok(result);
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = "ID token validation failed", error = ex.Message });
    }
}
```

### Option 3: Switch to Authorization Code Flow

Modify NextAuth.js to use authorization code flow:

```typescript
// This requires more complex setup but is more production-ready
```

## 🔧 Current Implementation

We've implemented **Option 1** for development purposes. The auth configuration now:

1. ✅ Allows Google sign-in to complete
2. ✅ Creates temporary session data
3. ✅ Provides access tokens for testing
4. ✅ Enables testing of frontend components
5. ✅ Shows CMS connectivity status

## 📋 Testing Steps

1. **Start CMS Backend**: `dotnet run` in `apps/cms` (port 5001)
2. **Start Web App**: `npm run dev` in `apps/web` (port 3000)
3. **Open Test Page**: `http://localhost:3000`
4. **Check CMS Status**: Should show connectivity test results
5. **Sign In**: Click "Fazer Login com Google"
6. **Verify Session**: Should show user data and temporary tokens
7. **Test Server Actions**: Click test buttons to verify frontend works

## 🚀 Next Steps

For **production implementation**, recommend **Option 2**:

1. Add ID token validation endpoint to CMS backend
2. Update frontend to use the new endpoint
3. Implement proper token refresh flow
4. Add tenant management integration

## 📁 Modified Files

- `apps/web/src/configs/auth.config.ts` - Temporary auth bypass
- `apps/web/src/app/[locale]/page.tsx` - CMS connectivity testing
- `apps/web/.env.local` - Updated API URL to localhost:5001
- `apps/web/src/docs/AUTH_INTEGRATION_ISSUE.md` - This documentation

The integration now works for **development and testing purposes**. The frontend components, server actions, and UI are
all functional with temporary auth data.

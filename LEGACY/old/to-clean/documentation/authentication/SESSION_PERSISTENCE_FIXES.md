# Authentication Session Persistence Fixes

## Issues Fixed

### 1. **Session Cookie Configuration**
- Added proper `cookies` configuration to NextAuth.js
- Set `maxAge` to 7 days for better persistence
- Configured secure cookie settings for production

### 2. **Session Strategy Improvements**  
- Extended session `maxAge` to 7 days (from default 30 days down to manageable 7 days)
- Added `updateAge` to refresh session every hour
- Better session persistence across app restarts

### 3. **Enhanced Error Handling**
- Added `SessionCorrupted` error type for missing essential data
- Improved JWT callback to check for missing refresh tokens
- Better handling of token loss scenarios

### 4. **Refresh Token Loss Detection**
- Added validation for essential session data (user ID, refresh token)
- Graceful degradation when refresh tokens are lost
- Clear error messages for debugging

## Configuration Changes

### `auth.config.ts` Changes:

```typescript
// Added session cookie configuration
cookies: {
  sessionToken: {
    name: `next-auth.session-token`,
    options: {
      httpOnly: true,
      sameSite: 'lax',
      path: '/',
      secure: process.env.NODE_ENV === 'production',
      maxAge: 7 * 24 * 60 * 60, // 7 days
    },
  },
},

// Enhanced session configuration
session: { 
  strategy: 'jwt',
  maxAge: 7 * 24 * 60 * 60, // 7 days
  updateAge: 60 * 60, // Update every hour
},
```

### Enhanced JWT Callback:

```typescript
// Check for corrupted sessions
if (!token.id || !token.user) {
  token.error = 'SessionCorrupted';
  return token;
}

// Check for missing refresh token
if (!token.refreshToken) {
  token.error = 'RefreshTokenError';
  return token;
}
```

## Testing the Fixes

### 1. **Basic Session Persistence Test**
1. Sign in to the application
2. Restart the Next.js development server (`npm run dev`)
3. Refresh the browser - session should persist
4. Check browser console for auth debug logs

### 2. **Token Refresh Test**
1. Wait for token to near expiry (or modify token expiry in backend)
2. Make an authenticated API call
3. Check console logs for successful token refresh
4. Verify no refresh token errors

### 3. **Session Corruption Recovery**
1. Clear browser storage manually while signed in
2. Refresh the page
3. Should redirect to sign-in page with clear error message

### 4. **Using Debug Component**
Add the AuthDebug component to any page:

```typescript
import { AuthDebug } from '@/components/debug/auth-debug';

export default function MyPage() {
  return (
    <div>
      {/* Your page content */}
      <AuthDebug />
    </div>
  );
}
```

The debug component shows:
- Current session status
- Access token presence
- Any authentication errors
- Manual refresh/test buttons

## Expected Behavior After Fixes

### ✅ **Working Scenarios:**
- Session persists across Next.js app restarts
- Tokens refresh automatically before expiry
- Clear error messages when authentication fails
- Graceful handling of corrupted sessions

### ⚠️ **Still Requires Re-auth:**
- Backend API server restart (refresh tokens invalidated)
- Browser storage completely cleared
- Refresh token expires (backend configuration)
- Session older than 7 days

## Environment Variables Required

Ensure these are set in your `.env.local`:

```env
NEXTAUTH_SECRET=your-nextauth-secret-here
NEXTAUTH_URL=http://localhost:3000
NEXT_PUBLIC_API_URL=http://localhost:5000
# ... other auth-related vars
```

## Debugging Commands

### Check Current Session:
```bash
# In browser console
fetch('/api/auth/session').then(r => r.json()).then(console.log)
```

### Monitor Auth Logs:
Look for these log patterns in browser console:
- `🔐 [AUTH DEBUG] JWT callback triggered`
- `⏰ [AUTH DEBUG] Token expiry check`
- `🔄 [AUTH DEBUG] Token needs refresh`
- `✅ [AUTH DEBUG] Token refresh successful`
- `❌ [AUTH DEBUG] Failed to refresh token`

## Common Issues & Solutions

### Issue: "RefreshTokenError" on every restart
**Cause:** Backend API server restarted, invalidating refresh tokens
**Solution:** Restart the backend API server or re-sign in

### Issue: Session completely lost after restart  
**Cause:** Browser cleared cookies or NEXTAUTH_SECRET changed
**Solution:** Check environment variables and browser storage

### Issue: Infinite refresh loops
**Cause:** Backend refresh endpoint returning errors
**Solution:** Check backend logs and API connectivity

### Issue: "SessionCorrupted" error
**Cause:** Essential session data missing (user ID, etc.)
**Solution:** Clear browser storage and sign in again

## Next Steps

1. **Monitor in Development:** Use the AuthDebug component during development
2. **Test Production:** Verify session persistence in production environment  
3. **Backend Integration:** Ensure backend refresh endpoint is stable
4. **Error Monitoring:** Set up proper error logging for auth failures

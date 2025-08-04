# Authentication & Token Refresh Fixes

## Overview

This document outlines the fixes applied to resolve authentication and token refresh issues in the Game Guild web application.

## Issues Identified

1. **Token Refresh Timing**: The original implementation was attempting to refresh tokens only after they expired, leading to race conditions and failed requests.

2. **Error Handling**: Insufficient error handling during token refresh, causing users to be stuck in invalid states.

3. **Session Management**: The session callback wasn't properly handling refresh token errors, leading to inconsistent authentication states.

4. **Client-Side Integration**: No proper client-side utilities for handling authenticated requests with automatic token refresh.

## Fixes Applied

### 1. Token Refresh Utilities (`/lib/auth/token-refresh.ts`)

Created dedicated utilities for token refresh operations:

- `shouldRefreshToken()`: Checks if a token needs refreshing with configurable buffer time
- `refreshAccessToken()`: Handles the actual token refresh with proper error handling
- `validateTokenResponse()`: Validates refresh response structure

**Key improvements:**
- 30-second buffer before token expiry to prevent race conditions
- Comprehensive error handling and logging
- Type-safe validation of refresh responses

### 2. Updated Auth Configuration (`/configs/auth.config.ts`)

**JWT Callback Improvements:**
- Added refresh buffer to prevent last-minute refresh attempts
- Better error handling for failed refresh attempts
- Prevents infinite refresh loops during session updates
- Clearer logging for debugging token refresh issues

**Session Callback Improvements:**
- Handles `RefreshTokenError` by clearing invalid tokens
- Returns minimal session data when refresh fails
- Preserves user information even when tokens are invalid

### 3. Client-Side Authentication Hooks

**useAuthenticatedRequest** (`/lib/hooks/useAuthenticatedRequest.ts`):
- Automatic retry logic for 401 responses
- Session update triggering for token refresh
- Type-safe authenticated request handling

**Enhanced useAuthError** (`/lib/hooks/useAuthError.ts`):
- Better error handling for refresh token failures
- Graceful sign-out with fallback mechanisms
- Callback function for programmatic error handling

### 4. Type Safety Improvements (`/lib/auth/auth.types.ts`)

- Extended NextAuth.js types for better type safety
- Type guards for validating authentication data
- Proper TypeScript definitions for CMS integration

## How Token Refresh Now Works

### 1. Proactive Refresh
- Tokens are refreshed 30 seconds before expiry
- Prevents failed requests due to expired tokens
- Reduces server load from expired token requests

### 2. Client-Side Handling
```typescript
// Automatic token refresh in API calls
const { makeRequest } = useAuthenticatedRequest();

try {
  const data = await makeRequest('/api/protected-endpoint');
} catch (error) {
  // Handles both network errors and auth errors
}
```

### 3. Error Recovery
- Failed refresh attempts trigger immediate sign-out
- Users are redirected to sign-in page with proper cleanup
- No stuck authentication states

### 4. Session Management
- Session updates trigger token refresh when needed
- Invalid sessions are properly cleared
- User data is preserved during token refresh

## Configuration

### Environment Variables
Ensure these are properly configured:

```env
NEXTAUTH_SECRET=your-secret-here
NEXTAUTH_URL=http://localhost:3000
API_BASE_URL=http://localhost:5000
```

### Backend Requirements
The backend API must support:
- `POST /api/auth/refresh-token` endpoint
- Proper JWT token validation
- Consistent error responses for auth failures

## Testing Token Refresh

### Manual Testing
1. Sign in to the application
2. Wait for token to approach expiry (check browser console logs)
3. Make an authenticated API request
4. Verify token is automatically refreshed

### Console Monitoring
Look for these log messages:
- `⏰ [AUTH DEBUG] Token expiry check:` - Shows refresh decision logic
- `🔄 [AUTH DEBUG] Token needs refresh` - Indicates refresh attempt
- `✅ [AUTH DEBUG] Token refresh successful` - Confirms successful refresh
- `❌ [AUTH DEBUG] Failed to refresh token` - Shows refresh failures

## Best Practices

### For Developers
1. Use `useAuthenticatedRequest` for all authenticated API calls
2. Implement `useAuthError` in layouts to handle auth errors globally
3. Monitor console logs during development for auth debugging

### For Production
1. Ensure backend token expiry times are reasonable (15-60 minutes)
2. Configure proper CORS for token refresh endpoints
3. Monitor auth error rates in application logs

## Troubleshooting

### Common Issues

**"RefreshTokenError" in session:**
- Backend refresh endpoint is not responding correctly
- Refresh token has been revoked or expired
- Network connectivity issues

**Infinite refresh loops:**
- Check that session updates don't trigger unnecessary refreshes
- Verify buffer time configuration
- Look for circular dependencies in auth hooks

**Failed authenticated requests:**
- Verify API endpoints return proper 401 responses
- Check that retry logic is working correctly
- Ensure session data is properly updated after refresh

### Debug Steps
1. Check browser console for auth debug logs
2. Verify backend API responses using network tab
3. Test token refresh endpoint directly
4. Check session storage for corruption

## Migration Notes

### Breaking Changes
- `useAuthError` hook now includes additional methods
- Session structure includes new error handling
- Some TypeScript types have been extended

### Backward Compatibility
- Existing authentication flows continue to work
- No changes to sign-in/sign-out behavior
- API client methods remain unchanged

## Future Improvements

1. **Token Storage**: Consider implementing secure token storage for better persistence
2. **Offline Support**: Add offline token validation for PWA scenarios
3. **Multi-Tab Sync**: Synchronize auth state across browser tabs
4. **Analytics**: Add metrics for token refresh success/failure rates

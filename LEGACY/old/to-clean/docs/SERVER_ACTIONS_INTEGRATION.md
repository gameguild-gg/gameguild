# Server Actions Integration with CMS Backend

This document explains how to use the server actions integration with your CMS backend for authentication and tenant
management.

## Overview

The integration uses Next.js server actions following your existing pattern with the HTTP client factory. All server
actions automatically include authentication headers and tenant context when making requests to your CMS backend.

## Server Actions

### Authentication Actions

#### Email/Password Authentication

```typescript
import { signInWithEmailAndPassword, signUpWithEmailAndPassword } from '@/lib/auth/email-password-actions';

// In your component with useActionState
const [state, formAction, isPending] = useActionState(signInWithEmailAndPassword, initialState);
```

#### Other Authentication Operations

```typescript
import { sendEmailVerification, verifyEmail, forgotPassword, resetPassword, changePassword } from '@/lib/auth/auth-actions';
```

### Tenant Management Actions

```typescript
import { getUserTenants, getTenantById, createTenant, updateTenant, deleteTenant } from '@/lib/auth/tenant-actions';

// Get user's tenants (server-side)
const result = await getUserTenants();

// Create tenant with form
const [state, formAction, isPending] = useActionState(createTenant, initialState);
```

## HTTP Client Integration

Your existing `FetchHttpClientAdapter` has been enhanced to:

1. **Auto-include authentication headers** from the session
2. **Add tenant context** via `X-Tenant-Id` header
3. **Handle CMS backend responses** with proper error handling

```typescript
// The HTTP client automatically adds:
// - Authorization: Bearer {accessToken}
// - X-Tenant-Id: {currentTenantId}
// - Content-Type: application/json
```

## Usage Examples

### 1. Email/Password Sign In Form

```tsx
'use client';

import { useActionState } from 'react';
import { signInWithEmailAndPassword, SignInFormState } from '@/lib/auth/email-password-actions';

const initialState: SignInFormState = { success: false };

export function SignInForm() {
  const [state, formAction, isPending] = useActionState(signInWithEmailAndPassword, initialState);

  return (
    <form action={formAction}>
      {state.error && <div className="error">{state.error}</div>}
      {state.success && <div className="success">Welcome {state.data?.user.email}!</div>}

      <input name="email" type="email" required />
      <input name="password" type="password" required />
      <input name="tenantId" type="text" placeholder="Tenant ID (optional)" />

      <button type="submit" disabled={isPending}>
        {isPending ? 'Signing in...' : 'Sign In'}
      </button>
    </form>
  );
}
```

### 2. Tenant Management

```tsx
'use client';

import { useEffect, useState } from 'react';
import { getUserTenants, createTenant } from '@/lib/auth/tenant-actions';

export function TenantManager() {
  const [tenants, setTenants] = useState([]);

  useEffect(() => {
    async function loadTenants() {
      const result = await getUserTenants();
      if (result.success) {
        setTenants(result.data);
      }
    }
    loadTenants();
  }, []);

  return (
    <div>
      {tenants.map((tenant) => (
        <div key={tenant.id}>{tenant.name}</div>
      ))}
    </div>
  );
}
```

### 3. Password Reset Flow

```tsx
'use client';

import { useActionState } from 'react';
import { forgotPassword, resetPassword } from '@/lib/auth/auth-actions';

// Forgot password form
export function ForgotPasswordForm() {
  const [state, formAction, isPending] = useActionState(forgotPassword, { success: false });

  return (
    <form action={formAction}>
      {state.error && <div>{state.error}</div>}
      {state.success && <div>{state.message}</div>}

      <input name="email" type="email" placeholder="Your email" required />
      <button type="submit" disabled={isPending}>
        Send Reset Email
      </button>
    </form>
  );
}

// Reset password form
export function ResetPasswordForm() {
  const [state, formAction, isPending] = useActionState(resetPassword, { success: false });

  return (
    <form action={formAction}>
      {state.error && <div>{state.error}</div>}
      {state.success && <div>{state.message}</div>}

      <input name="token" type="hidden" value="reset-token-from-url" />
      <input name="newPassword" type="password" placeholder="New password" required />
      <input name="confirmPassword" type="password" placeholder="Confirm password" required />
      <button type="submit" disabled={isPending}>
        Reset Password
      </button>
    </form>
  );
}
```

## Server Action States

All server actions return a consistent state structure:

```typescript
type ActionState = {
  error?: string; // ErrorMessage message if action failed
  success?: boolean; // True if action succeeded
  message?: string; // Success message (for some actions)
  data?: any; // Response data (varies by action)
};
```

## ErrorMessage Handling

Server actions include comprehensive error handling:

1. **Authentication errors** - "Not authenticated"
2. **Validation errors** - "Email and password are required"
3. **Backend errors** - ErrorMessage messages from CMS backend
4. **Network errors** - "An unexpected error occurred"

## Integration with NextAuth

Server actions work seamlessly with your NextAuth setup:

1. **Session data** is automatically accessed in server actions
2. **Access tokens** are included in API requests
3. **Tenant context** from session is added to requests
4. **Token refresh** is handled by NextAuth callbacks

## File Structure

```
src/lib/auth/
├── email-password-github.auth.actions.ts     # Email/password auth actions
├── auth-github.auth.actions.ts               # Other auth operations
├── tenant-github.auth.actions.ts             # Tenant management actions
├── sign-in-with-google.ts        # Google OAuth (unchanged)
├── sign-in-with-web3.ts          # Web3 auth (unchanged)
└── sign-in-with-magic-link.ts    # Magic link auth (unchanged)
```

## Benefits of Server Actions

1. **Server-side execution** - Secure API calls from server
2. **Form integration** - Works with HTML forms and `useActionState`
3. **Progressive enhancement** - Works without JavaScript
4. **Type safety** - Full TypeScript support
5. **ErrorMessage handling** - Consistent error states
6. **Caching** - Server-side caching benefits

## Migration from Client-side API

If you have existing client-side API calls, you can gradually migrate:

```typescript
// Old client-side approach
const handleSignIn = async () => {
  const response = await fetch('/api/auth/signin', { ... });
  // Handle response
};

// New server action approach
const [state, formAction] = useActionState(signInWithEmailAndPassword, initialState);
// Use formAction in form
```

## Testing Server Actions

```typescript
// You can test server actions directly
import { signInWithEmailAndPassword } from '@/lib/auth/email-password-actions';

const formData = new FormData();
formData.append('email', 'test@example.com');
formData.append('password', 'password123');

const result = await signInWithEmailAndPassword({}, formData);
console.log(result); // { success: true, data: { ... } }
```

This server actions integration provides a robust, type-safe way to interact with your CMS backend while following
Next.js best practices and your existing architecture patterns.

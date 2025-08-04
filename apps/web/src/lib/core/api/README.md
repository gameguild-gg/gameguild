# Using NextAuth.js Sessions with Auto-Generated API Client

This guide shows how to use your NextAuth.js sessions with the auto-generated API client in server actions.

## Overview

Your project uses `@hey-api/openapi-ts` to generate a type-safe API client. The generated SDK provides several ways to
handle authentication:

1. **Configure the default client** (Recommended for server actions)
2. **Create custom clients per request**
3. **Pass options directly to SDK functions** (Your current approach)
4. **Use utility functions for common configurations**

## Method 1: Configure Default Client (Recommended)

### Setup

Create an authentication utility:

```typescript
// /lib/api/authenticated-client.ts
'use server';

import { auth } from '@/auth';
import { client } from '@/lib/api/generated/client.gen';
import { environment } from '@/configs/environment';

export async function configureAuthenticatedClient() {
  const session = await auth();

  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  // Configure the default client with authentication
  client.setConfig({
    baseUrl: environment.apiBaseUrl,
    headers: {
      Authorization: `Bearer ${session.api.accessToken}`,
      'X-Tenant-Id': session.currentTenant?.id,
      'Content-Type': 'application/json',
    },
  });

  return session;
}
```

### Usage in Server Actions

```typescript
'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { getTestingRequests, postTestingRequests } from '@/lib/api/generated';

export async function getTestingRequestsData() {
  // Configure authentication once
  await configureAuthenticatedClient();

  try {
    // Use SDK functions directly - no need to pass headers
    const response = await getTestingRequests();
    return response.data || [];
  } catch (error) {
    console.error('Error:', error);
    throw new Error('Failed to fetch testing requests');
  }
}

export async function createTestingRequest(formData: FormData) {
  const session = await configureAuthenticatedClient();

  const title = formData.get('title') as string;

  try {
    const response = await postTestingRequests({
      body: { title, creatorId: session.user.id },
    });

    return { success: true, data: response.data };
  } catch (error) {
    return { success: false, error: 'Failed to create request' };
  }
}
```

## Method 2: Custom Client per Request

```typescript
'use server';

import { auth } from '@/auth';
import { createClient } from '@/lib/api/generated/client';
import { getTestingRequests } from '@/lib/api/generated';

export async function getTestingRequestsWithCustomClient() {
  const session = await auth();

  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  // Create a custom client for this request
  const authenticatedClient = createClient({
    baseUrl: environment.apiBaseUrl,
    headers: {
      Authorization: `Bearer ${session.api.accessToken}`,
      'X-Tenant-Id': session.currentTenant?.id,
    },
  });

  try {
    // Pass the custom client to the SDK function
    const response = await getTestingRequests({
      client: authenticatedClient,
    });

    return response.data || [];
  } catch (error) {
    throw new Error('Failed to fetch testing requests');
  }
}
```

## Method 3: Direct Options (Your Current Approach)

```typescript
'use server';

import { auth } from '@/auth';
import { getTestingRequests } from '@/lib/api/generated';

export async function getTestingRequestsWithOptions() {
  const session = await auth();

  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    // Pass options directly (like your current testing lab actions)
    const response = await getTestingRequests({
      baseUrl: environment.apiBaseUrl,
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'X-Tenant-Id': session.currentTenant?.id,
      },
    });

    return response.data || [];
  } catch (error) {
    throw new Error('Failed to fetch testing requests');
  }
}
```

## Method 4: Utility Functions

```typescript
'use server';

import { auth } from '@/auth';

export async function getAuthenticatedRequestOptions() {
  const session = await auth();

  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  return {
    baseUrl: environment.apiBaseUrl,
    headers: {
      Authorization: `Bearer ${session.api.accessToken}`,
      'X-Tenant-Id': session.currentTenant?.id,
      'Content-Type': 'application/json',
    },
  };
}

// Usage
export async function getTestingRequestsWithUtility() {
  const options = await getAuthenticatedRequestOptions();

  try {
    const response = await getTestingRequests(options);
    return response.data || [];
  } catch (error) {
    throw new Error('Failed to fetch testing requests');
  }
}
```

## Using in React Components

### With useActionState Hook

```typescript
'use client';

import { useActionState } from 'react';
import { createTestingRequest } from '@/lib/actions/testing-actions';

export function TestingRequestForm() {
  const [state, formAction, isPending] = useActionState(createTestingRequest, {
    success: false,
    error: '',
  });

  return (
    <form action={formAction}>
      <input name="title" placeholder="Request title" required />

      <button type="submit" disabled={isPending}>
        {isPending ? 'Creating...' : 'Create Request'}
      </button>

      {state.success && <p className="text-green-600">Success!</p>}
      {state.error && <p className="text-red-600">Error: {state.error}</p>}
    </form>
  );
}
```

### Direct Server Action Calls

```typescript
'use client';

import { getTestingRequests } from '@/lib/actions/testing-actions';

export function TestingRequestsList() {
  const handleRefresh = async () => {
    try {
      const requests = await getTestingRequests();
      console.log('Requests:', requests);
    } catch (error) {
      console.error('Failed to refresh:', error);
    }
  };

  return (
    <div>
      <button onClick={handleRefresh}>
        Refresh Data
      </button>
    </div>
  );
}
```

## Available Session Data

Your NextAuth session provides:

```typescript
const session = await auth();

// User information
session.user.id; // User ID
session.user.email; // User email
session.user.username; // Username

// API authentication
session.api.accessToken; // Bearer token for API calls

// Tenant information (multi-tenant support)
session.currentTenant; // Current selected tenant
session.availableTenants; // Array of available tenants

// Error states
session.error; // Authentication errors (if any)
```

## Error Handling

Always handle authentication errors in your server actions:

```typescript
export async function authenticatedAction() {
  const session = await auth();

  // Handle session errors
  if (session?.error === 'CorruptedSessionError') {
    throw new Error('Session corrupted. Please sign in again.');
  }

  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  // Continue with your logic...
}
```

## Best Practices

1. **Use Method 1 (default client configuration)** for most server actions
2. **Handle errors gracefully** with try-catch blocks
3. **Validate session data** before making API calls
4. **Include tenant context** when your API requires it
5. **Use revalidateTag** to update cached data after mutations
6. **Return consistent state shapes** for useActionState compatibility

## Migration from Current Approach

Your existing testing lab actions use Method 3 (direct options). You can gradually migrate to Method 1 for cleaner code:

```typescript
// Before (your current approach)
const response = await getTestingRequests({
  baseUrl: environment.apiBaseUrl,
  headers: {
    Authorization: `Bearer ${session.api.accessToken}`,
  },
});

// After (recommended approach)
await configureAuthenticatedClient();
const response = await getTestingRequests();
```

This provides cleaner, more maintainable code while leveraging your auto-generated API client effectively.

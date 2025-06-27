# NextAuth.js Integration with CMS Backend

This integration connects the Next.js frontend with the .NET CMS backend for authentication and tenant management.

## Features

- 🔐 **Google OAuth Authentication** - Seamless sign-in with Google accounts
- 🏢 **Multi-Tenant Support** - Switch between different tenant contexts
- 🔄 **Automatic Token Refresh** - Handles token expiration transparently
- 🛡️ **Protected Routes** - Middleware-based route protection
- 📡 **Authenticated API Calls** - Easy-to-use API client with tenant context

## Setup

### 1. Environment Variables

Make sure your `.env.local` file includes:

```env
# NextAuth
NEXTAUTH_SECRET=your-nextauth-secret
NEXTAUTH_URL=http://localhost:3000

# Google OAuth
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret

# API Configuration
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_WEB_URL=http://localhost:3000
```

### 2. CMS Backend Endpoints

The integration expects the following endpoints on your CMS backend:

- `POST /auth/google/callback` - Google OAuth token verification
- `POST /auth/refresh-token` - Token refresh
- `POST /auth/revoke-token` - Token revocation
- `GET /tenants` - Get user's available tenants
- `GET /tenants/{id}` - Get specific tenant details

## Usage

### Basic Authentication

```tsx
import { useSession, signIn, signOut } from 'next-auth/react';

function MyComponent() {
  const { data: session, status } = useSession();

  if (status === 'loading') return <p>Loading...</p>;
  
  if (session) {
    return (
      <>
        <p>Signed in as {session.user.email}</p>
        <button onClick={() => signOut()}>Sign out</button>
      </>
    );
  }
  
  return (
    <>
      <p>Not signed in</p>
      <button onClick={() => signIn('google')}>Sign in with Google</button>
    </>
  );
}
```

### Tenant Management

```tsx
import { useTenant } from '@/lib/context/TenantProvider';
import { TenantSelector } from '@/components/auth/TenantSelector';

function Dashboard() {
  const { currentTenant, availableTenants, switchTenant } = useTenant();

  return (
    <div>
      <h1>Current Tenant: {currentTenant?.name}</h1>
      <TenantSelector />
      
      {/* Your tenant-specific content */}
    </div>
  );
}
```

### Authenticated API Calls

```tsx
import { useAuthenticatedApi } from '@/lib/context/TenantProvider';

function DataComponent() {
  const { makeRequest } = useAuthenticatedApi();

  const fetchData = async () => {
    try {
      // Automatically includes authentication headers and tenant context
      const data = await makeRequest('/api/some-endpoint');
      console.log(data);
    } catch (error) {
      console.error('API Error:', error);
    }
  };

  return (
    <button onClick={fetchData}>Fetch Data</button>
  );
}
```

### Using Services

```tsx
import { TenantService } from '@/lib/services/tenant.service';
import { AuthService } from '@/lib/services/auth.service';
import { useSession } from 'next-auth/react';

function AdminPanel() {
  const { data: session } = useSession();

  const createTenant = async () => {
    if (!session?.accessToken) return;

    try {
      const newTenant = await TenantService.createTenant({
        name: 'New Tenant',
        description: 'A new tenant',
        isActive: true,
      }, session.accessToken);
      
      console.log('Created tenant:', newTenant);
    } catch (error) {
      console.error('Failed to create tenant:', error);
    }
  };

  return (
    <button onClick={createTenant}>Create Tenant</button>
  );
}
```

## File Structure

```
src/
├── components/
│   ├── auth/
│   │   └── TenantSelector.tsx      # Tenant switching component
│   └── providers/
│       └── Providers.tsx           # Provider wrapper
├── hooks/
│   └── useAuthError.ts             # Authentication error handling
├── lib/
│   ├── context/
│   │   └── tenant-provider.tsx       # Tenant state management
│   ├── services/
│   │   ├── auth.service.ts         # Authentication operations
│   │   └── tenant.service.ts       # Tenant operations
│   └── api-client.ts               # Base API client
├── types/
│   ├── auth.ts                     # Authentication types
│   ├── types.ts                   # Tenant types
│   └── next-auth.d.ts              # NextAuth type extensions
├── configs/
│   └── auth.config.ts              # NextAuth configuration
└── middleware.ts                   # Route protection middleware
```

## Authentication Flow

1. **User clicks "Sign in with Google"**
2. **NextAuth redirects to Google OAuth**
3. **Google returns with ID token**
4. **NextAuth sends ID token to CMS backend** (`/auth/google/callback`)
5. **CMS validates token and returns user data + JWT tokens**
6. **NextAuth stores tokens in JWT session**
7. **User is signed in with access to tenant context**

## Token Management

- **Access tokens** are automatically included in API requests
- **Refresh tokens** are used to get new access tokens when they expire
- **Token refresh** happens automatically in the JWT callback
- **Failed refresh** redirects user to sign-in page

## Tenant Context

- **Multi-tenant awareness** - API calls include tenant headers
- **Tenant switching** - Users can switch between available tenants
- **Tenant-scoped data** - All API calls respect current tenant context

## Error Handling

- **Authentication errors** are caught and handled gracefully
- **Token expiration** triggers automatic refresh or re-authentication
- **API errors** are propagated with meaningful error messages
- **Network errors** are handled with appropriate user feedback

## Security Features

- **JWT tokens** stored securely in NextAuth session
- **Automatic token refresh** prevents expired token usage
- **Route protection** via middleware
- **Tenant isolation** through headers and context
- **Secure token storage** using NextAuth's secure session handling

## Customization

### Adding New OAuth Providers

```typescript
// In auth.config.ts
import GitHub from 'next-auth/providers/github';

export const authConfig: NextAuthConfig = {
  providers: [
    Google({ /* config */ }),
    GitHub({ /* config */ }),
    // Add more providers
  ],
  // ...
};
```

### Custom API Endpoints

```typescript
// In your service files
import { apiClient } from '@/lib/api-client';

export class CustomService {
  static async customEndpoint(data: any, accessToken: string) {
    return apiClient.authenticatedRequest('/custom/endpoint', accessToken, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }
}
```

## Troubleshooting

### Common Issues

1. **"No access token available"** - User needs to sign in
2. **"Tenant not found"** - Ensure tenant exists and user has access
3. **"Token refresh failed"** - Backend may be down or token is invalid
4. **API calls failing** - Check if endpoints match backend routes

### Debugging

Enable debug logging in NextAuth:

```typescript
// In auth.config.ts
export const authConfig: NextAuthConfig = {
  debug: process.env.NODE_ENV === 'development',
  // ...
};
```

Check browser console for authentication flow details and API call logs.

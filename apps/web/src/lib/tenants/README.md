# Tenant Context System

This tenant context system provides flexible tenant management with automatic session synchronization and optional server-side data injection for optimal performance.

## Key Features

✅ **Flexible Initialization**: Supports both automatic session initialization AND server-side data injection  
✅ **Type-Safe**: Full TypeScript support with proper `TenantResponse` types  
✅ **Error Handling**: Comprehensive error states and loading indicators  
✅ **Optimistic Updates**: Smooth tenant switching with session sync  
✅ **Utility Hooks**: Pre-built hooks for common tenant operations  
✅ **Performance**: Quick start with server-side data, automatic session sync

## Basic Usage

### 1. Automatic Session Initialization (Default)

```tsx
// app/layout.tsx - Automatically reads from NextAuth session
import { TenantProvider } from '@/lib/tenants';

export default function RootLayout({ children }) {
  return (
    <SessionProvider>
      <TenantProvider>{children}</TenantProvider>
    </SessionProvider>
  );
}
```

### 2. Server-Side Data Injection (Recommended for SSR)

```tsx
// app/layout.tsx - Quick start with server data
import { TenantProvider } from '@/lib/tenants';
import { auth } from '@/auth'; // NextAuth server-side method

export default async function RootLayout({ children }) {
  const session = await auth();

  // Extract tenant data from session
  const serverTenantData = {
    currentTenant: session?.currentTenant,
    availableTenants: session?.availableTenants || [],
  };

  return (
    <SessionProvider>
      <TenantProvider initialState={serverTenantData}>{children}</TenantProvider>
    </SessionProvider>
  );
}
```

### 3. Server Actions with Tenant Context

```tsx
// app/dashboard/page.tsx
import { TenantProvider } from '@/lib/tenants';
import { auth } from '@/auth';

export default async function DashboardPage() {
  const session = await auth();

  // Server-side tenant data from session
  const serverTenantData = {
    currentTenant: session?.currentTenant,
    availableTenants: session?.availableTenants || [],
  };

  return (
    <TenantProvider initialState={serverTenantData}>
      <DashboardContent />
    </TenantProvider>
  );
}
```

### 4. Use Tenant Data in Components

```tsx
// components/header.tsx
import { useTenant, TenantSelector } from '@/lib/tenants';

export function Header() {
  const { currentTenant, loading, error } = useTenant();

  if (loading) return <div>Loading tenant...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <header>
      <h1>Welcome to {currentTenant?.name || 'GameGuild'}</h1>
      <TenantSelector />
    </header>
  );
}
```

### 3. Update Tenant Data Programmatically

```tsx
// components/tenant-manager.tsx
import { useTenant } from '@/lib/tenants';

export function TenantManager() {
  const { updateTenantData, currentTenant } = useTenant();

  const refreshTenantData = async () => {
    // Fetch fresh tenant data from server
    const freshData = await fetchTenantDataFromServer();

    // Update the context with new data
    updateTenantData({
      currentTenant: freshData.currentTenant,
      availableTenants: freshData.availableTenants,
    });
  };

  const switchToFirstTenant = () => {
    // Programmatically set a specific tenant
    updateTenantData({
      currentTenant: availableTenants[0],
    });
  };

  return (
    <div>
      <button onClick={refreshTenantData}>Refresh Tenant Data</button>
      <button onClick={switchToFirstTenant}>Switch to First Tenant</button>
    </div>
  );
}
```

### 5. Tenant-Scoped Data Fetching

```tsx
// components/projects-list.tsx
import { useTenantScoped } from '@/lib/tenants';

export function ProjectsList() {
  const { addTenantToParams, currentTenant } = useTenantScoped();

  const fetchProjects = async () => {
    const params = addTenantToParams({ limit: 10 });
    const response = await fetch('/api/projects', {
      method: 'POST',
      body: JSON.stringify(params),
    });
    return response.json();
  };

  // Use fetchProjects in your data fetching logic
}
```

### 4. Permission Checks

```tsx
// components/admin-panel.tsx
import { useTenantPermissions } from '@/lib/tenants';

export function AdminPanel() {
  const { canAccessTenantAdmin, canCreateInTenant } = useTenantPermissions();

  if (!canAccessTenantAdmin()) {
    return <div>Access denied</div>;
  }

  return (
    <div>
      <h2>Admin Panel</h2>
      {canCreateInTenant() && <button>Create New Project</button>}
    </div>
  );
}
```

## Session Integration

The tenant context automatically syncs with your NextAuth session. Your session should include:

```typescript
// types/next-auth.d.ts
declare module 'next-auth' {
  interface Session {
    user: {
      id: string;
      name: string;
      email: string;
    };
    currentTenant?: TenantResponse;
    availableTenants?: TenantResponse[];
    accessToken?: string;
  }
}
```

## Available Hooks

### `useTenant()`

Core tenant context with state and actions:

- `currentTenant`: Current active tenant
- `availableTenants`: List of user's accessible tenants
- `loading`: Loading state for tenant operations
- `error`: Error message if tenant operations fail
- `switchCurrentTenant(tenantId)`: Switch to a different tenant
- `updateTenantData(data)`: Manually update tenant data (current tenant and/or available tenants)
- `setError(message)`: Set an error message
- `clearError()`: Clear current error

### `useTenantUtils()`

Utility functions for tenant operations:

- `isTenantAvailable(tenantId)`: Check if tenant is available
- `getTenantById(tenantId)`: Get tenant by ID
- `hasAccessToTenant(tenantId)`: Check user access
- `getActiveTenants()`: Get only active tenants
- `isCurrentTenantActive()`: Check if current tenant is active
- `getTenantDisplayName(tenant)`: Get display name with fallback
- `hasMultipleTenants()`: Check if user has multiple tenants
- `getDefaultTenant()`: Get default (first active) tenant

### `useTenantScoped()`

Tenant-scoped data fetching utilities:

- `addTenantToParams(params)`: Add tenant ID to request params
- `addTenantToSearchParams(searchParams)`: Add tenant ID to URL params
- `addTenantToHeaders(headers)`: Add tenant ID to request headers
- `belongsToCurrentTenant(data)`: Check if data belongs to current tenant
- `filterByCurrentTenant(items)`: Filter array by current tenant

### `useTenantPermissions()`

Permission checking utilities:

- `canSwitchToTenant(tenantId)`: Check if user can switch to tenant
- `canAccessTenantAdmin()`: Check admin access
- `canCreateInTenant()`: Check creation permissions
- `canModifyTenantSettings()`: Check modification permissions
- `getAccessibleTenantIds()`: Get list of accessible tenant IDs

### `useAuthenticatedApi()`

Authenticated API requests with tenant context:

- `makeRequest(endpoint, options)`: Make authenticated API request
- `isAuthenticated`: Boolean authentication status
- `tenantId`: Current tenant ID
- `currentTenant`: Current tenant object

## Configuration Options

The `TenantProvider` accepts these props:

- `initialState?`: Initial tenant data (currentTenant, availableTenants, loading, error)

## Usage Patterns

### Pattern 1: Pure Session Mode (Default)

```tsx
<TenantProvider>{children}</TenantProvider>
```

- Automatically reads from session
- Real-time sync with session changes
- Good for: SPAs, client-heavy apps

### Pattern 2: Server-First Mode (Recommended)

```tsx
<TenantProvider initialState={serverData}>{children}</TenantProvider>
```

- Quick start with server data
- Still syncs with session for updates
- Good for: SSR, better initial page loads

## Migration Guide

### From Previous Version

```tsx
// Before - only session data
<TenantProvider>
  {children}
</TenantProvider>

// After - same behavior, always syncs with session
<TenantProvider>
  {children}
</TenantProvider>
```

### Adding Server-Side Data

```tsx
// Before - client-only initialization
<TenantProvider>{children}</TenantProvider>;

// After - server + client hybrid (recommended)
const session = await auth();
<TenantProvider
  initialState={{
    currentTenant: session?.currentTenant,
    availableTenants: session?.availableTenants || [],
  }}
>
  {children}
</TenantProvider>;
```

### Programmatic Updates

```tsx
// New capability - update tenant data from anywhere
const { updateTenantData } = useTenant();

// Update current tenant only
updateTenantData({ currentTenant: newTenant });

// Update available tenants only
updateTenantData({ availableTenants: freshTenants });

// Update both
updateTenantData({
  currentTenant: newTenant,
  availableTenants: freshTenants,
});
```

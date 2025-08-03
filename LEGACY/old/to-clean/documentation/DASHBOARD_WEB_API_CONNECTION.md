# Dashboard Web-API Connection Guide

This guide explains how the Next.js 15+ dashboard is connected to the .NET API backend using server-side rendering (SSR) and server actions.

## Architecture Overview

The dashboard implementation follows Next.js 15+ best practices with:

- **Server-Side Rendering (SSR)**: Data is fetched on the server before rendering
- **Server Actions**: Used for data mutations and cache invalidation  
- **Client Components**: Only for interactive UI elements like filters and refresh buttons
- **Proper Caching**: Using Next.js cache tags and revalidation strategies

## File Structure

```
apps/web/src/
├── lib/dashboard/
│   └── github.auth.actions.ts                          # Server actions for dashboard data
├── components/dashboard/
│   ├── server-dashboard-stats.tsx          # SSR dashboard statistics
│   ├── system-status.tsx                   # System status component
│   ├── refresh-button.tsx                  # Client component for refresh
│   └── dashboard-filters.tsx               # Client component for filters
└── app/[locale]/(dashboard)/dashboard/overview/
    └── page.tsx                            # Main dashboard page (SSR)
```

## Key Components

### 1. Server Actions (`lib/dashboard/github.auth.actions.ts`)

- `getDashboardData()`: Main server action that fetches all dashboard data
- `refreshDashboardData()`: Invalidates cache and refreshes data  
- `updateStatisticsFilters()`: Updates filters and fetches new data
- Uses proper authentication with session tokens
- Implements Next.js 15+ caching with tags and revalidation

### 2. Recent Activity Actions (`lib/dashboard/recent-activity-github.auth.actions.ts`)

- `getRecentActivity()`: Fetches recent user and post activities from API
- `refreshRecentActivity()`: Refreshes activity data
- Combines data from multiple API endpoints:
  - `/api/users` - Recent user registrations and updates
  - `/api/posts` - Recent post creations
- Proper TypeScript interfaces for API responses
- Real-time activity feed with formatted timestamps

### 3. Dashboard Page (`page.tsx`)

- Uses `async` function for SSR
- Processes search parameters for filtering
- Fetches data using server actions on the server
- Implements proper error handling and loading states
- Uses Suspense for progressive loading
- **Connected to real API data for statistics and recent activity**

### 4. Components

#### Server Components:
- `ServerDashboardStats`: Renders statistics using server data
- `SystemStatus`: Shows system health status
- `ServerRecentActivity`: **NEW** - Shows real recent activity from API

#### Client Components:
- `RefreshButton`: Uses `useTransition` for optimistic updates
- `DashboardFilters`: **NEW** - URL-based filtering for statistics with date ranges
- Uses `useRouter` for URL-based filtering

## API Connection

### Authentication
The dashboard connects to the API using JWT tokens from the user session:

```typescript
const session = await auth();
const response = await fetch(`${environment.apiBaseUrl}/api/users/statistics`, {
  headers: {
    Authorization: `Bearer ${session.api.accessToken}`,
    'Content-Type': 'application/json',
  },
  next: {
    revalidate: 300, // Cache for 5 minutes
    tags: ['dashboard-user-stats']
  }
});
```

### Available Endpoints

Currently connected to:
- `GET /api/users/statistics` - User statistics with date filtering
- `GET /api/users` - Recent user data for activity feed
- `GET /api/posts` - Recent posts for activity feed  
- `GET /health` - System health check

### Data Flow

1. **Initial Load (SSR)**:
   ```
   Browser Request → Next.js Server → Server Actions → Multiple APIs → Database
                  ← Next.js Server ← Combined Data ← Multiple APIs ← Database
   ```

2. **Client Interactions**:
   ```
   Filter Change → URL Update → Server Re-render → Server Actions → APIs → Database
   Refresh Click → Client Action → Server Action → Cache Invalidation → Fresh Data
   ```

3. **Recent Activity Feed**:
   ```
   Page Load → getRecentActivity() → Promise.allSettled([
     fetchRecentUsers(),    // /api/users
     fetchRecentPosts()     // /api/posts  
   ]) → Combined & Sorted Activity Feed
   ```

## Caching Strategy

- **Server-side**: Uses Next.js `fetch` with cache tags
- **Revalidation**: 5 minutes for statistics, 1 minute for health checks
- **Manual Refresh**: Refresh button invalidates cache tags
- **Filter Updates**: URL parameters trigger server-side re-fetch

## Benefits of This Approach

1. **Performance**: Data is fetched on the server, reducing client-side loading
2. **SEO**: Fully rendered pages with real data
3. **User Experience**: Instant page loads with server-rendered content
4. **Security**: API calls happen on the server with proper authentication
5. **Caching**: Intelligent caching reduces API load
6. **Type Safety**: Full TypeScript support throughout the data flow

## Next Steps

To extend the dashboard:

1. **Add New Endpoints**: Create new functions in `github.auth.actions.ts`
2. **New Components**: Follow the server/client component pattern
3. **More Filters**: Extend the `DashboardFilters` component
4. **Real-time Data**: Consider WebSocket integration for live updates
5. **Error Boundaries**: Add more granular error handling

## Environment Variables

Make sure these are set:
- `NEXT_PUBLIC_API_URL`: API base URL for client-side calls
- `NEXTAUTH_SECRET`: For session handling
- API authentication configuration

This implementation provides a solid foundation for a modern, performant dashboard that properly connects the Next.js frontend with the .NET API backend.

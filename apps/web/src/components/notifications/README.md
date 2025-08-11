# Notification System Documentation

## Overview

A complete notification system has been implemented for your Next.js 15+ app with modern design patterns, TypeScript
support, and server actions integration. The system includes:

## Features

✅ **Complete Notification Component**: Modern dropdown with tabs (All, Unread, Archived)
✅ **Server Actions Integration**: Full CRUD operations with server-side actions
✅ **TypeScript Support**: Comprehensive type definitions
✅ **Real-time Updates**: Auto-refresh and visibility change detection
✅ **Interactive Actions**: Accept/Decline invites, Mark as read, Archive
✅ **Modern UI**: Uses shadcn/ui components with proper styling
✅ **Toast Notifications**: Success/error feedback
✅ **Badge Count**: Unread notification counter
✅ **Responsive Design**: Works on all screen sizes

## Files Created

### Types

- `src/types/notification.ts` - TypeScript definitions for notifications

### Server Actions

- `src/lib/actions/notification-github.auth.actions.ts` - Server-side CRUD operations

### Components

- `src/components/common/notifications/notification-dropdown.tsx` - Main notification dropdown
- `src/components/common/notifications/notification-provider.tsx` - Context provider for state management
- `src/components/common/notifications/default-footer.tsx` - Export file

### Integration

- Updated `src/components/common/header/default-footer.tsx` to include the notification dropdown

## Usage

### Basic Integration (Already Done)

The notification system is already integrated into your header component:

```tsx
import { NotificationDropdown } from '@/components/common/notifications';

// In your header component
<NotificationDropdown />;
```

### With Context Provider (Optional)

For more advanced state management across components:

```tsx
import { NotificationProvider } from '@/components/common/notifications';

function App({ children }) {
  return <NotificationProvider refreshInterval={30000}>{children}</NotificationProvider>;
}
```

### Using the Hook

```tsx
import { useNotifications, useUnreadNotificationsCount } from '@/components/common/notifications';

function MyComponent() {
  const { notifications, unreadCount, refreshNotifications } = useNotifications();
  const unreadCount = useUnreadNotificationsCount();

  // Use the data...
}
```

## Notification Types

The system supports various notification types:

- **comment**: User comments on projects
- **follow**: New followers
- **invite**: Project invitations
- **reminder**: Reminders from users
- **task**: Task assignments
- **mention**: User mentions
- **system**: System notifications

## Server Actions Available

- `getNotifications(filters)` - Fetch notifications with filtering
- `markNotificationAsRead(id)` - Mark single notification as read
- `markAllNotificationsAsRead()` - Mark all notifications as read
- `archiveNotification(id)` - Archive a notification
- `acceptProjectInvite(id)` - Accept project invitation
- `declineProjectInvite(id)` - Decline project invitation

## Customization

### Adding New Notification Types

1. Update the type in `src/types/notification.ts`
2. Add the icon in `getNotificationIcon()` function
3. Update server actions to handle the new type

### Styling

The components use Tailwind CSS and shadcn/ui. You can customize:

- Colors: Update the theme colors in your tailwind.config
- Icons: Modify the `getNotificationIcon()` function
- Layout: Adjust the component structure in `notification-dropdown.tsx`

### Data Source

Currently uses mock data. To connect to a real backend:

1. Replace mock data in `notification-github.auth.actions.ts`
2. Add actual API calls to your backend
3. Update the revalidation logic as needed

## Real-time Features

- **Auto-refresh**: Notifications refresh every 30 seconds
- **Visibility API**: Refreshes when user returns to tab
- **Instant Updates**: Actions update the UI immediately
- **Optimistic Updates**: UI updates before server confirmation

## Demo Data

The system includes realistic demo data with:

- Comments from John on Project Alpha
- Project invitation from John to Project Beta
- New follower (Jennifer Lee)
- Task assignment from Eve Monroe
- Reminder from Boyd Larkin
- Status update from Josh Krajcik

## Next Steps

1. **Connect to Real Data**: Replace mock data with actual API calls
2. **WebSocket Integration**: Add real-time updates via WebSockets
3. **Push Notifications**: Integrate browser push notifications
4. **Email Notifications**: Add email notification preferences
5. **Advanced Filtering**: Add date filters, user filters, etc.
6. **Notification Settings**: User preferences for notification types
7. **Bulk Actions**: Select multiple notifications for batch operations

## Code Quality

The implementation follows Next.js 15+ best practices:

- Server Actions for data mutations
- Client Components for interactivity
- Proper TypeScript typing
- React hooks for state management
- Accessibility considerations
- Performance optimizations
- ErrorMessage handling and loading states

## Testing

To test the notification system:

1. Click the bell icon in the header
2. Try different tabs (All, Unread, Archived)
3. Use the action buttons (Accept, Decline, etc.)
4. Mark notifications as read or archive them
5. Test the "Mark all as read" functionality

The system is fully functional with mock data and ready for production with real backend integration.

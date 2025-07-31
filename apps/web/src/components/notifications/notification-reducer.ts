import type { NotificationAction, NotificationState, Notification } from '@/types/notification';

export const initialNotificationState: NotificationState = {
  notifications: [],
  unreadCount: 0,
  isLoading: false,
  error: null,
  filters: {
    type: 'all',
    category: 'all',
    notificationType: 'all',
    priority: 'all',
    limit: 50,
    offset: 0,
  },
  lastFetch: null,
};

export function notificationReducer(state: NotificationState, action: NotificationAction): NotificationState {
  switch (action.type) {
    case 'SET_NOTIFICATIONS': {
      const notifications = action.payload;
      const unreadCount = notifications.filter((n) => !n.isRead && !n.isArchived).length;

      return {
        ...state,
        notifications,
        unreadCount,
        isLoading: false,
        error: null,
        lastFetch: new Date(),
      };
    }

    case 'ADD_NOTIFICATION': {
      const newNotification = action.payload;
      const updatedNotifications = [newNotification, ...state.notifications];
      const unreadCount = updatedNotifications.filter((n) => !n.isRead && !n.isArchived).length;

      return {
        ...state,
        notifications: updatedNotifications,
        unreadCount,
      };
    }

    case 'MARK_AS_READ': {
      const notificationId = action.payload;
      const updatedNotifications = state.notifications.map((notification) => (notification.id === notificationId ? { ...notification, isRead: true } : notification));
      const unreadCount = updatedNotifications.filter((n) => !n.isRead && !n.isArchived).length;

      return {
        ...state,
        notifications: updatedNotifications,
        unreadCount,
      };
    }

    case 'MARK_ALL_AS_READ': {
      const updatedNotifications = state.notifications.map((notification) => ({
        ...notification,
        isRead: true,
      }));

      return {
        ...state,
        notifications: updatedNotifications,
        unreadCount: 0,
      };
    }

    case 'ARCHIVE_NOTIFICATION': {
      const notificationId = action.payload;
      const updatedNotifications = state.notifications.map((notification) => (notification.id === notificationId ? { ...notification, isArchived: true, isRead: true } : notification));
      const unreadCount = updatedNotifications.filter((n) => !n.isRead && !n.isArchived).length;

      return {
        ...state,
        notifications: updatedNotifications,
        unreadCount,
      };
    }

    case 'TOGGLE_STAR': {
      const notificationId = action.payload;
      const updatedNotifications = state.notifications.map((notification) => (notification.id === notificationId ? { ...notification, isStarred: !notification.isStarred } : notification));

      return {
        ...state,
        notifications: updatedNotifications,
      };
    }

    case 'REMOVE_NOTIFICATION': {
      const notificationId = action.payload;
      const updatedNotifications = state.notifications.filter((notification) => notification.id !== notificationId);
      const unreadCount = updatedNotifications.filter((n) => !n.isRead && !n.isArchived).length;

      return {
        ...state,
        notifications: updatedNotifications,
        unreadCount,
      };
    }

    case 'SET_LOADING': {
      return {
        ...state,
        isLoading: action.payload,
      };
    }

    case 'SET_ERROR': {
      return {
        ...state,
        error: action.payload,
        isLoading: false,
      };
    }

    case 'SET_FILTERS': {
      return {
        ...state,
        filters: { ...state.filters, ...action.payload },
      };
    }

    case 'RESET_STATE': {
      return initialNotificationState;
    }

    default:
      return state;
  }
}

// Helper functions for filtering notifications
export function getFilteredNotifications(notifications: Notification[], filters: NotificationState['filters']): Notification[] {
  return notifications.filter((notification) => {
    // Filter by read status
    if (filters.type === 'unread' && notification.isRead) return false;
    if (filters.type === 'read' && !notification.isRead) return false;
    if (filters.type === 'archived' && !notification.isArchived) return false;

    // Filter by category
    if (filters.category === 'archive' && !notification.isArchived) return false;
    if (filters.category === 'following' && notification.isArchived) return false;

    // Filter by notification type
    if (filters.notificationType && filters.notificationType !== 'all' && notification.type !== filters.notificationType) return false;

    // Filter by priority
    if (filters.priority && filters.priority !== 'all' && notification.priority !== filters.priority) return false;

    return true;
  });
}

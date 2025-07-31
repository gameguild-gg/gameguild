'use client';

import React, { createContext, useContext, useReducer, useCallback, useEffect, useMemo } from 'react';
import type { NotificationState, NotificationAction, Notification, NotificationFilters } from '@/types/notification';
import { notificationReducer, initialNotificationState, getFilteredNotifications } from '@/lib/notifications/notification-reducer';
import { getNotifications, markNotificationAsRead, markAllNotificationsAsRead, archiveNotification } from '@/lib/communication/notifications/notifications.actions';

interface NotificationContextValue {
  state: NotificationState;
  dispatch: React.Dispatch<NotificationAction>;
  // Derived state
  filteredNotifications: Notification[];
  unreadCount: number;
  // Actions
  refreshNotifications: () => Promise<void>;
  markAsRead: (id: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  archiveNotification: (id: string) => Promise<void>;
  toggleStar: (id: string) => void;
  removeNotification: (id: string) => void;
  setFilters: (filters: Partial<NotificationFilters>) => void;
  addNotification: (notification: Notification) => void;
}

const NotificationContext = createContext<NotificationContextValue | undefined>(undefined);

interface NotificationProviderProps {
  children: React.ReactNode;
  initialData?: Notification[];
  refreshInterval?: number;
  enableAutoRefresh?: boolean;
}

export function NotificationProvider({ children, initialData = [], refreshInterval = 30000, enableAutoRefresh = true }: NotificationProviderProps) {
  const [state, dispatch] = useReducer(notificationReducer, {
    ...initialNotificationState,
    notifications: initialData,
    unreadCount: initialData.filter((n) => !n.isRead && !n.isArchived).length,
  });

  // Derived state using useMemo for performance
  const filteredNotifications = useMemo(() => getFilteredNotifications(state.notifications, state.filters), [state.notifications, state.filters]);

  const unreadCount = useMemo(() => state.notifications.filter((n) => !n.isRead && !n.isArchived).length, [state.notifications]);

  // Actions
  const refreshNotifications = useCallback(async () => {
    try {
      dispatch({ type: 'SET_LOADING', payload: true });
      const response = await getNotifications(state.filters);
      dispatch({ type: 'SET_NOTIFICATIONS', payload: response.notifications });
    } catch (error) {
      console.error('Failed to refresh notifications:', error);
      dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Failed to load notifications' });
    }
  }, [state.filters]);

  const markAsRead = useCallback(async (id: string) => {
    try {
      await markNotificationAsRead(id);
      dispatch({ type: 'MARK_AS_READ', payload: id });
    } catch (error) {
      console.error('Failed to mark notification as read:', error);
      dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Failed to mark as read' });
    }
  }, []);

  const markAllAsRead = useCallback(async () => {
    try {
      await markAllNotificationsAsRead();
      dispatch({ type: 'MARK_ALL_AS_READ' });
    } catch (error) {
      console.error('Failed to mark all notifications as read:', error);
      dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Failed to mark all as read' });
    }
  }, []);

  const archiveNotificationAction = useCallback(async (id: string) => {
    try {
      await archiveNotification(id);
      dispatch({ type: 'ARCHIVE_NOTIFICATION', payload: id });
    } catch (error) {
      console.error('Failed to archive notification:', error);
      dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Failed to archive notification' });
    }
  }, []);

  const toggleStar = useCallback((id: string) => {
    dispatch({ type: 'TOGGLE_STAR', payload: id });
  }, []);

  const removeNotification = useCallback((id: string) => {
    dispatch({ type: 'REMOVE_NOTIFICATION', payload: id });
  }, []);

  const setFilters = useCallback((filters: Partial<NotificationFilters>) => {
    dispatch({ type: 'SET_FILTERS', payload: filters });
  }, []);

  const addNotification = useCallback((notification: Notification) => {
    dispatch({ type: 'ADD_NOTIFICATION', payload: notification });
  }, []);

  // Auto-refresh notifications when enabled
  useEffect(() => {
    if (!enableAutoRefresh || refreshInterval <= 0) return;

    const interval = setInterval(refreshNotifications, refreshInterval);
    return () => clearInterval(interval);
  }, [refreshNotifications, refreshInterval, enableAutoRefresh]);

  // Listen for visibility changes to refresh when user comes back to tab
  useEffect(() => {
    if (!enableAutoRefresh) return;

    const handleVisibilityChange = () => {
      if (!document.hidden) {
        refreshNotifications();
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => document.removeEventListener('visibilitychange', handleVisibilityChange);
  }, [refreshNotifications, enableAutoRefresh]);

  // Initial load if no initial data provided
  useEffect(() => {
    if (initialData.length === 0 && !state.lastFetch) {
      refreshNotifications();
    }
  }, [initialData.length, state.lastFetch, refreshNotifications]);

  const value: NotificationContextValue = useMemo(
    () => ({
      state,
      dispatch,
      filteredNotifications,
      unreadCount,
      refreshNotifications,
      markAsRead,
      markAllAsRead,
      archiveNotification: archiveNotificationAction,
      toggleStar,
      removeNotification,
      setFilters,
      addNotification,
    }),
    [state, dispatch, filteredNotifications, unreadCount, refreshNotifications, markAsRead, markAllAsRead, archiveNotificationAction, toggleStar, removeNotification, setFilters, addNotification],
  );

  return <NotificationContext.Provider value={value}>{children}</NotificationContext.Provider>;
}

export function useNotifications(): NotificationContextValue {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
}

// Specialized hooks for common use cases
export function useUnreadNotificationsCount(): number {
  const { unreadCount } = useNotifications();
  return unreadCount;
}

export function useNotificationFilters() {
  const { state, setFilters } = useNotifications();
  return {
    filters: state.filters,
    setFilters,
  };
}

export function useNotificationActions() {
  const { markAsRead, markAllAsRead, archiveNotification, toggleStar, removeNotification, addNotification, refreshNotifications } = useNotifications();
  
  return {
    markAsRead,
    markAllAsRead,
    archiveNotification,
    toggleStar,
    removeNotification,
    addNotification,
    refreshNotifications,
  };
}

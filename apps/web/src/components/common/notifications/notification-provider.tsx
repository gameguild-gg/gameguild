'use client';

import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import type { Notification, NotificationResponse } from '@/types/notification';
import { getNotifications } from '@/lib/actions/notification-actions';

interface NotificationContextValue {
  notifications: Notification[];
  unreadCount: number;
  isLoading: boolean;
  refreshNotifications: () => Promise<void>;
  markNotificationAsRead: (id: string) => void;
  archiveNotification: (id: string) => void;
}

const NotificationContext = createContext<NotificationContextValue | undefined>(undefined);

interface NotificationProviderProps {
  children: React.ReactNode;
  refreshInterval?: number; // in milliseconds
}

export const NotificationProvider: React.FC<NotificationProviderProps> = ({ children, refreshInterval = 30000 }) => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);

  const refreshNotifications = useCallback(async () => {
    try {
      setIsLoading(true);
      const response: NotificationResponse = await getNotifications({ limit: 50 });
      setNotifications(response.notifications);
      setUnreadCount(response.unreadCount);
    } catch (error) {
      console.error('Failed to refresh notifications:', error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const markNotificationAsRead = useCallback((id: string) => {
    setNotifications((prev) =>
      prev.map((notification) =>
        notification.id === id
          ? {
              ...notification,
              isRead: true,
            }
          : notification,
      ),
    );
    setUnreadCount((prev) => Math.max(0, prev - 1));
  }, []);

  const archiveNotification = useCallback((id: string) => {
    setNotifications((prev) =>
      prev.map((notification) =>
        notification.id === id
          ? {
              ...notification,
              isArchived: true,
              isRead: true,
            }
          : notification,
      ),
    );
    setUnreadCount((prev) => Math.max(0, prev - 1));
  }, []);

  // Auto-refresh notifications
  useEffect(() => {
    refreshNotifications();

    const interval = setInterval(refreshNotifications, refreshInterval);
    return () => clearInterval(interval);
  }, [refreshNotifications, refreshInterval]);

  // Listen for visibility changes to refresh when user comes back to tab
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (!document.hidden) {
        refreshNotifications();
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => document.removeEventListener('visibilitychange', handleVisibilityChange);
  }, [refreshNotifications]);

  const value: NotificationContextValue = {
    notifications,
    unreadCount,
    isLoading,
    refreshNotifications,
    markNotificationAsRead,
    archiveNotification,
  };

  return <NotificationContext.Provider value={value}>{children}</NotificationContext.Provider>;
};

export const useNotifications = (): NotificationContextValue => {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
};

// Hook for getting unread count specifically (useful for header badge)
export const useUnreadNotificationsCount = (): number => {
  const { unreadCount } = useNotifications();
  return unreadCount;
};

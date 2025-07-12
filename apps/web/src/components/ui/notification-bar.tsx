'use client';

import { useState, useEffect } from 'react';
import { X, AlertCircle, CheckCircle, Info, AlertTriangle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

export interface NotificationBarItem {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message?: string;
  action?: {
    label: string;
    onClick: () => void;
  };
  dismissible?: boolean;
  autoDismiss?: number; // milliseconds
  persistent?: boolean;
}

interface NotificationBarProps {
  notifications: NotificationBarItem[];
  onDismiss?: (id: string) => void;
  position?: 'top' | 'bottom';
  className?: string;
}

export function NotificationBar({ notifications, onDismiss, position = 'top', className }: NotificationBarProps) {
  const [visibleNotifications, setVisibleNotifications] = useState<NotificationBarItem[]>([]);

  useEffect(() => {
    setVisibleNotifications(notifications);

    // Handle auto-dismiss
    notifications.forEach((notification) => {
      if (notification.autoDismiss && !notification.persistent) {
        const timer = setTimeout(() => {
          handleDismiss(notification.id);
        }, notification.autoDismiss);

        return () => clearTimeout(timer);
      }
    });
  }, [notifications]);

  const handleDismiss = (id: string) => {
    setVisibleNotifications((prev) => prev.filter((n) => n.id !== id));
    onDismiss?.(id);
  };

  const getIcon = (type: NotificationBarItem['type']) => {
    switch (type) {
      case 'success':
        return <CheckCircle className="w-4 h-4" />;
      case 'error':
        return <AlertCircle className="w-4 h-4" />;
      case 'warning':
        return <AlertTriangle className="w-4 h-4" />;
      case 'info':
        return <Info className="w-4 h-4" />;
      default:
        return <Info className="w-4 h-4" />;
    }
  };

  const getTypeStyles = (type: NotificationBarItem['type']) => {
    switch (type) {
      case 'success':
        return 'bg-green-50 border-green-200 text-green-800 dark:bg-green-900/20 dark:border-green-800 dark:text-green-300';
      case 'error':
        return 'bg-red-50 border-red-200 text-red-800 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300';
      case 'warning':
        return 'bg-yellow-50 border-yellow-200 text-yellow-800 dark:bg-yellow-900/20 dark:border-yellow-800 dark:text-yellow-300';
      case 'info':
        return 'bg-blue-50 border-blue-200 text-blue-800 dark:bg-blue-900/20 dark:border-blue-800 dark:text-blue-300';
      default:
        return 'bg-gray-50 border-gray-200 text-gray-800 dark:bg-gray-900/20 dark:border-gray-800 dark:text-gray-300';
    }
  };

  if (visibleNotifications.length === 0) {
    return null;
  }

  return (
    <div className={cn('fixed left-0 right-0 z-50 space-y-2 p-4', position === 'top' ? 'top-0' : 'bottom-0', className)}>
      {visibleNotifications.map((notification) => (
        <div
          key={notification.id}
          className={cn(
            'flex items-center gap-3 px-4 py-3 border rounded-lg shadow-lg backdrop-blur-sm',
            'transition-all duration-300 ease-in-out',
            'animate-in slide-in-from-top-2 fade-in-0',
            getTypeStyles(notification.type),
          )}
        >
          {/* Icon */}
          <div className="flex-shrink-0">{getIcon(notification.type)}</div>

          {/* Content */}
          <div className="flex-1 min-w-0">
            <div className="font-medium text-sm">{notification.title}</div>
            {notification.message && <div className="text-xs opacity-90 mt-1">{notification.message}</div>}
          </div>

          {/* Action Button */}
          {notification.action && (
            <Button
              variant="ghost"
              size="sm"
              onClick={notification.action.onClick}
              className="h-auto py-1 px-2 text-xs font-medium hover:bg-black/10 dark:hover:bg-white/10"
            >
              {notification.action.label}
            </Button>
          )}

          {/* Dismiss Button */}
          {notification.dismissible !== false && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => handleDismiss(notification.id)}
              className="h-auto p-1 hover:bg-black/10 dark:hover:bg-white/10 flex-shrink-0"
            >
              <X className="w-3 h-3" />
            </Button>
          )}
        </div>
      ))}
    </div>
  );
}

// Provider component for managing global notifications
interface NotificationContextType {
  notifications: NotificationBarItem[];
  addNotification: (notification: Omit<NotificationBarItem, 'id'>) => void;
  removeNotification: (id: string) => void;
  clearAll: () => void;
}

import { createContext, useContext, ReactNode } from 'react';

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [notifications, setNotifications] = useState<NotificationBarItem[]>([]);

  const addNotification = (notification: Omit<NotificationBarItem, 'id'>) => {
    const id = Math.random().toString(36).substring(7);
    const newNotification: NotificationBarItem = {
      ...notification,
      id,
      dismissible: notification.dismissible ?? true,
      autoDismiss: notification.autoDismiss ?? (notification.type === 'success' ? 5000 : undefined),
    };

    setNotifications((prev) => [...prev, newNotification]);
  };

  const removeNotification = (id: string) => {
    setNotifications((prev) => prev.filter((n) => n.id !== id));
  };

  const clearAll = () => {
    setNotifications([]);
  };

  return (
    <NotificationContext.Provider
      value={{
        notifications,
        addNotification,
        removeNotification,
        clearAll,
      }}
    >
      {children}
      <NotificationBar notifications={notifications} onDismiss={removeNotification} />
    </NotificationContext.Provider>
  );
}

export function useNotifications() {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
}

// Helper hook for common notification types
export function useNotificationHelpers() {
  const { addNotification } = useNotifications();

  return {
    success: (title: string, message?: string, options?: Partial<NotificationBarItem>) => addNotification({ type: 'success', title, message, ...options }),

    error: (title: string, message?: string, options?: Partial<NotificationBarItem>) =>
      addNotification({ type: 'error', title, message, persistent: true, ...options }),

    warning: (title: string, message?: string, options?: Partial<NotificationBarItem>) => addNotification({ type: 'warning', title, message, ...options }),

    info: (title: string, message?: string, options?: Partial<NotificationBarItem>) => addNotification({ type: 'info', title, message, ...options }),
  };
}

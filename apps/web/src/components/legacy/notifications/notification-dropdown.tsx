'use client';

import * as React from 'react';
import { Archive, Bell, Check, MoreHorizontal } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Notification as AppNotification, NotificationFilters } from '@/components/legacy/types/notification';
import {
  acceptProjectInvite,
  archiveNotification,
  declineProjectInvite,
  getNotifications,
  markAllNotificationsAsRead,
  markNotificationAsRead,
} from '@/lib/notifications/notifications.actions';
import { useToast } from '@/lib/old/hooks/use-toast';

interface NotificationDropdownProps {
  className?: string;
}

interface NotificationItemProps {
  notification: AppNotification;
  onMarkAsRead: (id: string) => Promise<void>;
  onArchive: (id: string) => Promise<void>;
  onAccept?: (id: string) => Promise<void>;
  onDecline?: (id: string) => Promise<void>;
}

const formatTimeAgo = (date: Date): string => {
  const now = new Date();
  const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));

  if (diffInMinutes < 1) return 'Just now';
  if (diffInMinutes < 60) return `${diffInMinutes}m ago`;

  const diffInHours = Math.floor(diffInMinutes / 60);
  if (diffInHours < 24) return `${diffInHours}h ago`;

  const diffInDays = Math.floor(diffInHours / 24);
  if (diffInDays < 7) return `${diffInDays}d ago`;

  return date.toLocaleDateString();
};

const getNotificationIcon = (type: AppNotification['type']): string => {
  switch (type) {
    case 'comment':
      return '💬';
    case 'follow':
      return '👥';
    case 'invite':
      return '📨';
    case 'reminder':
      return '⏰';
    case 'task':
      return '📋';
    case 'mention':
      return '@';
    case 'system':
      return '⚙️';
    default:
      return '🔔';
  }
};

const NotificationItem: React.FC<NotificationItemProps> = ({ notification, onMarkAsRead, onArchive, onAccept, onDecline }) => {
  const [isLoading, setIsLoading] = React.useState(false);

  const handleAction = async (action: () => Promise<void>) => {
    setIsLoading(true);
    try {
      await action();
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div
      className={cn(
        'flex gap-3 p-4 hover:bg-slate-100 dark:hover:bg-slate-800 transition-all duration-200 border-l-2 border-transparent hover:border-blue-400 dark:hover:border-blue-500',
        !notification.isRead && 'bg-blue-50 dark:bg-blue-950 border-l-blue-500 dark:border-l-blue-400',
      )}
    >
      {/* Unread indicator */}
      {!notification.isRead && <div className="w-2 h-2 rounded-full bg-blue-500 mt-2 flex-shrink-0" />}

      {/* Avatar */}
      <div className="flex-shrink-0">
        {notification.user ? (
          <Avatar className="w-8 h-8">
            <AvatarImage src={notification.user.avatar} alt={notification.user.name} />
            <AvatarFallback>{notification.user.name.charAt(0).toUpperCase()}</AvatarFallback>
          </Avatar>
        ) : (
          <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center text-sm">{getNotificationIcon(notification.type)}</div>
        )}
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1">
            <p className="text-sm font-medium text-foreground">
              {notification.user && <span className="font-semibold">{notification.user.name} </span>}
              {notification.message}
              {notification.projectName && <span className="font-semibold"> {notification.projectName}</span>}
              {notification.taskId && <span className="font-mono text-xs bg-muted px-1 py-0.5 rounded ml-1">{notification.taskId}</span>}
            </p>
            <p className="text-xs text-muted-foreground mt-1">{formatTimeAgo(notification.timestamp)}</p>
          </div>

          {/* Action menu */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-6 w-6">
                <MoreHorizontal className="h-3 w-3" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {!notification.isRead && (
                <DropdownMenuItem onClick={() => handleAction(() => onMarkAsRead(notification.id))}>
                  <Check className="w-4 h-4 mr-2" />
                  Mark as read
                </DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={() => handleAction(() => onArchive(notification.id))}>
                <Archive className="w-4 h-4 mr-2" />
                Archive
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        {/* Action buttons */}
        {notification.actionButtons && (
          <div className="flex gap-2 mt-3">
            {notification.actionButtons.accept && onAccept && (
              <Button
                size="sm"
                disabled={isLoading}
                onClick={() => handleAction(() => onAccept(notification.id))}
                className="bg-green-500 hover:bg-green-600 text-white border-0 shadow-sm disabled:opacity-50"
              >
                Accept
              </Button>
            )}
            {notification.actionButtons.decline && onDecline && (
              <Button
                variant="outline"
                size="sm"
                disabled={isLoading}
                onClick={() => handleAction(() => onDecline(notification.id))}
                className="border-red-200 text-red-600 hover:bg-red-50 hover:border-red-300 dark:border-red-800 dark:text-red-400 dark:hover:bg-red-950/50 disabled:opacity-50"
              >
                Decline
              </Button>
            )}
            {notification.actionButtons.respond && (
              <Button
                variant="outline"
                size="sm"
                disabled={isLoading}
                className="border-blue-200 text-blue-600 hover:bg-blue-50 hover:border-blue-300 dark:border-blue-800 dark:text-blue-400 dark:hover:bg-blue-950/50 disabled:opacity-50"
              >
                Respond
              </Button>
            )}
            {notification.actionButtons.view && (
              <Button
                variant="outline"
                size="sm"
                disabled={isLoading}
                className="border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300 dark:border-slate-700 dark:text-slate-400 dark:hover:bg-slate-800/50 disabled:opacity-50"
              >
                View
              </Button>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export const NotificationDropdown: React.FC<NotificationDropdownProps> = ({ className }) => {
  const [isOpen, setIsOpen] = React.useState(false);
  const [notifications, setNotifications] = React.useState<AppNotification[]>([]);
  const [unreadCount, setUnreadCount] = React.useState(0);
  const [activeTab, setActiveTab] = React.useState<'all' | 'unread' | 'archived'>('all');
  const [isLoading, setIsLoading] = React.useState(false);
  const { toast } = useToast();

  const loadNotifications = React.useCallback(
    async (filters: NotificationFilters = {}) => {
      setIsLoading(true);
      try {
        const response = await getNotifications(filters);
        setNotifications(response.notifications);
        setUnreadCount(response.unreadCount);
      } catch {
        toast({
          title: 'Error',
          description: 'Failed to load notifications',
          variant: 'destructive',
        });
      } finally {
        setIsLoading(false);
      }
    },
    [toast],
  );

  React.useEffect(() => {
    if (isOpen) {
      loadNotifications({ type: activeTab === 'all' ? 'all' : activeTab });
    }
  }, [isOpen, activeTab, loadNotifications]);

  const handleMarkAsRead = async (notificationId: string) => {
    const result = await markNotificationAsRead(notificationId);
    if (result.success) {
      await loadNotifications({ type: activeTab === 'all' ? 'all' : activeTab });
    } else {
      toast({
        title: 'Error',
        description: result.error,
        variant: 'destructive',
      });
    }
  };

  const handleMarkAllAsRead = async () => {
    const result = await markAllNotificationsAsRead();
    if (result.success) {
      await loadNotifications({ type: activeTab === 'all' ? 'all' : activeTab });
      toast({
        title: 'Success',
        description: result.message,
      });
    } else {
      toast({
        title: 'Error',
        description: result.error,
        variant: 'destructive',
      });
    }
  };

  const handleArchive = async (notificationId: string) => {
    const result = await archiveNotification(notificationId);
    if (result.success) {
      await loadNotifications({ type: activeTab === 'all' ? 'all' : activeTab });
    } else {
      toast({
        title: 'Error',
        description: result.error,
        variant: 'destructive',
      });
    }
  };

  const handleAccept = async (notificationId: string) => {
    const result = await acceptProjectInvite(notificationId);
    if (result.success) {
      await loadNotifications({ type: activeTab === 'all' ? 'all' : activeTab });
      toast({
        title: 'Success',
        description: result.message,
      });
    } else {
      toast({
        title: 'Error',
        description: result.error,
        variant: 'destructive',
      });
    }
  };

  const handleDecline = async (notificationId: string) => {
    const result = await declineProjectInvite(notificationId);
    if (result.success) {
      await loadNotifications({ type: activeTab === 'all' ? 'all' : activeTab });
      toast({
        title: 'Success',
        description: result.message,
      });
    } else {
      toast({
        title: 'Error',
        description: result.error,
        variant: 'destructive',
      });
    }
  };

  const filteredNotifications = notifications.filter((notification) => {
    if (activeTab === 'unread') return !notification.isRead;
    if (activeTab === 'archived') return notification.isArchived;
    return !notification.isArchived; // 'all' shows non-archived
  });

  const getTabCounts = () => {
    const unread = notifications.filter((n) => !n.isRead && !n.isArchived).length;
    const archived = notifications.filter((n) => n.isArchived).length;
    return { unread, archived };
  };

  const { unread: unreadTabCount, archived: archivedCount } = React.useMemo(getTabCounts, [notifications]);

  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <PopoverTrigger asChild>
        <Button variant="ghost" size="icon" className={cn('relative', className)} aria-label="Notifications">
          <Bell className="h-5 w-5" />
          {unreadCount > 0 && (
            <Badge
              variant="destructive"
              className="absolute -top-1 -right-1 h-4 w-auto min-w-[16px] px-1 flex items-center justify-center text-[10px] leading-none font-medium"
            >
              {unreadCount > 99 ? '99+' : unreadCount}
            </Badge>
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-96 p-0 bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-700 shadow-2xl" align="end" sideOffset={8}>
        {/* Top border */}
        <div className="h-px bg-gradient-to-r from-transparent via-slate-400 dark:via-slate-500 to-transparent"></div>

        <div className="flex items-center justify-between p-4 border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800">
          <h3 className="font-semibold bg-gradient-to-r from-slate-900 to-slate-700 dark:from-slate-100 dark:to-slate-300 bg-clip-text text-transparent">
            Notifications
          </h3>
          {unreadCount > 0 && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleMarkAllAsRead}
              className="text-xs hover:bg-slate-200 dark:hover:bg-slate-700 transition-all duration-200"
            >
              Mark all as read
            </Button>
          )}
        </div>

        <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as typeof activeTab)}>
          <TabsList className="w-full rounded-none border-b bg-slate-50 dark:bg-slate-800 p-0 border-slate-200 dark:border-slate-700">
            <TabsTrigger
              value="all"
              className="flex-1 rounded-none border-b-2 border-transparent data-[state=active]:border-blue-500 data-[state=active]:bg-blue-100 dark:data-[state=active]:bg-blue-900 hover:bg-slate-200 dark:hover:bg-slate-700 transition-all duration-200"
            >
              All
            </TabsTrigger>
            <TabsTrigger
              value="unread"
              className="flex-1 rounded-none border-b-2 border-transparent data-[state=active]:border-purple-500 data-[state=active]:bg-purple-100 dark:data-[state=active]:bg-purple-900 hover:bg-slate-200 dark:hover:bg-slate-700 transition-all duration-200"
            >
              <span className="flex items-center gap-1">
                Unread
                {unreadTabCount > 0 && (
                  <Badge
                    variant="secondary"
                    className="h-4 w-auto min-w-[16px] px-1 text-[10px] leading-none bg-purple-200 text-purple-800 dark:bg-purple-800 dark:text-purple-200"
                  >
                    {unreadTabCount > 99 ? '99+' : unreadTabCount}
                  </Badge>
                )}
              </span>
            </TabsTrigger>
            <TabsTrigger
              value="archived"
              className="flex-1 rounded-none border-b-2 border-transparent data-[state=active]:border-green-500 data-[state=active]:bg-green-100 dark:data-[state=active]:bg-green-900 hover:bg-slate-200 dark:hover:bg-slate-700 transition-all duration-200"
            >
              <span className="flex items-center gap-1">
                Archived
                {archivedCount > 0 && (
                  <Badge
                    variant="secondary"
                    className="h-4 w-auto min-w-[16px] px-1 text-[10px] leading-none bg-green-200 text-green-800 dark:bg-green-800 dark:text-green-200"
                  >
                    {archivedCount > 99 ? '99+' : archivedCount}
                  </Badge>
                )}
              </span>
            </TabsTrigger>
          </TabsList>

          <TabsContent value={activeTab} className="mt-0">
            <ScrollArea className="h-96">
              {isLoading ? (
                <div className="flex items-center justify-center py-8">
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary"></div>
                </div>
              ) : filteredNotifications.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-8 text-muted-foreground">
                  <Bell className="h-8 w-8 mb-2" />
                  <p className="text-sm">No notifications</p>
                </div>
              ) : (
                <div className="divide-y">
                  {filteredNotifications.map((notification) => (
                    <NotificationItem
                      key={notification.id}
                      notification={notification}
                      onMarkAsRead={handleMarkAsRead}
                      onArchive={handleArchive}
                      onAccept={handleAccept}
                      onDecline={handleDecline}
                    />
                  ))}
                </div>
              )}
            </ScrollArea>
          </TabsContent>
        </Tabs>

        <div className="p-4 border-t border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800">
          <Button
            variant="outline"
            className="w-full bg-white dark:bg-slate-900 border-slate-300 dark:border-slate-600 hover:bg-blue-50 dark:hover:bg-blue-950 hover:border-blue-400 dark:hover:border-blue-500 transition-all duration-200"
            size="sm"
          >
            View all notifications
          </Button>
        </div>

        {/* Bottom border */}
        <div className="h-1 bg-gradient-to-r from-blue-400 via-purple-500 to-green-400 dark:from-blue-500 dark:via-purple-600 dark:to-green-500"></div>
      </PopoverContent>
    </Popover>
  );
};

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
import type { Notification, NotificationType } from '@/types/notification';
import { useNotifications, useNotificationActions } from './notification-provider';

interface NotificationDropdownProps {
  className?: string;
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

const getNotificationIcon = (type: NotificationType): string => {
  const icons = {
    comment: '💬',
    follow: '👥',
    invite: '📨',
    reminder: '⏰',
    task: '📋',
    mention: '@',
    system: '⚙️',
    course: '📚',
    achievement: '🏆',
    social: '👥',
    promotion: '🎁',
  };
  return icons[type] || '🔔';
};

interface NotificationItemProps {
  notification: Notification;
  onMarkAsRead: (id: string) => Promise<void>;
  onArchive: (id: string) => Promise<void>;
}

const NotificationItem: React.FC<NotificationItemProps> = ({ notification, onMarkAsRead, onArchive }) => {
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
        <div className="flex items-start justify-between">
          <div className="flex-1 min-w-0">
            <h4 className="text-sm font-medium text-foreground truncate">{notification.title}</h4>
            <p className="text-xs text-muted-foreground mt-1">
              {notification.user && `${notification.user.name} `}
              {notification.message}
              {notification.metadata?.projectName && ` ${notification.metadata.projectName}`}
            </p>
            <p className="text-xs text-muted-foreground mt-1">{formatTimeAgo(notification.timestamp)}</p>
          </div>

          {/* Actions */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="h-6 w-6 p-0" disabled={isLoading}>
                <MoreHorizontal className="h-3 w-3" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-40">
              {!notification.isRead && (
                <DropdownMenuItem onClick={() => handleAction(() => onMarkAsRead(notification.id))}>
                  <Check className="mr-2 h-3 w-3" />
                  Mark as read
                </DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={() => handleAction(() => onArchive(notification.id))}>
                <Archive className="mr-2 h-3 w-3" />
                Archive
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        {/* Action buttons */}
        {notification.actionButtons && (
          <div className="flex gap-1 mt-2">
            {notification.actionButtons.accept && (
              <Button size="sm" className="h-6 text-xs px-2">
                Accept
              </Button>
            )}
            {notification.actionButtons.decline && (
              <Button size="sm" variant="outline" className="h-6 text-xs px-2">
                Decline
              </Button>
            )}
            {notification.actionButtons.view && (
              <Button size="sm" variant="outline" className="h-6 text-xs px-2">
                View
              </Button>
            )}
            {notification.actionButtons.respond && (
              <Button size="sm" variant="outline" className="h-6 text-xs px-2">
                Respond
              </Button>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export function NotificationDropdown({ className }: NotificationDropdownProps) {
  const { filteredNotifications, state, setFilters } = useNotifications();
  const { markAsRead, archiveNotification, markAllAsRead, refreshNotifications } = useNotificationActions();
  const [activeTab, setActiveTab] = React.useState('following');

  // Filter notifications for each tab
  const followingNotifications = React.useMemo(() => filteredNotifications.filter((n) => !n.isArchived).slice(0, 10), [filteredNotifications]);

  const archivedNotifications = React.useMemo(() => filteredNotifications.filter((n) => n.isArchived).slice(0, 10), [filteredNotifications]);

  React.useEffect(() => {
    // Update filters based on active tab
    if (activeTab === 'following') {
      setFilters({ category: 'following' });
    } else {
      setFilters({ category: 'archive' });
    }
  }, [activeTab, setFilters]);

  const handleTabChange = (value: string) => {
    setActiveTab(value);
  };

  return (
    <div className={className}>
      <Popover>
        <PopoverTrigger asChild>
          <Button variant="ghost" size="sm" className="relative">
            <Bell className="h-4 w-4" />
            {state.unreadCount > 0 && (
              <Badge variant="destructive" className="absolute -top-1 -right-1 h-5 w-5 rounded-full p-0 text-xs flex items-center justify-center">
                {state.unreadCount > 9 ? '9+' : state.unreadCount}
              </Badge>
            )}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-96 p-0" align="end">
          <div className="flex items-center justify-between p-4 border-b">
            <h3 className="font-semibold">Notifications</h3>
            <div className="flex items-center gap-2">
              <Button variant="ghost" size="sm" onClick={refreshNotifications} disabled={state.isLoading}>
                <Bell className="h-3 w-3" />
              </Button>
              {state.unreadCount > 0 && (
                <Button variant="ghost" size="sm" onClick={markAllAsRead}>
                  <Check className="h-3 w-3 mr-1" />
                  Mark all read
                </Button>
              )}
            </div>
          </div>

          <Tabs value={activeTab} onValueChange={handleTabChange}>
            <TabsList className="grid w-full grid-cols-2 rounded-none border-b">
              <TabsTrigger value="following" className="rounded-none">
                Following
                {followingNotifications.length > 0 && (
                  <Badge variant="secondary" className="ml-1 text-xs">
                    {followingNotifications.length}
                  </Badge>
                )}
              </TabsTrigger>
              <TabsTrigger value="archive" className="rounded-none">
                Archive
                {archivedNotifications.length > 0 && (
                  <Badge variant="secondary" className="ml-1 text-xs">
                    {archivedNotifications.length}
                  </Badge>
                )}
              </TabsTrigger>
            </TabsList>

            <TabsContent value="following" className="mt-0">
              <ScrollArea className="h-96">
                {followingNotifications.length > 0 ? (
                  followingNotifications.map((notification) => <NotificationItem key={notification.id} notification={notification} onMarkAsRead={markAsRead} onArchive={archiveNotification} />)
                ) : (
                  <div className="flex flex-col items-center justify-center py-8 text-center">
                    <Bell className="h-8 w-8 text-muted-foreground mb-2" />
                    <p className="text-sm text-muted-foreground">No notifications yet</p>
                  </div>
                )}
              </ScrollArea>
            </TabsContent>

            <TabsContent value="archive" className="mt-0">
              <ScrollArea className="h-96">
                {archivedNotifications.length > 0 ? (
                  archivedNotifications.map((notification) => <NotificationItem key={notification.id} notification={notification} onMarkAsRead={markAsRead} onArchive={archiveNotification} />)
                ) : (
                  <div className="flex flex-col items-center justify-center py-8 text-center">
                    <Archive className="h-8 w-8 text-muted-foreground mb-2" />
                    <p className="text-sm text-muted-foreground">No archived notifications</p>
                  </div>
                )}
              </ScrollArea>
            </TabsContent>
          </Tabs>

          {followingNotifications.length > 0 && (
            <div className="p-2 border-t">
              <Button variant="ghost" size="sm" className="w-full justify-center text-xs">
                View all notifications
              </Button>
            </div>
          )}
        </PopoverContent>
      </Popover>
    </div>
  );
}

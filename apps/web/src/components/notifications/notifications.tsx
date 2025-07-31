'use client';

import { useState, useMemo } from 'react';
import { ArrowRight, Bell, Check, Star, Archive, Trash2, Filter } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import type { Notification, NotificationType, NotificationPriority } from '@/types/notification';
import { useNotifications, useNotificationActions } from './notification-provider';

interface NotificationsProps {
  className?: string;
  showFilters?: boolean;
  compact?: boolean;
}

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

const getPriorityColor = (priority?: NotificationPriority): string => {
  switch (priority) {
    case 'high':
      return 'bg-red-500';
    case 'medium':
      return 'bg-yellow-500';
    case 'low':
      return 'bg-green-500';
    default:
      return 'bg-gray-500';
  }
};

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

interface NotificationItemProps {
  notification: Notification;
  compact?: boolean;
  onAction?: (action: string) => void;
}

function NotificationItem({ notification, compact = false, onAction }: NotificationItemProps) {
  const { markAsRead, archiveNotification, toggleStar, removeNotification } = useNotificationActions();

  const handleMarkAsRead = () => {
    if (!notification.isRead) {
      markAsRead(notification.id);
    }
  };

  const handleArchive = () => {
    archiveNotification(notification.id);
  };

  const handleToggleStar = () => {
    toggleStar(notification.id);
  };

  const handleRemove = () => {
    removeNotification(notification.id);
  };

  const handleActionClick = (action: string) => {
    onAction?.(action);
  };

  return (
    <div className={`group relative flex gap-3 p-4 border-b last:border-b-0 hover:bg-muted/50 transition-colors ${!notification.isRead ? 'bg-blue-50/50 dark:bg-blue-950/20' : ''}`}>
      {/* Priority indicator */}
      {notification.priority && <div className={`absolute left-0 top-0 bottom-0 w-1 ${getPriorityColor(notification.priority)}`} />}

      {/* Unread indicator */}
      {!notification.isRead && <div className="w-2 h-2 rounded-full bg-blue-500 mt-2 flex-shrink-0" />}

      {/* Avatar */}
      <div className="flex-shrink-0">
        {notification.user ? (
          <Avatar className={compact ? 'w-8 h-8' : 'w-10 h-10'}>
            <AvatarImage src={notification.user.avatar} alt={notification.user.name} />
            <AvatarFallback>{notification.user.name.charAt(0).toUpperCase()}</AvatarFallback>
          </Avatar>
        ) : (
          <div className={`${compact ? 'w-8 h-8' : 'w-10 h-10'} rounded-full bg-muted flex items-center justify-center text-sm`}>{getNotificationIcon(notification.type)}</div>
        )}
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0 space-y-1">
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            <h4 className={`font-medium text-foreground ${compact ? 'text-sm' : 'text-base'}`}>{notification.title}</h4>
            <p className={`text-muted-foreground ${compact ? 'text-xs' : 'text-sm'} mt-1`}>
              {notification.user && `${notification.user.name} `}
              {notification.message}
              {notification.metadata?.projectName && ` ${notification.metadata.projectName}`}
            </p>
          </div>

          {/* Action menu */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="opacity-0 group-hover:opacity-100 transition-opacity">
                <Trash2 className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {!notification.isRead && (
                <DropdownMenuItem onClick={handleMarkAsRead}>
                  <Check className="h-4 w-4 mr-2" />
                  Mark as read
                </DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={handleToggleStar}>
                <Star className={`h-4 w-4 mr-2 ${notification.isStarred ? 'fill-current' : ''}`} />
                {notification.isStarred ? 'Unstar' : 'Star'}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={handleArchive}>
                <Archive className="h-4 w-4 mr-2" />
                Archive
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={handleRemove} className="text-destructive">
                <Trash2 className="h-4 w-4 mr-2" />
                Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        {/* Metadata */}
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <span>{formatTimeAgo(notification.timestamp)}</span>
          {notification.metadata?.taskId && (
            <>
              <Separator orientation="vertical" className="h-3" />
              <span>{notification.metadata.taskId}</span>
            </>
          )}
          {notification.priority && (
            <>
              <Separator orientation="vertical" className="h-3" />
              <Badge variant="outline" className="text-xs">
                {notification.priority}
              </Badge>
            </>
          )}
        </div>

        {/* Action buttons */}
        {notification.actionButtons && (
          <div className="flex gap-2 mt-2">
            {notification.actionButtons.accept && (
              <Button size="sm" onClick={() => handleActionClick('accept')}>
                Accept
              </Button>
            )}
            {notification.actionButtons.decline && (
              <Button size="sm" variant="outline" onClick={() => handleActionClick('decline')}>
                Decline
              </Button>
            )}
            {notification.actionButtons.view && (
              <Button size="sm" variant="outline" onClick={() => handleActionClick('view')}>
                View
                <ArrowRight className="ml-1 h-3 w-3" />
              </Button>
            )}
            {notification.actionButtons.respond && (
              <Button size="sm" variant="outline" onClick={() => handleActionClick('respond')}>
                Respond
              </Button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export function Notifications({ className, showFilters = true, compact = false }: NotificationsProps) {
  const { filteredNotifications, state, setFilters } = useNotifications();
  const { markAllAsRead, refreshNotifications } = useNotificationActions();
  const [activeTab, setActiveTab] = useState('unread');

  const tabs = useMemo(() => {
    const unread = filteredNotifications.filter((n) => !n.isRead && !n.isArchived);
    const read = filteredNotifications.filter((n) => n.isRead && !n.isArchived);
    const archived = filteredNotifications.filter((n) => n.isArchived);
    const starred = filteredNotifications.filter((n) => n.isStarred && !n.isArchived);

    return [
      { id: 'unread', label: 'Unread', count: unread.length, notifications: unread },
      { id: 'read', label: 'Read', count: read.length, notifications: read },
      { id: 'starred', label: 'Starred', count: starred.length, notifications: starred },
      { id: 'archived', label: 'Archived', count: archived.length, notifications: archived },
    ];
  }, [filteredNotifications]);

  const handleTabChange = (value: string) => {
    setActiveTab(value);
    const tabType = value === 'unread' ? 'unread' : value === 'read' ? 'read' : value === 'archived' ? 'archived' : 'all';
    setFilters({ type: tabType });
  };

  const handleNotificationAction = async (notificationId: string, action: string) => {
    // Handle notification-specific actions
    console.log(`Action ${action} for notification ${notificationId}`);
  };

  return (
    <Card className={className}>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg font-semibold">
            Notifications
            {state.unreadCount > 0 && (
              <Badge variant="secondary" className="ml-2">
                {state.unreadCount}
              </Badge>
            )}
          </CardTitle>
          <div className="flex items-center gap-2">
            {showFilters && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm">
                    <Filter className="h-4 w-4 mr-2" />
                    Filter
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
                  <div className="p-2">
                    <label className="text-xs font-medium text-muted-foreground">Type</label>
                    <Select value={state.filters.notificationType || 'all'} onValueChange={(value) => setFilters({ notificationType: value as NotificationType | 'all' })}>
                      <SelectTrigger className="h-8 mt-1">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All types</SelectItem>
                        <SelectItem value="comment">Comments</SelectItem>
                        <SelectItem value="follow">Follows</SelectItem>
                        <SelectItem value="invite">Invites</SelectItem>
                        <SelectItem value="task">Tasks</SelectItem>
                        <SelectItem value="reminder">Reminders</SelectItem>
                        <SelectItem value="system">System</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <DropdownMenuSeparator />
                  <div className="p-2">
                    <label className="text-xs font-medium text-muted-foreground">Priority</label>
                    <Select value={state.filters.priority || 'all'} onValueChange={(value) => setFilters({ priority: value as NotificationPriority | 'all' })}>
                      <SelectTrigger className="h-8 mt-1">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All priorities</SelectItem>
                        <SelectItem value="high">High</SelectItem>
                        <SelectItem value="medium">Medium</SelectItem>
                        <SelectItem value="low">Low</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
            <Button variant="outline" size="sm" onClick={refreshNotifications} disabled={state.isLoading}>
              <Bell className="h-4 w-4" />
            </Button>
            {state.unreadCount > 0 && (
              <Button variant="outline" size="sm" onClick={markAllAsRead}>
                <Check className="h-4 w-4 mr-2" />
                Mark all read
              </Button>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent className="p-0">
        <Tabs value={activeTab} onValueChange={handleTabChange}>
          <TabsList className="grid w-full grid-cols-4 mx-4 mb-4">
            {tabs.map((tab) => (
              <TabsTrigger key={tab.id} value={tab.id} className="text-xs">
                {tab.label}
                {tab.count > 0 && (
                  <Badge variant="secondary" className="ml-1 text-xs">
                    {tab.count}
                  </Badge>
                )}
              </TabsTrigger>
            ))}
          </TabsList>

          {tabs.map((tab) => (
            <TabsContent key={tab.id} value={tab.id} className="mt-0">
              <ScrollArea className="h-[600px]">
                {tab.notifications.length > 0 ? (
                  <div>
                    {tab.notifications.map((notification) => (
                      <NotificationItem key={notification.id} notification={notification} compact={compact} onAction={(action) => handleNotificationAction(notification.id, action)} />
                    ))}
                  </div>
                ) : (
                  <div className="flex flex-col items-center justify-center py-8 text-center">
                    <Bell className="h-12 w-12 text-muted-foreground mb-4" />
                    <h3 className="font-medium text-lg mb-2">No {tab.label.toLowerCase()} notifications</h3>
                    <p className="text-muted-foreground text-sm">
                      {tab.id === 'unread' && "You're all caught up! ✨"}
                      {tab.id === 'read' && 'No read notifications yet.'}
                      {tab.id === 'starred' && 'Star important notifications to see them here.'}
                      {tab.id === 'archived' && 'No archived notifications.'}
                    </p>
                  </div>
                )}
              </ScrollArea>
            </TabsContent>
          ))}
        </Tabs>
      </CardContent>
    </Card>
  );
}

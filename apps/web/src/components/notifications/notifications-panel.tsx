'use client';

import { useState } from 'react';
import { Bell, Check, ExternalLink, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import Image from 'next/image';
import type { Notification } from '@/types/notification';
import { useNotifications, useNotificationActions } from './notification-provider';

interface NotificationsPanelProps {
  isOpen: boolean;
  onClose: () => void;
  className?: string;
}

export function NotificationsPanel({ isOpen, onClose, className }: NotificationsPanelProps) {
  const { filteredNotifications, state } = useNotifications();
  const { markAllAsRead } = useNotificationActions();
  const [activeTab, setActiveTab] = useState('following');

  if (!isOpen) return null;

  const followingNotifications = filteredNotifications.filter((n) => !n.isArchived);
  const archivedNotifications = filteredNotifications.filter((n) => n.isArchived);

  const getInitials = (name: string): string => {
    return name
      .split(' ')
      .map((word) => word.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  const formatTimeAgo = (date: Date): string => {
    const now = new Date();
    const diffInHours = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60));

    if (diffInHours < 1) return 'Just now';
    if (diffInHours === 1) return '1 hour ago';
    if (diffInHours < 24) return `${diffInHours} hours ago`;

    const diffInDays = Math.floor(diffInHours / 24);
    if (diffInDays === 1) return '1 day ago';

    return `${diffInDays} days ago`;
  };

  const NotificationItem = ({ notification }: { notification: Notification }) => (
    <div className={`flex gap-3 p-4 border-b last:border-b-0 ${!notification.isRead ? 'bg-blue-50/50 dark:bg-blue-950/20' : ''}`}>
      <Avatar className="w-8 h-8 flex-shrink-0">
        {notification.user?.avatar ? (
          <Image src={notification.user.avatar} alt={notification.user.name} width={32} height={32} className="w-full h-full object-cover" />
        ) : (
          <AvatarFallback className="text-xs">{notification.user ? getInitials(notification.user.name) : '?'}</AvatarFallback>
        )}
      </Avatar>

      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between mb-1">
          <div className="flex-1 min-w-0">
            <p className="text-sm">
              <span className="font-medium">{notification.user?.name || 'System'}</span>
              <span className="text-muted-foreground ml-1">{notification.message}</span>
              {notification.metadata?.projectName && <span className="font-medium ml-1">{notification.metadata.projectName}</span>}
            </p>
            <p className="text-xs text-muted-foreground mt-1">{formatTimeAgo(notification.timestamp)}</p>
          </div>

          {!notification.isRead && <div className="w-2 h-2 bg-blue-500 rounded-full flex-shrink-0 mt-2" />}
        </div>

        {notification.actionButtons && (
          <div className="flex gap-2 mt-2">
            {notification.actionButtons.accept && (
              <Button size="sm" className="h-7 px-3 text-xs">
                Accept
              </Button>
            )}
            {notification.actionButtons.decline && (
              <Button size="sm" variant="outline" className="h-7 px-3 text-xs">
                Decline
              </Button>
            )}
            {notification.actionButtons.view && (
              <Button size="sm" variant="outline" className="h-7 px-3 text-xs">
                <ExternalLink className="w-3 h-3 mr-1" />
                View
              </Button>
            )}
          </div>
        )}

        {notification.metadata?.projectName && notification.type === 'comment' && <div className="mt-2 p-2 bg-muted rounded text-xs text-muted-foreground">Project: {notification.metadata.projectName}</div>}
      </div>
    </div>
  );

  return (
    <div className={`fixed inset-0 z-50 ${className}`}>
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/20" onClick={onClose} />

      {/* Panel */}
      <div className="absolute right-0 top-0 h-full w-96 bg-background border-l shadow-lg">
        <Card className="h-full rounded-none border-0">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
            <CardTitle className="text-lg font-semibold flex items-center gap-2">
              <Bell className="w-5 h-5" />
              Notifications
              {state.unreadCount > 0 && <Badge variant="secondary">{state.unreadCount}</Badge>}
            </CardTitle>
            <div className="flex items-center gap-2">
              {state.unreadCount > 0 && (
                <Button variant="ghost" size="sm" onClick={markAllAsRead}>
                  <Check className="w-4 h-4 mr-1" />
                  Mark all read
                </Button>
              )}
              <Button variant="ghost" size="sm" onClick={onClose}>
                <X className="w-4 h-4" />
              </Button>
            </div>
          </CardHeader>

          <CardContent className="p-0 flex-1">
            <Tabs value={activeTab} onValueChange={setActiveTab}>
              <TabsList className="grid w-full grid-cols-2 mx-4 mb-4">
                <TabsTrigger value="following" className="text-sm">
                  Following
                  {followingNotifications.length > 0 && (
                    <Badge variant="secondary" className="ml-1 text-xs">
                      {followingNotifications.length}
                    </Badge>
                  )}
                </TabsTrigger>
                <TabsTrigger value="archive" className="text-sm">
                  Archive
                  {archivedNotifications.length > 0 && (
                    <Badge variant="secondary" className="ml-1 text-xs">
                      {archivedNotifications.length}
                    </Badge>
                  )}
                </TabsTrigger>
              </TabsList>

              <TabsContent value="following" className="mt-0">
                <ScrollArea className="h-[calc(100vh-160px)]">
                  {followingNotifications.length > 0 ? (
                    followingNotifications.map((notification) => <NotificationItem key={notification.id} notification={notification} />)
                  ) : (
                    <div className="flex flex-col items-center justify-center py-12 text-center">
                      <Bell className="w-12 h-12 text-muted-foreground mb-4" />
                      <h3 className="font-medium text-lg mb-2">All caught up!</h3>
                      <p className="text-muted-foreground text-sm">No new notifications right now.</p>
                    </div>
                  )}
                </ScrollArea>
              </TabsContent>

              <TabsContent value="archive" className="mt-0">
                <ScrollArea className="h-[calc(100vh-160px)]">
                  {archivedNotifications.length > 0 ? (
                    archivedNotifications.map((notification) => <NotificationItem key={notification.id} notification={notification} />)
                  ) : (
                    <div className="flex flex-col items-center justify-center py-12 text-center">
                      <Bell className="w-12 h-12 text-muted-foreground mb-4" />
                      <h3 className="font-medium text-lg mb-2">No archived notifications</h3>
                      <p className="text-muted-foreground text-sm">Archived notifications will appear here.</p>
                    </div>
                  )}
                </ScrollArea>
              </TabsContent>
            </Tabs>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

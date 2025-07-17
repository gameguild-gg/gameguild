'use client';

import { useState } from 'react';
import { Bell, Check, ExternalLink, MoreHorizontal, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';

interface NotificationUser {
  id: string;
  name: string;
  avatar?: string;
  initials: string;
}

interface Notification {
  id: string;
  type: 'request' | 'activity' | 'comment' | 'general';
  user: NotificationUser;
  action: string;
  target?: string;
  description?: string;
  timestamp: string;
  isRead: boolean;
  hasActions?: boolean;
  link?: string;
  previewImage?: string;
}

interface NotificationsPanelProps {
  isOpen: boolean;
  onClose: () => void;
  notifications?: Notification[];
}

export function NotificationsPanel({ isOpen, onClose, notifications = [] }: NotificationsPanelProps) {
  const [activeTab, setActiveTab] = useState('following');

  const defaultNotifications: Notification[] = [
    {
      id: '1',
      type: 'request',
      user: {
        id: 'dylan',
        name: 'Dylan',
        initials: 'D',
      },
      action: 'has requested to edit the file',
      target: 'Astro Illustration',
      timestamp: '20 hours ago',
      isRead: false,
      hasActions: true,
    },
    {
      id: '2',
      type: 'activity',
      user: {
        id: 'marina',
        name: 'Marina Niki',
        initials: 'M',
      },
      action: 'edited',
      target: 'Earthfund - Master',
      description:
        "Everything you need to make a difference. Join the EarthFund community and help us decentralize the way we tackle humanity's biggest problems.",
      timestamp: '21 hours ago',
      isRead: false,
      link: 'MVP Launch earthfund.io/technology/donate...',
      previewImage: 'earthfund-preview',
    },
    {
      id: '3',
      type: 'comment',
      user: {
        id: 'tam',
        name: 'Tam',
        initials: 'T',
      },
      action: 'commented',
      target: 'Astro Clash: 3D Character Pack',
      timestamp: '1 day ago',
      isRead: true,
    },
    {
      id: '4',
      type: 'request',
      user: {
        id: 'dylan2',
        name: 'Dylan',
        initials: 'D',
      },
      action: 'has requested to edit the file',
      target: 'Earthfund - Signup/Causes',
      timestamp: '2 days ago',
      isRead: true,
    },
  ];

  const displayNotifications = notifications.length > 0 ? notifications : defaultNotifications;
  const unreadCount = displayNotifications.filter((n) => !n.isRead).length;

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'request':
        return <div className="w-2 h-2 bg-orange-500 rounded-full"></div>;
      case 'activity':
        return <ExternalLink className="w-3 h-3 text-blue-500" />;
      case 'comment':
        return <div className="w-2 h-2 bg-green-500 rounded-full"></div>;
      default:
        return <div className="w-2 h-2 bg-gray-500 rounded-full"></div>;
    }
  };

  const NotificationItem = ({ notification }: { notification: Notification }) => (
    <div className={`p-4 border-b border-border last:border-b-0 hover:bg-muted/20 transition-colors ${!notification.isRead ? 'bg-muted/10' : ''}`}>
      <div className="flex items-start gap-3">
        {/* Status Indicator */}
        <div className="flex-shrink-0 mt-2">{getNotificationIcon(notification.type)}</div>

        {/* Avatar */}
        <Avatar className="w-8 h-8 flex-shrink-0">
          <AvatarImage src={notification.user.avatar} alt={notification.user.name} />
          <AvatarFallback
            className={`text-white text-xs font-bold ${
              notification.user.id === 'dylan' || notification.user.id === 'dylan2'
                ? 'bg-red-500'
                : notification.user.id === 'marina'
                  ? 'bg-blue-500'
                  : 'bg-purple-500'
            }`}
          >
            {notification.user.initials}
          </AvatarFallback>
        </Avatar>

        {/* Content */}
        <div className="flex-1 min-w-0">
          <div className="text-sm">
            <span className="font-medium text-foreground">{notification.user.name}</span>
            <span className="text-muted-foreground"> {notification.action} </span>
            {notification.target && <span className="font-medium text-foreground">{notification.target}</span>}
          </div>

          <div className="text-xs text-muted-foreground mt-1">{notification.timestamp} · AstroClash UI Kit</div>

          {/* Description for activity notifications */}
          {notification.description && (
            <div className="mt-3 p-3 bg-muted/30 rounded-lg border border-border">
              <div className="flex items-start gap-2 mb-2">
                <div className="w-4 h-4 bg-green-600 rounded flex-shrink-0 mt-0.5"></div>
                <div>
                  <div className="font-medium text-sm text-foreground">{notification.target}</div>
                  <div className="text-xs text-muted-foreground">www.figma.com</div>
                </div>
              </div>

              <p className="text-xs text-muted-foreground leading-relaxed mb-3">{notification.description}</p>

              {notification.link && (
                <div className="text-xs">
                  <span className="text-muted-foreground">Link preview</span>
                  <div className="flex items-center gap-1 mt-1">
                    <ExternalLink className="w-3 h-3 text-muted-foreground" />
                    <span className="text-muted-foreground truncate">{notification.link}</span>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Action buttons for requests */}
          {notification.hasActions && (
            <div className="flex items-center gap-2 mt-3">
              <Button size="sm" variant="outline" className="h-7 px-3 text-xs">
                Deny
              </Button>
              <Button size="sm" className="h-7 px-3 text-xs bg-blue-600 hover:bg-blue-700">
                Approve
              </Button>
            </div>
          )}
        </div>

        {/* Project Icon */}
        <div className="flex-shrink-0">
          <div className={`w-8 h-8 rounded-lg flex items-center justify-center ${notification.user.id === 'marina' ? 'bg-blue-100' : 'bg-purple-100'}`}>
            {notification.user.id === 'marina' ? <div className="w-4 h-4 bg-blue-600 rounded"></div> : <div className="w-4 h-4 bg-purple-600 rounded"></div>}
          </div>
        </div>
      </div>
    </div>
  );

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 bg-black/20 backdrop-blur-sm">
      <div className="fixed right-0 top-0 h-full w-full max-w-md bg-background border-l border-border shadow-xl">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-border bg-background/95 backdrop-blur">
          <div className="flex items-center gap-2">
            <Bell className="w-5 h-5 text-foreground" />
            <h2 className="text-lg font-semibold text-foreground">Notifications</h2>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" className="text-xs text-blue-600 hover:text-blue-700">
              <Check className="w-3 h-3 mr-1" />
              Mark all as read
            </Button>
            <Button variant="ghost" size="icon" onClick={onClose} className="h-8 w-8">
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {/* Tabs */}
        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
          <div className="border-b border-border bg-background px-4">
            <TabsList className="grid w-full grid-cols-3 bg-transparent h-auto p-0">
              <TabsTrigger
                value="inbox"
                className="relative border-b-2 border-transparent data-[state=active]:border-foreground rounded-none bg-transparent px-4 py-3"
              >
                <div className="flex items-center gap-2">
                  <span className="text-sm">Inbox</span>
                  {unreadCount > 0 && (
                    <Badge variant="secondary" className="bg-orange-500 text-white text-xs h-5 min-w-5 px-1">
                      {unreadCount}
                    </Badge>
                  )}
                </div>
              </TabsTrigger>
              <TabsTrigger
                value="following"
                className="relative border-b-2 border-transparent data-[state=active]:border-foreground rounded-none bg-transparent px-4 py-3"
              >
                <div className="flex items-center gap-2">
                  <span className="text-sm">Following</span>
                  <Badge variant="secondary" className="bg-blue-600 text-white text-xs h-5 min-w-5 px-1">
                    24
                  </Badge>
                </div>
              </TabsTrigger>
              <TabsTrigger
                value="archived"
                className="relative border-b-2 border-transparent data-[state=active]:border-foreground rounded-none bg-transparent px-4 py-3"
              >
                <span className="text-sm">Archived</span>
              </TabsTrigger>
            </TabsList>
          </div>

          {/* Content */}
          <div className="flex-1 overflow-y-auto max-h-[calc(100vh-120px)]">
            <TabsContent value="inbox" className="m-0">
              <div className="divide-y divide-border">
                {displayNotifications.map((notification) => (
                  <NotificationItem key={notification.id} notification={notification} />
                ))}
              </div>
            </TabsContent>

            <TabsContent value="following" className="m-0">
              <div className="divide-y divide-border">
                {displayNotifications.map((notification) => (
                  <NotificationItem key={notification.id} notification={notification} />
                ))}
              </div>
            </TabsContent>

            <TabsContent value="archived" className="m-0">
              <div className="p-8 text-center text-muted-foreground">
                <Bell className="w-12 h-12 mx-auto mb-4 opacity-50" />
                <p>No archived notifications</p>
              </div>
            </TabsContent>
          </div>
        </Tabs>

        {/* View more button */}
        <div className="p-4 border-t border-border bg-background">
          <Button variant="ghost" className="w-full justify-center text-sm text-muted-foreground hover:text-foreground">
            <MoreHorizontal className="w-4 h-4 mr-2" />
            View 32 more
          </Button>
        </div>
      </div>
    </div>
  );
}

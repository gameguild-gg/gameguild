'use client';

import { useState } from 'react';
import { ArrowRight, Bell, Check } from 'lucide-react';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@game-guild/ui/components/tabs';
import { Avatar, AvatarFallback, AvatarImage } from '@game-guild/ui/components/avatar';
import { Badge } from '@game-guild/ui/components/badge';

interface Notification {
  id: string;
  type: 'reminder' | 'status_change' | 'trade_alert' | 'governance' | 'transaction';
  title: string;
  message: string;
  sender?: {
    name: string;
    avatar?: string;
  };
  timestamp: string;
  isRead: boolean;
  isArchived: boolean;
  actions?: {
    primary?: {
      label: string;
      variant?: 'default' | 'secondary';
    };
    secondary?: {
      label: string;
      variant?: 'default' | 'secondary';
    };
  };
  metadata?: {
    projectName?: string;
    statusFrom?: string;
    statusTo?: string;
    amount?: string;
    icon?: string;
  };
}

interface NotificationsProps {
  notifications?: Notification[];
  onNotificationAction?: (notificationId: string, action: string) => void;
  onMarkAsRead?: (notificationId: string) => void;
  onArchive?: (notificationId: string) => void;
}

export function Notifications({ notifications = [], onNotificationAction, onMarkAsRead, onArchive }: NotificationsProps) {
  const [activeTab, setActiveTab] = useState('unread');

  const defaultNotifications: Notification[] = [
    {
      id: '1',
      type: 'reminder',
      title: 'Reminder',
      message: 'has sent you a reminder',
      sender: {
        name: 'Boyd Larkin',
        avatar: undefined,
      },
      timestamp: '2 hours ago',
      isRead: false,
      isArchived: false,
      actions: {
        secondary: { label: 'Ignore', variant: 'secondary' },
        primary: { label: 'Respond', variant: 'default' },
      },
    },
    {
      id: '2',
      type: 'status_change',
      title: 'Status Update',
      message: 'has changed the status of',
      sender: {
        name: 'Josh Krajcik',
        avatar: undefined,
      },
      timestamp: '2 hours ago',
      isRead: false,
      isArchived: false,
      metadata: {
        projectName: 'Design Project',
        statusFrom: 'In Progress',
        statusTo: 'Completed',
      },
    },
    {
      id: '3',
      type: 'trade_alert',
      title: 'Trade Alert',
      message: 'Your Bitcoin (BTC) trade is ready for review.',
      timestamp: '8:30 PM',
      isRead: false,
      isArchived: false,
      actions: {
        primary: { label: 'Review Trade', variant: 'default' },
        secondary: { label: 'Learn More', variant: 'secondary' },
      },
      metadata: {
        icon: '₿',
      },
    },
    {
      id: '4',
      type: 'governance',
      title: 'Governance Proposal',
      message: "Voting on Proposal #283 from DeFi DAO has begun. A pivotal moment shaping decentralized finance's future.",
      timestamp: '8:30 PM',
      isRead: false,
      isArchived: false,
      metadata: {
        icon: '⚡',
      },
    },
    {
      id: '5',
      type: 'transaction',
      title: 'Sent USDT',
      message: 'You sent 50 USDT to alice.eth. alice.crypto requests 75 USDT. Help fund their new project.',
      timestamp: '8:30 PM',
      isRead: false,
      isArchived: false,
      metadata: {
        icon: '💸',
      },
    },
  ];

  const displayNotifications = notifications.length > 0 ? notifications : defaultNotifications;

  const unreadNotifications = displayNotifications.filter((n) => !n.isRead && !n.isArchived);
  const readNotifications = displayNotifications.filter((n) => n.isRead && !n.isArchived);
  const archivedNotifications = displayNotifications.filter((n) => n.isArchived);

  const getNotificationsByTab = (tab: string) => {
    switch (tab) {
      case 'unread':
        return unreadNotifications;
      case 'read':
        return readNotifications;
      case 'archived':
        return archivedNotifications;
      default:
        return unreadNotifications;
    }
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map((word) => word.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  const getAvatarColors = (name: string) => {
    const colorPairs = [
      { bg: 'bg-blue-500', text: 'text-white' },
      { bg: 'bg-emerald-500', text: 'text-white' },
      { bg: 'bg-purple-500', text: 'text-white' },
      { bg: 'bg-orange-500', text: 'text-white' },
      { bg: 'bg-pink-500', text: 'text-white' },
      { bg: 'bg-indigo-500', text: 'text-white' },
    ];
    const index = name.charCodeAt(0) % colorPairs.length;
    return colorPairs[index];
  };

  const getNotificationIcon = (notification: Notification) => {
    if (notification.metadata?.icon) {
      return <div className="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center text-lg">{notification.metadata.icon}</div>;
    }

    if (notification.sender) {
      const avatarColors = getAvatarColors(notification.sender.name);
      return (
        <Avatar className="w-10 h-10">
          <AvatarImage src={notification.sender.avatar} alt={notification.sender.name} />
          <AvatarFallback className={`${avatarColors.bg} ${avatarColors.text} text-sm font-medium`}>{getInitials(notification.sender.name)}</AvatarFallback>
        </Avatar>
      );
    }

    return (
      <div className="w-10 h-10 rounded-lg bg-muted flex items-center justify-center">
        <Bell className="w-5 h-5 text-muted-foreground" />
      </div>
    );
  };

  const formatNotificationContent = (notification: Notification) => {
    if (notification.type === 'status_change' && notification.metadata) {
      return (
        <div className="space-y-2">
          <div className="flex items-center gap-2 text-sm">
            <span className="text-foreground font-medium">{notification.sender?.name}</span>
            <span className="text-muted-foreground">{notification.message}</span>
            <span className="text-foreground font-medium">{notification.metadata.projectName}</span>
          </div>
          <div className="flex items-center gap-2 text-sm">
            <Badge variant="secondary" className="text-xs">
              <div className="w-2 h-2 bg-orange-500 rounded-full mr-1" />
              {notification.metadata.statusFrom}
            </Badge>
            <ArrowRight className="w-3 h-3 text-muted-foreground" />
            <Badge variant="secondary" className="text-xs">
              <Check className="w-3 h-3 text-green-500 mr-1" />
              {notification.metadata.statusTo}
            </Badge>
          </div>
        </div>
      );
    }

    if (notification.sender) {
      return (
        <div className="text-sm">
          <span className="text-foreground font-medium">{notification.sender.name}</span>
          <span className="text-muted-foreground ml-1">{notification.message}</span>
        </div>
      );
    }

    return <div className="text-sm text-muted-foreground">{notification.message}</div>;
  };

  const handleAction = (notificationId: string, action: string) => {
    onNotificationAction?.(notificationId, action);
  };

  const todayNotifications = getNotificationsByTab(activeTab).filter((n) => n.timestamp.includes('hours ago') || n.timestamp.includes('minutes ago'));

  const yesterdayNotifications = getNotificationsByTab(activeTab).filter((n) => !n.timestamp.includes('hours ago') && !n.timestamp.includes('minutes ago'));

  return (
    <Card className="w-full max-w-2xl bg-background border-border shadow-lg">
      <CardContent className="p-6">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold text-foreground">Notifications</h2>
        </div>

        {/* Tabs */}
        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
          <TabsList className="grid w-full grid-cols-3 bg-muted/50 p-1 rounded-lg h-auto mb-6">
            <TabsTrigger
              value="unread"
              className="text-sm font-medium px-4 py-2.5 rounded-md data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm text-muted-foreground"
            >
              Unread
            </TabsTrigger>
            <TabsTrigger
              value="read"
              className="text-sm font-medium px-4 py-2.5 rounded-md data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm text-muted-foreground"
            >
              Read
            </TabsTrigger>
            <TabsTrigger
              value="archived"
              className="text-sm font-medium px-4 py-2.5 rounded-md data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm text-muted-foreground"
            >
              Archived
            </TabsTrigger>
          </TabsList>

          <TabsContent value={activeTab} className="space-y-6">
            {/* Today Section */}
            {todayNotifications.length > 0 && (
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <h3 className="text-sm font-medium text-muted-foreground">Today</h3>
                    <Badge variant="secondary" className="text-xs">
                      {todayNotifications.length}
                    </Badge>
                  </div>
                  <Button variant="ghost" size="sm" className="text-xs text-muted-foreground hover:text-foreground">
                    See All
                  </Button>
                </div>

                <div className="space-y-3">
                  {todayNotifications.map((notification) => (
                    <Card key={notification.id} className="bg-muted/30 border-border/40 p-4">
                      <div className="flex items-start gap-3">
                        <div className="relative">
                          {getNotificationIcon(notification)}
                          {!notification.isRead && <div className="absolute -top-1 -right-1 w-3 h-3 bg-blue-500 rounded-full" />}
                        </div>

                        <div className="flex-1 space-y-2">
                          {formatNotificationContent(notification)}

                          <div className="flex items-center justify-between">
                            <span className="text-xs text-muted-foreground">{notification.timestamp}</span>

                            {notification.actions && (
                              <div className="flex items-center gap-2">
                                {notification.actions.secondary && (
                                  <Button variant="ghost" size="sm" className="h-8 px-3 text-xs" onClick={() => handleAction(notification.id, 'secondary')}>
                                    {notification.actions.secondary.label}
                                  </Button>
                                )}
                                {notification.actions.primary && (
                                  <Button
                                    size="sm"
                                    className="h-8 px-3 text-xs bg-primary hover:bg-primary/90"
                                    onClick={() => handleAction(notification.id, 'primary')}
                                  >
                                    {notification.actions.primary.label}
                                  </Button>
                                )}
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    </Card>
                  ))}
                </div>
              </div>
            )}

            {/* Yesterday Section */}
            {yesterdayNotifications.length > 0 && (
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <h3 className="text-sm font-medium text-muted-foreground">Yesterday</h3>
                    <Badge variant="secondary" className="text-xs">
                      {yesterdayNotifications.length}
                    </Badge>
                  </div>
                  <Button variant="ghost" size="sm" className="text-xs text-muted-foreground hover:text-foreground">
                    See All
                  </Button>
                </div>

                <div className="space-y-3">
                  {yesterdayNotifications.map((notification) => (
                    <Card key={notification.id} className="bg-muted/30 border-border/40 p-4">
                      <div className="flex items-start gap-3">
                        <div className="relative">
                          {getNotificationIcon(notification)}
                          {!notification.isRead && <div className="absolute -top-1 -right-1 w-3 h-3 bg-blue-500 rounded-full" />}
                        </div>

                        <div className="flex-1 space-y-2">
                          {formatNotificationContent(notification)}

                          <div className="flex items-center justify-between">
                            <span className="text-xs text-muted-foreground">{notification.timestamp}</span>

                            {notification.actions && (
                              <div className="flex items-center gap-2">
                                {notification.actions.secondary && (
                                  <Button variant="ghost" size="sm" className="h-8 px-3 text-xs" onClick={() => handleAction(notification.id, 'secondary')}>
                                    {notification.actions.secondary.label}
                                  </Button>
                                )}
                                {notification.actions.primary && (
                                  <Button
                                    size="sm"
                                    className="h-8 px-3 text-xs bg-primary hover:bg-primary/90"
                                    onClick={() => handleAction(notification.id, 'primary')}
                                  >
                                    {notification.actions.primary.label}
                                  </Button>
                                )}
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    </Card>
                  ))}
                </div>
              </div>
            )}

            {/* Empty State */}
            {getNotificationsByTab(activeTab).length === 0 && (
              <div className="text-center py-12 text-muted-foreground">
                <Bell className="w-12 h-12 mx-auto mb-3 opacity-40" />
                <p className="text-sm">No {activeTab} notifications</p>
                <p className="text-xs mt-1 opacity-75">
                  {activeTab === 'unread' && "You're all caught up!"}
                  {activeTab === 'read' && 'No read notifications yet'}
                  {activeTab === 'archived' && 'No archived notifications'}
                </p>
              </div>
            )}
          </TabsContent>
        </Tabs>
      </CardContent>
    </Card>
  );
}

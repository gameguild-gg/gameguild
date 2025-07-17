'use client';

import React, { useState } from 'react';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@game-guild/ui/components/avatar';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@game-guild/ui/components/tabs';
import { ScrollArea } from '@game-guild/ui/components/scroll-area';
import { Separator } from '@game-guild/ui/components/separator';
import { Switch } from '@game-guild/ui/components/switch';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@game-guild/ui/components/dropdown-menu';
import {
  AlertCircle,
  Bell,
  BookOpen,
  CheckCircle,
  Clock,
  Eye,
  EyeOff,
  Filter,
  Gift,
  Mail,
  MoreHorizontal,
  Settings,
  Smartphone,
  Star,
  Trash2,
  Trophy,
  Users,
} from 'lucide-react';

// Enhanced notification types
interface Notification {
  id: string;
  type: 'course' | 'achievement' | 'social' | 'system' | 'promotion' | 'reminder';
  title: string;
  message: string;
  timestamp: Date;
  isRead: boolean;
  isStarred?: boolean;
  priority: 'low' | 'medium' | 'high';
  actionUrl?: string;
  actionText?: string;
  avatar?: string;
  metadata?: {
    courseId?: string;
    userId?: string;
    achievementId?: string;
    groupId?: string;
  };
}

// Mock notifications data
const mockNotifications: Notification[] = [
  {
    id: '1',
    type: 'course',
    title: 'New Course Available',
    message: 'Advanced Unity Scripting is now live! Start learning advanced C# patterns for game development.',
    timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000), // 2 hours ago
    isRead: false,
    priority: 'high',
    actionUrl: '/courses/advanced-unity-scripting',
    actionText: 'View Course',
    avatar: '/courses/unity-advanced.jpg',
  },
  {
    id: '2',
    type: 'achievement',
    title: 'Achievement Unlocked!',
    message: 'Congratulations! You\'ve completed your first learning track and earned the "Track Master" badge.',
    timestamp: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000), // 1 day ago
    isRead: false,
    isStarred: true,
    priority: 'medium',
    actionUrl: '/achievements',
    actionText: 'View Achievement',
    metadata: { achievementId: 'track-master' },
  },
  {
    id: '3',
    type: 'social',
    title: 'Study Group Invitation',
    message: 'Sarah Chen invited you to join the "Unity Beginners" study group. 12 members are already participating!',
    timestamp: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000), // 3 days ago
    isRead: true,
    priority: 'medium',
    actionUrl: '/community/groups/unity-beginners',
    actionText: 'Join Group',
    avatar: '/avatars/sarah.jpg',
    metadata: { userId: 'sarah-chen', groupId: 'unity-beginners' },
  },
  {
    id: '4',
    type: 'reminder',
    title: 'Course Deadline Reminder',
    message: 'Your "Game Physics" course assignment is due in 2 days. Don\'t forget to submit your project!',
    timestamp: new Date(Date.now() - 4 * 60 * 60 * 1000), // 4 hours ago
    isRead: false,
    priority: 'high',
    actionUrl: '/courses/game-physics/assignment',
    actionText: 'View Assignment',
    metadata: { courseId: 'game-physics' },
  },
  {
    id: '5',
    type: 'promotion',
    title: 'Special Offer: 30% Off',
    message: 'Limited time offer! Get 30% off the Game Art & Design track. Offer expires in 24 hours.',
    timestamp: new Date(Date.now() - 6 * 60 * 60 * 1000), // 6 hours ago
    isRead: true,
    priority: 'medium',
    actionUrl: '/tracks/game-art-design?promo=30OFF',
    actionText: 'Claim Offer',
  },
  {
    id: '6',
    type: 'system',
    title: 'Maintenance Scheduled',
    message: 'Scheduled maintenance on Sunday, 2 AM - 4 AM EST. The platform will be temporarily unavailable.',
    timestamp: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000), // 2 days ago
    isRead: true,
    priority: 'low',
    actionUrl: '/status',
    actionText: 'More Info',
  },
];

// Notification settings interface
interface NotificationSettings {
  email: boolean;
  push: boolean;
  inApp: boolean;
  courses: boolean;
  achievements: boolean;
  social: boolean;
  promotions: boolean;
  reminders: boolean;
}

export default function EnhancedNotifications() {
  const [notifications, setNotifications] = useState<Notification[]>(mockNotifications);
  const [filter, setFilter] = useState<'all' | 'unread' | 'starred'>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [showSettings, setShowSettings] = useState(false);

  // Mock notification settings
  const [settings, setSettings] = useState<NotificationSettings>({
    email: true,
    push: true,
    inApp: true,
    courses: true,
    achievements: true,
    social: true,
    promotions: false,
    reminders: true,
  });

  const unreadCount = notifications.filter((n) => !n.isRead).length;
  const starredCount = notifications.filter((n) => n.isStarred).length;

  // Filter notifications
  const filteredNotifications = notifications.filter((notification) => {
    if (filter === 'unread' && notification.isRead) return false;
    if (filter === 'starred' && !notification.isStarred) return false;
    if (typeFilter !== 'all' && notification.type !== typeFilter) return false;
    return true;
  });

  // Notification type icons and colors
  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'course':
        return { icon: BookOpen, color: 'text-blue-600', bg: 'bg-blue-500/10' };
      case 'achievement':
        return { icon: Trophy, color: 'text-yellow-600', bg: 'bg-yellow-500/10' };
      case 'social':
        return { icon: Users, color: 'text-purple-600', bg: 'bg-purple-500/10' };
      case 'reminder':
        return { icon: Clock, color: 'text-orange-600', bg: 'bg-orange-500/10' };
      case 'promotion':
        return { icon: Gift, color: 'text-green-600', bg: 'bg-green-500/10' };
      case 'system':
        return { icon: AlertCircle, color: 'text-gray-600', bg: 'bg-gray-500/10' };
      default:
        return { icon: Bell, color: 'text-gray-600', bg: 'bg-gray-500/10' };
    }
  };

  // Format timestamp
  const formatTimestamp = (date: Date) => {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / (1000 * 60));
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));

    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    return `${days}d ago`;
  };

  // Mark notification as read
  const markAsRead = (id: string) => {
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
  };

  // Toggle star
  const toggleStar = (id: string) => {
    setNotifications((prev) =>
      prev.map((notification) =>
        notification.id === id
          ? {
              ...notification,
              isStarred: !notification.isStarred,
            }
          : notification,
      ),
    );
  };

  // Mark all as read
  const markAllAsRead = () => {
    setNotifications((prev) => prev.map((notification) => ({ ...notification, isRead: true })));
  };

  // Delete notification
  const deleteNotification = (id: string) => {
    setNotifications((prev) => prev.filter((notification) => notification.id !== id));
  };

  return (
    <div className="max-w-4xl mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Notifications</h1>
          <p className="text-muted-foreground">Stay updated with your learning progress and community activities</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={() => setShowSettings(!showSettings)}>
            <Settings className="h-4 w-4 mr-2" />
            Settings
          </Button>
          <Button onClick={markAllAsRead} disabled={unreadCount === 0}>
            <CheckCircle className="h-4 w-4 mr-2" />
            Mark All Read
          </Button>
        </div>
      </div>

      {/* Notification Settings Panel */}
      {showSettings && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Settings className="h-5 w-5" />
              Notification Settings
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-6">
            {/* Delivery Methods */}
            <div>
              <h3 className="font-semibold mb-3">Delivery Methods</h3>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="flex items-center justify-between p-3 border rounded-lg">
                  <div className="flex items-center gap-2">
                    <Mail className="h-4 w-4" />
                    <span>Email</span>
                  </div>
                  <Switch checked={settings.email} onCheckedChange={(checked) => setSettings((prev) => ({ ...prev, email: checked }))} />
                </div>
                <div className="flex items-center justify-between p-3 border rounded-lg">
                  <div className="flex items-center gap-2">
                    <Smartphone className="h-4 w-4" />
                    <span>Push</span>
                  </div>
                  <Switch checked={settings.push} onCheckedChange={(checked) => setSettings((prev) => ({ ...prev, push: checked }))} />
                </div>
                <div className="flex items-center justify-between p-3 border rounded-lg">
                  <div className="flex items-center gap-2">
                    <Bell className="h-4 w-4" />
                    <span>In-App</span>
                  </div>
                  <Switch checked={settings.inApp} onCheckedChange={(checked) => setSettings((prev) => ({ ...prev, inApp: checked }))} />
                </div>
              </div>
            </div>

            <Separator />

            {/* Notification Types */}
            <div>
              <h3 className="font-semibold mb-3">Notification Types</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <BookOpen className="h-4 w-4 text-blue-600" />
                    <span>Course Updates</span>
                  </div>
                  <Switch checked={settings.courses} onCheckedChange={(checked) => setSettings((prev) => ({ ...prev, courses: checked }))} />
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Trophy className="h-4 w-4 text-yellow-600" />
                    <span>Achievements</span>
                  </div>
                  <Switch
                    checked={settings.achievements}
                    onCheckedChange={(checked) =>
                      setSettings((prev) => ({
                        ...prev,
                        achievements: checked,
                      }))
                    }
                  />
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Users className="h-4 w-4 text-purple-600" />
                    <span>Social Activity</span>
                  </div>
                  <Switch checked={settings.social} onCheckedChange={(checked) => setSettings((prev) => ({ ...prev, social: checked }))} />
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Gift className="h-4 w-4 text-green-600" />
                    <span>Promotions</span>
                  </div>
                  <Switch checked={settings.promotions} onCheckedChange={(checked) => setSettings((prev) => ({ ...prev, promotions: checked }))} />
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Clock className="h-4 w-4 text-orange-600" />
                    <span>Reminders</span>
                  </div>
                  <Switch checked={settings.reminders} onCheckedChange={(checked) => setSettings((prev) => ({ ...prev, reminders: checked }))} />
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Filters and Stats */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-2">
          <Badge variant="secondary" className="flex items-center gap-1">
            <Bell className="h-3 w-3" />
            {notifications.length} Total
          </Badge>
          <Badge variant="destructive" className="flex items-center gap-1">
            <AlertCircle className="h-3 w-3" />
            {unreadCount} Unread
          </Badge>
          <Badge variant="outline" className="flex items-center gap-1">
            <Star className="h-3 w-3" />
            {starredCount} Starred
          </Badge>
        </div>

        <div className="flex items-center gap-2">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm">
                <Filter className="h-4 w-4 mr-2" />
                Type: {typeFilter === 'all' ? 'All' : typeFilter}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent>
              <DropdownMenuItem onClick={() => setTypeFilter('all')}>All Types</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setTypeFilter('course')}>Course Updates</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setTypeFilter('achievement')}>Achievements</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setTypeFilter('social')}>Social</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setTypeFilter('reminder')}>Reminders</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setTypeFilter('promotion')}>Promotions</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setTypeFilter('system')}>System</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      {/* Notification Tabs */}
      <Tabs value={filter} onValueChange={(value) => setFilter(value as any)}>
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="all" className="flex items-center gap-2">
            <Bell className="h-4 w-4" />
            All ({notifications.length})
          </TabsTrigger>
          <TabsTrigger value="unread" className="flex items-center gap-2">
            <AlertCircle className="h-4 w-4" />
            Unread ({unreadCount})
          </TabsTrigger>
          <TabsTrigger value="starred" className="flex items-center gap-2">
            <Star className="h-4 w-4" />
            Starred ({starredCount})
          </TabsTrigger>
        </TabsList>

        {['all', 'unread', 'starred'].map((tabValue) => (
          <TabsContent key={tabValue} value={tabValue} className="mt-6">
            <ScrollArea className="h-[600px]">
              <div className="space-y-4">
                {filteredNotifications.length === 0 ? (
                  <Card>
                    <CardContent className="text-center py-12">
                      <Bell className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
                      <h3 className="font-semibold text-lg mb-2">No notifications</h3>
                      <p className="text-muted-foreground">
                        {filter === 'unread'
                          ? "You're all caught up! No unread notifications."
                          : filter === 'starred'
                            ? 'No starred notifications yet.'
                            : 'No notifications to display.'}
                      </p>
                    </CardContent>
                  </Card>
                ) : (
                  filteredNotifications.map((notification) => {
                    const { icon: Icon, color, bg } = getNotificationIcon(notification.type);

                    return (
                      <Card
                        key={notification.id}
                        className={`transition-all duration-200 hover:shadow-md ${!notification.isRead ? 'border-l-4 border-l-primary bg-primary/5' : ''}`}
                      >
                        <CardContent className="p-4">
                          <div className="flex items-start gap-4">
                            {/* Notification Icon/Avatar */}
                            <div className={`${bg} rounded-full p-2 flex-shrink-0`}>
                              {notification.avatar ? (
                                <Avatar className="h-8 w-8">
                                  <AvatarImage src={notification.avatar} />
                                  <AvatarFallback>
                                    <Icon className={`h-4 w-4 ${color}`} />
                                  </AvatarFallback>
                                </Avatar>
                              ) : (
                                <Icon className={`h-4 w-4 ${color}`} />
                              )}
                            </div>

                            {/* Notification Content */}
                            <div className="flex-1 min-w-0">
                              <div className="flex items-start justify-between mb-2">
                                <h3 className={`font-semibold ${!notification.isRead ? 'text-foreground' : 'text-muted-foreground'}`}>{notification.title}</h3>
                                <div className="flex items-center gap-2 ml-2">
                                  <span className="text-xs text-muted-foreground whitespace-nowrap">{formatTimestamp(notification.timestamp)}</span>
                                  <DropdownMenu>
                                    <DropdownMenuTrigger asChild>
                                      <Button variant="ghost" size="sm" className="h-6 w-6 p-0">
                                        <MoreHorizontal className="h-3 w-3" />
                                      </Button>
                                    </DropdownMenuTrigger>
                                    <DropdownMenuContent align="end">
                                      <DropdownMenuItem onClick={() => markAsRead(notification.id)}>
                                        {notification.isRead ? (
                                          <>
                                            <EyeOff className="h-4 w-4 mr-2" />
                                            Mark Unread
                                          </>
                                        ) : (
                                          <>
                                            <Eye className="h-4 w-4 mr-2" />
                                            Mark Read
                                          </>
                                        )}
                                      </DropdownMenuItem>
                                      <DropdownMenuItem onClick={() => toggleStar(notification.id)}>
                                        <Star className={`h-4 w-4 mr-2 ${notification.isStarred ? 'fill-yellow-400 text-yellow-400' : ''}`} />
                                        {notification.isStarred ? 'Unstar' : 'Star'}
                                      </DropdownMenuItem>
                                      <DropdownMenuSeparator />
                                      <DropdownMenuItem onClick={() => deleteNotification(notification.id)} className="text-red-600 focus:text-red-600">
                                        <Trash2 className="h-4 w-4 mr-2" />
                                        Delete
                                      </DropdownMenuItem>
                                    </DropdownMenuContent>
                                  </DropdownMenu>
                                </div>
                              </div>

                              <p className="text-sm text-muted-foreground mb-3 leading-relaxed">{notification.message}</p>

                              <div className="flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                  <Badge variant="outline" className="text-xs capitalize">
                                    {notification.type}
                                  </Badge>
                                  {notification.priority === 'high' && (
                                    <Badge variant="destructive" className="text-xs">
                                      High Priority
                                    </Badge>
                                  )}
                                  {notification.isStarred && <Star className="h-3 w-3 fill-yellow-400 text-yellow-400" />}
                                </div>

                                {notification.actionUrl && (
                                  <Button
                                    size="sm"
                                    variant="outline"
                                    onClick={() => {
                                      markAsRead(notification.id);
                                      // Navigate to actionUrl
                                    }}
                                  >
                                    {notification.actionText || 'View'}
                                  </Button>
                                )}
                              </div>
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    );
                  })
                )}
              </div>
            </ScrollArea>
          </TabsContent>
        ))}
      </Tabs>
    </div>
  );
}

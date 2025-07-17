'use server';

import { revalidateTag } from 'next/cache';
import type { Notification, NotificationActionState, NotificationFilters, NotificationResponse } from '@/components/legacy/types/notification';

// Mock data for demonstration - in a real app, this would connect to your database
const mockNotifications: Notification[] = [
  {
    id: '1',
    type: 'comment',
    title: 'New Comment',
    message: 'Great work on the last update! The new features are exactly what we needed.',
    user: {
      id: 'john-doe',
      name: 'John',
      avatar: 'https://github.com/shadcn.png',
    },
    projectName: 'Project Alpha',
    timestamp: new Date('2024-10-11T11:00:00Z'),
    isRead: false,
    isArchived: false,
    actionButtons: {
      view: true,
      respond: true,
    },
  },
  {
    id: '2',
    type: 'invite',
    title: 'Project Invitation',
    message: 'invited you to',
    user: {
      id: 'john-doe',
      name: 'John',
      avatar: 'https://github.com/shadcn.png',
    },
    projectName: 'Project Beta',
    timestamp: new Date('2024-10-10T11:00:00Z'),
    isRead: false,
    isArchived: false,
    actionButtons: {
      accept: true,
      decline: true,
    },
  },
  {
    id: '3',
    type: 'follow',
    title: 'New Follower',
    message: 'followed you',
    user: {
      id: 'jennifer-lee',
      name: 'Jennifer Lee',
      avatar: 'https://github.com/shadcn.png',
    },
    timestamp: new Date('2024-11-06T20:12:00Z'),
    isRead: false,
    isArchived: false,
    actionButtons: {
      view: true,
    },
  },
  {
    id: '4',
    type: 'task',
    title: 'Task Assignment',
    message: 'assigned a task to you',
    user: {
      id: 'eve-monroe',
      name: 'Eve Monroe',
      avatar: 'https://github.com/shadcn.png',
    },
    taskId: '#IG-2137',
    timestamp: new Date('2024-11-05T20:56:00Z'),
    isRead: false,
    isArchived: false,
    actionButtons: {
      view: true,
    },
  },
  {
    id: '5',
    type: 'reminder',
    title: 'Reminder',
    message: 'has sent you a reminder',
    user: {
      id: 'boyd-larkin',
      name: 'Boyd Larkin',
      avatar: 'https://github.com/shadcn.png',
    },
    timestamp: new Date('2024-12-01T14:00:00Z'),
    isRead: false,
    isArchived: false,
    actionButtons: {
      respond: true,
    },
  },
  {
    id: '6',
    type: 'system',
    title: 'Status Update',
    message: 'has changed the status of Design Project from In Progress to Completed',
    user: {
      id: 'josh-krajcik',
      name: 'Josh Krajcik',
      avatar: 'https://github.com/shadcn.png',
    },
    projectName: 'Design Project',
    timestamp: new Date('2024-12-01T12:00:00Z'),
    isRead: true,
    isArchived: false,
    actionButtons: {
      view: true,
    },
  },
];

export async function getNotifications(filters: NotificationFilters = {}): Promise<NotificationResponse> {
  const { type = 'all', category = 'all', limit = 50, offset = 0 } = filters;

  // Simulate API delay
  await new Promise((resolve) => setTimeout(resolve, 100));

  let filteredNotifications = [...mockNotifications];

  // Apply filters
  if (type === 'unread') {
    filteredNotifications = filteredNotifications.filter((n) => !n.isRead);
  } else if (type === 'read') {
    filteredNotifications = filteredNotifications.filter((n) => n.isRead);
  } else if (type === 'archived') {
    filteredNotifications = filteredNotifications.filter((n) => n.isArchived);
  }

  if (category === 'following') {
    filteredNotifications = filteredNotifications.filter((n) => n.type === 'follow');
  } else if (category === 'archive') {
    filteredNotifications = filteredNotifications.filter((n) => n.isArchived);
  }

  // Sort by timestamp (newest first)
  filteredNotifications.sort((a, b) => b.timestamp.getTime() - a.timestamp.getTime());

  // Apply pagination
  const paginatedNotifications = filteredNotifications.slice(offset, offset + limit);

  const unreadCount = mockNotifications.filter((n) => !n.isRead && !n.isArchived).length;

  return {
    notifications: paginatedNotifications,
    unreadCount,
    totalCount: filteredNotifications.length,
  };
}

export async function markNotificationAsRead(notificationId: string): Promise<NotificationActionState> {
  try {
    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 100));

    const notification = mockNotifications.find((n) => n.id === notificationId);
    if (!notification) {
      return {
        success: false,
        error: 'Notification not found',
      };
    }

    notification.isRead = true;

    revalidateTag('notifications');

    return {
      success: true,
      message: 'Notification marked as read',
    };
  } catch {
    return {
      success: false,
      error: 'Failed to mark notification as read',
    };
  }
}

export async function markAllNotificationsAsRead(): Promise<NotificationActionState> {
  try {
    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 200));

    mockNotifications.forEach((notification) => {
      if (!notification.isArchived) {
        notification.isRead = true;
      }
    });

    revalidateTag('notifications');

    return {
      success: true,
      message: 'All notifications marked as read',
    };
  } catch {
    return {
      success: false,
      error: 'Failed to mark all notifications as read',
    };
  }
}

export async function archiveNotification(notificationId: string): Promise<NotificationActionState> {
  try {
    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 100));

    const notification = mockNotifications.find((n) => n.id === notificationId);
    if (!notification) {
      return {
        success: false,
        error: 'Notification not found',
      };
    }

    notification.isArchived = true;
    notification.isRead = true;

    revalidateTag('notifications');

    return {
      success: true,
      message: 'Notification archived',
    };
  } catch {
    return {
      success: false,
      error: 'Failed to archive notification',
    };
  }
}

export async function acceptProjectInvite(notificationId: string): Promise<NotificationActionState> {
  try {
    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 300));

    const notification = mockNotifications.find((n) => n.id === notificationId);
    if (!notification) {
      return {
        success: false,
        error: 'Notification not found',
      };
    }

    // Mark as read and remove action buttons
    notification.isRead = true;
    notification.actionButtons = undefined;
    notification.message = `You accepted the invitation to ${notification.projectName}`;

    revalidateTag('notifications');

    return {
      success: true,
      message: 'Project invitation accepted',
    };
  } catch {
    return {
      success: false,
      error: 'Failed to accept invitation',
    };
  }
}

export async function declineProjectInvite(notificationId: string): Promise<NotificationActionState> {
  try {
    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 300));

    const notification = mockNotifications.find((n) => n.id === notificationId);
    if (!notification) {
      return {
        success: false,
        error: 'Notification not found',
      };
    }

    // Mark as read and remove action buttons
    notification.isRead = true;
    notification.actionButtons = undefined;
    notification.message = `You declined the invitation to ${notification.projectName}`;

    revalidateTag('notifications');

    return {
      success: true,
      message: 'Project invitation declined',
    };
  } catch {
    return {
      success: false,
      error: 'Failed to decline invitation',
    };
  }
}

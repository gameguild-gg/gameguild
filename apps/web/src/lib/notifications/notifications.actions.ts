'use server';

import type { Notification, NotificationFilters } from '@/components/legacy/types/notification';

/**
 * Stub implementations for notification actions.
 * This module is disabled in production.
 */

export async function getNotifications(_filters?: NotificationFilters): Promise<{ notifications: Notification[]; unreadCount: number }> {
    return { notifications: [], unreadCount: 0 };
}

export async function getUnreadNotifications() {
    return { data: [], count: 0, error: null };
}

export async function markNotificationAsRead(_id: string): Promise<{ success: boolean; error?: string }> {
    return { success: true };
}

export async function markAllNotificationsAsRead(): Promise<{ success: boolean; message?: string; error?: string }> {
    return { success: true, message: 'All notifications marked as read' };
}

export async function deleteNotification(_id: string): Promise<{ success: boolean; error?: string }> {
    return { success: true };
}

export async function archiveNotification(_id: string): Promise<{ success: boolean; error?: string }> {
    return { success: true };
}

export async function getNotificationPreferences() {
    return { data: {}, error: null };
}

export async function updateNotificationPreferences(_data: any) {
    return { success: true };
}

// Project invite actions (stub)
export async function acceptProjectInvite(_notificationId: string): Promise<{ success: boolean; message?: string; error?: string }> {
    return { success: false, error: 'Project invites are disabled' };
}

export async function declineProjectInvite(_notificationId: string): Promise<{ success: boolean; message?: string; error?: string }> {
    return { success: false, error: 'Project invites are disabled' };
}

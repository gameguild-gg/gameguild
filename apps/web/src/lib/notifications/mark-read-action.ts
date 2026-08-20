'use server';

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { revalidatePath } from 'next/cache';

/**
 * Server actions for the dashboard bell dropdown (settings header).
 *
 * Operates on the Identity.Users UserNotifications table via the generated
 * UsersNotificationsModule — intentionally disconnected from the
 * Notifications email pipeline. userId always comes from the session,
 * never from the client payload.
 */

export type NotificationReadActionStatus = 'success' | 'unauthorized' | 'error';

export interface NotificationReadActionResult {
  success: boolean;
  status: NotificationReadActionStatus;
}

async function getSessionUserId(): Promise<string | null> {
  const session = await auth();
  if (!session || typeof session === 'function') return null;
  return session.user?.id ?? null;
}

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function toActionResult(ok: boolean): NotificationReadActionResult {
  if (ok) return { success: true, status: 'success' };
  return { success: false, status: 'error' };
}

export async function setNotificationReadAction(
  notificationId: string,
  isRead: boolean,
): Promise<NotificationReadActionResult> {
  const userId = await getSessionUserId();
  if (!userId || !notificationId) {
    return { success: false, status: 'unauthorized' };
  }

  const notifications = new GeneratedApi.UsersNotificationsModule(getApiClient());
  const result = isRead
    ? await notifications.postUsersNotificationsMarkAsReadForPostUsersByUserIdNotificationsByNotificationIdMarkAsRead(
        userId,
        notificationId,
      )
    : await notifications.postUsersNotificationsMarkAsUnreadForPostUsersByUserIdNotificationsByNotificationIdMarkAsUnread(
        userId,
        notificationId,
      );

  if (!result.ok) {
    return toActionResult(false);
  }

  revalidatePath('/', 'layout');
  return { success: true, status: 'success' };
}

export async function markAllNotificationsReadAction(): Promise<NotificationReadActionResult> {
  const userId = await getSessionUserId();
  if (!userId) {
    return { success: false, status: 'unauthorized' };
  }

  const notifications = new GeneratedApi.UsersNotificationsModule(getApiClient());
  const result =
    await notifications.postUsersNotificationsMarkAsReadForPostUsersByUserIdNotificationsMarkAsRead(
      userId,
      // Matches the bell list filter (non-archived) so the badge reflects
      // exactly what the dropdown excludes.
      { filterCriteria: { isRead: false, isArchived: false } },
    );

  if (!result.ok) {
    return toActionResult(false);
  }

  revalidatePath('/', 'layout');
  return { success: true, status: 'success' };
}

import { getToken } from '@/auth';
import { createServerClient, GeneratedApi, type IdentityUsersUserNotificationDto } from '@game-guild/client';
import { getWorkspaceMyTeamInvitations, type WorkspaceMyTeamInvitation } from '@/lib/workspaces';

export interface DashboardNotificationItem {
  id: string;
  title: string;
  message: string;
  createdLabel: string;
  isRead: boolean;
  actionUrl?: string;
  actionText?: string;
}

export interface DashboardNotificationSummary {
  items: DashboardNotificationItem[];
  unreadCount: number;
}

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function formatNotificationDate(value?: string | null) {
  if (!value) return 'Unknown time';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Unknown time';

  return `${new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    timeZone: 'UTC',
  }).format(date)} UTC`;
}

function mapNotification(notification: IdentityUsersUserNotificationDto): DashboardNotificationItem {
  const createdAt = notification.createdAt ?? notification.updatedAt;

  return {
    id: notification.id ?? `${notification.title ?? 'notification'}-${createdAt ?? 'unknown'}`,
    title: notification.title?.trim() || notification.type?.trim() || 'Notification',
    message: notification.message?.trim() || notification.category?.trim() || 'No additional details.',
    createdLabel: formatNotificationDate(createdAt),
    isRead: notification.isRead ?? false,
    actionUrl: notification.actionUrl ?? undefined,
    actionText: notification.actionText ?? undefined,
  };
}

function notificationItem(invitation: WorkspaceMyTeamInvitation): DashboardNotificationItem {
  return {
    id: `team-invitation-${invitation.id}`,
    title: 'Team invitation',
    message: `${invitation.teamName} invited you to join their team.`,
    createdLabel: 'Pending',
    isRead: false,
    actionUrl: '/workspace/invitations',
    actionText: 'Review invitation',
  };
}

function getPagedItems(data: unknown): IdentityUsersUserNotificationDto[] {
  const items = (data as { items?: unknown[] | null } | null)?.items;
  return Array.isArray(items) ? (items as IdentityUsersUserNotificationDto[]) : [];
}

export async function getDashboardNotificationSummary(userId: string): Promise<DashboardNotificationSummary> {
  if (!userId) {
    return { items: [], unreadCount: 0 };
  }

  try {
    const notifications = new GeneratedApi.UsersNotificationsModule(getApiClient());
    const [recentResult, unreadResult, invitations] = await Promise.all([
      notifications.getUsersNotificationsForGetUsersByUserIdNotifications(userId, {
        page: 1,
        pageSize: 5,
        isArchived: false,
        sortBy: 'createdAt',
        sortDirection: 'desc',
      }),
      notifications.getUsersNotificationsForGetUsersByUserIdNotifications(userId, {
        page: 1,
        pageSize: 1,
        isArchived: false,
        isRead: false,
      }),
      getWorkspaceMyTeamInvitations(),
    ]);

    const items = recentResult.ok ? getPagedItems(recentResult.data).map(mapNotification) : [];
    const unreadCount = unreadResult.ok
      ? unreadResult.data.totalCount ?? getPagedItems(unreadResult.data).length
      : items.filter((item) => !item.isRead).length;

    const invitationItems = invitations.map(notificationItem);

    return { items: [...invitationItems, ...items], unreadCount: unreadCount + invitations.length };
  } catch {
    return { items: [], unreadCount: 0 };
  }
}

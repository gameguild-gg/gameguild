import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  getUsersNotificationsForGetUsersByUserIdNotifications: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    UsersNotificationsModule: class {
      getUsersNotificationsForGetUsersByUserIdNotifications = mocks.getUsersNotificationsForGetUsersByUserIdNotifications;
    },
  },
}));

import { getDashboardNotificationSummary } from './dashboard-notifications';

describe('dashboard notification summary', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('access-token');
  });

  it('loads recent notifications and unread count from the user notifications API', async () => {
    mocks.getUsersNotificationsForGetUsersByUserIdNotifications
      .mockResolvedValueOnce({
        ok: true,
        data: {
          items: [
            {
              id: 'notification-1',
              title: 'Project review ready',
              message: 'Your project received a new review.',
              isRead: false,
              actionUrl: '/console/community',
              actionText: 'Open community',
              createdAt: '2026-06-14T10:30:00.000Z',
            },
          ],
          totalCount: 12,
        },
      })
      .mockResolvedValueOnce({
        ok: true,
        data: {
          items: [],
          totalCount: 4,
        },
      });

    const summary = await getDashboardNotificationSummary('user-1');

    expect(mocks.getUsersNotificationsForGetUsersByUserIdNotifications).toHaveBeenNthCalledWith(1, 'user-1', {
      page: 1,
      pageSize: 5,
      isArchived: false,
      sortBy: 'createdAt',
      sortDirection: 'desc',
    });
    expect(mocks.getUsersNotificationsForGetUsersByUserIdNotifications).toHaveBeenNthCalledWith(2, 'user-1', {
      page: 1,
      pageSize: 1,
      isArchived: false,
      isRead: false,
    });
    expect(summary).toEqual({
      unreadCount: 4,
      items: [
        {
          id: 'notification-1',
          title: 'Project review ready',
          message: 'Your project received a new review.',
          createdLabel: 'Jun 14, 10:30 AM UTC',
          isRead: false,
          actionUrl: '/console/community',
          actionText: 'Open community',
        },
      ],
    });
  });

  it('falls back to an empty summary when the user is missing or the API fails', async () => {
    await expect(getDashboardNotificationSummary('')).resolves.toEqual({ items: [], unreadCount: 0 });

    mocks.getUsersNotificationsForGetUsersByUserIdNotifications.mockRejectedValue(new Error('network failed'));

    await expect(getDashboardNotificationSummary('user-2')).resolves.toEqual({ items: [], unreadCount: 0 });
  });
});

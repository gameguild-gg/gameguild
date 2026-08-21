import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  revalidatePath: vi.fn(),
  markAsReadById: vi.fn(),
  markAsUnreadById: vi.fn(),
  markAsReadBulk: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    UsersNotificationsModule: class {
      postUsersNotificationsMarkAsReadForPostUsersByUserIdNotificationsByNotificationIdMarkAsRead =
        mocks.markAsReadById;
      postUsersNotificationsMarkAsUnreadForPostUsersByUserIdNotificationsByNotificationIdMarkAsUnread =
        mocks.markAsUnreadById;
      postUsersNotificationsMarkAsReadForPostUsersByUserIdNotificationsMarkAsRead = mocks.markAsReadBulk;
    },
  },
}));

const { setNotificationReadAction, markAllNotificationsReadAction } = await import(
  './mark-read-action'
);

function apiError(status: number) {
  return { ok: false as const, error: { name: 'ApiError' as const, status, code: 'ERROR' as const, message: 'boom' } };
}

describe('notification mark-read actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
    mocks.getToken.mockResolvedValue('access-token');
    mocks.markAsReadById.mockResolvedValue({ ok: true, data: undefined });
    mocks.markAsUnreadById.mockResolvedValue({ ok: true, data: undefined });
    mocks.markAsReadBulk.mockResolvedValue({ ok: true, data: undefined });
  });

  it('marks a single notification read against the session user and revalidates', async () => {
    const result = await setNotificationReadAction('notif-1', true);

    expect(result).toEqual({ success: true, status: 'success' });
    expect(mocks.markAsReadById).toHaveBeenCalledWith('user-1', 'notif-1');
    expect(mocks.markAsUnreadById).not.toHaveBeenCalled();
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/', 'layout');
  });

  it('marks a single notification unread via the unread endpoint', async () => {
    const result = await setNotificationReadAction('notif-1', false);

    expect(result).toEqual({ success: true, status: 'success' });
    expect(mocks.markAsUnreadById).toHaveBeenCalledWith('user-1', 'notif-1');
    expect(mocks.markAsReadById).not.toHaveBeenCalled();
  });

  it('returns unauthorized without calling the API when the session is missing', async () => {
    mocks.auth.mockResolvedValue(null);

    const result = await setNotificationReadAction('notif-1', true);

    expect(result).toEqual({ success: false, status: 'unauthorized' });
    expect(mocks.markAsReadById).not.toHaveBeenCalled();
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });

  it('maps API failures to error and skips revalidation', async () => {
    mocks.markAsReadById.mockResolvedValue(apiError(500));

    const result = await setNotificationReadAction('notif-1', true);

    expect(result).toEqual({ success: false, status: 'error' });
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });

  it('marks all unread non-archived notifications read via the bulk endpoint', async () => {
    const result = await markAllNotificationsReadAction();

    expect(result).toEqual({ success: true, status: 'success' });
    expect(mocks.markAsReadBulk).toHaveBeenCalledWith('user-1', {
      filterCriteria: { isRead: false, isArchived: false },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/', 'layout');
  });

  it('maps bulk API failures to error', async () => {
    mocks.markAsReadBulk.mockResolvedValue(apiError(403));

    const result = await markAllNotificationsReadAction();

    expect(result).toEqual({ success: false, status: 'error' });
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });
});

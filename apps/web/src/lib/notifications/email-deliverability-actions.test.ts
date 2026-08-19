import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  revalidatePath: vi.fn(),
  deleteSuppression: vi.fn(),
  postRequeue: vi.fn(),
  getTimeline: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    NotificationsModule: class {
      deleteEmailDeliverySuppressions = mocks.deleteSuppression;
      postEmailDeliveryNotificationsRequeue = mocks.postRequeue;
      getEmailDeliveryNotificationsTimeline = mocks.getTimeline;
    },
  },
}));

const {
  unsuppressEmailAction,
  requeueNotificationAction,
  getNotificationTimelineAction,
} = await import('./email-deliverability-actions');

function apiError(status: number) {
  return { ok: false as const, error: { name: 'ApiError' as const, status, code: 'ERROR' as const, message: 'boom' } };
}

describe('email deliverability actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
    mocks.getToken.mockResolvedValue('access-token');
    mocks.deleteSuppression.mockResolvedValue({ ok: true, data: { emailAddress: 'a@b.co', wasActive: true } });
    mocks.postRequeue.mockResolvedValue({ ok: true, data: { id: 'n-1', deliveryStatus: 'Pending', requeueCount: 1 } });
    mocks.getTimeline.mockResolvedValue({
      ok: true,
      data: {
        notificationId: 'n-1',
        providerMessageId: 'ses-123',
        events: [
          { id: 'e-2', eventType: 'Delivery', occurredAt: '2026-08-19T10:02:00Z', recipientEmail: 'a@b.co' },
          { id: 'e-1', eventType: 'Send', occurredAt: '2026-08-19T10:01:00Z', recipientEmail: 'a@b.co', bounceType: null, diagnosticCode: null, payloadPreview: null },
        ],
      },
    });
  });

  describe('unsuppressEmailAction', () => {
    it('calls the suppressions delete endpoint with the email and revalidates', async () => {
      const result = await unsuppressEmailAction('a@b.co');

      expect(result).toEqual({ success: true, status: 'success' });
      expect(mocks.deleteSuppression).toHaveBeenCalledTimes(1);
      expect(mocks.deleteSuppression).toHaveBeenCalledWith('a@b.co');
      expect(mocks.revalidatePath).toHaveBeenCalledWith('/', 'layout');
    });

    it('returns error without revalidating when the API rejects', async () => {
      mocks.deleteSuppression.mockResolvedValue(apiError(403));

      const result = await unsuppressEmailAction('a@b.co');

      expect(result).toEqual({ success: false, status: 'error' });
      expect(mocks.revalidatePath).not.toHaveBeenCalled();
    });

    it('returns unauthorized without calling the API when there is no session', async () => {
      mocks.auth.mockResolvedValue(null);

      const result = await unsuppressEmailAction('a@b.co');

      expect(result).toEqual({ success: false, status: 'unauthorized' });
      expect(mocks.deleteSuppression).not.toHaveBeenCalled();
    });
  });

  describe('requeueNotificationAction', () => {
    it('calls the requeue endpoint with the notification id and revalidates', async () => {
      const result = await requeueNotificationAction('n-1');

      expect(result).toEqual({ success: true, status: 'success' });
      expect(mocks.postRequeue).toHaveBeenCalledTimes(1);
      expect(mocks.postRequeue).toHaveBeenCalledWith('n-1');
      expect(mocks.revalidatePath).toHaveBeenCalledWith('/', 'layout');
    });

    it('returns error without revalidating on a 409 (suppression active / not dead-lettered)', async () => {
      mocks.postRequeue.mockResolvedValue(apiError(409));

      const result = await requeueNotificationAction('n-1');

      expect(result).toEqual({ success: false, status: 'error' });
      expect(mocks.revalidatePath).not.toHaveBeenCalled();
    });

    it('returns unauthorized when there is no session', async () => {
      mocks.auth.mockResolvedValue(null);

      const result = await requeueNotificationAction('n-1');

      expect(result).toEqual({ success: false, status: 'unauthorized' });
      expect(mocks.postRequeue).not.toHaveBeenCalled();
    });
  });

  describe('getNotificationTimelineAction', () => {
    it('normalizes nullable DTO fields into the timeline shape', async () => {
      const result = await getNotificationTimelineAction('n-1');

      expect(result).toEqual({
        success: true,
        status: 'success',
        providerMessageId: 'ses-123',
        events: [
          {
            id: 'e-2',
            eventType: 'Delivery',
            occurredAt: '2026-08-19T10:02:00Z',
            recipientEmail: 'a@b.co',
            bounceType: null,
            diagnosticCode: null,
            payloadPreview: null,
          },
          {
            id: 'e-1',
            eventType: 'Send',
            occurredAt: '2026-08-19T10:01:00Z',
            recipientEmail: 'a@b.co',
            bounceType: null,
            diagnosticCode: null,
            payloadPreview: null,
          },
        ],
      });
      expect(mocks.getTimeline).toHaveBeenCalledWith('n-1');
    });

    it('does not revalidate (read-only drill-down)', async () => {
      await getNotificationTimelineAction('n-1');

      expect(mocks.revalidatePath).not.toHaveBeenCalled();
    });

    it('returns empty events on API failure', async () => {
      mocks.getTimeline.mockResolvedValue(apiError(404));

      const result = await getNotificationTimelineAction('missing');

      expect(result).toEqual({ success: false, status: 'error', providerMessageId: null, events: [] });
    });

    it('returns unauthorized when there is no session', async () => {
      mocks.auth.mockResolvedValue(null);

      const result = await getNotificationTimelineAction('n-1');

      expect(result).toEqual({ success: false, status: 'unauthorized', providerMessageId: null, events: [] });
    });
  });
});

import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(async () => 'token'),
  createServerClient: vi.fn((config: unknown) => config),
  approve: vi.fn(),
  reject: vi.fn(),
  revalidatePath: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth, getToken: mocks.getToken }));
vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    EconomyModule: class {
      postAdminEconomyPayoutRequestsApprove = mocks.approve;
      postAdminEconomyPayoutRequestsReject = mocks.reject;
    },
  },
}));

import { reviewPayoutRequestAction } from './admin-actions';

describe('Payout review server action', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'reviewer' }, tenantId: 'tenant' });
    mocks.approve.mockResolvedValue({ ok: true, data: {} });
    mocks.reject.mockResolvedValue({ ok: true, data: {} });
    delete process.env.API_URL;
    delete process.env.NEXT_PUBLIC_API_URL;
  });

  it('validates the request and immutable reason before authentication', async () => {
    await expect(reviewPayoutRequestAction(' ', 'approve', 'reason')).resolves.toMatchObject({ success: false });
    await expect(reviewPayoutRequestAction('request', 'approve', ' ')).resolves.toMatchObject({ success: false });
    expect(mocks.auth).not.toHaveBeenCalled();
  });

  it('fails closed for missing, function-shaped, invalid, and rejected auth sessions', async () => {
    for (const session of [null, () => undefined, { user: {} }]) {
      mocks.auth.mockResolvedValueOnce(session);
      await expect(reviewPayoutRequestAction('request', 'approve', 'reason')).resolves.toMatchObject({ success: false });
    }
    mocks.auth.mockRejectedValueOnce(new Error('auth down'));
    await expect(reviewPayoutRequestAction('request', 'approve', 'reason')).resolves.toMatchObject({ success: false });
  });

  it('records both dual-control review outcomes without accepting tenant authority', async () => {
    process.env.API_URL = 'https://api.internal';
    const approved = await reviewPayoutRequestAction('request', 'approve', ' reviewed ');
    const rejected = await reviewPayoutRequestAction('request', 'reject', ' denied ');

    expect(approved).toEqual({ success: true, message: expect.stringContaining('second independent') });
    expect(rejected).toEqual({ success: true, message: expect.stringContaining('No value was dispatched') });
    expect(mocks.approve).toHaveBeenCalledWith('request', { reason: 'reviewed' });
    expect(mocks.reject).toHaveBeenCalledWith('request', { reason: 'denied' });
    expect(mocks.revalidatePath).toHaveBeenCalledTimes(2);
    const config = mocks.createServerClient.mock.calls[0][0] as {
      auth: { getAccessToken: () => Promise<string> };
      baseUrl: string;
      tenant: { getTenantId: () => Promise<string | null> };
    };
    expect(config.baseUrl).toBe('https://api.internal');
    await expect(config.auth.getAccessToken()).resolves.toBe('token');
    await expect(config.tenant.getTenantId()).resolves.toBe('tenant');
  });

  it('uses public and local API fallbacks and a null tenant when absent', async () => {
    process.env.NEXT_PUBLIC_API_URL = 'https://api.public';
    mocks.auth.mockResolvedValueOnce({ user: { id: 'reviewer' }, tenantId: null });
    await reviewPayoutRequestAction('request', 'reject', 'reason');
    let config = mocks.createServerClient.mock.calls.at(-1)?.[0] as { baseUrl: string; tenant: { getTenantId: () => Promise<string | null> } };
    expect(config.baseUrl).toBe('https://api.public');
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    delete process.env.NEXT_PUBLIC_API_URL;
    await reviewPayoutRequestAction('request', 'reject', 'reason');
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    expect(config.baseUrl).toBe('http://localhost:8080');
  });

  it('preserves provider errors and supplies a safe fallback', async () => {
    mocks.approve.mockResolvedValueOnce({ ok: false, error: { message: 'provider denied' } });
    mocks.reject.mockResolvedValueOnce({ ok: false, error: { message: null } });
    await expect(reviewPayoutRequestAction('request', 'approve', 'reason')).resolves.toEqual({ success: false, message: 'provider denied' });
    await expect(reviewPayoutRequestAction('request', 'reject', 'reason')).resolves.toEqual({ success: false, message: 'The payout review decision was not accepted.' });
  });
});

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getAuthenticatedUserId: vi.fn(),
  getUserSettingsApiClient: vi.fn(),
  request: vi.fn(),
}));

vi.mock('@/lib/user-settings/api-client', () => ({
  getAuthenticatedUserId: mocks.getAuthenticatedUserId,
  getUserSettingsApiClient: mocks.getUserSettingsApiClient,
}));

import { recordConsentChangeAction } from './server';

const ok = (data: unknown = undefined) => ({ ok: true as const, data });
const fail = (status = 500, message = 'boom') => ({
  ok: false as const,
  error: { status, message },
});

describe('recordConsentChangeAction', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(console, 'error').mockImplementation(() => {});
    mocks.getAuthenticatedUserId.mockResolvedValue('user-1');
    mocks.getUserSettingsApiClient.mockReturnValue({ request: mocks.request });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('returns recorded=false without any request for anonymous users', async () => {
    mocks.getAuthenticatedUserId.mockResolvedValue(null);

    await expect(
      recordConsentChangeAction({ analytics: true, marketing: false, method: 'banner' }),
    ).resolves.toEqual({ recorded: false });
    expect(mocks.request).not.toHaveBeenCalled();
  });

  it('posts the consent decision to the bridge endpoint', async () => {
    mocks.request.mockResolvedValue(ok({ changedRecords: 1 }));

    await expect(
      recordConsentChangeAction({ analytics: true, marketing: false, method: 'settings' }),
    ).resolves.toEqual({ recorded: false });

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/api/compliance/consent/user-consent',
      body: { analyticsGranted: true, marketingGranted: false, method: 'settings' },
      requiresAuth: true,
    });
  });

  it('retries exactly once when the write fails, then gives up without throwing', async () => {
    mocks.request
      .mockResolvedValueOnce(fail())
      .mockResolvedValueOnce(fail());

    await expect(
      recordConsentChangeAction({ analytics: false, marketing: false, method: 'banner' }),
    ).resolves.toEqual({ recorded: false });
    expect(mocks.request).toHaveBeenCalledTimes(2);
    expect(console.error).toHaveBeenCalledTimes(2);
  });

  it('succeeds on the single retry', async () => {
    mocks.request
      .mockResolvedValueOnce(fail(503, 'unavailable'))
      .mockResolvedValueOnce(ok({ changedRecords: 2 }));

    await expect(
      recordConsentChangeAction({ analytics: true, marketing: true, method: 'banner' }),
    ).resolves.toEqual({ recorded: false });
    expect(mocks.request).toHaveBeenCalledTimes(2);
  });

  it('never throws when the client itself rejects', async () => {
    mocks.request.mockRejectedValue(new Error('network down'));

    await expect(
      recordConsentChangeAction({ analytics: true, marketing: true, method: 'banner' }),
    ).resolves.toEqual({ recorded: false });
    expect(mocks.request).toHaveBeenCalledTimes(2);
  });
});

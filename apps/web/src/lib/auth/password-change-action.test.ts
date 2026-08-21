import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  postAuthPasswordChange: vi.fn(),
  revalidatePath: vi.fn(),
}));

vi.mock('@/auth', () => ({ getToken: vi.fn().mockResolvedValue('token') }));

vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    AuthModule: class {
      postAuthPasswordChange = mocks.postAuthPasswordChange;
    },
  },
}));

const { changePasswordAction } = await import('./password-change-action');

const VALID_INPUT = {
  currentPassword: 'Admin123!',
  newPassword: 'NewPassw0rd!',
  confirmPassword: 'NewPassw0rd!',
  revokeOtherSessions: true,
};

function apiError(status: number, message: string, detail?: string) {
  return { ok: false as const, error: { name: 'ApiError' as const, status, code: 'VALIDATION_ERROR' as const, message, ...(detail !== undefined ? { detail } : {}) } };
}

describe('changePasswordAction', () => {
  beforeEach(() => {
    mocks.postAuthPasswordChange.mockReset();
    mocks.revalidatePath.mockReset();
  });

  it('returns success and revalidates the settings page on ok result', async () => {
    mocks.postAuthPasswordChange.mockResolvedValue({ ok: true, data: { success: true, message: 'Password changed', sessionsRevoked: 0 } });

    const result = await changePasswordAction(VALID_INPUT);

    expect(result).toEqual({ success: true, status: 'success' });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/workspace/settings/account');
  });

  it('sends an empty currentPassword as-is for the set-initial flow', async () => {
    mocks.postAuthPasswordChange.mockResolvedValue({ ok: true, data: { success: true } });

    await changePasswordAction({ ...VALID_INPUT, currentPassword: '' });

    expect(mocks.postAuthPasswordChange).toHaveBeenCalledWith({
      currentPassword: '',
      newPassword: 'NewPassw0rd!',
      confirmPassword: 'NewPassw0rd!',
      revokeOtherSessions: true,
    });
  });

  it('maps a 400 whose surfaced message contains "Current password" to wrongCurrent', async () => {
    // Actual transport today drops the PasswordChangeResult body message; this
    // pins the mapping for when a client surfaces it (detail or body message).
    mocks.postAuthPasswordChange.mockResolvedValue(
      apiError(400, 'Bad Request', 'Current password is incorrect'),
    );

    const result = await changePasswordAction(VALID_INPUT);

    expect(result).toEqual({ success: false, status: 'wrongCurrent', message: 'Current password is incorrect' });
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });

  it('maps a 400 with a policy-failing password to weakPassword even without a body message', async () => {
    // Verified mechanism: transform.ts reads only ProblemDetails title/detail,
    // so error.message is the statusText "Bad Request" for this endpoint.
    mocks.postAuthPasswordChange.mockResolvedValue(apiError(400, 'Bad Request'));

    const result = await changePasswordAction({ ...VALID_INPUT, newPassword: 'weak', confirmPassword: 'weak' });

    expect(result).toEqual({ success: false, status: 'weakPassword' });
  });

  it('maps a policy-passing 400 without a body message to wrongCurrent', async () => {
    mocks.postAuthPasswordChange.mockResolvedValue(apiError(400, 'Bad Request'));

    const result = await changePasswordAction(VALID_INPUT);

    expect(result).toEqual({ success: false, status: 'wrongCurrent' });
  });

  it('maps 401 to unauthorized', async () => {
    mocks.postAuthPasswordChange.mockResolvedValue(apiError(401, 'Unauthorized'));

    const result = await changePasswordAction(VALID_INPUT);

    expect(result).toEqual({ success: false, status: 'unauthorized' });
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });

  it('maps unexpected statuses to error', async () => {
    mocks.postAuthPasswordChange.mockResolvedValue(apiError(500, 'Internal Server Error'));

    const result = await changePasswordAction(VALID_INPUT);

    expect(result).toEqual({ success: false, status: 'error' });
  });
});

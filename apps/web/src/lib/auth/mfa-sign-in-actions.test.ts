import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  emailChallenge: vi.fn(),
  forwardedIpHeaders: vi.fn(),
}));

vi.mock('server-only', () => ({}));
vi.mock('@/auth', () => ({ authConfig: {} }));
vi.mock('@/lib/server/forwarded-headers', () => ({ forwardedIpHeaders: mocks.forwardedIpHeaders }));
vi.mock('@game-guild/client', () => ({
  createClient: vi.fn(() => ({})),
  createMfaEnrollmentCompleteAction: vi.fn(() => vi.fn()),
  GeneratedApi: {
    AuthMultiFactorModule: class {
      postAuthMfaEmailChallenge = mocks.emailChallenge;
    },
  },
}));

const { requestEmailMfaChallengeAction } = await import('./mfa-sign-in-actions');

describe('requestEmailMfaChallengeAction (issue #1298 J2)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.forwardedIpHeaders.mockResolvedValue({});
  });

  it('addresses the API by the subject of the pending challenge and returns the masked address', async () => {
    mocks.emailChallenge.mockResolvedValueOnce({
      ok: true,
      data: { emailMasked: 'j***@example.com', expiresInSeconds: 600, message: 'Verification code sent' },
    });

    const result = await requestEmailMfaChallengeAction('subject-1');

    expect(mocks.emailChallenge).toHaveBeenCalledWith({ userId: 'subject-1' });
    expect(result).toEqual({
      success: true,
      status: 'success',
      emailMasked: 'j***@example.com',
      expiresInSeconds: 600,
    });
  });

  it('carries the API reason when the code is throttled', async () => {
    mocks.emailChallenge.mockResolvedValueOnce({
      ok: false,
      error: { status: 400, code: 'UNKNOWN', message: 'A verification code was sent recently. Wait before requesting another.' },
    });

    const result = await requestEmailMfaChallengeAction('subject-1');

    expect(result).toEqual({
      success: false,
      status: 'error',
      message: 'A verification code was sent recently. Wait before requesting another.',
    });
  });

  it('never turns an unreachable API into a misleading reason', async () => {
    mocks.emailChallenge.mockResolvedValueOnce({
      ok: false,
      error: { status: 0, code: 'NETWORK_ERROR', message: 'Network Error' },
    });

    const result = await requestEmailMfaChallengeAction('subject-1');

    expect(result.success).toBe(false);
    expect(result.message).toBe('We could not send an email code.');
  });

  it('asks for nothing without a subject', async () => {
    const result = await requestEmailMfaChallengeAction('');

    expect(mocks.emailChallenge).not.toHaveBeenCalled();
    expect(result.success).toBe(false);
  });
});

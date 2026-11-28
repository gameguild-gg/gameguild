import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  request: vi.fn(),
  cont: vi.fn(),
  confirm: vi.fn(),
  cancel: vi.fn(),
  anonymousClient: vi.fn(() => ({ kind: 'anonymous' })),
  serverClient: vi.fn(() => ({ kind: 'server' })),
  getToken: vi.fn().mockResolvedValue('session-access-token'),
}));

vi.mock('server-only', () => ({}));
vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('@/lib/server/forwarded-headers', () => ({ forwardedIpHeaders: vi.fn().mockResolvedValue({}) }));
vi.mock('@game-guild/client', () => ({
  createClient: vi.fn(() => ({ kind: 'anonymous-client' })),
  createServerClient: mocks.serverClient,
  GeneratedApi: {
    AuthMultiFactorModule: class {
      postAuthMfaRecoveryRequest = mocks.request;
      postAuthMfaRecoveryContinue = mocks.cont;
      postAuthMfaRecoveryConfirm = mocks.confirm;
      postAuthMfaRecoveryCancel = mocks.cancel;

      constructor(public client: unknown) {}
    },
  },
}));

const {
  cancelMfaRecoveryAction,
  confirmMfaRecoveryAction,
  continueMfaRecoveryAction,
  requestMfaRecoveryAction,
} = await import('./mfa-recovery-actions');
const { createAnonymousApiClient, isUnreachable } = await import('./anonymous-api');

const unreachable = { name: 'ApiError', message: 'Network Error', status: 0, code: 'NETWORK_ERROR' };
const apiError = (status: number, extra: Record<string, unknown> = {}) => ({
  name: 'ApiError',
  message: 'denied',
  status,
  code: 'UNKNOWN',
  ...extra,
});

describe('lost-second-factor recovery actions (issue #1298 J2, part C)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('session-access-token');
  });

  it('starts a recovery anonymously and reports only whether the request went out', async () => {
    mocks.request.mockResolvedValueOnce({ ok: true, data: { success: true, message: 'If an account matches' } });
    await expect(requestMfaRecoveryAction('a@example.com')).resolves.toEqual({ status: 'sent' });
    expect(mocks.request).toHaveBeenCalledWith({ email: 'a@example.com' });

    mocks.request.mockResolvedValueOnce({ ok: false, error: apiError(400) });
    await expect(requestMfaRecoveryAction('a@example.com')).resolves.toEqual({ status: 'failed' });
  });

  it('tells a sent confirmation, a cooling-off wait, a denial and a failure apart', async () => {
    mocks.cont.mockResolvedValueOnce({ ok: true, data: { success: true, message: null } });
    await expect(continueMfaRecoveryAction('request-token')).resolves.toEqual({ status: 'sent' });

    // The API answers a valid request still cooling off as HTTP 425; the client keeps its body.
    mocks.cont.mockResolvedValueOnce({ ok: false, error: apiError(425, { metadata: { body: { retryAfterSeconds: 4200 } } }) });
    await expect(continueMfaRecoveryAction('request-token')).resolves.toEqual({
      status: 'cooling-off',
      retryAfterSeconds: 4200,
    });

    mocks.cont.mockResolvedValueOnce({ ok: false, error: apiError(425, { metadata: { body: {} } }) });
    await expect(continueMfaRecoveryAction('request-token')).resolves.toEqual({
      status: 'cooling-off',
      retryAfterSeconds: 0,
    });

    mocks.cont.mockResolvedValueOnce({ ok: false, error: apiError(400) });
    await expect(continueMfaRecoveryAction('request-token')).resolves.toEqual({ status: 'denied' });

    mocks.cont.mockResolvedValueOnce({ ok: false, error: unreachable });
    await expect(continueMfaRecoveryAction('request-token')).resolves.toEqual({ status: 'failed' });
    expect(mocks.cont).toHaveBeenCalledWith({ requestToken: 'request-token' });
  });

  it('confirms only on the API word, and never turns an unreachable API into a denial', async () => {
    mocks.confirm.mockResolvedValueOnce({ ok: true, data: { success: true, message: null } });
    await expect(confirmMfaRecoveryAction('confirmation-token')).resolves.toEqual({ status: 'confirmed' });

    mocks.confirm.mockResolvedValueOnce({ ok: true, data: { success: false, message: 'denied' } });
    await expect(confirmMfaRecoveryAction('confirmation-token')).resolves.toEqual({ status: 'denied' });

    mocks.confirm.mockResolvedValueOnce({ ok: false, error: apiError(400) });
    await expect(confirmMfaRecoveryAction('confirmation-token')).resolves.toEqual({ status: 'denied' });

    mocks.confirm.mockResolvedValueOnce({ ok: false, error: { ...unreachable, status: 503, code: 'SERVER_ERROR' } });
    await expect(confirmMfaRecoveryAction('confirmation-token')).resolves.toEqual({ status: 'failed' });
    expect(mocks.confirm).toHaveBeenCalledWith({ confirmationToken: 'confirmation-token' });
  });

  it('cancels with the signed-in owner session, never anonymously', async () => {
    mocks.cancel.mockResolvedValueOnce({ ok: true, data: { cancelled: true, success: true, message: null } });
    await expect(cancelMfaRecoveryAction()).resolves.toEqual({ status: 'cancelled' });
    expect(mocks.serverClient).toHaveBeenCalledWith(
      expect.objectContaining({ auth: expect.objectContaining({ getAccessToken: expect.any(Function) }) }),
    );
    expect(mocks.request).not.toHaveBeenCalled();

    mocks.cancel.mockResolvedValueOnce({ ok: true, data: { cancelled: false, success: false, message: null } });
    await expect(cancelMfaRecoveryAction()).resolves.toEqual({ status: 'denied' });

    mocks.cancel.mockResolvedValueOnce({ ok: false, error: apiError(401) });
    await expect(cancelMfaRecoveryAction()).resolves.toEqual({ status: 'denied' });

    mocks.cancel.mockResolvedValueOnce({ ok: false, error: { ...unreachable, code: 'PARSE_ERROR' } });
    await expect(cancelMfaRecoveryAction()).resolves.toEqual({ status: 'failed' });
  });
});

describe('anonymous API client', () => {
  it('forwards the trusted client address on every request', async () => {
    const { forwardedIpHeaders } = await import('@/lib/server/forwarded-headers');
    vi.mocked(forwardedIpHeaders).mockResolvedValueOnce({ 'X-Forwarded-For': '203.0.113.7' });
    const client = await createAnonymousApiClient();

    const { createClient } = await import('@game-guild/client');
    const options = vi.mocked(createClient).mock.calls[0][0];
    expect(client).toEqual({ kind: 'anonymous-client' });
    await expect(options.interceptors?.[0].onRequest({ headers: { accept: 'application/json' } } as never)).resolves.toEqual({
      headers: { accept: 'application/json', 'X-Forwarded-For': '203.0.113.7' },
    });
  });

  it('calls only an unreachable API, or an unreadable answer, unreachable', () => {
    expect(isUnreachable(unreachable)).toBe(true);
    expect(isUnreachable({ status: 503, code: 'SERVER_ERROR' })).toBe(true);
    expect(isUnreachable({ status: 200, code: 'PARSE_ERROR' })).toBe(true);
    expect(isUnreachable({ status: 400, code: 'UNKNOWN' })).toBe(false);
    expect(isUnreachable({ status: 425, code: 'UNKNOWN' })).toBe(false);
  });
});

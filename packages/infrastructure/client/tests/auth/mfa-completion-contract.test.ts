import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { verifyMfa, MfaVerificationError } from '../../src/runtime/auth/extended-operations.js';
import { AuthModule } from '../../src/generated/modules/auth.gen.js';

const API_URL = 'https://api.gameguild.gg';
const MFA_TOKEN = 'x'.repeat(43);
const USER_ID = '11111111-1111-4111-8111-111111111111';
const TENANT_ID = '22222222-2222-4222-8222-222222222222';
const SESSION_ID = '33333333-3333-4333-8333-333333333333';
const SUCCESS = {
  success: true,
  requiresMfa: false,
  requiresStepUp: false,
  accessToken: 'verified-access',
  refreshToken: 'verified-refresh',
  userId: USER_ID,
  email: 'actor@example.com',
  sessionId: SESSION_ID,
  tenantId: TENANT_ID,
  expiresIn: 3600,
  accessTokenExpiresAt: '2026-11-04T08:00:00+00:00',
  refreshTokenExpiresAt: '2026-11-11T08:00:00+00:00',
  user: { id: USER_ID, email: 'actor@example.com', username: 'actor' },
};
const fetchMock = vi.fn<typeof fetch>();

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal('fetch', fetchMock);
});

afterEach(() => {
  vi.useRealTimers();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe('limited-challenge MFA completion through the public SDK helper', () => {
  it.each([
    { method: 'totp' as const, wireMethod: 'Totp', code: '123456' },
    { method: 'backup_code' as const, wireMethod: 'BackupCode', code: 'backup-code' },
  ])('completes $method through the generated sign-in operation without sending an ordinary bearer', async ({ method, wireMethod, code }) => {
    const generated = vi.spyOn(AuthModule.prototype, 'postAuthMfaSignInComplete');
    fetchMock.mockImplementation((url, options) => {
      const path = url instanceof Request ? url.url : url.toString();
      if (path !== `${API_URL}/v1/auth/mfa/sign-in/complete`) return Promise.resolve(Response.json({}, { status: 404 }));
      if (new Headers(options?.headers).has('Authorization')) return Promise.resolve(Response.json({}, { status: 403 }));
      return Promise.resolve(Response.json(SUCCESS));
    });
    // Extra caller assertions must not be forwarded: the challenge owns the actor/tenant.
    const input = { mfaToken: MFA_TOKEN, method, code, tenantId: 'spoofed-tenant', userId: 'spoofed-user', ipAddress: '198.51.100.7' };
    const result = await verifyMfa(API_URL, input, 'ordinary-bearer-must-not-be-sent');
    expect(generated).toHaveBeenCalledOnce();
    expect(fetchMock).toHaveBeenCalledOnce();
    const [, options] = fetchMock.mock.calls[0];
    expect(options?.method).toBe('POST');
    expect(options?.redirect).toBe('error');
    expect(JSON.parse(options?.body as string)).toEqual({ mfaToken: MFA_TOKEN, method: wireMethod, code });
    expect(new Headers(options?.headers).get('Content-Type')).toBe('application/json');
    expect(result.tokens).toMatchObject({ accessToken: SUCCESS.accessToken, refreshToken: SUCCESS.refreshToken, expiresIn: 3600 });
    expect(result.user).toMatchObject({ id: USER_ID, email: SUCCESS.email, name: 'actor' });
    expect(result.sessionId).toBe(SESSION_ID);
    expect(result.tenantId).toBe(TENANT_ID);
  });

  it('accepts an opaque challenge in the deprecated alias without converting session IDs', async () => {
    fetchMock.mockResolvedValueOnce(Response.json(SUCCESS));
    await expect(verifyMfa(API_URL, { mfaSessionId: MFA_TOKEN, code: '123456', method: 'totp' })).resolves.toHaveProperty('user.id', USER_ID);
    expect(JSON.parse(fetchMock.mock.calls[0][1]?.body as string)).toEqual({ mfaToken: MFA_TOKEN, code: '123456', method: 'Totp' });
  });

  it('uses the explicit challenge instead of a legacy session ID and forwards only the optional device binding', async () => {
    fetchMock.mockResolvedValueOnce(Response.json(SUCCESS));
    await verifyMfa(API_URL, { mfaToken: MFA_TOKEN, mfaSessionId: SESSION_ID, method: 'totp', code: '123456', deviceFingerprint: 'bound-device' });
    expect(JSON.parse(fetchMock.mock.calls[0][1]?.body as string)).toEqual({
      mfaToken: MFA_TOKEN,
      method: 'Totp',
      code: '123456',
      deviceFingerprint: 'bound-device',
    });
  });

  it('retains validated identity fields, legacy profile/claims and one-time enrollment codes', async () => {
    fetchMock.mockResolvedValueOnce(
      Response.json({
        ...SUCCESS,
        roles: ['TenantAdmin'],
        permissions: ['pages:read'],
        user: { ...SUCCESS.user, displayName: 'Display Actor', profilePictureUrl: 'https://cdn.gameguild.gg/profile.png' },
        mfaEnrollmentBackupCodes: ['once-only-code'],
      }),
    );
    const result = await verifyMfa(API_URL, { mfaToken: MFA_TOKEN, method: 'totp', code: '123456' });
    expect(result.user).toMatchObject({ id: USER_ID, name: 'Display Actor', roles: ['TenantAdmin'], permissions: ['pages:read'] });
    expect(result.user.image).toBe('https://cdn.gameguild.gg/profile.png');
    expect(result.mfaEnrollmentBackupCodes).toEqual(['once-only-code']);
    expect(result.tokens).not.toHaveProperty('mfaEnrollmentBackupCodes');
  });

  it('accepts the validated nested identity when a legacy backend omits the flat user ID', async () => {
    const body = { ...SUCCESS, userId: undefined };
    fetchMock.mockResolvedValueOnce(Response.json(body));
    await expect(verifyMfa(API_URL, { mfaToken: MFA_TOKEN, method: 'totp', code: '123456' })).resolves.toHaveProperty('user.id', USER_ID);
  });

  it.each([
    { mfaSessionId: SESSION_ID, method: 'totp', code: '123456' },
    { mfaToken: '', method: 'totp', code: '123456' },
    { mfaToken: 'x'.repeat(42), method: 'totp', code: '123456' },
    { mfaToken: '/'.repeat(43), method: 'totp', code: '123456' },
    { mfaToken: MFA_TOKEN, method: 'sms', code: '123456' },
    { mfaToken: MFA_TOKEN, method: 'totp', code: '' },
    { mfaToken: MFA_TOKEN, method: 'totp', code: '   ' },
    { mfaToken: MFA_TOKEN, method: 'totp', code: 'x'.repeat(65) },
    { mfaToken: MFA_TOKEN, method: 'totp', code: '123456', deviceFingerprint: 'x'.repeat(65) },
  ])('rejects invalid or unsupported proofs before network access: %j', async (input) => {
    // Deliberately exercise runtime validation of untyped callers too.
    await expect(verifyMfa(API_URL, input as Parameters<typeof verifyMfa>[1])).rejects.toThrow(MfaVerificationError);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it.each([
    { ...SUCCESS, success: false },
    { ...SUCCESS, requiresMfa: true, mfaToken: MFA_TOKEN },
    { ...SUCCESS, requiresStepUp: true },
    { ...SUCCESS, accessToken: null },
    { ...SUCCESS, accessToken: ' ' },
    { ...SUCCESS, refreshToken: null },
    { ...SUCCESS, refreshToken: ' ' },
    { ...SUCCESS, userId: undefined, user: {} },
    { ...SUCCESS, userId: 'not-a-guid' },
    { ...SUCCESS, user: { ...SUCCESS.user, id: 'not-a-guid' } },
    { ...SUCCESS, sessionId: 'not-a-guid' },
    { ...SUCCESS, tenantId: 'not-a-guid' },
    { ...SUCCESS, expiresIn: '3600' },
    { ...SUCCESS, accessTokenExpiresAt: '2026-11-04T08:00:00' },
    { ...SUCCESS, mfaEnrollmentBackupCodes: ['one', 42] },
    {},
    null,
  ])('does not return ordinary credentials from incomplete, pending or invalid successful replies: %j', async (body) => {
    fetchMock.mockResolvedValueOnce(Response.json(body));
    await expect(verifyMfa(API_URL, { mfaToken: MFA_TOKEN, method: 'totp', code: '123456' })).rejects.toThrow(MfaVerificationError);
  });

  it.each([400, 401, 403, 429, 500, 503])('rejects HTTP %i without falling back to settings verification or refreshing', async (status) => {
    fetchMock.mockResolvedValueOnce(Response.json({ title: 'Unauthorized', detail: 'Additional verification failed' }, { status }));
    await expect(verifyMfa(API_URL, { mfaToken: MFA_TOKEN, method: 'totp', code: '123456' })).rejects.toThrow(MfaVerificationError);
    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_URL}/v1/auth/mfa/sign-in/complete`);
  });

  it('wraps network failures without leaking challenge/proof data through diagnostics or errors', async () => {
    const log = vi.spyOn(console, 'log').mockImplementation(() => undefined);
    const group = vi.spyOn(console, 'groupCollapsed').mockImplementation(() => undefined);
    fetchMock.mockRejectedValueOnce(new Error(`Network rejected ${MFA_TOKEN}`));
    const error: unknown = await verifyMfa(API_URL, { mfaToken: MFA_TOKEN, method: 'totp', code: '123456' }).catch((error: unknown) => error);
    expect(error).toBeInstanceOf(MfaVerificationError);
    if (!(error instanceof Error)) throw new Error('Expected an authentication error');
    expect(error.message).not.toContain(MFA_TOKEN);
    expect(log).not.toHaveBeenCalled();
    expect(group).not.toHaveBeenCalled();
  });

  it('rejects invalid JSON with the public error', async () => {
    fetchMock.mockResolvedValueOnce(new Response('{', { headers: { 'Content-Type': 'application/json' } }));
    await expect(verifyMfa(API_URL, { mfaToken: MFA_TOKEN, method: 'totp', code: '123456' })).rejects.toThrow(MfaVerificationError);
  });

  it('bounds stalled network access even when the fetch adapter ignores abort', async () => {
    vi.useFakeTimers();
    fetchMock.mockImplementation(() => new Promise<Response>(() => undefined));
    const settled = expect(verifyMfa(API_URL, { mfaToken: MFA_TOKEN, method: 'totp', code: '123456' })).rejects.toThrow(MfaVerificationError);
    await vi.advanceTimersByTimeAsync(5_000);
    await settled;
    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock.mock.calls[0][1]?.signal?.aborted).toBe(true);
  });
});

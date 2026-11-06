import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { changePassword, confirmPasswordReset, requestPasswordReset, PasswordResetError } from '../../src/runtime/auth/extended-operations.js';
import { AuthModule } from '../../src/generated/modules/auth.gen.js';

const API_URL = 'https://api.gameguild.gg';
const PASSWORD = 'Synthetic-password-123!';
const ACCESS_TOKEN = 'actor-access-token';
const TENANT_ID = '22222222-2222-4222-8222-222222222222';
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

const mutations = [
  {
    name: 'reset',
    path: '/v1/auth/password:reset',
    generated: 'postAuthPasswordReset' as const,
    invoke: () => confirmPasswordReset(API_URL, { token: 'reset-proof', newPassword: PASSWORD }),
    expected: { token: 'reset-proof', newPassword: PASSWORD, confirmPassword: PASSWORD },
  },
  {
    name: 'change',
    path: '/v1/auth/password:change',
    generated: 'postAuthPasswordChange' as const,
    invoke: () => changePassword(API_URL, { currentPassword: 'old-password', newPassword: PASSWORD }, ACCESS_TOKEN),
    expected: { currentPassword: 'old-password', newPassword: PASSWORD, confirmPassword: PASSWORD },
  },
];

describe('password contracts through the public SDK helpers', () => {
  for (const operation of mutations) {
    it(`${operation.name} sends the mandatory confirmation through the generated operation for existing callers`, async () => {
      const generated = vi.spyOn(AuthModule.prototype, operation.generated);
      fetchMock.mockImplementation((_url, options) => {
        const input: unknown = JSON.parse(options?.body as string);
        if (!input || typeof input !== 'object' || !('confirmPassword' in input) || input.confirmPassword !== PASSWORD) {
          return Promise.resolve(Response.json({ message: 'Password confirmation is required' }, { status: 400 }));
        }
        return Promise.resolve(Response.json({ success: true }));
      });
      await expect(operation.invoke()).resolves.toBeUndefined();
      expect(generated).toHaveBeenCalledOnce();
      expect(fetchMock).toHaveBeenCalledOnce();
      const [url, options] = fetchMock.mock.calls[0];
      expect(url).toBe(`${API_URL}${operation.path}`);
      expect(options?.method).toBe('POST');
      expect(options?.redirect).toBe('error');
      expect(JSON.parse(options?.body as string)).toEqual(operation.expected);
      const headers = new Headers(options?.headers);
      expect(headers.get('Content-Type')).toBe('application/json');
      expect(headers.get('Authorization')).toBe(operation.name === 'change' ? `Bearer ${ACCESS_TOKEN}` : null);
    });

    it.each([400, 401, 403, 429, 500])(`${operation.name} rejects HTTP %i without retrying or rotating credentials`, async (status) => {
      fetchMock.mockResolvedValueOnce(Response.json({ message: 'Password operation rejected' }, { status }));
      await expect(operation.invoke()).rejects.toThrow(PasswordResetError);
      expect(fetchMock).toHaveBeenCalledOnce();
    });

    it.each([{ body: {} }, { body: null }, { body: { success: false } }, { body: { success: 'true' } }, { body: { success: true, message: 42 } }])(
      `${operation.name} cannot report completion from an invalid or unsuccessful HTTP 200: %j`,
      async ({ body }) => {
        fetchMock.mockResolvedValueOnce(Response.json(body));
        await expect(operation.invoke()).rejects.toThrow(PasswordResetError);
      },
    );

    it(`${operation.name} rejects an empty successful reply instead of assuming the password changed`, async () => {
      fetchMock.mockResolvedValueOnce(new Response(null, { status: 204 }));
      await expect(operation.invoke()).rejects.toThrow(PasswordResetError);
    });
  }

  it('reset forwards explicit confirmation and tenant context but never caller identity or IP', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({ success: true }));
    const input = { token: 'reset-proof', newPassword: PASSWORD, confirmPassword: PASSWORD, tenantId: TENANT_ID, userId: 'spoofed', ipAddress: '198.51.100.8' };
    await confirmPasswordReset(API_URL, input);
    expect(JSON.parse(fetchMock.mock.calls[0][1]?.body as string)).toEqual({
      token: 'reset-proof',
      newPassword: PASSWORD,
      confirmPassword: PASSWORD,
      tenantId: TENANT_ID,
    });
  });

  it('change forwards explicit session policy without accepting a caller actor, tenant or session ID', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({ success: true, sessionsRevoked: 0 }));
    const input = {
      currentPassword: 'old',
      newPassword: PASSWORD,
      confirmPassword: PASSWORD,
      revokeOtherSessions: false,
      userId: 'spoofed',
      tenantId: 'spoofed',
      currentSessionId: 'spoofed',
      accessToken: 'spoofed',
    };
    await changePassword(API_URL, input, ACCESS_TOKEN);
    expect(JSON.parse(fetchMock.mock.calls[0][1]?.body as string)).toEqual({
      currentPassword: 'old',
      newPassword: PASSWORD,
      confirmPassword: PASSWORD,
      revokeOtherSessions: false,
    });
    expect(new Headers(fetchMock.mock.calls[0][1]?.headers).get('Authorization')).toBe(`Bearer ${ACCESS_TOKEN}`);
  });

  it('preserves empty current-password support for an authenticated OAuth-only account', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({ success: true }));
    await changePassword(API_URL, { currentPassword: '', newPassword: PASSWORD }, ACCESS_TOKEN);
    expect(JSON.parse(fetchMock.mock.calls[0][1]?.body as string)).toEqual({ currentPassword: '', newPassword: PASSWORD, confirmPassword: PASSWORD });
  });

  it('rejects mismatched confirmations locally for both mutations', async () => {
    await expect(confirmPasswordReset(API_URL, { token: 'proof', newPassword: PASSWORD, confirmPassword: 'different' })).rejects.toThrow(PasswordResetError);
    await expect(changePassword(API_URL, { currentPassword: 'old', newPassword: PASSWORD, confirmPassword: 'different' }, ACCESS_TOKEN)).rejects.toThrow(
      PasswordResetError,
    );
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it.each([
    { token: '', newPassword: PASSWORD },
    { token: 'proof', newPassword: 'short' },
    { token: 'proof', newPassword: PASSWORD, tenantId: 'invalid-guid' },
  ])('validates reset input before any request: %j', async (input) => {
    await expect(confirmPasswordReset(API_URL, input)).rejects.toThrow(PasswordResetError);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it.each(['', '   '])('change refuses an empty actor bearer: %j', async (token) => {
    await expect(changePassword(API_URL, { currentPassword: 'old', newPassword: PASSWORD }, token)).rejects.toThrow(PasswordResetError);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('change validates new password and optional revocation policy before any request', async () => {
    await expect(changePassword(API_URL, { currentPassword: 'old', newPassword: 'short' }, ACCESS_TOKEN)).rejects.toThrow(PasswordResetError);
    const input = { currentPassword: 'old', newPassword: PASSWORD, revokeOtherSessions: 'false' };
    await expect(changePassword(API_URL, input as unknown as Parameters<typeof changePassword>[1], ACCESS_TOKEN)).rejects.toThrow(PasswordResetError);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('reset rejects a malformed result identity and change rejects an invalid revocation count', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({ success: true, userId: 'invalid-guid' }));
    await expect(confirmPasswordReset(API_URL, { token: 'proof', newPassword: PASSWORD })).rejects.toThrow(PasswordResetError);
    fetchMock.mockResolvedValueOnce(Response.json({ success: true, sessionsRevoked: 'all' }));
    await expect(changePassword(API_URL, { currentPassword: 'old', newPassword: PASSWORD }, ACCESS_TOKEN)).rejects.toThrow(PasswordResetError);
  });

  it('reset requests use the generated anonymous operation with only email and optional tenant', async () => {
    const generated = vi.spyOn(AuthModule.prototype, 'postAuthPasswordResetRequest');
    fetchMock.mockResolvedValueOnce(Response.json({ success: true, message: 'Neutral acknowledgement', expiresInMinutes: 60 }));
    const input = { email: 'actor@example.com', tenantId: TENANT_ID, userId: 'spoofed', ipAddress: '198.51.100.8' };
    await expect(requestPasswordReset(API_URL, input)).resolves.toBeUndefined();
    expect(generated).toHaveBeenCalledOnce();
    const [url, options] = fetchMock.mock.calls[0];
    expect(url).toBe(`${API_URL}/v1/auth/password:reset-request`);
    expect(options?.method).toBe('POST');
    expect(options?.redirect).toBe('error');
    expect(new Headers(options?.headers).has('Authorization')).toBe(false);
    expect(JSON.parse(options?.body as string)).toEqual({ email: 'actor@example.com', tenantId: TENANT_ID });
  });

  it.each([200, 400, 401, 403, 404, 429, 500])('reset request HTTP %i returns the same acknowledgement without exposing account state', async (status) => {
    fetchMock.mockResolvedValueOnce(Response.json({ success: false, message: 'This must never identify an account' }, { status }));
    await expect(requestPasswordReset(API_URL, { email: 'actor@example.com' })).resolves.toBeUndefined();
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it.each([
    { name: 'invalid schema', response: Response.json({ message: 42 }) },
    { name: 'invalid JSON', response: new Response('{', { headers: { 'Content-Type': 'application/json' } }) },
    { name: 'HTML proxy response', response: new Response('<html>Proxy error</html>') },
  ])('reset request $name retains the neutral acknowledgement', async ({ response }) => {
    fetchMock.mockResolvedValueOnce(response);
    await expect(requestPasswordReset(API_URL, { email: 'actor@example.com' })).resolves.toBeUndefined();
  });

  it.each([{ email: 'invalid' }, { email: 'actor@example.com', tenantId: 'invalid-guid' }])(
    'reset request rejects locally invalid syntax: %j',
    async (input) => {
      await expect(requestPasswordReset(API_URL, input)).rejects.toThrow(PasswordResetError);
      expect(fetchMock).not.toHaveBeenCalled();
    },
  );

  for (const operation of [...mutations, { name: 'request', invoke: () => requestPasswordReset(API_URL, { email: 'actor@example.com' }) }]) {
    it(`${operation.name} wraps network failures without logging or echoing credentials`, async () => {
      const log = vi.spyOn(console, 'log').mockImplementation(() => undefined);
      const group = vi.spyOn(console, 'groupCollapsed').mockImplementation(() => undefined);
      fetchMock.mockRejectedValueOnce(new Error(`Failed with ${PASSWORD} ${ACCESS_TOKEN}`));
      const error: unknown = await operation.invoke().catch((error: unknown) => error);
      expect(error).toBeInstanceOf(PasswordResetError);
      if (!(error instanceof Error)) throw new Error('Expected a password error');
      expect(error.message).not.toContain(PASSWORD);
      expect(error.message).not.toContain(ACCESS_TOKEN);
      expect(log).not.toHaveBeenCalled();
      expect(group).not.toHaveBeenCalled();
    });

    it(`${operation.name} bounds a stalled fetch even if the adapter ignores abort`, async () => {
      vi.useFakeTimers();
      fetchMock.mockImplementation(() => new Promise<Response>(() => undefined));
      const settled = expect(operation.invoke()).rejects.toThrow(PasswordResetError);
      await vi.advanceTimersByTimeAsync(5_000);
      await settled;
      expect(fetchMock).toHaveBeenCalledOnce();
      expect(fetchMock.mock.calls[0][1]?.signal?.aborted).toBe(true);
    });
  }
});

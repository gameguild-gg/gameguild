import { afterEach, describe, expect, it, vi } from 'vitest';
import { readFileSync } from 'node:fs';
import { CredentialsProvider } from '../../../src/runtime/auth/providers/credentials.js';
import { AccountLockedError, AuthServiceUnavailableError, CredentialsSignInError, MfaRequiredError } from '../../../src/runtime/auth/errors.js';

const userId = '11111111-1111-4111-8111-111111111111';
const tenantId = '22222222-2222-4222-8222-222222222222';
const credentials = { email: 'member@example.com', password: 'password123' };
const success = { accessToken: 'access-token', refreshToken: 'refresh-token', userId, email: credentials.email };

function respond(body: unknown, status = 200) {
  const fetcher = vi.fn<typeof fetch>().mockResolvedValue(Response.json(body, { status }));
  vi.stubGlobal('fetch', fetcher);
  return fetcher;
}

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
  vi.restoreAllMocks();
  vi.useRealTimers();
});

describe('credentials provider generated API contract', () => {
  it.each([
    ['email format', { ...credentials, email: 'not-an-email' }],
    ['email type', { ...credentials, email: 123 }],
    ['password type', { ...credentials, password: { value: 'secret' } }],
    ['tenant identifier', { ...credentials, tenantId: 'not-a-guid' }],
  ])('rejects invalid %s before sending credentials', async (_name, input) => {
    const fetcher = respond(success);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(input, undefined)).rejects.toBeInstanceOf(CredentialsSignInError);
    expect(fetcher).not.toHaveBeenCalled();
  });

  it.each([
    ['expiry type', { expiresIn: '3600' }],
    ['expiry timestamp', { accessTokenExpiresAt: 'tomorrow' }],
    ['refresh-token type', { refreshToken: 42 }],
    ['user identifier', { userId: 'not-a-guid' }],
    ['MFA flag type', { requiresMfa: 'false' }],
    ['nested user identifier', { user: { id: 'not-a-guid' } }],
    ['recovery-code type', { mfaEnrollmentBackupCodes: [123] }],
  ])('rejects a successful HTTP response with invalid %s', async (_name, invalid) => {
    respond({ ...success, ...invalid });
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toBeInstanceOf(AuthServiceUnavailableError);
  });

  it('validates the MFA completion response against the same sign-in contract', async () => {
    respond({ ...success, expiresIn: '3600' });
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize({ mfaToken: 'A'.repeat(43), method: 'Totp', code: '123456' }, undefined)).rejects.toBeInstanceOf(
      AuthServiceUnavailableError,
    );
  });

  it('sends CAPTCHA, tenant and persistence fields without credentials in the URL', async () => {
    const fetcher = respond(success);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    const result = await provider.authorize({ ...credentials, tenantId, rememberMe: true, turnstileToken: 'captcha-proof' }, undefined);

    expect(result?.tokens.accessToken).toBe(success.accessToken);
    expect(result?.rememberMe).toBe(true);
    expect(fetcher).toHaveBeenCalledTimes(1);
    expect(fetcher.mock.calls[0][0]).toBe('https://api.example.com/v1/auth/sign-in');
    expect(JSON.parse(fetcher.mock.calls[0][1]?.body as string)).toEqual({
      ...credentials,
      tenantId,
      rememberMe: true,
      turnstileToken: 'captcha-proof',
    });
  });

  it('keeps the custom sign-in path supported with the sign-in contract', async () => {
    const fetcher = respond(success);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com', signInPath: '/custom/sign-in' });

    await expect(provider.authorize(credentials, undefined)).resolves.toMatchObject({ tokens: { accessToken: success.accessToken } });
    expect(fetcher.mock.calls[0][0]).toBe('https://api.example.com/custom/sign-in');
  });

  it('retains the MFA challenge instead of issuing a session', async () => {
    respond({ requiresMfa: true, mfaToken: 'A'.repeat(43), availableMethods: ['Totp'] });
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toBeInstanceOf(MfaRequiredError);
  });

  it('accepts the nullable risk level serialized by the backend on an MFA challenge', async () => {
    respond({ requiresMfa: true, mfaToken: 'A'.repeat(43), availableMethods: ['Totp'], riskLevel: null });
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toBeInstanceOf(MfaRequiredError);
  });

  it('accepts the full native pending-MFA response without creating a session', async () => {
    // Captured from SignInMfaPendingResponse serialization with a synthetic subject.
    const wire: unknown = JSON.parse(readFileSync(new URL('./fixtures/pending-mfa-response.json', import.meta.url), 'utf8'));
    respond(wire);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toBeInstanceOf(MfaRequiredError);
  });

  it('accepts the native enrollment offer with its DateTimeOffset expiry', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-04T03:00:00Z'));
    const wire: unknown = JSON.parse(readFileSync(new URL('./fixtures/pending-mfa-enrollment-response.json', import.meta.url), 'utf8'));
    respond(wire);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toMatchObject({
      name: 'MfaRequiredError',
      mfaEnrollment: { secretKey: 'SYNTHETIC-SETUP', expiresAt: '2026-11-04T09:00:00-03:00' },
    });
  });

  it('rejects an expiry timestamp without a timezone', async () => {
    respond({ ...success, accessTokenExpiresAt: '2026-11-04T03:00:00' });
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toBeInstanceOf(AuthServiceUnavailableError);
  });

  it.each(['+00:00', '-03:00'])('accepts a valid date-time offset %s', async (offset) => {
    respond({ ...success, accessTokenExpiresAt: `2026-11-04T12:00:00${offset}` });
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).resolves.toMatchObject({
      tokens: { accessTokenExpiresAt: `2026-11-04T12:00:00${offset}` },
    });
  });

  it('retains the account-lock error', async () => {
    respond({ detail: 'Account locked temporarily' }, 423);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toThrow(AccountLockedError);
  });

  it.each([401, 423])('preserves the API message on HTTP %s', async (status) => {
    respond({ message: 'Account verification required' }, status);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toThrow('Account verification required');
  });

  it('keeps standard ProblemDetails ahead of the legacy message', async () => {
    respond({ title: 'Reviewed authentication error', message: 'Legacy fallback' }, 401);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toThrow('Reviewed authentication error');
  });

  it('does not expose a non-string legacy error message', async () => {
    respond({ message: { private: 'must-not-be-displayed' } }, 401);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toThrow('Unknown error');
  });

  it('retains the distinction between service failure and invalid credentials', async () => {
    respond({ detail: 'Service unavailable' }, 503);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await expect(provider.authorize(credentials, undefined)).rejects.toBeInstanceOf(AuthServiceUnavailableError);
  });

  it('keeps credential transport diagnostics disabled in development', async () => {
    vi.stubEnv('NODE_ENV', 'development');
    const logs = (['log', 'info', 'warn', 'error', 'debug', 'group', 'groupCollapsed', 'groupEnd'] as const).map((method) =>
      vi.spyOn(console, method).mockImplementation(() => {}),
    );
    respond(success);
    const provider = CredentialsProvider({ apiUrl: 'https://api.example.com' });

    await provider.authorize(credentials, undefined);

    for (const log of logs) expect(log).not.toHaveBeenCalled();
  });
});

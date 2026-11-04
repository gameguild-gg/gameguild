/**
 * Credentials Provider Tests
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { CredentialsProvider } from '../../../src/runtime/auth/providers/credentials.js';
import { CredentialsSignInError, AccountLockedError, AuthServiceUnavailableError, MfaRequiredError } from '../../../src/runtime/auth/errors.js';

function makeUnsignedJwt(payload: Record<string, unknown>): string {
  const encode = (value: unknown) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode(payload)}.`;
}

describe('CredentialsProvider', () => {
  let originalFetch: typeof globalThis.fetch;

  beforeEach(() => {
    originalFetch = globalThis.fetch;
    vi.restoreAllMocks();
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('should create provider with correct config', () => {
    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });

    expect(provider.id).toBe('credentials');
    expect(provider.name).toBe('Credentials');
    expect(provider.type).toBe('credentials');
    expect(provider.authorize).toBeDefined();
  });

  it('should use custom authorize function when provided', async () => {
    const customAuthorize = vi.fn().mockResolvedValue({
      tokens: { accessToken: 'custom', tokenType: 'Bearer' },
      user: { id: '1' },
    });

    const provider = CredentialsProvider({ authorize: customAuthorize });

    const result = await provider.authorize({ email: 'a@b.com', password: 'pass' }, undefined);

    expect(customAuthorize).toHaveBeenCalled();
    expect(result?.tokens.accessToken).toBe('custom');
  });

  it('should throw if no apiUrl', async () => {
    const provider = CredentialsProvider();

    await expect(provider.authorize({ email: 'a@b.com', password: 'pass' }, undefined)).rejects.toThrow(CredentialsSignInError);
  });

  it('should throw if email is missing', async () => {
    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });

    await expect(provider.authorize({ email: '', password: 'pass' }, undefined)).rejects.toThrow(CredentialsSignInError);
  });

  it('should throw if password is missing', async () => {
    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });

    await expect(provider.authorize({ email: 'a@b.com', password: '' }, undefined)).rejects.toThrow(CredentialsSignInError);
  });

  it('should call sign-in endpoint on successful auth', async () => {
    const mockFetch = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json(
        {
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600,
          userId: '11111111-1111-4111-8111-111111111111',
          email: 'test@example.com',
          roles: ['Admin', 'User'],
          permissions: ['users:read', 'users:create'],
          user: { displayName: 'Test', profilePictureUrl: 'https://img.com/pic.png' },
        },
        { status: 200 },
      ),
    );
    globalThis.fetch = mockFetch;

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });
    const result = await provider.authorize({ email: 'test@example.com', password: 'password123' }, undefined);

    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:8080/v1/auth/sign-in',
      expect.objectContaining({
        method: 'POST',
      }),
    );
    expect(new Headers(mockFetch.mock.calls[0][1]?.headers).get('Content-Type')).toBe('application/json');

    expect(result).not.toBeNull();
    expect(result!.tokens.accessToken).toBe('access-token');
    expect(result!.tokens.refreshToken).toBe('refresh-token');
    expect(result!.user.id).toBe('11111111-1111-4111-8111-111111111111');
    expect(result!.user.email).toBe('test@example.com');
    expect(result!.user.name).toBe('Test');
    expect(result!.user.image).toBe('https://img.com/pic.png');
    expect(result!.user.roles).toEqual(['Admin', 'User']);
    expect(result!.user.permissions).toEqual(['users:read', 'users:create']);
  });

  it('should derive roles and permissions from access token claims when response omits arrays', async () => {
    const accessToken = makeUnsignedJwt({
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': ['Admin', 'User'],
      scope: 'users:read users:create',
    });
    const mockFetch = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json(
        {
          accessToken,
          refreshToken: 'refresh-token',
          expiresIn: 3600,
          userId: '11111111-1111-4111-8111-111111111111',
          email: 'test@example.com',
          user: { username: 'admin' },
        },
        { status: 200 },
      ),
    );
    globalThis.fetch = mockFetch;

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });
    const result = await provider.authorize({ email: 'test@example.com', password: 'password123' }, undefined);

    expect(result!.user.roles).toEqual(['Admin', 'User']);
    expect(result!.user.permissions).toEqual(['users:read', 'users:create']);
  });

  it('should use custom signInPath', async () => {
    const mockFetch = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json(
        {
          accessToken: 'token',
          userId: '11111111-1111-4111-8111-111111111111',
        },
        { status: 200 },
      ),
    );
    globalThis.fetch = mockFetch;

    const provider = CredentialsProvider({
      apiUrl: 'http://localhost:8080',
      signInPath: '/custom/sign-in',
    });
    await provider.authorize({ email: 'a@b.com', password: 'pass' }, undefined);

    expect(mockFetch).toHaveBeenCalledWith('http://localhost:8080/custom/sign-in', expect.anything());
  });

  it('should include tenantId in request body when provided', async () => {
    const mockFetch = vi
      .fn<typeof fetch>()
      .mockResolvedValue(Response.json({ accessToken: 'token', userId: '11111111-1111-4111-8111-111111111111' }, { status: 200 }));
    globalThis.fetch = mockFetch;

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });
    await provider.authorize({ email: 'a@b.com', password: 'pass', tenantId: '22222222-2222-4222-8222-222222222222' }, undefined);

    const body: unknown = JSON.parse(mockFetch.mock.calls[0][1]?.body as string);
    expect(body).toMatchObject({ tenantId: '22222222-2222-4222-8222-222222222222' });
  });

  it('should include rememberMe=true in request body when requested', async () => {
    const mockFetch = vi
      .fn<typeof fetch>()
      .mockResolvedValue(Response.json({ accessToken: 'token', userId: '11111111-1111-4111-8111-111111111111' }, { status: 200 }));
    globalThis.fetch = mockFetch;

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });
    const result = await provider.authorize({ email: 'a@b.com', password: 'pass', rememberMe: true }, undefined);

    const body: unknown = JSON.parse(mockFetch.mock.calls[0][1]?.body as string);
    expect(body).toMatchObject({ rememberMe: true });
    expect(result?.rememberMe).toBe(true);
  });

  it('should omit rememberMe from the request body when not requested', async () => {
    const mockFetch = vi
      .fn<typeof fetch>()
      .mockResolvedValue(Response.json({ accessToken: 'token', userId: '11111111-1111-4111-8111-111111111111' }, { status: 200 }));
    globalThis.fetch = mockFetch;

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });
    const result = await provider.authorize({ email: 'a@b.com', password: 'pass', rememberMe: false }, undefined);

    const body: unknown = JSON.parse(mockFetch.mock.calls[0][1]?.body as string);
    expect(body).not.toHaveProperty('rememberMe');
    expect(result?.rememberMe).toBe(false);
  });

  it('should throw AccountLockedError on 423 response', async () => {
    globalThis.fetch = vi.fn<typeof fetch>().mockResolvedValue(Response.json({ message: 'Too many attempts' }, { status: 423 }));

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });

    await expect(provider.authorize({ email: 'a@b.com', password: 'pass' }, undefined)).rejects.toThrow(AccountLockedError);
  });

  it('should throw CredentialsSignInError on 401 response', async () => {
    globalThis.fetch = vi.fn<typeof fetch>().mockResolvedValue(Response.json({ message: 'Wrong password' }, { status: 401 }));

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });

    await expect(provider.authorize({ email: 'a@b.com', password: 'pass' }, undefined)).rejects.toThrow(CredentialsSignInError);
  });

  it('should throw MfaRequiredError when response has requiresMfa', async () => {
    globalThis.fetch = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json(
        {
          requiresMfa: true,
          mfaSessionId: 'mfa-session-123',
        },
        { status: 200 },
      ),
    );

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });

    await expect(provider.authorize({ email: 'a@b.com', password: 'pass' }, undefined)).rejects.toThrow(MfaRequiredError);
  });

  it('should handle non-JSON error response', async () => {
    globalThis.fetch = vi.fn<typeof fetch>().mockResolvedValue(new Response('not JSON', { status: 500 }));

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });

    await expect(provider.authorize({ email: 'a@b.com', password: 'pass' }, undefined)).rejects.toThrow(AuthServiceUnavailableError);
  });

  it('preserves the limited native MFA challenge and methods without issuing tokens', async () => {
    const mfaToken = 'x'.repeat(43);
    globalThis.fetch = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json({
        requiresMfa: true,
        mfaToken,
        availableMethods: ['TOTP', 'BackupCode'],
      }),
    );
    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });
    const error: unknown = await provider.authorize({ email: 'a@b.com', password: 'pass' }).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(MfaRequiredError);
    if (!(error instanceof MfaRequiredError)) throw new Error('Expected the limited MFA challenge');
    expect(error.toJSON()).toMatchObject({ error: 'MfaRequired', mfaToken, availableMethods: ['TOTP', 'BackupCode'] });
    expect(error.toJSON()).not.toHaveProperty('accessToken');
    expect(error.toJSON()).not.toHaveProperty('user');
  });

  it('carries the enrollment offer from a required-MFA unenrolled first factor', async () => {
    globalThis.fetch = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json({
        requiresMfa: true,
        mfaToken: 'x'.repeat(43),
        availableMethods: ['TOTP'],
        mfaEnrollment: {
          secretKey: 'SYNTHETIC-SETUP',
          qrCodeUri: 'otpauth://totp/Example?secret=SYNTHETIC',
          expiresAt: new Date(Date.now() + 300_000).toISOString(),
        },
      }),
    );
    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });
    const error: unknown = await provider.authorize({ email: 'a@b.com', password: 'pass' }).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(MfaRequiredError);
    if (!(error instanceof MfaRequiredError)) throw new Error('Expected the limited MFA challenge');
    expect(error.mfaEnrollment).toMatchObject({ secretKey: 'SYNTHETIC-SETUP', qrCodeUri: 'otpauth://totp/Example?secret=SYNTHETIC' });
    expect(error.toJSON().mfaEnrollment).toMatchObject({ secretKey: 'SYNTHETIC-SETUP' });
  });

  it('completes MFA with only the limited bearer, method and code', async () => {
    const mockFetch = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json({
        accessToken: 'verified-access',
        refreshToken: 'verified-refresh',
        userId: '33333333-3333-4333-8333-333333333333',
        email: 'server@example.com',
        mfaEnrollmentBackupCodes: ['one-time-code'],
      }),
    );
    globalThis.fetch = mockFetch;
    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });
    const result = await provider.authorize({
      mfaToken: 'x'.repeat(43),
      method: 'Totp',
      code: '123456',
      email: 'untrusted@example.com',
      password: 'must-not-be-forwarded',
      tenantId: 'untrusted-tenant',
      rememberMe: true,
    });
    expect(mockFetch.mock.calls[0][0]).toBe('http://localhost:8080/v1/auth/mfa/sign-in/complete');
    expect(JSON.parse(mockFetch.mock.calls[0][1]?.body as string)).toEqual({ mfaToken: 'x'.repeat(43), method: 'Totp', code: '123456' });
    expect(result?.user.email).toBe('server@example.com');
    expect(result?.tokens.accessToken).toBe('verified-access');
    expect(result).toHaveProperty('mfaEnrollmentBackupCodes', ['one-time-code']);
    expect(result?.rememberMe).toBe(true);
    expect(result?.tokens).not.toHaveProperty('mfaEnrollmentBackupCodes');
  });

  it.each([
    { mfaToken: 'bad', method: 'Totp', code: '123456' },
    { mfaToken: 'x'.repeat(43), method: 'WebAuthn', code: '123456' },
    { mfaToken: 'x'.repeat(43), method: 'Totp', code: '' },
  ])('rejects invalid completion input before contacting the backend: %j', async (credentials) => {
    globalThis.fetch = vi.fn();
    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });
    await expect(provider.authorize(credentials)).rejects.toThrow(CredentialsSignInError);
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });

  it('rejects a successful HTTP response that supplies no ordinary access token', async () => {
    globalThis.fetch = vi.fn<typeof fetch>().mockResolvedValue(Response.json({ userId: '11111111-1111-4111-8111-111111111111' }));
    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });
    await expect(provider.authorize({ mfaToken: 'x'.repeat(43), method: 'BackupCode', code: 'backup-code' })).rejects.toThrow(CredentialsSignInError);
  });

  it('should throw service unavailable on backend 500 instead of credentials error', async () => {
    globalThis.fetch = vi.fn<typeof fetch>().mockResolvedValue(Response.json({ title: 'An error occurred while processing your request.' }, { status: 500 }));

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });

    await expect(provider.authorize({ email: 'a@b.com', password: 'pass' }, undefined)).rejects.toThrow(AuthServiceUnavailableError);
  });

  it('should throw service unavailable when the auth API cannot be reached', async () => {
    globalThis.fetch = vi.fn().mockRejectedValue(new TypeError('fetch failed'));

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });

    await expect(provider.authorize({ email: 'a@b.com', password: 'pass' }, undefined)).rejects.toThrow(AuthServiceUnavailableError);
  });

  it('should classify non-Error transport failures as service unavailable', async () => {
    globalThis.fetch = vi.fn().mockRejectedValue('connection refused');

    const provider = CredentialsProvider({ apiUrl: 'http://localhost:8080' });

    await expect(provider.authorize({ email: 'a@b.com', password: 'pass' }, undefined)).rejects.toMatchObject({
      name: 'AuthServiceUnavailableError',
      cause: undefined,
    });
  });

  it('should use __apiUrl from credentials as fallback', async () => {
    const mockFetch = vi
      .fn<typeof fetch>()
      .mockResolvedValue(Response.json({ accessToken: 'token', userId: '11111111-1111-4111-8111-111111111111' }, { status: 200 }));
    globalThis.fetch = mockFetch;

    const provider = CredentialsProvider(); // no apiUrl in options
    await provider.authorize({ email: 'a@b.com', password: 'pass', __apiUrl: 'http://injected:5295' }, undefined);

    expect(mockFetch).toHaveBeenCalledWith('http://injected:5295/v1/auth/sign-in', expect.anything());
  });
});

import { afterEach } from 'vitest';

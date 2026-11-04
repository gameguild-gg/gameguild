/**
 * Tests for Extended Auth Operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  verifyMfa,
  setupTotpMfa,
  getMfaMethods,
  requestPasswordReset,
  confirmPasswordReset,
  changePassword,
  sendVerificationEmail,
  resendVerificationEmail,
  verifyEmail,
  listSessions,
  terminateSession,
  terminateOtherSessions,
  terminateAllSessions,
  MfaVerificationError,
  PasswordResetError,
  EmailVerificationError,
} from '../../src/runtime/auth/extended-operations.js';
import { SessionTerminationError } from '../../src/runtime/auth/errors.js';

const API_URL = 'https://api.test.com';
const TOKEN = 'test-access-token';

// Mock fetch globally
const mockFetch = vi.fn<(url: string, options: RequestInit) => Promise<unknown>>();

beforeEach(() => {
  vi.stubGlobal('fetch', mockFetch);
  mockFetch.mockReset();
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('Extended Auth Operations', () => {
  describe('MFA', () => {
    it('verifyMfa sends correct request and returns ProviderResult on success', async () => {
      mockFetch.mockResolvedValueOnce(
        Response.json({
          accessToken: 'new-at',
          refreshToken: 'new-rt',
          expiresIn: 3600,
          userId: '11111111-1111-4111-8111-111111111111',
          user: { id: '11111111-1111-4111-8111-111111111111', email: 'test@test.com', displayName: 'Tester' },
        }),
      );
      const result = await verifyMfa(API_URL, { mfaSessionId: 'x'.repeat(43), code: '123456', method: 'totp' });
      const [url, options] = mockFetch.mock.calls[0];
      expect(url).toBe(`${API_URL}/v1/auth/mfa/sign-in/complete`);
      expect(options.method).toBe('POST');
      expect(JSON.parse(options.body as string)).toEqual({ mfaToken: 'x'.repeat(43), code: '123456', method: 'Totp' });
      expect(result.tokens.accessToken).toBe('new-at');
      expect(result.user.id).toBe('11111111-1111-4111-8111-111111111111');
    });

    it('verifyMfa throws MfaVerificationError on failure', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({ message: 'Invalid code' }, { status: 401 }));
      await expect(verifyMfa(API_URL, { mfaToken: 'x'.repeat(43), code: '000000', method: 'totp' })).rejects.toThrow(MfaVerificationError);
    });

    it('verifyMfa never sends the deprecated ordinary accessToken argument', async () => {
      mockFetch.mockResolvedValueOnce(
        Response.json({
          accessToken: 'at',
          refreshToken: 'rt',
          userId: '11111111-1111-4111-8111-111111111111',
        }),
      );
      await verifyMfa(API_URL, { mfaToken: 'x'.repeat(43), code: '123456', method: 'totp' }, TOKEN);
      const [, opts] = mockFetch.mock.calls[0];
      expect(new Headers(opts.headers).has('Authorization')).toBe(false);
    });
  });

  describe('Password Reset', () => {
    it('requestPasswordReset always acknowledges HTTP failures (prevents email enumeration)', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({}, { status: 404 }));
      await expect(requestPasswordReset(API_URL, { email: 'x@y.com' })).resolves.toBeUndefined();
    });

    it('confirmPasswordReset calls correct endpoint with mandatory confirmation', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({ success: true }));
      await confirmPasswordReset(API_URL, { token: 'reset-token', newPassword: 'new-password' });
      const [url, options] = mockFetch.mock.calls[0];
      expect(url).toBe(`${API_URL}/v1/auth/password:reset`);
      expect(options.method).toBe('POST');
      expect(JSON.parse(options.body as string)).toEqual({ token: 'reset-token', newPassword: 'new-password', confirmPassword: 'new-password' });
    });

    it('confirmPasswordReset throws PasswordResetError on failure', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({ message: 'Token expired' }, { status: 400 }));
      await expect(confirmPasswordReset(API_URL, { token: 'bad', newPassword: 'new-password' })).rejects.toThrow(PasswordResetError);
    });

    it('changePassword sends auth header and mandatory confirmation', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({ success: true }));
      await changePassword(API_URL, { currentPassword: 'old', newPassword: 'new-password' }, TOKEN);
      const [url, opts] = mockFetch.mock.calls[0];
      expect(url).toBe(`${API_URL}/v1/auth/password:change`);
      expect(new Headers(opts.headers).get('Authorization')).toBe(`Bearer ${TOKEN}`);
      expect(JSON.parse(opts.body as string)).toEqual({ currentPassword: 'old', newPassword: 'new-password', confirmPassword: 'new-password' });
    });
  });

  describe('Email Verification', () => {
    it('sendVerificationEmail resolves the actor email before sending the required body with auth', async () => {
      const userId = '11111111-1111-4111-8111-111111111111';
      mockFetch
        .mockResolvedValueOnce(Response.json({ userId }))
        .mockResolvedValueOnce(Response.json({ id: userId, email: 'actor@example.com' }))
        .mockResolvedValueOnce(Response.json({ message: 'Acknowledged' }));
      await sendVerificationEmail(API_URL, TOKEN);
      const [url, opts] = mockFetch.mock.calls[2];
      expect(url).toBe(`${API_URL}/v1/auth/email:send-verification`);
      expect(new Headers(opts.headers).get('Authorization')).toBe(`Bearer ${TOKEN}`);
      expect(JSON.parse(opts.body as string)).toEqual({ email: 'actor@example.com' });
      expect(mockFetch.mock.calls[0][0]).toBe(`${API_URL}/v1/auth/session-state`);
      expect(mockFetch.mock.calls[1][0]).toBe(`${API_URL}/v1/users/${userId}`);
    });

    it('sendVerificationEmail throws on failure', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({ message: 'Unauthorized' }, { status: 401 }));
      await expect(sendVerificationEmail(API_URL, TOKEN)).rejects.toThrow(EmailVerificationError);
    });

    it('resendVerificationEmail calls anonymous endpoint with email body', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({ message: 'Acknowledged' }));
      await resendVerificationEmail(API_URL, { email: 'user@example.com' });
      expect(mockFetch).toHaveBeenCalledWith(
        `${API_URL}/v1/auth/email:send-verification`,
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ email: 'user@example.com' }),
        }),
      );
    });

    it('verifyEmail calls correct endpoint', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({ success: true }));
      await verifyEmail(API_URL, { token: 'verify-token' });
      expect(mockFetch).toHaveBeenCalledWith(`${API_URL}/v1/auth/email:verify`, expect.objectContaining({ method: 'POST' }));
    });
  });

  describe('Session Management', () => {
    it('listSessions returns session array', async () => {
      const sessions = [
        { id: 's1', createdAt: '2024-01-01', lastActiveAt: '2024-01-02', isCurrent: true },
        { id: 's2', createdAt: '2024-01-01', lastActiveAt: '2024-01-01', isCurrent: false },
      ];
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => sessions,
      });

      const result = await listSessions(API_URL, TOKEN);
      expect(result).toHaveLength(2);
      expect(result[0].isCurrent).toBe(true);
    });

    it('listSessions returns empty array on failure', async () => {
      mockFetch.mockResolvedValueOnce({ ok: false });
      const result = await listSessions(API_URL, TOKEN);
      expect(result).toEqual([]);
    });

    it('terminateSession calls DELETE with session ID', async () => {
      mockFetch.mockResolvedValueOnce({ ok: true });

      await terminateSession(API_URL, 'session-123', TOKEN);

      const [url, opts] = mockFetch.mock.calls[0];
      expect(url).toBe(`${API_URL}/v1/auth/sessions/session-123`);
      expect(opts.method).toBe('DELETE');
    });

    it('terminateSession throws SessionTerminationError on failure', async () => {
      mockFetch.mockResolvedValueOnce({ ok: false });

      await expect(terminateSession(API_URL, 'session-bad', TOKEN)).rejects.toThrow(SessionTerminationError);
    });

    it('terminateOtherSessions calls correct endpoint', async () => {
      mockFetch.mockResolvedValueOnce({ ok: true });

      await terminateOtherSessions(API_URL, TOKEN);

      expect(mockFetch).toHaveBeenCalledWith(`${API_URL}/v1/auth/sessions:terminate-others`, expect.objectContaining({ method: 'POST' }));
    });

    it('terminateOtherSessions throws SessionTerminationError on failure', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: 'Failed' }),
      });

      await expect(terminateOtherSessions(API_URL, TOKEN)).rejects.toThrow(SessionTerminationError);
    });

    it('terminateAllSessions calls correct endpoint', async () => {
      mockFetch.mockResolvedValueOnce({ ok: true });

      await terminateAllSessions(API_URL, TOKEN);

      expect(mockFetch).toHaveBeenCalledWith(`${API_URL}/v1/auth/sessions:terminate-all`, expect.objectContaining({ method: 'POST' }));
    });

    it('setupTotpMfa returns MfaSetupResult on success', async () => {
      mockFetch.mockResolvedValueOnce(
        Response.json({
          secretKey: 'JBSWY3DPEHPK3PXP',
          qrCodeUri: 'otpauth://totp/GameGuild?secret=JBSWY3DPEHPK3PXP',
          backupCodes: ['abc123', 'def456'],
        }),
      );

      const result = await setupTotpMfa(API_URL, TOKEN);
      const [url, options] = mockFetch.mock.calls[0];
      expect(url).toBe(`${API_URL}/v1/auth/mfa/totp:setup`);
      expect(options.method).toBe('POST');
      expect(new Headers(options.headers).get('Authorization')).toBe(`Bearer ${TOKEN}`);
      expect(result).toEqual({
        secret: 'JBSWY3DPEHPK3PXP',
        qrCodeUri: 'otpauth://totp/GameGuild?secret=JBSWY3DPEHPK3PXP',
        backupCodes: ['abc123', 'def456'],
        requiresVerification: true,
      });
    });

    it('setupTotpMfa throws MfaVerificationError on failure', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({ message: 'MFA already configured' }, { status: 400 }));
      await expect(setupTotpMfa(API_URL, TOKEN)).rejects.toThrow(MfaVerificationError);
    });

    it('getMfaMethods returns methods array on success', async () => {
      mockFetch.mockResolvedValueOnce(
        Response.json({
          methods: ['Totp', 'Sms'].map((method) => ({
            method,
            name: null,
            description: null,
            isAvailable: true,
            isEnabled: true,
            priority: 1,
          })),
        }),
      );

      const result = await getMfaMethods(API_URL, TOKEN);
      const [url, options] = mockFetch.mock.calls[0];
      expect(url).toBe(`${API_URL}/v1/auth/mfa/methods`);
      expect(new Headers(options.headers).get('Authorization')).toBe(`Bearer ${TOKEN}`);
      expect(result).toEqual(['totp', 'sms']);
    });

    it('getMfaMethods returns empty array on failure', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({}, { status: 401 }));
      const result = await getMfaMethods(API_URL, TOKEN);
      expect(result).toEqual([]);
    });

    it('getMfaMethods rejects malformed responses when methods field is missing', async () => {
      mockFetch.mockResolvedValueOnce(Response.json({}));
      await expect(getMfaMethods(API_URL, TOKEN)).rejects.toThrow(MfaVerificationError);
    });
  });

  describe('Error Classes', () => {
    it('MfaVerificationError has correct properties', () => {
      const err = new MfaVerificationError('Invalid code', { attemptsRemaining: 3 });
      expect(err.name).toBe('MfaVerificationError');
      expect(err.type).toBe('MfaVerificationError');
      expect(err.status).toBe(401);
      expect(err.attemptsRemaining).toBe(3);
    });

    it('PasswordResetError has correct properties', () => {
      const err = new PasswordResetError('Token expired');
      expect(err.name).toBe('PasswordResetError');
      expect(err.status).toBe(400);
    });

    it('EmailVerificationError has correct properties', () => {
      const err = new EmailVerificationError();
      expect(err.name).toBe('EmailVerificationError');
      expect(err.status).toBe(400);
    });

    it('SessionTerminationError has correct properties', () => {
      const err = new SessionTerminationError('Test');
      expect(err.name).toBe('SessionTerminationError');
      expect(err.status).toBe(500);
      expect(err.type).toBe('SessionTerminationError');
    });
  });
});

/**
 * Tests for Extended Auth Operations
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  verifyMfa,
  requestPasswordReset,
  confirmPasswordReset,
  changePassword,
  sendVerificationEmail,
  verifyEmail,
  listSessions,
  terminateSession,
  terminateOtherSessions,
  terminateAllSessions,
  MfaVerificationError,
  PasswordResetError,
  EmailVerificationError,
} from '../../src/runtime/auth/extended-operations.js';

const API_URL = 'https://api.test.com';
const TOKEN = 'test-access-token';

// Mock fetch globally
const mockFetch = vi.fn();

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
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          accessToken: 'new-at',
          refreshToken: 'new-rt',
          expiresIn: 3600,
          userId: 'u1',
          user: { id: 'u1', email: 'test@test.com', displayName: 'Tester' },
        }),
      });

      const result = await verifyMfa(API_URL, {
        mfaSessionId: 'mfa-123',
        code: '123456',
        method: 'totp',
      });

      expect(mockFetch).toHaveBeenCalledWith(
        `${API_URL}/v1/auth/mfa/verify`,
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({
            mfaSessionId: 'mfa-123',
            code: '123456',
            method: 'totp',
          }),
        })
      );
      expect(result.tokens.accessToken).toBe('new-at');
      expect(result.user.id).toBe('u1');
    });

    it('verifyMfa throws MfaVerificationError on failure', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: 'Invalid code', attemptsRemaining: 2 }),
      });

      await expect(
        verifyMfa(API_URL, { mfaSessionId: 'mfa-1', code: '000000', method: 'totp' })
      ).rejects.toThrow(MfaVerificationError);
    });

    it('verifyMfa sends auth header when accessToken provided', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          accessToken: 'at', refreshToken: 'rt', userId: 'u1',
        }),
      });

      await verifyMfa(API_URL, { mfaSessionId: 'm1', code: '1', method: 'totp' }, TOKEN);

      const [, opts] = mockFetch.mock.calls[0];
      expect(opts.headers.Authorization).toBe(`Bearer ${TOKEN}`);
    });
  });

  describe('Password Reset', () => {
    it('requestPasswordReset always succeeds (prevents email enumeration)', async () => {
      mockFetch.mockResolvedValueOnce({ ok: false }); // Even on failure
      await expect(requestPasswordReset(API_URL, { email: 'x@y.com' })).resolves.toBeUndefined();
    });

    it('confirmPasswordReset calls correct endpoint', async () => {
      mockFetch.mockResolvedValueOnce({ ok: true });

      await confirmPasswordReset(API_URL, { token: 'reset-token', newPassword: 'newpass' });

      expect(mockFetch).toHaveBeenCalledWith(
        `${API_URL}/v1/auth/password:reset`,
        expect.objectContaining({ method: 'POST' })
      );
    });

    it('confirmPasswordReset throws PasswordResetError on failure', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: 'Token expired' }),
      });

      await expect(
        confirmPasswordReset(API_URL, { token: 'bad', newPassword: 'x' })
      ).rejects.toThrow(PasswordResetError);
    });

    it('changePassword sends auth header', async () => {
      mockFetch.mockResolvedValueOnce({ ok: true });

      await changePassword(API_URL, { currentPassword: 'old', newPassword: 'new' }, TOKEN);

      const [url, opts] = mockFetch.mock.calls[0];
      expect(url).toBe(`${API_URL}/v1/auth/password:change`);
      expect(opts.headers.Authorization).toBe(`Bearer ${TOKEN}`);
    });
  });

  describe('Email Verification', () => {
    it('sendVerificationEmail calls correct endpoint with auth', async () => {
      mockFetch.mockResolvedValueOnce({ ok: true });

      await sendVerificationEmail(API_URL, TOKEN);

      const [url, opts] = mockFetch.mock.calls[0];
      expect(url).toBe(`${API_URL}/v1/auth/email:send-verification`);
      expect(opts.headers.Authorization).toBe(`Bearer ${TOKEN}`);
    });

    it('sendVerificationEmail throws on failure', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: 'Rate limited' }),
      });

      await expect(sendVerificationEmail(API_URL, TOKEN)).rejects.toThrow(EmailVerificationError);
    });

    it('verifyEmail calls correct endpoint', async () => {
      mockFetch.mockResolvedValueOnce({ ok: true });

      await verifyEmail(API_URL, { token: 'verify-token' });

      expect(mockFetch).toHaveBeenCalledWith(
        `${API_URL}/v1/auth/email:verify`,
        expect.objectContaining({ method: 'POST' })
      );
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

    it('terminateOtherSessions calls correct endpoint', async () => {
      mockFetch.mockResolvedValueOnce({ ok: true });

      await terminateOtherSessions(API_URL, TOKEN);

      expect(mockFetch).toHaveBeenCalledWith(
        `${API_URL}/v1/auth/sessions:terminate-others`,
        expect.objectContaining({ method: 'POST' })
      );
    });

    it('terminateAllSessions calls correct endpoint', async () => {
      mockFetch.mockResolvedValueOnce({ ok: true });

      await terminateAllSessions(API_URL, TOKEN);

      expect(mockFetch).toHaveBeenCalledWith(
        `${API_URL}/v1/auth/sessions:terminate-all`,
        expect.objectContaining({ method: 'POST' })
      );
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
  });
});

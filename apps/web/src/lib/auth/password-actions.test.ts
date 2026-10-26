import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  verifyTurnstileToken: vi.fn(),
  forwardedIpHeaders: vi.fn(),
}));

vi.mock('@/lib/antibot/turnstile', () => ({ verifyTurnstileToken: mocks.verifyTurnstileToken }));
vi.mock('@/lib/server/forwarded-headers', () => ({ forwardedIpHeaders: mocks.forwardedIpHeaders }));

import {
  requestPasswordResetAction,
  completePasswordResetAction,
} from './password-actions';

const API_URL = 'http://localhost:8080';

function fetchResponding(status: number, body: unknown = {}) {
  return vi.fn().mockResolvedValue(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    }),
  );
}

describe('password-actions Turnstile gating', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', API_URL);
    mocks.verifyTurnstileToken.mockResolvedValue({ success: true });
    mocks.forwardedIpHeaders.mockResolvedValue({ 'X-Forwarded-For': '203.0.113.7' });
  });

  describe('requestPasswordResetAction', () => {
    it('returns typed failure and does NOT call the API when verification fails', async () => {
      mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: 'verify-failed' });
      const fetchMock = vi.fn();
      vi.stubGlobal('fetch', fetchMock);

      const result = await requestPasswordResetAction('user@example.com', 'bad-token');

      expect(result).toEqual({ success: false, error: 'Bot check failed. Please refresh and try again.' });
      expect(fetchMock).not.toHaveBeenCalled();
    });

    it('proceeds to the API when verification succeeds and forwards XFF but not the token', async () => {
      const fetchMock = fetchResponding(200);
      vi.stubGlobal('fetch', fetchMock);

      const result = await requestPasswordResetAction('user@example.com', 'tok-123');

      expect(result).toEqual({ success: true, data: undefined });
      expect(fetchMock).toHaveBeenCalledTimes(1);
      const [url, init] = fetchMock.mock.calls[0];
      expect(String(url)).toBe('/v1/auth/password:reset-request');
      expect(init.headers['X-Forwarded-For']).toBe('203.0.113.7');
      expect(JSON.parse(init.body)).toEqual({ email: 'user@example.com' });
      expect(JSON.parse(init.body).turnstileToken).toBeUndefined();
    });

    it('maps backend error payloads to a typed failure result', async () => {
      vi.stubGlobal('fetch', fetchResponding(400, { message: 'Email is not allowed.' }));

      const result = await requestPasswordResetAction('user@example.com', 'tok-123');

      expect(result).toEqual({ success: false, error: 'Email is not allowed.' });
    });
  });

  describe('completePasswordResetAction', () => {
    it('returns typed failure and does NOT call the API when verification fails', async () => {
      mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: 'missing-token' });
      const fetchMock = vi.fn();
      vi.stubGlobal('fetch', fetchMock);

      const result = await completePasswordResetAction('reset-token', 'newpassword1', 'newpassword1', 'bad');

      expect(result).toEqual({ success: false, error: 'Bot check failed. Please refresh and try again.' });
      expect(fetchMock).not.toHaveBeenCalled();
    });

    it('proceeds to the API when verification succeeds and forwards XFF but not the turnstile token', async () => {
      const fetchMock = fetchResponding(200);
      vi.stubGlobal('fetch', fetchMock);

      const result = await completePasswordResetAction('reset-token', 'newpassword1', 'newpassword1', 'tok-456');

      expect(result).toEqual({ success: true, data: undefined });
      expect(fetchMock).toHaveBeenCalledTimes(1);
      const [url, init] = fetchMock.mock.calls[0];
      expect(String(url)).toBe('/v1/auth/password:reset');
      expect(init.headers['X-Forwarded-For']).toBe('203.0.113.7');
      expect(JSON.parse(init.body)).toEqual({ token: 'reset-token', newPassword: 'newpassword1' });
      expect(JSON.parse(init.body).turnstileToken).toBeUndefined();
    });

    it('maps backend error payloads to a typed failure result', async () => {
      vi.stubGlobal('fetch', fetchResponding(400, { detail: 'Reset link expired.' }));

      const result = await completePasswordResetAction('reset-token', 'newpassword1', 'newpassword1', 'tok-456');

      expect(result).toEqual({ success: false, error: 'Reset link expired.' });
    });
  });
});
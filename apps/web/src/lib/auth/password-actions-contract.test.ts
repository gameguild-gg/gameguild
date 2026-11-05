// @vitest-environment node
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { GeneratedApi } from '@game-guild/client';

const requestHeaders = vi.hoisted(() => vi.fn());
vi.mock('next/headers', () => ({ headers: requestHeaders }));

import { requestPasswordResetAction, completePasswordResetAction, verifyEmailAction, resendVerificationEmailAction } from './password-actions';

const API = 'https://api.gameguild.gg';
const SITEVERIFY = 'https://challenges.cloudflare.com/turnstile/v0/siteverify';
const PASSWORD = 'Synthetic-password-123!';
const fetchMock = vi.fn<typeof fetch>();

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal('fetch', fetchMock);
  vi.stubEnv('API_URL', API);
  vi.stubEnv('TURNSTILE_SECRET_KEY', 'synthetic-turnstile-secret');
  requestHeaders.mockResolvedValue(new Headers({ 'cf-ray': 'trusted-ray', 'cf-connecting-ip': '198.51.100.8', 'x-forwarded-for': 'spoofed' }));
});
afterEach(() => {
  vi.useRealTimers();
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
  vi.restoreAllMocks();
});

function captchaAccepted() {
  fetchMock.mockResolvedValueOnce(Response.json({ success: true }));
}

describe('password server actions with real CAPTCHA/IP and generated SDK boundaries', () => {
  it('sends confirmation to the native reset contract only after the CAPTCHA succeeds', async () => {
    const generated = vi.spyOn(GeneratedApi.AuthModule.prototype, 'postAuthPasswordReset');
    fetchMock.mockImplementation((url, options) => {
      if (url === SITEVERIFY) return Promise.resolve(Response.json({ success: true }));
      const input: unknown = JSON.parse(options?.body as string);
      if (!input || typeof input !== 'object' || !('confirmPassword' in input) || input.confirmPassword !== PASSWORD) {
        return Promise.resolve(Response.json({ message: 'Confirmation is required' }, { status: 400 }));
      }
      return Promise.resolve(Response.json({ success: true }));
    });
    await expect(completePasswordResetAction(' reset-proof ', PASSWORD, PASSWORD, 'captcha-proof')).resolves.toEqual({ success: true, data: undefined });
    expect(generated).toHaveBeenCalledWith({ token: 'reset-proof', newPassword: PASSWORD, confirmPassword: PASSWORD });
    expect(fetchMock.mock.calls.map(([url]) => url)).toEqual([SITEVERIFY, `${API}/v1/auth/password:reset`]);
    const captcha = JSON.parse(fetchMock.mock.calls[0][1]?.body as string) as Record<string, unknown>;
    expect(captcha.response).toBe('captcha-proof');
    expect(captcha.remoteip).toBe('198.51.100.8');
    const options = fetchMock.mock.calls[1][1];
    expect(JSON.parse(options?.body as string)).toEqual({ token: 'reset-proof', newPassword: PASSWORD, confirmPassword: PASSWORD });
    expect(new Headers(options?.headers).get('X-Forwarded-For')).toBe('198.51.100.8');
    expect(new Headers(options?.headers).has('Authorization')).toBe(false);
    expect(options?.redirect).toBe('error');
  });

  it('uses the generated reset-request operation without exposing server account flags or CAPTCHA data', async () => {
    const generated = vi.spyOn(GeneratedApi.AuthModule.prototype, 'postAuthPasswordResetRequest');
    captchaAccepted();
    fetchMock.mockResolvedValueOnce(Response.json({ success: false, message: 'Never expose this account state' }));
    await expect(requestPasswordResetAction(' actor@example.com ', 'captcha-proof')).resolves.toEqual({ success: true, data: undefined });
    expect(generated).toHaveBeenCalledWith({ email: 'actor@example.com' });
    expect(fetchMock.mock.calls[1][0]).toBe(`${API}/v1/auth/password:reset-request`);
    expect(JSON.parse(fetchMock.mock.calls[1][1]?.body as string)).toEqual({ email: 'actor@example.com' });
  });

  it('does not forward a direct-origin client-supplied XFF or uncorroborated Cloudflare IP', async () => {
    requestHeaders.mockResolvedValue(new Headers({ 'cf-connecting-ip': '198.51.100.9', 'x-forwarded-for': 'spoofed' }));
    captchaAccepted();
    fetchMock.mockResolvedValueOnce(Response.json({ success: true }));
    await completePasswordResetAction('proof', PASSWORD, PASSWORD, 'captcha-proof');
    expect(new Headers(fetchMock.mock.calls[1][1]?.headers).has('X-Forwarded-For')).toBe(false);
    expect(JSON.parse(fetchMock.mock.calls[0][1]?.body as string)).not.toHaveProperty('remoteip');
  });

  const operations = [
    { name: 'request', invoke: () => requestPasswordResetAction('actor@example.com', 'captcha-proof') },
    { name: 'complete', invoke: () => completePasswordResetAction('proof', PASSWORD, PASSWORD, 'captcha-proof') },
  ];
  for (const operation of operations) {
    it(`${operation.name} makes no API request after explicit CAPTCHA rejection`, async () => {
      fetchMock.mockResolvedValueOnce(Response.json({ success: false }));
      await expect(operation.invoke()).resolves.toEqual({ success: false, error: 'Bot check failed. Please refresh and try again.' });
      expect(fetchMock).toHaveBeenCalledOnce();
      expect(fetchMock.mock.calls[0][0]).toBe(SITEVERIFY);
    });
    it.each([400, 429])(`${operation.name} preserves a rejected backend HTTP %i as a failure result`, async (status) => {
      captchaAccepted();
      fetchMock.mockResolvedValueOnce(Response.json({ detail: 'Request rejected' }, { status }));
      await expect(operation.invoke()).resolves.toEqual({ success: false, error: 'Request rejected' });
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });
    it(`${operation.name} returns a generic failure for invalid JSON`, async () => {
      captchaAccepted();
      fetchMock.mockResolvedValueOnce(new Response('{', { headers: { 'Content-Type': 'application/json' } }));
      const result = await operation.invoke();
      expect(result.success).toBe(false);
    });
    it(`${operation.name} does not log or return a network exception containing password data`, async () => {
      const log = vi.spyOn(console, 'log').mockImplementation(() => undefined),
        group = vi.spyOn(console, 'groupCollapsed').mockImplementation(() => undefined);
      captchaAccepted();
      fetchMock.mockRejectedValueOnce(new Error(`Network leaked ${PASSWORD}`));
      const result = await operation.invoke();
      expect(result.success).toBe(false);
      if (result.success) throw new Error('Expected failure');
      expect(result.error).not.toContain(PASSWORD);
      expect(log).not.toHaveBeenCalled();
      expect(group).not.toHaveBeenCalled();
    });
    it(`${operation.name} bounds the API fetch after CAPTCHA even if the adapter ignores abort`, async () => {
      vi.useFakeTimers();
      captchaAccepted();
      fetchMock.mockImplementationOnce(() => new Promise<Response>(() => undefined));
      const pending = operation.invoke();
      await vi.advanceTimersByTimeAsync(5_000);
      const result = await pending;
      expect(result.success).toBe(false);
      expect(fetchMock).toHaveBeenCalledTimes(2);
      expect(fetchMock.mock.calls[1][1]?.signal?.aborted).toBe(true);
    });
  }

  it.each([{ body: {} }, { body: { success: false } }, { body: { success: 'true' } }, { body: { success: true, userId: 'invalid' } }])(
    'does not announce a password change from an invalid or unsuccessful HTTP 200: %j',
    async ({ body }) => {
      captchaAccepted();
      fetchMock.mockResolvedValueOnce(Response.json(body));
      expect((await completePasswordResetAction('proof', PASSWORD, PASSWORD, 'captcha-proof')).success).toBe(false);
    },
  );

  it('rejects invalid email locally after CAPTCHA without calling the API', async () => {
    captchaAccepted();
    const result = await requestPasswordResetAction('invalid', 'captcha-proof');
    expect(result.success).toBe(false);
    expect(fetchMock).toHaveBeenCalledOnce();
  });
  it('retains confirmation and minimum-length guards without contacting the API', async () => {
    captchaAccepted();
    expect((await completePasswordResetAction('proof', PASSWORD, 'different', 'captcha-proof')).success).toBe(false);
    captchaAccepted();
    expect((await completePasswordResetAction('proof', 'short', 'short', 'captcha-proof')).success).toBe(false);
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls.every(([url]) => url === SITEVERIFY)).toBe(true);
  });
  it('handles non-string action arguments without throwing or reaching the API', async () => {
    captchaAccepted();
    await expect(requestPasswordResetAction(42 as unknown as string, 'captcha-proof')).resolves.toMatchObject({ success: false });
    captchaAccepted();
    await expect(completePasswordResetAction('proof', null as unknown as string, 'x', 'captcha-proof')).resolves.toMatchObject({ success: false });
    expect(fetchMock.mock.calls.every(([url]) => url === SITEVERIFY)).toBe(true);
  });

  it('rejects a non-string CAPTCHA proof before invoking external services', async () => {
    const badProof = { proof: 'untrusted' } as unknown as string;
    await expect(requestPasswordResetAction('actor@example.com', badProof)).resolves.toEqual({
      success: false,
      error: 'Bot check failed. Please refresh and try again.',
    });
    await expect(completePasswordResetAction('proof', PASSWORD, PASSWORD, badProof)).resolves.toEqual({
      success: false,
      error: 'Bot check failed. Please refresh and try again.',
    });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('retains email verification/resend and rejects their non-string action inputs', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({ success: true }));
    await expect(verifyEmailAction(' proof ')).resolves.toEqual({ success: true, data: undefined });
    fetchMock.mockResolvedValueOnce(Response.json({ message: 'Acknowledged' }));
    await expect(resendVerificationEmailAction(' actor@example.com ')).resolves.toEqual({ success: true, data: undefined });
    await expect(verifyEmailAction(42 as unknown as string)).resolves.toMatchObject({ success: false });
    await expect(resendVerificationEmailAction(null as unknown as string)).resolves.toMatchObject({ success: false });
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});

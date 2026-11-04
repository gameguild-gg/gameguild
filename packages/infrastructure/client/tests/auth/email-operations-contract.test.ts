import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { sendVerificationEmail, resendVerificationEmail, verifyEmail, EmailVerificationError } from '../../src/runtime/auth/extended-operations.js';
import { AuthModule } from '../../src/generated/modules/auth.gen.js';
import { UsersModule } from '../../src/generated/modules/users.gen.js';

const API = 'https://api.gameguild.gg';
const USER = '11111111-1111-4111-8111-111111111111';
const OTHER = '22222222-2222-4222-8222-222222222222';
const TOKEN = 'opaque-access-credential';
const STATE = { userId: USER, sessionId: OTHER, sessionValid: true, tokenRevoked: false };
const PROFILE = { id: USER, email: 'actor@example.com' };
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

describe('email verification through generated actor-bound contracts', () => {
  it('resolves the actor and own email on the server before sending the required body', async () => {
    const state = vi.spyOn(AuthModule.prototype, 'getAuthSessionState');
    const profile = vi.spyOn(UsersModule.prototype, 'getUsersForGetUsersByUserId');
    const send = vi.spyOn(AuthModule.prototype, 'postAuthEmailSendVerification');
    fetchMock
      .mockResolvedValueOnce(Response.json(STATE))
      .mockResolvedValueOnce(Response.json(PROFILE))
      .mockResolvedValueOnce(Response.json({ message: 'Acknowledged' }));
    await expect(sendVerificationEmail(API, TOKEN)).resolves.toBeUndefined();
    expect(state).toHaveBeenCalledOnce();
    expect(profile).toHaveBeenCalledWith(USER);
    expect(send).toHaveBeenCalledWith({ email: PROFILE.email });
    expect(fetchMock.mock.calls.map(([url]) => url)).toEqual([
      `${API}/v1/auth/session-state`,
      `${API}/v1/users/${USER}`,
      `${API}/v1/auth/email:send-verification`,
    ]);
    for (const [, options] of fetchMock.mock.calls) {
      expect(new Headers(options?.headers).get('Authorization')).toBe(`Bearer ${TOKEN}`);
      expect(options?.redirect).toBe('error');
    }
    expect(fetchMock.mock.calls.map(([, options]) => options?.method)).toEqual(['GET', 'GET', 'POST']);
    expect(JSON.parse(fetchMock.mock.calls[2][1]?.body as string)).toEqual({ email: PROFILE.email });
  });

  it('does not require a browser session for an otherwise authenticated stateless principal', async () => {
    fetchMock
      .mockResolvedValueOnce(Response.json({ userId: USER, sessionId: null, sessionValid: false, tokenRevoked: false }))
      .mockResolvedValueOnce(Response.json(PROFILE))
      .mockResolvedValueOnce(Response.json({ message: null }));
    await expect(sendVerificationEmail(API, TOKEN)).resolves.toBeUndefined();
  });

  it.each([{ body: {} }, { body: { userId: 'invalid' } }, { body: { ...STATE, tokenRevoked: true } }, { body: { ...STATE, sessionValid: false } }])(
    'does not fetch a profile or send mail from invalid/revoked session state: %j',
    async ({ body }) => {
      fetchMock.mockResolvedValueOnce(Response.json(body));
      await expect(sendVerificationEmail(API, TOKEN)).rejects.toThrow(EmailVerificationError);
      expect(fetchMock).toHaveBeenCalledOnce();
    },
  );

  it.each([{ body: {} }, { body: { ...PROFILE, id: OTHER } }, { body: { ...PROFILE, email: null } }, { body: { ...PROFILE, email: 'invalid' } }])(
    'never sends to a missing, invalid or different actor profile: %j',
    async ({ body }) => {
      fetchMock.mockResolvedValueOnce(Response.json(STATE)).mockResolvedValueOnce(Response.json(body));
      await expect(sendVerificationEmail(API, TOKEN)).rejects.toThrow(EmailVerificationError);
      expect(fetchMock).toHaveBeenCalledTimes(2);
    },
  );

  it.each([0, 1, 2])('stops at a rejected workflow step %i', async (step) => {
    for (const body of [STATE, PROFILE].slice(0, step)) fetchMock.mockResolvedValueOnce(Response.json(body));
    fetchMock.mockResolvedValueOnce(Response.json({ detail: 'Denied' }, { status: 403 }));
    await expect(sendVerificationEmail(API, TOKEN)).rejects.toThrow(EmailVerificationError);
    expect(fetchMock).toHaveBeenCalledTimes(step + 1);
  });

  it.each(['', ' '])('does not start authenticated verification without a bearer: %j', async (token) => {
    await expect(sendVerificationEmail(API, token)).rejects.toThrow(EmailVerificationError);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('resends anonymously through the generated operation without actor assertions', async () => {
    const generated = vi.spyOn(AuthModule.prototype, 'postAuthEmailSendVerification');
    fetchMock.mockResolvedValueOnce(Response.json({ message: 'Neutral acknowledgement' }));
    const input = { email: 'actor@example.com', userId: OTHER, accessToken: TOKEN };
    await expect(resendVerificationEmail(API, input)).resolves.toBeUndefined();
    expect(generated).toHaveBeenCalledWith({ email: input.email });
    expect(fetchMock.mock.calls[0][0]).toBe(`${API}/v1/auth/email:send-verification`);
    expect(new Headers(fetchMock.mock.calls[0][1]?.headers).has('Authorization')).toBe(false);
    expect(JSON.parse(fetchMock.mock.calls[0][1]?.body as string)).toEqual({ email: input.email });
  });

  it('verifies with the generated token operation and optional supported tenant context', async () => {
    const generated = vi.spyOn(AuthModule.prototype, 'postAuthEmailVerify');
    fetchMock.mockResolvedValueOnce(Response.json({ success: true, userId: USER, email: PROFILE.email, verifiedAt: '2026-11-04T08:00:00+00:00' }));
    const input = { token: 'email-proof', tenantId: OTHER, userId: 'spoofed' };
    await expect(verifyEmail(API, input)).resolves.toBeUndefined();
    expect(generated).toHaveBeenCalledWith({ token: 'email-proof', tenantId: OTHER });
    expect(new Headers(fetchMock.mock.calls[0][1]?.headers).has('Authorization')).toBe(false);
  });

  it.each([{ body: {} }, { body: { success: false } }, { body: { success: 'true' } }, { body: { success: true, userId: 'invalid' } }, { body: null }])(
    'cannot verify from an invalid or unsuccessful HTTP 200: %j',
    async ({ body }) => {
      fetchMock.mockResolvedValueOnce(Response.json(body));
      await expect(verifyEmail(API, { token: 'proof' })).rejects.toThrow(EmailVerificationError);
    },
  );

  it('validates email/token/tenant syntax before anonymous requests', async () => {
    await expect(resendVerificationEmail(API, { email: 'invalid' })).rejects.toThrow(EmailVerificationError);
    await expect(verifyEmail(API, { token: '' })).rejects.toThrow(EmailVerificationError);
    await expect(verifyEmail(API, { token: 'proof', tenantId: 'invalid' })).rejects.toThrow(EmailVerificationError);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  for (const operation of [
    { name: 'resend', invoke: () => resendVerificationEmail(API, { email: PROFILE.email }) },
    { name: 'verify', invoke: () => verifyEmail(API, { token: 'proof' }) },
  ]) {
    it.each([400, 401, 429, 500])(`${operation.name} rejects HTTP %i without retries or refresh`, async (status) => {
      fetchMock.mockResolvedValueOnce(Response.json({ detail: 'Rejected' }, { status }));
      await expect(operation.invoke()).rejects.toThrow(EmailVerificationError);
      expect(fetchMock).toHaveBeenCalledOnce();
    });
    it(`${operation.name} rejects malformed JSON with the public error`, async () => {
      fetchMock.mockResolvedValueOnce(new Response('{', { headers: { 'Content-Type': 'application/json' } }));
      await expect(operation.invoke()).rejects.toThrow(EmailVerificationError);
    });
  }

  for (const operation of [
    { name: 'send', invoke: () => sendVerificationEmail(API, TOKEN) },
    { name: 'resend', invoke: () => resendVerificationEmail(API, { email: PROFILE.email }) },
    { name: 'verify', invoke: () => verifyEmail(API, { token: 'proof' }) },
  ]) {
    it(`${operation.name} wraps network errors without credential logging`, async () => {
      const log = vi.spyOn(console, 'log').mockImplementation(() => undefined);
      const group = vi.spyOn(console, 'groupCollapsed').mockImplementation(() => undefined);
      fetchMock.mockRejectedValueOnce(new Error(`Failed with ${TOKEN}`));
      const error: unknown = await operation.invoke().catch((error: unknown) => error);
      expect(error).toBeInstanceOf(EmailVerificationError);
      if (!(error instanceof Error)) throw new Error('Expected verification error');
      expect(error.message).not.toContain(TOKEN);
      expect(log).not.toHaveBeenCalled();
      expect(group).not.toHaveBeenCalled();
    });
    it(`${operation.name} bounds a fetch adapter that ignores abort`, async () => {
      vi.useFakeTimers();
      fetchMock.mockImplementation(() => new Promise<Response>(() => undefined));
      const settled = expect(operation.invoke()).rejects.toThrow(EmailVerificationError);
      await vi.advanceTimersByTimeAsync(5_000);
      await settled;
      expect(fetchMock.mock.calls[0][1]?.signal?.aborted).toBe(true);
    });
  }

  it('shares one five-second budget across the authenticated lookup/send workflow', async () => {
    vi.useFakeTimers();
    fetchMock
      .mockImplementationOnce(() => new Promise((resolve) => setTimeout(() => resolve(Response.json(STATE)), 3_000)))
      .mockImplementationOnce(() => new Promise((resolve) => setTimeout(() => resolve(Response.json(PROFILE)), 1_500)))
      .mockImplementationOnce(() => new Promise<Response>(() => undefined));
    const settled = expect(sendVerificationEmail(API, TOKEN)).rejects.toThrow(EmailVerificationError);
    await vi.advanceTimersByTimeAsync(5_000);
    await settled;
    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(fetchMock.mock.calls[2][1]?.signal?.aborted).toBe(true);
  });
});

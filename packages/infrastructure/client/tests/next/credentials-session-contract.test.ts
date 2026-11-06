import { randomBytes } from 'node:crypto';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { GameGuildAuth } from '../../src/integrations/next/auth.js';
import { CredentialsProvider } from '../../src/runtime/auth/providers/credentials.js';
import { SessionStore, resolveCookieOptions } from '../../src/runtime/auth/cookies.js';

const API = 'http://127.0.0.1:9';
const WEB = 'http://localhost:3000';
const completed = {
  success: true,
  accessToken: 'synthetic-access',
  refreshToken: 'synthetic-refresh',
  userId: '11111111-1111-4111-8111-111111111111',
  sessionId: '22222222-2222-4222-8222-222222222222',
  email: 'synthetic@example.test',
  expiresIn: 900,
};

afterEach(() => vi.unstubAllGlobals());

async function signIn(body: Record<string, unknown>) {
  const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(Response.json(body));
  vi.stubGlobal('fetch', fetchMock);
  const auth = GameGuildAuth({
    providers: [CredentialsProvider({ apiUrl: API })],
    apiUrl: API,
    secret: randomBytes(48).toString('base64url'),
    cookies: { name: 'gg-synthetic-proof', secure: false, path: '/', httpOnly: true, sameSite: 'lax' },
  });
  const csrfResponse = await auth.handlers.GET(new Request(`${WEB}/api/auth/csrf`));
  expect(csrfResponse.status).toBe(200);
  const csrf = (await csrfResponse.json()) as { csrfToken: string };
  const cookie = csrfResponse.headers
    .getSetCookie()
    .map((value) => value.split(';', 1)[0])
    .join('; ');
  const response = await auth.handlers.POST(
    new Request(`${WEB}/api/auth/signin/credentials`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Cookie: cookie },
      body: JSON.stringify({ csrfToken: csrf.csrfToken, email: 'synthetic@example.test', password: 'synthetic-password', redirect: false }),
    }),
  );
  const cookieName = new SessionStore(resolveCookieOptions(auth.config.cookies)).getCookieName();
  const sessionCookieIssued = response.headers.getSetCookie().some((value) => value.startsWith(`${cookieName}=`));
  expect(fetchMock).toHaveBeenCalledOnce();
  expect(fetchMock.mock.calls[0][0]).toBe(`${API}/v1/auth/sign-in`);
  return { response, sessionCookieIssued };
}

describe('default credentials through real CSRF, handlers and encrypted session creation', () => {
  it.each([
    { name: 'native success', body: completed },
    { name: 'legacy success without an explicit flag', body: { ...completed, success: undefined } },
  ])('creates an ordinary session for $name', async ({ body }) => {
    const result = await signIn(body);
    expect(result.response.status).toBe(200);
    expect(result.sessionCookieIssued).toBe(true);
  });

  it.each([
    { name: 'explicitly unsuccessful response', body: { ...completed, success: false } },
    { name: 'uncompleted step-up despite populated credentials', body: { ...completed, requiresStepUp: true } },
  ])('does not issue an ordinary session for $name', async ({ body }) => {
    const result = await signIn(body);
    expect(result.sessionCookieIssued).toBe(false);
    expect(result.response.status).toBe(401);
  });

  it.each([
    { name: 'missing identity', body: { ...completed, userId: undefined } },
    { name: 'missing refresh credential', body: { ...completed, refreshToken: undefined } },
    { name: 'blank refresh credential', body: { ...completed, refreshToken: '   ' } },
  ])('reports an incomplete backend response without a session for $name', async ({ body }) => {
    const result = await signIn(body);
    expect(result.sessionCookieIssued).toBe(false);
    expect(result.response.status).toBe(503);
  });

  it('retains a limited MFA challenge rather than interpreting success:false as a terminal denial', async () => {
    const result = await signIn({ success: false, requiresMfa: true, mfaToken: 'x'.repeat(43), availableMethods: ['Totp'] });
    expect(result.sessionCookieIssued).toBe(false);
    expect(result.response.status).toBe(403);
    expect(await result.response.json()).toMatchObject({ error: 'MfaRequired', mfaToken: 'x'.repeat(43), availableMethods: ['Totp'] });
  });

  it('retains a native compliance step-up without ordinary credentials as a failed sign-in', async () => {
    const result = await signIn({ success: false, requiresStepUp: true, stepUpToken: 'synthetic-stepup', userId: completed.userId });
    expect(result.sessionCookieIssued).toBe(false);
    expect(result.response.status).toBe(401);
  });

  it('retains a validated nested legacy identity and profile', async () => {
    const result = await signIn({ ...completed, userId: undefined, user: { id: completed.userId, email: completed.email, displayName: 'Synthetic profile' } });
    expect(result.response.status).toBe(200);
    expect(result.sessionCookieIssued).toBe(true);
  });
});

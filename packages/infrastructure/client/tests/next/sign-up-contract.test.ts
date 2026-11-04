import { randomUUID } from 'node:crypto';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createSignUpAction } from '../../src/integrations/next/actions.js';
import { resolveConfig } from '../../src/integrations/next/config.js';
import { createHandlers } from '../../src/integrations/next/handlers.js';
import { AuthError, SignUpError } from '../../src/runtime/auth/errors.js';
import { AuthModule } from '../../src/generated/modules/auth.gen.js';
import type { ResolvedAuthConfig } from '../../src/runtime/auth/types.js';

const cookies = vi.hoisted(() => ({ values: new Map<string, string>(), set: vi.fn() }));
vi.mock('next/headers', () => ({
  cookies: () =>
    Promise.resolve({
      get: (name: string) => (cookies.values.has(name) ? { value: cookies.values.get(name) } : undefined),
      set: cookies.set,
    }),
}));

const userId = '11111111-1111-4111-8111-111111111111';
const tenantId = '22222222-2222-4222-8222-222222222222';
const input = { username: 'created-member', email: 'created@example.test', password: `A1!${randomUUID()}` };
const success = { userId, email: input.email, accessToken: 'synthetic-access', refreshToken: 'synthetic-refresh' };
type Mode = 'route' | 'action';

function config() {
  return resolveConfig({ providers: [], apiUrl: 'https://api.example.test', secret: 'contract-fixture-' + 's'.repeat(40), cookies: { secure: false } });
}

function respond(body: unknown, status = 200) {
  const fetcher = vi.fn<typeof fetch>().mockResolvedValue(Response.json(body, { status }));
  vi.stubGlobal('fetch', fetcher);
  return fetcher;
}

async function submit(mode: Mode, configuration: ResolvedAuthConfig, data: Record<string, unknown>) {
  if (mode === 'action') {
    try {
      await createSignUpAction(configuration)(data as Parameters<ReturnType<typeof createSignUpAction>>[0], { redirect: false });
      return { status: 200, body: {}, error: undefined, sessionCookies: cookies.set.mock.calls.length };
    } catch (error) {
      return {
        status: error instanceof AuthError ? error.status : 500,
        body: error instanceof AuthError ? error.toJSON() : {},
        error,
        sessionCookies: cookies.set.mock.calls.length,
      };
    }
  }
  const handlers = createHandlers(configuration);
  const csrf = await handlers.GET(new Request('https://web.example.test/api/auth/csrf'));
  const { csrfToken } = (await csrf.json()) as { csrfToken: string };
  const cookieHeader = csrf.headers
    .getSetCookie()
    .map((value) => value.split(';')[0])
    .join('; ');
  const response = await handlers.POST(
    new Request('https://web.example.test/api/auth/signup', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', cookie: cookieHeader },
      body: JSON.stringify({ ...data, csrfToken }),
    }),
  );
  return {
    status: response.status,
    body: (await response.json()) as Record<string, unknown>,
    error: undefined,
    sessionCookies: response.headers.getSetCookie().length,
  };
}

beforeEach(() => {
  cookies.values.clear();
  cookies.set.mockReset();
});
afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
  vi.restoreAllMocks();
});

describe.each<Mode>(['route', 'action'])('generated sign-up contract through the %s entry point', (mode) => {
  it.each([
    ['email format', { ...input, email: 'invalid-email' }],
    ['username type', { ...input, username: 123 }],
    ['password type', { ...input, password: { value: 'invalid' } }],
    ['tenant identifier', { ...input, tenantId: 'not-a-guid' }],
    ['profile field type', { ...input, firstName: 123 }],
  ])('rejects invalid %s before sending a registration', async (_name, data) => {
    const fetcher = respond(success);
    const configuration = config();
    const jwt = vi.spyOn(configuration.callbacks, 'jwt');
    const result = await submit(mode, configuration, data);
    expect(result.status).toBe(400);
    expect(fetcher).not.toHaveBeenCalled();
    expect(jwt).not.toHaveBeenCalled();
    expect(result.sessionCookies).toBe(0);
  });

  it.each([
    ['expiry type', { expiresIn: '3600' }],
    ['expiry timestamp', { accessTokenExpiresAt: 'tomorrow' }],
    ['refresh token type', { refreshToken: 42 }],
    ['subject identifier', { userId: 'not-a-guid' }],
    ['nested subject identifier', { user: { id: 'not-a-guid' } }],
    ['MFA flag type', { requiresMfa: 'false' }],
    ['missing ordinary credential', { accessToken: undefined }],
    ['blank ordinary credential', { accessToken: ' ' }],
    ['denied outcome with credentials', { success: false }],
    ['pending step-up with credentials', { requiresStepUp: true }],
  ])('does not finalize a session for an invalid successful HTTP response: %s', async (_name, invalid) => {
    respond({ ...success, ...invalid });
    const configuration = config();
    const jwt = vi.spyOn(configuration.callbacks, 'jwt');
    const result = await submit(mode, configuration, input);
    expect(result.status).toBe(503);
    expect(jwt).not.toHaveBeenCalled();
    expect(result.sessionCookies).toBe(0);
  });

  it('returns a limited MFA outcome without issuing a session', async () => {
    respond({ ...success, success: false, requiresMfa: true, mfaToken: 'A'.repeat(43) });
    const configuration = config();
    const jwt = vi.spyOn(configuration.callbacks, 'jwt');
    const result = await submit(mode, configuration, input);
    expect(result.status).toBe(403);
    expect(result.body).toMatchObject({ error: 'MfaRequired', mfaToken: 'A'.repeat(43) });
    expect(jwt).not.toHaveBeenCalled();
    expect(result.sessionCookies).toBe(0);
  });

  it('uses the generated registration method and retains accepted request fields', async () => {
    const fetcher = respond(success);
    const operation = vi.spyOn(AuthModule.prototype, 'postAuthSignUp');
    const data = {
      ...input,
      tenantId,
      firstName: 'Created',
      lastName: 'Member',
      turnstileToken: 'synthetic-captcha',
      ...(mode === 'route' ? { guestAccessToken: 'synthetic-guest-bearer' } : {}),
    };
    const result = await submit(mode, config(), data);
    expect(result.status).toBe(200);
    expect(operation).toHaveBeenCalledOnce();
    expect(fetcher).toHaveBeenCalledOnce();
    expect(fetcher.mock.calls[0]?.[0]).toBe('https://api.example.test/v1/auth/sign-up');
    expect(JSON.parse(fetcher.mock.calls[0]?.[1]?.body as string)).toEqual(data);
    expect(result.sessionCookies).toBeGreaterThan(0);
  });

  it('retains legacy profile and claim fields after validating declared fields', async () => {
    respond({
      ...success,
      roles: ['Member'],
      permissions: ['assets:read'],
      user: { id: userId, displayName: 'Created Member', profilePictureUrl: 'https://web.example.test/avatar.png' },
    });
    const configuration = config();
    const jwt = vi.spyOn(configuration.callbacks, 'jwt');
    const result = await submit(mode, configuration, input);
    expect(result.status).toBe(200);
    expect(jwt.mock.calls[0]?.[0].user).toMatchObject({
      id: userId,
      name: 'Created Member',
      image: 'https://web.example.test/avatar.png',
      roles: ['Member'],
      permissions: ['assets:read'],
    });
  });

  it('retains field errors from rejected registrations without finalizing a session', async () => {
    respond({ detail: 'Registration rejected', errors: { email: ['Already registered'] } }, 400);
    const result = await submit(mode, config(), input);
    expect(result.status).toBe(400);
    expect(result.sessionCookies).toBe(0);
    if (mode === 'action') {
      expect(result.error).toBeInstanceOf(SignUpError);
      expect(result.error).toMatchObject({ fieldErrors: { email: ['Already registered'] } });
    }
  });

  it.each(['server', 'network', 'malformed JSON'])('classifies %s failures as service unavailability', async (failure) => {
    const fetcher = vi.fn<typeof fetch>();
    if (failure === 'network') fetcher.mockRejectedValue(new TypeError('synthetic offline'));
    else if (failure === 'server') fetcher.mockResolvedValue(Response.json({ detail: 'internal upstream detail' }, { status: 503 }));
    else fetcher.mockResolvedValue(new Response('invalid-json', { headers: { 'Content-Type': 'application/json' } }));
    vi.stubGlobal('fetch', fetcher);
    const result = await submit(mode, config(), input);
    expect(result.status).toBe(503);
    expect(result.sessionCookies).toBe(0);
    expect(result.body).not.toHaveProperty('accessToken');
    expect(result.body).not.toHaveProperty('refreshToken');
  });
});

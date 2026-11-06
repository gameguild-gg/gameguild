import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthModule } from '../../src/generated/modules/auth.gen.js';
import { createSignOutAction } from '../../src/integrations/next/actions.js';
import { resolveConfig } from '../../src/integrations/next/config.js';
import { createHandlers } from '../../src/integrations/next/handlers.js';
import { CsrfStore, SessionStore, resolveCookieOptions } from '../../src/runtime/auth/cookies.js';
import { createCSRFToken } from '../../src/runtime/auth/csrf.js';
import { encodeJWT } from '../../src/runtime/auth/jwt.js';
import type { JWTPayload, ResolvedAuthConfig } from '../../src/runtime/auth/types.js';

const jar = vi.hoisted(() => ({
  values: new Map<string, string>(),
  writes: vi.fn<(name: string, value: string, options?: { maxAge?: number }) => void>(),
}));
vi.mock('next/headers', () => ({
  cookies: () =>
    Promise.resolve({
      get: (name: string) => (jar.values.has(name) ? { value: jar.values.get(name) } : undefined),
      set: (name: string, value: string, options?: { maxAge?: number }) => {
        jar.values.set(name, value);
        jar.writes(name, value, options);
      },
    }),
}));

const accessToken = 'synthetic-current-access';
const refreshToken = 'synthetic-current-refresh';
type Mode = 'route' | 'action';

function config() {
  return resolveConfig({ providers: [], apiUrl: 'https://api.example.test', secret: 'logout-contract-fixture-' + 's'.repeat(40), cookies: { secure: false } });
}

async function seedSession(configuration: ResolvedAuthConfig, patch: Record<string, unknown> = {}) {
  const payload = {
    user: { id: '11111111-1111-4111-8111-111111111111', email: 'member@example.test', name: 'Member', image: null },
    accessToken,
    refreshToken,
    accessTokenExpires: Date.now() + 60_000,
    ...patch,
  } as JWTPayload;
  const encrypted = await encodeJWT({ token: payload, secret: configuration.secret });
  const store = new SessionStore(resolveCookieOptions(configuration.cookies, false));
  store.write(encrypted, (name, value) => {
    if (value) jar.values.set(name, value);
  });
  return [...jar.values.keys()];
}

async function signOut(mode: Mode, configuration: ResolvedAuthConfig, body: Record<string, unknown> = {}) {
  if (mode === 'action') {
    await createSignOutAction(configuration)({ redirect: false });
    return { status: 200, deletions: jar.writes.mock.calls.filter(([, value, options]) => value === '' && options?.maxAge === 0).map(([name]) => name) };
  }
  const csrf = await createCSRFToken(configuration.secret);
  new CsrfStore(resolveCookieOptions(configuration.cookies, false)).write(csrf.cookie, (name, value) => jar.values.set(name, value));
  const response = await createHandlers(configuration).POST(
    new Request('https://web.example.test/api/auth/signout', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', cookie: [...jar.values].map(([name, value]) => `${name}=${value}`).join('; ') },
      body: JSON.stringify({ csrfToken: csrf.token, ...body }),
    }),
  );
  return {
    status: response.status,
    deletions: response.headers
      .getSetCookie()
      .filter((value) => /Max-Age=0/i.test(value))
      .map((value) => value.slice(0, value.indexOf('='))),
  };
}

function respond(status = 204) {
  const fetcher = vi.fn<typeof fetch>().mockResolvedValue(status === 204 ? new Response(null, { status }) : Response.json({}, { status }));
  vi.stubGlobal('fetch', fetcher);
  return fetcher;
}

beforeEach(() => {
  jar.values.clear();
  jar.writes.mockClear();
});
afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
  vi.restoreAllMocks();
  vi.useRealTimers();
});

describe.each<Mode>(['route', 'action'])('authenticated logout through the %s entry point', (mode) => {
  it('revokes the owned refresh token using the session bearer and generated operation', async () => {
    const configuration = config();
    const sessionCookies = await seedSession(configuration);
    let revoked = false;
    const fetcher = vi.fn<typeof fetch>().mockImplementation((_url, init) => {
      if (new Headers(init?.headers).get('Authorization') !== `Bearer ${accessToken}`) return Promise.resolve(Response.json({}, { status: 401 }));
      const body = JSON.parse(init?.body as string) as Record<string, unknown>;
      revoked = body.token === refreshToken;
      return Promise.resolve(new Response(null, { status: 204 }));
    });
    vi.stubGlobal('fetch', fetcher);
    const operation = vi.spyOn(AuthModule.prototype, 'postAuthTokensRevoke');

    const result = await signOut(mode, configuration);

    expect(result.status).toBe(200);
    expect(revoked).toBe(true);
    expect(operation).toHaveBeenCalledExactlyOnceWith({ token: refreshToken });
    expect(fetcher).toHaveBeenCalledOnce();
    expect(fetcher.mock.calls[0]?.[0]).toBe('https://api.example.test/v1/auth/tokens:revoke');
    const request = fetcher.mock.calls[0]?.[1];
    expect(request?.method).toBe('POST');
    expect(new Headers(request?.headers).get('Content-Type')).toBe('application/json');
    expect(JSON.parse(request?.body as string)).toEqual({ token: refreshToken });
    expect(result.deletions).toEqual(expect.arrayContaining(sessionCookies));
  });

  it.each([undefined, null, '', ' ', 42, { value: 'invalid' }])('does not send a missing or malformed access credential: %j', async (invalid) => {
    const configuration = config();
    const sessionCookies = await seedSession(configuration, { accessToken: invalid });
    const fetcher = respond();
    const result = await signOut(mode, configuration);
    expect(fetcher).not.toHaveBeenCalled();
    expect(result.status).toBe(200);
    expect(result.deletions).toEqual(expect.arrayContaining(sessionCookies));
  });

  it.each([undefined, null, '', ' ', 42, { value: 'invalid' }])(
    'does not turn an invalid refresh credential into a revocation request: %j',
    async (invalid) => {
      const configuration = config();
      const sessionCookies = await seedSession(configuration, { refreshToken: invalid });
      const fetcher = respond();
      const result = await signOut(mode, configuration);
      expect(fetcher).not.toHaveBeenCalled();
      expect(result.status).toBe(200);
      expect(result.deletions).toEqual(expect.arrayContaining(sessionCookies));
    },
  );

  it.each([401, 403, 503, 'network'] as const)('clears local cookies when revocation fails: %s', async (failure) => {
    const configuration = config();
    const sessionCookies = await seedSession(configuration);
    const fetcher = failure === 'network' ? vi.fn<typeof fetch>().mockRejectedValue(new TypeError('synthetic offline')) : respond(failure);
    vi.stubGlobal('fetch', fetcher);
    const result = await signOut(mode, configuration);
    expect(result.status).toBe(200);
    expect(fetcher).toHaveBeenCalledOnce();
    expect(result.deletions).toEqual(expect.arrayContaining(sessionCookies));
  });

  it('never refreshes or rotates tokens during logout, including when the access credential is expired', async () => {
    const configuration = config();
    const sessionCookies = await seedSession(configuration, { accessTokenExpires: Date.now() - 60_000 });
    const fetcher = respond(401);
    const result = await signOut(mode, configuration);
    expect(fetcher).toHaveBeenCalledOnce();
    expect(fetcher.mock.calls[0]?.[0]).toBe('https://api.example.test/v1/auth/tokens:revoke');
    expect(result.deletions).toEqual(expect.arrayContaining(sessionCookies));
  });

  it('bounds a stalled revocation to five seconds before clearing local cookies', async () => {
    const configuration = config();
    const sessionCookies = await seedSession(configuration);
    let signalStarted: (() => void) | undefined;
    const started = new Promise<void>((resolve) => {
      signalStarted = resolve;
    });
    // Simulate a fetch adapter that never settles and ignores abort signals.
    const fetcher = vi.fn<typeof fetch>().mockImplementation(() => {
      signalStarted?.();
      return new Promise<Response>(() => undefined);
    });
    vi.stubGlobal('fetch', fetcher);
    vi.useFakeTimers();
    let completed: Awaited<ReturnType<typeof signOut>> | undefined;
    const pending = signOut(mode, configuration).then((result) => {
      completed = result;
    });
    await started;
    await vi.advanceTimersByTimeAsync(4_999);
    expect(completed).toBeUndefined();
    await vi.advanceTimersByTimeAsync(1);
    expect(completed?.status).toBe(200);
    expect(completed?.deletions).toEqual(expect.arrayContaining(sessionCookies));
    expect(fetcher).toHaveBeenCalledOnce();
    expect(fetcher.mock.calls[0]?.[1]?.signal?.aborted).toBe(true);
    await pending;
  });

  it.each(['missing', 'tampered', 'expired'] as const)('does not call the API for a %s session', async (state) => {
    const configuration = config();
    if (state === 'tampered') jar.values.set('__me.session-token', 'invalid-encrypted-session');
    if (state === 'expired') await seedSession(configuration, { exp: Math.floor(Date.now() / 1000) - 3600 });
    const fetcher = respond();
    const result = await signOut(mode, configuration);
    expect(fetcher).not.toHaveBeenCalled();
    expect(result.status).toBe(200);
    expect(result.deletions).toContain('__me.session-token');
  });

  it('reads and removes every chunk of an encrypted session', async () => {
    const configuration = config();
    const sessionCookies = await seedSession(configuration, {
      user: { id: '11111111-1111-4111-8111-111111111111', email: 'member@example.test', name: 'x'.repeat(9000), image: null },
    });
    expect(sessionCookies.length).toBeGreaterThan(1);
    const fetcher = respond();
    const result = await signOut(mode, configuration);
    expect(new Headers(fetcher.mock.calls[0]?.[1]?.headers).get('Authorization')).toBe(`Bearer ${accessToken}`);
    expect(result.deletions).toEqual(expect.arrayContaining(sessionCookies));
  });

  it('keeps token transport diagnostics silent in development', async () => {
    vi.stubEnv('NODE_ENV', 'development');
    await seedSession(config());
    respond();
    const logs = (['log', 'info', 'warn', 'error', 'debug', 'group', 'groupCollapsed', 'groupEnd'] as const).map((method) =>
      vi.spyOn(console, method).mockImplementation(() => {}),
    );
    await signOut(mode, config());
    for (const log of logs) expect(log).not.toHaveBeenCalled();
  });
});

it('ignores caller-supplied tokens and actor fields on the logout route', async () => {
  const configuration = config();
  await seedSession(configuration);
  const fetcher = respond();
  await signOut('route', configuration, {
    accessToken: 'forged-access',
    token: 'forged-refresh',
    refreshToken: 'forged-refresh',
    userId: 'another-user',
    ipAddress: '203.0.113.1',
  });
  const request = fetcher.mock.calls[0]?.[1];
  expect(new Headers(request?.headers).get('Authorization')).toBe(`Bearer ${accessToken}`);
  expect(JSON.parse(request?.body as string)).toEqual({ token: refreshToken });
});

it('rejects invalid CSRF before attempting revocation or deleting the session', async () => {
  const configuration = config();
  await seedSession(configuration);
  const fetcher = respond();
  const result = await signOut('route', configuration, { csrfToken: 'invalid-csrf' });
  expect(result.status).toBe(403);
  expect(fetcher).not.toHaveBeenCalled();
  expect(result.deletions).toEqual([]);
});

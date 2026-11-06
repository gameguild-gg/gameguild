import { afterEach, expect, it, vi } from 'vitest';
import { createSignUpAction } from '../../src/integrations/next/actions.js';
import { createHandlers } from '../../src/integrations/next/handlers.js';
import type { ResolvedAuthConfig } from '../../src/runtime/auth/types.js';

vi.mock('next/headers', () => ({ cookies: async () => null }));
vi.mock('../../src/runtime/auth/csrf.js', () => ({
  validateCSRFToken: vi.fn(async () => true), createCSRFToken: vi.fn(),
}));

const config: ResolvedAuthConfig = {
  providers: [], apiUrl: 'http://api.example', secret: crypto.randomUUID(),
  callbacks: {
    jwt: async ({ token }) => token, session: async ({ session }) => session,
    signIn: async () => true, authorized: async () => false,
    redirect: async ({ url, baseUrl }) => new URL(url, baseUrl).href,
  },
  pages: {},
  cookies: { name: '__me', secure: false, sameSite: 'lax', path: '/', maxAge: 2592000, httpOnly: true },
  maxAge: 2592000, updateAge: 0, basePath: '/api/auth', debug: false,
  trustHost: false, tenantHeader: 'X-Tenant-Id',
};

afterEach(() => vi.unstubAllGlobals());

it.each(['action', 'handler'])('forwards the fresh sign-up CAPTCHA to the API exactly once through %s', async (surface) => {
  const credentials = {
    username: 'reader', email: 'reader@example.invalid', password: crypto.randomUUID(),
    turnstileToken: crypto.randomUUID(),
  };
  const requests: { url: string; body: Record<string, unknown> }[] = [];
  vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
    requests.push({ url, body: JSON.parse(String(init?.body)) as Record<string, unknown> });
    // Stop at the API response: no session should be created for a refused sign-up.
    return Response.json({ message: 'Account unavailable' }, { status: 400 });
  }));

  if (surface === 'action') {
    await expect(createSignUpAction(config)(credentials, { redirect: false })).rejects.toThrow('Account unavailable');
  } else {
    const response = await createHandlers(config).POST(new Request('http://web.example/api/auth/signup', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(credentials),
    }));
    expect(response.status).toBe(400);
  }

  expect(requests).toEqual([{ url: 'http://api.example/v1/auth/sign-up', body: credentials }]);
});

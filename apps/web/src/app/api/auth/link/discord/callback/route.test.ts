import { NextRequest } from 'next/server';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  LINK_STATE_COOKIE_NAME,
  signLinkStatePayload,
} from '@/lib/auth/oauth-link-state';

const SECRET = 'test-secret-min-32-chars-long-ok';

const mocks = vi.hoisted(() => {
  const store = new Map<string, string>();
  return {
    cookieStore: {
      store,
      get: (name: string) => (store.has(name) ? { name, value: store.get(name) } : undefined),
      delete: vi.fn((name: string) => store.delete(name)),
    },
    postAuthExternalLoginsDiscordLinkCallback: vi.fn(),
  };
});

vi.mock('@/auth', () => ({
  authConfig: { secret: 'test-secret-min-32-chars-long-ok' },
  getToken: vi.fn().mockResolvedValue('access-token'),
}));

vi.mock('next/headers', () => ({
  cookies: vi.fn().mockResolvedValue(mocks.cookieStore),
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn().mockReturnValue({}),
  GeneratedApi: {
    AuthModule: class {
      postAuthExternalLoginsDiscordLinkCallback = mocks.postAuthExternalLoginsDiscordLinkCallback;
    },
  },
}));

import { GET } from './route';

const STATE = 'state-abc';

async function primeCookie(overrides: Record<string, unknown> = {}) {
  mocks.cookieStore.store.set(
    LINK_STATE_COOKIE_NAME,
    await signLinkStatePayload(
      {
        state: STATE,
        flow: 'link',
        exp: Date.now() + 60_000,
        locale: 'pt-BR',
        ...overrides,
      },
      SECRET,
    ),
  );
}

function request(query: string) {
  return new NextRequest(`http://localhost/api/auth/link/discord/callback${query}`);
}

describe('Discord link callback route', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.cookieStore.store.clear();
    mocks.postAuthExternalLoginsDiscordLinkCallback.mockResolvedValue({ ok: true, data: undefined });
  });

  it('exchanges the code with the bearer client and redirects to settings?linked=discord', async () => {
    await primeCookie();

    const response = await GET(request(`?code=auth-code&state=${STATE}`));

    expect(mocks.postAuthExternalLoginsDiscordLinkCallback).toHaveBeenCalledWith({
      code: 'auth-code',
      state: STATE,
      redirectUri: 'http://localhost/api/auth/link/discord/callback',
    });
    expect(response.status).toBe(302);
    expect(response.headers.get('location')).toBe(
      'http://localhost/pt-BR/workspace/settings/account?linked=discord',
    );
    expect(mocks.cookieStore.delete).toHaveBeenCalledWith(LINK_STATE_COOKIE_NAME);
  });

  it('rejects a tampered cookie without calling the backend', async () => {
    mocks.cookieStore.store.set(LINK_STATE_COOKIE_NAME, 'garbage.deadbeef');

    const response = await GET(request(`?code=auth-code&state=${STATE}`));

    expect(mocks.postAuthExternalLoginsDiscordLinkCallback).not.toHaveBeenCalled();
    expect(response.status).toBe(302);
    // Tampered cookie → payload unreadable → locale unknown → default locale.
    expect(response.headers.get('location')).toBe(
      'http://localhost/en-US/workspace/settings/account?error=state_mismatch',
    );
    expect(mocks.cookieStore.delete).toHaveBeenCalledWith(LINK_STATE_COOKIE_NAME);
  });

  it('rejects a state query that does not match the signed cookie', async () => {
    await primeCookie();

    const response = await GET(request('?code=auth-code&state=attacker-state'));

    expect(mocks.postAuthExternalLoginsDiscordLinkCallback).not.toHaveBeenCalled();
    expect(response.headers.get('location')).toContain('error=state_mismatch');
  });

  it('maps a backend 409 to ?error=conflict', async () => {
    await primeCookie();
    mocks.postAuthExternalLoginsDiscordLinkCallback.mockResolvedValue({
      ok: false,
      error: { status: 409, message: 'conflict' },
    });

    const response = await GET(request(`?code=auth-code&state=${STATE}`));

    expect(response.headers.get('location')).toBe(
      'http://localhost/pt-BR/workspace/settings/account?error=conflict',
    );
  });
});

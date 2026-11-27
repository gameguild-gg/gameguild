import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
}));

vi.mock('next/navigation', () => ({ redirect: mocks.redirect }));

import VerifyEmailRedirectPage from './page';

// A stand-in with the real token's length (32), deliberately not hex: a hex
// literal of that length is what secret scanners look for.
const TOKEN = 'token-'.padEnd(32, '0');

async function renderPage(query: Record<string, string | string[] | undefined>) {
  return VerifyEmailRedirectPage({
    params: Promise.resolve({ locale: 'en-US' }),
    searchParams: Promise.resolve(query),
  } as never);
}

describe('/verify-email redirect (email verification link target, #1298 J1)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('forwards the emailed token to the verification page', async () => {
    await expect(renderPage({ token: TOKEN })).rejects.toThrow(`redirect:/verify?token=${TOKEN}`);
  });

  it('trims the token before forwarding it', async () => {
    await expect(renderPage({ token: `  ${TOKEN}\n` })).rejects.toThrow(`redirect:/verify?token=${TOKEN}`);
  });

  it('encodes a token that carries URL syntax instead of splicing it into the target', async () => {
    await expect(renderPage({ token: 'a&next=//evil.example' })).rejects.toThrow(
      'redirect:/verify?token=a%26next%3D%2F%2Fevil.example',
    );
  });

  it('drops everything else from the incoming query', async () => {
    await expect(renderPage({ token: TOKEN, email: 'someone@example.com', redirectTo: '//evil.example' })).rejects.toThrow(
      `redirect:/verify?token=${TOKEN}`,
    );
  });

  it.each([
    ['an absent token', {}],
    ['an empty token', { token: '' }],
    ['a blank token', { token: '   ' }],
    ['a repeated token parameter', { token: ['a', 'b'] }],
    ['a token longer than any real one', { token: 'x'.repeat(257) }],
  ])('goes to the bare verification page for %s', async (_label, query) => {
    await expect(renderPage(query)).rejects.toThrow('redirect:/verify');
  });
});

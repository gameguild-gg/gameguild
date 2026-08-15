import '@testing-library/jest-dom/vitest';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
}));

vi.mock('next/navigation', () => ({ redirect: mocks.redirect }));

import AuthErrorLegacyRedirectPage from './page';

describe('legacy /auth-error redirect', () => {
  it('forwards the error code to sign-in', async () => {
    await expect(
      AuthErrorLegacyRedirectPage({
        params: Promise.resolve({ locale: 'en-US' }),
        searchParams: Promise.resolve({ error: 'access_denied' }),
      } as never),
    ).rejects.toThrow('redirect:/en-US/sign-in?error=access_denied');
  });

  it('redirects to sign-in without query when no error code is present', async () => {
    await expect(
      AuthErrorLegacyRedirectPage({
        params: Promise.resolve({ locale: 'pt-BR' }),
        searchParams: Promise.resolve({}),
      } as never),
    ).rejects.toThrow('redirect:/pt-BR/sign-in');
  });

  it('encodes unsafe error values', async () => {
    await expect(
      AuthErrorLegacyRedirectPage({
        params: Promise.resolve({ locale: 'en-US' }),
        searchParams: Promise.resolve({ error: 'a b&c=d' }),
      } as never),
    ).rejects.toThrow(
      `redirect:/en-US/sign-in?error=${encodeURIComponent('a b&c=d')}`,
    );
  });
});

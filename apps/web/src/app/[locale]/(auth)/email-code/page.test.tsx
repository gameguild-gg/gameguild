import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  redirect: vi.fn((args: unknown) => {
    throw new Error(`redirect:${JSON.stringify(args)}`);
  }),
  resolveRedirect: vi.fn((value: string | undefined) => value ?? '/'),
}));

vi.mock('@/i18n/navigation', () => ({ redirect: mocks.redirect }));
vi.mock('@/lib/auth/cross-domain-auth', () => ({
  resolveAllowedAuthRedirect: mocks.resolveRedirect,
}));

import EmailCodeRedirectPage from './page';

describe('legacy /email-code redirect (#1236: code is not a sign-in method)', () => {
  it('lands on the single passwordless entry when no redirect is supplied', async () => {
    await expect(
      EmailCodeRedirectPage({
        params: Promise.resolve({ locale: 'en-US' }),
        searchParams: Promise.resolve({}),
      } as never),
    ).rejects.toThrow(
      'redirect:{"href":"/magic-link","locale":"en-US"}',
    );
    expect(mocks.resolveRedirect).toHaveBeenCalledWith(undefined);
  });

  it('preserves a validated redirect destination through to /magic-link', async () => {
    await expect(
      EmailCodeRedirectPage({
        params: Promise.resolve({ locale: 'pt-BR' }),
        searchParams: Promise.resolve({ redirectTo: '/workspace' }),
      } as never),
    ).rejects.toThrow(
      'redirect:{"href":"/magic-link?redirectTo=%2Fworkspace","locale":"pt-BR"}',
    );
    expect(mocks.resolveRedirect).toHaveBeenCalledWith('/workspace');
  });

  it('drops the default "/" redirect instead of re-encoding it', async () => {
    await expect(
      EmailCodeRedirectPage({
        params: Promise.resolve({ locale: 'en-US' }),
        searchParams: Promise.resolve({}),
      } as never),
    ).rejects.toThrow('redirect:{"href":"/magic-link","locale":"en-US"}');
  });
});

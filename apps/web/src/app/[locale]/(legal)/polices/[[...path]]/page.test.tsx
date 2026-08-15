import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
}));

vi.mock('next/navigation', () => ({ redirect: mocks.redirect }));

import LegacyPolicesRedirectPage from './page';

function props(locale: string, path?: string[]) {
  return { params: Promise.resolve({ locale, path }) } as never;
}

describe('legacy /polices (typo) catch-all redirect', () => {
  it.each([
    [undefined, '/en-US/legal'],
    [['privacy'], '/en-US/legal/privacy'],
    [['cookies'], '/en-US/legal/cookies'],
  ])('%j → %s', async (path, expected) => {
    await expect(LegacyPolicesRedirectPage(props('en-US', path))).rejects.toThrow(`redirect:${expected}`);
  });
});

import { describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
}));

import LegacyLaunchPadRedirectPage from './page';

function props(path?: string[]) {
  return { params: Promise.resolve({ locale: 'en-US', path }) } as never;
}

describe('legacy /dashboard/launch-pad catch-all redirect', () => {
  it.each([
    [undefined, '/en-US/dashboard/community/launch-pad'],
    [['events'], '/en-US/dashboard/community/launch-pad/events'],
    [['applications'], '/en-US/dashboard/community/launch-pad/applications'],
  ])('%j → %s', async (path, expected) => {
    await expect(LegacyLaunchPadRedirectPage(props(path))).rejects.toThrow(`redirect:${expected}`);
  });
});

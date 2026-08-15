import { describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
}));

import LegacyTestingLabRedirectPage from './page';

function props(path?: string[]) {
  return { params: Promise.resolve({ locale: 'en-US', path }) } as never;
}

describe('legacy /dashboard/testing-lab catch-all redirect', () => {
  it.each([
    [undefined, '/en-US/dashboard/community/testing-lab'],
    [['events', 'evt-1', 'schedule'], '/en-US/dashboard/community/testing-lab/events/evt-1/schedule'],
    [['settings', 'access'], '/en-US/dashboard/community/testing-lab/settings/access'],
  ])('%j → %s', async (path, expected) => {
    await expect(LegacyTestingLabRedirectPage(props(path))).rejects.toThrow(`redirect:${expected}`);
  });
});

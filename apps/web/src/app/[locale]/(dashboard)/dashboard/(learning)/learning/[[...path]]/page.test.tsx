import { describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
}));

import LegacyLearningRedirectPage from './page';

function props(path?: string[]) {
  return { params: Promise.resolve({ locale: 'en-US', path }) } as never;
}

describe('legacy /dashboard/learning catch-all redirect', () => {
  it.each([
    [undefined, '/en-US/dashboard/platform/learning'],
    [[], '/en-US/dashboard/platform/learning'],
    [['courses'], '/en-US/dashboard/platform/learning/courses'],
    [['courses', 'course-1', 'overview'], '/en-US/dashboard/platform/learning/courses/course-1/overview'],
  ])('%j → %s', async (path, expected) => {
    await expect(LegacyLearningRedirectPage(props(path))).rejects.toThrow(`redirect:${expected}`);
  });
});

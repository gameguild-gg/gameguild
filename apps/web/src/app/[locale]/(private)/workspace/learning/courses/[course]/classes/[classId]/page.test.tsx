import { describe, expect, it, vi } from 'vitest';

const { redirect } = vi.hoisted(() => ({
  redirect: vi.fn((path: string) => {
    throw new Error(`redirect:${path}`);
  }),
}));

vi.mock('next/navigation', () => ({ redirect }));

import ClassWorkspacePage from './page';

describe('ClassWorkspacePage', () => {
  it('opens the class schedule workspace by default', async () => {
    await expect(
      ClassWorkspacePage({
        params: Promise.resolve({ locale: 'en-US', course: 'advanced-game-ai', classId: 'cohort-1' }),
      }),
    ).rejects.toThrow('redirect:/workspace/learning/courses/advanced-game-ai/classes/cohort-1/schedule');
  });
});

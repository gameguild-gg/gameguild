import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getCourse: vi.fn(),
  getCourseRouteParam: vi.fn(),
  redirect: vi.fn((input: unknown) => {
    throw new Error(`redirect:${JSON.stringify(input)}`);
  }),
}));

vi.mock('@/i18n/navigation', () => ({ redirect: mocks.redirect }));
vi.mock('@/lib/learning', () => ({ getCourse: mocks.getCourse }));
vi.mock('@/lib/learning/course-route', () => ({
  getCourseRouteParam: mocks.getCourseRouteParam,
}));

import CoursePage from './page';

describe('workspace course root redirect', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getCourseRouteParam.mockReturnValue('game-ai-by-author');
  });

  it('uses locale-aware navigation for a resolved canonical route', async () => {
    const course = { id: 'course-1', slug: 'game-ai' };
    mocks.getCourse.mockResolvedValue(course);

    await expect(
      CoursePage({
        params: Promise.resolve({ locale: 'en-US', course: 'course-1' }),
      }),
    ).rejects.toThrow('redirect:');

    expect(mocks.getCourseRouteParam).toHaveBeenCalledWith(course);
    expect(mocks.redirect).toHaveBeenCalledWith({
      href: '/workspace/learning/courses/game-ai-by-author/overview',
      locale: 'en-US',
    });
  });

  it('preserves a non-default locale and unresolved route identifier', async () => {
    mocks.getCourse.mockResolvedValue(null);

    await expect(
      CoursePage({
        params: Promise.resolve({ locale: 'pt-BR', course: 'missing course' }),
      }),
    ).rejects.toThrow('redirect:');

    expect(mocks.getCourseRouteParam).not.toHaveBeenCalled();
    expect(mocks.redirect).toHaveBeenCalledWith({
      href: '/workspace/learning/courses/missing%20course/overview',
      locale: 'pt-BR',
    });
  });
});

import { beforeEach, describe, expect, it, vi } from 'vitest';

const { getCourseBySlugMock, unstableCacheMock } = vi.hoisted(() => ({
  getCourseBySlugMock: vi.fn(),
  unstableCacheMock: vi.fn((callback: (...args: unknown[]) => unknown) => callback),
}));

vi.mock('next/cache', () => ({ unstable_cache: unstableCacheMock }));
vi.mock('@/lib/courses/services/course.service', () => ({ getCourseBySlug: getCourseBySlugMock }));

describe('getCachedPublicCourseBySlug', () => {
  beforeEach(() => {
    getCourseBySlugMock.mockReset();
  });

  it('shares a slug-keyed successful lookup with a five-minute revalidation interval', async () => {
    vi.resetModules();
    const { getCachedPublicCourseBySlug } = await import('./cached-public-course');

    expect(unstableCacheMock).toHaveBeenCalledWith(
      expect.any(Function),
      ['public-course-by-slug'],
      { revalidate: 300, tags: ['public-course-metadata'] },
    );

    const result = { success: true, data: { slug: 'game-design' } };
    getCourseBySlugMock.mockResolvedValue(result);

    await expect(getCachedPublicCourseBySlug('game-design')).resolves.toBe(result);
    expect(getCourseBySlugMock).toHaveBeenCalledWith('game-design');
  });

  it('returns failed lookups without resolving the cache callback', async () => {
    vi.resetModules();
    const { getCachedPublicCourseBySlug } = await import('./cached-public-course');
    const result = { success: false, reason: 'unavailable', error: 'API offline' };
    getCourseBySlugMock.mockResolvedValue(result);

    await expect(getCachedPublicCourseBySlug('game-design')).resolves.toBe(result);
    expect(getCourseBySlugMock).toHaveBeenCalledTimes(1);
  });

  it('converts unexpected cache failures to an unavailable lookup', async () => {
    vi.resetModules();
    const { getCachedPublicCourseBySlug } = await import('./cached-public-course');
    getCourseBySlugMock.mockRejectedValue(new Error('cache failed'));

    await expect(getCachedPublicCourseBySlug('game-design')).resolves.toEqual({
      success: false,
      reason: 'unavailable',
      error: 'cache failed',
    });
  });
});

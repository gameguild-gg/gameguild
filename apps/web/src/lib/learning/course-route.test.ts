import { describe, expect, it } from 'vitest';
import { getCourseLookupSlug, getCourseRouteParam } from './course-route';

describe('course route helpers', () => {
  it('builds dashboard course params as course-slug-by-author', () => {
    expect(
      getCourseRouteParam({
        id: 'course-1',
        slug: 'ai-for-boss-encounters',
        creatorName: 'Ada Lovelace',
      }),
    ).toBe('ai-for-boss-encounters-by-ada-lovelace');
  });

  it('extracts the canonical API slug from a slug-by-author route param', () => {
    expect(getCourseLookupSlug('ai-for-boss-encounters-by-ada-lovelace')).toBe('ai-for-boss-encounters');
  });

  it('keeps legacy plain slug params resolvable', () => {
    expect(getCourseLookupSlug('ai-for-boss-encounters')).toBe('ai-for-boss-encounters');
  });
});

import { unstable_cache } from 'next/cache';
import { getCourseBySlug, type CourseLookupResult } from '@/lib/courses/services/course.service';

class UncacheableCourseLookup extends Error {
  constructor(readonly result: CourseLookupResult) {
    super(result.error ?? 'Public course lookup failed');
  }
}

const getCachedSuccessfulCourseBySlug = unstable_cache(
  async (slug: string): Promise<CourseLookupResult> => {
    const result = await getCourseBySlug(slug);

    // A temporary API outage or a newly published slug must not be cached as a
    // missing course for five minutes. Next only caches resolved values.
    if (!result.success || !result.data) {
      throw new UncacheableCourseLookup(result);
    }

    return result;
  },
  ['public-course-by-slug'],
  { revalidate: 300, tags: ['public-course-metadata'] },
);

/** Share a bounded public-course lookup between the page and its SEO metadata. */
export async function getCachedPublicCourseBySlug(slug: string): Promise<CourseLookupResult> {
  try {
    return await getCachedSuccessfulCourseBySlug(slug);
  } catch (error) {
    if (error instanceof UncacheableCourseLookup) {
      return error.result;
    }

    return {
      success: false,
      reason: 'unavailable',
      error: error instanceof Error ? error.message : 'Public course lookup failed',
    };
  }
}

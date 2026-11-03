import { beforeEach, describe, expect, it, vi } from 'vitest';

const { getCachedPublicCourseBySlugMock } = vi.hoisted(() => ({
  getCachedPublicCourseBySlugMock: vi.fn(),
}));

vi.mock('@/lib/courses/cached-public-course', () => ({
  getCachedPublicCourseBySlug: getCachedPublicCourseBySlugMock,
}));
vi.mock('@/lib/courses/actions/enrollment.actions', () => ({
  getProductsContainingCourse: vi.fn(),
}));
vi.mock('@/lib/courses/services/course-viewer-access', () => ({
  getCourseViewerAccess: vi.fn(),
}));
vi.mock('@/components/courses/course/course-landing-page', () => ({
  CourseLandingPage: () => null,
}));

import { generateMetadata } from './page';
import { SITE_BASE_URL } from '@/lib/seo';

describe('course page metadata', () => {
  beforeEach(() => {
    getCachedPublicCourseBySlugMock.mockReset();
  });

  it('uses the course title, canonical, and hreflang alternates', async () => {
    getCachedPublicCourseBySlugMock.mockResolvedValue({
      success: true,
      data: {
        title: 'Preview Course',
        description: 'The course description.',
        thumbnail: 'https://example.test/preview.png',
      },
    });

    const metadata = await generateMetadata({ params: Promise.resolve({ course: 'preview-course' }) });

    expect(getCachedPublicCourseBySlugMock).toHaveBeenCalledWith('preview-course');
    expect((metadata.title as { absolute: string }).absolute).toBe('Preview Course — GameGuild');
    expect(metadata.description).toBe('The course description.');
    expect((metadata.openGraph as { images?: string[] }).images).toEqual(['https://example.test/preview.png']);
    expect(metadata.alternates?.canonical).toBe(`${SITE_BASE_URL}/courses/preview-course`);
    const languages = metadata.alternates?.languages as Record<string, string>;
    expect(languages['en-US']).toBe(`${SITE_BASE_URL}/courses/preview-course`);
    expect(languages['pt-BR']).toBe(`${SITE_BASE_URL}/pt-BR/courses/preview-course`);
    expect(languages['x-default']).toBe(`${SITE_BASE_URL}/courses/preview-course`);
  });
});

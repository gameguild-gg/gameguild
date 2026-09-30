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

describe('course page metadata', () => {
  beforeEach(() => {
    getCachedPublicCourseBySlugMock.mockReset();
  });

  it('uses the selected course title without duplicating the layout brand suffix', async () => {
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
    expect(metadata).toMatchObject({
      title: 'Preview Course',
      description: 'The course description.',
      openGraph: {
        title: 'Preview Course',
        description: 'The course description.',
        images: ['https://example.test/preview.png'],
      },
    });
  });
});

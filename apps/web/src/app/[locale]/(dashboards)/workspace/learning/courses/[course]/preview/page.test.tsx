import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const getCourseMock = vi.fn();
const getCourseContentMock = vi.fn();
const courseLandingPageMock = vi.fn();

vi.mock('@/lib/learning', () => ({
  getCourse: getCourseMock,
  getCourseContent: getCourseContentMock,
}));

vi.mock('@/components/courses/course/course-landing-page', () => ({
  CourseLandingPage: ({ course, viewerAccess }: { course: unknown; viewerAccess: unknown }) => {
    courseLandingPageMock(course, viewerAccess);
    return <div data-testid="course-landing-preview">Preview route rendered</div>;
  },
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning',
  notFound: vi.fn(() => {
    throw new Error('not-found');
  }),
}));

const { default: PreviewPage } = await import('./page');

describe('CoursePreviewPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the public landing page shell from authenticated dashboard course data', async () => {
    getCourseMock.mockResolvedValue({
      id: 'course-123',
      title: 'Draft Boss AI Course',
      description: 'Draft storefront copy.',
      metadata: '{"landingFaq":[]}',
      slug: 'draft-boss-ai-course',
      status: 'draft',
      visibility: 'private',
      thumbnail: 'https://example.com/cover.png',
      videoShowcaseUrl: 'https://example.com/trailer.mp4',
      estimatedHours: 18,
      category: 'AI',
      difficulty: 'Intermediate',
      skillsRequired: 'Behavior trees',
      skillsProvided: 'Combat AI tuning',
      enrollmentStatus: 'Open',
      maxEnrollments: 24,
      enrollmentDeadline: null,
      currentEnrollments: 3,
      averageRating: 4.7,
      totalRatings: 12,
      isEnrollmentOpen: true,
      deliveryMode: 'on-demand',
      pricingModel: 'paid',
      features: {
        hasClasses: true,
        hasRecordings: true,
        hasSchedule: true,
        hasOnDemandContent: true,
        hasPricing: true,
        hasCertificate: true,
        hasAssessments: true,
        hasDiscussions: true,
      },
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-02T00:00:00.000Z',
    });
    getCourseContentMock.mockResolvedValue({
      items: [
        {
          id: 'lesson-1',
          title: 'Boss behavior states',
          description: 'Map the readable AI loop.',
          type: 'Lesson',
          parentId: null,
          duration: 45,
        },
      ],
      total: 1,
    });

    render(await PreviewPage({ params: Promise.resolve({ course: 'course-123' }) }));

    expect(screen.getByTestId('course-landing-preview')).toBeInTheDocument();
    expect(courseLandingPageMock).toHaveBeenCalledWith(
      expect.objectContaining({
        id: 'course-123',
        title: 'Draft Boss AI Course',
        slug: 'draft-boss-ai-course',
        status: 'Draft',
        visibility: 'Private',
        programContents: [
          expect.objectContaining({
            id: 'lesson-1',
            title: 'Boss behavior states',
            estimatedMinutes: 45,
          }),
        ],
      }),
      { state: 'has-access' },
    );
  });
});

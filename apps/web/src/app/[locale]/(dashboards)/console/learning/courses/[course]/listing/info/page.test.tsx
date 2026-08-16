import '@testing-library/jest-dom/vitest';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { StrictMode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { CourseDetails } from '@/lib/learning/types';
import ListingInfoPage from './page';

const fetchCourseMock = vi.fn();
const updateCourseMock = vi.fn();
const refreshMock = vi.fn();
const replaceMock = vi.fn();

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning',
  useRouter: () => ({
    refresh: refreshMock,
    replace: replaceMock,
  }),
}));

vi.mock('@/lib/learning/actions', () => ({
  fetchCourse: (...args: unknown[]) => fetchCourseMock(...args),
  updateCourse: (...args: unknown[]) => updateCourseMock(...args),
}));

const courseFixture: CourseDetails = {
  id: 'course-1',
  creatorId: 'creator-1',
  creatorHandle: 'instructor-one',
  title: 'AI for Boss Encounters',
  description: 'Build production-ready enemy behavior for portfolio projects.',
  metadata: null,
  slug: 'ai-for-boss-encounters',
  status: 'draft',
  visibility: 'private',
  thumbnail: 'https://cdn.gameguild.gg/courses/boss-ai.jpg',
  videoShowcaseUrl: null,
  estimatedHours: 32,
  passingScore: 60,
  category: 'GameDevelopment',
  difficulty: 'Advanced',
  skillsRequired: 'Unity basics',
  skillsProvided: 'Behavior trees, Combat pacing',
  enrollmentStatus: 'Open',
  maxEnrollments: null,
  enrollmentDeadline: null,
  currentEnrollments: 0,
  averageRating: 0,
  totalRatings: 0,
  isEnrollmentOpen: true,
  deliveryMode: 'on-demand',
  pricingModel: 'free',
  features: {
    hasClasses: false,
    hasRecordings: false,
    hasSchedule: false,
    hasOnDemandContent: true,
    hasPricing: false,
    hasCertificate: true,
    hasAssessments: true,
    hasDiscussions: true,
  },
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-02T00:00:00.000Z',
};

describe('ListingInfoPage', () => {
  beforeEach(() => {
    fetchCourseMock.mockReset();
    updateCourseMock.mockReset();
    refreshMock.mockReset();
    replaceMock.mockReset();
    fetchCourseMock.mockResolvedValue(courseFixture);
    updateCourseMock.mockResolvedValue({ success: true, data: null });
  });

  it('loads the course and saves storefront identity fields through updateCourse', async () => {
    render(<ListingInfoPage params={Promise.resolve({ locale: 'en-US', course: 'ai-for-boss-encounters-by-instructor-one' })} />);

    const titleInput = await screen.findByLabelText(/course title/i);
    fireEvent.change(titleInput, { target: { value: 'Advanced Encounter AI' } });
    fireEvent.change(screen.getByLabelText(/url slug/i), { target: { value: 'advanced-encounter-ai' } });
    fireEvent.change(screen.getByLabelText(/^description$/i), {
      target: { value: 'Design readable enemy behavior loops for shipped game prototypes.' },
    });
    fireEvent.change(screen.getByLabelText(/estimated hours/i), { target: { value: '48' } });
    fireEvent.change(screen.getByLabelText(/passing score/i), { target: { value: '70' } });
    fireEvent.change(screen.getByLabelText(/skills students will learn/i), {
      target: { value: 'Boss AI, Telemetry tuning' },
    });
    fireEvent.change(screen.getByLabelText(/prerequisites/i), {
      target: { value: 'Unity basics, C# fundamentals' },
    });

    fireEvent.click(screen.getByRole('button', { name: /save changes/i }));

    await waitFor(() => {
      expect(updateCourseMock).toHaveBeenCalledWith({
        courseId: 'course-1',
        title: 'Advanced Encounter AI',
        slug: 'advanced-encounter-ai',
        description: 'Design readable enemy behavior loops for shipped game prototypes.',
        category: 'GameDevelopment',
        difficulty: 'Advanced',
        estimatedHours: 48,
        passingScore: 70,
        skillsRequired: 'Unity basics, C# fundamentals',
        skillsProvided: 'Boss AI, Telemetry tuning',
      });
    });
    expect(refreshMock).toHaveBeenCalled();
    expect(replaceMock).toHaveBeenCalledWith(
      '/console/learning/courses/advanced-encounter-ai-by-instructor-one/listing/info',
    );
    expect(screen.getByText('Course info updated successfully.')).toBeInTheDocument();
  });

  it('does not overwrite professor edits when an obsolete StrictMode load resolves late', async () => {
    let resolveFirst!: (course: CourseDetails) => void;
    let resolveSecond!: (course: CourseDetails) => void;
    fetchCourseMock
      .mockReturnValueOnce(new Promise<CourseDetails>((resolve) => { resolveFirst = resolve; }))
      .mockReturnValueOnce(new Promise<CourseDetails>((resolve) => { resolveSecond = resolve; }));

    render(
      <StrictMode>
        <ListingInfoPage params={Promise.resolve({ locale: 'en-US', course: 'ai-for-boss-encounters-by-instructor-one' })} />
      </StrictMode>,
    );

    await waitFor(() => expect(fetchCourseMock).toHaveBeenCalledTimes(2));
    resolveSecond(courseFixture);
    const titleInput = await screen.findByLabelText(/course title/i);
    fireEvent.change(titleInput, { target: { value: 'Professor work in progress' } });

    await act(async () => {
      resolveFirst({ ...courseFixture, title: 'Obsolete server title' });
      await Promise.resolve();
    });

    expect(titleInput).toHaveValue('Professor work in progress');
  });
});

import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { CourseDetails } from '@/lib/learning/types';
import ListingInfoPage from './page';

const fetchCourseMock = vi.fn();
const updateCourseMock = vi.fn();
const refreshMock = vi.fn();

vi.mock('next/navigation', () => ({
  useRouter: () => ({
    refresh: refreshMock,
  }),
}));

vi.mock('@/lib/learning/actions', () => ({
  fetchCourse: (...args: unknown[]) => fetchCourseMock(...args),
  updateCourse: (...args: unknown[]) => updateCourseMock(...args),
}));

const courseFixture: CourseDetails = {
  id: 'course-1',
  title: 'AI for Boss Encounters',
  description: 'Build production-ready enemy behavior for portfolio projects.',
  metadata: null,
  slug: 'ai-for-boss-encounters',
  status: 'draft',
  visibility: 'private',
  thumbnail: 'https://cdn.gameguild.gg/courses/boss-ai.jpg',
  videoShowcaseUrl: null,
  estimatedHours: 32,
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
    fetchCourseMock.mockResolvedValue(courseFixture);
    updateCourseMock.mockResolvedValue({ success: true, data: null });
  });

  it('loads the course and saves storefront identity fields through updateCourse', async () => {
    render(<ListingInfoPage params={Promise.resolve({ locale: 'en-US', course: 'course-1' })} />);

    const titleInput = await screen.findByLabelText(/course title/i);
    fireEvent.change(titleInput, { target: { value: 'Advanced Encounter AI' } });
    fireEvent.change(screen.getByLabelText(/url slug/i), { target: { value: 'advanced-encounter-ai' } });
    fireEvent.change(screen.getByLabelText(/^description$/i), {
      target: { value: 'Design readable enemy behavior loops for shipped game prototypes.' },
    });
    fireEvent.change(screen.getByLabelText(/estimated hours/i), { target: { value: '48' } });
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
        skillsRequired: 'Unity basics, C# fundamentals',
        skillsProvided: 'Boss AI, Telemetry tuning',
      });
    });
    expect(refreshMock).toHaveBeenCalled();
    expect(screen.getByText('Course info updated successfully.')).toBeInTheDocument();
  });
});

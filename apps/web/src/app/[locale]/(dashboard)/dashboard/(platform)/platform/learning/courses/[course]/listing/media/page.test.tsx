import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { CourseDetails } from '@/lib/learning/types';
import ListingMediaPage from './page';

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
  creatorId: 'creator-1',
  creatorHandle: 'instructor-one',
  title: 'AI for Boss Encounters',
  description: 'Build production-ready enemy behavior for portfolio projects.',
  metadata: null,
  slug: 'ai-for-boss-encounters',
  status: 'draft',
  visibility: 'private',
  thumbnail: 'https://cdn.gameguild.gg/courses/boss-ai.jpg',
  videoShowcaseUrl: 'https://video.example.com/original',
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

describe('ListingMediaPage', () => {
  beforeEach(() => {
    fetchCourseMock.mockReset();
    updateCourseMock.mockReset();
    refreshMock.mockReset();
    fetchCourseMock.mockResolvedValue(courseFixture);
    updateCourseMock.mockResolvedValue({ success: true, data: null });
  });

  it('loads the course and saves media URLs through updateCourse', async () => {
    render(<ListingMediaPage params={Promise.resolve({ locale: 'en-US', course: 'ai-for-boss-encounters-by-instructor-one' })} />);

    const thumbnailInput = await screen.findByLabelText(/thumbnail url/i);
    fireEvent.change(thumbnailInput, {
      target: { value: 'https://cdn.gameguild.gg/courses/advanced-encounter-ai.jpg' },
    });
    fireEvent.change(screen.getByLabelText(/video url/i), {
      target: { value: 'https://video.example.com/advanced-encounter-ai' },
    });

    fireEvent.click(screen.getByRole('button', { name: /save media/i }));

    await waitFor(() => {
      expect(updateCourseMock).toHaveBeenCalledWith({
        courseId: 'course-1',
        thumbnail: 'https://cdn.gameguild.gg/courses/advanced-encounter-ai.jpg',
        videoShowcaseUrl: 'https://video.example.com/advanced-encounter-ai',
      });
    });
    expect(screen.getByText('Media updated successfully.')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /save media/i })).toBeEnabled();
    });
    expect(refreshMock).not.toHaveBeenCalled();
  });
});

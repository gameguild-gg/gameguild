import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { CourseDetails } from '@/lib/learning/types';
import { getCourse } from './course';
import { getCourseFaq, getCourseLandingProjects } from './listing';

vi.mock('./course', () => ({
  getCourse: vi.fn(),
}));

vi.mock('./http', () => ({
  learningApiGet: vi.fn(),
}));

const baseCourse: CourseDetails = {
  id: 'course-1',
  creatorId: 'creator-1',
  creatorHandle: 'instructor-one',
  title: 'Dashboard Editable Course',
  description: 'Course description',
  metadata: null,
  slug: 'dashboard-editable-course',
  status: 'draft',
  visibility: 'private',
  thumbnail: null,
  videoShowcaseUrl: null,
  estimatedHours: 12,
  category: 'GameDevelopment',
  difficulty: 'Intermediate',
  skillsRequired: null,
  skillsProvided: null,
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
    hasClasses: true,
    hasRecordings: true,
    hasSchedule: true,
    hasOnDemandContent: true,
    hasPricing: false,
    hasCertificate: true,
    hasAssessments: true,
    hasDiscussions: true,
  },
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-02T00:00:00.000Z',
};

describe('course listing queries', () => {
  beforeEach(() => {
    vi.mocked(getCourse).mockReset();
  });

  it('uses dashboard-edited FAQ stored in course metadata', async () => {
    vi.mocked(getCourse).mockResolvedValue({
      ...baseCourse,
      metadata: JSON.stringify({
        landingFaq: [
          { question: 'Can I edit this from the dashboard?', answer: 'Yes, this FAQ is metadata-backed.' },
          { question: 'Will the storefront use it?', answer: 'Yes, edited metadata wins over generated defaults.' },
        ],
      }),
    } as CourseDetails & { metadata: string });

    const faq = await getCourseFaq('course-1');

    expect(faq.total).toBe(2);
    expect(faq.items).toEqual([
      expect.objectContaining({
        id: 'course-1-faq-1',
        question: 'Can I edit this from the dashboard?',
        answer: 'Yes, this FAQ is metadata-backed.',
        order: 1,
      }),
      expect.objectContaining({
        id: 'course-1-faq-2',
        question: 'Will the storefront use it?',
        answer: 'Yes, edited metadata wins over generated defaults.',
        order: 2,
      }),
    ]);
  });

  it('uses dashboard-edited project carousel items stored in course metadata', async () => {
    vi.mocked(getCourse).mockResolvedValue({
      ...baseCourse,
      metadata: JSON.stringify({
        landingProjects: [
          {
            title: 'Boss behavior sandbox',
            summary: 'Students build a readable boss encounter with inspectable AI states.',
            image: 'https://example.com/boss-sandbox.jpg',
            skills: ['State debugging', 'Combat pacing'],
            deliverable: 'A playable boss encounter with annotated decision logic.',
            moduleLabel: 'Project A',
          },
        ],
      }),
    } as CourseDetails & { metadata: string });

    const projects = await getCourseLandingProjects('course-1');

    expect(projects.total).toBe(1);
    expect(projects.items).toEqual([
      expect.objectContaining({
        id: 'course-1-project-1',
        title: 'Boss behavior sandbox',
        summary: 'Students build a readable boss encounter with inspectable AI states.',
        image: 'https://example.com/boss-sandbox.jpg',
        skills: ['State debugging', 'Combat pacing'],
        deliverable: 'A playable boss encounter with annotated decision logic.',
        moduleLabel: 'Project A',
        order: 1,
      }),
    ]);
  });

  it('does not invent project carousel items when metadata has not been configured', async () => {
    vi.mocked(getCourse).mockResolvedValue(baseCourse);

    const projects = await getCourseLandingProjects('course-1');

    expect(projects).toEqual({ items: [], total: 0 });
  });
});

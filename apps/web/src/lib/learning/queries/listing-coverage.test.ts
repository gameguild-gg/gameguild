import type { CourseViewModel } from '@/lib/learning/view-models';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  getCourse: vi.fn(),
  resolveCourseId: vi.fn(),
  getReviews: vi.fn(),
  getReview: vi.fn(),
  getPricing: vi.fn(),
}));

vi.mock('react', () => ({ cache: (callback: unknown) => callback }));
vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('./course', () => ({ getCourse: mocks.getCourse, resolveCourseId: mocks.resolveCourseId }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesPricing = mocks.getPricing;
    },
    LearningExperienceSocialReviewsModule: class {
      getApiSocialCoursesReviews = mocks.getReviews;
      getApiSocialReviews = mocks.getReview;
    },
  },
}));

import {
  getCourseFaq,
  getCourseFaqItem,
  getCourseLandingProjects,
  getCourseListingInfo,
  getCourseListingMedia,
  getCoursePricing,
  getCourseTestimonial,
  getCourseTestimonials,
} from './listing';

const course: CourseViewModel = {
  id: 'course-1',
  creatorId: 'creator-1',
  creatorHandle: 'teacher',
  title: 'Game Systems',
  description: 'Build game systems.',
  metadata: null,
  slug: 'game-systems',
  status: 'published',
  visibility: 'public',
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

describe('course listing query coverage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', '');
    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('token');
    mocks.resolveCourseId.mockResolvedValue('course-1');
    mocks.getCourse.mockResolvedValue(course);
  });

  it('maps listing information for every level and normalizes skill lists', async () => {
    mocks.getCourse
      .mockResolvedValueOnce(null)
      .mockResolvedValueOnce({
        ...course,
        difficulty: 'Beginner',
        skillsProvided: ' C++, , Blueprints ',
        skillsRequired: ' Logic,  ',
      })
      .mockResolvedValueOnce({ ...course, difficulty: 'Intermediate' })
      .mockResolvedValueOnce({ ...course, difficulty: 'Advanced', estimatedHours: null })
      .mockResolvedValueOnce({ ...course, difficulty: 'Expert' });

    await expect(getCourseListingInfo('missing')).resolves.toBeNull();
    await expect(getCourseListingInfo('beginner')).resolves.toMatchObject({
      objectives: ['C++', 'Blueprints'],
      requirements: ['Logic'],
      level: 'beginner',
      estimatedDuration: 12,
    });
    await expect(getCourseListingInfo('intermediate')).resolves.toMatchObject({
      objectives: ['Complete the published course curriculum'],
      requirements: [],
      level: 'intermediate',
    });
    await expect(getCourseListingInfo('advanced')).resolves.toMatchObject({ level: 'advanced', estimatedDuration: 0 });
    await expect(getCourseListingInfo('other')).resolves.toMatchObject({ level: 'all-levels' });
  });

  it('maps absent and configured listing media', async () => {
    mocks.getCourse
      .mockResolvedValueOnce(null)
      .mockResolvedValueOnce(course)
      .mockResolvedValueOnce({ ...course, thumbnail: 'cover.png', videoShowcaseUrl: 'promo.mp4' })
      .mockResolvedValueOnce({ ...course, videoShowcaseUrl: 'promo.mp4' });

    await expect(getCourseListingMedia('missing')).resolves.toBeNull();
    await expect(getCourseListingMedia('empty')).resolves.toMatchObject({ coverImage: null, promoVideo: null });
    await expect(getCourseListingMedia('complete')).resolves.toMatchObject({
      coverImage: { url: 'cover.png', alt: 'Game Systems cover', width: 1280, height: 720 },
      promoVideo: { url: 'promo.mp4', duration: 0, thumbnailUrl: 'cover.png' },
    });
    await expect(getCourseListingMedia('video-only')).resolves.toMatchObject({
      coverImage: null,
      promoVideo: { thumbnailUrl: '' },
    });
  });

  it('configures the listing API client with server, public, and default URLs', async () => {
    mocks.getReviews.mockResolvedValue({ ok: true, data: [] });

    vi.stubEnv('API_URL', 'https://internal.example');
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public.example');
    await getCourseTestimonials('server');
    let options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe('https://internal.example');
    await expect(options.auth.getAccessToken()).resolves.toBe('token');

    vi.stubEnv('API_URL', '');
    await getCourseTestimonials('public');
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe('https://public.example');

    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    await getCourseTestimonials('default');
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe('http://localhost:8080');
  });

  it('maps complete and defaulted testimonials and computes rating distribution', async () => {
    mocks.getReviews.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'review-1', courseId: 'course-1', userId: 'abcdefgh-more', rating: 9,
          title: 'Excellent', content: 'Useful', isFeatured: true, isApproved: true,
          isVerifiedPurchase: true, helpfulCount: 4, createdAt: '2026-01-03T00:00:00.000Z',
        },
        { rating: -2 },
        { rating: 2.6 },
      ],
    });

    const result = await getCourseTestimonials('course-slug');

    expect(result.total).toBe(3);
    expect(result.averageRating).toBeCloseTo(3.2);
    expect(result.ratingDistribution).toEqual({ 1: 1, 2: 0, 3: 1, 4: 0, 5: 1 });
    expect(result.testimonials[0]).toMatchObject({
      id: 'review-1', studentId: 'abcdefgh-more', studentName: 'Student abcdefgh',
      title: 'Excellent', content: 'Useful', featured: true, approved: true,
      verified: true, helpful: 4, createdAt: '2026-01-03T00:00:00.000Z',
      updatedAt: '2026-01-03T00:00:00.000Z',
    });
    expect(result.testimonials[1]).toMatchObject({
      id: '', courseId: '', studentId: '', studentName: 'Student', rating: -2,
      title: 'Course review', content: '', featured: false, approved: false,
      verified: false, helpful: 0,
    });
  });

  it('returns empty testimonials for failed and null review responses', async () => {
    mocks.getReviews
      .mockResolvedValueOnce({ ok: false, error: {} })
      .mockResolvedValueOnce({ ok: true, data: null });

    await expect(getCourseTestimonials('failed')).resolves.toEqual({
      testimonials: [], total: 0, averageRating: 0, ratingDistribution: { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 },
    });
    await expect(getCourseTestimonials('null')).resolves.toEqual(expect.objectContaining({ testimonials: [], total: 0 }));
  });

  it('loads and maps a single testimonial or returns null', async () => {
    mocks.getReview
      .mockResolvedValueOnce({ ok: true, data: { id: 'review-1', userId: 'student-1' } })
      .mockResolvedValueOnce({ ok: false, error: {} });

    await expect(getCourseTestimonial('review-1')).resolves.toMatchObject({ id: 'review-1', rating: 0 });
    await expect(getCourseTestimonial('missing')).resolves.toBeNull();
  });

  it('maps valid FAQ metadata, skips invalid rows, and defaults categories', async () => {
    mocks.getCourse.mockResolvedValue({
      ...course,
      metadata: JSON.stringify({
        landingFaq: [
          { question: ' Question? ', answer: ' Answer. ', category: ' Custom ' },
          { question: 'Second?', answer: 'Yes.', category: ' ' },
          { question: 7, answer: 'invalid' },
          { question: 'invalid', answer: null },
        ],
      }),
    });

    await expect(getCourseFaq('course-1')).resolves.toMatchObject({
      total: 2,
      items: [
        { question: 'Question?', answer: 'Answer.', category: 'Custom' },
        { question: 'Second?', answer: 'Yes.', category: 'Course details' },
      ],
    });
  });

  it.each([undefined, '', 'not-json', 'null', '[]', JSON.stringify({ landingFaq: 'invalid' }), JSON.stringify({ landingFaq: [] })])(
    'falls back to generated FAQ for metadata %s',
    async (metadata) => {
      mocks.getCourse.mockResolvedValue({ ...course, metadata, estimatedHours: null });
      const result = await getCourseFaq('course-1');
      expect(result.total).toBe(2);
      expect(result.items[0]?.answer).toBe('The duration depends on the published curriculum.');
    },
  );

  it('handles missing courses and estimated duration in FAQ fallbacks', async () => {
    mocks.getCourse.mockResolvedValueOnce(null).mockResolvedValueOnce(course);
    await expect(getCourseFaq('missing')).resolves.toEqual({ items: [], total: 0 });
    await expect(getCourseFaq('course-1')).resolves.toMatchObject({
      items: [{ answer: '12 hours of estimated work.' }, { answer: 'Intermediate level in GameDevelopment.' }],
      total: 2,
    });
  });

  it('finds FAQ items and rejects malformed or unknown identifiers', async () => {
    mocks.getCourse.mockResolvedValue({ ...course, metadata: null });
    await expect(getCourseFaqItem('duration')).resolves.toBeNull();
    await expect(getCourseFaqItem('course-1-duration')).resolves.toMatchObject({ id: 'course-1-duration' });
    await expect(getCourseFaqItem('course-1-unknown')).resolves.toBeNull();
  });

  it('maps project metadata lists and fallbacks while skipping invalid projects', async () => {
    mocks.getCourse.mockResolvedValue({
      ...course,
      thumbnail: 'fallback.png',
      metadata: JSON.stringify({
        landingProjects: [
          {
            title: ' Project ', summary: ' Summary ', deliverable: ' Build ', image: ' custom.png ',
            skills: [' C++ ', '', ' AI '], moduleLabel: ' Capstone ',
          },
          {
            title: 'Second', summary: 'Summary', deliverable: 'Ship', image: '',
            skills: 'C#; Unity\nTesting', moduleLabel: '',
          },
          { title: 1, summary: 'invalid', deliverable: 'invalid' },
          { title: 'invalid', summary: null, deliverable: 'invalid' },
          { title: 'invalid', summary: 'invalid', deliverable: null },
        ],
      }),
    });

    const result = await getCourseLandingProjects('course-1');
    expect(result).toMatchObject({
      total: 2,
      items: [
        { title: 'Project', image: 'custom.png', skills: ['C++', 'AI'], moduleLabel: 'Capstone' },
        { title: 'Second', image: 'fallback.png', skills: ['C#', 'Unity', 'Testing'], moduleLabel: 'Project 02' },
      ],
    });
  });

  it.each([null, 'bad-json', 'null', '[]', JSON.stringify({ landingProjects: 'invalid' }), JSON.stringify({ landingProjects: [] })])(
    'returns no projects for absent or invalid metadata %s',
    async (metadata) => {
      mocks.getCourse.mockResolvedValue({ ...course, metadata });
      await expect(getCourseLandingProjects('course-1')).resolves.toEqual({ items: [], total: 0 });
    },
  );

  it('handles missing courses and non-list project skills', async () => {
    mocks.getCourse
      .mockResolvedValueOnce(null)
      .mockResolvedValueOnce({
        ...course,
        metadata: JSON.stringify({
          landingProjects: [{ title: 'Project', summary: 'Summary', deliverable: 'Ship', skills: 42 }],
        }),
      });
    await expect(getCourseLandingProjects('missing')).resolves.toEqual({ items: [], total: 0 });
    await expect(getCourseLandingProjects('course-1')).resolves.toMatchObject({ items: [{ skills: [], image: '' }] });
  });

  it('maps free, one-time, monthly, yearly, and defaulted pricing', async () => {
    mocks.getPricing
      .mockResolvedValueOnce({ ok: false, error: {} })
      .mockResolvedValueOnce({ ok: true, data: { isMonetizationEnabled: false } })
      .mockResolvedValueOnce({ ok: true, data: { isMonetizationEnabled: true, isSubscription: false, price: 50, currency: 'BRL' } })
      .mockResolvedValueOnce({ ok: true, data: { isMonetizationEnabled: true, isSubscription: true } })
      .mockResolvedValueOnce({ ok: true, data: { isMonetizationEnabled: true, isSubscription: true, subscriptionDurationDays: 30 } })
      .mockResolvedValueOnce({ ok: true, data: { isMonetizationEnabled: true, isSubscription: true, subscriptionDurationDays: 365 } });

    await expect(getCoursePricing('failed')).resolves.toMatchObject({ tiers: [], refundPolicy: 'Free courses do not collect payment.' });
    await expect(getCoursePricing('disabled')).resolves.toMatchObject({ tiers: [], hasFreeTrial: false });
    await expect(getCoursePricing('one-time')).resolves.toMatchObject({ tiers: [{ interval: 'one-time', price: 50, currency: 'BRL' }] });
    await expect(getCoursePricing('monthly-defaults')).resolves.toMatchObject({ tiers: [{ interval: 'monthly', price: 0, currency: 'USD' }] });
    await expect(getCoursePricing('monthly')).resolves.toMatchObject({ tiers: [{ interval: 'monthly' }] });
    await expect(getCoursePricing('yearly')).resolves.toMatchObject({ tiers: [{ interval: 'yearly' }] });
  });
});

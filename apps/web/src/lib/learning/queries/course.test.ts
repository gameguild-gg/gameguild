import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  clientRequest: vi.fn(),
  getCourses1: vi.fn(),
  getCoursesAnalytics: vi.fn(),
  getCoursesUsers: vi.fn(),
  getCoursesContent: vi.fn(),
  getUsers1: vi.fn(),
  getApiLearningEnrollmentsCourses: vi.fn(),
  getToken: vi.fn(),
  learningApiGet: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCourses1 = mocks.getCourses1;
      getCoursesAnalytics = mocks.getCoursesAnalytics;
      getCoursesUsers = mocks.getCoursesUsers;
    },
    LearningCoursesProgramcontentModule: class {
      getCoursesContent = mocks.getCoursesContent;
    },
    LearningEnrollmentsModule: class {
      getApiLearningEnrollmentsCourses = mocks.getApiLearningEnrollmentsCourses;
    },
    UsersModule: class {
      getUsers1 = mocks.getUsers1;
    },
  },
}));

vi.mock('./http', () => ({
  learningApiGet: mocks.learningApiGet,
}));

import { getCourse, getCourseAnalytics, getCourseClass, getCourseContent, getCourseStudents, resolveCourseId } from './course';

describe('course analytics query', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    mocks.createServerClient.mockReturnValue({});
    mocks.clientRequest.mockReset();
    mocks.getCourses1.mockReset();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.learningApiGet.mockReset();
    mocks.getApiLearningEnrollmentsCourses.mockReset();
    mocks.getCoursesUsers.mockReset();
    mocks.getCoursesContent.mockReset();
    mocks.getUsers1.mockReset();
  });

  it('resolves dashboard course slugs through the authenticated slug endpoint', async () => {
    mocks.learningApiGet.mockResolvedValue({
        id: '08691da8-245e-4d9e-b729-83c9023ba061',
        title: 'AI for Boss Encounters',
        description: 'Build readable encounter AI.',
        slug: 'ai-for-boss-encounters',
        status: 'Published',
        visibility: 'Public',
        createdAt: '2026-06-01T00:00:00.000Z',
    });

    const course = await getCourse('ai-for-boss-encounters');

    expect(mocks.learningApiGet).toHaveBeenCalledWith('/v1/courses/slug/ai-for-boss-encounters', 0);
    expect(course).toMatchObject({
      id: '08691da8-245e-4d9e-b729-83c9023ba061',
      slug: 'ai-for-boss-encounters',
      title: 'AI for Boss Encounters',
      status: 'published',
      visibility: 'public',
    });
  });

  it('resolves slug-by-author dashboard params through the clean API slug', async () => {
    mocks.learningApiGet.mockResolvedValue({
        id: '18691da8-245e-4d9e-b729-83c9023ba062',
        title: 'AI for Boss Encounters',
        description: 'Build readable encounter AI.',
        slug: 'ai-for-boss-encounters',
        status: 'Published',
        visibility: 'Public',
        createdAt: '2026-06-01T00:00:00.000Z',
    });

    const course = await getCourse('ai-for-boss-encounters-by-ada-lovelace');

    expect(mocks.learningApiGet).toHaveBeenCalledWith('/v1/courses/slug/ai-for-boss-encounters', 0);
    expect(course?.slug).toBe('ai-for-boss-encounters');
  });

  it('does not retry a missing dashboard slug through the UUID endpoint', async () => {
    mocks.learningApiGet.mockResolvedValueOnce(null);

    await expect(getCourse('retired-course-by-ada-lovelace')).resolves.toBeNull();

    expect(mocks.learningApiGet).toHaveBeenCalledTimes(1);
    expect(mocks.learningApiGet).toHaveBeenCalledWith('/v1/courses/slug/retired-course', 0);
  });

  it('keeps legacy UUID route params working through the course ID endpoint', async () => {
    mocks.learningApiGet.mockResolvedValue({
        id: '08691da8-245e-4d9e-b729-83c9023ba061',
        title: 'AI for Boss Encounters',
        description: 'Build readable encounter AI.',
        slug: 'ai-for-boss-encounters',
        status: 'Published',
        visibility: 'Public',
        createdAt: '2026-06-01T00:00:00.000Z',
    });

    const course = await getCourse('08691da8-245e-4d9e-b729-83c9023ba061');

    expect(mocks.learningApiGet).toHaveBeenCalledWith('/v1/courses/08691da8-245e-4d9e-b729-83c9023ba061', 0);
    expect(mocks.clientRequest).not.toHaveBeenCalled();
    expect(course?.slug).toBe('ai-for-boss-encounters');
  });

  it('loads authenticated course identity through the no-store HTTP query helper', async () => {
    const courseId = '28691da8-245e-4d9e-b729-83c9023ba063';
    mocks.learningApiGet.mockResolvedValue({
      id: courseId,
      title: 'Immediately Published Course',
      description: 'The latest persisted lifecycle state.',
      slug: 'immediately-published-course',
      status: 'Published',
      visibility: 'Public',
      createdAt: '2026-06-01T00:00:00.000Z',
    });

    const course = await getCourse(courseId);

    expect(mocks.learningApiGet).toHaveBeenCalledWith(`/v1/courses/${courseId}`, 0);
    expect(course?.status).toBe('published');
  });

  it('returns the canonical API course ID for a dashboard slug', async () => {
    mocks.learningApiGet.mockResolvedValue({
        id: '1caa16bb-6810-4e53-bb0d-91f0d5702333',
        title: 'Creature Design Production',
        slug: 'creature-design-production',
        status: 'Draft',
        visibility: 'Private',
    });

    await expect(resolveCourseId('creature-design-production')).resolves.toBe('1caa16bb-6810-4e53-bb0d-91f0d5702333');
  });

  it('deduplicates nested content when the API also returns children in the flat collection', async () => {
    const courseId = '08691da8-245e-4d9e-b729-83c9023ba061';
    const child = {
      id: '28691da8-245e-4d9e-b729-83c9023ba063',
      parentId: '18691da8-245e-4d9e-b729-83c9023ba062',
      title: 'Lesson',
      type: 'Lesson',
      visibility: 'Public',
      sortOrder: 0,
      children: [],
    };
    mocks.getCoursesContent.mockResolvedValue({
      ok: true,
      data: [
        {
          id: '18691da8-245e-4d9e-b729-83c9023ba062',
          parentId: null,
          title: 'Module',
          type: 'Module',
          visibility: 'Public',
          sortOrder: 0,
          children: [child],
        },
        child,
      ],
    });

    const content = await getCourseContent(courseId);

    expect(content.items.map((item) => item.id)).toEqual([
      '18691da8-245e-4d9e-b729-83c9023ba062',
      '28691da8-245e-4d9e-b729-83c9023ba063',
    ]);
    expect(content.total).toBe(2);
  });

  it('maps aggregate API analytics without inventing detail rows', async () => {
    mocks.getCoursesAnalytics.mockResolvedValue({
      ok: true,
      data: {
        totalUsers: 18,
        activeUsers: 7,
        completedUsers: 9,
        completionRate: 50,
        averageCompletionTime: '12:30:00',
        totalViews: 245,
        lastActivity: '2026-06-10T12:00:00.000Z',
      },
    });

    const analytics = await getCourseAnalytics('course-analytics-aggregate');

    expect(analytics).toMatchObject({
      totalUsers: 18,
      activeUsers: 7,
      completedUsers: 9,
      completionRate: 50,
      averageCompletionTime: '12:30:00',
      totalViews: 245,
      lastActivity: '2026-06-10T12:00:00.000Z',
    });
    expect(analytics.enrollments).toEqual([]);
    expect(analytics.ratings).toEqual([]);
    expect(analytics.revenue).toEqual([]);
  });

  it('derives completion rate from aggregate counts when the API omits it', async () => {
    mocks.getCoursesAnalytics.mockResolvedValue({
      ok: true,
      data: {
        totalUsers: 10,
        completedUsers: 3,
      },
    });

    const analytics = await getCourseAnalytics('course-analytics-derived-rate');

    expect(analytics.totalUsers).toBe(10);
    expect(analytics.completedUsers).toBe(3);
    expect(analytics.completionRate).toBe(30);
  });

  it('returns empty aggregate analytics when the API request fails', async () => {
    mocks.getCoursesAnalytics.mockResolvedValue({
      ok: false,
      error: { status: 500, message: 'failed' },
    });

    await expect(getCourseAnalytics('course-analytics-failure')).resolves.toEqual({
      totalUsers: 0,
      activeUsers: 0,
      completedUsers: 0,
      completionRate: 0,
      averageCompletionTime: null,
      totalViews: 0,
      lastActivity: null,
      enrollments: [],
      ratings: [],
      revenue: [],
    });
  });

  it('loads class attendees from cohort enrollments without default session settings', async () => {
    mocks.learningApiGet.mockResolvedValue({
      id: 'class-enrollment-detail',
      courseId: 'course-with-cohorts',
      name: 'Portfolio critique',
      description: 'Live review session',
      startDate: '2026-06-20T13:00:00.000Z',
      endDate: '2026-06-20T14:30:00.000Z',
      maxCapacity: 24,
      currentEnrollmentCount: 2,
      status: 'Scheduled',
      isOpen: true,
      instructorId: 'instructor-1',
      meetingSchedule: 'https://meet.example/class',
      createdAt: '2026-06-01T10:00:00.000Z',
    });
    mocks.getApiLearningEnrollmentsCourses.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'enrollment-1',
          userId: 'student-1',
          cohortId: 'class-enrollment-detail',
          status: 'Active',
          progress: 45.6,
          enrolledAt: '2026-06-02T10:00:00.000Z',
          completedAt: null,
          lastActivityAt: '2026-06-10T10:00:00.000Z',
        },
        {
          id: 'enrollment-2',
          userId: 'student-2',
          cohortId: 'another-class',
          status: 'Completed',
          progress: 100,
          enrolledAt: '2026-06-03T10:00:00.000Z',
          completedAt: '2026-06-10T10:00:00.000Z',
        },
      ],
    });

    const classDetail = await getCourseClass('class-enrollment-detail');

    expect(mocks.learningApiGet).toHaveBeenCalledWith('/api/cohorts/class-enrollment-detail', 60);
    expect(mocks.getApiLearningEnrollmentsCourses).toHaveBeenCalledWith('course-with-cohorts');
    expect(classDetail).toMatchObject({
      id: 'class-enrollment-detail',
      title: 'Portfolio critique',
      attendeeCount: 2,
      attendees: [
        {
          id: 'enrollment-1',
          userId: 'student-1',
          status: 'active',
          progress: 46,
          enrolledAt: '2026-06-02T10:00:00.000Z',
          completedAt: null,
          lastActivityAt: '2026-06-10T10:00:00.000Z',
        },
      ],
    });
    expect(classDetail).not.toHaveProperty('settings');
    expect(classDetail?.instructor).toBeUndefined();
  });

  it('loads canonical course enrollments and joins real user identity data', async () => {
    mocks.getCoursesUsers.mockResolvedValue({
      ok: true,
      data: [{
        enrollmentId: 'enrollment-1',
        courseId: 'course-1',
        userId: 'user-1',
        completionPercentage: 42.4,
        startedAt: '2026-06-01T00:00:00.000Z',
        lastAccessedAt: '2026-06-10T00:00:00.000Z',
        completedAt: null,
      }],
    });
    mocks.getUsers1.mockResolvedValue({
      ok: true,
      data: { id: 'user-1', name: 'Ada Learner', email: 'ada@example.com' },
    });

    const result = await getCourseStudents('course-1');

    expect(mocks.getCoursesUsers).toHaveBeenCalledWith('course-1', { take: 200 });
    expect(mocks.getUsers1).toHaveBeenCalledWith('user-1');
    expect(result).toEqual({
      students: [{
        id: 'enrollment-1',
        userId: 'user-1',
        name: 'Ada Learner',
        email: 'ada@example.com',
        enrolledAt: '2026-06-01T00:00:00.000Z',
        progress: 42,
        completedAt: null,
        lastActivity: '2026-06-10T00:00:00.000Z',
      }],
      total: 1,
    });
  });
});

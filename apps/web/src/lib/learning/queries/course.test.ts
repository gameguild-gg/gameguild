import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getCoursesAnalytics: vi.fn(),
  getToken: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesAnalytics = mocks.getCoursesAnalytics;
    },
    LearningCoursesProgramcontentModule: class {},
  },
}));

import { getCourseAnalytics } from './course';

describe('course analytics query', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('access-token');
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
});

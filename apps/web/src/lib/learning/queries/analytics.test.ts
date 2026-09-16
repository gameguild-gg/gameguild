import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  getCourseContent: vi.fn(),
  getCoursesAnalyticsEngagement: vi.fn(),
  getCoursesAnalyticsCompletionRates: vi.fn(),
  getCoursesAnalyticsRevenue: vi.fn(),
  resolveCourseId: vi.fn(),
}));

vi.mock("@/auth", () => ({ getToken: mocks.getToken }));
vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesAnalyticsEngagement = mocks.getCoursesAnalyticsEngagement;
      getCoursesAnalyticsCompletionRates =
        mocks.getCoursesAnalyticsCompletionRates;
      getCoursesAnalyticsRevenue = mocks.getCoursesAnalyticsRevenue;
    },
  },
}));
vi.mock("./course", () => ({
  getCourseContent: mocks.getCourseContent,
  resolveCourseId: mocks.resolveCourseId,
}));

import {
  getCourseCompletionAnalytics,
  getCourseEngagementAnalytics,
  getCourseRevenueAnalytics,
} from "./analytics";

describe("learning analytics queries", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue("access-token");
    mocks.getCourseContent.mockResolvedValue({
      items: [{ id: "lesson-1", title: "Blocking combat reads" }],
      total: 1,
    });
    mocks.getCoursesAnalyticsEngagement.mockReset();
    mocks.getCoursesAnalyticsCompletionRates.mockReset();
    mocks.getCoursesAnalyticsRevenue.mockReset();
    mocks.resolveCourseId.mockImplementation(
      async (courseId: string) => courseId,
    );
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.useRealTimers();
  });

  it("uses generated engagement aggregates without synthetic daily activity", async () => {
    mocks.getCoursesAnalyticsEngagement.mockResolvedValue({
      ok: true,
      data: {
        dailyActiveUsers: 2,
        weeklyActiveUsers: 5,
        averageSessionDuration: "00:12:30",
        totalSessions: 9,
        contentEngagement: { "lesson-1": 4 },
      },
    });

    const analytics = await getCourseEngagementAnalytics(
      "course-engagement-no-fake-days",
    );

    expect(mocks.getCoursesAnalyticsEngagement).toHaveBeenCalledWith(
      "course-engagement-no-fake-days",
    );
    expect(analytics.activeStudents).toBe(5);
    expect(analytics.totalViews).toBe(9);
    expect(analytics.avgSessionDuration).toBe(750);
    expect(analytics.dailyActivity).toEqual([]);
    expect(analytics.contentViews).toEqual([
      {
        contentId: "lesson-1",
        contentTitle: "Blocking combat reads",
        views: 4,
        avgWatchTime: 0,
        completionRate: 0,
      },
    ]);
  });

  it("keeps only API-dated completion trend rows", async () => {
    mocks.getCoursesAnalyticsCompletionRates.mockResolvedValue({
      ok: true,
      data: {
        overallCompletionRate: 50,
        contentCompletionRates: { "lesson-1": 75 },
        completionTrends: [
          { completedCount: 1, totalCount: 4 },
          {
            date: "2026-06-10T00:00:00.000Z",
            completedCount: 2,
            totalCount: 4,
          },
        ],
      },
    });

    const analytics = await getCourseCompletionAnalytics(
      "course-completion-no-fallback-date",
    );

    expect(mocks.getCoursesAnalyticsCompletionRates).toHaveBeenCalledWith(
      "course-completion-no-fallback-date",
    );
    expect(analytics.completionRate).toBe(50);
    expect(analytics.completionTrend).toEqual([
      {
        date: "2026-06-10T00:00:00.000Z",
        completions: 2,
        cumulative: 4,
      },
    ]);
  });

  it("does not invent revenue tiers, sources, or trend dates", async () => {
    mocks.getCoursesAnalyticsRevenue.mockResolvedValue({
      ok: true,
      data: {
        totalRevenue: 120,
        totalPurchases: 3,
        revenueChart: [
          { revenue: 20, purchases: 1 },
          { date: "2026-06-11T00:00:00.000Z", revenue: 100, purchases: 2 },
        ],
      },
    });

    const analytics = await getCourseRevenueAnalytics(
      "course-revenue-no-fake-breakdown",
    );

    expect(mocks.getCoursesAnalyticsRevenue).toHaveBeenCalledWith(
      "course-revenue-no-fake-breakdown",
    );
    expect(analytics.totalRevenue).toBe(120);
    expect(analytics.avgTransactionValue).toBe(40);
    expect(analytics.revenueByTier).toEqual([]);
    expect(analytics.revenueBySource).toEqual([]);
    expect(analytics.revenueTrend).toEqual([
      {
        date: "2026-06-11T00:00:00.000Z",
        revenue: 100,
        transactions: 2,
      },
    ]);
  });

  it("uses daily engagement and preserves explicit periods when weekly data is absent", async () => {
    mocks.resolveCourseId.mockResolvedValue("resolved-course");
    mocks.getCourseContent.mockResolvedValue({
      items: [],
      total: 0,
    });
    mocks.getCoursesAnalyticsEngagement.mockResolvedValue({
      ok: true,
      data: {
        dailyActiveUsers: 2,
        averageSessionDuration: "invalid-duration",
        contentEngagement: { "unknown-content": 3 },
      },
    });
    const period = {
      from: "2026-01-01T00:00:00.000Z",
      to: "2026-01-31T23:59:59.999Z",
    };

    const analytics = await getCourseEngagementAnalytics("course-slug", period);

    expect(analytics).toMatchObject({
      courseId: "resolved-course",
      period,
      activeStudents: 2,
      totalViews: 0,
      avgSessionDuration: 0,
    });
    expect(analytics.contentViews[0]?.contentTitle).toBe("unknown-content");
  });

  it("returns safe engagement defaults when the API has no metrics", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-09-15T12:30:00.000Z"));
    mocks.getCoursesAnalyticsEngagement.mockResolvedValue({
      ok: false,
      error: {},
    });

    const analytics = await getCourseEngagementAnalytics(
      "course-without-metrics",
    );

    expect(analytics.period).toEqual({
      from: "2026-09-09T00:00:00.000Z",
      to: "2026-09-15T12:30:00.000Z",
    });
    expect(analytics).toMatchObject({
      activeStudents: 0,
      totalViews: 0,
      avgSessionDuration: 0,
      contentViews: [],
    });
  });

  it("rejects malformed three-part engagement durations", async () => {
    mocks.getCoursesAnalyticsEngagement.mockResolvedValue({
      ok: true,
      data: { averageSessionDuration: "00:not-a-number:30" },
    });

    const analytics = await getCourseEngagementAnalytics("invalid-duration");

    expect(analytics.avgSessionDuration).toBe(0);
  });

  it("returns safe completion defaults for failed analytics requests", async () => {
    mocks.getCoursesAnalyticsCompletionRates.mockResolvedValue({
      ok: false,
      error: {},
    });
    const period = {
      from: "2026-02-01T00:00:00.000Z",
      to: "2026-02-28T23:59:59.999Z",
    };

    const analytics = await getCourseCompletionAnalytics(
      "course-without-completion",
      period,
    );

    expect(analytics).toMatchObject({
      period,
      totalEnrolled: 0,
      totalCompleted: 0,
      completionRate: 0,
      dropOffPoints: [],
      completionTrend: [],
    });
    expect(analytics.funnel).toEqual([
      { stage: "Enrolled", count: 0, percentage: 100 },
      { stage: "Completed", count: 0, percentage: 0 },
    ]);
  });

  it("normalizes unknown content, over-completion, and sparse completion trends", async () => {
    mocks.getCourseContent.mockResolvedValue({ items: [], total: 0 });
    mocks.getCoursesAnalyticsCompletionRates.mockResolvedValue({
      ok: true,
      data: {
        contentCompletionRates: { "unknown-content": 125 },
        completionTrends: [{ date: "2026-07-01T00:00:00.000Z" }],
      },
    });

    const analytics = await getCourseCompletionAnalytics("sparse-completion");

    expect(analytics.dropOffPoints).toEqual([
      expect.objectContaining({
        contentTitle: "unknown-content",
        dropOffRate: 0,
      }),
    ]);
    expect(analytics.totalCompleted).toBe(0);
    expect(analytics.totalEnrolled).toBe(0);
    expect(analytics.completionRate).toBe(0);
    expect(analytics.completionTrend).toEqual([
      {
        date: "2026-07-01T00:00:00.000Z",
        completions: 0,
        cumulative: 0,
      },
    ]);
  });

  it("returns safe revenue defaults without purchases or chart data", async () => {
    mocks.getCoursesAnalyticsRevenue.mockResolvedValue({
      ok: false,
      error: {},
    });
    const period = {
      from: "2026-03-01T00:00:00.000Z",
      to: "2026-03-31T23:59:59.999Z",
    };

    const analytics = await getCourseRevenueAnalytics(
      "course-without-revenue",
      period,
    );

    expect(analytics).toMatchObject({
      period,
      totalRevenue: 0,
      totalTransactions: 0,
      avgTransactionValue: 0,
      revenueTrend: [],
    });
  });

  it("normalizes sparse revenue chart points", async () => {
    mocks.getCoursesAnalyticsRevenue.mockResolvedValue({
      ok: true,
      data: {
        revenueChart: [{ date: "2026-08-01T00:00:00.000Z" }],
      },
    });

    const analytics = await getCourseRevenueAnalytics("sparse-revenue");

    expect(analytics.revenueTrend).toEqual([
      {
        date: "2026-08-01T00:00:00.000Z",
        revenue: 0,
        transactions: 0,
      },
    ]);
  });

  it("configures server, public, and local analytics clients with the request token", async () => {
    mocks.getCoursesAnalyticsRevenue.mockResolvedValue({
      ok: false,
      error: {},
    });
    vi.stubEnv("API_URL", "https://server-api.example");
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://public-api.example");
    await getCourseRevenueAnalytics("server-client");
    let options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("https://server-api.example");
    await expect(options.auth.getAccessToken()).resolves.toBe("access-token");

    vi.stubEnv("API_URL", "");
    await getCourseRevenueAnalytics("public-client");
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("https://public-api.example");

    vi.stubEnv("NEXT_PUBLIC_API_URL", "");
    await getCourseRevenueAnalytics("local-client");
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("http://localhost:8080");
  });
});

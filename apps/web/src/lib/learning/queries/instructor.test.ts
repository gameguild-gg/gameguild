import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  getCourses: vi.fn(),
  getAnalytics: vi.fn(),
  getUsers: vi.fn(),
}));

vi.mock("@/auth", () => ({ getToken: mocks.getToken }));
vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesForGetCourses = mocks.getCourses;
      getCoursesAnalytics = mocks.getAnalytics;
      getCoursesUsers = mocks.getUsers;
    },
  },
}));

import { getInstructorStats, getRecentActivity } from "./instructor";

const originalApiUrl = process.env.API_URL;
const originalPublicApiUrl = process.env.NEXT_PUBLIC_API_URL;

describe("instructor dashboard queries", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue("token");
    process.env.API_URL = "https://api.example.test";
    delete process.env.NEXT_PUBLIC_API_URL;
  });

  afterEach(() => {
    if (originalApiUrl === undefined) delete process.env.API_URL;
    else process.env.API_URL = originalApiUrl;
    if (originalPublicApiUrl === undefined) delete process.env.NEXT_PUBLIC_API_URL;
    else process.env.NEXT_PUBLIC_API_URL = originalPublicApiUrl;
  });

  it("maps instructor KPIs across successful and degraded analytics", async () => {
    mocks.getCourses.mockResolvedValue({
      ok: true,
      data: [
        {
          id: "course-1",
          title: "Published",
          status: 2,
          currentEnrollments: 2,
          totalRatings: 2,
          averageRating: 4.5,
        },
        {
          id: "course-2",
          title: null,
          status: "3",
          currentEnrollments: -1,
          totalRatings: 0,
        },
        {
          id: "course-3",
          title: "Named status",
          status: "published",
          currentEnrollments: 7,
          totalRatings: 1,
          averageRating: null,
        },
        {
          id: "course-4",
          title: "Archived",
          status: "archived",
          currentEnrollments: 8,
          totalRatings: -1,
        },
        {
          id: undefined,
          title: "Draft",
          status: null,
          currentEnrollments: null,
          totalRatings: null,
        },
        {
          id: "course-5",
          title: "Fallback metrics",
          status: "Draft",
          currentEnrollments: 3,
          totalRatings: 0,
        },
        {
          id: "course-6",
          title: "Empty cohort",
          status: "Draft",
          currentEnrollments: 3,
          totalRatings: 0,
        },
      ],
    });
    mocks.getAnalytics.mockImplementation(async (courseId: string) => {
      if (courseId === "course-1") {
        return { ok: true, data: { totalUsers: 10, completionRate: -5 } };
      }
      if (courseId === "course-2") {
        return { ok: true, data: { totalUsers: 4, completedUsers: 5 } };
      }
      if (courseId === "course-3") return { ok: false, error: {} };
      if (courseId === "course-5") return { ok: true, data: {} };
      if (courseId === "course-6") return { ok: true, data: { totalUsers: 0 } };
      throw new Error("analytics unavailable");
    });

    const result = await getInstructorStats();

    expect(result.courses).toEqual([
      {
        id: "course-1",
        title: "Published",
        status: "published",
        enrolledCount: 10,
        completionPercent: 0,
        averageRating: 4.5,
        totalRatings: 2,
      },
      expect.objectContaining({
        id: "course-2",
        title: "Untitled course",
        status: "archived",
        enrolledCount: 4,
        completionPercent: 100,
        averageRating: null,
      }),
      expect.objectContaining({
        status: "published",
        enrolledCount: 7,
        completionPercent: null,
        averageRating: 0,
      }),
      expect.objectContaining({
        status: "archived",
        enrolledCount: 8,
        completionPercent: null,
      }),
      expect.objectContaining({
        id: "",
        status: "draft",
        enrolledCount: 0,
        completionPercent: null,
      }),
      expect.objectContaining({
        id: "course-5",
        enrolledCount: 3,
        completionPercent: 0,
      }),
      expect.objectContaining({
        id: "course-6",
        enrolledCount: 0,
        completionPercent: null,
      }),
    ]);
    expect(mocks.createServerClient).toHaveBeenCalledWith(
      expect.objectContaining({ baseUrl: "https://api.example.test" }),
    );
    await expect(
      mocks.createServerClient.mock.calls[0]![0].auth.getAccessToken(),
    ).resolves.toBe("token");
  });

  it.each([
    { ok: false, error: {} },
    { ok: true, data: null },
  ])("returns no KPIs for an unusable course response", async (response) => {
    mocks.getCourses.mockResolvedValue(response);
    await expect(getInstructorStats()).resolves.toEqual({ courses: [] });
  });

  it("contains course-list failures and exercises API URL fallbacks", async () => {
    mocks.getCourses.mockRejectedValueOnce(new Error("offline"));
    await expect(getInstructorStats()).resolves.toEqual({ courses: [] });

    mocks.getCourses.mockResolvedValue({ ok: true, data: [] });
    delete process.env.API_URL;
    process.env.NEXT_PUBLIC_API_URL = "https://public-api.example.test";
    await getInstructorStats();
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(
      expect.objectContaining({ baseUrl: "https://public-api.example.test" }),
    );

    delete process.env.NEXT_PUBLIC_API_URL;
    await getInstructorStats();
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(
      expect.objectContaining({ baseUrl: "http://localhost:8080" }),
    );
  });

  it("builds, validates, sorts, and limits recent learner activity", async () => {
    mocks.getCourses.mockResolvedValue({
      ok: true,
      data: [
        { id: undefined, title: "Ignored" },
        { id: "course-1", title: "Course One" },
        { id: "course-2", title: null },
        { id: "course-3", title: "Malformed" },
        { id: "course-4", title: "Unavailable" },
        { id: "course-5", title: "Forbidden" },
      ],
    });
    const students = Array.from({ length: 13 }, (_, index) => ({
      userName: index === 0 ? " Ada " : index === 1 ? "" : undefined,
      userEmail: index === 1 ? " grace@example.test " : undefined,
      startedAt: new Date(Date.UTC(2026, 0, 1, 0, index)).toISOString(),
      completedAt:
        index % 2 === 0 || index === 1
          ? new Date(Date.UTC(2026, 0, 2, 0, index)).toISOString()
          : undefined,
      lastAccessedAt:
        index === 0
          ? new Date(Date.UTC(2026, 0, 3)).toISOString()
          : index === 1
            ? new Date(Date.UTC(2026, 0, 1, 0, index)).toISOString()
            : index === 2
              ? "not-a-date"
              : index === 4
                ? new Date(Date.UTC(2026, 0, 2, 0, index)).toISOString()
              : undefined,
    }));
    mocks.getUsers.mockImplementation(async (courseId: string) => {
      if (courseId === "course-1") return { ok: true, data: students };
      if (courseId === "course-2") {
        return {
          ok: true,
          data: [{ userName: "", userEmail: "second@example.test", completedAt: "2026-01-04T00:00:00.000Z" }],
        };
      }
      if (courseId === "course-3") return { ok: true, data: null };
      if (courseId === "course-5") return { ok: false, error: {} };
      throw new Error("students unavailable");
    });

    const result = await getRecentActivity();

    expect(result.activities).toHaveLength(20);
    expect(result.activities[0]).toEqual(
      expect.objectContaining({
        type: "completion",
        studentName: "second@example.test",
        courseName: "Untitled course",
      }),
    );
    expect(result.activities).toContainEqual(
      expect.objectContaining({
        type: "activity",
        studentName: "Ada",
        courseName: "Course One",
      }),
    );
    expect(result.activities).toContainEqual(
      expect.objectContaining({ studentName: "grace@example.test" }),
    );
    expect(result.activities).toContainEqual(
      expect.objectContaining({ studentName: "Student 3" }),
    );
    expect(result.activities).toContainEqual(
      expect.objectContaining({
        studentName: "second@example.test",
        courseName: "Untitled course",
        type: "completion",
      }),
    );
    expect(result.activities.every((activity) => !Number.isNaN(Date.parse(activity.timestamp)))).toBe(true);
  });

  it.each([
    { ok: false, error: {} },
    { ok: true, data: null },
    { ok: true, data: [] },
  ])("returns no recent activity when courses are unavailable", async (response) => {
    mocks.getCourses.mockResolvedValue(response);
    await expect(getRecentActivity()).resolves.toEqual({ activities: [] });
    expect(mocks.getUsers).not.toHaveBeenCalled();
  });

  it("contains top-level activity failures", async () => {
    mocks.getCourses.mockRejectedValue(new Error("offline"));
    await expect(getRecentActivity()).resolves.toEqual({ activities: [] });
  });
});

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  getCourses: vi.fn(),
  getAnalytics: vi.fn(),
  getUser: vi.fn(),
}));

vi.mock("@/auth", () => ({ getToken: mocks.getToken }));

vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesForGetCourses = mocks.getCourses;
      getCoursesAnalytics = mocks.getAnalytics;
    },
    UsersModule: class {
      getUsersByUserId = mocks.getUser;
    },
  },
}));

import { GeneratedApi } from "@game-guild/client";
import { getCourses } from "./courses";

const originalApiUrl = process.env.API_URL;
const originalPublicApiUrl = process.env.NEXT_PUBLIC_API_URL;

describe("course list queries", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue("token");
    process.env.API_URL = "https://private-api.example.test";
    delete process.env.NEXT_PUBLIC_API_URL;
  });

  afterEach(() => {
    if (originalApiUrl === undefined) delete process.env.API_URL;
    else process.env.API_URL = originalApiUrl;
    if (originalPublicApiUrl === undefined) delete process.env.NEXT_PUBLIC_API_URL;
    else process.env.NEXT_PUBLIC_API_URL = originalPublicApiUrl;
  });

  it("maps course identities, creator handles, metrics, status, and visibility", async () => {
    mocks.getCourses.mockResolvedValue({
      ok: true,
      data: [
        {
          id: "course-1",
          slug: " first-course ",
          creatorId: "creator-1",
          title: "First course",
          thumbnail: "cover.png",
          status: 2,
          visibility: 4,
          currentEnrollments: 3,
          totalRatings: 2,
        },
        {
          id: "course-2",
          slug: "second-course",
          creatorId: "creator-1",
          title: null,
          thumbnail: 42,
          status: "3",
          visibility: "Public",
          currentEnrollments: -1,
          totalRatings: 0,
        },
        {
          id: "course-3",
          creatorId: "creator-2",
          title: "Third",
          status: "published",
          visibility: "unlisted",
          currentEnrollments: 5,
          totalRatings: 1,
          averageRating: 4.45,
        },
        {
          id: "course-4",
          creatorId: "creator-3",
          title: "Fourth",
          status: "archived",
          visibility: "Private",
          currentEnrollments: 7,
          totalRatings: -2,
        },
        {
          id: undefined,
          creatorId: "creator-4",
          title: "Legacy",
          status: null,
          visibility: null,
          currentEnrollments: null,
          totalRatings: null,
        },
        {
          id: "course-6",
          creatorId: null,
          title: "No creator",
          status: "1",
          visibility: "0",
        },
        {
          id: "course-7",
          creatorId: "creator-5",
          title: "Fallback metrics",
          status: "Draft",
          visibility: "Internal",
          currentEnrollments: 6,
          totalRatings: 0,
        },
      ],
    });
    mocks.getUser.mockImplementation(async (creatorId: string) => {
      if (creatorId === "creator-1") return { ok: true, data: { name: "Ada Lovelace" } };
      if (creatorId === "creator-2") return { ok: true, data: { email: "grace.hopper@example.test" } };
      if (creatorId === "creator-3") return { ok: false, error: {} };
      if (creatorId === "creator-5") return { ok: true, data: { name: "" } };
      throw new Error("directory unavailable");
    });
    mocks.getAnalytics.mockImplementation(async (courseId: string) => {
      if (courseId === "course-1") {
        return { ok: true, data: { totalUsers: 10, completionRate: 110 } };
      }
      if (courseId === "course-2") {
        return { ok: true, data: { totalUsers: 4, completedUsers: 1 } };
      }
      if (courseId === "course-3") return { ok: false, error: {} };
      if (courseId === "course-4") throw new Error("analytics unavailable");
      if (courseId === "course-7") return { ok: true, data: {} };
      return { ok: true, data: { totalUsers: 0 } };
    });

    const result = await getCourses();

    expect(result.error).toBeNull();
    expect(mocks.getUser).toHaveBeenCalledTimes(5);
    expect(mocks.getAnalytics).toHaveBeenCalledTimes(6);
    expect(result.courses).toEqual([
      expect.objectContaining({
        id: "course-1",
        slug: "first-course",
        creatorHandle: "ada-lovelace",
        status: "published",
        visibility: "public",
        enrolledCount: 10,
        completionPercent: 100,
        avgRating: "0.0",
      }),
      expect.objectContaining({
        title: "Untitled course",
        thumbnail: null,
        status: "archived",
        completionPercent: 25,
        avgRating: null,
      }),
      expect.objectContaining({
        creatorHandle: "grace-hopper",
        status: "published",
        visibility: "unlisted",
        enrolledCount: 5,
        completionPercent: null,
        avgRating: "4.5",
      }),
      expect.objectContaining({
        creatorHandle: "creator-3",
        status: "archived",
        visibility: "private",
        enrolledCount: 7,
        completionPercent: null,
      }),
      expect.objectContaining({
        id: "",
        creatorHandle: "creator-4",
        status: "draft",
        visibility: "private",
        enrolledCount: 0,
        completionPercent: null,
      }),
      expect.objectContaining({
        creatorId: null,
        creatorHandle: null,
        status: "draft",
        visibility: "private",
        completionPercent: null,
      }),
      expect.objectContaining({
        creatorHandle: "creator-5",
        enrolledCount: 6,
        completionPercent: 0,
      }),
    ]);
    expect(mocks.createServerClient).toHaveBeenCalledWith(
      expect.objectContaining({ baseUrl: "https://private-api.example.test" }),
    );
    const auth = mocks.createServerClient.mock.calls[0]![0].auth;
    await expect(auth.getAccessToken()).resolves.toBe("token");
  });

  it("falls back to creator IDs when the generated users module is unavailable", async () => {
    const UsersModule = GeneratedApi.UsersModule;
    (GeneratedApi as { UsersModule?: unknown }).UsersModule = undefined;
    mocks.getCourses.mockResolvedValue({
      ok: true,
      data: [
        {
          id: "course-1",
          slug: "course",
          creatorId: "***",
          title: "Course",
          totalRatings: 0,
        },
      ],
    });
    mocks.getAnalytics.mockResolvedValue({ ok: true, data: { totalUsers: 0 } });
    try {
      const result = await getCourses();
      expect(result.courses[0]?.creatorHandle).toBe("gameguild");
      expect(mocks.getUser).not.toHaveBeenCalled();
    } finally {
      (GeneratedApi as { UsersModule?: unknown }).UsersModule = UsersModule;
    }
  });

  it.each([
    [{ status: 403, code: "FORBIDDEN", detail: "No access" }, "[403 FORBIDDEN] No access"],
    [{ status: 500, message: "API failed" }, "[500] API failed"],
    [undefined, "[unknown] Failed to load courses"],
  ])("formats course API failures", async (error, expected) => {
    mocks.getCourses.mockResolvedValue({ ok: false, error });
    await expect(getCourses()).resolves.toEqual({ courses: [], error: expected });
  });

  it.each([
    [new Error("network down"), "Unexpected: network down"],
    ["offline", "Unexpected: offline"],
  ])("contains unexpected failures", async (failure, expected) => {
    mocks.getCourses.mockRejectedValue(failure);
    await expect(getCourses()).resolves.toEqual({ courses: [], error: expected });
  });

  it("uses the public and local API fallbacks", async () => {
    mocks.getCourses.mockResolvedValue({ ok: true, data: [] });
    delete process.env.API_URL;
    process.env.NEXT_PUBLIC_API_URL = "https://public-api.example.test";
    await getCourses();
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(
      expect.objectContaining({ baseUrl: "https://public-api.example.test" }),
    );

    delete process.env.NEXT_PUBLIC_API_URL;
    await getCourses();
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(
      expect.objectContaining({ baseUrl: "http://localhost:8080" }),
    );
  });
});

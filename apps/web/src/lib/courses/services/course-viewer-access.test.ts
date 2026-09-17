import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => {
  process.env.API_URL = "http://api.example.test/";
  return {
    auth: vi.fn(),
    createServerClient: vi.fn(),
    getCoursesMeProgress: vi.fn(),
    getToken: vi.fn(),
  };
});

vi.mock("@/auth", () => ({ auth: mocks.auth, getToken: mocks.getToken }));
vi.mock("server-only", () => ({}));
vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesMeProgress = mocks.getCoursesMeProgress;
    },
  },
}));

import { getCourseViewerAccess } from "./course-viewer-access";

describe("getCourseViewerAccess", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    process.env.API_URL = "http://api.example.test/";
    delete process.env.NEXT_PUBLIC_API_URL;
    mocks.getToken.mockResolvedValue("access-token");
    mocks.auth.mockResolvedValue({ user: { id: "user-1" } });
    mocks.createServerClient.mockReturnValue({});
    mocks.getCoursesMeProgress.mockResolvedValue({
      ok: true,
      data: {
        completionPercentage: 7500,
        lastAccessedAt: "2026-09-15T09:00:00Z",
      },
    });
  });

  it("does not call Learning APIs without both token and authenticated user", async () => {
    mocks.getToken.mockResolvedValueOnce(null);
    await expect(getCourseViewerAccess("course-1")).resolves.toEqual({
      state: "signed-out",
    });
    expect(mocks.auth).not.toHaveBeenCalled();

    mocks.auth.mockResolvedValueOnce(null);
    await expect(getCourseViewerAccess("course-1")).resolves.toEqual({
      state: "signed-out",
    });
    mocks.auth.mockResolvedValueOnce({ user: {} });
    await expect(getCourseViewerAccess("course-1")).resolves.toEqual({
      state: "signed-out",
    });
    expect(mocks.getCoursesMeProgress).not.toHaveBeenCalled();
  });

  it("returns progress and initializes the generated client with normalized configuration", async () => {
    await expect(getCourseViewerAccess("course-1")).resolves.toEqual({
      state: "has-access",
      progressPercentage: 75,
      lastAccessedAt: "2026-09-15T09:00:00Z",
    });
    expect(mocks.getCoursesMeProgress).toHaveBeenCalledWith("course-1");
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: "http://api.example.test",
      auth: { getAccessToken: expect.any(Function) },
    });
    const getAccessToken =
      mocks.createServerClient.mock.calls[0]?.[0].auth.getAccessToken;
    await expect(getAccessToken()).resolves.toBe("access-token");
  });

  it("uses the public API URL and localhost fallbacks when the server URL is absent", async () => {
    delete process.env.API_URL;
    process.env.NEXT_PUBLIC_API_URL = "http://public-api.example.test/";
    await getCourseViewerAccess("course-1");
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(
      expect.objectContaining({ baseUrl: "http://public-api.example.test" }),
    );

    delete process.env.NEXT_PUBLIC_API_URL;
    await getCourseViewerAccess("course-1");
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(
      expect.objectContaining({ baseUrl: "http://localhost:8080" }),
    );
  });

  it("defaults optional successful progress fields", async () => {
    mocks.getCoursesMeProgress.mockResolvedValueOnce({ ok: true, data: {} });
    await expect(getCourseViewerAccess("course-1")).resolves.toEqual({
      state: "has-access",
      progressPercentage: 0,
      lastAccessedAt: null,
    });
  });

  it.each([401, 403, 404])(
    "maps access HTTP status %s without exposing an infrastructure error",
    async (status) => {
      mocks.getCoursesMeProgress.mockResolvedValueOnce({
        ok: false,
        error: { status, message: "Denied" },
      });
      await expect(getCourseViewerAccess("course-1")).resolves.toEqual({
        state: status === 401 ? "signed-out" : "no-access",
      });
    },
  );

  it.each([
    [undefined, "Unknown error"],
    [
      {
        status: 503,
        detail: "Progress unavailable",
        message: "Request failed",
      },
      "[503] Progress unavailable",
    ],
    [
      { status: "invalid", message: "Gateway unavailable" },
      "[unknown] Gateway unavailable",
    ],
    [{ status: 500, message: "" }, "[500] Request failed"],
  ])("formats unavailable API responses safely", async (error, expected) => {
    mocks.getCoursesMeProgress.mockResolvedValueOnce({ ok: false, error });
    await expect(getCourseViewerAccess("course-1")).resolves.toEqual({
      state: "unavailable",
      error: expected,
    });
  });

  it("normalizes thrown Error and unknown failures", async () => {
    mocks.getCoursesMeProgress.mockRejectedValueOnce(
      new Error("Network unavailable"),
    );
    await expect(getCourseViewerAccess("course-1")).resolves.toEqual({
      state: "unavailable",
      error: "Network unavailable",
    });

    mocks.getCoursesMeProgress.mockRejectedValueOnce("offline");
    await expect(getCourseViewerAccess("course-1")).resolves.toEqual({
      state: "unavailable",
      error: "Unknown error",
    });
  });
});

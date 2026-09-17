import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  selfEnroll: vi.fn(),
}));

vi.mock("@/auth", () => ({ getToken: mocks.getToken }));

vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      postCoursesSelfEnroll = mocks.selfEnroll;
    },
  },
}));

import { enrollInCourse } from "./enrollment-actions";

describe("enrollInCourse", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.getToken.mockResolvedValue("access-token");
    mocks.createServerClient.mockReturnValue({ kind: "learning-client" });
    mocks.selfEnroll.mockResolvedValue({ ok: true, data: {} });
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("requires an authenticated learner", async () => {
    mocks.getToken.mockResolvedValue(null);

    await expect(enrollInCourse("course-1")).resolves.toEqual({
      success: false,
      error: "Your session expired. Sign in again.",
    });
    expect(mocks.createServerClient).not.toHaveBeenCalled();
    expect(mocks.selfEnroll).not.toHaveBeenCalled();
  });

  it("enrolls the authenticated learner", async () => {
    await expect(enrollInCourse("course-1")).resolves.toEqual({
      success: true,
    });
    expect(mocks.selfEnroll).toHaveBeenCalledWith("course-1");
  });

  it("prefers API details, then messages, then the safe fallback", async () => {
    mocks.selfEnroll
      .mockResolvedValueOnce({
        ok: false,
        error: { detail: "Course is full." },
      })
      .mockResolvedValueOnce({
        ok: false,
        error: { message: "Enrollment closed." },
      })
      .mockResolvedValueOnce({ ok: false, error: {} });

    await expect(enrollInCourse("course-1")).resolves.toEqual({
      success: false,
      error: "Course is full.",
    });
    await expect(enrollInCourse("course-1")).resolves.toEqual({
      success: false,
      error: "Enrollment closed.",
    });
    await expect(enrollInCourse("course-1")).resolves.toEqual({
      success: false,
      error: "Enrollment could not be completed.",
    });
  });

  it("normalizes failures thrown while enrolling", async () => {
    mocks.selfEnroll
      .mockRejectedValueOnce({
        detail: "Enrollment service rejected the request.",
      })
      .mockRejectedValueOnce(new Error("Enrollment service is offline."))
      .mockRejectedValueOnce(undefined);

    await expect(enrollInCourse("course-1")).resolves.toEqual({
      success: false,
      error: "Enrollment service rejected the request.",
    });
    await expect(enrollInCourse("course-1")).resolves.toEqual({
      success: false,
      error: "Enrollment service is offline.",
    });
    await expect(enrollInCourse("course-1")).resolves.toEqual({
      success: false,
      error: "Enrollment could not be completed.",
    });
  });

  it("uses the server, public, and local API URLs in priority order", async () => {
    vi.stubEnv("API_URL", "https://server-api.example");
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://public-api.example");
    await enrollInCourse("course-1");
    let options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("https://server-api.example");
    await expect(options.auth.getAccessToken()).resolves.toBe("access-token");

    vi.stubEnv("API_URL", "");
    await enrollInCourse("course-1");
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("https://public-api.example");

    vi.stubEnv("NEXT_PUBLIC_API_URL", "");
    await enrollInCourse("course-1");
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("http://localhost:8080");
  });
});

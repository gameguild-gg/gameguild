import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  completeInteraction: vi.fn(),
  createInteraction: vi.fn(),
  createServerClient: vi.fn(),
  getInteraction: vi.fn(),
  getProgress: vi.fn(),
  getToken: vi.fn(),
  updateProgress: vi.fn(),
}));

vi.mock("@/auth", () => ({ getToken: mocks.getToken }));

vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesMeProgress = mocks.getProgress;
    },
    LearningCoursesContentInteractionModule: class {
      getCourseInteractionsUserContent = mocks.getInteraction;
      postCourseInteractions = mocks.createInteraction;
      postCourseInteractionsComplete = mocks.completeInteraction;
      putCourseInteractionsProgress = mocks.updateProgress;
    },
  },
}));

import { beginCourseContent, completeCourseContent } from "./progress-actions";

const identifiers = {
  courseId: "course-1",
  contentId: "content-1",
  enrollmentId: "enrollment-1",
};

function success<T>(data: T) {
  return { ok: true as const, data };
}

function failure(error: unknown = { detail: "Request failed." }) {
  return { ok: false as const, error };
}

describe("learner progress actions", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.createServerClient.mockReturnValue({ kind: "learning-client" });
    mocks.getToken.mockResolvedValue("access-token");
    mocks.getProgress.mockResolvedValue(
      success({ enrollmentId: identifiers.enrollmentId }),
    );
    mocks.getInteraction.mockResolvedValue(
      success({ id: "interaction-1", completionPercentage: 0 }),
    );
    mocks.createInteraction.mockResolvedValue(success({ id: "interaction-2" }));
    mocks.updateProgress.mockResolvedValue(success({ id: "interaction-1" }));
    mocks.completeInteraction.mockResolvedValue(
      success({ id: "interaction-1" }),
    );
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("begins an existing interaction at the minimum visible progress", async () => {
    mocks.getInteraction.mockResolvedValue(
      success({ id: "interaction-1", completionPercentage: undefined }),
    );

    await expect(
      beginCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({ success: true });

    expect(mocks.updateProgress).toHaveBeenCalledWith(
      "interaction-1",
      {
        contentId: identifiers.contentId,
        programUserId: identifiers.enrollmentId,
        completionPercentage: 1,
      },
      { programId: identifiers.courseId },
    );
    expect(mocks.createInteraction).not.toHaveBeenCalled();
  });

  it("preserves progress above the minimum when resuming content", async () => {
    mocks.getInteraction.mockResolvedValue(
      success({ id: "interaction-1", completionPercentage: 72 }),
    );

    await beginCourseContent(identifiers.courseId, identifiers.contentId);

    expect(mocks.updateProgress).toHaveBeenCalledWith(
      "interaction-1",
      expect.objectContaining({ completionPercentage: 72 }),
      { programId: identifiers.courseId },
    );
  });

  it.each([
    failure({ detail: "Not started." }),
    success({ completionPercentage: 0 }),
  ])("creates an interaction when no reusable one exists", async (existing) => {
    mocks.getInteraction.mockResolvedValue(existing);

    await expect(
      beginCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({ success: true });
    expect(mocks.createInteraction).toHaveBeenCalledWith(
      {
        contentId: identifiers.contentId,
        programUserId: identifiers.enrollmentId,
      },
      { programId: identifiers.courseId },
    );
    expect(mocks.updateProgress).not.toHaveBeenCalled();
  });

  it("returns update errors without losing their API detail", async () => {
    mocks.updateProgress.mockResolvedValue(
      failure({ detail: "Progress is locked." }),
    );

    await expect(
      beginCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({ success: false, error: "Progress is locked." });
  });

  it("returns creation errors using the API message fallback", async () => {
    mocks.getInteraction.mockResolvedValue(failure());
    mocks.createInteraction.mockResolvedValue(
      failure({ detail: "", message: "Enrollment is inactive." }),
    );

    await expect(
      beginCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({
      success: false,
      error: "Enrollment is inactive.",
    });
  });

  it("returns a safe error when beginning content throws an unknown value", async () => {
    mocks.getProgress.mockRejectedValue(undefined);

    await expect(
      beginCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({
      success: false,
      error: "Unable to update course progress.",
    });
  });

  it("reports enrollment API failures", async () => {
    mocks.getProgress.mockResolvedValue(
      failure({ message: "Enrollment service is unavailable." }),
    );

    await expect(
      beginCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({
      success: false,
      error: "Enrollment service is unavailable.",
    });
  });

  it("reports successful progress responses without an enrollment id", async () => {
    mocks.getProgress.mockResolvedValue(success({ enrollmentId: "" }));

    await expect(
      beginCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({
      success: false,
      error: "Your course enrollment could not be resolved.",
    });
  });

  it("completes an existing interaction", async () => {
    await expect(
      completeCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({ success: true });

    expect(mocks.completeInteraction).toHaveBeenCalledWith(
      "interaction-1",
      {
        contentId: identifiers.contentId,
        programUserId: identifiers.enrollmentId,
      },
      { programId: identifiers.courseId },
    );
    expect(mocks.createInteraction).not.toHaveBeenCalled();
  });

  it("creates a missing interaction before completing it", async () => {
    mocks.getInteraction.mockResolvedValue(failure({ detail: "Missing." }));

    await expect(
      completeCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({ success: true });

    expect(mocks.createInteraction).toHaveBeenCalledWith(
      {
        contentId: identifiers.contentId,
        programUserId: identifiers.enrollmentId,
      },
      { programId: identifiers.courseId },
    );
    expect(mocks.completeInteraction).toHaveBeenCalledWith(
      "interaction-2",
      expect.any(Object),
      { programId: identifiers.courseId },
    );
  });

  it("reports failed and unidentified interaction creation", async () => {
    mocks.getInteraction.mockResolvedValue(failure());
    mocks.createInteraction
      .mockResolvedValueOnce(failure({ detail: "Cannot start content." }))
      .mockResolvedValueOnce(success({ id: "" }));

    await expect(
      completeCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({ success: false, error: "Cannot start content." });
    await expect(
      completeCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({
      success: false,
      error: "The lesson interaction could not be resolved.",
    });
  });

  it("returns completion API errors", async () => {
    mocks.completeInteraction.mockResolvedValue(
      failure({ detail: "Completion was rejected." }),
    );

    await expect(
      completeCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({
      success: false,
      error: "Completion was rejected.",
    });
  });

  it("returns caught completion errors", async () => {
    mocks.completeInteraction.mockRejectedValue({
      message: "Completion service is down.",
    });

    await expect(
      completeCourseContent(identifiers.courseId, identifiers.contentId),
    ).resolves.toEqual({
      success: false,
      error: "Completion service is down.",
    });
  });

  it("configures the authenticated client with server, public, and local URLs", async () => {
    vi.stubEnv("API_URL", "https://server-api.example");
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://public-api.example");
    await beginCourseContent(identifiers.courseId, identifiers.contentId);
    let options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("https://server-api.example");
    await expect(options.auth.getAccessToken()).resolves.toBe("access-token");

    vi.stubEnv("API_URL", "");
    await beginCourseContent(identifiers.courseId, identifiers.contentId);
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("https://public-api.example");

    vi.stubEnv("NEXT_PUBLIC_API_URL", "");
    await beginCourseContent(identifiers.courseId, identifiers.contentId);
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("http://localhost:8080");
  });
});

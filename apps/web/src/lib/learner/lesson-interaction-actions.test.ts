import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createInteraction: vi.fn(),
  createServerClient: vi.fn(),
  getInteraction: vi.fn(),
  getToken: vi.fn(),
  recordEvent: vi.fn(),
}));

vi.mock("@/auth", () => ({ getToken: mocks.getToken }));

vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesContentInteractionModule: class {
      getCourseInteractionsUserContent = mocks.getInteraction;
      postCourseInteractions = mocks.createInteraction;
    },
    LearningCoursesLessonInteractionEventsModule: class {
      postCoursesInteractionsEvents = mocks.recordEvent;
    },
  },
}));

import {
  recordLessonEvent,
  type LessonEventInput,
} from "./lesson-interaction-actions";

const event: LessonEventInput = {
  courseId: "course-1",
  enrollmentId: "enrollment-1",
  contentId: "content-1",
  type: "VideoProgress",
  positionSeconds: 42,
  durationSeconds: 120,
  progressPercentage: 35,
  idempotencyKey: "event-1",
};

describe("recordLessonEvent", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.getToken.mockResolvedValue("access-token");
    mocks.createServerClient.mockReturnValue({ kind: "lesson-client" });
    mocks.getInteraction.mockResolvedValue({
      ok: true,
      data: { id: "interaction-1" },
    });
    mocks.createInteraction.mockResolvedValue({
      ok: true,
      data: { id: "interaction-created" },
    });
    mocks.recordEvent.mockResolvedValue({ ok: true, data: {} });
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.useRealTimers();
  });

  it("requires an authenticated learner", async () => {
    mocks.getToken.mockResolvedValue(null);

    await expect(recordLessonEvent(event)).resolves.toEqual({
      success: false,
      error: "Your session expired.",
    });
    expect(mocks.createServerClient).not.toHaveBeenCalled();
  });

  it("records an event against an existing interaction", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-09-15T12:30:00.000Z"));

    await expect(recordLessonEvent(event)).resolves.toEqual({ success: true });

    expect(mocks.getInteraction).toHaveBeenCalledWith(
      "enrollment-1",
      "content-1",
      { programId: "course-1" },
    );
    expect(mocks.createInteraction).not.toHaveBeenCalled();
    expect(mocks.recordEvent).toHaveBeenCalledWith(
      "course-1",
      "interaction-1",
      {
        type: "VideoProgress",
        occurredAt: "2026-09-15T12:30:00.000Z",
        positionSeconds: 42,
        durationSeconds: 120,
        progressPercentage: 3500,
        idempotencyKey: "event-1",
      },
    );

    vi.useRealTimers();
  });

  it("creates an interaction before recording when none exists", async () => {
    mocks.getInteraction.mockResolvedValue({
      ok: false,
      error: { status: 404 },
    });

    await expect(recordLessonEvent(event)).resolves.toEqual({ success: true });
    expect(mocks.createInteraction).toHaveBeenCalledWith(
      { contentId: "content-1", programUserId: "enrollment-1" },
      { programId: "course-1" },
    );
    expect(mocks.recordEvent).toHaveBeenCalledWith(
      "course-1",
      "interaction-created",
      expect.any(Object),
    );
  });

  it("rejects failed and unidentified interactions", async () => {
    mocks.getInteraction
      .mockResolvedValueOnce({ ok: false })
      .mockResolvedValueOnce({ ok: true, data: {} });
    mocks.createInteraction.mockResolvedValue({ ok: false });

    await expect(recordLessonEvent(event)).resolves.toEqual({
      success: false,
      error: "Unable to start lesson tracking.",
    });
    await expect(recordLessonEvent(event)).resolves.toEqual({
      success: false,
      error: "Unable to start lesson tracking.",
    });
    expect(mocks.recordEvent).not.toHaveBeenCalled();
  });

  it("reports a rejected event write", async () => {
    mocks.recordEvent.mockResolvedValue({ ok: false, error: { status: 409 } });

    await expect(recordLessonEvent(event)).resolves.toEqual({
      success: false,
      error: "Unable to record lesson progress.",
    });
  });

  it("preserves caught Error messages and safely handles unknown failures", async () => {
    mocks.getInteraction
      .mockRejectedValueOnce(new Error("Interaction service is unavailable."))
      .mockRejectedValueOnce("offline");

    await expect(recordLessonEvent(event)).resolves.toEqual({
      success: false,
      error: "Interaction service is unavailable.",
    });
    await expect(recordLessonEvent(event)).resolves.toEqual({
      success: false,
      error: "Unable to record lesson progress.",
    });
  });

  it("configures server, public, and local API clients with the request token", async () => {
    vi.stubEnv("API_URL", "https://server-api.example");
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://public-api.example");
    await recordLessonEvent(event);
    let options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("https://server-api.example");
    await expect(options.auth.getAccessToken()).resolves.toBe("access-token");

    vi.stubEnv("API_URL", "");
    await recordLessonEvent(event);
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("https://public-api.example");

    vi.stubEnv("NEXT_PUBLIC_API_URL", "");
    await recordLessonEvent(event);
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("http://localhost:8080");
  });
});

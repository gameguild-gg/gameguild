import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  collectHiddenContentIds: vi.fn(),
  createServerClient: vi.fn(),
  flattenUniqueContent: vi.fn(),
  getCoursesContent: vi.fn(),
  getCoursesMeProgress: vi.fn(),
  getCoursesPricing: vi.fn(),
  getCoursesProducts: vi.fn(),
  getCoursesPublic: vi.fn(),
  getCoursesSlug: vi.fn(),
  getLearningMeDashboard: vi.fn(),
  getToken: vi.fn(),
  unstableRethrow: vi.fn(),
}));

vi.mock("@/auth", () => ({
  getToken: mocks.getToken,
}));

vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesMeProgress = mocks.getCoursesMeProgress;
      getCoursesPricing = mocks.getCoursesPricing;
      getCoursesProducts = mocks.getCoursesProducts;
      getCoursesPublic = mocks.getCoursesPublic;
      getCoursesSlug = mocks.getCoursesSlug;
    },
    LearningCoursesProgramContentModule: class {
      getCoursesContent = mocks.getCoursesContent;
    },
    LearningWorkspacesLearnerWorkspaceModule: class {
      getLearningMeDashboard = mocks.getLearningMeDashboard;
    },
  },
}));

vi.mock("@/lib/learner/content-tree", () => ({
  collectHiddenContentIds: mocks.collectHiddenContentIds,
  flattenUniqueContent: mocks.flattenUniqueContent,
}));

vi.mock("next/navigation", () => ({
  usePathname: () => "/workspace/learning",
  unstable_rethrow: mocks.unstableRethrow,
}));

import {
  getCourseAccessData,
  getCourseAttendanceData,
  getLearnerDashboard,
  getMyLearningCourses,
  getPublicCourseBySlug,
  getPublicCourses,
  mapLearnerCourseSummary,
} from "./courses";

const publicCourse = {
  id: "course-1",
  title: "Game Production",
  slug: "game-production",
  description: "Ship a playable game.",
  thumbnail: "https://cdn.example/course.png",
  category: "Game Development",
  difficulty: "Intermediate",
  estimatedHours: 24,
  currentEnrollments: 12,
  averageRating: 4.7,
  isEnrollmentOpen: true,
};

function ok<T>(data: T) {
  return { ok: true as const, data };
}

function failure(status = 500) {
  return { ok: false as const, error: { status, message: "Request failed" } };
}

describe("learner course aggregate adapter", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    process.env.AUTH_SECRET = "test-auth-secret";
    mocks.createServerClient.mockReturnValue({ kind: "learner-client" });
    mocks.getToken.mockResolvedValue("access-token");
    mocks.collectHiddenContentIds.mockReturnValue(new Set());
    mocks.flattenUniqueContent.mockImplementation((content) => content);
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("maps aggregate summaries and preserves the current lesson", () => {
    expect(
      mapLearnerCourseSummary({
        courseId: "course-1",
        enrollmentId: "enrollment-1",
        title: "Game Production",
        slug: "game-production",
        description: "Ship a playable game.",
        progressPercentage: 4_260,
        totalItems: 10,
        completedItems: 4,
        remainingMinutes: 75,
        currentContentId: "lesson-2",
        currentContentTitle: "Prototype loop",
        currentContentType: "Assignment",
      }),
    ).toMatchObject({
      id: "course-1",
      enrollmentId: "enrollment-1",
      overallProgress: 43,
      currentItem: {
        id: "lesson-2",
        title: "Prototype loop",
        type: "assignment",
        status: "in-progress",
      },
    });
  });

  it.each([
    ["Questionnaire", "quiz"],
    ["Quiz", "quiz"],
    ["Discussion", "peer-review"],
    ["Code", "activity"],
    ["Project", "activity"],
    ["Reflection", "activity"],
    ["Survey", "activity"],
    ["Lesson", "lesson"],
  ] as const)("maps a current %s workspace item to %s", (type, expected) => {
    expect(
      mapLearnerCourseSummary({
        courseId: "course-1",
        slug: "game-production",
        progressPercentage: 0,
        currentContentId: "content-1",
        currentContentTitle: "",
        currentContentType: type,
      }).currentItem,
    ).toMatchObject({
      title: "Continue course",
      type: expected,
      status: "available",
    });
  });

  it("clamps aggregate progress and supplies safe summary defaults", () => {
    expect(mapLearnerCourseSummary({ progressPercentage: -20 })).toMatchObject({
      id: "",
      title: "Untitled course",
      slug: "",
      description: "",
      thumbnail: null,
      overallProgress: 0,
      totalItems: 0,
      completedItems: 0,
      remainingMinutes: 0,
      currentItem: undefined,
    });
    expect(
      mapLearnerCourseSummary({ progressPercentage: 140 }).overallProgress,
    ).toBe(100);
  });

  it("maps the public catalog and its missing optional values", async () => {
    mocks.getCoursesPublic.mockResolvedValue(ok([publicCourse, {}]));

    await expect(getPublicCourses()).resolves.toEqual([
      {
        ...publicCourse,
      },
      {
        id: "",
        title: "Untitled course",
        slug: "",
        description: "",
        thumbnail: null,
        category: "General",
        difficulty: "Beginner",
        estimatedHours: null,
        currentEnrollments: 0,
        averageRating: 0,
        isEnrollmentOpen: false,
      },
    ]);
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: "http://localhost:8080",
      auth: undefined,
    });
  });

  it("honors the server API URL before the public API URL", async () => {
    vi.stubEnv("API_URL", "https://server-api.example");
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://public-api.example");
    mocks.getCoursesPublic.mockResolvedValue(ok([]));

    await getPublicCourses();

    expect(mocks.createServerClient).toHaveBeenCalledWith(
      expect.objectContaining({ baseUrl: "https://server-api.example" }),
    );
  });

  it("returns an empty public catalog for invalid responses and exceptions", async () => {
    const errorSpy = vi
      .spyOn(console, "error")
      .mockImplementation(() => undefined);
    mocks.getCoursesPublic
      .mockResolvedValueOnce(failure())
      .mockResolvedValueOnce(ok({ courses: [] }))
      .mockRejectedValueOnce(new Error("network down"));

    await expect(getPublicCourses()).resolves.toEqual([]);
    await expect(getPublicCourses()).resolves.toEqual([]);
    await expect(getPublicCourses()).resolves.toEqual([]);
    expect(errorSpy).toHaveBeenCalledWith(
      "[learning] Failed to fetch public courses",
      expect.any(Error),
    );
  });

  it("loads a public course by its encoded slug", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));

    await expect(
      getPublicCourseBySlug("game design/101"),
    ).resolves.toMatchObject({ id: "course-1", slug: "game-production" });
    expect(mocks.getCoursesSlug).toHaveBeenCalledWith("game%20design%2F101");
  });

  it("returns no public course for an API failure or exception", async () => {
    const errorSpy = vi
      .spyOn(console, "error")
      .mockImplementation(() => undefined);
    mocks.getCoursesSlug
      .mockResolvedValueOnce(failure(404))
      .mockRejectedValueOnce(new Error("network down"));

    await expect(getPublicCourseBySlug("missing")).resolves.toBeNull();
    await expect(getPublicCourseBySlug("broken")).resolves.toBeNull();
    expect(errorSpy).toHaveBeenCalledWith(
      "[learning] Failed to fetch course by slug",
      expect.any(Error),
    );
  });

  it("requires a session before loading attendance progress", async () => {
    mocks.getToken.mockResolvedValue(null);

    await expect(
      getCourseAttendanceData("game-production", { includeProgress: true }),
    ).resolves.toBeNull();
    expect(mocks.getCoursesSlug).not.toHaveBeenCalled();
  });

  it("returns no attendance when a course response fails or has no id", async () => {
    mocks.getCoursesSlug
      .mockResolvedValueOnce(failure(500))
      .mockResolvedValueOnce(ok({ ...publicCourse, id: undefined }));

    await expect(getCourseAttendanceData("broken")).resolves.toBeNull();
    await expect(getCourseAttendanceData("missing-id")).resolves.toBeNull();
  });

  it("returns a public course shell when content is unavailable", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesContent.mockResolvedValue(failure());

    await expect(getCourseAttendanceData("game-production")).resolves.toEqual(
      expect.objectContaining({
        id: "course-1",
        modules: [],
        overallProgress: 0,
        totalItems: 0,
        completedItems: 0,
        remainingMinutes: 0,
      }),
    );
    expect(mocks.getToken).not.toHaveBeenCalled();
    expect(mocks.getCoursesMeProgress).not.toHaveBeenCalled();
  });

  it("uses the public API URL and authenticated client for attendance progress", async () => {
    vi.stubEnv("API_URL", "");
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://public-api.example");
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesContent.mockResolvedValue(ok([]));
    mocks.getCoursesMeProgress.mockResolvedValue(
      ok({ enrollmentId: "enrollment-1", contentProgress: [] }),
    );

    await expect(
      getCourseAttendanceData("game-production", { includeProgress: true }),
    ).resolves.toMatchObject({ enrollmentId: "enrollment-1" });

    const authenticatedClientOptions = mocks.createServerClient.mock.calls.find(
      ([options]) => options.auth,
    )?.[0];
    expect(authenticatedClientOptions.baseUrl).toBe(
      "https://public-api.example",
    );
    await expect(
      authenticatedClientOptions.auth.getAccessToken(),
    ).resolves.toBe("access-token");
  });

  it("rejects missing and failed progress responses", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesContent.mockResolvedValue(ok([]));
    mocks.getCoursesMeProgress
      .mockResolvedValueOnce(undefined)
      .mockResolvedValueOnce(failure());

    await expect(
      getCourseAttendanceData("game-production", { includeProgress: true }),
    ).resolves.toBeNull();
    await expect(
      getCourseAttendanceData("game-production", { includeProgress: true }),
    ).resolves.toBeNull();
  });

  it("rejects unavailable content for authenticated attendance", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesContent.mockResolvedValue(failure());
    mocks.getCoursesMeProgress.mockResolvedValue(ok({ contentProgress: [] }));

    await expect(
      getCourseAttendanceData("game-production", { includeProgress: true }),
    ).resolves.toBeNull();
  });

  it("maps flat course content, progress, visibility, and every item type", async () => {
    const content = [
      {
        id: "assignment",
        slug: "assignment",
        title: "Assignment",
        type: "Assignment",
        sortOrder: 1,
        estimatedMinutes: 10,
        description: "Submit work",
        isRequired: true,
        jsonBody: { blocks: [] },
      },
      {
        id: "questionnaire",
        title: "Questionnaire",
        type: "Questionnaire",
        sortOrder: 2,
        estimatedMinutes: 5,
        body: "Questions",
      },
      { id: "discussion", title: "Discussion", type: "Discussion" },
      { id: "code", title: "Code", type: "Code", estimatedMinutes: 20 },
      { id: "project", title: "Project", type: "Project" },
      { id: "reflection", title: "Reflection", type: "Reflection" },
      { id: "survey", title: "Survey", type: "Survey" },
      { id: "lesson", title: "Lesson", type: "Lesson" },
      { id: "unknown", title: null, type: undefined, sortOrder: null },
      { id: "hidden", title: "Hidden", type: "Lesson" },
      { title: "No id", type: "Lesson" },
    ];
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesContent.mockResolvedValue(ok(content));
    mocks.flattenUniqueContent.mockReturnValue(content);
    mocks.collectHiddenContentIds.mockReturnValue(new Set(["hidden"]));
    mocks.getCoursesMeProgress.mockResolvedValue(
      ok({
        enrollmentId: "enrollment-1",
        contentProgress: [
          { contentId: "assignment", status: "Completed" },
          { contentId: "questionnaire", status: "Submitted" },
          { contentId: "discussion", status: "NotStarted" },
          { contentId: "code", status: "InProgress" },
          { contentId: "survey", status: "Completed" },
          { status: "Completed" },
        ],
      }),
    );

    const result = await getCourseAttendanceData("game-production", {
      includeProgress: true,
    });

    expect(result).toMatchObject({
      totalItems: 9,
      completedItems: 3,
      overallProgress: 33,
      remainingMinutes: 20,
      enrollmentId: "enrollment-1",
      currentItem: { id: "discussion", status: "available" },
      modules: [
        {
          id: "course-1-content",
          title: "Course Content",
          progress: 33,
        },
      ],
    });
    expect(result?.modules[0]?.items).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          id: "assignment",
          type: "assignment",
          status: "completed",
          content: { blocks: [] },
        }),
        expect.objectContaining({
          id: "questionnaire",
          type: "quiz",
          status: "completed",
          content: "Questions",
        }),
        expect.objectContaining({
          id: "discussion",
          type: "peer-review",
          status: "available",
        }),
        expect.objectContaining({ id: "code", type: "activity" }),
        expect.objectContaining({ id: "lesson", type: "lesson" }),
        expect.objectContaining({
          id: "unknown",
          title: "Untitled content",
          type: "lesson",
          status: "locked",
          order: 0,
          isRequired: false,
        }),
      ]),
    );
  });

  it("maps nested modules, empty modules, and an in-progress current item", async () => {
    const content = [
      {
        id: "module-empty",
        title: null,
        description: null,
        sortOrder: null,
        type: "Lesson",
      },
      {
        id: "module-one",
        title: "Module one",
        description: "Start here",
        sortOrder: 1,
        type: "Lesson",
      },
      {
        id: "completed-late",
        parentId: "module-one",
        title: "Later lesson",
        type: "Lesson",
        sortOrder: 2,
      },
      {
        id: "in-progress",
        parentId: "module-one",
        title: "Current lesson",
        type: "Lesson",
        sortOrder: null,
      },
      {
        id: "completed-early",
        parentId: "module-one",
        title: "Earlier lesson",
        type: "Lesson",
        sortOrder: 1,
      },
    ];
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesContent.mockResolvedValue(ok(content));
    mocks.flattenUniqueContent.mockReturnValue(content);
    mocks.getCoursesMeProgress.mockResolvedValue(
      ok({
        contentProgress: [
          { contentId: "in-progress", status: "InProgress" },
          { contentId: "completed-late", status: "Completed" },
          { contentId: "completed-early", status: "Completed" },
        ],
      }),
    );

    const result = await getCourseAttendanceData("game-production", {
      includeProgress: true,
    });

    expect(result?.currentItem).toMatchObject({
      id: "in-progress",
      status: "in-progress",
    });
    expect(result?.modules).toEqual([
      expect.objectContaining({
        id: "module-empty",
        title: "Untitled module",
        description: "",
        order: 0,
        items: [],
        progress: 0,
      }),
      expect.objectContaining({ id: "module-one", progress: 67 }),
    ]);
  });

  it("returns zero progress for an empty content tree", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesContent.mockResolvedValue(ok([]));

    await expect(
      getCourseAttendanceData("game-production"),
    ).resolves.toMatchObject({
      overallProgress: 0,
      totalItems: 0,
      completedItems: 0,
      currentItem: undefined,
      modules: [{ items: [], progress: 0 }],
    });
  });

  it("returns no attendance when course assembly throws", async () => {
    const errorSpy = vi
      .spyOn(console, "error")
      .mockImplementation(() => undefined);
    mocks.getCoursesSlug.mockRejectedValue(new Error("network down"));

    await expect(
      getCourseAttendanceData("game-production"),
    ).resolves.toBeNull();
    expect(errorSpy).toHaveBeenCalledWith(
      "[learning] Failed to build course attendance data",
      expect.any(Error),
    );
  });

  it.each([
    [failure(404), "not-found"],
    [failure(500), "unavailable"],
    [ok({ ...publicCourse, id: undefined }), "unavailable"],
  ] as const)(
    "maps an inaccessible course response",
    async (response, kind) => {
      mocks.getCoursesSlug.mockResolvedValue(response);

      await expect(getCourseAccessData("missing")).resolves.toMatchObject({
        kind,
      });
    },
  );

  it("reports an expired session while preserving public course details", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getToken.mockResolvedValue(null);

    await expect(getCourseAccessData("game-production")).resolves.toMatchObject(
      {
        kind: "unavailable",
        course: { id: "course-1" },
        message: "Your session expired. Sign in again to continue.",
      },
    );
  });

  it("returns ready access after verifying enrollment and attendance", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesMeProgress.mockResolvedValue(
      ok({ enrollmentId: "enrollment-1", contentProgress: [] }),
    );
    mocks.getCoursesContent.mockResolvedValue(ok([]));

    await expect(getCourseAccessData("game-production")).resolves.toMatchObject(
      {
        kind: "ready",
        course: { id: "course-1", enrollmentId: "enrollment-1" },
      },
    );
    expect(mocks.getCoursesMeProgress).toHaveBeenCalledTimes(2);
    await expect(
      mocks.createServerClient.mock.calls[1]?.[0].auth.getAccessToken(),
    ).resolves.toBe("access-token");
  });

  it("reports a temporarily unavailable classroom after enrollment verification", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesMeProgress.mockResolvedValue(ok({ contentProgress: [] }));
    mocks.getCoursesContent.mockResolvedValue(failure());

    await expect(getCourseAccessData("game-production")).resolves.toMatchObject(
      {
        kind: "unavailable",
        course: { id: "course-1" },
        message: "The classroom is temporarily unavailable.",
      },
    );
  });

  it("reports non-not-found enrollment verification failures", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesMeProgress.mockResolvedValue(failure(503));

    await expect(getCourseAccessData("game-production")).resolves.toMatchObject(
      {
        kind: "unavailable",
        message: "Your enrollment could not be verified. Try again.",
      },
    );
  });

  it("reports a closed enrollment after a missing enrollment", async () => {
    mocks.getCoursesSlug.mockResolvedValue(
      ok({ ...publicCourse, isEnrollmentOpen: false }),
    );
    mocks.getCoursesMeProgress.mockResolvedValue(failure(404));

    await expect(getCourseAccessData("game-production")).resolves.toMatchObject(
      {
        kind: "enrollment-closed",
      },
    );
  });

  it("requires enrollment when the course has no purchasable products", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesMeProgress.mockResolvedValue(failure(404));
    mocks.getCoursesProducts
      .mockResolvedValueOnce(failure())
      .mockResolvedValueOnce(ok([null, ""]));

    await expect(getCourseAccessData("game-production")).resolves.toMatchObject(
      {
        kind: "enrollment-required",
      },
    );
    await expect(getCourseAccessData("game-production")).resolves.toMatchObject(
      {
        kind: "enrollment-required",
      },
    );
  });

  it("returns configured and fallback pricing for paid enrollment", async () => {
    mocks.getCoursesSlug.mockResolvedValue(ok(publicCourse));
    mocks.getCoursesMeProgress.mockResolvedValue(failure(404));
    mocks.getCoursesProducts.mockResolvedValue(ok([null, "product-1"]));
    mocks.getCoursesPricing
      .mockResolvedValueOnce(ok({ price: 29.99, currency: "BRL" }))
      .mockResolvedValueOnce(failure());

    await expect(getCourseAccessData("game-production")).resolves.toMatchObject(
      {
        kind: "payment-required",
        price: 29.99,
        currency: "BRL",
      },
    );
    await expect(getCourseAccessData("game-production")).resolves.toMatchObject(
      {
        kind: "payment-required",
        price: null,
        currency: "USD",
      },
    );
  });

  it("returns unavailable when course access resolution throws", async () => {
    const errorSpy = vi
      .spyOn(console, "error")
      .mockImplementation(() => undefined);
    mocks.getCoursesSlug.mockRejectedValue(new Error("network down"));

    await expect(getCourseAccessData("game-production")).resolves.toEqual({
      kind: "unavailable",
      message: "Course access could not be verified. Try again.",
    });
    expect(errorSpy).toHaveBeenCalledWith(
      "[learning] Failed to resolve course access",
      expect.any(Error),
    );
  });

  it("loads enrolled courses with one dashboard request", async () => {
    mocks.getLearningMeDashboard.mockResolvedValue({
      ok: true,
      data: {
        courses: [
          {
            courseId: "course-1",
            enrollmentId: "enrollment-1",
            title: "Game Production",
            slug: "game-production",
            progressPercentage: 2_500,
          },
        ],
      },
    });

    await expect(getMyLearningCourses()).resolves.toEqual([
      expect.objectContaining({ id: "course-1", slug: "game-production" }),
    ]);
    expect(mocks.getLearningMeDashboard).toHaveBeenCalledTimes(1);
    expect(mocks.createServerClient).toHaveBeenCalledTimes(1);
    await expect(
      mocks.createServerClient.mock.calls[0]?.[0].auth.getAccessToken(),
    ).resolves.toBe("access-token");
  });

  it("uses the shared auth session when development relies on the auth fallback secret", async () => {
    delete process.env.AUTH_SECRET;
    mocks.getLearningMeDashboard.mockResolvedValue({
      ok: true,
      data: { courses: [] },
    });

    await expect(getLearnerDashboard()).resolves.toMatchObject({ courses: [] });
    expect(mocks.getToken).toHaveBeenCalledTimes(1);
    expect(mocks.getLearningMeDashboard).toHaveBeenCalledTimes(1);
  });

  it("returns no dashboard when the session has no token", async () => {
    mocks.getToken.mockResolvedValue(null);

    await expect(getLearnerDashboard()).resolves.toBeNull();
    expect(mocks.getLearningMeDashboard).not.toHaveBeenCalled();
  });

  it("returns null when the dashboard endpoint fails", async () => {
    mocks.getLearningMeDashboard.mockResolvedValue({
      ok: false,
      error: { message: "Unavailable" },
    });
    const errorSpy = vi
      .spyOn(console, "error")
      .mockImplementation(() => undefined);

    await expect(getLearnerDashboard()).resolves.toBeNull();
    expect(errorSpy).toHaveBeenCalled();
  });

  it("filters incomplete dashboard course summaries", async () => {
    mocks.getLearningMeDashboard.mockResolvedValue(
      ok({
        courses: [
          { courseId: "", slug: "missing-id" },
          { courseId: "course-2", slug: "" },
          { courseId: "course-3", slug: "valid" },
        ],
      }),
    );

    await expect(getMyLearningCourses()).resolves.toEqual([
      expect.objectContaining({ id: "course-3", slug: "valid" }),
    ]);
  });

  it("returns no courses when a dashboard omits its course collection", async () => {
    mocks.getLearningMeDashboard.mockResolvedValue(ok({}));

    await expect(getMyLearningCourses()).resolves.toEqual([]);
  });

  it("rethrows framework errors before recovering from a dashboard exception", async () => {
    const error = new Error("dashboard unavailable");
    const errorSpy = vi
      .spyOn(console, "error")
      .mockImplementation(() => undefined);
    mocks.getLearningMeDashboard.mockRejectedValue(error);

    await expect(getMyLearningCourses()).resolves.toEqual([]);
    expect(mocks.unstableRethrow).toHaveBeenCalledWith(error);
    expect(errorSpy).toHaveBeenCalledWith(
      "[learning] Failed to build learner dashboard",
      error,
    );
  });
});

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  getLearningMeSearch: vi.fn(),
  createServerClient: vi.fn(() => ({ client: true })),
  getToken: vi.fn(),
}));

vi.mock("@/auth", () => ({ getToken: mocks.getToken }));
vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningWorkspacesLearnerWorkspaceModule: class {
      getLearningMeSearch = mocks.getLearningMeSearch;
    },
  },
}));

const { searchLearnerWorkspace } = await import("./search-actions");

describe("searchLearnerWorkspace", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue("access-token");
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("does not call the API until the query has at least two characters", async () => {
    await expect(searchLearnerWorkspace(" a ")).resolves.toEqual({
      success: true,
      items: [],
    });
    expect(mocks.getLearningMeSearch).not.toHaveBeenCalled();
  });

  it("returns only safe, navigable results from the permission-filtered API", async () => {
    mocks.getLearningMeSearch.mockResolvedValue({
      ok: true,
      data: [
        {
          id: "course-1",
          kind: "Course",
          title: "Game AI",
          description: "Advanced agents",
          route: "/learn/courses/game-ai",
        },
        {
          id: "unsafe",
          kind: "Lesson",
          title: "External redirect",
          route: "//malicious.example",
        },
        { id: "incomplete", title: "No route" },
      ],
    });

    await expect(searchLearnerWorkspace(" game ")).resolves.toEqual({
      success: true,
      items: [
        {
          id: "course-1",
          kind: "Course",
          title: "Game AI",
          description: "Advanced agents",
          route: "/learn/courses/game-ai",
        },
      ],
    });
    expect(mocks.getLearningMeSearch).toHaveBeenCalledWith({
      q: "game",
      take: 12,
    });
  });

  it("surfaces API failures as a recoverable search state", async () => {
    mocks.getLearningMeSearch.mockResolvedValue({
      ok: false,
      error: { detail: "Search service unavailable" },
    });

    await expect(searchLearnerWorkspace("design")).resolves.toEqual({
      success: false,
      error: "Search service unavailable",
    });
  });

  it("filters every incomplete and unsafe result while applying display defaults", async () => {
    mocks.getLearningMeSearch.mockResolvedValue({
      ok: true,
      data: [
        { title: "No id", route: "/learn/no-id" },
        { id: "no-title", route: "/learn/no-title" },
        { id: "no-route", title: "No route" },
        { id: "relative", title: "Relative route", route: "learn/relative" },
        {
          id: "protocol-relative",
          title: "Unsafe route",
          route: "//example.com",
        },
        { id: "safe", title: "Safe resource", route: "/learn/safe" },
      ],
    });

    await expect(searchLearnerWorkspace("resources")).resolves.toEqual({
      success: true,
      items: [
        {
          id: "safe",
          kind: "Learning resource",
          title: "Safe resource",
          description: "",
          route: "/learn/safe",
        },
      ],
    });
  });

  it("falls back from API error details to messages and a safe status", async () => {
    mocks.getLearningMeSearch
      .mockResolvedValueOnce({
        ok: false,
        error: { message: "Search index is rebuilding." },
      })
      .mockResolvedValueOnce({ ok: false, error: {} });

    await expect(searchLearnerWorkspace("first")).resolves.toEqual({
      success: false,
      error: "Search index is rebuilding.",
    });
    await expect(searchLearnerWorkspace("second")).resolves.toEqual({
      success: false,
      error: "Learning search is temporarily unavailable.",
    });
  });

  it("normalizes thrown Error and unknown failures", async () => {
    mocks.getLearningMeSearch
      .mockRejectedValueOnce(new Error("Search connection failed."))
      .mockRejectedValueOnce(new Error(""))
      .mockRejectedValueOnce("offline");

    await expect(searchLearnerWorkspace("first")).resolves.toEqual({
      success: false,
      error: "Search connection failed.",
    });
    await expect(searchLearnerWorkspace("second")).resolves.toEqual({
      success: false,
      error: "Learning search is temporarily unavailable.",
    });
    await expect(searchLearnerWorkspace("third")).resolves.toEqual({
      success: false,
      error: "Learning search is temporarily unavailable.",
    });
  });

  it("configures server, public, and local search clients with the request token", async () => {
    mocks.getLearningMeSearch.mockResolvedValue({ ok: true, data: [] });
    vi.stubEnv("API_URL", "https://server-api.example");
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://public-api.example");
    await searchLearnerWorkspace("server");
    let options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("https://server-api.example");
    await expect(options.auth.getAccessToken()).resolves.toBe("access-token");

    vi.stubEnv("API_URL", "");
    await searchLearnerWorkspace("public");
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("https://public-api.example");

    vi.stubEnv("NEXT_PUBLIC_API_URL", "");
    await searchLearnerWorkspace("local");
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe("http://localhost:8080");
  });
});

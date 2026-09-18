import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  authContext: vi.fn(),
}));

vi.mock("@/auth", () => ({ getRequestAuthContext: mocks.authContext }));

import {
  applyAiProposal,
  cancelAiAuthoringRun,
  createAiAuthoringRun,
  discardAiProposal,
  getAiAuthoringRun,
  getAiConversations,
  getAiEntitlement,
  getAuthoringDraft,
  publishAuthoringDraft,
  saveAuthoringDraft,
} from "./authoring";

describe("authoring server actions", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    mocks.authContext.mockResolvedValue({
      token: "signed-token",
      tenantId: "tenant-1",
    });
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("derives billing identity exclusively from the authenticated server context", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(JSON.stringify({ id: "run-1" }), {
        status: 202,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await createAiAuthoringRun("course-1", "lesson-1", {
      draftRevision: 2,
      instruction: "Expand this explanation",
      proposalKind: "ReplaceDocument",
      idempotencyKey: "request-1",
    });

    const [, init] = fetchMock.mock.calls[0]!;
    expect(init?.headers).toMatchObject({
      Authorization: "Bearer signed-token",
      "X-Tenant-Id": "tenant-1",
    });
    const body = JSON.parse(String(init?.body)) as Record<string, unknown>;
    expect(body).not.toHaveProperty("actorId");
    expect(body).not.toHaveProperty("userId");
    expect(body).not.toHaveProperty("tenantId");
  });

  it("does not call the API when no authenticated tenant actor exists", async () => {
    mocks.authContext.mockResolvedValue({ token: null, tenantId: null });
    const fetchMock = vi.spyOn(globalThis, "fetch");

    const result = await saveAuthoringDraft(
      "course-1",
      "lesson-1",
      1,
      {} as never,
    );

    expect(result).toMatchObject({ success: false, status: 401 });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("rejects authentication contexts missing either the token or tenant", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch");
    mocks.authContext
      .mockResolvedValueOnce({ token: null, tenantId: "tenant-1" })
      .mockResolvedValueOnce({ token: "signed-token", tenantId: null });

    await expect(
      getAuthoringDraft("course-1", "lesson-1"),
    ).resolves.toMatchObject({
      success: false,
      status: 401,
    });
    await expect(
      getAuthoringDraft("course-1", "lesson-1"),
    ).resolves.toMatchObject({
      success: false,
      status: 401,
    });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("surfaces the server's current revision on optimistic concurrency conflicts", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(
        JSON.stringify({
          code: "AUTHORING_REVISION_CONFLICT",
          detail: "stale",
          currentRevision: 7,
        }),
        { status: 409, headers: { "Content-Type": "application/json" } },
      ),
    );

    const result = await saveAuthoringDraft(
      "course-1",
      "lesson-1",
      4,
      {} as never,
    );

    expect(result).toEqual({
      success: false,
      error: "stale",
      status: 409,
      code: "AUTHORING_REVISION_CONFLICT",
      currentRevision: 7,
    });
  });

  it("cancels only the authenticated actor's run through the scoped authoring endpoint", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(JSON.stringify({ id: "run-1", status: "Cancelled" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await cancelAiAuthoringRun("course-1", "lesson-1", "run-1");

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining(
        "/v1/courses/course-1/content/lesson-1/authoring/ai/runs/run-1/cancel",
      ),
      expect.objectContaining({
        method: "POST",
        headers: expect.objectContaining({
          Authorization: "Bearer signed-token",
          "X-Tenant-Id": "tenant-1",
        }),
      }),
    );
  });

  it("routes every authoring operation through its encoded scoped endpoint", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockImplementation(
      async () =>
        new Response(JSON.stringify({ id: "result-1" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
    );
    const courseId = "course/with spaces";
    const contentId = "lesson/one";

    await getAuthoringDraft(courseId, contentId);
    await publishAuthoringDraft(courseId, contentId, 3);
    await getAiEntitlement(courseId, contentId);
    await getAiConversations(courseId, contentId);
    await getAiAuthoringRun(courseId, contentId, "run/one");
    await applyAiProposal(courseId, contentId, "proposal/one", 4, 12);
    await discardAiProposal(courseId, contentId, "proposal/one");

    const calls = fetchMock.mock.calls;
    expect(calls[0]).toEqual([
      expect.stringContaining(
        "/v1/courses/course%2Fwith%20spaces/content/lesson%2Fone/authoring",
      ),
      expect.objectContaining({ cache: "no-store" }),
    ]);
    expect(calls[1]).toEqual([
      expect.stringContaining("/authoring/publish"),
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ revision: 3 }),
      }),
    ]);
    expect(calls[2]?.[0]).toEqual(
      expect.stringContaining("/authoring/ai/entitlement"),
    );
    expect(calls[3]?.[0]).toEqual(
      expect.stringContaining("/authoring/ai/conversations"),
    );
    expect(calls[4]?.[0]).toEqual(
      expect.stringContaining("/authoring/ai/runs/run%2Fone"),
    );
    expect(calls[5]).toEqual([
      expect.stringContaining("/authoring/ai/proposals/proposal%2Fone/apply"),
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ draftRevision: 4, cursorOffset: 12 }),
      }),
    ]);
    expect(calls[6]).toEqual([
      expect.stringContaining("/authoring/ai/proposals/proposal%2Fone"),
      expect.objectContaining({ method: "DELETE" }),
    ]);
    expect(calls[0]?.[1]?.headers).not.toHaveProperty("Content-Type");
    expect(calls[1]?.[1]?.headers).toMatchObject({
      "Content-Type": "application/json",
    });
  });

  it("returns successful empty data when the server response has no JSON body", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response("not-json", { status: 200 }),
    );

    await expect(getAuthoringDraft("course-1", "lesson-1")).resolves.toEqual({
      success: true,
      data: null,
    });
  });

  it("normalizes API errors from title, status text, and the final fallback", async () => {
    vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({ title: "Validation failed", code: 123 }),
          {
            status: 422,
            headers: { "Content-Type": "application/json" },
          },
        ),
      )
      .mockResolvedValueOnce(
        new Response("not-json", {
          status: 503,
          statusText: "Service Unavailable",
        }),
      )
      .mockResolvedValueOnce(
        new Response("not-json", { status: 500, statusText: "" }),
      );

    await expect(getAuthoringDraft("course-1", "lesson-1")).resolves.toEqual({
      success: false,
      error: "Validation failed",
      status: 422,
      code: undefined,
      currentRevision: undefined,
    });
    await expect(
      getAuthoringDraft("course-1", "lesson-1"),
    ).resolves.toMatchObject({
      success: false,
      error: "Service Unavailable",
      status: 503,
    });
    await expect(
      getAuthoringDraft("course-1", "lesson-1"),
    ).resolves.toMatchObject({
      success: false,
      error: "Authoring request failed.",
      status: 500,
    });
  });

  it("uses API URL configuration in server, public, and local priority order", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(JSON.stringify({ id: "draft-1" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    vi.stubEnv("API_URL", "https://server-api.example/");
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://public-api.example/");
    vi.resetModules();
    let actions = await import("./authoring");
    await actions.getAuthoringDraft("course-1", "lesson-1");
    expect(vi.mocked(fetch).mock.calls.at(-1)?.[0]).toBe(
      "https://server-api.example/v1/courses/course-1/content/lesson-1/authoring",
    );

    vi.stubEnv("API_URL", "");
    vi.resetModules();
    actions = await import("./authoring");
    await actions.getAuthoringDraft("course-1", "lesson-1");
    expect(vi.mocked(fetch).mock.calls.at(-1)?.[0]).toBe(
      "https://public-api.example/v1/courses/course-1/content/lesson-1/authoring",
    );

    vi.stubEnv("NEXT_PUBLIC_API_URL", "");
    vi.resetModules();
    actions = await import("./authoring");
    await actions.getAuthoringDraft("course-1", "lesson-1");
    expect(vi.mocked(fetch).mock.calls.at(-1)?.[0]).toBe(
      "http://localhost:8080/v1/courses/course-1/content/lesson-1/authoring",
    );
  });
});

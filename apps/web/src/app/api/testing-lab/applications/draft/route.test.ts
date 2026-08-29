import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  getRequestAuthContext: vi.fn(),
}));

vi.mock("@/auth", () => ({
  getRequestAuthContext: mocks.getRequestAuthContext,
}));

import { POST } from "./route";

function createDraftRequest(
  origin: string,
  payload: Record<string, unknown> = {
    eventId: "event-id",
    projectId: "project-id",
  },
): NextRequest {
  return new NextRequest(
    "http://localhost:44079/api/testing-lab/applications/draft",
    {
      method: "POST",
      headers: {
        "content-type": "application/json",
        host: "127.0.0.1:44079",
        origin,
      },
      body: JSON.stringify(payload),
    },
  );
}

describe("Testing Lab application draft route", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getRequestAuthContext.mockResolvedValue({
      session: null,
      token: null,
      tenantId: null,
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("accepts the browser origin from the request host when the internal URL differs", async () => {
    const response = await POST(createDraftRequest("http://127.0.0.1:44079"));

    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toEqual({
      success: false,
      error: "You must be signed in to save a project application.",
    });
    expect(mocks.getRequestAuthContext).toHaveBeenCalledOnce();
  });

  it("rejects a draft request from a different origin", async () => {
    const response = await POST(
      createDraftRequest("https://untrusted.example"),
    );

    expect(response.status).toBe(403);
    await expect(response.json()).resolves.toEqual({
      success: false,
      error: "Cross-origin requests are not allowed.",
    });
  });

  it("submits with the same request token and tenant used to save the draft", async () => {
    mocks.getRequestAuthContext.mockResolvedValue({
      session: { tenantId: "tenant-id" },
      token: "access-token",
      tenantId: "tenant-id",
    });
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        Response.json({ id: "application-id", status: "Draft" }),
      )
      .mockResolvedValueOnce(
        Response.json({ id: "application-id", status: "Draft" }),
      )
      .mockResolvedValueOnce(
        Response.json({ id: "application-id", status: "Pending" }),
      );
    vi.stubGlobal("fetch", fetchMock);

    const response = await POST(
      createDraftRequest("http://127.0.0.1:44079", {
        eventId: "event-id",
        projectId: "project-id",
        intent: "submit",
      }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      success: true,
      message: "Project application submitted.",
      data: { id: "application-id", status: "Pending" },
    });
    expect(mocks.getRequestAuthContext).toHaveBeenCalledOnce();
    expect(fetchMock).toHaveBeenCalledTimes(3);
    for (const [, options] of fetchMock.mock.calls) {
      expect(options).toMatchObject({
        headers: expect.objectContaining({
          Authorization: "Bearer access-token",
          "X-Tenant-Id": "tenant-id",
        }),
      });
    }
  });
});

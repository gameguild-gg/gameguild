import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({ getToken: vi.fn(), fetch: vi.fn() }));
vi.mock("@/auth", () => ({ getToken: mocks.getToken }));

import { GET, POST } from "./route";

describe("learning asset collection proxy", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("fetch", mocks.fetch);
    vi.stubEnv("API_URL", "http://localhost:8080");
    mocks.getToken.mockResolvedValue("author-token");
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  it("lists parent-scoped assets in one authenticated API request", async () => {
    mocks.fetch.mockResolvedValue(new Response(JSON.stringify([
      { id: "asset-1", displayName: "Cover image", content: { contentHash: "abc", mimeType: "image/png", sizeBytes: 3 } },
      { id: "asset-2", displayName: "Notes", content: { contentHash: "def", mimeType: "text/plain", sizeBytes: 2 } },
    ]), { status: 200, headers: { "content-type": "application/json" } }));
    const request = new NextRequest(
      "http://localhost/api/assets?resourceType=ProgramContent&resourceId=11111111-1111-4111-8111-111111111111&search=cover&limit=10",
    );

    const response = await GET(request);

    expect(mocks.fetch).toHaveBeenCalledTimes(1);
    expect(mocks.fetch).toHaveBeenCalledWith(
      "http://localhost:8080/v1/assets?parentType=ProgramContent&parentId=11111111-1111-4111-8111-111111111111",
      expect.objectContaining({
        cache: "no-store",
        headers: { authorization: "Bearer author-token" },
        signal: request.signal,
      }),
    );
    await expect(response.json()).resolves.toEqual({
      items: [expect.objectContaining({ id: "asset-1", displayName: "Cover image" })],
    });
  });

  it("forwards the stable reference id and parent scope on upload", async () => {
    mocks.fetch.mockResolvedValue(new Response(JSON.stringify({ assetReferenceId: "asset-1" }), {
      status: 201,
      headers: { "content-type": "application/json" },
    }));
    const request = new NextRequest(
      "http://localhost/api/assets?referenceId=asset-1&displayName=cover.png&accessPolicy=Private&parentResourceType=ProgramContent&parentResourceId=lesson-1&actorId=attacker",
      { method: "POST" },
    );
    const form = new FormData();
    form.set("file", new Blob(["png"], { type: "image/png" }), "cover.png");
    vi.spyOn(request, "formData").mockResolvedValue(form);

    const response = await POST(request);

    expect(response.status).toBe(201);
    const [url, init] = mocks.fetch.mock.calls[0] as [URL, RequestInit];
    expect(url.toString()).toBe(
      "http://localhost:8080/v1/assets?displayName=cover.png&accessPolicy=Private&parentResourceType=ProgramContent&parentResourceId=lesson-1&referenceId=asset-1",
    );
    expect(init.headers).toEqual({ authorization: "Bearer author-token" });
    expect(init.signal).toBe(request.signal);
    expect((init.body as FormData).get("file")).toBeInstanceOf(File);
  });

  it("rejects anonymous collection access", async () => {
    mocks.getToken.mockResolvedValue(null);
    const response = await GET(new NextRequest(
      "http://localhost/api/assets?resourceType=ProgramContent&resourceId=11111111-1111-4111-8111-111111111111",
    ));
    expect(response.status).toBe(401);
    expect(mocks.fetch).not.toHaveBeenCalled();
  });
});

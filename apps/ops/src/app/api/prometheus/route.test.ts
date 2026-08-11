import { describe, it, expect, vi, beforeEach } from "vitest";

vi.mock("../../../lib/prometheus", () => ({
  PROMETHEUS_URL: "http://prom-mock:9090",
}));

import { POST } from "./route";

function makeReq(body: unknown): Request {
  return new Request("http://localhost/api/prometheus", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify(body),
  });
}

describe("prometheus/route POST", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("returns 400 when query missing or empty", async () => {
    const missing = await POST(makeReq({}));
    expect(missing.status).toBe(400);
    expect(await missing.json()).toEqual({ error: "query required" });

    const empty = await POST(makeReq({ query: "" }));
    expect(empty.status).toBe(400);
  });

  it("proxies query and returns raw prometheus json", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ status: "success", data: { result: [] } }), {
        status: 200,
        headers: { "content-type": "application/json" },
      }),
    );
    globalThis.fetch = fetchMock as unknown as typeof fetch;

    const res = await POST(makeReq({ query: "up" }));
    expect(res.status).toBe(200);
    expect(await res.json()).toEqual({ status: "success", data: { result: [] } });
    expect(fetchMock).toHaveBeenCalledOnce();
    const [calledUrl, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(calledUrl).toBe("http://prom-mock:9090/api/v1/query?query=up");
    expect(init).toMatchObject({ cache: "no-store" });
  });

  it("URL-encodes the query string", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response("{}", { status: 200, headers: { "content-type": "application/json" } }),
    );
    globalThis.fetch = fetchMock as unknown as typeof fetch;

    await POST(makeReq({ query: 'sum(rate(foo[5m])) by (instance="a b")' }));
    const [calledUrl] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(calledUrl).toContain(encodeURIComponent('sum(rate(foo[5m])) by (instance="a b")'));
  });

  it("returns 500 when fetch rejects", async () => {
    globalThis.fetch = vi.fn().mockRejectedValue(new Error("network down")) as unknown as typeof fetch;
    const res = await POST(makeReq({ query: "up" }));
    expect(res.status).toBe(500);
    expect((await res.json()).error).toContain("network down");
  });
});

import { describe, it, expect, vi, beforeEach } from "vitest";
import { GET } from "./route";

function jsonRes(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });
}

describe("alerts/route GET", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("maps alertmanager payload to {name,severity,status,activeAt,summary}", async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      jsonRes([
        {
          labels: { alertname: "HighCPU", severity: "critical", summary: "label-sum" },
          annotations: { summary: "annot-sum" },
          status: { state: "firing" },
          activeAt: "2024-01-01T00:00:00Z",
        },
      ]),
    ) as unknown as typeof fetch;

    const res = await GET();
    expect(res.status).toBe(200);
    expect(await res.json()).toEqual([
      {
        name: "HighCPU",
        severity: "critical",
        status: "firing",
        activeAt: "2024-01-01T00:00:00Z",
        summary: "annot-sum",
      },
    ]);
  });

  it("falls back to labels.summary when annotation missing", async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      jsonRes([
        {
          labels: { alertname: "DiskFull", severity: "warning", summary: "label-sum" },
          status: { state: "pending" },
          activeAt: "2024-01-02T00:00:00Z",
        },
      ]),
    ) as unknown as typeof fetch;

    const body = await (await GET()).json();
    expect(body[0].summary).toBe("label-sum");
  });

  it("returns 500 when alertmanager is unreachable", async () => {
    globalThis.fetch = vi.fn().mockRejectedValue(new Error("conn refused")) as unknown as typeof fetch;
    const res = await GET();
    expect(res.status).toBe(500);
    expect((await res.json()).error).toContain("conn refused");
  });
});

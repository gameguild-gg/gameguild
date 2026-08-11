import { describe, it, expect, vi, beforeEach } from "vitest";
import { GET } from "./route";

function jsonRes(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });
}

describe("services/route GET", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("returns pass for all 6 services when each predicate matches", async () => {
    globalThis.fetch = vi.fn(async (input: URL | RequestInfo, _init?: RequestInit) => {
      const u = String(input);
      if (u.includes("/api/healthz")) return jsonRes({ status: "pass" });
      if (u.includes("devtron-service")) return jsonRes({ result: "OK" });
      if (u.includes("grafana")) return jsonRes({ database: "ok" });
      if (u.endsWith("/v2/")) return new Response("auth required", { status: 401 });
      return new Response("", { status: 200 });
    }) as unknown as typeof fetch;

    const res = await GET();
    expect(res.status).toBe(200);
    const body = (await res.json()) as Array<{ name: string; status: string; httpStatus: number }>;
    expect(body).toHaveLength(6);
    for (const r of body) {
      expect(r.status).toBe("pass");
    }
  });

  it("returns fail for Forgejo when its body predicate misses, others still pass", async () => {
    globalThis.fetch = vi.fn(async (input: URL | RequestInfo, _init?: RequestInit) => {
      const u = String(input);
      if (u.includes("/api/healthz")) return jsonRes({ status: "fail" });
      if (u.includes("devtron-service")) return jsonRes({ result: "OK" });
      if (u.includes("grafana")) return jsonRes({ database: "ok" });
      if (u.endsWith("/v2/")) return new Response("", { status: 401 });
      return new Response("", { status: 200 });
    }) as unknown as typeof fetch;

    const body = (await (await GET()).json()) as Array<{ name: string; status: string }>;
    const byName = Object.fromEntries(body.map((r) => [r.name, r.status]));
    expect(byName.Forgejo).toBe("fail");
    expect(byName.Devtron).toBe("pass");
    expect(byName.Grafana).toBe("pass");
    expect(byName.API).toBe("pass");
    expect(byName.Web).toBe("pass");
    expect(byName.Registry).toBe("pass");
    expect(body).toHaveLength(6);
  });

  it("returns fail for Registry when registry returns 200 instead of 401", async () => {
    globalThis.fetch = vi.fn(async () => new Response("", { status: 200 })) as unknown as typeof fetch;
    const body = (await (await GET()).json()) as Array<{ name: string; status: string; httpStatus: number }>;
    const registry = body.find((r) => r.name === "Registry");
    expect(registry?.status).toBe("fail");
    expect(registry?.httpStatus).toBe(200);
  });

  it("returns fail with httpStatus=0 for every service when fetch rejects", async () => {
    globalThis.fetch = vi.fn().mockRejectedValue(new Error("ECONNREFUSED")) as unknown as typeof fetch;
    const body = (await (await GET()).json()) as Array<{ status: string; httpStatus: number }>;
    expect(body).toHaveLength(6);
    expect(body.every((r) => r.status === "fail")).toBe(true);
    expect(body.every((r) => r.httpStatus === 0)).toBe(true);
  });

  it("always returns 200 even when all probes fail", async () => {
    globalThis.fetch = vi.fn().mockRejectedValue(new Error("down")) as unknown as typeof fetch;
    const res = await GET();
    expect(res.status).toBe(200);
  });
});

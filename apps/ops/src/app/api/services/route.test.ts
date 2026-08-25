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

  it("returns pass for all 8 services when each predicate matches", async () => {
    globalThis.fetch = vi.fn(async (input: URL | RequestInfo, _init?: RequestInit) => {
      const u = String(input);
      if (u.includes("/api/healthz")) return jsonRes({ status: "pass" });
      if (u.includes("devtron-service")) return jsonRes({ result: "OK" });
      if (u.includes("grafana")) return jsonRes({ database: "ok" });
      if (u.includes("metrics")) return new Response("# HELP redis_up\nredis_up 1\n", { status: 200 });
      if (u.endsWith("/v2/")) return new Response("auth required", { status: 401 });
      return new Response("", { status: 200 });
    }) as unknown as typeof fetch;

    const res = await GET();
    expect(res.status).toBe(200);
    const body = (await res.json()) as Array<{ name: string; status: string; httpStatus: number }>;
    expect(body).toHaveLength(8);
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
      if (u.includes("metrics")) return new Response("# HELP redis_up\nredis_up 1\n", { status: 200 });
      if (u.endsWith("/v2/")) return new Response("", { status: 401 });
      return new Response("", { status: 200 });
    }) as unknown as typeof fetch;

    const body = (await (await GET()).json()) as Array<{ name: string; status: string }>;
    const byName = Object.fromEntries(body.map((r) => [r.name, r.status]));
    expect(byName.Forgejo).toBe("fail");
    expect(byName.Devtron).toBe("pass");
    expect(byName.Grafana).toBe("pass");
    expect(byName.Redis).toBe("pass");
    expect(byName.API).toBe("pass");
    expect(byName.Web).toBe("pass");
    expect(byName.Registry).toBe("pass");
    expect(byName.AFFiNE).toBe("pass");
    expect(body).toHaveLength(8);
  });

  it("returns fail for AFFiNE when /info returns 500, others still pass", async () => {
    globalThis.fetch = vi.fn(async (input: URL | RequestInfo, _init?: RequestInit) => {
      const u = String(input);
      if (u.includes("/info")) return new Response("", { status: 500 });
      if (u.includes("/api/healthz")) return jsonRes({ status: "pass" });
      if (u.includes("devtron-service")) return jsonRes({ result: "OK" });
      if (u.includes("grafana")) return jsonRes({ database: "ok" });
      if (u.includes("metrics")) return new Response("# HELP redis_up\nredis_up 1\n", { status: 200 });
      if (u.endsWith("/v2/")) return new Response("", { status: 401 });
      return new Response("", { status: 200 });
    }) as unknown as typeof fetch;

    const body = (await (await GET()).json()) as Array<{ name: string; status: string }>;
    const byName = Object.fromEntries(body.map((r) => [r.name, r.status]));
    expect(byName.AFFiNE).toBe("fail");
    expect(body).toHaveLength(8);
    expect(body.filter((r) => r.status === "pass")).toHaveLength(7);
  });

  it("returns fail for Redis when the exporter reports redis_up 0", async () => {
    globalThis.fetch = vi.fn(async (input: URL | RequestInfo) => {
      const u = String(input);
      if (u.includes("metrics")) return new Response("# HELP redis_up\nredis_up 0\n", { status: 200 });
      return jsonRes({});
    }) as unknown as typeof fetch;

    const body = (await (await GET()).json()) as Array<{ name: string; status: string }>;
    const redis = body.find((r) => r.name === "Redis");
    expect(redis?.status).toBe("fail");
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
    expect(body).toHaveLength(8);
    expect(body.every((r) => r.status === "fail")).toBe(true);
    expect(body.every((r) => r.httpStatus === 0)).toBe(true);
  });

  it("always returns 200 even when all probes fail", async () => {
    globalThis.fetch = vi.fn().mockRejectedValue(new Error("down")) as unknown as typeof fetch;
    const res = await GET();
    expect(res.status).toBe(200);
  });
});

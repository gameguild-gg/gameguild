import { NextResponse } from "next/server";

export const dynamic = "force-dynamic";

type ServiceResult = {
  name: string;
  url: string;
  status: "pass" | "fail";
  responseTimeMs: number;
  httpStatus: number;
};

type ServiceProbe = {
  name: string;
  url: string;
  pass: (body: unknown, status: number) => boolean;
};

// ponytail: 8 services hardcoded. They change ~once a year; no service-registry
// abstraction is worth the indirection. Add when count > 12 or probes fan out.
const SERVICES: readonly ServiceProbe[] = [
  {
    name: "Forgejo",
    url: "http://forgejo-gitea-http.forgejo:3000/api/healthz",
    pass: (b, s) => s === 200 && (b as { status?: string } | null)?.status === "pass",
  },
  {
    name: "Devtron",
    url: "http://devtron-service.devtroncd:80/health",
    pass: (b, s) => s === 200 && (b as { result?: string } | null)?.result === "OK",
  },
  {
    name: "Grafana",
    url: "http://kube-prometheus-stack-grafana.monitoring:80/api/health",
    pass: (b, s) => s === 200 && (b as { database?: string } | null)?.database === "ok",
  },
  {
    name: "Redis",
    url: "http://redis-metrics.redis:9121/metrics",
    pass: (b, s) => s === 200 && String(b).includes("redis_up 1"),
  },
  {
    name: "API",
    url: "http://api-production-service.prod.svc.cluster.local:80/",
    pass: (_b, s) => s === 200,
  },
  {
    name: "Web",
    url: "http://web-production-service.prod.svc.cluster.local:80/",
    pass: (_b, s) => s === 200,
  },
  {
    name: "Registry",
    url: "http://forgejo-gitea-http.forgejo:3000/v2/",
    pass: (_b, s) => s === 401,
  },
  {
    name: "AFFiNE",
    url: "http://affine.affine.svc.cluster.local:3010/info",
    pass: (_b, s) => s === 200,
  },
];

async function probe(service: ServiceProbe): Promise<ServiceResult> {
  const start = Date.now();
  try {
    const res = await fetch(service.url, {
      cache: "no-store",
      signal: AbortSignal.timeout(5000),
    });
    const responseTimeMs = Date.now() - start;
    const httpStatus = res.status;
    // Read the stream once (json() would consume it), then parse; non-JSON
    // bodies (prometheus text exposition) reach predicates as raw text.
    const raw = await res.text().catch(() => undefined);
    let body: unknown = raw;
    if (typeof raw === "string" && raw.length > 0) {
      try {
        body = JSON.parse(raw);
      } catch {
        // text body, keep raw
      }
    }
    return {
      name: service.name,
      url: service.url,
      status: service.pass(body, httpStatus) ? "pass" : "fail",
      responseTimeMs,
      httpStatus,
    };
  } catch {
    return {
      name: service.name,
      url: service.url,
      status: "fail",
      responseTimeMs: Date.now() - start,
      httpStatus: 0,
    };
  }
}

export async function GET(): Promise<Response> {
  // Promise.allSettled per spec: probe() swallows its own errors so this never
  // rejects, but allSettled keeps the array shape contract even if a future
  // change to probe() lets an exception escape.
  const results = await Promise.allSettled(SERVICES.map(probe));
  const body: ServiceResult[] = results.map((r) =>
    r.status === "fulfilled"
      ? r.value
      : { name: "unknown", url: "unknown", status: "fail", responseTimeMs: 0, httpStatus: 0 },
  );
  return NextResponse.json(body);
}

import { NextResponse } from "next/server";
import { PROMETHEUS_URL } from "../../../lib/prometheus";

export const dynamic = "force-dynamic";

export async function POST(request: Request): Promise<Response> {
  try {
    const { query } = (await request.json()) as { query?: unknown };
    if (typeof query !== "string" || query.length === 0) {
      return NextResponse.json({ error: "query required" }, { status: 400 });
    }
    const url = `${PROMETHEUS_URL}/api/v1/query?query=${encodeURIComponent(query)}`;
    const res = await fetch(url, {
      cache: "no-store",
      signal: AbortSignal.timeout(10_000),
    });
    if (!res.ok) {
      const body = await res.text();
      return NextResponse.json(
        { error: `prometheus ${res.status}: ${body}` },
        { status: res.status },
      );
    }
    return NextResponse.json(await res.json());
  } catch (err) {
    return NextResponse.json(
      { error: err instanceof Error ? err.message : "prometheus proxy failed" },
      { status: 500 },
    );
  }
}

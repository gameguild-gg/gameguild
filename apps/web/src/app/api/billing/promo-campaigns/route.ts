import { getRequestAuthContext } from "@/auth";
import { type NextRequest, NextResponse } from "next/server";

export const dynamic = "force-dynamic";

/** Dashboard page size for the campaign list; the API caps take at its own bound. */
const PAGE_TAKE = 50;

const LIFECYCLE_ACTIONS = new Set(["activate", "pause", "complete", "cancel"]);

function apiBaseUrl(): string {
  return (process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080").replace(/\/$/, "");
}

function upstreamHeaders(token: string): Record<string, string> {
  return { authorization: `Bearer ${token}`, accept: "application/json", "content-type": "application/json" };
}

/**
 * Browser-facing promo-campaigns dashboard proxy (issue #418). The caller's session
 * token authorizes every upstream call server-side; the promo-campaigns controller
 * keeps enforcing its permission policy. GET without a campaign id returns the paged
 * campaign list; GET with a campaign id returns the windowed performance report;
 * POST issues one lifecycle transition (activate/pause/complete/cancel).
 */
export async function GET(request: NextRequest): Promise<Response> {
  const { token } = (await getRequestAuthContext()) as { token: string | null };
  if (!token) return NextResponse.json({ error: "Authentication required." }, { status: 401 });

  const parameters = new URL(request.url).searchParams;
  const campaignId = parameters.get("campaignId");
  const headers = upstreamHeaders(token);

  if (campaignId) {
    const response = await fetch(
      `${apiBaseUrl()}/api/v1/promo-campaigns/${encodeURIComponent(campaignId)}/performance`,
      { headers, cache: "no-store", signal: request.signal },
    );
    return passthrough(response);
  }

  const listParameters = new URLSearchParams();
  listParameters.set("skip", parameters.get("skip") ?? "0");
  listParameters.set("take", parameters.get("take") ?? String(PAGE_TAKE));
  const status = parameters.get("status");
  const searchTerm = parameters.get("searchTerm");
  if (status) listParameters.set("status", status);
  if (searchTerm) listParameters.set("searchTerm", searchTerm);

  const response = await fetch(`${apiBaseUrl()}/api/v1/promo-campaigns?${listParameters.toString()}`, {
    headers,
    cache: "no-store",
    signal: request.signal,
  });
  return passthrough(response);
}

/** Issues one campaign lifecycle transition; the API validates the state machine. */
export async function POST(request: NextRequest): Promise<Response> {
  const { token } = (await getRequestAuthContext()) as { token: string | null };
  if (!token) return NextResponse.json({ error: "Authentication required." }, { status: 401 });

  const body = (await request.json().catch(() => null)) as { campaignId?: string; action?: string } | null;
  if (!body?.campaignId || !body.action || !LIFECYCLE_ACTIONS.has(body.action)) {
    return NextResponse.json({ error: "A campaignId and a supported action are required." }, { status: 400 });
  }

  const response = await fetch(
    `${apiBaseUrl()}/api/v1/promo-campaigns/${encodeURIComponent(body.campaignId)}:${body.action}`,
    {
      method: "POST",
      headers: upstreamHeaders(token),
      cache: "no-store",
      signal: request.signal,
    },
  );
  return passthrough(response);
}

async function passthrough(upstream: Response): Promise<Response> {
  if (!upstream.ok) {
    return new Response((await upstream.text()) || null, {
      status: upstream.status,
      headers: {
        "content-type": upstream.headers.get("content-type") ?? "application/json",
        "cache-control": "no-store",
      },
    });
  }

  const payload: unknown = await upstream.json().catch(() => null);
  return NextResponse.json(payload ?? {});
}

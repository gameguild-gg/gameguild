import { getRequestAuthContext } from "@/auth";
import { createServerClient, GeneratedApi } from "@game-guild/client";
import { type NextRequest, NextResponse } from "next/server";

export const dynamic = "force-dynamic";

/** Dashboard page size for the campaign list; the API caps take at its own bound. */
const PAGE_TAKE = 50;

const CAMPAIGN_STATUSES = ["Draft", "Active", "Paused", "Completed", "Cancelled", "Archived"] as const;

const LIFECYCLE_ACTIONS = {
  activate: (campaigns: GeneratedApi.CommerceProductsPromoCampaignsModule, campaignId: string) =>
    campaigns.postPromoCampaignsActivate(campaignId),
  pause: (campaigns: GeneratedApi.CommerceProductsPromoCampaignsModule, campaignId: string) =>
    campaigns.postPromoCampaignsPause(campaignId),
  complete: (campaigns: GeneratedApi.CommerceProductsPromoCampaignsModule, campaignId: string) =>
    campaigns.postPromoCampaignsComplete(campaignId),
  cancel: (campaigns: GeneratedApi.CommerceProductsPromoCampaignsModule, campaignId: string) =>
    campaigns.postPromoCampaignsCancel(campaignId),
} as const;

function campaignModules(token: string): GeneratedApi.CommerceProductsPromoCampaignsModule {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";
  return new GeneratedApi.CommerceProductsPromoCampaignsModule(
    createServerClient({
      baseUrl: apiUrl,
      auth: { getAccessToken: () => Promise.resolve(token) },
    }),
  );
}

function upstreamError(error: { status?: number; message?: string; detail?: string }): NextResponse {
  const status = error.status && error.status >= 400 && error.status <= 599 ? error.status : 502;
  const message = error.detail || error.message || "The promo-campaign API rejected the request.";
  return NextResponse.json({ error: message }, { status, headers: { "cache-control": "no-store" } });
}

/**
 * Browser-facing promo-campaigns dashboard proxy (issue #418). The caller's session
 * token authorizes every upstream call server-side through the generated client
 * (`/v1/promo-campaigns` — the API serves no `/api/v1` prefix, issue #1217); the
 * promo-campaigns controller keeps enforcing its permission policy. GET without a
 * campaign id returns the paged campaign list; GET with a campaign id returns the
 * windowed performance report; POST issues one lifecycle transition
 * (activate/pause/complete/cancel).
 */
export async function GET(request: NextRequest): Promise<Response> {
  const { token } = (await getRequestAuthContext()) as { token: string | null };
  if (!token) return NextResponse.json({ error: "Authentication required." }, { status: 401 });

  const parameters = new URL(request.url).searchParams;
  const campaignId = parameters.get("campaignId");
  const campaigns = campaignModules(token);

  if (campaignId) {
    const from = parameters.get("from");
    const to = parameters.get("to");
    try {
      const result = await campaigns.getPromoCampaignsPerformance(campaignId, {
        ...(from ? { from } : {}),
        ...(to ? { to } : {}),
      });
      return result.ok ? NextResponse.json(result.data ?? {}) : upstreamError(result.error);
    } catch {
      // safeParse throws on contract drift between the deployed API and the
      // generated client; surface it as a gateway failure instead of crashing.
      return upstreamError({ status: 502, message: "The campaign performance report violated its API contract." });
    }
  }

  const status = parameters.get("status");
  const searchTerm = parameters.get("searchTerm");
  const listParameters: {
    skip: number;
    take: number;
    status?: (typeof CAMPAIGN_STATUSES)[number];
    searchTerm?: string;
  } = {
    skip: Number(parameters.get("skip") ?? "0") || 0,
    take: Number(parameters.get("take") ?? String(PAGE_TAKE)) || PAGE_TAKE,
  };
  if (status && (CAMPAIGN_STATUSES as readonly string[]).includes(status)) {
    listParameters.status = status as (typeof CAMPAIGN_STATUSES)[number];
  }
  if (searchTerm) listParameters.searchTerm = searchTerm;

  try {
    const result = await campaigns.getPromoCampaignsForGetPromoCampaigns(listParameters);
    return result.ok ? NextResponse.json(result.data ?? {}) : upstreamError(result.error);
  } catch {
    return upstreamError({ status: 502, message: "The campaign list violated its API contract." });
  }
}

/** Issues one campaign lifecycle transition; the API validates the state machine. */
export async function POST(request: NextRequest): Promise<Response> {
  const { token } = (await getRequestAuthContext()) as { token: string | null };
  if (!token) return NextResponse.json({ error: "Authentication required." }, { status: 401 });

  const body = (await request.json().catch(() => null)) as { campaignId?: string; action?: string } | null;
  const lifecycle = body?.action && Object.prototype.hasOwnProperty.call(LIFECYCLE_ACTIONS, body.action)
    ? LIFECYCLE_ACTIONS[body.action as keyof typeof LIFECYCLE_ACTIONS]
    : undefined;
  if (!body?.campaignId || !lifecycle) {
    return NextResponse.json({ error: "A campaignId and a supported action are required." }, { status: 400 });
  }

  try {
    const result = await lifecycle(campaignModules(token), body.campaignId);
    return result.ok ? NextResponse.json(result.data ?? {}) : upstreamError(result.error);
  } catch {
    return upstreamError({ status: 502, message: "The campaign lifecycle transition violated its API contract." });
  }
}

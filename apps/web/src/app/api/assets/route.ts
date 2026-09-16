import { getToken } from "@/auth";
import { type NextRequest, NextResponse } from "next/server";

export const dynamic = "force-dynamic";

function apiBaseUrl(): string {
  return (process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080").replace(/\/$/, "");
}

function upstreamResponse(upstream: Response): Promise<Response> {
  return upstream.arrayBuffer().then((body) => new Response(
    upstream.status === 204 || upstream.status === 304 ? null : body,
    {
    status: upstream.status,
    statusText: upstream.statusText,
    headers: {
      "content-type": upstream.headers.get("content-type") || "application/json",
      "cache-control": "no-store",
    },
  }));
}

export async function GET(request: NextRequest): Promise<Response> {
  const token = await getToken();
  if (!token) return NextResponse.json({ error: "Authentication required." }, { status: 401 });
  const resourceType = request.nextUrl.searchParams.get("resourceType")?.trim();
  const resourceId = request.nextUrl.searchParams.get("resourceId")?.trim();
  if (!resourceType || !resourceId) {
    return NextResponse.json({ error: "resourceType and resourceId are required." }, { status: 400 });
  }

  const upstreamUrl = new URL(`${apiBaseUrl()}/v1/assets`);
  upstreamUrl.searchParams.set("parentType", resourceType);
  upstreamUrl.searchParams.set("parentId", resourceId);
  const upstream = await fetch(upstreamUrl.toString(), {
    headers: { authorization: `Bearer ${token}` },
    cache: "no-store",
    signal: request.signal,
  });
  if (!upstream.ok) return upstreamResponse(upstream);
  const assets = await upstream.json() as Array<{ displayName?: string | null }>;
  const search = request.nextUrl.searchParams.get("search")?.trim().toLowerCase();
  const filtered = search
    ? assets.filter((asset) => asset.displayName?.toLowerCase().includes(search))
    : assets;
  const limit = Math.min(Math.max(Number(request.nextUrl.searchParams.get("limit")) || 200, 1), 500);
  return NextResponse.json({ items: filtered.slice(0, limit) }, { headers: { "cache-control": "no-store" } });
}

export async function POST(request: NextRequest): Promise<Response> {
  const token = await getToken();
  if (!token) return NextResponse.json({ error: "Authentication required." }, { status: 401 });
  const upstreamUrl = new URL(`${apiBaseUrl()}/v1/assets`);
  for (const name of [
    "displayName",
    "accessPolicy",
    "parentResourceType",
    "parentResourceId",
    "folderId",
    "referenceId",
  ]) {
    const value = request.nextUrl.searchParams.get(name);
    if (value) upstreamUrl.searchParams.set(name, value);
  }
  const upstream = await fetch(upstreamUrl, {
    method: "POST",
    headers: { authorization: `Bearer ${token}` },
    body: await request.formData(),
    cache: "no-store",
    signal: request.signal,
  });
  return upstreamResponse(upstream);
}

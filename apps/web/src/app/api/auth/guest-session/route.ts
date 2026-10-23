import { type NextRequest, NextResponse } from "next/server";

export const dynamic = "force-dynamic";

const GUEST_SESSION_PATH = "/v1/auth/guest-session";
const MAX_FINGERPRINT_LENGTH = 200;

function apiBaseUrl(): string {
  return (
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    "http://localhost:8080"
  ).replace(/\/$/, "");
}

/**
 * POST /api/auth/guest-session — same-origin proxy for the backend's anonymous
 * guest-session issuance (issue #213).
 *
 * Keeps the API host private and avoids CORS on the anonymous backend endpoint.
 * The caller presents no credentials; validation, per-IP rate limiting and the
 * security-event audit remain the backend's responsibility. A 403 from the
 * upstream means guest access is disabled by policy and is surfaced as such.
 */
export async function POST(request: NextRequest): Promise<NextResponse> {
  let body: unknown = null;
  try {
    body = await request.json();
  } catch {
    // An empty body is acceptable: every issuance field is optional.
  }

  const payload = (body ?? {}) as { deviceFingerprint?: unknown };
  const deviceFingerprint =
    typeof payload.deviceFingerprint === "string" &&
    payload.deviceFingerprint.trim().length > 0
      ? payload.deviceFingerprint.trim().slice(0, MAX_FINGERPRINT_LENGTH)
      : undefined;

  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl()}${GUEST_SESSION_PATH}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(deviceFingerprint ? { deviceFingerprint } : {}),
      cache: "no-store",
      signal: request.signal,
    });
  } catch {
    return NextResponse.json(
      { error: "guest_service_unavailable" },
      { status: 503 },
    );
  }

  if (response.status === 403) {
    // Fail closed on the backend's policy decision: guest access is disabled.
    return NextResponse.json(
      { error: "guest_access_disabled" },
      { status: 403, headers: { "Cache-Control": "no-store" } },
    );
  }

  if (!response.ok) {
    return NextResponse.json(
      { error: "guest_session_failed" },
      { status: response.status >= 500 ? 502 : response.status, headers: { "Cache-Control": "no-store" } },
    );
  }

  const data = (await response.json().catch(() => null)) as {
    guestSessionId?: unknown;
    accessToken?: unknown;
    expiresAt?: unknown;
    expiresInSeconds?: unknown;
    expiresIn?: unknown;
    permissions?: unknown;
    tokenType?: unknown;
  } | null;

  if (
    !data ||
    typeof data.guestSessionId !== "string" ||
    typeof data.accessToken !== "string"
  ) {
    return NextResponse.json(
      { error: "invalid_guest_session_response" },
      { status: 502, headers: { "Cache-Control": "no-store" } },
    );
  }

  return NextResponse.json(
    {
      guestSessionId: data.guestSessionId,
      accessToken: data.accessToken,
      expiresAt: data.expiresAt,
      permissions: Array.isArray(data.permissions)
        ? data.permissions.filter((permission): permission is string => typeof permission === "string")
        : [],
    },
    { headers: { "Cache-Control": "no-store" } },
  );
}

/**
 * GET /api/auth/guest-session — same-origin proxy for guest-session
 * introspection. The guest bearer rides the x-guest-token header from the
 * browser store and is forwarded as the upstream Authorization credential on
 * the backend's dedicated guest scheme; a user session token is never valid
 * here and a guest token is never sent to user endpoints.
 */
export async function GET(request: NextRequest): Promise<Response> {
  const guestToken = request.headers.get("x-guest-token");
  if (!guestToken || guestToken.trim().length === 0) {
    return NextResponse.json(
      { error: "guest_token_required" },
      { status: 401, headers: { "Cache-Control": "no-store" } },
    );
  }

  let upstream: Response;
  try {
    upstream = await fetch(`${apiBaseUrl()}${GUEST_SESSION_PATH}`, {
      headers: { authorization: `Bearer ${guestToken.trim()}` },
      cache: "no-store",
      signal: request.signal,
    });
  } catch {
    return NextResponse.json(
      { error: "guest_service_unavailable" },
      { status: 503, headers: { "Cache-Control": "no-store" } },
    );
  }

  const body = await upstream.text();
  return new Response(upstream.status === 204 || upstream.status === 304 ? null : body, {
    status: upstream.status,
    statusText: upstream.statusText,
    headers: {
      "content-type": upstream.headers.get("content-type") || "application/json",
      "cache-control": "no-store",
    },
  });
}

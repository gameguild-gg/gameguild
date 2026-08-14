import { authConfig, getToken } from '@/auth';
import {
  constantTimeEqual,
  LINK_STATE_COOKIE_NAME,
  settingsAccountPath,
  verifyLinkStateCookie,
} from '@/lib/auth/oauth-link-state';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { cookies } from 'next/headers';
import { NextRequest, NextResponse } from 'next/server';

/**
 * GET /api/auth/link/discord/callback — complete the Discord account link.
 *
 * Validates the signed link-state cookie (HMAC + exp + flow === 'link' +
 * constant-time state match against the query), deletes it (single-use), then
 * exchanges the authorization code server-to-server with the caller's bearer
 * and redirects back to the account settings page with `?linked=discord` or
 * an `?error=` code for the localized banner.
 */
export async function GET(request: NextRequest): Promise<NextResponse> {
  const url = request.nextUrl;
  const code = url.searchParams.get('code');
  const state = url.searchParams.get('state');

  const cookieStore = await cookies();
  const cookieValue = cookieStore.get(LINK_STATE_COOKIE_NAME)?.value;

  const payload = await verifyLinkStateCookie(cookieValue, authConfig.secret);

  if (!payload || payload.flow !== 'link' || !state || !code) {
    cookieStore.delete(LINK_STATE_COOKIE_NAME);
    return redirectToSettings(url, payload?.locale, '?error=state_mismatch');
  }
  if (!constantTimeEqual(payload.state, state)) {
    cookieStore.delete(LINK_STATE_COOKIE_NAME);
    return redirectToSettings(url, payload.locale, '?error=state_mismatch');
  }

  cookieStore.delete(LINK_STATE_COOKIE_NAME);

  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  const authModule = new GeneratedApi.AuthModule(
    createServerClient({
      baseUrl: apiUrl,
      auth: { getAccessToken: () => getToken() },
    }),
  );

  const result = await authModule.postAuthExternalLoginsDiscordLinkCallback({
    code,
    state,
    redirectUri: `${url.origin}/api/auth/link/discord/callback`,
  });

  if (result.ok) {
    return redirectToSettings(url, payload.locale, '?linked=discord');
  }
  // 409 — Discord identity already linked to another account.
  return redirectToSettings(
    url,
    payload.locale,
    result.error?.status === 409 ? '?error=conflict' : '?error=generic',
  );
}

function redirectToSettings(
  requestUrl: URL,
  locale: string | undefined,
  query: string,
): NextResponse {
  const target = new URL(settingsAccountPath(locale) + query, requestUrl.origin);
  return NextResponse.redirect(target, 302);
}

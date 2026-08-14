import { auth, authConfig, getToken } from '@/auth';
import { routing } from '@/i18n';
import {
  LINK_STATE_COOKIE_NAME,
  linkStateCookieOptions,
  signLinkStatePayload,
} from '@/lib/auth/oauth-link-state';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { NextRequest, NextResponse } from 'next/server';

/**
 * GET /api/auth/link/discord — start the Discord ACCOUNT-LINK flow.
 *
 * Distinct from /api/auth/signin/discord (client lib sign-in flow): this
 * requires an existing session, calls the authenticated
 * `external-logins/discord:link-authorize` endpoint with the caller's bearer,
 * and stashes a signed link-state cookie (`__gg-oauth-link-state-discord`,
 * flow:'link') validated by the callback route.
 */
export async function GET(request: NextRequest): Promise<NextResponse> {
  const session = await auth();
  if (!session || typeof session === 'function') {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const redirectUri = `${request.nextUrl.origin}/api/auth/link/discord/callback`;

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

  const result = await authModule.postAuthExternalLoginsDiscordLinkAuthorize({ redirectUri });
  if (!result.ok || !result.data.authUrl || !result.data.state) {
    // Pass the backend status through (503 when Discord OAuth is not
    // configured, 401 on token issues) — visible proof of correct wiring.
    return NextResponse.json(
      { error: result.ok ? 'invalid_authorize_response' : 'link_authorize_failed', message: result.ok ? undefined : result.error?.message },
      { status: !result.ok ? (result.error?.status ?? 500) : 502 },
    );
  }

  const localeParam = request.nextUrl.searchParams.get('locale') ?? '';
  const locale = routing.locales.includes(localeParam as (typeof routing.locales)[number])
    ? localeParam
    : undefined;

  const cookieValue = await signLinkStatePayload(
    {
      state: result.data.state,
      flow: 'link',
      exp: Date.now() + 600_000,
      locale,
    },
    authConfig.secret,
  );

  const response = NextResponse.redirect(result.data.authUrl);
  response.cookies.set(LINK_STATE_COOKIE_NAME, cookieValue, linkStateCookieOptions());
  return response;
}

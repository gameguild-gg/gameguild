import { authConfig } from '@/auth';
import { resolveAllowedAuthRedirect } from '@/lib/auth/cross-domain-auth';
import {
  SAML_STATE_COOKIE_NAME,
  SAML_STATE_COOKIE_MAX_AGE,
  samlStateCookieOptions,
  signSamlStatePayload,
} from '@/lib/auth/saml-signin-state';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { cookies } from 'next/headers';
import { NextRequest, NextResponse } from 'next/server';

/**
 * GET /api/auth/saml/[slug]/signin — start a SAML federation sign-in (issue #268).
 *
 * Asks the API to issue an AuthnRequest (`POST /v1/auth/saml/{slug}:sign-in-authorize`),
 * binds the returned RelayState token to a signed, single-use, HttpOnly state
 * cookie in the initiating browser (SameSite=None: the IdP returns the browser
 * through a top-level cross-site form POST), then redirects the browser to the
 * identity provider. The ACS receiver (`/api/auth/saml/[slug]/callback`) later
 * refuses any callback whose posted RelayState does not match this cookie.
 */
export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ slug: string }> },
): Promise<NextResponse> {
  const { slug } = await params;
  const url = request.nextUrl;
  const errorPage = authConfig.pages.error || '/sign-in';

  if (!isValidSlug(slug)) {
    return NextResponse.redirect(new URL(`${errorPage}?error=saml_invalid_provider`, url.origin), 302);
  }

  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  const authModule = new GeneratedApi.AuthModule(
    createServerClient({ baseUrl: apiUrl }),
  );

  const result = await authModule.postAuthSamlSignInAuthorize(slug, {});

  if (!result.ok || !result.data.redirectUrl || !result.data.relayState) {
    // Unknown/disabled/misconfigured provider, or the API could not issue the
    // request — fail closed, never send the browser to the provider.
    return NextResponse.redirect(new URL(`${errorPage}?error=saml_unavailable`, url.origin), 302);
  }

  const rememberMe = url.searchParams.get('rememberMe');
  const tenantId = url.searchParams.get('tenantId');
  const cookieStore = await cookies();
  cookieStore.set(
    SAML_STATE_COOKIE_NAME,
    await signSamlStatePayload(
      {
        state: result.data.relayState,
        slug,
        flow: 'saml',
        redirectTo: resolveAllowedAuthRedirect(url.searchParams.get('redirectTo')),
        // Only a real UUID survives: the callback forwards this value into the
        // API's UUID-typed DTO, and a malformed one must degrade to "no tenant
        // requested" here — not surface as a 500 at callback time.
        tenantId: tenantId && UUID_PATTERN.test(tenantId) ? tenantId : undefined,
        locale: url.searchParams.get('locale') ?? undefined,
        rememberMe:
          rememberMe === null ? undefined : rememberMe.toLowerCase() === 'true',
        exp: Date.now() + SAML_STATE_COOKIE_MAX_AGE * 1000,
      },
      authConfig.secret,
    ),
    samlStateCookieOptions(),
  );

  return NextResponse.redirect(result.data.redirectUrl, 302);
}

/**
 * Provider slugs are the configured `Saml:<slug>` keys and must satisfy the
 * shared federation-slug contract enforced by the API
 * (`ExternalProviderOptions.IsValidFederationSlug`): lowercase ASCII letters,
 * digits, and hyphens, starting and ending with a letter or digit, max 64.
 */
function isValidSlug(slug: string): boolean {
  return /^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$/.test(slug);
}

/** Tenant ids are GUIDs on the API side; anything else is dropped, never forwarded. */
const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

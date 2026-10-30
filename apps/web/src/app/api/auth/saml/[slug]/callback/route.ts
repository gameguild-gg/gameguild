import { authConfig } from '@/auth';
import { routing } from '@/i18n/routing';
import {
  constantTimeEqual,
  SAML_STATE_COOKIE_NAME,
  verifySamlStateCookie,
} from '@/lib/auth/saml-signin-state';
import {
  createJWTPayload,
  createServerClient,
  encodeSession,
  GeneratedApi,
  parseBackendAuthResponse,
  resolveCookieOptions,
  SessionStore,
} from '@game-guild/client';
import { cookies } from 'next/headers';
import { NextRequest, NextResponse } from 'next/server';

/**
 * POST /api/auth/saml/[slug]/callback — browser form receiver for the SAML
 * HTTP-POST binding (issue #268).
 *
 * This is the Assertion Consumer Service URL the web app registers with the
 * identity provider: the provider answers the AuthnRequest with an
 * auto-submitting HTML form (`application/x-www-form-urlencoded` carrying the
 * Base64 `SAMLResponse` and the `RelayState` issued at sign-in initiation).
 * The route binds that callback to the initiating browser before anything is
 * forwarded: the posted RelayState must constant-time-match the signed,
 * single-use state cookie set in this browser when the flow started, and the
 * cookie must name this provider slug. Foreign, missing, reused (the cookie is
 * deleted on first use), or cross-provider state all fail closed. Only then is
 * the response forwarded to the API's JSON ACS
 * (`POST /v1/auth/saml/{slug}:assertion-consumer`), which enforces the
 * cryptographic validation (signatures, issuer, audience, lifetimes, signed
 * subject correlation) and single-use consumption. On success the web session
 * cookie is established exactly like the social-provider callback.
 */
export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ slug: string }> },
): Promise<NextResponse> {
  const { slug } = await params;
  const url = request.nextUrl;
  const errorPage = authConfig.pages.error || '/sign-in';

  const form = await request.formData().catch(() => null);
  const samlResponse = form?.get('SAMLResponse');
  const postedRelayState = form?.get('RelayState');

  const cookieStore = await cookies();
  const cookieValue = cookieStore.get(SAML_STATE_COOKIE_NAME)?.value;
  const payload = await verifySamlStateCookie(cookieValue, authConfig.secret);

  if (
    !payload ||
    payload.flow !== 'saml' ||
    payload.slug !== slug ||
    typeof samlResponse !== 'string' ||
    samlResponse.length === 0 ||
    typeof postedRelayState !== 'string' ||
    !constantTimeEqual(payload.state, postedRelayState)
  ) {
    // Missing (never initiated here / already used), tampered, expired,
    // cross-provider, or foreign RelayState — one fail-closed branch, and the
    // cookie is always burnt so it can never be retried against this browser.
    cookieStore.delete(SAML_STATE_COOKIE_NAME);
    return NextResponse.redirect(
      new URL(`${errorPage}?error=state_mismatch`, url.origin),
      302,
    );
  }

  cookieStore.delete(SAML_STATE_COOKIE_NAME);

  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  const authModule = new GeneratedApi.AuthModule(
    createServerClient({ baseUrl: apiUrl }),
  );

  const result = await authModule.postAuthSamlAssertionConsumer(slug, {
    samlResponse,
    relayState: postedRelayState,
    rememberMe: payload.rememberMe,
    tenantId: payload.tenantId,
  });

  if (!result.ok || !result.data.accessToken) {
    // 401 — the API's fail-closed validation (signature, correlation, replay,
    // tenant isolation, MFA policy); anything else is a federation failure.
    const code = !result.ok && result.error.status === 401 ? 'saml_rejected' : 'saml_failed';
    return NextResponse.redirect(
      new URL(`${errorPage}?error=${code}`, url.origin),
      302,
    );
  }

  const providerResult = parseBackendAuthResponse(
    result.data as unknown as Record<string, unknown>,
  );
  providerResult.rememberMe = payload.rememberMe;

  const signInAllowed = await authConfig.callbacks.signIn({
    user: providerResult.user,
    provider: `saml:${slug}`,
  });
  if (signInAllowed === false) {
    return NextResponse.redirect(
      new URL(`${errorPage}?error=access_denied`, url.origin),
      302,
    );
  }

  // Establish the session exactly like the library's OAuth callback
  // (createJWTPayload → jwt callback → encodeSession → session cookie).
  let token = createJWTPayload(providerResult, authConfig);
  token = await authConfig.callbacks.jwt({
    token,
    user: providerResult.user,
    trigger: 'signIn',
  });
  const encrypted = await encodeSession(token, authConfig);
  const cookieOptions = resolveCookieOptions(
    authConfig.cookies,
    authConfig.cookies.secure,
  );
  new SessionStore(cookieOptions).write(
    encrypted,
    (name, value, options) => cookieStore.set(name, value, options),
    { storage: payload.rememberMe === false ? 'session' : 'persistent' },
  );

  let callbackUrl = payload.redirectTo || '/';
  if (
    payload.locale &&
    routing.locales.includes(payload.locale as (typeof routing.locales)[number]) &&
    !callbackUrl.startsWith(`/${payload.locale}`)
  ) {
    callbackUrl = `/${payload.locale}${callbackUrl}`;
  }
  if (!callbackUrl.startsWith('/') || callbackUrl.startsWith('//')) {
    callbackUrl = '/';
  }

  return NextResponse.redirect(new URL(callbackUrl, url.origin), 302);
}

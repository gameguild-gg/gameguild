/**
 * Signed SAML Sign-In State Cookie (issue #268 — browser callback-state binding)
 *
 * The browser leg of the SAML callback-state binding. When the browser starts
 * a SAML sign-in (`GET /api/auth/saml/[slug]/signin`), the web app binds the
 * API-issued RelayState token to this HttpOnly cookie; the provider echoes the
 * same RelayState back inside the auto-submitting HTTP-POST form, and the ACS
 * receiver route refuses any callback whose RelayState does not
 * constant-time-match the signed cookie (foreign/missing/reused state all
 * fail closed). The provider leg of the binding lives in the API
 * (the pending AuthnRequest is stored server-side fingerprinted with the same
 * RelayState and consumed atomically — see
 * PostgreSqlSamlProtocolStateStore).
 *
 * Mirrors the client lib's sign-in state cookie spec
 * (packages/infrastructure/client/src/integrations/next/oauth-state.ts —
 * internal, not exported) and the Discord link flow
 * (lib/auth/oauth-link-state.ts) with a DISTINCT cookie name and flow:'saml',
 * so a sign-in/link state cookie can never be replayed against the SAML ACS
 * (and vice versa). The provider slug is inside the signed payload, so a
 * cookie minted for one provider can never complete another provider's
 * callback.
 *
 * Cookie spec:
 *   - name: `__gg-saml-signin-state`
 *   - value: base64url(JSON payload) + '.' + hex HMAC-SHA256(payload, secret)
 *   - attributes: HttpOnly, SameSite=None + Secure (unlike the OAuth flows,
 *     the SAML HTTP-POST binding returns the browser through a top-level
 *     cross-site form POST, which never carries SameSite=Lax cookies), Path=/,
 *     MaxAge=600
 *
 * The secret is the web app's resolved AUTH_SECRET (same raw string the
 * session JWT uses); HMAC uses Web Crypto like the lib.
 */

export const SAML_STATE_COOKIE_MAX_AGE = 600;

/** Cookie attributes for the SAML state cookie (Next.js `cookies().set` shape). */
export interface SamlStateCookieAttributes {
  httpOnly: true;
  sameSite: 'none';
  secure: true;
  path: string;
  maxAge: number;
}

/** Cookie name for the SAML sign-in state cookie. */
export const SAML_STATE_COOKIE_NAME = '__gg-saml-signin-state';

export interface SamlSignInStatePayload {
  /** RelayState value expected back inside the provider's posted form. */
  state: string;
  /** Provider slug the sign-in was issued for; callbacks for any other slug fail. */
  slug: string;
  flow: 'saml';
  /** Relative-only post-sign-in redirect target (validated at issuance). */
  redirectTo: string;
  tenantId?: string;
  locale?: string;
  /** Whether the user asked for a persistent ("remember me") session. */
  rememberMe?: boolean;
  /** Expiry in epoch milliseconds. */
  exp: number;
}

/**
 * Cookie attributes for set (maxAge=600) and delete (maxAge=0).
 * SameSite=None requires Secure, or browsers drop the cookie outright.
 */
export function samlStateCookieOptions(maxAge: number = SAML_STATE_COOKIE_MAX_AGE): SamlStateCookieAttributes {
  return {
    httpOnly: true,
    sameSite: 'none',
    secure: true,
    path: '/',
    maxAge,
  };
}

/**
 * Sign a SAML state payload: base64url(payload) + '.' + hex HMAC-SHA256.
 */
export async function signSamlStatePayload(
  payload: SamlSignInStatePayload,
  secret: string,
): Promise<string> {
  const encoded = Buffer.from(JSON.stringify(payload), 'utf8').toString('base64url');
  return `${encoded}.${await hmacHex(encoded, secret)}`;
}

/**
 * Verify the cookie signature and expiry. Returns the payload, or null on any
 * failure (missing, bad format, HMAC mismatch, undecodable JSON, expired).
 * Flow, slug, and RelayState matching are the caller's policy.
 */
export async function verifySamlStateCookie(
  value: string | undefined,
  secret: string,
): Promise<SamlSignInStatePayload | null> {
  if (!value) return null;

  const dot = value.lastIndexOf('.');
  if (dot <= 0 || dot === value.length - 1) return null;

  const encoded = value.slice(0, dot);
  const mac = value.slice(dot + 1);

  if (!constantTimeEqual(mac, await hmacHex(encoded, secret))) return null;

  try {
    const payload = JSON.parse(
      Buffer.from(encoded, 'base64url').toString('utf8'),
    ) as SamlSignInStatePayload;
    if (typeof payload.exp !== 'number' || payload.exp <= Date.now()) return null;
    return payload;
  } catch {
    return null;
  }
}

/**
 * Constant-time string comparison. Branch-free over content; the early
 * length return leaks only lengths (hex digests have fixed length anyway).
 */
export function constantTimeEqual(a: string, b: string): boolean {
  if (a.length !== b.length) return false;
  let diff = 0;
  for (let i = 0; i < a.length; i++) {
    diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  }
  return diff === 0;
}

async function hmacHex(payload: string, secret: string): Promise<string> {
  const encoder = new TextEncoder();
  const key = await crypto.subtle.importKey(
    'raw',
    encoder.encode(secret),
    { name: 'HMAC', hash: 'SHA-256' },
    false,
    ['sign'],
  );
  const signature = await crypto.subtle.sign('HMAC', key, encoder.encode(payload));
  return Buffer.from(signature).toString('hex');
}

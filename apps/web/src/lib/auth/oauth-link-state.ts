/**
 * Signed OAuth Link-State Cookie (Discord account linking)
 *
 * Mirrors the client lib's sign-in state cookie spec
 * (packages/infrastructure/client/src/integrations/next/oauth-state.ts —
 * internal, not exported) with a DISTINCT cookie name and flow:'link' so a
 * sign-in state cookie can never be replayed against the link callback (and
 * vice versa).
 *
 * Cookie spec:
 *   - name: `__gg-oauth-link-state-discord`
 *   - value: base64url(JSON payload) + '.' + hex HMAC-SHA256(payload, secret)
 *   - attributes: HttpOnly, SameSite=Lax (never Strict — must survive
 *     Discord's top-level cross-site redirect), Path=/, MaxAge=600
 *
 * The secret is the web app's resolved AUTH_SECRET (same raw string the
 * session JWT uses); HMAC uses Web Crypto like the lib.
 */

export const LINK_STATE_COOKIE_MAX_AGE = 600;

/** Cookie name for the Discord link-flow state cookie. */
export const LINK_STATE_COOKIE_NAME = '__gg-oauth-link-state-discord';

export interface OAuthLinkStatePayload {
  /** OAuth state value expected back on the callback query string. */
  state: string;
  flow: 'link';
  /** Expiry in epoch milliseconds. */
  exp: number;
  /** Locale to prefix the post-callback settings redirect (en-US = none). */
  locale?: string;
}

export function linkStateCookieOptions(maxAge: number = LINK_STATE_COOKIE_MAX_AGE) {
  return {
    httpOnly: true,
    sameSite: 'lax' as const,
    path: '/',
    maxAge,
  };
}

/**
 * Sign a link-state payload: base64url(payload) + '.' + hex HMAC-SHA256.
 */
export async function signLinkStatePayload(
  payload: OAuthLinkStatePayload,
  secret: string,
): Promise<string> {
  const encoded = Buffer.from(JSON.stringify(payload), 'utf8').toString('base64url');
  return `${encoded}.${await hmacHex(encoded, secret)}`;
}

/**
 * Verify the cookie signature and expiry. Returns the payload, or null on any
 * failure (bad format, HMAC mismatch, undecodable JSON, expired). Flow and
 * state matching are the caller's policy.
 */
export async function verifyLinkStateCookie(
  value: string | undefined,
  secret: string,
): Promise<OAuthLinkStatePayload | null> {
  if (!value) return null;

  const dot = value.lastIndexOf('.');
  if (dot <= 0 || dot === value.length - 1) return null;

  const encoded = value.slice(0, dot);
  const mac = value.slice(dot + 1);

  if (!constantTimeEqual(mac, await hmacHex(encoded, secret))) return null;

  try {
    const payload = JSON.parse(
      Buffer.from(encoded, 'base64url').toString('utf8'),
    ) as OAuthLinkStatePayload;
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

/**
 * Locale-aware path for the account settings page. All locales use explicit
 * prefixes so the Next 16 proxy never loops while resolving the default.
 */
export function settingsAccountPath(locale: string | undefined): string {
  const resolvedLocale = locale === 'pt-BR' ? locale : 'en-US';
  return `/${resolvedLocale}/workspace/settings/account`;
}

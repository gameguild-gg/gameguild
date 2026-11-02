import { routing } from './routing';

/**
 * Automatic locale detection (issue #92) with an explicit-choice override
 * (issue #99).
 *
 * Requests without an explicit locale prefix resolve their locale in this
 * order:
 *
 *   1. the `NEXT_LOCALE` cookie — a deliberate user choice written by the
 *      settings language selector and by visiting an explicit default-locale
 *      URL; it overrides auto-detection so a switch sticks;
 *   2. the `Accept-Language` header, negotiated against `routing.locales`
 *      (exact tag first, then primary-language subtag);
 *   3. `routing.defaultLocale`.
 *
 * The cookie name matches next-intl's `localeCookie` default, so cookies
 * written by next-intl's own middleware are honored here and vice versa.
 */
export const LOCALE_COOKIE_NAME = 'NEXT_LOCALE';
export const LOCALE_COOKIE_MAX_AGE_SECONDS = 60 * 60 * 24 * 365;

function normalizeLocaleTag(tag: string): string {
  return tag.trim().toLowerCase();
}

function findSupportedLocale(tag: string): string | undefined {
  const normalized = normalizeLocaleTag(tag);
  return routing.locales.find((locale) => normalizeLocaleTag(locale) === normalized);
}

/**
 * Reads the persisted locale preference from a raw `cookie` header value.
 * Returns the matching configured locale (case-normalized) or `undefined`
 * when the cookie is absent or holds an unsupported locale.
 */
export function getPreferredLocaleFromCookieHeader(
  cookieHeader: string | null | undefined,
): string | undefined {
  if (!cookieHeader) return undefined;
  for (const part of cookieHeader.split(';')) {
    const separator = part.indexOf('=');
    if (separator === -1) continue;
    const name = part.slice(0, separator).trim();
    if (name !== LOCALE_COOKIE_NAME) continue;
    return findSupportedLocale(part.slice(separator + 1));
  }
  return undefined;
}

export interface AcceptLanguageEntry {
  tag: string;
  quality: number;
}

/**
 * Parses an `Accept-Language` header per RFC 9110: comma-separated ranges,
 * each optionally followed by `;q=<weight>` (default 1, clamped to [0, 1]).
 * Ranges with `q=0` are excluded, and entries keep header order between
 * equal qualities so the browser's stated preference order wins.
 */
export function parseAcceptLanguageHeader(
  header: string | null | undefined,
): AcceptLanguageEntry[] {
  if (!header) return [];
  const entries: AcceptLanguageEntry[] = [];
  for (const raw of header.split(',')) {
    const segment = raw.trim();
    if (!segment) continue;
    const [tagPart, ...params] = segment.split(';');
    const tag = tagPart?.trim();
    if (!tag) continue;
    let quality = 1;
    for (const param of params) {
      const trimmed = param.trim();
      if (!trimmed.toLowerCase().startsWith('q=')) continue;
      const parsed = Number.parseFloat(trimmed.slice(2));
      if (Number.isFinite(parsed)) quality = Math.min(Math.max(parsed, 0), 1);
    }
    if (quality <= 0) continue;
    entries.push({ tag, quality });
  }
  return entries.sort((a, b) => b.quality - a.quality);
}

function primaryLanguageSubtag(tag: string): string {
  return normalizeLocaleTag(tag).split('-')[0] ?? '';
}

/**
 * Negotiates the best supported locale for an `Accept-Language` header.
 * A case-insensitive exact tag wins, then the first entry whose primary
 * language subtag matches a supported locale (e.g. `pt-PT` or bare `pt`
 * resolve to `pt-BR`); unsupported languages fall through to the default
 * locale (English).
 */
export function resolveLocaleFromAcceptLanguageHeader(
  header: string | null | undefined,
): string {
  for (const { tag } of parseAcceptLanguageHeader(header)) {
    if (tag === '*') continue;
    const exact = findSupportedLocale(tag);
    if (exact) return exact;
    const byLanguage = routing.locales.find(
      (locale) => primaryLanguageSubtag(locale) === primaryLanguageSubtag(tag),
    );
    if (byLanguage) return byLanguage;
  }
  return routing.defaultLocale;
}

/**
 * Full locale resolution for an unprefixed request: the stored preference
 * (explicit choice) wins over browser detection, which in turn wins over
 * the default locale.
 */
export function resolveLocaleForRequest(
  cookieHeader: string | null | undefined,
  acceptLanguageHeader: string | null | undefined,
): string {
  return (
    getPreferredLocaleFromCookieHeader(cookieHeader) ??
    resolveLocaleFromAcceptLanguageHeader(acceptLanguageHeader)
  );
}

/**
 * Client-side persistence for the language selector (issue #99): writes the
 * explicit choice so subsequent requests — including the navigation that
 * follows — carry it and automatic detection cannot bounce the visitor back
 * to their browser locale.
 */
export function setPreferredLocaleCookie(locale: string): void {
  if (typeof document === 'undefined') return;
  const supported = findSupportedLocale(locale);
  if (!supported) return;
  document.cookie = [
    `${LOCALE_COOKIE_NAME}=${supported}`,
    'path=/',
    `max-age=${LOCALE_COOKIE_MAX_AGE_SECONDS}`,
    'samesite=lax',
  ].join('; ');
}

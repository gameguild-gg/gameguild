import { auth } from "@/auth";
import {
  LOCALE_COOKIE_MAX_AGE_SECONDS,
  LOCALE_COOKIE_NAME,
  resolveLocaleForRequest,
} from "@/i18n/locale-detection";
import { routing } from "@/i18n/routing";
import createMiddleware from "next-intl/middleware";
import { NextResponse, type NextRequest } from "next/server";

const intlMiddleware = createMiddleware(routing);
const INTERNAL_LOCALE_HEADER = "x-gameguild-internal-locale-rewrite";
// Browser-visible path+query forwarded to server components, which cannot
// otherwise recover it (layouts get no search params; issue #864).
const PUBLIC_PATH_HEADER = "x-gameguild-path";
const DEFAULT_LOCALE_PREFIX = `/${routing.defaultLocale}`;
const NON_DEFAULT_LOCALE_PREFIXES = routing.locales
  .filter((locale) => locale !== routing.defaultLocale)
  .map((locale) => `/${locale}`);
// Canonical web origin (issue #103): the legacy `web.` host alias must never
// serve duplicate content — it is permanently redirected to the canonical
// domain so crawlers consolidate on a single host.
const CANONICAL_ORIGIN = (process.env.NEXT_PUBLIC_APP_URL || "https://gameguild.gg").replace(/\/+$/, "");
const CANONICAL_HOST = new URL(CANONICAL_ORIGIN).host;
const LEGACY_HOST_ALIASES = new Set(["web.gameguild.gg"]);

function matchesPrefix(pathname: string, prefix: string): boolean {
  return pathname === prefix || pathname.startsWith(`${prefix}/`);
}

function publicPath(pathname: string, search: string): string {
  const localePrefix = routing.locales
    .map((locale) => `/${locale}`)
    .find((prefix) => matchesPrefix(pathname, prefix));
  const unprefixed = localePrefix ? pathname.slice(localePrefix.length) || "/" : pathname;
  return search ? `${unprefixed}${search}` : unprefixed;
}

function rewriteWithLocale(request: NextRequest, pathname: string, locale: string): NextResponse {
  // NextURL normalizes numeric loopback hosts to localhost. Keep the actual
  // request origin so redirects and rewrites retain host-only session cookies.
  const url = new URL(request.url);
  url.pathname = pathname;

  const requestHeaders = new Headers(request.headers);
  requestHeaders.set(INTERNAL_LOCALE_HEADER, "1");
  requestHeaders.set("x-next-intl-locale", locale);
  requestHeaders.set(
    PUBLIC_PATH_HEADER,
    publicPath(request.nextUrl.pathname, request.nextUrl.search),
  );

  return NextResponse.rewrite(url, { request: { headers: requestHeaders } });
}

function redirectToPath(request: NextRequest, pathname: string): NextResponse {
  const url = new URL(request.url);
  url.pathname = pathname;
  return NextResponse.redirect(url);
}

/**
 * Canonicalizes an explicit default-locale URL and persists the choice when
 * it conflicts with what automatic detection would otherwise serve: an
 * `/en-US/...` visit from a pt-BR browser (or with a pt-BR preference
 * stored) is a deliberate English request, so the preference cookie must be
 * written before the redirect is followed. Otherwise detection (issue #92)
 * would bounce that visitor right back to `/pt-BR` on the next request and
 * the switch could never stick (issue #99). Without a conflict the response
 * stays cookie-free, preserving the session-cookie contract of plain
 * canonicalization redirects.
 */
function redirectToUnprefixedPath(request: NextRequest, unprefixedPath: string): NextResponse {
  const response = redirectToPath(request, unprefixedPath);
  const wouldAutoDetectNonDefault =
    resolveLocaleForRequest(
      request.headers.get("cookie"),
      request.headers.get("accept-language"),
    ) !== routing.defaultLocale;
  if (wouldAutoDetectNonDefault) {
    response.cookies.set(LOCALE_COOKIE_NAME, routing.defaultLocale, {
      path: "/",
      maxAge: LOCALE_COOKIE_MAX_AGE_SECONDS,
      sameSite: "lax",
    });
  }
  return response;
}

/**
 * Permanent host canonicalization (issue #103): `web.gameguild.gg` historically
 * served the same pages as `gameguild.gg`, duplicating content and splitting
 * SEO signals across hosts. Requests arriving on a legacy alias are redirected
 * to the canonical host with path and query preserved. Only the actual request
 * host is trusted — client-provided forwarding headers must not influence
 * routing (same posture as the origin-trust behavior below).
 */
function redirectToCanonicalHost(request: NextRequest): NextResponse | null {
  const host = (request.headers.get("host") ?? request.nextUrl.host).split(",")[0]?.trim().toLowerCase();
  if (!host || !LEGACY_HOST_ALIASES.has(host)) return null;

  const url = new URL(request.url);
  url.protocol = CANONICAL_ORIGIN.startsWith("http://") ? "http:" : "https:";
  url.host = CANONICAL_HOST;
  return NextResponse.redirect(url, 308);
}

/**
 * Keeps the default locale internal while preserving explicit non-default
 * locales. The marker header prevents Next 16 from canonicalizing the
 * internal `/en-US` rewrite back into a redirect loop.
 */
export function routeRequest(request: NextRequest): NextResponse {
  const canonicalRedirect = redirectToCanonicalHost(request);
  if (canonicalRedirect) return canonicalRedirect;

  const pathname = request.nextUrl.pathname;

  // Server Action redirects inherit the rewritten request's headers. Only
  // bypass routing when the destination itself already includes a locale;
  // an unprefixed destination still needs the internal locale rewrite.
  if (
    request.headers.get(INTERNAL_LOCALE_HEADER) === "1" &&
    routing.locales.some((locale) => matchesPrefix(pathname, `/${locale}`))
  ) {
    return NextResponse.next();
  }

  if (matchesPrefix(pathname, DEFAULT_LOCALE_PREFIX)) {
    const unprefixedPath = pathname.slice(DEFAULT_LOCALE_PREFIX.length) || "/";
    if (unprefixedPath === "/social") {
      return redirectToUnprefixedPath(request, "/feed");
    }
    // RSC navigation targets the internal locale tree. A canonical browser
    // redirect makes Next request that internal tree again and creates a loop.
    if (request.headers.get("rsc") === "1") {
      return rewriteWithLocale(request, pathname, routing.defaultLocale);
    }
    return redirectToUnprefixedPath(request, unprefixedPath);
  }

  const nonDefaultPrefix = NON_DEFAULT_LOCALE_PREFIXES.find((prefix) => matchesPrefix(pathname, prefix));
  if (nonDefaultPrefix) {
    if (pathname === `${nonDefaultPrefix}/social`) {
      return redirectToPath(request, `${nonDefaultPrefix}/feed`);
    }
    const response = intlMiddleware(request);
    response.headers.set(
      `x-middleware-request-${PUBLIC_PATH_HEADER}`,
      publicPath(request.nextUrl.pathname, request.nextUrl.search),
    );
    return response;
  }

  if (pathname === "/social") {
    return redirectToPath(request, "/feed");
  }

  // Automatic locale detection (issue #92): a request without an explicit
  // locale prefix resolves the visitor's preferred locale — a stored choice
  // first (issue #99 override), then the browser's Accept-Language, then the
  // default. A detected non-default locale is served through its canonical
  // prefixed URL so the browser, history entries and next-intl link
  // generation all agree on the locale; fetch-based RSC navigations follow
  // the redirect transparently. The default locale keeps the internal
  // rewrite so default-locale URLs stay unprefixed.
  const detectedLocale = resolveLocaleForRequest(
    request.headers.get("cookie"),
    request.headers.get("accept-language"),
  );
  if (detectedLocale !== routing.defaultLocale) {
    const suffix = pathname === "/" ? "" : pathname;
    return redirectToPath(request, `/${detectedLocale}${suffix}`);
  }

  const localizedPath = pathname === "/" ? DEFAULT_LOCALE_PREFIX : `${DEFAULT_LOCALE_PREFIX}${pathname}`;
  return rewriteWithLocale(request, localizedPath, routing.defaultLocale);
}

export default auth((request) => {
  const nextRequest = request as unknown as NextRequest;
  return routeRequest(nextRequest);
});

export const config = {
  matcher: "/((?!api|trpc|_next|_vercel|.*\\..*).*)",
};

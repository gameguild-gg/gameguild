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
 * Canonicalizes an explicit default-locale URL and persists the choice: an
 * `/en-US/...` visit is a deliberate English request, so the preference
 * cookie must be written before the redirect is followed. Otherwise
 * automatic detection (issue #92) would bounce a pt-BR browser right back
 * to `/pt-BR` on the next request and the switch could never stick
 * (issue #99).
 */
function redirectToUnprefixedPath(request: NextRequest, unprefixedPath: string): NextResponse {
  const response = redirectToPath(request, unprefixedPath);
  response.cookies.set(LOCALE_COOKIE_NAME, routing.defaultLocale, {
    path: "/",
    maxAge: LOCALE_COOKIE_MAX_AGE_SECONDS,
    sameSite: "lax",
  });
  return response;
}

/**
 * Keeps the default locale internal while preserving explicit non-default
 * locales. The marker header prevents Next 16 from canonicalizing the
 * internal `/en-US` rewrite back into a redirect loop.
 */
export function routeRequest(request: NextRequest): NextResponse {
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

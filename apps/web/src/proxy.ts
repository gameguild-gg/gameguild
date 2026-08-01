import { NextRequest, NextResponse } from 'next/server';
import { routing } from '@/i18n';
import { getTrackProgramHref } from '@/lib/tracks/catalog';
import { elapsedMs, getRequestId, logWebRequest } from '@/lib/server/request-logging';
import {
  getRequestHostname,
  resolveLearningHostRoute,
} from '@/lib/routing/learning-host-routing';

function isPublicAssetPath(pathname: string): boolean {
  return (
    pathname === '/favicon.ico' ||
    pathname === '/favicon.svg' ||
    pathname === '/manifest.webmanifest' ||
    pathname === '/robots.txt' ||
    pathname === '/sitemap.xml' ||
    /\.[^/]+$/.test(pathname)
  );
}

function hasLocalePrefix(pathname: string): boolean {
  const firstSegment = pathname.split('/').filter(Boolean)[0];
  return routing.locales.includes(firstSegment as (typeof routing.locales)[number]);
}

function rewriteDefaultLocalePath(request: NextRequest): NextResponse | null {
  const { pathname } = request.nextUrl;

  if (isPublicAssetPath(pathname)) {
    return null;
  }

  if (hasLocalePrefix(pathname)) {
    return null;
  }

  const rewriteUrl = request.nextUrl.clone();
  rewriteUrl.pathname = `/${routing.defaultLocale}${pathname === '/' ? '' : pathname}`;
  return NextResponse.rewrite(rewriteUrl);
}

function getTrackRedirectPath(pathname: string): string | null {
  const segments = pathname.split('/').filter(Boolean);
  const locale = routing.locales.includes(segments[0] as (typeof routing.locales)[number]) ? segments.shift() : null;

  if (segments[0] !== 'tracks') {
    return null;
  }

  const target = segments[1] ? getTrackProgramHref(segments[1]) : '/programs';
  return locale ? `/${locale}${target}` : target;
}

export async function proxy(request: NextRequest): Promise<NextResponse> {
  const startedAt = performance.now();
  const requestId = getRequestId(request.headers);
  let response: NextResponse;
  let proxyAction = 'next';
  const hostDecision =
    process.env.UNIFIED_LEARNING_ENABLED === 'true'
      ? resolveLearningHostRoute({
          config: {
            defaultLocale: routing.defaultLocale,
            locales: routing.locales,
            learningOrigin:
              process.env.LEARNING_PUBLIC_URL ||
              process.env.NEXT_PUBLIC_LEARNING_APP_URL ||
              'https://learning.gameguild.gg',
            webOrigin:
              process.env.WEB_PUBLIC_URL ||
              process.env.NEXT_PUBLIC_APP_URL ||
              'https://gameguild.gg',
          },
          hostname: getRequestHostname(request.headers),
          url: request.nextUrl,
        })
      : { action: 'next' as const };
  const trackRedirectPath = getTrackRedirectPath(request.nextUrl.pathname);

  if (hostDecision.action === 'redirect') {
    response = NextResponse.redirect(hostDecision.url, hostDecision.status);
    proxyAction = 'redirect';
  } else if (hostDecision.action === 'rewrite') {
    const requestHeaders = new Headers(request.headers);
    requestHeaders.set('x-gameguild-visible-url', request.nextUrl.toString());
    response = NextResponse.rewrite(hostDecision.url, {
      request: { headers: requestHeaders },
    });
    proxyAction = 'rewrite';
  } else if (trackRedirectPath) {
    const redirectUrl = request.nextUrl.clone();
    redirectUrl.pathname = trackRedirectPath;
    redirectUrl.search = '';
    response = NextResponse.redirect(redirectUrl, 308);
    proxyAction = 'redirect';
  } else {
    const defaultLocaleRewrite = rewriteDefaultLocalePath(request);
    if (defaultLocaleRewrite) {
      response = defaultLocaleRewrite;
      proxyAction = 'rewrite';
    } else {
      response = NextResponse.next();
    }
  }

  response.headers.set('x-request-id', requestId);
  logWebRequest({
    event: 'web.proxy.complete',
    method: request.method,
    path: request.nextUrl.pathname,
    status: response.status,
    durationMs: elapsedMs(startedAt),
    requestId,
    ...(proxyAction !== 'next' ? { action: proxyAction, level: 'info' } : {}),
  });

  return response;
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico|favicon.svg|manifest.webmanifest|sitemap.xml|robots.txt).*)'],
};

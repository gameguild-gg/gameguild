import { NextRequest, NextResponse } from 'next/server';
import { routing } from '@/i18n';
import { getTrackProgramHref } from '@/lib/tracks/catalog';

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
  const trackRedirectPath = getTrackRedirectPath(request.nextUrl.pathname);

  if (trackRedirectPath) {
    const redirectUrl = request.nextUrl.clone();
    redirectUrl.pathname = trackRedirectPath;
    redirectUrl.search = '';
    return NextResponse.redirect(redirectUrl, 308);
  }

  const defaultLocaleRewrite = rewriteDefaultLocalePath(request);
  if (defaultLocaleRewrite) {
    return defaultLocaleRewrite;
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - api (API routes)
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon/manifest/sitemap/robots/static public files
     */
    {
      source: '/((?!api|_next/static|_next/image|favicon.ico|favicon.svg|manifest.webmanifest|sitemap.xml|robots.txt).*)',
      missing: [
        { type: 'header', key: 'next-router-prefetch' },
        { type: 'header', key: 'purpose', value: 'prefetch' },
      ],
    },

    {
      source: '/((?!api|_next/static|_next/image|favicon.ico|favicon.svg|manifest.webmanifest|sitemap.xml|robots.txt).*)',
      has: [
        { type: 'header', key: 'next-router-prefetch' },
        { type: 'header', key: 'purpose', value: 'prefetch' },
      ],
    },

    {
      source: '/((?!api|_next/static|_next/image|favicon.ico|favicon.svg|manifest.webmanifest|sitemap.xml|robots.txt).*)',
      has: [{ type: 'header', key: 'x-present' }],
      missing: [{ type: 'header', key: 'x-missing', value: 'prefetch' }],
    },
  ],
};

import { NextRequest, NextResponse } from 'next/server';
import createMiddleware from 'next-intl/middleware';
import { routing } from '@/i18n';
import { getTrackProgramHref } from '@/lib/tracks/catalog';

// next-intl middleware handles locale detection and redirects
const intlMiddleware = createMiddleware(routing);

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

  return intlMiddleware(request);
}

export const config = {
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - api (API routes)
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico, sitemap.xml, robots.txt (metadata files)
     */
    {
      source: '/((?!api|_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)',
      missing: [
        { type: 'header', key: 'next-router-prefetch' },
        { type: 'header', key: 'purpose', value: 'prefetch' },
      ],
    },

    {
      source: '/((?!api|_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)',
      has: [
        { type: 'header', key: 'next-router-prefetch' },
        { type: 'header', key: 'purpose', value: 'prefetch' },
      ],
    },

    {
      source: '/((?!api|_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)',
      has: [{ type: 'header', key: 'x-present' }],
      missing: [{ type: 'header', key: 'x-missing', value: 'prefetch' }],
    },
  ],
};

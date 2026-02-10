import { NextRequest, NextResponse } from 'next/server';
import { auth } from '@/auth';

export async function proxy(request: NextRequest): Promise<NextResponse> {
  // Public paths that don't require authentication
  const publicPaths = ['/sign-in', '/sign-up', '/forgot-password'];
  const isPublicPath = publicPaths.some((path) =>
    request.nextUrl.pathname.endsWith(path)
  );

  if (isPublicPath) {
    return NextResponse.next();
  }

  // Check session — auth() reads encrypted JWT from cookies
  const session = await auth();

  if (!session) {
    // Redirect unauthenticated users to sign-in
    const signInUrl = new URL('/en/sign-in', request.url);
    signInUrl.searchParams.set('callbackUrl', request.nextUrl.pathname);
    return NextResponse.redirect(signInUrl);
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

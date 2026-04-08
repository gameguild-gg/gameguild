import { NextRequest, NextResponse } from 'next/server';
import createMiddleware from 'next-intl/middleware';
import { processSession, encodeSession, SessionStore, resolveCookieOptions } from '@game-guild/client';
import { authConfig } from '@/auth';
import { routing } from '@/i18n';

// next-intl middleware handles locale detection and redirects
const intlMiddleware = createMiddleware(routing);

// Pre-compute cookie options once at module load
const cookieOptions = resolveCookieOptions(authConfig.cookies, authConfig.cookies.secure);

export async function proxy(request: NextRequest): Promise<NextResponse> {
  // Public paths that don't require authentication
  const publicPaths = ['/sign-in', '/sign-up', '/forgot-password', '/verify'];
  const isPublicPath = publicPaths.some((path) => request.nextUrl.pathname.endsWith(path));

  if (isPublicPath) {
    return intlMiddleware(request);
  }

  // Read session cookie directly
  const sessionStore = new SessionStore(cookieOptions);
  const encrypted = sessionStore.read((name) => request.cookies.get(name)?.value);

  if (!encrypted) {
    const signInUrl = new URL('/sign-in', request.url);
    signInUrl.searchParams.set('callbackUrl', request.nextUrl.pathname);
    return NextResponse.redirect(signInUrl);
  }

  // Process session — includes automatic token refresh when near-expiry
  const { session, token, updated } = await processSession(encrypted, authConfig);

  if (!session) {
    // Session invalid or expired and refresh failed — force re-login
    const signInUrl = new URL('/sign-in', request.url);
    signInUrl.searchParams.set('callbackUrl', request.nextUrl.pathname);
    return NextResponse.redirect(signInUrl);
  }

  // If token was refreshed, update the request cookies so that downstream
  // Server Components (which read cookies via next/headers) see the fresh token.
  // This is critical because cookies().set() is read-only in Server Components,
  // so the middleware is the ONLY place we can persist refreshed tokens.
  let newEncrypted: string | null = null;
  if (updated && token) {
    newEncrypted = await encodeSession(token, authConfig);
    // Propagate to request so Server Components see the refreshed cookie
    sessionStore.write(newEncrypted, (name, value) => {
      request.cookies.set(name, value);
    });
  }

  // Run intl middleware (with potentially updated request cookies)
  const response = intlMiddleware(request);

  // Persist refreshed token to response cookies (sent to browser for future requests)
  if (newEncrypted) {
    sessionStore.write(newEncrypted, (name, value, opts) => {
      response.cookies.set(name, value, opts);
    });
  }

  return response;
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

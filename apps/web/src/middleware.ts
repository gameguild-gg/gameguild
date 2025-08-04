import { auth } from '@/auth';
import { routing } from '@/i18n/routing';
import createMiddleware from 'next-intl/middleware';
import { NextRequest } from 'next/server';

// Create the internationalization middleware
const intlMiddleware = createMiddleware(routing);

// Define public routes that don't require authentication
const publicRoutes = [
  '/',
  '/en',
  '/pt-BR',
  '/sign-in',
  '/sign-up',
  '/about',
  '/contact',
  '/privacy',
  '/terms',
  '/blog',
  '/courses',
  '/jobs',
];

// Check if the current path is a public route
function isPublicRoute(pathname: string): boolean {
  // Simple check - if it's a root path or starts with /en or /pt-BR, it's public
  return pathname === '/' || 
         pathname === '/en' || 
         pathname === '/pt-BR' ||
         pathname.startsWith('/en/') || 
         pathname.startsWith('/pt-BR/');
}

// Chain auth middleware first, then intl middleware
export default auth((request) => {
  const { pathname } = request.nextUrl;
  
  // Debug logging
  console.log('Middleware processing pathname:', pathname, 'isPublic:', isPublicRoute(pathname));
  
  // If it's a public route, skip authentication and just handle i18n
  if (isPublicRoute(pathname)) {
    return intlMiddleware(request);
  }

  // Get tenant ID from auth session and create request with tenant headers
  const tenantId = request.auth?.tenantId || request.auth?.currentTenant?.id;

  const finalRequest = tenantId
    ? new NextRequest(request.url, {
        headers: new Headers({ ...request.headers, 'x-tenant-id': tenantId }),
      })
    : request;

  // Always proceed with intl middleware
  return intlMiddleware(finalRequest);
});

export const config = {
  // Match all pathnames except for
  // - … if they start with `/api`, `/trpc`, `/_next` or `/_vercel`
  // - … the ones containing a dot (e.g. `favicon.ico`)
  matcher: ['/((?!api|trpc|_next|_vercel|.*\\..*).*)'],
};

import { auth } from '@/auth';
import { routing } from '@/i18n/routing';
import createMiddleware from 'next-intl/middleware';
import { NextRequest } from 'next/server';

const intlMiddleware = createMiddleware(routing);

export default auth((request) => {
  // Get tenant ID from auth session
  const tenantId = request.auth?.tenantId || (request.auth as { currentTenant?: { id: string } })?.currentTenant?.id;

  // Add tenant ID to request headers if available
  if (tenantId) {
    const requestHeaders = new Headers(request.headers);
    requestHeaders.set('x-tenant-id', tenantId);

    // Create a new request with tenant headers
    const requestWithTenant = new NextRequest(request.url, {
      headers: requestHeaders,
    });

    // Handle internationalization with a tenant-aware request
    return intlMiddleware(requestWithTenant);
  }

  // Handle internationalization normally if no tenant
  return intlMiddleware(request as NextRequest);
});

export const config = {
  // Match all pathnames except for
  // - … if they start with `/api`, `/trpc`, `/_next` or `/_vercel`
  // - … the ones containing a dot (e.g. `favicon.ico`)
  matcher: '/((?!api|trpc|_next|_vercel|.*\\..*).*)',
};

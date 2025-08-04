import { auth } from '@/auth';
import { routing } from '@/i18n/routing';
import createMiddleware from 'next-intl/middleware';
import { NextRequest } from 'next/server';

// Create the internationalization middleware
const intlMiddleware = createMiddleware(routing);

// Chain auth middleware first, then intl middleware
export default auth((request) => {
  // Get the hostname from the request
  const hostname = request.nextUrl.hostname;
  
  // If we're in development and getting requests for external domains, reject them
  if (process.env.NODE_ENV !== 'production' && 
      hostname !== 'localhost' && 
      hostname !== '127.0.0.1' && 
      !hostname.startsWith('192.168.') && 
      !hostname.startsWith('10.') &&
      hostname !== request.nextUrl.hostname) {
    console.warn(`Rejecting request to external hostname: ${hostname}`);
    return new Response('External requests not allowed in development', { status: 403 });
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

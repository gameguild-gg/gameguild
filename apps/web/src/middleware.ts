import { auth } from '@/auth';
import { NextRequest, NextResponse } from 'next/server';

export default auth((request) => {
  // Get tenant ID from auth session
  const tenantId = request.auth?.tenantId || (request.auth as { currentTenant?: { id: string } })?.currentTenant?.id;

  // Add tenant ID to request headers if available
  if (tenantId) {
    const requestHeaders = new Headers(request.headers);
    requestHeaders.set('x-tenant-id', tenantId);

    // Create a response that continues to the next middleware/handler
    const response = NextResponse.next({
      request: {
        headers: requestHeaders,
      },
    });

    return response;
  }

  // Continue normally if no tenant
  return NextResponse.next();
});

export const config = {
  // Match all pathnames except for
  // - … if they start with `/api`, `/trpc`, `/_next` or `/_vercel`
  // - … the ones containing a dot (e.g. `favicon.ico`)
  matcher: '/((?!api|trpc|_next|_vercel|.*\\..*).*)',
};

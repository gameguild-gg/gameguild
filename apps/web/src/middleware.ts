import { auth } from '@/auth';
import { routing } from '@/i18n/routing';
import createMiddleware from 'next-intl/middleware';
import { NextRequest, NextResponse } from 'next/server';

export function middleware(request: NextRequest) {
  const hostname = request.headers.get('host') || '';
  const locale = request.cookies.get('NEXT_LOCALE')?.value || 'en';

  // Subdomain handling for dev.gameguild.gg
  if (hostname.startsWith('dev.')) {
    const url = request.nextUrl.clone();

    // For subdomain, handle locale routing differently
    if (locale === "en") {
      // For default locale, don't add locale prefix
      request.nextUrl.pathname = `${url.pathname}`;
    } else {
      // For non-default locales, add locale prefix if not already present
      if (!url.pathname.startsWith(`/${locale}`)) {
        request.nextUrl.pathname = `/${locale}${url.pathname}`;
      }
    }
  }

  // Handle internationalization routing
  // The next-intl middleware will automatically pick up any basePath from next.config.js
  const handleI18nRouting = createMiddleware(routing);
  const response = handleI18nRouting(request);
  
  return response;
}

export const config = {
  // The `matcher` is relative to the `basePath`
  matcher: [
    // This entry handles the root of the base path and should always be included
    '/',
    // Match all other pathnames except for
    // - … if they start with `/api`, `/_next` or `/_vercel`
    // - … the ones containing a dot (e.g. `favicon.ico`)
    '/((?!api|_next|_vercel|.*\\..*).*)' 
  ],
};

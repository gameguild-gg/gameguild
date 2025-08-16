import { routing } from '@/i18n/routing';
import createMiddleware from 'next-intl/middleware';
import { NextRequest, NextResponse } from 'next/server';

export function middleware(request: NextRequest) {
  const hostname = request.headers.get('host') || '';
  const locale = request.cookies.get('NEXT_LOCALE')?.value || 'en';

  // Debug logging
  console.log('Middleware processing:', {
    url: request.url,
    pathname: request.nextUrl.pathname,
    method: request.method,
    hostname: hostname,
    locale: locale
  });
  
  // Check if this is a subdomain (has more than one dot, excluding localhost/127.0.0.1)
  // Examples: dev.gameguild.gg (subdomain), gameguild.gg (main domain), localhost:3000 (not subdomain)
  const isSubdomain = hostname.split('.').length > 2 && !hostname.startsWith('localhost') && !hostname.startsWith('127.0.0.1');
  
  // Subdomain handling for any subdomain (dev, staging, prod, etc.)
  if (isSubdomain) {
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

  // Custom rewrites: support legacy paths like /projects and /projects/:slug
  // Map them to the dashboard route while preserving locale prefix semantics
  const pathname = request.nextUrl.pathname;
  const localePattern = '(?:en|pt-BR)';
  // Match: /projects or /:locale/projects
  const projectsIndexRegex = new RegExp(`^\/(?:${localePattern}\/)?projects\/?$`);
  // Match: /projects/:slug or /:locale/projects/:slug
  const projectsSlugRegex = new RegExp(`^\/(?:(${localePattern})\/)?projects\/([^\/]+)\/?$`);

  if (projectsIndexRegex.test(pathname)) {
    const url = request.nextUrl.clone();
    // Keep current path's locale if present or cookie/default
    const hasLocaleInPath = new RegExp(`^\/(${localePattern})\/`).test(pathname);
    url.pathname = `${hasLocaleInPath && locale !== 'en' ? `/${locale}` : ''}/dashboard/projects`;
    return NextResponse.redirect(url);
  }

  const slugMatch = pathname.match(projectsSlugRegex);
  if (slugMatch) {
    const url = request.nextUrl.clone();
    const slug = slugMatch[2];
    const pathHasLocale = !!slugMatch[1];
    url.pathname = `${pathHasLocale && locale !== 'en' ? `/${locale}` : ''}/dashboard/projects/${slug}`;
    return NextResponse.redirect(url);
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

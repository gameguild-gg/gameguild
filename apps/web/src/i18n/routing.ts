import { defineRouting } from 'next-intl/routing';

export const routing = defineRouting({
  // A list of all locales that are supported
  locales: ['en', 'pt-BR'],
  localePrefix: 'always',
  // Used when no locale matches
  defaultLocale: 'en',
  // Disable automatic redirects to prevent DNS resolution issues
  pathnames: {
    '/': '/',
    '/en': '/en',
    '/pt-BR': '/pt-BR',
  },
});

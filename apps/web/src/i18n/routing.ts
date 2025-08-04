import { defineRouting } from 'next-intl/routing';

export const routing = defineRouting({
  // A list of all locales that are supported
  locales: ['en', 'pt-BR'],
  localePrefix: 'as-needed',
  // Used when no locale matches
  defaultLocale: 'en',
  // Add domains configuration to handle subdomains properly
  domains: process.env.NODE_ENV === 'production' ? [] : undefined,
});

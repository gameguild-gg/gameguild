import { defineRouting } from 'next-intl/routing';

export const routing = defineRouting({
  // A list of all locales that are supported
  locales: ['en', 'pt-BR'],
  localePrefix: 'as-needed',
  // Used when no locale matches
  defaultLocale: 'en',
  // Configure locale cookie for subdomain handling
  localeCookie: {
    path: "/",
    domain: ".gameguild.gg",
    maxAge: 60 * 60 * 24 * 4, // 4 days
    sameSite: "lax",
    secure: process.env.NODE_ENV === 'production'
  },
});

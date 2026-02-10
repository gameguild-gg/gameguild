import { defineRouting } from 'next-intl/routing';

const locales = ['en-US', 'pt-BR'] as const;

const prefixes = Object.fromEntries(
  locales.map((locale) => [locale, locale.toLowerCase()])
) as Record<(typeof locales)[number], string>;

export const routing = defineRouting({
  locales: ['en-US', 'pt-BR'],
  localePrefix: {
    mode: 'as-needed',
    prefixes: prefixes,
  },
  defaultLocale: 'en-US',
});

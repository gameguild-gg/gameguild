import { defineRouting } from 'next-intl/routing';

export const routing = defineRouting({
  locales: ['en-US', 'pt-BR'],
  localePrefix: 'as-needed',
  defaultLocale: 'en-US',
});

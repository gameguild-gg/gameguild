import { defineRouting } from 'next-intl/routing';

export const routing = defineRouting({
  locales: ['en-US', 'pt-BR'],
  // Next 16 reprocesses the internal default-locale rewrite produced by
  // `as-needed`, which turns it into a redirect back to the unprefixed URL.
  // Keeping every locale explicit avoids that production-only redirect loop.
  localePrefix: 'always',
  defaultLocale: 'en-US',
});

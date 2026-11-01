import { hasLocale } from 'next-intl';
import { getRequestConfig } from 'next-intl/server';
import { loadMessagesForLocale } from './load-messages';
import { routing } from './routing';

export default getRequestConfig(async ({ requestLocale }) => {
  const requested = await requestLocale;
  const locale = hasLocale(routing.locales, requested) ? requested : routing.defaultLocale;

  return {
    locale: locale,
    // Non-default locales are deep-merged over the default catalog so keys
    // missing from a translation fall back to English gracefully (issue #92).
    messages: await loadMessagesForLocale(locale),
  };
});

import { routing } from './routing';

type Messages = Record<string, unknown>;

function isPlainObject(value: unknown): value is Messages {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/**
 * Deep-merges localized messages over the default locale's messages so a
 * missing key in a translation falls back to English instead of rendering a
 * raw key path (issue #92). Localized values always win; keys absent from
 * the override keep the fallback value.
 */
export function deepMergeMessages(base: Messages, override: Messages): Messages {
  const merged: Messages = { ...base };
  for (const [key, value] of Object.entries(override)) {
    const existing = merged[key];
    merged[key] =
      isPlainObject(value) && isPlainObject(existing) ? deepMergeMessages(existing, value) : value;
  }
  return merged;
}

/**
 * Loads the message catalog for a locale. The default locale loads its own
 * catalog; every other locale is deep-merged over the default catalog, so
 * untranslated keys degrade gracefully to English at runtime even when the
 * build-time parity checks have not caught up with a new key.
 */
export async function loadMessagesForLocale(locale: string): Promise<Messages> {
  const localized: unknown = (await import(`./messages/${locale}.json`)).default;
  if (locale === routing.defaultLocale || !isPlainObject(localized)) {
    return isPlainObject(localized) ? localized : {};
  }
  const fallback: unknown = (await import(`./messages/${routing.defaultLocale}.json`)).default;
  return deepMergeMessages(isPlainObject(fallback) ? fallback : {}, localized);
}

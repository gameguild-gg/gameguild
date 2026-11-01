import { routing } from './routing';

// Structurally identical to next-intl's AbstractIntlMessages, so the loaded
// catalog is assignable to `messages` in the request config without a cast.
export type MessageCatalog = { [key: string]: string | MessageCatalog };

function isMessageNamespace(value: unknown): value is MessageCatalog {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/**
 * Deep-merges localized messages over the default locale's messages so a
 * missing key in a translation falls back to English instead of rendering a
 * raw key path (issue #92). Localized values always win; keys absent from
 * the override keep the fallback value.
 */
export function deepMergeMessages(base: MessageCatalog, override: MessageCatalog): MessageCatalog {
  const merged: MessageCatalog = { ...base };
  for (const [key, value] of Object.entries(override)) {
    const existing = merged[key];
    merged[key] =
      isMessageNamespace(value) && isMessageNamespace(existing)
        ? deepMergeMessages(existing, value)
        : value;
  }
  return merged;
}

/**
 * Loads the message catalog for a locale. The default locale loads its own
 * catalog; every other locale is deep-merged over the default catalog, so
 * untranslated keys degrade gracefully to English at runtime even when the
 * build-time parity checks have not caught up with a new key.
 */
export async function loadMessagesForLocale(locale: string): Promise<MessageCatalog> {
  // The dynamic specifier defeats JSON module typing; assert the module shape
  // so the catalogs flow through the namespace narrowing below.
  const localized = (await import(`./messages/${locale}.json`)) as { default: unknown };
  if (locale === routing.defaultLocale || !isMessageNamespace(localized.default)) {
    return isMessageNamespace(localized.default) ? localized.default : {};
  }
  const fallback = (await import(`./messages/${routing.defaultLocale}.json`)) as {
    default: unknown;
  };
  return deepMergeMessages(
    isMessageNamespace(fallback.default) ? fallback.default : {},
    localized.default,
  );
}

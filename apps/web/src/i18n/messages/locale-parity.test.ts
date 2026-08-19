import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * en-US and pt-BR must stay key-for-key compatible for the namespaces this
 * feature owns, so next-intl never falls back to a raw key path in one locale.
 */
function flatten(object: Record<string, unknown>, prefix = ''): string[] {
  return Object.entries(object).flatMap(([key, value]) =>
    typeof value === 'object' && value !== null
      ? flatten(value as Record<string, unknown>, `${prefix}${key}.`)
      : [`${prefix}${key}`],
  );
}

function loadMessages(locale: string): Record<string, unknown> {
  return JSON.parse(
    readFileSync(join(__dirname, `${locale}.json`), 'utf8'),
  );
}

describe('i18n locale parity', () => {
  it.each(['notificationPrefs', 'settings'])(
    '%s has identical key sets in en-US and pt-BR',
    (namespace) => {
      const en = flatten(loadMessages('en-US')[namespace] as Record<string, unknown>).sort();
      const pt = flatten(loadMessages('pt-BR')[namespace] as Record<string, unknown>).sort();

      expect(pt).toEqual(en);
    },
  );
});

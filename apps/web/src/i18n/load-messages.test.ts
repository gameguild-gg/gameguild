import { describe, expect, it } from 'vitest';

import { deepMergeMessages, loadMessagesForLocale } from './load-messages';
import { routing } from './routing';

describe('deepMergeMessages', () => {
  it('keeps fallback values for keys the override does not translate', () => {
    expect(deepMergeMessages({ greeting: 'Hello', farewell: 'Goodbye' }, { greeting: 'Olá' })).toEqual({
      greeting: 'Olá',
      farewell: 'Goodbye',
    });
  });

  it('merges nested namespaces without losing sibling fallback keys', () => {
    expect(
      deepMergeMessages(
        { settings: { language: 'Language', timezone: 'Timezone' } },
        { settings: { language: 'Idioma' } },
      ),
    ).toEqual({ settings: { language: 'Idioma', timezone: 'Timezone' } });
  });

  it('lets the localized value win for every key it defines', () => {
    expect(deepMergeMessages({ save: 'Save' }, { save: 'Salvar' })).toEqual({ save: 'Salvar' });
  });

  it('replaces non-object values instead of merging them', () => {
    expect(deepMergeMessages({ items: ['a'] }, { items: ['b'] })).toEqual({ items: ['b'] });
    expect(deepMergeMessages({ label: 'x' }, { label: { nested: 'y' } })).toEqual({
      label: { nested: 'y' },
    });
  });
});

function flattenLeaves(value: unknown, prefix = ''): Record<string, unknown> {
  if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
    return Object.entries(value).reduce<Record<string, unknown>>((acc, [key, child]) => {
      Object.assign(acc, flattenLeaves(child, `${prefix}${key}.`));
      return acc;
    }, {});
  }
  return { [prefix.slice(0, -1)]: value };
}

describe('loadMessagesForLocale fallback behavior (issue #92)', () => {
  it('serves every default-locale key under a non-default locale', async () => {
    const fallback = await loadMessagesForLocale(routing.defaultLocale);
    const localized = await loadMessagesForLocale('pt-BR');

    expect(Object.keys(flattenLeaves(localized)).sort()).toEqual(
      Object.keys(flattenLeaves(fallback)).sort(),
    );
  });

  it('prefers the pt-BR translation where one exists', async () => {
    const fallback = await loadMessagesForLocale(routing.defaultLocale);
    const localized = await loadMessagesForLocale('pt-BR');

    const fallbackLeaves = flattenLeaves(fallback);
    const localizedLeaves = flattenLeaves(localized);
    const translatedPaths = Object.keys(localizedLeaves).filter(
      (path) => fallbackLeaves[path] !== localizedLeaves[path],
    );

    // The merge must load the real pt-BR catalog, not just the fallback.
    expect(translatedPaths.length).toBeGreaterThan(0);
  });
});

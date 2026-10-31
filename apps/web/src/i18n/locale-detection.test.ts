import { describe, expect, it } from 'vitest';

import {
  getPreferredLocaleFromCookieHeader,
  LOCALE_COOKIE_MAX_AGE_SECONDS,
  LOCALE_COOKIE_NAME,
  parseAcceptLanguageHeader,
  resolveLocaleForRequest,
  resolveLocaleFromAcceptLanguageHeader,
} from './locale-detection';
import { routing } from './routing';

describe('Accept-Language parsing', () => {
  it('parses simple and weighted ranges preserving header order between equal qualities', () => {
    expect(parseAcceptLanguageHeader('pt-BR,en;q=0.8')).toEqual([
      { tag: 'pt-BR', quality: 1 },
      { tag: 'en', quality: 0.8 },
    ]);
  });

  it('sorts entries by descending quality', () => {
    expect(parseAcceptLanguageHeader('en;q=0.5,pt-BR;q=0.9')).toEqual([
      { tag: 'pt-BR', quality: 0.9 },
      { tag: 'en', quality: 0.5 },
    ]);
  });

  it('defaults a missing quality to 1', () => {
    expect(parseAcceptLanguageHeader('fr')[0]?.quality).toBe(1);
  });

  it('drops ranges explicitly excluded with q=0', () => {
    expect(parseAcceptLanguageHeader('en-US,pt-BR;q=0')).toEqual([
      { tag: 'en-US', quality: 1 },
    ]);
  });

  it('clamps out-of-range qualities into [0, 1]', () => {
    expect(parseAcceptLanguageHeader('de;q=5,en;q=-2')).toEqual([
      { tag: 'de', quality: 1 },
    ]);
  });

  it('ignores malformed segments and whitespace', () => {
    expect(parseAcceptLanguageHeader('  pt-BR ; q=0.9 , , ;; ,en')).toEqual([
      { tag: 'en', quality: 1 },
      { tag: 'pt-BR', quality: 0.9 },
    ]);
  });

  it('returns an empty list for missing or empty headers', () => {
    expect(parseAcceptLanguageHeader(null)).toEqual([]);
    expect(parseAcceptLanguageHeader('')).toEqual([]);
    expect(parseAcceptLanguageHeader(undefined)).toEqual([]);
  });
});

describe('Accept-Language negotiation against supported locales', () => {
  it('returns the exact supported locale', () => {
    expect(resolveLocaleFromAcceptLanguageHeader('pt-BR,pt;q=0.9,en;q=0.8')).toBe('pt-BR');
    expect(resolveLocaleFromAcceptLanguageHeader('en-US,en;q=0.9')).toBe('en-US');
  });

  it('matches case-insensitively', () => {
    expect(resolveLocaleFromAcceptLanguageHeader('PT-br')).toBe('pt-BR');
  });

  it('maps a language-only range onto the supported locale of that language', () => {
    expect(resolveLocaleFromAcceptLanguageHeader('pt')).toBe('pt-BR');
    expect(resolveLocaleFromAcceptLanguageHeader('en')).toBe('en-US');
  });

  it('maps a regional variant onto the supported locale of that language', () => {
    expect(resolveLocaleFromAcceptLanguageHeader('pt-PT,pt;q=0.9')).toBe('pt-BR');
    expect(resolveLocaleFromAcceptLanguageHeader('en-GB,en;q=0.9')).toBe('en-US');
  });

  it('respects quality ordering across entries', () => {
    expect(resolveLocaleFromAcceptLanguageHeader('en;q=0.5,pt-BR;q=0.9')).toBe('pt-BR');
  });

  it('falls back to English for unsupported languages (issue #99 German report)', () => {
    expect(resolveLocaleFromAcceptLanguageHeader('de-DE,de;q=0.9,en;q=0.8')).toBe('en-US');
    expect(resolveLocaleFromAcceptLanguageHeader('ja')).toBe('en-US');
  });

  it('skips q=0 ranges before falling back', () => {
    expect(resolveLocaleFromAcceptLanguageHeader('pt-BR;q=0,en-US')).toBe('en-US');
  });

  it('falls back to the default locale for wildcard, empty or missing headers', () => {
    expect(resolveLocaleFromAcceptLanguageHeader('*')).toBe(routing.defaultLocale);
    expect(resolveLocaleFromAcceptLanguageHeader(null)).toBe(routing.defaultLocale);
    expect(resolveLocaleFromAcceptLanguageHeader('')).toBe(routing.defaultLocale);
  });
});

describe('locale preference cookie', () => {
  it('reads a supported locale from the cookie header among other cookies', () => {
    expect(
      getPreferredLocaleFromCookieHeader('session=abc; NEXT_LOCALE=pt-BR; theme=dark'),
    ).toBe('pt-BR');
  });

  it('normalizes cookie casing to the configured locale', () => {
    expect(getPreferredLocaleFromCookieHeader('NEXT_LOCALE=pt-br')).toBe('pt-BR');
  });

  it('ignores unsupported or missing cookie values', () => {
    expect(getPreferredLocaleFromCookieHeader('NEXT_LOCALE=de')).toBeUndefined();
    expect(getPreferredLocaleFromCookieHeader('theme=dark')).toBeUndefined();
    expect(getPreferredLocaleFromCookieHeader(null)).toBeUndefined();
  });

  it('exposes the next-intl-compatible cookie contract', () => {
    expect(LOCALE_COOKIE_NAME).toBe('NEXT_LOCALE');
    expect(LOCALE_COOKIE_MAX_AGE_SECONDS).toBe(60 * 60 * 24 * 365);
  });
});

describe('full request resolution', () => {
  it('prefers the stored choice over browser detection (issue #99 override)', () => {
    expect(resolveLocaleForRequest('NEXT_LOCALE=en-US', 'pt-BR,pt;q=0.9')).toBe('en-US');
    expect(resolveLocaleForRequest('NEXT_LOCALE=pt-BR', 'de-DE,de;q=0.9')).toBe('pt-BR');
  });

  it('uses browser detection when no valid preference is stored', () => {
    expect(resolveLocaleForRequest(null, 'pt-BR')).toBe('pt-BR');
    expect(resolveLocaleForRequest('NEXT_LOCALE=de', 'pt-BR')).toBe('pt-BR');
    expect(resolveLocaleForRequest(undefined, undefined)).toBe(routing.defaultLocale);
  });
});

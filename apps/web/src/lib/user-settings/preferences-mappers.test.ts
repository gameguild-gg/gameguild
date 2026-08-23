import { describe, expect, it } from 'vitest';

import {
  DEFAULT_ACCESSIBILITY,
  MAX_FONT_SIZE,
  MIN_FONT_SIZE,
  buildAccessibilityPayload,
  buildLocalizationPayload,
  buildPrivacyPayload,
  normalizeAccessibility,
  parseGeneralPreferences,
} from './preferences-mappers';

describe('accessibility preference mapping', () => {
  it('writes only the accessibility choices the web shell can apply', () => {
    const payload = buildAccessibilityPayload({
      ...DEFAULT_ACCESSIBILITY,
      highContrast: true,
      largeText: true,
      reducedMotion: true,
      fontSize: 18,
    });

    expect(payload).toEqual({
      accessibilityPreferences: {
        HighContrast: true,
        LargeText: true,
        ReducedMotion: true,
        FontSize: 18,
      },
    });
  });

  it('keeps the persisted preferences within the supported font-size range', () => {
    expect(normalizeAccessibility({ fontSize: MIN_FONT_SIZE - 4 }).fontSize).toBe(MIN_FONT_SIZE);
    expect(normalizeAccessibility({ fontSize: MAX_FONT_SIZE + 4 }).fontSize).toBe(MAX_FONT_SIZE);
  });

  it('maps localization strings and privacy booleans to the API scalar contract', () => {
    expect(buildLocalizationPayload({
      language: 'pt-BR',
      timezone: 'America/Sao_Paulo',
      dateFormat: 'dd/MM/yyyy',
      timeFormat: '24h',
      currency: 'BRL',
    })).toEqual({
      localizationPreferences: {
        Language: 'pt-BR',
        Timezone: 'America/Sao_Paulo',
        DateFormat: 'dd/MM/yyyy',
        TimeFormat: '24h',
        Currency: 'BRL',
      },
    });
    expect(buildPrivacyPayload({
      profileVisibility: 'private',
      activityTracking: false,
      marketingEmails: true,
      analyticsCookies: false,
      personalizedContent: true,
    })).toEqual({
      privacyPreferences: {
        ProfileVisibility: 'private',
        ActivityTracking: false,
        MarketingEmails: true,
        AnalyticsCookies: false,
        PersonalizedContent: true,
      },
    });
  });

  it('reads the server-backed theme and editor preferences from their documented keys', () => {
    expect(parseGeneralPreferences({
      theme: 'dark',
      EditorPreferences: { modalSize: 'widescreen', editor: { fontSize: 16 } },
    })).toEqual({
      theme: 'dark',
      editorPreferences: { modalSize: 'widescreen', editor: { fontSize: 16 } },
    });
  });
});

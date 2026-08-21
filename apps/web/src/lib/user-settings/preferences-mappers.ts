/**
 * User settings preference mappers
 *
 * Pure functions translating between the API's preference payloads and the
 * shapes the settings forms consume. The API stores each preference
 * category as a flat `Dictionary<string, JsonElement>` (scalar values per
 * key, PascalCase keys), so these mappers are the single place that knows
 * how to read and write that contract.
 */

export type ThemePreference = 'light' | 'dark' | 'system';
export type PreferencePatch = Record<string, Record<string, unknown>>;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function getString(record: Record<string, unknown>, key: string): string | null {
  const value = record[key];
  return typeof value === 'string' ? value : null;
}

function getBoolean(record: Record<string, unknown>, key: string): boolean | null {
  const value = record[key];
  return typeof value === 'boolean' ? value : null;
}

function getNumber(record: Record<string, unknown>, key: string): number | null {
  const value = record[key];
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

export function parseThemePreference(value: unknown): ThemePreference | null {
  if (value === 'light' || value === 'dark' || value === 'system') {
    return value;
  }

  return null;
}

/** Raw shape of the `generalPreferences` bag returned by GET /users/{id}/preferences. */
export type GeneralPreferencesData = {
  readonly theme: ThemePreference | null;
  readonly editorPreferences: EditorGlobalPreferencesBlob | null;
};

/**
 * Mirrors the editor's global preferences (`EditorPreferences` global scope)
 * stored as one object value inside `generalPreferences`. Structurally typed
 * so the server actions never need to import the editor's modules.
 */
export type EditorGlobalPreferencesBlob = {
  readonly modalSize?: string;
  readonly editor?: Record<string, unknown>;
  readonly preview?: Record<string, unknown>;
};

function parseEditorGlobalPreferences(value: unknown): EditorGlobalPreferencesBlob | null {
  if (!isRecord(value)) return null;

  const modalSize = getString(value, 'modalSize');
  const editor = isRecord(value['editor']) ? value['editor'] : undefined;
  const preview = isRecord(value['preview']) ? value['preview'] : undefined;

  return {
    ...(modalSize ? { modalSize } : {}),
    ...(editor ? { editor } : {}),
    ...(preview ? { preview } : {}),
  };
}

export function parseGeneralPreferences(raw: unknown): GeneralPreferencesData {
  if (!isRecord(raw)) {
    return { theme: null, editorPreferences: null };
  }

  return {
    theme: parseThemePreference(raw['theme']),
    editorPreferences: parseEditorGlobalPreferences(raw['EditorPreferences']),
  };
}

// ---------------------------------------------------------------------------
// Localization
// ---------------------------------------------------------------------------

export type LocalizationPreferenceData = {
  readonly language: string;
  readonly timezone: string;
  readonly dateFormat: string;
  readonly timeFormat: string;
  readonly currency: string;
};

export const DEFAULT_LOCALIZATION: LocalizationPreferenceData = {
  language: 'en-US',
  timezone: 'UTC',
  dateFormat: 'MM/dd/yyyy',
  timeFormat: '12h',
  currency: 'USD',
};

export function normalizeLocalization(dto: unknown): LocalizationPreferenceData {
  if (!isRecord(dto)) return { ...DEFAULT_LOCALIZATION };

  const language = getString(dto, 'language')?.trim();
  const timezone = getString(dto, 'timezone')?.trim();
  const dateFormat = getString(dto, 'dateFormat')?.trim();
  const timeFormat = getString(dto, 'timeFormat');
  const currency = getString(dto, 'currency')?.trim();

  return {
    language: language || DEFAULT_LOCALIZATION.language,
    timezone: timezone || DEFAULT_LOCALIZATION.timezone,
    dateFormat: dateFormat || DEFAULT_LOCALIZATION.dateFormat,
    timeFormat: timeFormat === '12h' || timeFormat === '24h' ? timeFormat : DEFAULT_LOCALIZATION.timeFormat,
    currency: currency || DEFAULT_LOCALIZATION.currency,
  };
}

/** Builds the PATCH body fragment for the localization preferences endpoint. */
export function buildLocalizationPayload(data: LocalizationPreferenceData): PreferencePatch {
  return {
    localizationPreferences: {
      Language: data.language,
      Timezone: data.timezone,
      DateFormat: data.dateFormat,
      TimeFormat: data.timeFormat,
      Currency: data.currency,
    },
  };
}

// ---------------------------------------------------------------------------
// Privacy
// ---------------------------------------------------------------------------

export type ProfileVisibility = 'public' | 'members' | 'private';

export type PrivacyPreferenceData = {
  readonly profileVisibility: ProfileVisibility;
  readonly activityTracking: boolean;
  readonly marketingEmails: boolean;
  readonly analyticsCookies: boolean;
  readonly personalizedContent: boolean;
};

export const DEFAULT_PRIVACY: PrivacyPreferenceData = {
  profileVisibility: 'public',
  activityTracking: true,
  marketingEmails: true,
  analyticsCookies: true,
  personalizedContent: true,
};

function parseProfileVisibility(value: unknown): ProfileVisibility {
  if (value === 'public' || value === 'members' || value === 'private') {
    return value;
  }

  return DEFAULT_PRIVACY.profileVisibility;
}

export function normalizePrivacy(dto: unknown): PrivacyPreferenceData {
  if (!isRecord(dto)) return { ...DEFAULT_PRIVACY };

  return {
    profileVisibility: parseProfileVisibility(dto['profileVisibility']),
    activityTracking: getBoolean(dto, 'activityTracking') ?? DEFAULT_PRIVACY.activityTracking,
    marketingEmails: getBoolean(dto, 'marketingEmails') ?? DEFAULT_PRIVACY.marketingEmails,
    analyticsCookies: getBoolean(dto, 'analyticsCookies') ?? DEFAULT_PRIVACY.analyticsCookies,
    personalizedContent: getBoolean(dto, 'personalizedContent') ?? DEFAULT_PRIVACY.personalizedContent,
  };
}

/** Builds the PATCH body fragment for the privacy preferences endpoint. */
export function buildPrivacyPayload(data: PrivacyPreferenceData): PreferencePatch {
  return {
    privacyPreferences: {
      ProfileVisibility: data.profileVisibility,
      ActivityTracking: data.activityTracking,
      MarketingEmails: data.marketingEmails,
      AnalyticsCookies: data.analyticsCookies,
      PersonalizedContent: data.personalizedContent,
    },
  };
}

// ---------------------------------------------------------------------------
// Accessibility
// ---------------------------------------------------------------------------

export type AccessibilityPreferenceData = {
  readonly highContrast: boolean;
  readonly largeText: boolean;
  readonly reducedMotion: boolean;
  readonly fontSize: number;
};

export const DEFAULT_ACCESSIBILITY: AccessibilityPreferenceData = {
  highContrast: false,
  largeText: false,
  reducedMotion: false,
  fontSize: 14,
};

export const MIN_FONT_SIZE = 12;
export const MAX_FONT_SIZE = 20;

function clampFontSize(value: unknown): number {
  const parsed = typeof value === 'number' ? value : Number.NaN;
  if (!Number.isFinite(parsed)) return DEFAULT_ACCESSIBILITY.fontSize;
  return Math.min(MAX_FONT_SIZE, Math.max(MIN_FONT_SIZE, Math.round(parsed)));
}

export function normalizeAccessibility(dto: unknown): AccessibilityPreferenceData {
  if (!isRecord(dto)) return { ...DEFAULT_ACCESSIBILITY };

  return {
    highContrast: getBoolean(dto, 'highContrast') ?? DEFAULT_ACCESSIBILITY.highContrast,
    largeText: getBoolean(dto, 'largeText') ?? DEFAULT_ACCESSIBILITY.largeText,
    reducedMotion: getBoolean(dto, 'reducedMotion') ?? DEFAULT_ACCESSIBILITY.reducedMotion,
    fontSize: clampFontSize(getNumber(dto, 'fontSize')),
  };
}

/** Builds the PATCH body fragment for the accessibility preferences endpoint. */
export function buildAccessibilityPayload(data: AccessibilityPreferenceData): PreferencePatch {
  return {
    accessibilityPreferences: {
      HighContrast: data.highContrast,
      LargeText: data.largeText,
      ReducedMotion: data.reducedMotion,
      FontSize: data.fontSize,
    },
  };
}

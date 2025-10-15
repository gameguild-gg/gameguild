/**
 * Cookie management utilities for handling user consent and preferences
 * Compatible with Next.js 15+ and GDPR compliance
 */

export type CookieCategory = 'essential' | 'analytics' | 'marketing' | 'functional';

export interface CookiePreferences {
  essential: boolean;
  analytics: boolean;
  marketing: boolean;
  functional: boolean;
  consentDate: string;
  version: string;
}

export const COOKIE_CONSENT_KEY = 'game_guild_cookie_consent';
export const COOKIE_PREFERENCES_KEY = 'game_guild_cookie_preferences';
export const COOKIE_VERSION = '1.0.0';

// Tri-state consent model
export type ConsentState = 'accepted' | 'denied' | 'not_answered';

// Default preferences - essential cookies are always enabled
export const defaultPreferences: CookiePreferences = {
  essential: true,
  analytics: false,
  marketing: false,
  functional: false,
  consentDate: new Date().toISOString(),
  version: COOKIE_VERSION,
};

/**
 * Check if the code is running on the client side
 */
export const isClient = () => typeof window !== 'undefined';

/**
 * Get cookie preferences from localStorage
 */
export const getCookiePreferences = (): CookiePreferences => {
  if (!isClient()) return defaultPreferences;

  try {
    const stored = localStorage.getItem(COOKIE_PREFERENCES_KEY);
    if (!stored) return defaultPreferences;

    const parsed = JSON.parse(stored) as CookiePreferences;

    // Check if version matches, if not reset to defaults
    if (parsed.version !== COOKIE_VERSION) {
      return defaultPreferences;
    }

    return {
      ...defaultPreferences,
      ...parsed,
      essential: true, // Essential cookies cannot be disabled
    };
  } catch (error) {
    console.warn('Failed to parse cookie preferences:', error);
    return defaultPreferences;
  }
};

/**
 * Save cookie preferences to localStorage
 */
export const saveCookiePreferences = (preferences: Partial<CookiePreferences>): void => {
  if (!isClient()) return;

  const currentPreferences = getCookiePreferences();
  const newPreferences: CookiePreferences = {
    ...currentPreferences,
    ...preferences,
    essential: true, // Essential cookies cannot be disabled
    consentDate: new Date().toISOString(),
    version: COOKIE_VERSION,
  };

  try {
    localStorage.setItem(COOKIE_PREFERENCES_KEY, JSON.stringify(newPreferences));

    // Set consent to accepted (tri-state)
    localStorage.setItem(COOKIE_CONSENT_KEY, 'accepted');

    // Trigger custom event for other components to listen to
    if (isClient()) {
      window.dispatchEvent(
        new CustomEvent('cookiePreferencesChanged', {
          detail: newPreferences,
        }),
      );
    }
  } catch (error) {
    console.error('Failed to save cookie preferences:', error);
  }
};

// Get tri-state consent value (maps legacy values)
export const getConsentState = (): ConsentState => {
  if (!isClient()) return 'not_answered';
  const raw = localStorage.getItem(COOKIE_CONSENT_KEY);
  if (raw === 'accepted' || raw === 'true') return 'accepted';
  if (raw === 'denied' || raw === 'declined') return 'denied';
  return 'not_answered';
};

/**
 * Check if user has given consent
 */
export const hasUserConsented = (): boolean => {
  return getConsentState() === 'accepted';
};

/**
 * Check if user explicitly declined consent
 */
export const hasUserDeclined = (): boolean => {
  return getConsentState() === 'denied';
};

/**
 * Accept all cookies with default preferences
 */
export const acceptAllCookies = (): void => {
  saveCookiePreferences({
    analytics: true,
    marketing: true,
    functional: true,
  });
};

/**
 * Accept only essential cookies
 */
export const acceptEssentialOnly = (): void => {
  saveCookiePreferences({
    analytics: false,
    marketing: false,
    functional: false,
  });
};

/**
 * Explicitly decline non-essential cookies and mark consent as denied
 */
export const declineAllCookies = (): void => {
  if (!isClient()) return;

  const newPreferences: CookiePreferences = {
    ...defaultPreferences,
    essential: true,
    analytics: false,
    marketing: false,
    functional: false,
    consentDate: new Date().toISOString(),
    version: COOKIE_VERSION,
  };

  try {
    localStorage.setItem(COOKIE_PREFERENCES_KEY, JSON.stringify(newPreferences));
    localStorage.setItem(COOKIE_CONSENT_KEY, 'denied');

    // Trigger custom event for other components to listen to
    window.dispatchEvent(
      new CustomEvent('cookiePreferencesChanged', {
        detail: newPreferences,
      }),
    );
  } catch (error) {
    console.error('Failed to decline cookie preferences:', error);
  }
};

/**
 * Reset cookie preferences to defaults
 */
export const resetCookiePreferences = (): void => {
  if (!isClient()) return;

  try {
    localStorage.removeItem(COOKIE_PREFERENCES_KEY);
    localStorage.removeItem(COOKIE_CONSENT_KEY);

    // Trigger custom event
    window.dispatchEvent(
      new CustomEvent('cookiePreferencesChanged', {
        detail: defaultPreferences,
      }),
    );
  } catch (error) {
    console.error('Failed to reset cookie preferences:', error);
  }
};

/**
 * Check if a specific cookie category is enabled
 */
export const isCookieCategoryEnabled = (category: CookieCategory): boolean => {
  if (!hasUserConsented()) return category === 'essential';

  const preferences = getCookiePreferences();
  return preferences[category];
};

/**
 * Get analytics scripts based on cookie preferences
 */
export const getEnabledAnalyticsScripts = (): string[] => {
  const scripts: string[] = [];

  if (isCookieCategoryEnabled('analytics')) {
    // Add your analytics scripts here
    scripts.push('google-analytics', 'mixpanel');
  }

  if (isCookieCategoryEnabled('marketing')) {
    // Add your marketing scripts here
    scripts.push('facebook-pixel', 'google-ads');
  }

  return scripts;
};

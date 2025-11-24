'use client';

import { useCallback, useEffect, useState } from 'react';
import { type CookieCategory, type CookiePreferences, acceptAllCookies, acceptEssentialOnly, getCookiePreferences, hasUserConsented, isCookieCategoryEnabled, resetCookiePreferences, saveCookiePreferences, hasUserDeclined, declineAllCookies, type ConsentState, getConsentState } from '@/lib/cookies';

export interface UseCookiesReturn {
  preferences: CookiePreferences;
  hasConsented: boolean;
  isLoading: boolean;
  updatePreference: (category: CookieCategory, enabled: boolean) => void;
  acceptAll: () => void;
  acceptEssential: () => void;
  reset: () => void;
  savePreferences: (prefs: Partial<CookiePreferences>) => void;
  isCategoryEnabled: (category: CookieCategory) => boolean;
  hasDeclined: boolean;
  decline: () => void;
  consentState: ConsentState;
}

/**
 * Custom hook for managing cookie preferences
 * Handles client-side state management and localStorage operations
 */
export const useCookies = (): UseCookiesReturn => {
  const [preferences, setPreferences] = useState<CookiePreferences>(getCookiePreferences());
  const [hasConsented, setHasConsented] = useState<boolean>(false);
  const [hasDeclined, setHasDeclined] = useState<boolean>(false);
  const [consentState, setConsentState] = useState<ConsentState>('not_answered');
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Initialize state on client side
  useEffect(() => {
    const initializeState = () => {
      setPreferences(getCookiePreferences());
      setHasConsented(hasUserConsented());
      setHasDeclined(hasUserDeclined());
      setConsentState(getConsentState());
      setIsLoading(false);
    };

    initializeState();

    // Listen for cookie preference changes from other components/tabs
    const handlePreferenceChange = (event: CustomEvent<CookiePreferences>) => {
      setPreferences(event.detail);
      setHasConsented(hasUserConsented());
      setHasDeclined(hasUserDeclined());
      setConsentState(getConsentState());
    };

    // Listen for storage changes (for cross-tab synchronization)
    const handleStorageChange = (event: StorageEvent) => {
      if (event.key === 'game_guild_cookie_consent' || event.key === 'game_guild_cookie_preferences') {
        initializeState();
      }
    };

    window.addEventListener('cookiePreferencesChanged', handlePreferenceChange as EventListener);
    window.addEventListener('storage', handleStorageChange);

    return () => {
      window.removeEventListener('cookiePreferencesChanged', handlePreferenceChange as EventListener);
      window.removeEventListener('storage', handleStorageChange);
    };
  }, []);

  const updatePreference = useCallback((category: CookieCategory, enabled: boolean) => {
    if (category === 'essential') {
      // Essential cookies cannot be disabled
      return;
    }

    setPreferences((prev) => ({
      ...prev,
      [category]: enabled,
    }));
  }, []);

  const acceptAll = useCallback(() => {
    acceptAllCookies();
    setHasConsented(true);
    setHasDeclined(false);
    setConsentState('accepted');
  }, []);

  const acceptEssential = useCallback(() => {
    acceptEssentialOnly();
    setHasConsented(true);
    setHasDeclined(false);
    setConsentState('accepted');
  }, []);

  const decline = useCallback(() => {
    declineAllCookies();
    setHasConsented(false);
    setHasDeclined(true);
    setConsentState('denied');
  }, []);

  const reset = useCallback(() => {
    resetCookiePreferences();
    setHasConsented(false);
    setHasDeclined(false);
    setConsentState('not_answered');
    setPreferences(getCookiePreferences());
  }, []);

  const savePreferences = useCallback(() => {
    saveCookiePreferences(preferences);
    setHasConsented(true);
    setHasDeclined(false);
    setConsentState('accepted');
  }, [preferences]);

  const isCategoryEnabled = useCallback((category: CookieCategory): boolean => {
    return isCookieCategoryEnabled(category);
  }, []);

  return {
    preferences,
    hasConsented,
    isLoading,
    updatePreference,
    acceptAll,
    acceptEssential,
    reset,
    savePreferences,
    isCategoryEnabled,
    hasDeclined,
    decline,
    consentState,
  };
};

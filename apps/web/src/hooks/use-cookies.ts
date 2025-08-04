'use client';

import { useCallback, useEffect, useState } from 'react';
import { type CookieCategory, type CookiePreferences, acceptAllCookies, acceptEssentialOnly, getCookiePreferences, hasUserConsented, isCookieCategoryEnabled, resetCookiePreferences, saveCookiePreferences } from '@/lib/cookies';

export interface UseCookiesReturn {
  preferences: CookiePreferences;
  hasConsented: boolean;
  isLoading: boolean;
  updatePreference: (category: CookieCategory, enabled: boolean) => void;
  acceptAll: () => void;
  acceptEssential: () => void;
  reset: () => void;
  savePreferences: () => void;
  isCategoryEnabled: (category: CookieCategory) => boolean;
}

/**
 * Custom hook for managing cookie preferences
 * Handles client-side state management and localStorage operations
 */
export const useCookies = (): UseCookiesReturn => {
  const [preferences, setPreferences] = useState<CookiePreferences>(getCookiePreferences());
  const [hasConsented, setHasConsented] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Initialize state on client side
  useEffect(() => {
    const initializeState = () => {
      setPreferences(getCookiePreferences());
      setHasConsented(hasUserConsented());
      setIsLoading(false);
    };

    initializeState();

    // Listen for cookie preference changes from other components/tabs
    const handlePreferenceChange = (event: CustomEvent<CookiePreferences>) => {
      setPreferences(event.detail);
      setHasConsented(true);
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
  }, []);

  const acceptEssential = useCallback(() => {
    acceptEssentialOnly();
    setHasConsented(true);
  }, []);

  const reset = useCallback(() => {
    resetCookiePreferences();
    setHasConsented(false);
    setPreferences(getCookiePreferences());
  }, []);

  const savePreferences = useCallback(() => {
    saveCookiePreferences(preferences);
    setHasConsented(true);
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
  };
};

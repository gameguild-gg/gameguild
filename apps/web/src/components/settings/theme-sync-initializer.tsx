'use client';

import { getThemePreferenceAction } from '@/lib/user-settings/actions';
import { useTheme } from 'next-themes';
import * as React from 'react';

/**
 * Roams the theme preference across devices: when the browser has no local
 * next-themes value (first visit on a new device), applies the theme stored
 * in the user's server-side general preferences. A local value always wins
 * — the server copy is synced on every change by the toggle and the
 * appearance settings page.
 */
export function ThemeSyncInitializer() {
  const { setTheme } = useTheme();

  React.useEffect(() => {
    let cancelled = false;

    // next-themes persists under the "theme" localStorage key.
    const hasLocalPreference = window.localStorage.getItem('theme') !== null;
    if (hasLocalPreference) return;

    void getThemePreferenceAction().then((result) => {
      if (cancelled || !result.success || !result.data) return;
      setTheme(result.data);
    });

    return () => {
      cancelled = true;
    };
  }, [setTheme]);

  return null;
}

'use client';

import { getAccessibilityPreferenceAction } from '@/lib/user-settings/actions';
import type { AccessibilityPreferenceData } from '@/lib/user-settings/preferences-mappers';
import * as React from 'react';

export function applyAccessibilityPreferences(preferences: AccessibilityPreferenceData): void {
  const root = document.documentElement;

  root.dataset.accessibilityHighContrast = String(preferences.highContrast);
  root.dataset.accessibilityReducedMotion = String(preferences.reducedMotion);
  root.dataset.accessibilityLargeText = String(preferences.largeText);
  root.style.fontSize = `${preferences.largeText ? Math.max(preferences.fontSize, 18) : preferences.fontSize}px`;
}

export function AccessibilitySyncInitializer(): null {
  React.useEffect(() => {
    let cancelled = false;

    void getAccessibilityPreferenceAction().then((result) => {
      if (!cancelled && result.success && result.data) {
        applyAccessibilityPreferences(result.data);
      }
    });

    return () => {
      cancelled = true;
    };
  }, []);

  return null;
}

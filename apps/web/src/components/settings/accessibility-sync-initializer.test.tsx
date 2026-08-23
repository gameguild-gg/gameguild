import { afterEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/user-settings/actions', () => ({
  getAccessibilityPreferenceAction: vi.fn(),
}));

import { applyAccessibilityPreferences } from './accessibility-sync-initializer';

describe('applyAccessibilityPreferences', () => {
  afterEach(() => {
    const root = document.documentElement;
    delete root.dataset.accessibilityHighContrast;
    delete root.dataset.accessibilityReducedMotion;
    delete root.dataset.accessibilityLargeText;
    root.style.removeProperty('font-size');
  });

  it('applies contrast, motion, and enlarged text to the document root', () => {
    applyAccessibilityPreferences({
      highContrast: true,
      largeText: true,
      reducedMotion: true,
      fontSize: 14,
    });

    const root = document.documentElement;
    expect(root.dataset.accessibilityHighContrast).toBe('true');
    expect(root.dataset.accessibilityReducedMotion).toBe('true');
    expect(root.dataset.accessibilityLargeText).toBe('true');
    expect(root.style.fontSize).toBe('18px');
  });
});

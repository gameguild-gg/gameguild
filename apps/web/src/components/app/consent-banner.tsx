'use client';

import { isDoNotTrackEnabled, readConsent, saveConsent } from '@/lib/consent/store';
import type { ConsentState } from '@/lib/consent/state';
import { useEffect, useState } from 'react';

export interface ConsentBannerProps {
  /** Server-side privacy preference for signed-in users (analyticsCookies). */
  readonly serverAnalyticsConsent?: boolean;
}

type ConsentChoice = { analytics: boolean; marketing: boolean };

/**
 * Consent coordinator (issue #1206).
 *
 * - Signed-in users: the server privacy preference
 *   (`privacyPreferences.AnalyticsCookies`) stays authoritative — it is
 *   synced into the client store on load (server sync is read-only; the
 *   settings page remains the mutation surface, via
 *   `updatePrivacyPreferenceAction`).
 * - Anonymous visitors with no persisted choice get the granular banner.
 *   Do Not Track suppresses the banner (tracking stays denied).
 */
export function ConsentBanner({ serverAnalyticsConsent }: ConsentBannerProps) {
  const [visible, setVisible] = useState(false);
  const [choice, setChoice] = useState<ConsentChoice>({ analytics: false, marketing: false });

  useEffect(() => {
    const existing = readConsent();
    if (existing) {
      setVisible(false);
      return;
    }

    if (typeof serverAnalyticsConsent === 'boolean') {
      // Signed-in: server preference wins; record it locally so loaders and
      // other tabs pick it up without re-showing the banner.
      saveConsent({ analytics: serverAnalyticsConsent, marketing: false });
      setVisible(false);
      return;
    }

    if (isDoNotTrackEnabled()) {
      setVisible(false);
      return;
    }

    setVisible(true);
  }, [serverAnalyticsConsent]);

  if (!visible) return null;

  function confirm(next: ConsentChoice) {
    saveConsent(next);
    setVisible(false);
  }

  return (
    <div
      role="dialog"
      aria-label="Cookie consent"
      className="fixed inset-x-0 bottom-0 z-50 flex justify-center p-4"
    >
      <div className="w-full max-w-xl rounded-lg border bg-background p-4 shadow-lg">
        <p className="text-sm font-medium">Cookies &amp; analytics</p>
        <p className="mt-1 text-xs text-muted-foreground">
          We use optional cookies to understand site usage (analytics) and to measure campaigns
          (marketing). Everything stays off until you opt in.
        </p>
        <div className="mt-3 space-y-2">
          <label className="flex items-center gap-2 text-xs">
            <input
              type="checkbox"
              checked={choice.analytics}
              onChange={(event) => setChoice((c) => ({ ...c, analytics: event.target.checked }))}
            />
            Analytics
          </label>
          <label className="flex items-center gap-2 text-xs">
            <input
              type="checkbox"
              checked={choice.marketing}
              onChange={(event) => setChoice((c) => ({ ...c, marketing: event.target.checked }))}
            />
            Marketing
          </label>
        </div>
        <div className="mt-3 flex justify-end gap-2">
          <button
            type="button"
            className="rounded-md border px-3 py-1.5 text-xs"
            onClick={() => confirm({ analytics: false, marketing: false })}
          >
            Reject all
          </button>
          <button
            type="button"
            className="rounded-md border px-3 py-1.5 text-xs"
            onClick={() => confirm(choice)}
          >
            Save choices
          </button>
          <button
            type="button"
            className="rounded-md bg-primary px-3 py-1.5 text-xs text-primary-foreground"
            onClick={() => confirm({ analytics: true, marketing: true })}
          >
            Accept all
          </button>
        </div>
      </div>
    </div>
  );
}

export type { ConsentState };

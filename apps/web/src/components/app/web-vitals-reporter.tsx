'use client';

/**
 * Consent-gated Web Vitals + page-load timing reporter (issue #1210).
 *
 * Mounted from `AnalyticsConsent`. Collects LCP/INP/CLS/TTFB/FCP via the
 * `web-vitals` library and a Navigation Timing 2 `page_load_timing` event
 * (once per document), and forwards both to GA4 (through the
 * consent-mode gtag shim) and Firebase Analytics (env-gated; no-op without
 * `NEXT_PUBLIC_FIREBASE_*`). Zero beacons while analytics consent is denied.
 */

import type { ConsentWindow } from '@/lib/consent/consent-mode';
import { readConsent, subscribeConsent } from '@/lib/consent/store';
import {
  buildPageLoadTimingPayload,
  buildWebVitalsPayload,
  createDispatcher,
  registerWebVitalsCallbacks,
  type FirebaseAnalyticsLoader,
} from '@/lib/analytics/web-vitals';
import { usePathname } from 'next/navigation';
import { useEffect } from 'react';
import { onCLS, onFCP, onINP, onLCP, onTTFB } from 'web-vitals';

const firebaseConfigured = Boolean(
  process.env.NEXT_PUBLIC_FIREBASE_API_KEY?.trim() &&
    process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID?.trim() &&
    process.env.NEXT_PUBLIC_FIREBASE_APP_ID?.trim(),
);

/**
 * Dedupe key for `page_load_timing`: the Navigation Timing entry's
 * `startTime`. Module-level so it survives effect re-runs across SPA route
 * changes within the same document.
 */
let reportedPageLoadStartTime: number | null = null;


const loadFirebaseAnalytics: FirebaseAnalyticsLoader = async () => {
  if (!firebaseConfigured || typeof window === 'undefined') return null;
  const [{ initializeApp, getApps }, { getAnalytics, isSupported, logEvent, setAnalyticsCollectionEnabled }] = await Promise.all([
    import('@firebase/app'),
    import('@firebase/analytics'),
  ]);
  if (!(await isSupported())) return null;
  const app = getApps()[0] ?? initializeApp({
    apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY,
    projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID,
    appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID,
    ...(process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN?.trim()
      ? { authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN }
      : {}),
    ...(process.env.NEXT_PUBLIC_FIREBASE_MEASUREMENT_ID?.trim()
      ? { measurementId: process.env.NEXT_PUBLIC_FIREBASE_MEASUREMENT_ID }
      : {}),
  });
  const analytics = getAnalytics(app);
  const dispatch = (eventName: string, payload: Record<string, unknown>) => {
    logEvent(analytics, eventName as 'page_view', payload);
  };
  return {
    logEvent: dispatch,
    setCollectionEnabled: (enabled: boolean) => {
      // SDK-managed automatic collection runs independently of the custom
      // event dispatcher; withdrawal must disable it too.
      setAnalyticsCollectionEnabled(analytics, enabled);
    },
  };
};

export function WebVitalsReporter() {
  const pathname = usePathname();

  useEffect(() => {
    const w = window as ConsentWindow;
    const dispatcher = createDispatcher(readConsent, loadFirebaseAnalytics);

    const dispatchWebVital = (payload: Record<string, string | number>) => {
      dispatcher.sendGa4(w, 'web_vitals', payload);
      void dispatcher.sendFirebase('web_vitals', payload);
    };

    const stopVitals = registerWebVitalsCallbacks(
      { onLCP, onINP, onCLS, onTTFB, onFCP },
      (metric) => {
        dispatchWebVital(
          buildWebVitalsPayload(metric, pathname) as unknown as Record<string, string | number>,
        );
      },
    );

    // The document's Navigation Timing entry describes the initial load only;
    // reporting it again on every SPA route change would misattribute the
    // same numbers to every page. Fire once per document, keyed by the
    // navigation entry's startTime (effect re-runs on route change see the
    // same entry and must skip).
    const reportPageLoad = () => {
      try {
        const [entry] = performance.getEntriesByType('navigation');
        if (!(entry instanceof PerformanceNavigationTiming)) return;
        if (entry.startTime === reportedPageLoadStartTime) return;
        reportedPageLoadStartTime = entry.startTime;
        const payload = buildPageLoadTimingPayload(entry, pathname) as unknown as Record<string, string | number>;
        dispatcher.sendGa4(w, 'page_load_timing', payload);
        void dispatcher.sendFirebase('page_load_timing', payload);
      } catch {
        // Fail closed: missing Navigation Timing 2 → no-op.
      }
    };

    if (document.readyState === 'complete') {
      reportPageLoad();
    } else {
      window.addEventListener('load', reportPageLoad, { once: true });
    }

    // Consent changes: toggle SDK-managed Firebase collection so withdrawal
    // stops automatic collection immediately, not just custom events.
    const unsubscribe = subscribeConsent((state) => {
      const analyticsGranted = state?.analytics === true;
      void dispatcher.setFirebaseCollection(analyticsGranted);
    });

    return () => {
      stopVitals();
      unsubscribe();
      window.removeEventListener('load', reportPageLoad);
    };
  }, [pathname]);

  return null;
}

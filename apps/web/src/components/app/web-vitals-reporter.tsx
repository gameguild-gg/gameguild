'use client';

/**
 * Consent-gated Web Vitals + page-load timing reporter (issue #1210).
 *
 * Mounted from `AnalyticsConsent`. Collects LCP/INP/CLS/TTFB/FCP via the
 * `web-vitals` library and a Navigation Timing 2 `page_load_timing` event
 * (re-sent on SPA route changes), and forwards both to GA4 (through the
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

const loadFirebaseAnalytics: FirebaseAnalyticsLoader = async () => {
  if (!firebaseConfigured || typeof window === 'undefined') return null;
  const [{ initializeApp, getApps }, { getAnalytics, isSupported }] = await Promise.all([
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
  return getAnalytics(app) as unknown as {
    logEvent: (eventName: string, payload: Record<string, unknown>) => void;
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

    const reportPageLoad = () => {
      try {
        const [entry] = performance.getEntriesByType('navigation');
        if (!(entry instanceof PerformanceNavigationTiming)) return;
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

    // Re-subscribe so consent changes are picked up on the next event.
    const unsubscribe = subscribeConsent(() => {});

    return () => {
      stopVitals();
      unsubscribe();
      window.removeEventListener('load', reportPageLoad);
    };
  }, [pathname]);

  return null;
}

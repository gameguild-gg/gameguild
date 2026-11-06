'use client';

/**
 * Consent-gated Web Vitals + page-load timing + SPA page_view reporter
 * (issues #1210, #1221).
 *
 * Mounted from `AnalyticsConsent`. Collects LCP/INP/CLS/TTFB/FCP via the
 * `web-vitals` library and a Navigation Timing 2 `page_load_timing` event
 * (re-sent on SPA route changes), dispatches an explicit consent-gated
 * `page_view` on every SPA route change (GA4 gtag.js transport is
 * configured with `send_page_view: false`, so this is the single source —
 * issue #1221), and forwards everything to GA4 (through the consent-mode
 * gtag shim) and Firebase Analytics (env-gated; no-op without
 * `NEXT_PUBLIC_FIREBASE_*`). Zero beacons while analytics consent is denied.
 *
 * Every payload carries `event_version` from the versioned event contract
 * (`@/lib/analytics/events`).
 */

import type { ConsentWindow } from '@/lib/consent/consent-mode';
import { readConsent, subscribeConsent } from '@/lib/consent/store';
import {
  PAGE_LOAD_TIMING_EVENT,
  PAGE_VIEW_EVENT,
  WEB_VITALS_EVENT,
  withContractVersion,
} from '@/lib/analytics/events';
import {
  buildPageLoadTimingPayload,
  buildWebVitalsPayload,
  createDispatcher,
  type FirebaseAnalyticsLoader,
} from '@/lib/analytics/web-vitals';
import type { Metric } from 'web-vitals';
import { usePathname } from 'next/navigation';
import { useEffect, useRef } from 'react';
import { onCLS, onFCP, onINP, onLCP, onTTFB } from 'web-vitals';

const firebaseConfigured = Boolean(
  process.env.NEXT_PUBLIC_FIREBASE_API_KEY?.trim() &&
    process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID?.trim() &&
    process.env.NEXT_PUBLIC_FIREBASE_APP_ID?.trim(),
);

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
    ...(process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET?.trim()
      ? { storageBucket: process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET }
      : {}),
    ...(process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID?.trim()
      ? { messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID }
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
  // Dedupe keys stable across pathname-effect re-runs: a metric must never
  // re-fire for the same navigationId even when the effect tears down and
  // re-registers its callbacks.
  const reportedRef = useRef<Set<string>>(new Set());
  // Last path a page_view was emitted for — keeps the initial mount from
  // double-firing with the first pathname effect run (and idempotent across
  // effect re-runs) while still re-firing when the user returns to a path.
  const lastPageViewPathRef = useRef<string | null>(null);
  const pageLoadSentRef = useRef(false);
  // Previous effective analytics grant, so a same-page grant transition
  // (denied → granted) can emit the entry page's page_view exactly once.
  const wasAnalyticsGrantedRef = useRef(false);

  useEffect(() => {
    const w = window as ConsentWindow;
    const dispatcher = createDispatcher(readConsent, loadFirebaseAnalytics);

    const dispatch = (eventName: string, payload: Record<string, string | number>) => {
      const stamped = withContractVersion(payload);
      dispatcher.sendGa4(w, eventName, stamped);
      void dispatcher.sendFirebase(eventName, stamped);
    };

    let vitalsStopped = false;
    const handleMetric = (metric: Metric) => {
      if (vitalsStopped) return;
      const key = `${metric.name}:${metric.id ?? metric.navigationId ?? 'page'}`;
      if (reportedRef.current.has(key)) return;
      reportedRef.current.add(key);
      dispatch(WEB_VITALS_EVENT, buildWebVitalsPayload(metric, pathname) as unknown as Record<string, string | number>);
    };
    onLCP(handleMetric);
    onINP(handleMetric);
    onCLS(handleMetric);
    onTTFB(handleMetric);
    onFCP(handleMetric);
    const stopVitals = () => {
      vitalsStopped = true;
    };

    // The document's Navigation Timing entry describes the initial load only;
    // reporting it again on every SPA route change would misattribute the
    // same numbers to every page. Fire once per document (survives effect
    // re-runs via the ref).
    const reportPageLoad = () => {
      try {
        if (pageLoadSentRef.current) return;
        const [entry] = performance.getEntriesByType('navigation');
        if (!(entry instanceof PerformanceNavigationTiming)) return;
        pageLoadSentRef.current = true;
        dispatch(
          PAGE_LOAD_TIMING_EVENT,
          buildPageLoadTimingPayload(entry, pathname) as unknown as Record<string, string | number>,
        );
      } catch {
        // Fail closed: missing Navigation Timing 2 → no-op.
      }
    };

    if (document.readyState === 'complete') {
      reportPageLoad();
    } else {
      window.addEventListener('load', reportPageLoad, { once: true });
    }

    // SPA page_view (issue #1221): gtag.js is configured with
    // `send_page_view: false`, so this explicit emission owns the event.
    // One initial page_view on mount plus one per pathname change; the
    // per-path set dedupes so the mount run and the first pathname run
    // emit exactly one page_view for the entry path.
    const sendPageView = () => {
      if (lastPageViewPathRef.current === pathname) return;
      lastPageViewPathRef.current = pathname;
      dispatch(PAGE_VIEW_EVENT, { page_path: pathname });
    };
    const initialGrant = readConsent()?.analytics === true;
    if (initialGrant) {
      sendPageView();
    } else {
      // Path not yet "seen" while consent is denied, so the first grant
      // below emits the entry page's page_view instead of skipping it.
      lastPageViewPathRef.current = null;
    }
    wasAnalyticsGrantedRef.current = initialGrant;

    const unsubscribe = subscribeConsent((state) => {
      const analyticsGranted = state?.analytics === true;
      void dispatcher.setFirebaseCollection(analyticsGranted);
      const wasGranted = wasAnalyticsGrantedRef.current;
      wasAnalyticsGrantedRef.current = analyticsGranted;
      if (analyticsGranted && !wasGranted) {
        lastPageViewPathRef.current = null;
        sendPageView();
      }
    });

    return () => {
      stopVitals();
      unsubscribe();
      window.removeEventListener('load', reportPageLoad);
    };
  }, [pathname]);

  return null;
}

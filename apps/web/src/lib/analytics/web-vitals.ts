/**
 * Web Vitals + page-load timing reporters (issue #1210).
 *
 * Collects LCP / INP / CLS / TTFB / FCP via the `web-vitals` library and a
 * Navigation Timing 2 `page_load_timing` event, and forwards them to GA4
 * (`gtag('event', …)` through the consent-mode shim) and Firebase Analytics
 * (`logEvent`). Both destinations are strictly gated on the #1206 consent
 * store: nothing is dispatched while analytics consent is denied or absent —
 * zero beacons, zero logEvent calls.
 *
 * Every payload carries `{ metric_name, metric_value, metric_rating,
 * metric_id, page_path }` and each metric fires at most once per page view
 * (per navigation id). Destination initialization failures fail closed to a
 * silent no-op.
 */

import { type ConsentWindow } from '@/lib/consent/consent-mode';
import type { ConsentState } from '@/lib/consent/store';

import type { Metric } from 'web-vitals';

export interface WebVitalsPayload {
  readonly metric_name: string;
  readonly metric_value: number;
  readonly metric_rating: string;
  readonly metric_id: string;
  readonly page_path: string;
}

export interface PageLoadTimingPayload {
  readonly metric_name: 'page_load_timing';
  readonly response_start_ms: number;
  readonly dom_content_loaded_ms: number;
  readonly load_event_ms: number;
  readonly transfer_size: number;
  readonly page_path: string;
}

/** Rounds CLS-like unitless scores into GA-compatible integers, keeps ms values. */
function roundMetricValue(name: string, value: number): number {
  return name === 'CLS' ? Math.round(value * 1000) : Math.round(value);
}

export function buildWebVitalsPayload(metric: Metric, pagePath: string): WebVitalsPayload {
  return {
    metric_name: metric.name,
    metric_value: roundMetricValue(metric.name, metric.value),
    metric_rating: metric.rating ?? 'unknown',
    metric_id: metric.id,
    page_path: pagePath,
  };
}

export function buildPageLoadTimingPayload(entry: PerformanceNavigationTiming, pagePath: string): PageLoadTimingPayload {
  return {
    metric_name: 'page_load_timing',
    response_start_ms: Math.round(entry.responseStart),
    dom_content_loaded_ms: Math.round(entry.domContentLoadedEventEnd),
    load_event_ms: Math.round(entry.loadEventEnd),
    transfer_size: entry.transferSize ?? 0,
    page_path: pagePath,
  };
}

/** GA4 destination: pushes through the consent-mode gtag/dataLayer shim. */
export function sendToGa4(w: ConsentWindow, eventName: string, payload: Record<string, string | number>): void {
  try {
    w.dataLayer = w.dataLayer || [];
    w.gtag ??= (...args: unknown[]) => {
      w.dataLayer!.push(args);
    };
    w.gtag('event', eventName, payload);
  } catch {
    // Fail closed: no transport → no-op.
  }
}

export type FirebaseAnalyticsLike = {
  logEvent(eventName: string, payload: Record<string, unknown>): void;
};

export type FirebaseAnalyticsLoader = () => Promise<FirebaseAnalyticsLike | null>;

/**
 * Wraps a Firebase Analytics instance loader. Returns a destination that
 * resolves the instance at most once (memoized); any init failure resolves
 * to null forever — silent no-op.
 */
export function createFirebaseDestination(loader: FirebaseAnalyticsLoader): FirebaseAnalyticsLoader {
  let cached: FirebaseAnalyticsLike | null | undefined;
  return async () => {
    if (cached !== undefined) return cached;
    try {
      cached = await loader();
    } catch {
      cached = null;
    }
    return cached;
  };
}

export interface Destination {
  sendGa4(w: ConsentWindow, eventName: string, payload: Record<string, string | number>): void;
  sendFirebase(eventName: string, payload: Record<string, string | number>): Promise<void>;
}

/**
 * Consent-gated dispatcher: forwards to every destination only when the
 * current effective consent state grants analytics. Denied or absent state
 * dispatches nothing.
 */
export function createDispatcher(
  getCurrentConsent: () => ConsentState | null,
  firebaseLoader: FirebaseAnalyticsLoader,
): Destination {
  const firebaseDestination = createFirebaseDestination(firebaseLoader);
  return {
    sendGa4(w, eventName, payload) {
      const state = getCurrentConsent();
      if (!state?.analytics) return;
      sendToGa4(w, eventName, payload);
    },
    async sendFirebase(eventName, payload) {
      if (!getCurrentConsent()?.analytics) return;
      const analytics = await firebaseDestination();
      // Consent may have been revoked while the loader was in flight —
      // re-check before dispatching.
      if (!getCurrentConsent()?.analytics) return;
      if (!analytics) return;
      try {
        analytics.logEvent(eventName, payload);
      } catch {
        // Fail closed: destination error → silent no-op.
      }
    },
  };
}

/**
 * Registers all five web-vitals metric callbacks. Each metric is fired at
 * most once per page view (keyed by metric name + navigation id, reset via
 * `resetMetricTracking`). Returns a cleanup that stops further reporting.
 */
export function registerWebVitalsCallbacks(
  hooks: {
    onLCP(cb: (metric: Metric) => void): void;
    onINP(cb: (metric: Metric) => void): void;
    onCLS(cb: (metric: Metric) => void): void;
    onTTFB(cb: (metric: Metric) => void): void;
    onFCP(cb: (metric: Metric) => void): void;
  },
  onMetric: (metric: Metric) => void,
): () => void {
  let stopped = false;
  const reported = new Set<string>();

  const handle = (metric: Metric) => {
    if (stopped) return;
    const key = `${metric.name}:${metric.navigationId ?? 'page'}`;
    if (reported.has(key)) return;
    reported.add(key);
    onMetric(metric);
  };

  hooks.onLCP(handle);
  hooks.onINP(handle);
  hooks.onCLS(handle);
  hooks.onTTFB(handle);
  hooks.onFCP(handle);

  return () => {
    stopped = true;
  };
}

/**
 * Versioned analytics event contract (issues #1210, #1221).
 *
 * Single source of truth for event names, payload types, and the contract
 * version stamped on every dispatched event (`event_version`). GA4 param
 * names stay ≤40 chars, alphanumeric + underscore. Firebase Analytics
 * limits: event name ≤40 chars, ≤25 params/event, values ≤100 chars.
 *
 * Versioning policy: bump `ANALYTICS_EVENT_CONTRACT_VERSION` when adding,
 * renaming, or removing params; additive-only within a version. See
 * `docs/frontend/analytics-event-contract.md`.
 */

export const ANALYTICS_EVENT_CONTRACT_VERSION = 1;

/** GA4/Firebase param carrying the contract version on every event. */
export const EVENT_VERSION_PARAM = 'event_version';

/** Core Web Vitals event (LCP/INP/CLS/TTFB/FCP) — issue #1210. */
export const WEB_VITALS_EVENT = 'web_vitals';

/** Navigation Timing 2 page-load event — issue #1210. */
export const PAGE_LOAD_TIMING_EVENT = 'page_load_timing';

/**
 * SPA page view event — issue #1221. GA4 gtag.js owns the transport
 * (`send_page_view: false` disables its built-in page_view so this explicit
 * emission is the single source).
 */
export const PAGE_VIEW_EVENT = 'page_view';

import type { PageLoadTimingPayload, WebVitalsPayload } from './web-vitals';

/** `web_vitals` payload plus the contract version param. */
export type WebVitalsEventPayload = WebVitalsPayload & {
  readonly [EVENT_VERSION_PARAM]: number;
};

/** `page_load_timing` payload plus the contract version param. */
export type PageLoadTimingEventPayload = PageLoadTimingPayload & {
  readonly [EVENT_VERSION_PARAM]: number;
};

/** `page_view` payload — issue #1221 SPA navigation tracking. */
export interface PageViewEventPayload {
  readonly page_path: string;
  readonly [EVENT_VERSION_PARAM]: number;
}

/** Stamps the contract version onto a payload copy. */
export function withContractVersion<T extends object>(payload: T): T & { readonly [EVENT_VERSION_PARAM]: number } {
  return { ...payload, [EVENT_VERSION_PARAM]: ANALYTICS_EVENT_CONTRACT_VERSION };
}

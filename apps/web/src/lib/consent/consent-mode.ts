/**
 * Google Consent Mode v2 helpers (issue #1206).
 *
 * The denied-by-default `gtag('consent', 'default', …)` call must run BEFORE
 * any tag loads — it is what suppresses cookieless pings and cookie writes
 * while consent is denied. The gtag shim here is only ever created to carry
 * consent commands; transport scripts (gtag.js / gtm.js) are injected only
 * once at least one purpose is granted.
 */

import { toConsentMode } from './state';
import type { ConsentState } from './state';

export type ConsentWindow = Window & {
  dataLayer?: unknown[];
  gtag?: (...args: unknown[]) => void;
};

/**
 * Ensures `window.dataLayer` + the `window.gtag` shim exist and pushes the
 * denied-by-default consent command (idempotent — guarded by a dataLayer
 * marker so repeated mounts never re-issue a default over a later grant).
 */
export function ensureConsentDefaults(w: ConsentWindow): void {
  w.dataLayer = w.dataLayer || [];
  w.gtag ??= (...args: unknown[]) => {
    w.dataLayer!.push(args);
  };

  const alreadySet = (w.dataLayer as Array<{ 'gg:consent-default'?: boolean } | unknown[]>).some(
    (entry) =>
      typeof entry === 'object' &&
      entry !== null &&
      !Array.isArray(entry) &&
      (entry as { 'gg:consent-default'?: boolean })['gg:consent-default'] === true,
  );
  if (alreadySet) return;

  w.dataLayer.push({ 'gg:consent-default': true });
  w.gtag('consent', 'default', {
    ad_storage: 'denied',
    analytics_storage: 'denied',
    ad_user_data: 'denied',
    ad_personalization: 'denied',
    wait_for_update: 500,
  });
}

/** Pushes a consent update mapped from the granular state. */
export function pushConsentUpdate(w: ConsentWindow, state: ConsentState): void {
  ensureConsentDefaults(w);
  w.gtag!('consent', 'update', toConsentMode(state));
}

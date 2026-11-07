'use client';

import { ensureConsentDefaults, pushConsentUpdate, type ConsentWindow } from '@/lib/consent/consent-mode';
import { DENIED_CONSENT, subscribeConsent } from '@/lib/consent/store';
import Script from 'next/script';
import { useEffect, useState } from 'react';

export interface GoogleAnalyticsProps {
  /**
   * GA4 measurement id (for example `G-XXXXXXX`). When missing or blank the
   * component is a complete no-op: it renders nothing, never touches
   * `window.dataLayer`/`window.gtag`, and injects no scripts. This keeps
   * development and undeployed environments free of any GA signature.
   */
  readonly measurementId?: string;

  /** Test seam for NEXT_PUBLIC_FIREBASE_MEASUREMENT_ID (single transport rule). */
  readonly __firebaseMeasurementIdForTest?: string;
}

/**
 * Google Consent Mode v2 gated Google Analytics 4 (gtag.js) loader.
 *
 * Always (when configured) installs the denied-by-default consent command
 * BEFORE anything else — that is what suppresses cookieless pings and cookie
 * writes while consent is denied. The gtag.js transport script, the `js`
 * bootstrap, and the `config` call only happen once the client consent store
 * grants the analytics purpose; a later revocation pushes a `denied` consent
 * update, so no further GA hits carry cookies or identifiers.
 */
export function GoogleAnalytics({ measurementId: rawMeasurementId, __firebaseMeasurementIdForTest }: GoogleAnalyticsProps) {
  const measurementId = rawMeasurementId?.trim() || undefined;
  const [transportReady, setTransportReady] = useState(false);

  useEffect(() => {
    if (!measurementId) return;

    const w = window as ConsentWindow;
    ensureConsentDefaults(w);

    let configured = false;
    return subscribeConsent((state) => {
      // null (missing/invalid/cleared record) is a denial — push it, never
      // leave a previously granted consent state active.
      const effective = state ?? DENIED_CONSENT;
      pushConsentUpdate(w, effective);
      if (effective.analytics && !configured) {
        configured = true;
        w.gtag!('js', new Date());
        // send_page_view:false — the explicit consent-gated page_view in
        // web-vitals-reporter owns SPA page views (issue #1221); gtag.js's
        // built-in would double-fire.
        w.gtag!('config', measurementId, { send_page_view: false });
        setTransportReady(true);
      }
      if (!effective.analytics) {
        // Consent Mode denial alone does not stop an already-loaded tag from
        // sending cookieless pings; the disable cookie hard-stops collection.
        document.cookie = `ga-disable-${measurementId}=true; path=/; max-age=31536000`;
        setTransportReady(false);
      } else {
        document.cookie = `ga-disable-${measurementId}=; path=/; max-age=0`;
      }
    });
  }, [measurementId]);

  if (!measurementId || !transportReady) return null;

  // Firebase Analytics with the SAME measurement id injects gtag.js itself on
  // getAnalytics() (post-consent). A second gtag.js for the same id races two
  // processors over one dataLayer — the survivor never registers our consent
  // default/update (implicit-only consent, zero /g/collect hits; observed
  // live 2026-11-07). Firebase's script is then the single transport; this
  // component still owns consent commands, js/config bootstrap, and the
  // ga-disable hard stop via the shared dataLayer.
  const firebaseMeasurementId = __firebaseMeasurementIdForTest?.trim() || process.env.NEXT_PUBLIC_FIREBASE_MEASUREMENT_ID?.trim();
  if (firebaseMeasurementId && firebaseMeasurementId === measurementId) return null;

  return <Script id="gg-ga4-loader" src={`https://www.googletagmanager.com/gtag/js?id=${measurementId}`} strategy="afterInteractive" />;
}

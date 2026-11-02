'use client';

import { ensureConsentDefaults, pushConsentUpdate, type ConsentWindow } from '@/lib/consent/consent-mode';
import { subscribeConsent } from '@/lib/consent/store';
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
export function GoogleAnalytics({ measurementId: rawMeasurementId }: GoogleAnalyticsProps) {
  const measurementId = rawMeasurementId?.trim() || undefined;
  const [transportReady, setTransportReady] = useState(false);

  useEffect(() => {
    if (!measurementId) return;

    const w = window as ConsentWindow;
    ensureConsentDefaults(w);

    let configured = false;
    return subscribeConsent((state) => {
      if (!state) return;
      pushConsentUpdate(w, state);
      if (state.analytics && !configured) {
        configured = true;
        w.gtag!('js', new Date());
        w.gtag!('config', measurementId);
        setTransportReady(true);
      }
    });
  }, [measurementId]);

  if (!measurementId || !transportReady) return null;

  return <Script id="gg-ga4-loader" src={`https://www.googletagmanager.com/gtag/js?id=${measurementId}`} strategy="afterInteractive" />;
}

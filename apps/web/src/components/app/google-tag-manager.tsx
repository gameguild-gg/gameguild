'use client';

import { ensureConsentDefaults, pushConsentUpdate, type ConsentWindow } from '@/lib/consent/consent-mode';
import { subscribeConsent } from '@/lib/consent/store';
import Script from 'next/script';
import { useEffect, useState } from 'react';

/**
 * Window shape expected by the standard Google Tag Manager container snippet.
 */
declare global {
  interface Window {
    dataLayer?: Record<string, unknown>[];
  }
}

export interface GoogleTagManagerProps {
  /**
   * GTM container id (for example `GTM-XXXXXXX`). When missing or blank the
   * component is a complete no-op: it renders nothing, never touches
   * `window.dataLayer`, and injects no scripts. This keeps development and
   * undeployed environments free of any GTM signature.
   */
  readonly containerId?: string;
}

/**
 * Google Consent Mode v2 gated Google Tag Manager container loader.
 *
 * Installs the denied-by-default consent command before any GTM code runs.
 * The container transport (`gtm.js` + `<noscript>` iframe) only mounts once
 * a granted consent purpose exists (analytics or marketing); a revocation
 * pushes a `denied` consent update so the container stops processing.
 */
export function GoogleTagManager({ containerId: rawContainerId }: GoogleTagManagerProps) {
  const containerId = rawContainerId?.trim() || undefined;
  const [transportReady, setTransportReady] = useState(false);

  useEffect(() => {
    if (!containerId) return;

    const w = window as ConsentWindow;
    ensureConsentDefaults(w);

    let started = false;
    return subscribeConsent((state) => {
      if (!state) return;
      pushConsentUpdate(w, state);
      if ((state.analytics || state.marketing) && !started) {
        started = true;
        w.dataLayer!.push({ 'gtm.start': new Date().getTime(), event: 'gtm.js' });
        setTransportReady(true);
      }
    });
  }, [containerId]);

  if (!containerId || !transportReady) return null;

  return (
    <>
      <Script
        id="gg-gtm-loader"
        src={`https://www.googletagmanager.com/gtm.js?id=${containerId}`}
        strategy="afterInteractive"
      />
      <noscript>
        <iframe
          src={`https://www.googletagmanager.com/ns.html?id=${containerId}`}
          height="0"
          width="0"
          style={{ display: 'none', visibility: 'hidden' }}
        />
      </noscript>
    </>
  );
}

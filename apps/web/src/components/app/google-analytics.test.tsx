import '@testing-library/jest-dom/vitest';
import { act, render } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { CONSENT_STATE_VERSION, CONSENT_STORAGE_KEY } from '@/lib/consent/state';

const scriptRenders: Array<Record<string, unknown>> = [];

vi.mock('next/script', () => ({
  default: (props: Record<string, unknown>) => {
    scriptRenders.push(props);
    return null;
  },
}));

import { GoogleAnalytics } from './google-analytics';

type AnalyticsWindow = Window & {
  dataLayer?: unknown[];
  gtag?: (...args: unknown[]) => void;
};

function analyticsWindow(): AnalyticsWindow {
  return window as AnalyticsWindow;
}

function persistConsent(analytics: boolean, marketing = false): void {
  window.localStorage.setItem(
    CONSENT_STORAGE_KEY,
    JSON.stringify({ version: CONSENT_STATE_VERSION, analytics, marketing, updatedAt: '2026-01-01T00:00:00.000Z' }),
  );
}

describe('GoogleAnalytics (consent mode v2)', () => {
  beforeEach(() => {
    scriptRenders.length = 0;
    window.localStorage.clear();
    delete analyticsWindow().dataLayer;
    delete analyticsWindow().gtag;
    Object.defineProperty(navigator, 'doNotTrack', { value: undefined, configurable: true });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('is a no-op without a measurement id', () => {
    const { container } = render(<GoogleAnalytics />);

    expect(container).toBeEmptyDOMElement();
    expect(scriptRenders).toHaveLength(0);
    expect(analyticsWindow().dataLayer).toBeUndefined();
  });

  it('treats a blank measurement id as unset', () => {
    const { container } = render(<GoogleAnalytics measurementId="   " />);

    expect(container).toBeEmptyDOMElement();
    expect(scriptRenders).toHaveLength(0);
  });

  it('injects no script and issues no events while consent is denied or absent', () => {
    render(<GoogleAnalytics measurementId="G-TESTID" />);

    // Consent default is installed before anything loads…
    const w = analyticsWindow();
    expect(typeof w.gtag).toBe('function');
    expect(w.dataLayer).toEqual([
      { 'gg:consent-default': true },
      ['consent', 'default', expect.anything()],
      // Absent consent is pushed as an explicit denial (fail closed)…
      ['consent', 'update', expect.anything()],
    ]);
    // …which also sets the ga-disable cookie so an already-loaded tag stops collecting.
    expect(document.cookie).toContain('ga-disable-G-TESTID=true');
    // …but no transport script, no js/config calls.
    expect(scriptRenders).toHaveLength(0);
  });

  it('sets the denied consent default before any tag loads', () => {
    render(<GoogleAnalytics measurementId="G-TESTID" />);

    const w = analyticsWindow();
    expect(w.dataLayer![1]).toEqual([
      'consent',
      'default',
      {
        ad_storage: 'denied',
        analytics_storage: 'denied',
        ad_user_data: 'denied',
        ad_personalization: 'denied',
        wait_for_update: 500,
      },
    ]);
  });

  it('loads gtag.js only after analytics consent is granted, with a granted update first', () => {
    persistConsent(true);
    render(<GoogleAnalytics measurementId="G-TESTID" />);

    const layer = analyticsWindow().dataLayer!;
    expect(scriptRenders).toHaveLength(1);
    expect(scriptRenders[0]).toMatchObject({
      id: 'gg-ga4-loader',
      strategy: 'afterInteractive',
      src: 'https://www.googletagmanager.com/gtag/js?id=G-TESTID',
    });
    expect(layer).toEqual([
      { 'gg:consent-default': true },
      ['consent', 'default', expect.anything()],
      ['consent', 'update', { analytics_storage: 'granted', ad_storage: 'denied', ad_user_data: 'denied', ad_personalization: 'denied' }],
      ['js', expect.any(Date)],
      ['config', 'G-TESTID', { send_page_view: false }],
    ]);
  });

  it('marketing-only consent does not enable analytics storage', () => {
    persistConsent(false, true);
    render(<GoogleAnalytics measurementId="G-TESTID" />);

    const layer = analyticsWindow().dataLayer!;
    expect(scriptRenders).toHaveLength(0); // GA transport needs analytics purpose
    expect(layer.some((e) => Array.isArray(e) && e[0] === 'config')).toBe(false);
  });

  it('pushes a denied update on revocation and does not re-issue the consent default', async () => {
    persistConsent(true);
    render(<GoogleAnalytics measurementId="G-TESTID" />);
    expect(scriptRenders).toHaveLength(1);

    await act(async () => {
      persistConsent(false);
      window.dispatchEvent(new StorageEvent('storage', { key: CONSENT_STORAGE_KEY, newValue: window.localStorage.getItem(CONSENT_STORAGE_KEY) }));
    });

    const layer = analyticsWindow().dataLayer!;
    expect(layer.filter((e) => Array.isArray(e) && e[1] === 'default')).toHaveLength(1);
    const lastUpdate = [...layer].reverse().find((e) => Array.isArray(e) && e[1] === 'update');
    expect(lastUpdate).toEqual([
      'consent',
      'update',
      { analytics_storage: 'denied', ad_storage: 'denied', ad_user_data: 'denied', ad_personalization: 'denied' },
    ]);
  });
});

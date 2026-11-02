import '@testing-library/jest-dom/vitest';
import { render } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { CONSENT_STATE_VERSION, CONSENT_STORAGE_KEY } from '@/lib/consent/state';

const scriptRenders: Array<Record<string, unknown>> = [];

vi.mock('next/script', () => ({
  default: (props: Record<string, unknown>) => {
    scriptRenders.push(props);
    return null;
  },
}));

import { GoogleTagManager } from './google-tag-manager';

type DataLayerWindow = Window & { dataLayer?: Record<string, unknown>[] };

function dataLayer(): Record<string, unknown>[] | undefined {
  return (window as DataLayerWindow).dataLayer;
}

function persistConsent(analytics: boolean, marketing = false): void {
  window.localStorage.setItem(
    CONSENT_STORAGE_KEY,
    JSON.stringify({ version: CONSENT_STATE_VERSION, analytics, marketing, updatedAt: '2026-01-01T00:00:00.000Z' }),
  );
}

describe('GoogleTagManager (consent mode v2)', () => {
  beforeEach(() => {
    scriptRenders.length = 0;
    window.localStorage.clear();
    delete (window as DataLayerWindow).dataLayer;
    Object.defineProperty(navigator, 'doNotTrack', { value: undefined, configurable: true });
  });

  it('is a no-op without a container id', () => {
    const { container } = render(<GoogleTagManager />);

    expect(container).toBeEmptyDOMElement();
    expect(scriptRenders).toHaveLength(0);
    expect(dataLayer()).toBeUndefined();
  });

  it('treats a blank container id as unset', () => {
    const { container } = render(<GoogleTagManager containerId="   " />);

    expect(container).toBeEmptyDOMElement();
    expect(scriptRenders).toHaveLength(0);
  });

  it('stays dormant (no script, no gtm.js event) while consent is denied or absent', () => {
    const { container } = render(<GoogleTagManager containerId="GTM-TESTID" />);

    expect(container).toBeEmptyDOMElement();
    expect(scriptRenders).toHaveLength(0);
    const layer = dataLayer();
    expect(layer).toBeDefined();
    expect(layer!.some((entry) => entry['event'] === 'gtm.js')).toBe(false);
    // Consent default still installed first.
    expect(layer!.some((entry) => entry['gg:consent-default'] === true)).toBe(true);
  });

  it('sets the denied consent default before the container loads', () => {
    persistConsent(true);
    render(<GoogleTagManager containerId="GTM-TESTID" />);

    const layer = dataLayer()!;
    const defaultIndex = layer.findIndex((entry) => Array.isArray(entry) && entry[1] === 'default');
    const startIndex = layer.findIndex((entry) => entry['event'] === 'gtm.js');
    expect(defaultIndex).toBeGreaterThanOrEqual(0);
    expect(startIndex).toBeGreaterThan(defaultIndex);
    expect(layer[defaultIndex]).toEqual([
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

  it('loads the container script and initializes dataLayer once consent is granted', () => {
    persistConsent(true);
    const { container } = render(<GoogleTagManager containerId="GTM-TESTID" />);

    expect(scriptRenders).toHaveLength(1);
    expect(scriptRenders[0]).toMatchObject({
      id: 'gg-gtm-loader',
      strategy: 'afterInteractive',
      src: 'https://www.googletagmanager.com/gtm.js?id=GTM-TESTID',
    });

    const layer = dataLayer()!;
    const startEntry = layer.find((entry) => entry['event'] === 'gtm.js');
    expect(startEntry).toMatchObject({ event: 'gtm.js' });
    expect(startEntry!['gtm.start']).toEqual(expect.any(Number));

    expect(container.querySelector('noscript')).not.toBeNull();
  });

  it('starts the container for marketing-only consent too', () => {
    persistConsent(false, true);
    render(<GoogleTagManager containerId="GTM-TESTID" />);

    expect(scriptRenders).toHaveLength(1);
    const update = dataLayer()!.find((entry) => Array.isArray(entry) && entry[1] === 'update');
    expect(update).toEqual([
      'consent',
      'update',
      { analytics_storage: 'denied', ad_storage: 'granted', ad_user_data: 'granted', ad_personalization: 'granted' },
    ]);
  });

  it('keeps an existing dataLayer and appends to it', () => {
    persistConsent(true);
    (window as DataLayerWindow).dataLayer = [{ event: 'existing' }];

    render(<GoogleTagManager containerId="GTM-TESTID" />);

    const layer = dataLayer()!;
    expect(layer[0]).toMatchObject({ event: 'existing' });
    expect(layer.some((entry) => entry['event'] === 'gtm.js')).toBe(true);
  });
});

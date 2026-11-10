import '@testing-library/jest-dom/vitest';
import { act, render } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { CONSENT_STATE_VERSION, CONSENT_STORAGE_KEY } from '@/lib/consent/state';
import { ANALYTICS_EVENT_CONTRACT_VERSION, EVENT_VERSION_PARAM } from '@/lib/analytics/events';

const metricCallbacks = {} as Record<string, (m: unknown) => void>;

vi.mock('web-vitals', () => ({
  onLCP: (cb: (m: unknown) => void) => { metricCallbacks.LCP = cb; },
  onINP: (cb: (m: unknown) => void) => { metricCallbacks.INP = cb; },
  onCLS: (cb: (m: unknown) => void) => { metricCallbacks.CLS = cb; },
  onTTFB: (cb: (m: unknown) => void) => { metricCallbacks.TTFB = cb; },
  onFCP: (cb: (m: unknown) => void) => { metricCallbacks.FCP = cb; },
}));

vi.mock('next/navigation', () => ({
  usePathname: () => mocks.pathname,
}));

const mocks = vi.hoisted(() => ({
  pathname: '/',
}));

import { WebVitalsReporter } from './web-vitals-reporter';

// gtag.js queue entries are arguments objects (array-like), not arrays.
const isArgs = (e: unknown): e is ArrayLike<unknown> => typeof e === 'object' && e !== null && 'length' in e && 'callee' in e;

const cmd = (e: unknown) => (isArgs(e) ? Array.from(e) : e);


type AnalyticsWindow = Window & {
  dataLayer?: unknown[];
  gtag?: (...args: unknown[]) => void;
};

function w(): AnalyticsWindow {
  return window as AnalyticsWindow;
}

function events(name: string): Array<unknown[]> {
  return (w().dataLayer ?? [])
    .filter((e) => isArgs(e) && e[0] === 'event' && e[1] === name)
    .map(cmd);
}

function allEvents(): Array<unknown[]> {
  return (w().dataLayer ?? []).filter((e) => isArgs(e) && e[0] === 'event').map(cmd);
}

function fakeMetric(overrides: Record<string, unknown> = {}): unknown {
  return {
    name: 'LCP',
    value: 2450.7,
    rating: 'good',
    id: 'v3-abc123',
    delta: 2450.7,
    navigationType: 'navigate',
    navigationId: 'nav-1',
    entries: [],
    ...overrides,
  };
}

function persistConsent(analytics: boolean): void {
  window.localStorage.setItem(
    CONSENT_STORAGE_KEY,
    JSON.stringify({ version: CONSENT_STATE_VERSION, analytics, marketing: false, updatedAt: '2026-01-01T00:00:00.000Z' }),
  );
}

describe('WebVitalsReporter (issues #1210, #1221)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    for (const key of Object.keys(metricCallbacks)) delete metricCallbacks[key];
    window.localStorage.clear();
    delete w().dataLayer;
    delete w().gtag;
    Object.defineProperty(navigator, 'doNotTrack', { value: undefined, configurable: true });
    mocks.pathname = '/';
    vi.stubGlobal('performance', {
      getEntriesByType: () => [],
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('dispatches page_view once on mount (not double on first effect run)', () => {
    persistConsent(true);
    render(<WebVitalsReporter />);

    expect(events('page_view')).toEqual([
      ['event', 'page_view', { page_path: '/', event_version: ANALYTICS_EVENT_CONTRACT_VERSION }],
    ]);
  });

  it('dispatches one page_view per pathname change and re-fires on return', () => {
    persistConsent(true);
    const { rerender } = render(<WebVitalsReporter />);
    expect(events('page_view')).toHaveLength(1);

    act(() => {
      mocks.pathname = '/courses';
      rerender(<WebVitalsReporter />);
    });
    expect(events('page_view')).toHaveLength(2);
    expect(events('page_view')[1]).toEqual(
      ['event', 'page_view', { page_path: '/courses', event_version: ANALYTICS_EVENT_CONTRACT_VERSION }],
    );

    act(() => {
      mocks.pathname = '/';
      rerender(<WebVitalsReporter />);
    });
    expect(events('page_view')).toHaveLength(3);
  });

  it('stamps event_version on web_vitals payloads', () => {
    persistConsent(true);
    render(<WebVitalsReporter />);

    act(() => {
      metricCallbacks.LCP!(fakeMetric());
    });

    const webVitals = events('web_vitals');
    expect(webVitals).toHaveLength(1);
    expect((webVitals[0] as unknown[])[2]).toMatchObject({
      metric_name: 'LCP',
      [EVENT_VERSION_PARAM]: ANALYTICS_EVENT_CONTRACT_VERSION,
    });
  });

  it('never re-fires a metric for the same navigationId across effect re-runs', () => {
    persistConsent(true);
    const { rerender } = render(<WebVitalsReporter />);
    act(() => {
      metricCallbacks.LCP!(fakeMetric({ id: 'lcp-1', navigationId: 'nav-1' }));
    });
    expect(events('web_vitals')).toHaveLength(1);

    // Simulate a pathname-effect re-run (same component instance) and the
    // library re-delivering the same metric for the old navigation.
    mocks.pathname = '/courses';
    rerender(<WebVitalsReporter />);
    act(() => {
      metricCallbacks.LCP!(fakeMetric({ id: 'lcp-1', navigationId: 'nav-1', value: 9999 }));
    });

    const webVitals = events('web_vitals');
    expect(webVitals).toHaveLength(1);
    expect((webVitals[0] as unknown[])[2]).toMatchObject({ metric_value: 2451 });
  });

  it('dispatches both CLS reports for one navigation when ids differ (final value not dropped)', () => {
    persistConsent(true);
    render(<WebVitalsReporter />);

    act(() => {
      metricCallbacks.CLS!(fakeMetric({ name: 'CLS', id: 'cls-1', navigationId: 'nav-1', value: 120 }));
    });
    act(() => {
      metricCallbacks.CLS!(fakeMetric({ name: 'CLS', id: 'cls-1', navigationId: 'nav-1', value: 999 }));
    });
    act(() => {
      metricCallbacks.CLS!(fakeMetric({ name: 'CLS', id: 'cls-2', navigationId: 'nav-1', value: 340 }));
    });

    const cls = events('web_vitals');
    expect(cls).toHaveLength(2);
    expect((cls[0] as unknown[])[2]).toMatchObject({ metric_value: 120000 });
    expect((cls[1] as unknown[])[2]).toMatchObject({ metric_value: 340000 });
  });

  it('sends nothing without analytics consent (fail closed)', () => {
    render(<WebVitalsReporter />);

    act(() => {
      metricCallbacks.LCP!(fakeMetric());
    });

    expect(w().dataLayer ?? []).toHaveLength(0);
    expect(allEvents()).toHaveLength(0);
  });
});

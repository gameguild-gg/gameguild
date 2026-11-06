import { cleanup, render } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import type { ConsentState } from '@/lib/consent/store';
import type { Metric } from 'web-vitals';

const readConsentMock = vi.fn<() => ConsentState | null>();
const subscribeConsentMock = vi.fn(() => vi.fn());

vi.mock('next/navigation', () => ({
  usePathname: vi.fn(() => '/'),
}));

vi.mock('@/lib/consent/store', () => ({
  readConsent: (...args: unknown[]) => readConsentMock(...(args as [])),
  subscribeConsent: (...args: unknown[]) => subscribeConsentMock(...(args as [])),
}));

const metricCallbacks = {} as Record<string, (m: Metric) => void>;
const vitalsMocks = {
  onLCP: (cb: (m: Metric) => void) => { metricCallbacks.LCP = cb; },
  onINP: (cb: (m: Metric) => void) => { metricCallbacks.INP = cb; },
  onCLS: (cb: (m: Metric) => void) => { metricCallbacks.CLS = cb; },
  onTTFB: (cb: (m: Metric) => void) => { metricCallbacks.TTFB = cb; },
  onFCP: (cb: (m: Metric) => void) => { metricCallbacks.FCP = cb; },
};
vi.mock('web-vitals', () => vitalsMocks);

if (typeof PerformanceNavigationTiming === 'undefined') {
  (globalThis as any).PerformanceNavigationTiming = class PerformanceNavigationTiming {};
}

let WebVitalsReporter: React.ComponentType;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
let usePathname: any;

function fakeMetric(overrides: Partial<Metric> = {}): Metric {
  return {
    name: 'LCP',
    value: 1200,
    rating: 'good',
    id: 'v3-1',
    delta: 1200,
    navigationType: 'navigate',
    entries: [],
    ...overrides,
  } as unknown as Metric;
}

function fakeNavigationEntry(startTime = 0): PerformanceNavigationTiming {
  return Object.assign(
    Object.create(PerformanceNavigationTiming.prototype),
    { startTime, responseStart: 100, domContentLoadedEventEnd: 500, loadEventEnd: 900, transferSize: 2048 },
  );
}

function dataLayerEvents(w: Window, name: string) {
  const layer = (w as { dataLayer?: unknown[][] }).dataLayer ?? [];
  return layer.filter((args) => args[0] === 'event' && args[1] === name);
}

beforeEach(async () => {
  vi.resetModules();
  metricCallbacks.LCP = undefined as unknown as (m: Metric) => void;
  ({ WebVitalsReporter } = await import('./web-vitals-reporter'));
  ({ usePathname } = await import('next/navigation'));
  vi.clearAllMocks();
  vi.mocked(usePathname).mockReturnValue('/');
  readConsentMock.mockReturnValue({ analytics: true } as ConsentState);
  Object.defineProperty(document, 'readyState', { configurable: true, value: 'complete' });
  vi.spyOn(performance, 'getEntriesByType').mockReturnValue([fakeNavigationEntry(0)]);
  subscribeConsentMock.mockReturnValue(vi.fn());
  delete (window as { dataLayer?: unknown[][] }).dataLayer;
});

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

describe('WebVitalsReporter page_load_timing dedupe', () => {
  it('sends page_load_timing once on initial load', () => {
    render(<WebVitalsReporter />);
    expect(dataLayerEvents(window, 'page_load_timing')).toHaveLength(1);
    expect(dataLayerEvents(window, 'page_load_timing')[0]![2]).toMatchObject({ page_path: '/' });
  });

  it('does not re-send page_load_timing on SPA route change', () => {
    const { rerender } = render(<WebVitalsReporter />);
    vi.mocked(usePathname).mockReturnValue('/courses');
    rerender(<WebVitalsReporter />);

    const events = dataLayerEvents(window, 'page_load_timing');
    expect(events).toHaveLength(1);
    expect(events[0]![2]).toMatchObject({ page_path: '/' });
  });

  it('still sends web_vitals per route change', () => {
    const { rerender } = render(<WebVitalsReporter />);
    metricCallbacks.LCP?.(fakeMetric({ id: 'lcp-1', navigationId: 1 }));

    vi.mocked(usePathname).mockReturnValue('/courses');
    rerender(<WebVitalsReporter />);
    metricCallbacks.LCP?.(fakeMetric({ id: 'lcp-1', navigationId: 2 }));

    const events = dataLayerEvents(window, 'web_vitals');
    expect(events).toHaveLength(2);
    expect(events[0]![2]).toMatchObject({ page_path: '/' });
    expect(events[1]![2]).toMatchObject({ page_path: '/courses' });
  });
});

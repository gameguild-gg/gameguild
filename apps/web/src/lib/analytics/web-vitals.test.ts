import { describe, expect, it, vi } from 'vitest';
import {
  buildPageLoadTimingPayload,
  buildWebVitalsPayload,
  createDispatcher,
  registerWebVitalsCallbacks,
  sendToGa4,
  type FirebaseAnalyticsLike,
} from './web-vitals';
import type { ConsentWindow } from '@/lib/consent/consent-mode';
import { DENIED_CONSENT, type ConsentState } from '@/lib/consent/store';
import type { Metric } from 'web-vitals';

function fakeMetric(overrides: Partial<Metric> = {}): Metric {
  return {
    name: 'LCP',
    value: 2450.7,
    rating: 'good',
    id: 'v3-abc123',
    delta: 2450.7,
    navigationType: 'navigate',
    entries: [],
    ...overrides,
  } as unknown as Metric;
}

function grantedConsent(): ConsentState {
  return { ...DENIED_CONSENT, analytics: true, updatedAt: '2026-01-01T00:00:00.000Z' };
}

describe('buildWebVitalsPayload', () => {
  it('maps metric fields into the web_vitals payload shape', () => {
    expect(buildWebVitalsPayload(fakeMetric(), '/courses')).toEqual({
      metric_name: 'LCP',
      metric_value: 2451,
      metric_rating: 'good',
      metric_id: 'v3-abc123',
      page_path: '/courses',
    });
  });

  it('scales CLS to an integer (per-thousand) but keeps ms metrics rounded', () => {
    const cls = buildWebVitalsPayload(fakeMetric({ name: 'CLS', value: 0.128, rating: 'good', id: 'cls-1' }), '/');
    expect(cls.metric_value).toBe(128);
    const inp = buildWebVitalsPayload(fakeMetric({ name: 'INP', value: 203.4, rating: 'needs-improvement' }), '/');
    expect(inp.metric_value).toBe(203);
  });

  it('falls back to unknown rating when missing', () => {
    const payload = buildWebVitalsPayload(fakeMetric({ rating: undefined }), '/');
    expect(payload.metric_rating).toBe('unknown');
  });
});

describe('buildPageLoadTimingPayload', () => {
  it('maps Navigation Timing 2 fields', () => {
    const entry = {
      responseStart: 120.4,
      domContentLoadedEventEnd: 900.9,
      loadEventEnd: 1500.2,
      transferSize: 4096,
    } as PerformanceNavigationTiming;
    expect(buildPageLoadTimingPayload(entry, '/play')).toEqual({
      metric_name: 'page_load_timing',
      response_start_ms: 120,
      dom_content_loaded_ms: 901,
      load_event_ms: 1500,
      transfer_size: 4096,
      page_path: '/play',
    });
  });
});

describe('sendToGa4', () => {
  it('creates the dataLayer shim lazily and pushes the event', () => {
    const w = {} as ConsentWindow;
    sendToGa4(w, 'web_vitals', { metric_name: 'LCP' });
    expect(w.dataLayer).toBeDefined();
    // arguments object, not array (gtag.js requirement) — 'callee' presence marks it,
    // but reading .callee in strict mode throws, so check with `in`.
    const entry = w.dataLayer?.[0] as object;
    expect(entry).toMatchObject({ 0: 'event', 1: 'web_vitals', 2: { metric_name: 'LCP' } });
    expect(Array.isArray(entry)).toBe(false);
    expect('callee' in entry).toBe(true);
  });

  it('fails closed on shim errors', () => {
    const w = { get dataLayer() { throw new Error('blocked'); } } as unknown as ConsentWindow;
    expect(() => sendToGa4(w, 'x', {})).not.toThrow();
  });
});

describe('createDispatcher consent gating', () => {
  const loader = vi.fn(async (): Promise<FirebaseAnalyticsLike | null> => ({
    logEvent: vi.fn(),
  }));

  function gaWindow() {
    const w = {} as ConsentWindow;
    w.gtag = vi.fn((...args: unknown[]) => {
      w.dataLayer = w.dataLayer || [];
      w.dataLayer.push(args);
    });
    w.dataLayer = [];
    return w;
  }

  it('sends nothing when consent is denied or absent', async () => {
    const dispatcher = createDispatcher(() => DENIED_CONSENT, loader);
    const w = gaWindow();
    dispatcher.sendGa4(w, 'web_vitals', { a: 1 });
    await dispatcher.sendFirebase('web_vitals', { a: 1 });
    expect(w.dataLayer).toHaveLength(0);
    expect(loader).not.toHaveBeenCalled();
  });

  it('sends to GA4 and Firebase when analytics consent is granted', async () => {
    const analytics = { logEvent: vi.fn() };
    const loaderGranted = vi.fn(async () => analytics);
    const dispatcher = createDispatcher(grantedConsent, loaderGranted);
    const w = gaWindow();
    dispatcher.sendGa4(w, 'web_vitals', { a: 1 });
    await dispatcher.sendFirebase('web_vitals', { a: 1 });
    expect(w.dataLayer).toContainEqual(['event', 'web_vitals', { a: 1 }]);
    expect(analytics.logEvent).toHaveBeenCalledWith('web_vitals', { a: 1 });
  });

  it('fails closed when Firebase init throws', async () => {
    const failing = vi.fn(async () => {
      throw new Error('no config');
    });
    const dispatcher = createDispatcher(grantedConsent, failing);
    await expect(dispatcher.sendFirebase('web_vitals', { a: 1 })).resolves.toBeUndefined();
  });

  it('fails closed when logEvent throws', async () => {
    const analytics = {
      logEvent: vi.fn(() => {
        throw new Error('offline');
      }),
    };
    const dispatcher = createDispatcher(grantedConsent, async () => analytics);
    await expect(dispatcher.sendFirebase('web_vitals', { a: 1 })).resolves.toBeUndefined();
  });
});

describe('registerWebVitalsCallbacks', () => {
  function captureHooks() {
    const callbacks = {} as Record<string, (m: Metric) => void>;
    const hooks = {
      onLCP: (cb: (m: Metric) => void) => { callbacks.LCP = cb; },
      onINP: (cb: (m: Metric) => void) => { callbacks.INP = cb; },
      onCLS: (cb: (m: Metric) => void) => { callbacks.CLS = cb; },
      onTTFB: (cb: (m: Metric) => void) => { callbacks.TTFB = cb; },
      onFCP: (cb: (m: Metric) => void) => { callbacks.FCP = cb; },
    };
    return { hooks, callbacks };
  }

  it('registers all five metric callbacks (INP, not FID)', () => {
    const { hooks, callbacks } = captureHooks();
    registerWebVitalsCallbacks(hooks, vi.fn());
    for (const name of ['LCP', 'INP', 'CLS', 'TTFB', 'FCP']) {
      expect(callbacks[name]).toBeTypeOf('function');
    }
    expect(callbacks.FID).toBeUndefined();
  });

  it('fires each metric once per page view (navigation id)', () => {
    const { hooks, callbacks } = captureHooks();
    const onMetric = vi.fn();
    registerWebVitalsCallbacks(hooks, onMetric);

    callbacks.LCP(fakeMetric({ id: 'lcp-1' }));
    callbacks.LCP(fakeMetric({ id: 'lcp-1', value: 3000 })); // duplicate
    callbacks.LCP(fakeMetric({ navigationId: 'nav-2', id: 'lcp-1' })); // new page view

    expect(onMetric).toHaveBeenCalledTimes(2);
  });

  it('stops reporting after cleanup', () => {
    const { hooks, callbacks } = captureHooks();
    const onMetric = vi.fn();
    const stop = registerWebVitalsCallbacks(hooks, onMetric);
    stop();
    callbacks.TTFB(fakeMetric({ name: 'TTFB', id: 't1' }));
    expect(onMetric).not.toHaveBeenCalled();
  });
});

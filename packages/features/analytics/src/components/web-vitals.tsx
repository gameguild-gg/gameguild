'use client';

import {useEffect} from 'react';

export interface WebVitalsProps {
  endpoint?: string;
  onMetric?: (metric: {name: string; value: number; rating?: string; path: string}) => void;
}

function sendMetric(endpoint: string, metric: {name: string; value: number; rating?: string; path: string}): void {
  const payload = JSON.stringify({...metric, timestamp: new Date().toISOString()});

  if (navigator.sendBeacon) {
    navigator.sendBeacon(endpoint, new Blob([payload], {type: 'application/json'}));
    return;
  }

  void fetch(endpoint, {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: payload,
    keepalive: true,
  }).catch(() => undefined);
}

function rateMetric(name: string, value: number): string {
  if (name === 'CLS') return value <= 0.1 ? 'good' : value <= 0.25 ? 'needs-improvement' : 'poor';
  if (name === 'LCP') return value <= 2500 ? 'good' : value <= 4000 ? 'needs-improvement' : 'poor';
  if (name === 'INP') return value <= 200 ? 'good' : value <= 500 ? 'needs-improvement' : 'poor';
  return 'observed';
}

export const WebVitals = ({endpoint = '/api/analytics/web-vitals', onMetric}: WebVitalsProps): null => {
  useEffect(() => {
    if (typeof PerformanceObserver === 'undefined') {
      return;
    }

    const observers: PerformanceObserver[] = [];
    const observe = (entryType: string, name: string, getValue: (entry: PerformanceEntry) => number): void => {
      try {
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            const value = getValue(entry);
            const metric = {
              name,
              value,
              rating: rateMetric(name, value),
              path: window.location.pathname,
            };
            onMetric?.(metric);
            sendMetric(endpoint, metric);
          }
        });

        observer.observe({type: entryType, buffered: true});
        observers.push(observer);
      } catch {
        // Some browsers do not support every metric entry type.
      }
    };

    observe('largest-contentful-paint', 'LCP', (entry) => entry.startTime);
    observe('layout-shift', 'CLS', (entry) => ('hadRecentInput' in entry && entry.hadRecentInput ? 0 : Number('value' in entry ? entry.value : 0)));
    observe('event', 'INP', (entry) => Number('duration' in entry ? entry.duration : 0));

    return () => {
      observers.forEach((observer) => observer.disconnect());
    };
  }, [endpoint, onMetric]);

  return null;
};

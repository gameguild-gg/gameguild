'use client';

import { useReportWebVitals } from 'next/web-vitals';
import { sendGAEvent } from '@next/third-parties/google';

const WEB_VITALS_ENDPOINT = '/api/analytics/web-vitals'; // Replace with your actual endpoint

export const WebVitals = (): null => {
  useReportWebVitals((metric) => {
    const payload = JSON.stringify(metric);

    // TODO: Uncomment this when the endpoint is ready.
    // Try to send the metric to an analytics endpoint
    // using `navigator.sendBeacon` if available.
    if (navigator.sendBeacon) navigator.sendBeacon(WEB_VITALS_ENDPOINT, payload);
    else {
      void fetch(WEB_VITALS_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: payload,
        keepalive: true,
      });
    }

    sendGAEvent('event', metric.name, {
      value: Math.round(metric.name === 'CLS' ? metric.value * 1000 : metric.value),
      metric_id: metric.id,
      metric_category: 'Web Vitals',
      metric_value: metric.value,
      metric_delta: metric.delta,
      metric_rating: metric.rating,
      metric_navigation_type: metric.navigationType,
      page_path: window.location.pathname,
      non_interaction: true,
    });

    // TODO: Remove this console log in production
    console.log('Web Vitals metric:', metric);
  });

  return null;
};

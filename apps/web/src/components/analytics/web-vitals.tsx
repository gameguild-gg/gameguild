'use client';

import { useReportWebVitals } from 'next/web-vitals';
import { sendGAEvent } from '@next/third-parties/google';

const WEB_VITALS_ENDPOINT = '/api/analytics/web-vitals'; // Replace with your actual endpoint

export const WebVitals = (): null => {
  useReportWebVitals((metric) => {
    const payload = JSON.stringify(metric);

    //   // Send the metric to our analytics endpoint
    //   // using `navigator.sendBeacon` if available for better reliability
    //   if (typeof navigator !== 'undefined' && navigator.sendBeacon) {
    //     navigator.sendBeacon(WEB_VITALS_ENDPOINT, payload);
    //   } else {
    //     // Fallback to fetch for environments where sendBeacon is not available
    //     void fetch(WEB_VITALS_ENDPOINT, {
    //       method: 'POST',
    //       headers: { 'Content-Type': 'application/json' },
    //       body: payload,
    //       keepalive: true,
    //     }).catch((error) => {
    //       console.warn('Failed to send web vitals to analytics endpoint:', error);
    //     });
    //   }

    //   // Also send to Google Analytics if available
    //   if (typeof window !== 'undefined' && window.gtag) {
    //     sendGAEvent('event', metric.name, {
    //       value: Math.round(metric.name === 'CLS' ? metric.value * 1000 : metric.value),
    //       metric_id: metric.id,
    //       metric_category: 'Web Vitals',
    //       metric_value: metric.value,
    //       metric_delta: metric.delta,
    //       metric_rating: metric.rating,
    //       metric_navigation_type: metric.navigationType,
    //       page_path: window.location.pathname,
    //       non_interaction: true,
    //     });
    //   }
  });

  return null;
};

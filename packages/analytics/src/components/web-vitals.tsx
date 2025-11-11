'use client';

// const WEB_VITALS_ENDPOINT = '/api/analytics/web-vitals';

export const WebVitals = (): null => {
  // useReportWebVitals((metric): void => {
  // const payload = JSON.stringify(metric);
  // Try to send the metric to an analytics endpoint
  // using `navigator.sendBeacon` if available for better reliability
  // if (navigator.sendBeacon) {
  //   navigator.sendBeacon(WEB_VITALS_ENDPOINT, payload);
  // } else {
  //   // Fallback to fetch for environments where sendBeacon is not available
  //   void fetch(WEB_VITALS_ENDPOINT, {
  //     method: 'POST',
  //     headers: { 'Content-Type': 'application/json' },
  //     body: payload,
  //     keepalive: true,
  //   }).catch((error) => {
  //     console.warn('Failed to send web vitals to analytics endpoint:', error);
  //   });
  // }
  // TODO: https://github.com/GoogleChrome/web-vitals#send-the-results-to-google-analytics
  // Also send it to Google Analytics if available
  // if (typeof window !== 'undefined' && window.gtag) {
  //   sendGAEvent('event', metric.name, {
  //     value: Math.round(metric.name === 'CLS' ? metric.value * 1000 : metric.value),
  //     metric_id: metric.id,
  //     metric_category: 'Web Vitals',
  //     metric_value: metric.value,
  //     metric_delta: metric.delta,
  //     metric_rating: metric.rating,
  //     metric_navigation_type: metric.navigationType,
  //     page_path: window.location.pathname,
  //     non_interaction: true,
  //   });
  // }
  // });

  return null;
};

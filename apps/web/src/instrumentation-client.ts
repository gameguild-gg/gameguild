const telemetryEndpoint = process.env.NEXT_PUBLIC_TELEMETRY_ENDPOINT;

function reportClientError(error: unknown, source: string): void {
  if (!telemetryEndpoint) {
    return;
  }

  const payload = JSON.stringify({
    source,
    message: error instanceof Error ? error.message : String(error),
    stack: error instanceof Error ? error.stack : undefined,
    url: window.location.href,
    userAgent: navigator.userAgent,
    timestamp: new Date().toISOString(),
  });

  if (navigator.sendBeacon) {
    navigator.sendBeacon(telemetryEndpoint, new Blob([payload], { type: 'application/json' }));
    return;
  }

  void fetch(telemetryEndpoint, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: payload,
    keepalive: true,
  }).catch(() => undefined);
}

performance.mark('app-init');

window.addEventListener('error', (event) => {
  reportClientError(event.error ?? event.message, 'window.error');
});

window.addEventListener('unhandledrejection', (event) => {
  reportClientError(event.reason, 'window.unhandledrejection');
});

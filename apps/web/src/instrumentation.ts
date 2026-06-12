import { type Instrumentation } from 'next';

export const register = () => {};

export const onRequestError: Instrumentation.onRequestError = async (error, request, context): Promise<void> => {
  const telemetryEndpoint = process.env.TELEMETRY_ENDPOINT;
  const normalizedError = error instanceof Error ? error : new Error(String(error));
  const digest = typeof error === 'object' && error !== null && 'digest' in error ? String(error.digest) : undefined;

  if (!telemetryEndpoint) {
    console.error('Request Error:', { error: normalizedError, request, context });
    return;
  }

  await fetch(telemetryEndpoint, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      source: 'next.request',
      message: normalizedError.message,
      stack: normalizedError.stack,
      digest,
      request,
      context,
      timestamp: new Date().toISOString(),
    }),
  }).catch(() => {
    console.error('Request Error:', { error: normalizedError, request, context });
  });
};

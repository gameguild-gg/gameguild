import { NextResponse } from 'next/server';
import { elapsedMs, getErrorMessage, getRequestId, logWebRequest } from '@/lib/server/request-logging';

export const GET = async (request: Request): Promise<NextResponse> => {
  const startedAt = performance.now();
  const requestId = getRequestId(request.headers);

  try {
    const response = NextResponse.json(
      {
        status: 'healthy',
        timestamp: new Date().toISOString(),
        service: 'web',
        version: process.env.npm_package_version || 'Unknown',
      },
      { status: 200 },
    );
    response.headers.set('x-request-id', requestId);
    logWebRequest({
      event: 'web.route.complete',
      method: request.method,
      path: new URL(request.url).pathname,
      status: 200,
      durationMs: elapsedMs(startedAt),
      requestId,
    });

    return response;
  } catch (error) {
    const response = NextResponse.json(
      {
        status: 'unhealthy',
        timestamp: new Date().toISOString(),
        service: 'web',
        error: getErrorMessage(error) ?? 'Unknown error',
      },
      { status: 503 },
    );
    response.headers.set('x-request-id', requestId);
    logWebRequest({
      event: 'web.route.error',
      method: request.method,
      path: new URL(request.url).pathname,
      status: 503,
      durationMs: elapsedMs(startedAt),
      requestId,
      error,
    });

    return response;
  }
};

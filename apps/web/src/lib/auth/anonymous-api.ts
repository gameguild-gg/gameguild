import 'server-only';

import { createClient } from '@game-guild/client';
import { forwardedIpHeaders } from '@/lib/server/forwarded-headers';

/**
 * A server-side client for anonymous API endpoints (the lost-second-factor ceremony, the
 * emailed sign-in code): no bearer, and the visitor's address forwarded when a trusted
 * edge provides one, so the API's per-address limits see the real client.
 */
export async function createAnonymousApiClient() {
  const forwardedHeaders = await forwardedIpHeaders();
  return createClient({
    baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || '',
    autoRefresh: false,
    timeout: 5_000,
    devtools: { enabled: false },
    interceptors: [
      {
        onRequest(request) {
          return Promise.resolve({ ...request, headers: { ...request.headers, ...forwardedHeaders } });
        },
      },
    ],
  });
}

/** Whether a failed call says nothing about the request itself: the API was not reached, or
 *  its answer could not be read. Those become "try again", never "denied". */
export function isUnreachable(error: { status: number; code?: string }): boolean {
  return error.status === 0 || error.status >= 500 || error.code === 'PARSE_ERROR';
}

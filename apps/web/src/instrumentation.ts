import { type Instrumentation } from 'next';

import { purgeCloudflareCache } from '@/lib/core/cloudflare-cache-purge';

/**
 * Called once when the Next.js server starts (both dev and production).
 * Used to run one-time startup tasks like flushing stale CDN caches.
 */
export async function register(): Promise<void> {
  // Only purge on the server side (not during Edge runtime or builds).
  if (process.env.NEXT_RUNTIME === 'nodejs') {
    // Delay the cache purge so the canary deployment has time to become
    // healthy and receive traffic before we invalidate the CDN cache.
    // Without this delay the old version would still be serving and
    // Cloudflare would immediately re-cache stale content.
    const delayMs = Number(process.env.CLOUDFLARE_PURGE_DELAY_MS) || 60_000;
    console.log(`[cloudflare] Scheduling cache purge in ${delayMs / 1000}s …`);
    setTimeout(() => {
      purgeCloudflareCache().catch((err) =>
        console.error('[cloudflare] Deferred cache purge error:', err),
      );
    }, delayMs);
  }
}

export const onRequestError: Instrumentation.onRequestError = async (error, request, context): Promise<void> => {
  // Log the error details for debugging purposes
  console.error('Request Error:', { error, request, context });
  //
  // TODO: add additional error handling logic here, such as sending the error to an external logging service.
  // or notifying the development team.
  // error: { digest: string } & Error,
  // await fetch('https://.../report-error', {
  //   method: 'POST',
  //   body: JSON.stringify({
  //     message: error.message,
  //     request,
  //     context,
  //   }),
  //   headers: {
  //     'Content-Type': 'application/json',
  //   },
  // });
};

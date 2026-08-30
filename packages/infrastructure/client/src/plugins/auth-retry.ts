/**
 * Auth Retry Plugin
 *
 * Wraps a transport to automatically refresh the access token and retry
 * the original request when a 401 Unauthorized response is received.
 *
 * This is separate from the general retry plugin which handles 5xx/429/network
 * errors. This specifically handles the "token expired mid-request" scenario.
 *
 * @example
 * ```typescript
 * import { createAuthRetryPlugin } from '@game-guild/client';
 *
 * const authRetry = createAuthRetryPlugin({
 *   refreshToken: async () => {
 *     const res = await fetch('/api/auth/session', { method: 'POST' });
 *     return res.ok;
 *   },
 *   maxRetries: 1,
 * });
 *
 * const transport = authRetry.wrapTransport(baseTransport);
 * ```
 */

import { err } from '../runtime/result/helpers.js';
import type { Result } from '../runtime/result/types.js';
import type { ApiError } from '../runtime/errors/types.js';
import type { RequestConfig, Transport, ApiResponse } from '../runtime/transport/types.js';

/**
 * Configuration for auth retry plugin
 */
export interface AuthRetryConfig {
  /**
   * Function that attempts to refresh the access token.
   * Return true if refresh succeeded, false if it failed
   * (which means the user must re-authenticate).
   */
  refreshToken: () => Promise<boolean>;

  /**
   * Maximum number of refresh attempts per request (default: 1).
   * Usually 1 is sufficient — if the first refresh fails, the token
   * is likely invalid and the user needs to re-login.
   */
  maxRetries?: number;

  /**
   * Called when refresh fails and re-authentication is required.
   * Typically redirects to the sign-in page.
   */
  onAuthenticationRequired?: () => void | Promise<void>;

  /**
   * Optional filter: only retry specific 401 responses.
   * By default all 401s trigger a refresh attempt.
   */
  shouldRetryOnUnauthorized?: (error: ApiError) => boolean;
}

/**
 * Create an auth retry plugin that handles 401 responses.
 *
 * Flow:
 * 1. Make request
 * 2. If 401 → refresh token (deduplicated via per-instance mutex)
 * 3. If refresh succeeds → retry original request
 * 4. If refresh fails → call onAuthenticationRequired, return error
 */
export function createAuthRetryPlugin(config: AuthRetryConfig): {
  wrapTransport: (transport: Transport) => Transport;
} {
  const maxRetries = config.maxRetries ?? 1;
  const shouldRetry = config.shouldRetryOnUnauthorized ?? (() => true);
  // Per-instance mutex — each plugin instance gets its own refresh lock
  let refreshPromise: Promise<boolean> | null = null;

  return {
    wrapTransport(transport: Transport): Transport {
      return {
        async request<T>(requestConfig: RequestConfig): Promise<Result<ApiResponse<T>, ApiError>> {
          let attempt = 0;

          while (attempt <= maxRetries) {
            const result = await transport.request<T>(requestConfig);

            // Success — return immediately
            if (result.ok) {
              return result;
            }

            // Not a 401 — don't handle, let other plugins/caller deal with it
            if (result.error.status !== 401) {
              return result;
            }

            // 401 but filter says don't retry
            if (!shouldRetry(result.error)) {
              return result;
            }

            attempt++;

            if (attempt > maxRetries) {
              // All refresh attempts exhausted
              if (config.onAuthenticationRequired) {
                await config.onAuthenticationRequired();
              }
              return err({
                ...result.error,
                metadata: {
                  ...result.error.metadata,
                  authRetryExhausted: true,
                  totalRefreshAttempts: attempt,
                },
              });
            }

            // Deduplicate concurrent refreshes with a mutex
            let refreshSucceeded: boolean;
            if (refreshPromise) {
              // Another request is already refreshing — wait for it
              refreshSucceeded = await refreshPromise;
            } else {
              // We're the first — do the refresh
              refreshPromise = config.refreshToken();
              try {
                refreshSucceeded = await refreshPromise;
              } finally {
                refreshPromise = null;
              }
            }

            if (!refreshSucceeded) {
              /* v8 ignore start */
              if (config.onAuthenticationRequired) {
                await config.onAuthenticationRequired();
              }
              /* v8 ignore stop */
              return err({
                ...result.error,
                metadata: {
                  ...result.error.metadata,
                  authRefreshFailed: true,
                },
              });
            }

            // Refresh succeeded — retry the original request
            // The transport should pick up the new token from the token provider
          }

          // Should not reach here, but TypeScript needs it
          /* v8 ignore start */
          return err({
            name: 'ApiError',
            status: 401,
            code: 'AUTHENTICATION_ERROR' as const,
            message: 'Authentication retry exhausted',
          });
          /* v8 ignore stop */
        },
      };
    },
  };
}

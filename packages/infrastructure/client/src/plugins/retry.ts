/**
 * Retry Plugin
 *
 * Automatic retry with exponential backoff for failed requests.
 */

import { err } from '../runtime/result/helpers.js';
import type { Result } from '../runtime/result/types.js';
import type { ApiError } from '../runtime/errors/types.js';
import type { Interceptor, RequestConfig, Transport } from '../runtime/transport/types.js';
import { isRetryableError, getRetryAfter } from '../runtime/errors/guards.js';

/**
 * Configuration for retry plugin
 */
export interface RetryConfig {
  /** Maximum number of retries */
  maxRetries?: number;
  /** Base delay between retries (ms) */
  baseDelay?: number;
  /** Maximum delay between retries (ms) */
  maxDelay?: number;
  /** Whether to use exponential backoff */
  exponentialBackoff?: boolean;
  /** Function to determine if an error should be retried */
  shouldRetry?: (error: ApiError) => boolean;
}

const DEFAULT_RETRY_CONFIG: Required<RetryConfig> = {
  maxRetries: 3,
  baseDelay: 1000,
  maxDelay: 30000,
  exponentialBackoff: true,
  shouldRetry: isRetryableError,
};

/**
 * Calculate delay for retry attempt
 */
function calculateDelay(attempt: number, config: Required<RetryConfig>, retryAfter?: number): number {
  // Respect Retry-After header if provided
  if (retryAfter && retryAfter > 0) {
    return Math.min(retryAfter * 1000, config.maxDelay);
  }

  if (config.exponentialBackoff) {
    const delay = config.baseDelay * Math.pow(2, attempt - 1);
    // Add jitter to prevent thundering herd
    const jitter = delay * 0.1 * Math.random();
    return Math.min(delay + jitter, config.maxDelay);
  }

  return config.baseDelay;
}

/**
 * Sleep utility
 */
function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Create a retry plugin that wraps a transport
 *
 * Unlike simple interceptors, this plugin wraps the transport to actually
 * re-execute failed requests with exponential backoff.
 *
 * @example
 * ```typescript
 * const retryPlugin = createRetryPlugin({ maxRetries: 3 });
 * const wrappedTransport = retryPlugin.wrapTransport(transport);
 * ```
 */
export function createRetryPlugin(userConfig?: RetryConfig): {
  wrapTransport: (transport: Transport) => Transport;
} {
  const config = { ...DEFAULT_RETRY_CONFIG, ...userConfig };

  return {
    wrapTransport(transport: Transport): Transport {
      return {
        async request<T>(requestConfig: RequestConfig): Promise<Result<import('../runtime/transport/types.js').ApiResponse<T>, ApiError>> {
          let attempt = 0;
          let lastError: ApiError | null = null;

          while (attempt <= config.maxRetries) {
            const result = await transport.request<T>(requestConfig);

            if (result.ok) {
              return result;
            }

            lastError = result.error;

            // Check if we should retry
            if (!config.shouldRetry(result.error)) {
              return result;
            }

            attempt++;

            if (attempt > config.maxRetries) {
              break;
            }

            // Calculate delay
            const retryAfter = getRetryAfter(result.error);
            const delay = calculateDelay(attempt, config, retryAfter ?? undefined);

            // Wait before retry
            await sleep(delay);
          }

          // All retries exhausted
          return err({
            ...lastError!,
            metadata: {
              ...lastError!.metadata,
              retriesExhausted: true,
              totalAttempts: attempt,
            },
          });
        },
      };
    },
  };
}

/**
 * Create a simple retry interceptor (for logging/tracking purposes)
 *
 * Note: This interceptor only marks errors for retry. For actual retry functionality,
 * use createRetryPlugin().wrapTransport() instead.
 *
 * @deprecated Use createRetryPlugin() for actual retry functionality
 */
export function createRetryInterceptor(userConfig?: RetryConfig): Interceptor {
  const config = { ...DEFAULT_RETRY_CONFIG, ...userConfig };

  return {
    async onError(error: ApiError): Promise<Result<never, ApiError>> {
      // Check if we should retry
      if (!config.shouldRetry(error)) {
        return err(error);
      }

      const attempt = (error.metadata?.retryAttempt as number) || 0;
      if (attempt >= config.maxRetries) {
        return err({
          ...error,
          metadata: {
            ...error.metadata,
            retriesExhausted: true,
          },
        });
      }

      // Mark for potential retry (actual retry handled by transport wrapper)
      return err({
        ...error,
        metadata: {
          ...error.metadata,
          retryAttempt: attempt + 1,
          shouldRetry: true,
        },
      });
    },
  };
}

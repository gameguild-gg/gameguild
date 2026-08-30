/**
 * Request Deduplication
 *
 * Prevents duplicate in-flight requests by caching pending promises
 */

import type { Result } from '../result/types.js';
import type { ApiError } from '../errors/types.js';

export interface DeduplicationConfig {
  /**
   * Enable deduplication (default: true)
   */
  enabled?: boolean;

  /**
   * Custom key generator for deduplication
   */
  keyGenerator?: (method: string, url: string, body?: unknown) => string;
}

/**
 * Default key generator for deduplication
 */
function defaultKeyGenerator(method: string, url: string, body?: unknown): string {
  const bodyKey = body ? JSON.stringify(body) : '';
  return `${method}:${url}:${bodyKey}`;
}

/**
 * Request deduplication manager
 */
export class RequestDeduplicator {
  private pendingRequests = new Map<string, Promise<Result<unknown, ApiError>>>();
  private keyGenerator: (method: string, url: string, body?: unknown) => string;

  constructor(config: DeduplicationConfig = {}) {
    this.keyGenerator = config.keyGenerator || defaultKeyGenerator;
  }

  /**
   * Deduplicate a request by caching in-flight promises
   */
  async deduplicate<T>(method: string, url: string, body: unknown | undefined, executor: () => Promise<Result<T, ApiError>>): Promise<Result<T, ApiError>> {
    const key = this.keyGenerator(method, url, body);

    // Check if request is already in-flight
    const existing = this.pendingRequests.get(key);
    if (existing) {
      return existing as Promise<Result<T, ApiError>>;
    }

    // Execute request and cache promise
    const promise = executor();
    this.pendingRequests.set(key, promise as Promise<Result<unknown, ApiError>>);

    // Clean up after completion
    promise.finally(() => {
      this.pendingRequests.delete(key);
    });

    return promise;
  }

  /**
   * Clear all pending requests
   */
  clear(): void {
    this.pendingRequests.clear();
  }

  /**
   * Get number of pending requests
   */
  get pendingCount(): number {
    return this.pendingRequests.size;
  }
}

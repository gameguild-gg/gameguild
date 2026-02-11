/**
 * Cache Plugin
 *
 * In-memory caching with TTL and tag-based invalidation.
 */

import type { Interceptor, RequestConfig, ApiResponse } from '../runtime/transport/types.js';

/**
 * Cache entry
 */
interface CacheEntry<T> {
  data: T;
  timestamp: number;
  ttl: number;
  tags: string[];
}

/**
 * Configuration for cache plugin
 */
export interface CacheConfig {
  /** Default TTL in milliseconds */
  defaultTtl?: number;
  /** Maximum cache entries */
  maxEntries?: number;
  /** Methods to cache (default: GET only) */
  cacheMethods?: string[];
  /** Function to generate cache tags from request */
  generateTags?: (request: RequestConfig) => string[];
}

/**
 * Simple in-memory cache with LRU eviction
 */
export class MemoryCache {
  private cache = new Map<string, CacheEntry<unknown>>();
  private maxEntries: number;

  constructor(maxEntries = 100) {
    this.maxEntries = maxEntries;
  }

  /**
   * Get cached value
   */
  get<T>(key: string): T | null {
    const entry = this.cache.get(key) as CacheEntry<T> | undefined;
    if (!entry) return null;

    // Check if expired
    if (Date.now() > entry.timestamp + entry.ttl) {
      this.cache.delete(key);
      return null;
    }

    // Move to end for LRU
    this.cache.delete(key);
    this.cache.set(key, entry);

    return entry.data;
  }

  /**
   * Set cached value
   */
  set<T>(key: string, data: T, ttl: number, tags: string[] = []): void {
    // Evict oldest if at capacity
    if (this.cache.size >= this.maxEntries) {
      const oldestKey = this.cache.keys().next().value;
      /* v8 ignore start -- oldestKey always truthy when cache.size >= maxEntries */
      if (oldestKey) {
        this.cache.delete(oldestKey);
      }
      /* v8 ignore stop */
    }

    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl,
      tags,
    });
  }

  /**
   * Check if key exists and is not expired
   */
  has(key: string): boolean {
    return this.get(key) !== null;
  }

  /**
   * Invalidate specific key
   */
  invalidate(key: string): boolean {
    return this.cache.delete(key);
  }

  /**
   * Invalidate all entries with a specific tag
   */
  invalidateByTag(tag: string): number {
    let count = 0;
    for (const [key, entry] of this.cache.entries()) {
      if (entry.tags.includes(tag)) {
        this.cache.delete(key);
        count++;
      }
    }
    return count;
  }

  /**
   * Invalidate entries matching a pattern
   */
  invalidateByPattern(pattern: RegExp): number {
    let count = 0;
    for (const key of this.cache.keys()) {
      if (pattern.test(key)) {
        this.cache.delete(key);
        count++;
      }
    }
    return count;
  }

  /**
   * Invalidate entries by path prefix
   */
  invalidateByPath(pathPrefix: string): number {
    let count = 0;
    for (const key of Array.from(this.cache.keys())) {
      // Keys don't have : prefix, match actual path
      if (key.startsWith(pathPrefix)) {
        this.cache.delete(key);
        count++;
      }
    }
    return count;
  }

  /**
   * Alias for invalidateByPath
   */
  invalidatePath(pathPrefix: string): number {
    return this.invalidateByPath(pathPrefix);
  }

  /**
   * Invalidate entries by multiple tags
   */
  invalidateTags(tags: string[]): number {
    let count = 0;
    for (const tag of tags) {
      count += this.invalidateByTag(tag);
    }
    return count;
  }

  /**
   * Clear all cache entries
   */
  clear(): void {
    this.cache.clear();
  }

  /**
   * Get cache statistics
   */
  stats(): { size: number; maxEntries: number } {
    return {
      size: this.cache.size,
      maxEntries: this.maxEntries,
    };
  }
}

/**
 * Create cache key from request
 */
function createCacheKey(request: RequestConfig): string {
  const params = request.params ? JSON.stringify(request.params) : '';
  return `${request.method}:${request.path}:${params}`;
}

/**
 * Default tag generator - extracts entity IDs from paths
 */
function defaultGenerateTags(request: RequestConfig): string[] {
  const tags: string[] = [];

  // Extract entity type and ID from common REST patterns
  // e.g., /users/123 -> ['users', 'users:123']
  const pathParts = request.path.split('/').filter(Boolean);

  for (let i = 0; i < pathParts.length; i++) {
    const part = pathParts[i];

    // Skip version prefixes
    if (/^v\d+$/.test(part)) continue;

    tags.push(part);

    // If next part looks like an ID, add entity:id tag
    const nextPart = pathParts[i + 1];
    if (nextPart && /^[a-f0-9-]{8,}$/i.test(nextPart)) {
      tags.push(`${part}:${nextPart}`);
    }
  }

  return tags;
}

/**
 * Cache interceptor with exposed cache for manual invalidation
 */
export interface CacheInterceptor extends Interceptor {
  /** Access to the underlying cache for manual operations */
  cache: MemoryCache;
}

/**
 * Create a caching interceptor
 *
 * @example
 * ```typescript
 * const cachePlugin = createCacheInterceptor({ defaultTtl: 30000 });
 *
 * const client = createClient({
 *   baseUrl: 'https://api.example.com',
 *   interceptors: [cachePlugin],
 * });
 *
 * // Manual invalidation
 * cachePlugin.cache.invalidateByTag('users:123');
 * cachePlugin.cache.invalidateByPath('/users');
 * ```
 */
export function createCacheInterceptor(userConfig?: CacheConfig): CacheInterceptor {
  const config = {
    defaultTtl: userConfig?.defaultTtl ?? 30000,
    maxEntries: userConfig?.maxEntries ?? 100,
    cacheMethods: userConfig?.cacheMethods ?? ['GET'],
    generateTags: userConfig?.generateTags ?? defaultGenerateTags,
  };

  const cache = new MemoryCache(config.maxEntries);

  const interceptor: CacheInterceptor = {
    cache,

    async onRequest(request: RequestConfig): Promise<RequestConfig> {
      // Only cache specified methods
      if (!config.cacheMethods.includes(request.method)) {
        return request;
      }

      const key = createCacheKey(request);
      const cached = cache.get(key);

      if (cached !== null) {
        // Attach cached data to request for early return
        (request as RequestConfig & { _cachedData?: unknown })._cachedData = cached;
      }

      return request;
    },

    async onResponse<T>(response: ApiResponse<T>): Promise<ApiResponse<T>> {
      // Cache successful responses
      if (response.status >= 200 && response.status < 300) {
        const request = (response as ApiResponse<T> & { _request?: RequestConfig })._request;
        if (request && config.cacheMethods.includes(request.method)) {
          const key = createCacheKey(request);
          const tags = config.generateTags(request);
          cache.set(key, response.data, config.defaultTtl, tags);
        }
      }

      return response;
    },
  };

  return interceptor;
}

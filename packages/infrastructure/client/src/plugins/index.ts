/**
 * Plugins
 *
 * Extensible plugins for the API client.
 *
 * @example
 * ```typescript
 * import {
 *   createRetryPlugin,
 *   createLoggingInterceptor,
 *   createCacheInterceptor,
 *   createMetricsInterceptor,
 * } from '@game-guild/client/plugins';
 * ```
 */

// Retry plugin
export { createRetryPlugin, createRetryInterceptor, type RetryConfig } from './retry';

// Logging plugin
export { createLoggingInterceptor, type LoggingConfig, type LogLevel, type LoggerFn } from './logging';

// Cache plugin
export { createCacheInterceptor, MemoryCache, type CacheConfig, type CacheInterceptor } from './cache';

// Metrics plugin
export { createMetricsInterceptor, type MetricsConfig, type RequestMetrics, type AggregatedMetrics, type MetricsInterceptor } from './metrics';

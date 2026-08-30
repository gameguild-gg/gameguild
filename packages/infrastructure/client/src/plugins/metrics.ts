/**
 * Metrics Plugin
 *
 * Request/response timing and success rate tracking.
 */

import { err } from '../runtime/result/helpers.js';
import type { Result } from '../runtime/result/types.js';
import type { ApiError } from '../runtime/errors/types.js';
import type { Interceptor, RequestConfig, ApiResponse } from '../runtime/transport/types.js';

/**
 * Request metrics
 */
export interface RequestMetrics {
  /** HTTP method */
  method: string;
  /** Request path */
  path: string;
  /** HTTP status code */
  status: number;
  /** Request duration in milliseconds */
  duration: number;
  /** Timestamp when request completed */
  timestamp: number;
  /** Whether request was successful (2xx) */
  success: boolean;
  /** Request ID for correlation */
  requestId?: string;
  /** Error code if request failed */
  error?: string;
}

/**
 * Aggregated metrics
 */
export interface AggregatedMetrics {
  /** Total requests */
  totalRequests: number;
  /** Successful requests */
  successfulRequests: number;
  /** Failed requests */
  failedRequests: number;
  /** Success rate (0-1) */
  successRate: number;
  /** Average duration in milliseconds */
  averageDuration: number;
  /** P50 duration */
  p50Duration: number;
  /** P95 duration */
  p95Duration: number;
  /** P99 duration */
  p99Duration: number;
  /** Requests by status code */
  byStatus: Record<number, number>;
  /** Requests by path */
  byPath: Record<string, number>;
  /** Errors by error code */
  errorsByCode: Record<string, number>;
}

/**
 * Configuration for metrics plugin
 */
export interface MetricsConfig {
  /** Callback when metrics are collected */
  onMetrics?: (metrics: RequestMetrics) => void;
  /** Maximum metrics to keep in memory */
  maxMetrics?: number;
  /** Whether to include request ID in metrics */
  includeRequestId?: boolean;
}

/**
 * Calculate percentile from sorted array
 */
function percentile(sorted: number[], p: number): number {
  if (sorted.length === 0) return 0;
  const index = Math.ceil((p / 100) * sorted.length) - 1;
  return sorted[Math.max(0, index)];
}

/**
 * Metrics interceptor with metric retrieval methods
 */
export interface MetricsInterceptor extends Interceptor {
  /** Get all collected metrics */
  getMetrics: () => RequestMetrics[];
  /** Get aggregated metrics */
  getAggregatedMetrics: () => AggregatedMetrics;
  /** Clear all metrics */
  clearMetrics: () => void;
}

/**
 * Create a metrics interceptor
 *
 * @example
 * ```typescript
 * const metricsPlugin = createMetricsInterceptor({
 *   onMetrics: (m) => console.log(`${m.method} ${m.path}: ${m.duration}ms`),
 * });
 *
 * const client = createClient({
 *   baseUrl: 'https://api.example.com',
 *   interceptors: [metricsPlugin],
 * });
 *
 * // Get aggregated metrics
 * const stats = metricsPlugin.getAggregatedMetrics();
 * console.log(`Success rate: ${stats.successRate * 100}%`);
 * ```
 */
export function createMetricsInterceptor(userConfig?: MetricsConfig): MetricsInterceptor {
  const config = {
    onMetrics: userConfig?.onMetrics,
    maxMetrics: userConfig?.maxMetrics ?? 1000,
    includeRequestId: userConfig?.includeRequestId ?? true,
  };

  const metrics: RequestMetrics[] = [];
  const requestTimes = new Map<string, { startTime: number; requestId?: string; method: string; path: string }>();

  const interceptor: MetricsInterceptor = {
    getMetrics: () => [...metrics],

    getAggregatedMetrics: (): AggregatedMetrics => {
      const total = metrics.length;
      const successful = metrics.filter((m) => m.success).length;
      const durations = metrics.map((m) => m.duration).sort((a, b) => a - b);

      const byStatus: Record<number, number> = {};
      const byPath: Record<string, number> = {};
      const errorsByCode: Record<string, number> = {};

      for (const m of metrics) {
        byStatus[m.status] = (byStatus[m.status] || 0) + 1;
        byPath[m.path] = (byPath[m.path] || 0) + 1;
        if (m.error) {
          errorsByCode[m.error] = (errorsByCode[m.error] || 0) + 1;
        }
      }

      return {
        totalRequests: total,
        successfulRequests: successful,
        failedRequests: total - successful,
        successRate: total > 0 ? successful / total : 0,
        averageDuration: total > 0 ? durations.reduce((a, b) => a + b, 0) / total : 0,
        p50Duration: percentile(durations, 50),
        p95Duration: percentile(durations, 95),
        p99Duration: percentile(durations, 99),
        byStatus,
        byPath,
        errorsByCode,
      };
    },

    clearMetrics: () => {
      metrics.length = 0;
      requestTimes.clear();
    },

    async onRequest(request: RequestConfig): Promise<RequestConfig> {
      const requestId = (request as RequestConfig & { _requestId?: string })._requestId;
      const key = `${request.method}:${request.path}:${Date.now()}:${Math.random()}`;

      requestTimes.set(key, {
        startTime: performance.now(),
        requestId,
        method: request.method,
        path: request.path,
      });

      (request as RequestConfig & { _metricsKey?: string })._metricsKey = key;
      return request;
    },

    async onResponse<T>(response: ApiResponse<T>): Promise<ApiResponse<T>> {
      // Extract the metrics key we attached during onRequest
      const metricsKey = (response as ApiResponse<T> & { _metricsKey?: string })._metricsKey;
      if (!metricsKey) return response;

      const timing = requestTimes.get(metricsKey);
      if (!timing) return response;

      requestTimes.delete(metricsKey);

      const metric: RequestMetrics = {
        method: timing.method,
        path: timing.path,
        status: response.status,
        duration: performance.now() - timing.startTime,
        timestamp: Date.now(),
        success: response.status >= 200 && response.status < 300,
        requestId: config.includeRequestId ? timing.requestId : undefined,
      };

      if (metrics.length >= config.maxMetrics) {
        metrics.shift();
      }
      metrics.push(metric);
      config.onMetrics?.(metric);

      return response;
    },

    async onError(error: ApiError): Promise<Result<never, ApiError>> {
      // Extract the metrics key if available
      const metricsKey = (error as ApiError & { _metricsKey?: string })._metricsKey;
      if (metricsKey) {
        const timing = requestTimes.get(metricsKey);
        /* v8 ignore start */
        if (timing) {
          /* v8 ignore stop */
          requestTimes.delete(metricsKey);

          const metric: RequestMetrics = {
            method: timing.method,
            path: timing.path,
            /* v8 ignore start */
            status: error.status || 0,
            /* v8 ignore stop */
            duration: performance.now() - timing.startTime,
            timestamp: Date.now(),
            success: false,
            error: error.code,
            /* v8 ignore start */
            requestId: config.includeRequestId ? timing.requestId : undefined,
            /* v8 ignore stop */
          };

          if (metrics.length >= config.maxMetrics) {
            metrics.shift();
          }
          metrics.push(metric);
          config.onMetrics?.(metric);
        }
      }

      return err(error);
    },
  };

  return interceptor;
}

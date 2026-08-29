/**
 * DevTools Integration
 *
 * Development mode logging and debugging utilities
 */

import type { RequestConfig } from '../transport/types.js';
import type { Result } from '../result/types.js';
import type { ApiError } from '../errors/types.js';

export interface DevToolsConfig {
  /**
   * Enable DevTools (default: true in development)
   */
  enabled?: boolean;

  /**
   * Log level
   */
  logLevel?: 'silent' | 'error' | 'warn' | 'info' | 'debug';

  /**
   * Collapse log groups by default
   */
  collapsed?: boolean;

  /**
   * Custom logger
   */
  logger?: DevToolsLogger;
}

export interface DevToolsLogger {
  group(label: string, collapsed?: boolean): void;
  groupEnd(): void;
  log(message: string, ...args: unknown[]): void;
  warn(message: string, ...args: unknown[]): void;
  error(message: string, ...args: unknown[]): void;
  info(message: string, ...args: unknown[]): void;
  debug(message: string, ...args: unknown[]): void;
}

/**
 * Console logger implementation
 */
class ConsoleLogger implements DevToolsLogger {
  constructor(private logLevel: DevToolsConfig['logLevel'] = 'info') {}

  group(label: string, collapsed = false): void {
    if (collapsed) {
      console.groupCollapsed(label);
    } else {
      console.group(label);
    }
  }

  groupEnd(): void {
    console.groupEnd();
  }

  /* v8 ignore next 3 */
  log(message: string, ...args: unknown[]): void {
    console.log(message, ...args);
  }

  warn(message: string, ...args: unknown[]): void {
    if (this.logLevel !== 'silent' && this.logLevel !== 'error') {
      console.warn(message, ...args);
    }
  }

  error(message: string, ...args: unknown[]): void {
    if (this.logLevel !== 'silent') {
      console.error(message, ...args);
    }
  }

  info(message: string, ...args: unknown[]): void {
    if (this.logLevel === 'info' || this.logLevel === 'debug') {
      console.info(message, ...args);
    }
  }

  debug(message: string, ...args: unknown[]): void {
    if (this.logLevel === 'debug') {
      console.log(message, ...args);
    }
  }
}

/**
 * DevTools manager
 */
export class DevTools {
  private logger: DevToolsLogger;
  private collapsed: boolean;
  private requestTimes = new Map<string, number>();

  constructor(config: DevToolsConfig = {}) {
    const enabled =
      config.enabled ?? (typeof process !== 'undefined' ? process.env.NODE_ENV === 'development' : /* v8 ignore start */ false); /* v8 ignore stop */

    if (!enabled) {
      // Use silent logger if disabled
      this.logger = new ConsoleLogger('silent');
    } else {
      this.logger = config.logger || new ConsoleLogger(config.logLevel);
    }

    this.collapsed = config.collapsed ?? true;
  }

  /**
   * Log request start
   */
  logRequestStart(config: RequestConfig): void {
    const method = config.method || 'GET';
    const path = config.path || '/';
    const requestId = config.requestId || 'unknown';

    this.requestTimes.set(requestId, Date.now());

    const emoji = this.getMethodEmoji(method);
    this.logger.group(`${emoji} ${method} ${path}`, this.collapsed);

    this.logger.info('Request ID:', requestId);

    if (config.params && Object.keys(config.params).length > 0) {
      this.logger.debug('Query:', config.params);
    }

    if (config.headers && Object.keys(config.headers).length > 0) {
      this.logger.debug('Headers:', this.sanitizeHeaders(config.headers));
    }

    if (config.body) {
      this.logger.debug('Body:', config.body);
    }
  }

  /**
   * Log request completion
   */
  logRequestComplete<T>(config: RequestConfig, result: Result<T, ApiError>, duration?: number): void {
    const requestId = config.requestId || 'unknown';
    const startTime = this.requestTimes.get(requestId);
    const actualDuration = duration ?? (startTime ? Date.now() - startTime : 0);

    if (result.ok) {
      this.logger.info(`✅ Success (${actualDuration}ms)`);
      this.logger.debug('Response:', result.data);
    } else {
      this.logger.error(`❌ Error (${actualDuration}ms)`);
      this.logger.error('Error:', result.error);
    }

    this.logger.groupEnd();
    this.requestTimes.delete(requestId);
  }

  /**
   * Log validation error
   */
  logValidationError(context: 'request' | 'response', error: unknown): void {
    this.logger.error(`⚠️ ${context === 'request' ? 'Request' : 'Response'} validation failed:`, error);
  }

  /**
   * Log cache hit
   */
  logCacheHit(key: string): void {
    this.logger.debug('💾 Cache hit:', key);
  }

  /**
   * Log cache miss
   */
  logCacheMiss(key: string): void {
    this.logger.debug('🔍 Cache miss:', key);
  }

  /**
   * Log retry attempt
   */
  logRetry(attempt: number, maxRetries: number, error: ApiError): void {
    this.logger.warn(`🔄 Retry attempt ${attempt}/${maxRetries}`, error);
  }

  /**
   * Log deduplication
   */
  logDeduplication(key: string): void {
    this.logger.debug('🔗 Deduplicated request:', key);
  }

  /**
   * Get emoji for HTTP method
   */
  private getMethodEmoji(method: string): string {
    switch (method.toUpperCase()) {
      case 'GET':
        return '🔍';
      case 'POST':
        return '✉️';
      case 'PUT':
        return '📝';
      case 'PATCH':
        return '🔧';
      case 'DELETE':
        return '🗑️';
      default:
        return '🌐';
    }
  }

  /**
   * Sanitize headers to hide sensitive data
   */
  private sanitizeHeaders(headers: Record<string, string>): Record<string, string> {
    const sanitized = { ...headers };
    const sensitiveKeys = ['authorization', 'cookie', 'x-api-key', 'x-auth-token'];

    for (const key of Object.keys(sanitized)) {
      if (sensitiveKeys.includes(key.toLowerCase())) {
        sanitized[key] = '[REDACTED]';
      }
    }

    return sanitized;
  }
}

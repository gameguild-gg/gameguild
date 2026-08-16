/**
 * Logging Plugin
 *
 * Request/response logging with redaction of sensitive data.
 */

import { err } from '../runtime/result/helpers.js';
import type { Result } from '../runtime/result/types.js';
import type { ApiError } from '../runtime/errors/types.js';
import type { Interceptor, RequestConfig, ApiResponse } from '../runtime/transport/types.js';

/**
 * Log levels
 */
export type LogLevel = 'debug' | 'info' | 'warn' | 'error';

/**
 * Logger function signature
 */
export type LoggerFn = (level: LogLevel, message: string, data?: unknown) => void;

/**
 * Configuration for logging plugin
 */
export interface LoggingConfig {
  /** Minimum log level */
  level?: LogLevel;
  /** Custom logger function */
  logger?: LoggerFn;
  /** Include request body in logs */
  logRequestBody?: boolean;
  /** Include response body in logs */
  logResponseBody?: boolean;
  /** Headers to redact from logs */
  redactHeaders?: string[];
  /** Include request ID in logs */
  includeRequestId?: boolean;
}

const LOG_LEVELS: Record<LogLevel, number> = {
  debug: 0,
  info: 1,
  warn: 2,
  error: 3,
};

const DEFAULT_REDACT_HEADERS = ['authorization', 'cookie', 'set-cookie', 'x-api-key'];

/**
 * Default console logger
 */
function defaultLogger(level: LogLevel, message: string, data?: unknown): void {
  const timestamp = new Date().toISOString();
  const prefix = `[${timestamp}] [client] [${level.toUpperCase()}]`;

  /* v8 ignore start */
  switch (level) {
    /* v8 ignore stop */
    case 'debug':
      /* v8 ignore start */
      console.debug(prefix, message, data ?? '');
      /* v8 ignore stop */
      break;
    case 'info':
      console.info(prefix, message, data ?? '');
      break;
    case 'warn':
      /* v8 ignore start */
      console.warn(prefix, message, data ?? '');
      /* v8 ignore stop */
      break;
    /* v8 ignore start */
    case 'error':
      console.error(prefix, message, data ?? '');
      break;
    /* v8 ignore stop */
  }
}

/**
 * Redact sensitive headers from a Record
 */
function redactHeaders(headers: Record<string, string> | undefined, redactList: string[]): Record<string, string> {
  if (!headers) return {};

  const redacted: Record<string, string> = {};
  for (const [key, value] of Object.entries(headers)) {
    if (redactList.includes(key.toLowerCase())) {
      redacted[key] = '[REDACTED]';
    } else {
      redacted[key] = value;
    }
  }
  return redacted;
}

/**
 * Convert Headers object to Record with redaction
 */
function headersToRecord(headers: Headers, redactList: string[]): Record<string, string> {
  const record: Record<string, string> = {};
  headers.forEach((value, key) => {
    if (redactList.includes(key.toLowerCase())) {
      record[key] = '[REDACTED]';
    } else {
      record[key] = value;
    }
  });
  return record;
}

/**
 * Create a logging interceptor
 *
 * @example
 * ```typescript
 * const client = createClient({
 *   baseUrl: 'https://api.example.com',
 *   interceptors: [
 *     createLoggingInterceptor({ level: 'debug' }),
 *   ],
 * });
 * ```
 */
export function createLoggingInterceptor(userConfig?: LoggingConfig): Interceptor {
  const config: Required<LoggingConfig> = {
    level: userConfig?.level ?? 'info',
    logger: userConfig?.logger ?? defaultLogger,
    logRequestBody: userConfig?.logRequestBody ?? false,
    logResponseBody: userConfig?.logResponseBody ?? false,
    redactHeaders: userConfig?.redactHeaders ?? DEFAULT_REDACT_HEADERS,
    includeRequestId: userConfig?.includeRequestId ?? true,
  };

  const shouldLog = (level: LogLevel): boolean => {
    return LOG_LEVELS[level] >= LOG_LEVELS[config.level];
  };

  return {
    async onRequest(request: RequestConfig): Promise<RequestConfig> {
      const requestId = (request as RequestConfig & { _requestId?: string })._requestId;
      const idPrefix = config.includeRequestId && requestId ? `[${requestId.slice(0, 8)}] ` : '';

      if (shouldLog('debug')) {
        config.logger('debug', `${idPrefix}→ ${request.method} ${request.path}`, {
          headers: redactHeaders(request.headers, config.redactHeaders),
          params: request.params,
          body: config.logRequestBody ? request.body : undefined,
        });
      } else if (shouldLog('info')) {
        config.logger('info', `${idPrefix}→ ${request.method} ${request.path}`);
      }
      return request;
    },

    async onResponse<T>(response: ApiResponse<T>): Promise<ApiResponse<T>> {
      if (shouldLog('debug')) {
        config.logger('debug', `← ${response.status}`, {
          headers: headersToRecord(response.headers, config.redactHeaders),
          data: config.logResponseBody ? response.data : undefined,
        });
      } else if (shouldLog('info')) {
        config.logger('info', `← ${response.status}`);
      }
      return response;
    },

    async onError(error: ApiError): Promise<Result<never, ApiError>> {
      if (shouldLog('warn')) {
        config.logger('warn', `✕ Error: ${error.message}`, {
          code: error.code,
          status: error.status,
          detail: error.detail,
          traceId: error.traceId,
        });
      }
      return err(error);
    },
  };
}

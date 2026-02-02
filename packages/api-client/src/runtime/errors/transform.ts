/**
 * Error Transformation
 *
 * Transform HTTP responses into typed ApiError instances.
 */

import type { ApiError, ApiErrorCode } from './types.js';

/**
 * ProblemDetails response from ASP.NET
 */
interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/**
 * Create an ApiError from an HTTP response
 */
export async function createApiError(response: Response): Promise<ApiError> {
  const status = response.status;
  let body: ProblemDetails | null = null;

  try {
    body = await response.json();
  } catch {
    // Response may not have a body
  }

  const code = statusToErrorCode(status, body);
  const message = body?.title || body?.detail || response.statusText || 'Unknown error';

  const error: ApiError = {
    name: 'ApiError',
    message,
    status,
    code,
    detail: body?.detail,
    traceId: body?.traceId,
  };

  // Add field errors for validation errors
  if (code === 'VALIDATION_ERROR' && body?.errors) {
    return {
      ...error,
      fieldErrors: body.errors,
    } as ApiError & { fieldErrors: Record<string, string[]> };
  }

  // Add retry-after for rate limits
  if (code === 'RATE_LIMITED') {
    const retryAfter = response.headers.get('Retry-After');
    if (retryAfter) {
      return {
        ...error,
        retryAfter: parseInt(retryAfter, 10),
      } as ApiError & { retryAfter: number };
    }
  }

  return error;
}

/**
 * Create a network error
 */
export function createNetworkError(cause: unknown): ApiError {
  const isTimeout = cause instanceof Error && cause.name === 'AbortError';

  return {
    name: 'ApiError',
    message: isTimeout ? 'Request timed out' : 'Network error',
    status: 0,
    code: isTimeout ? 'TIMEOUT' : 'NETWORK_ERROR',
    cause,
  };
}

/**
 * Map HTTP status code to error code
 */
function statusToErrorCode(status: number, body?: ProblemDetails | null): ApiErrorCode {
  // Check for specific error codes in the response
  const errorType = body?.type?.toLowerCase() || '';

  switch (status) {
    case 400:
      if (body?.errors && Object.keys(body.errors).length > 0) {
        return 'VALIDATION_ERROR';
      }
      return 'VALIDATION_ERROR';

    case 401:
      if (errorType.includes('expired')) return 'TOKEN_EXPIRED';
      if (errorType.includes('invalid')) return 'TOKEN_INVALID';
      if (errorType.includes('missing')) return 'TOKEN_MISSING';
      return 'AUTHENTICATION_ERROR';

    case 403:
      if (errorType.includes('permission')) return 'INSUFFICIENT_PERMISSIONS';
      if (errorType.includes('feature')) return 'FEATURE_NOT_AVAILABLE';
      return 'FORBIDDEN';

    case 404:
      return 'NOT_FOUND';

    case 409:
      return 'CONFLICT';

    case 429:
      return 'RATE_LIMITED';

    case 500:
    case 502:
    case 503:
    case 504:
      return 'SERVER_ERROR';

    default:
      return 'UNKNOWN';
  }
}

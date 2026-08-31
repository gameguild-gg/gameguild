/**
 * Error Type Guards
 *
 * Type-safe guards for checking error types.
 */

import type {
  ApiError,
  AuthenticationError,
  AuthorizationError,
  ConflictError,
  NetworkError,
  NotFoundError,
  RateLimitError,
  ServerError,
  ValidationError,
} from './types.js';

/**
 * Check if error is an ApiError
 */
export function isApiError(error: unknown): error is ApiError {
  return typeof error === 'object' && error !== null && 'name' in error && error.name === 'ApiError' && 'status' in error && 'code' in error;
}

/**
 * Check if error is a validation error (400)
 */
export function isValidationError(error: unknown): error is ValidationError {
  return isApiError(error) && error.code === 'VALIDATION_ERROR' && error.status === 400;
}

/**
 * Check if error is an authentication error (401)
 */
export function isAuthenticationError(error: unknown): error is AuthenticationError {
  return isApiError(error) && error.status === 401;
}

/**
 * Check if error is specifically "unauthorized" (401)
 */
export function isUnauthorized(error: unknown): error is AuthenticationError {
  return isAuthenticationError(error);
}

/**
 * Check if error is a token expired error
 */
export function isTokenExpired(error: unknown): error is AuthenticationError {
  return isApiError(error) && error.code === 'TOKEN_EXPIRED';
}

/**
 * Check if error is an authorization error (403)
 */
export function isAuthorizationError(error: unknown): error is AuthorizationError {
  return isApiError(error) && error.status === 403;
}

/**
 * Check if error is specifically "forbidden" (403)
 */
export function isForbidden(error: unknown): error is AuthorizationError {
  return isAuthorizationError(error);
}

/**
 * Check if error is due to insufficient permissions
 */
export function isInsufficientPermissions(error: unknown): error is AuthorizationError {
  return isApiError(error) && error.code === 'INSUFFICIENT_PERMISSIONS';
}

/**
 * Check if error is due to unavailable feature
 */
export function isFeatureNotAvailable(error: unknown): error is AuthorizationError {
  return isApiError(error) && error.code === 'FEATURE_NOT_AVAILABLE';
}

/**
 * Check if error is a not found error (404)
 */
export function isNotFoundError(error: unknown): error is NotFoundError {
  return isApiError(error) && error.code === 'NOT_FOUND' && error.status === 404;
}

/**
 * Check if error is a conflict error (409)
 */
export function isConflictError(error: unknown): error is ConflictError {
  return isApiError(error) && error.code === 'CONFLICT' && error.status === 409;
}

/**
 * Check if error is a rate limit error (429)
 */
export function isRateLimitError(error: unknown): error is RateLimitError {
  return isApiError(error) && error.code === 'RATE_LIMITED' && error.status === 429;
}

/**
 * Check if error is a network error
 */
export function isNetworkError(error: unknown): error is NetworkError {
  return isApiError(error) && (error.code === 'NETWORK_ERROR' || error.code === 'TIMEOUT');
}

/**
 * Check if error is a server error (5xx)
 */
export function isServerError(error: unknown): error is ServerError {
  return isApiError(error) && error.status >= 500 && error.status < 600;
}

/**
 * Check if error is retryable
 */
export function isRetryableError(error: unknown): boolean {
  if (!isApiError(error)) return false;

  // Network errors and timeouts are retryable
  if (isNetworkError(error)) return true;

  // Rate limits are retryable (with backoff)
  if (isRateLimitError(error)) return true;

  // Some server errors are retryable
  if (error.status === 502 || error.status === 503 || error.status === 504) {
    return true;
  }

  return false;
}

/**
 * Get required permissions from an authorization error
 */
export function getRequiredPermissions(error: unknown): string[] | undefined {
  if (isAuthorizationError(error)) {
    return error.requiredPermissions;
  }
  return undefined;
}

/**
 * Get required feature from a feature not available error
 */
export function getRequiredFeature(error: unknown): string | undefined {
  if (isFeatureNotAvailable(error)) {
    return error.requiredFeature;
  }
  return undefined;
}

/**
 * Get retry-after value from rate limit error (in seconds)
 */
export function getRetryAfter(error: unknown): number | undefined {
  if (isRateLimitError(error)) {
    return error.retryAfter;
  }
  return undefined;
}

/**
 * Errors Module
 *
 * Re-exports all error-related types and utilities.
 */

export type {
  ApiError,
  ApiErrorCode,
  AuthenticationError,
  AuthorizationError,
  ConflictError,
  NetworkError,
  NotFoundError,
  RateLimitError,
  RequiredPermission,
  ServerError,
  SpecificApiError,
  ValidationError,
} from './types.js';

export {
  isApiError,
  isAuthenticationError,
  isAuthorizationError,
  isConflictError,
  isFeatureNotAvailable,
  isForbidden,
  isInsufficientPermissions,
  isNetworkError,
  isNotFoundError,
  isRateLimitError,
  isRetryableError,
  isServerError,
  isTokenExpired,
  isUnauthorized,
  isValidationError,
  getRequiredFeature,
  getRequiredPermissions,
  getRetryAfter,
} from './guards.js';

export { createApiError, createNetworkError } from './transform.js';
export { transformZodError, isZodError, safeParse } from './validation.js';

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
} from './types';

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
} from './guards';

export { createApiError, createNetworkError } from './transform';
export { transformZodError, isZodError, safeParse } from './validation';

/**
 * Runtime Module
 *
 * Re-exports all runtime functionality.
 */

// Result types
export type { Result, Ok, Err, ResultData, ResultError } from './result/types';
export { ok, err, isOk, isErr, unwrap, unwrapOr, unwrapOrElse, map, mapErr, flatMap, match, fromPromise, toPromise } from './result/helpers';

// Error types
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
} from './errors/types';

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
  createApiError,
  createNetworkError,
} from './errors/index';

// Transport types
export type {
  ApiResponse,
  HttpMethod,
  Interceptor,
  RequestConfig,
  RequestInterceptor,
  ResponseInterceptor,
  Transport,
  TransportConfig,
} from './transport/types';

export { createFetchTransport, createHeaderInterceptor } from './transport/fetch';

// Auth types
export type { AuthConfig, AuthMode, TokenPair, TokenProvider } from './auth/types';
export { TokenRefreshManager, type TokenRefreshConfig } from './auth/refresh';

// Tenant types
export type { TenantConfig, TenantProvider } from './tenant/types';

// Client types
export type { ApiClient, RequestExecutor } from './client';

/**
 * Runtime Module
 *
 * Re-exports all runtime functionality.
 */

// Result types
export type { Result, Ok, Err, ResultData, ResultError } from './result/types.js';
export { ok, err, isOk, isErr, unwrap, unwrapOr, unwrapOrElse, map, mapErr, flatMap, match, fromPromise, toPromise } from './result/helpers.js';

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
} from './errors/types.js';

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
} from './errors/index.js';

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
} from './transport/types.js';

export { createFetchTransport, createHeaderInterceptor } from './transport/fetch.js';

// Auth types
export type { AuthConfig, AuthMode, TokenPair, TokenProvider } from './auth/types.js';
export { TokenRefreshManager, type TokenRefreshConfig } from './auth/refresh.js';

// Tenant types
export type { TenantConfig, TenantProvider } from './tenant/types.js';

// Client types
export type { ApiClient, RequestExecutor } from './client.js';

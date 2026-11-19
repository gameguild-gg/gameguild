/**
 * API Error Types
 *
 * Type definitions for API errors with discriminated unions.
 */

/**
 * Base API error
 */
export interface ApiError {
  readonly name: 'ApiError';
  readonly message: string;
  readonly status: number;
  readonly code: ApiErrorCode;
  readonly detail?: string;
  readonly traceId?: string;
  readonly cause?: unknown;
  /** Additional metadata for plugins/middleware */
  readonly metadata?: Record<string, unknown>;
}

/**
 * Error codes for programmatic handling
 */
export type ApiErrorCode =
  | 'VALIDATION_ERROR'
  | 'AUTHENTICATION_ERROR'
  | 'TOKEN_EXPIRED'
  | 'TOKEN_INVALID'
  | 'TOKEN_MISSING'
  | 'FORBIDDEN'
  | 'INSUFFICIENT_PERMISSIONS'
  | 'FEATURE_NOT_AVAILABLE'
  | 'NOT_FOUND'
  | 'CONFLICT'
  | 'RATE_LIMITED'
  | 'SERVER_ERROR'
  | 'NETWORK_ERROR'
  | 'TIMEOUT'
  | 'PARSE_ERROR'
  | 'UNKNOWN';

/**
 * Validation error with field-level errors
 */
export interface ValidationError extends ApiError {
  readonly code: 'VALIDATION_ERROR';
  readonly status: 400;
  readonly fieldErrors: Record<string, string[]>;
}

/**
 * Authentication error (401)
 */
export interface AuthenticationError extends ApiError {
  readonly code: 'AUTHENTICATION_ERROR' | 'TOKEN_EXPIRED' | 'TOKEN_INVALID' | 'TOKEN_MISSING';
  readonly status: 401;
}

/**
 * Authorization error (403)
 */
export interface AuthorizationError extends ApiError {
  readonly code: 'FORBIDDEN' | 'INSUFFICIENT_PERMISSIONS' | 'FEATURE_NOT_AVAILABLE';
  readonly status: 403;
  readonly requiredPermissions?: string[];
  readonly currentPermissions?: string[];
  readonly requiredFeature?: string;
  readonly tenantId?: string;
}

/**
 * Not found error (404)
 */
export interface NotFoundError extends ApiError {
  readonly code: 'NOT_FOUND';
  readonly status: 404;
  readonly resourceType?: string;
  readonly resourceId?: string;
}

/**
 * Conflict error (409)
 */
export interface ConflictError extends ApiError {
  readonly code: 'CONFLICT';
  readonly status: 409;
}

/**
 * Rate limit error (429)
 */
export interface RateLimitError extends ApiError {
  readonly code: 'RATE_LIMITED';
  readonly status: 429;
  readonly retryAfter?: number;
}

/**
 * Network error (no HTTP response)
 */
export interface NetworkError extends ApiError {
  readonly code: 'NETWORK_ERROR' | 'TIMEOUT';
  readonly status: 0;
}

/**
 * Server error (5xx)
 */
export interface ServerError extends ApiError {
  readonly code: 'SERVER_ERROR';
  readonly status: 500 | 502 | 503 | 504;
}

/**
 * Union of all specific error types
 */
export type SpecificApiError =
  ValidationError | AuthenticationError | AuthorizationError | NotFoundError | ConflictError | RateLimitError | NetworkError | ServerError;

/**
 * Required permission structure
 */
export interface RequiredPermission {
  readonly resource: string;
  readonly action: string;
  readonly scope?: string;
}

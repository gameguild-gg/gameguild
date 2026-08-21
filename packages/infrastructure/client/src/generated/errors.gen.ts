/**
 * @game-guild/client - Generated Error Types
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 *
 * Generated from: GameGuild API
 * API Version: 4.3.0
 */
/**
 * These types extend the base error types from the runtime.
 */

/* eslint-disable @typescript-eslint/no-explicit-any */
/**
 * Base API error from the server
 */
export interface ApiErrorResponse {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/**
 * Validation error response (HTTP 400)
 */
export interface ValidationErrorResponse extends ApiErrorResponse {
  status: 400;
  errors: Record<string, string[]>;
}

/**
 * Authentication error response (HTTP 401)
 */
export interface AuthenticationErrorResponse extends ApiErrorResponse {
  status: 401;
  code?: "TOKEN_EXPIRED" | "TOKEN_INVALID" | "TOKEN_MISSING";
}

/**
 * Authorization error response (HTTP 403)
 */
export interface AuthorizationErrorResponse extends ApiErrorResponse {
  status: 403;
  code?: "FORBIDDEN" | "INSUFFICIENT_PERMISSIONS" | "FEATURE_NOT_AVAILABLE";
  requiredPermissions?: string[];
  requiredFeature?: string;
}

/**
 * Not found error response (HTTP 404)
 */
export interface NotFoundErrorResponse extends ApiErrorResponse {
  status: 404;
  resourceType?: string;
  resourceId?: string;
}

/**
 * Conflict error response (HTTP 409)
 */
export interface ConflictErrorResponse extends ApiErrorResponse {
  status: 409;
  conflictReason?: string;
}

/**
 * Rate limit error response (HTTP 429)
 */
export interface RateLimitErrorResponse extends ApiErrorResponse {
  status: 429;
  retryAfter?: number;
}

/**
 * Server error response (HTTP 5xx)
 */
export interface ServerErrorResponse extends ApiErrorResponse {
  status: 500 | 502 | 503 | 504;
}

/**
 * Union of all possible error responses
 */
export type ErrorResponse =
  | ValidationErrorResponse
  | AuthenticationErrorResponse
  | AuthorizationErrorResponse
  | NotFoundErrorResponse
  | ConflictErrorResponse
  | RateLimitErrorResponse
  | ServerErrorResponse
  | ApiErrorResponse;

/**
 * Error codes for programmatic handling
 */
export type ApiErrorCode =
  | "VALIDATION_ERROR"
  | "AUTHENTICATION_ERROR"
  | "TOKEN_EXPIRED"
  | "TOKEN_INVALID"
  | "TOKEN_MISSING"
  | "FORBIDDEN"
  | "INSUFFICIENT_PERMISSIONS"
  | "FEATURE_NOT_AVAILABLE"
  | "NOT_FOUND"
  | "CONFLICT"
  | "RATE_LIMITED"
  | "SERVER_ERROR"
  | "NETWORK_ERROR"
  | "TIMEOUT"
  | "UNKNOWN";

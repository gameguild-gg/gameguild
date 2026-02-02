/**
 * Error Type Code Generator
 *
 * Generates TypeScript error types for API responses.
 */

import type { OpenApiSpec } from '../fetch-spec.js';
import type { OpenAPIV3 } from 'openapi-types';
import { BaseGenerator } from './core/BaseGenerator.js';
import { ERROR_STATUS_CODES } from './constants.js';

/**
 * Generate error type definitions
 */
export function generateErrors(spec: OpenApiSpec): string {
  const generator = new ErrorsGenerator(spec);
  return generator.generate();
}

class ErrorsGenerator extends BaseGenerator {
  protected getFileDescription(): string {
    return 'Error Types';
  }

  protected generateImports(): string {
    return `/**
 * These types extend the base error types from the runtime.
 */

/* eslint-disable @typescript-eslint/no-explicit-any */`;
  }

  protected generateContent(): string {
    const lines: string[] = [];

    // Generate base error interface from ProblemDetails pattern
    lines.push(this.generateBaseErrorInterface());
    lines.push('');

    // Generate specific error types
    lines.push(this.generateValidationError());
    lines.push('');
    lines.push(this.generateAuthenticationError());
    lines.push('');
    lines.push(this.generateAuthorizationError());
    lines.push('');
    lines.push(this.generateNotFoundError());
    lines.push('');
    lines.push(this.generateConflictError());
    lines.push('');
    lines.push(this.generateRateLimitError());
    lines.push('');
    lines.push(this.generateServerError());
    lines.push('');

    // Generate union type
    lines.push(this.generateErrorUnion());
    lines.push('');

    // Generate error codes
    lines.push(this.generateErrorCodes());

    return lines.join('\n');
  }

  private generateBaseErrorInterface(): string {
    return `/**
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
}`;
  }

  private generateValidationError(): string {
    return `/**
 * Validation error response (HTTP ${ERROR_STATUS_CODES.BAD_REQUEST})
 */
export interface ValidationErrorResponse extends ApiErrorResponse {
  status: ${ERROR_STATUS_CODES.BAD_REQUEST};
  errors: Record<string, string[]>;
}`;
  }

  private generateAuthenticationError(): string {
    return `/**
 * Authentication error response (HTTP ${ERROR_STATUS_CODES.UNAUTHORIZED})
 */
export interface AuthenticationErrorResponse extends ApiErrorResponse {
  status: ${ERROR_STATUS_CODES.UNAUTHORIZED};
  code?: 'TOKEN_EXPIRED' | 'TOKEN_INVALID' | 'TOKEN_MISSING';
}`;
  }

  private generateAuthorizationError(): string {
    return `/**
 * Authorization error response (HTTP ${ERROR_STATUS_CODES.FORBIDDEN})
 */
export interface AuthorizationErrorResponse extends ApiErrorResponse {
  status: ${ERROR_STATUS_CODES.FORBIDDEN};
  code?: 'FORBIDDEN' | 'INSUFFICIENT_PERMISSIONS' | 'FEATURE_NOT_AVAILABLE';
  requiredPermissions?: string[];
  requiredFeature?: string;
}`;
  }

  private generateNotFoundError(): string {
    return `/**
 * Not found error response (HTTP ${ERROR_STATUS_CODES.NOT_FOUND})
 */
export interface NotFoundErrorResponse extends ApiErrorResponse {
  status: ${ERROR_STATUS_CODES.NOT_FOUND};
  resourceType?: string;
  resourceId?: string;
}`;
  }

  private generateConflictError(): string {
    return `/**
 * Conflict error response (HTTP ${ERROR_STATUS_CODES.CONFLICT})
 */
export interface ConflictErrorResponse extends ApiErrorResponse {
  status: ${ERROR_STATUS_CODES.CONFLICT};
  conflictReason?: string;
}`;
  }

  private generateRateLimitError(): string {
    return `/**
 * Rate limit error response (HTTP ${ERROR_STATUS_CODES.TOO_MANY_REQUESTS})
 */
export interface RateLimitErrorResponse extends ApiErrorResponse {
  status: ${ERROR_STATUS_CODES.TOO_MANY_REQUESTS};
  retryAfter?: number;
}`;
  }

  private generateServerError(): string {
    const serverCodes = [
      ERROR_STATUS_CODES.INTERNAL_SERVER_ERROR,
      ERROR_STATUS_CODES.BAD_GATEWAY,
      ERROR_STATUS_CODES.SERVICE_UNAVAILABLE,
      ERROR_STATUS_CODES.GATEWAY_TIMEOUT,
    ].join(' | ');

    return `/**
 * Server error response (HTTP 5xx)
 */
export interface ServerErrorResponse extends ApiErrorResponse {
  status: ${serverCodes};
}`;
  }

  private generateErrorUnion(): string {
    return `/**
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
  | ApiErrorResponse;`;
  }

  private generateErrorCodes(): string {
    return `/**
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
  | 'UNKNOWN';`;
  }
}

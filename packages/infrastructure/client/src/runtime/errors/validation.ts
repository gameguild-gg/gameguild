/**
 * Validation Error Transformation
 *
 * Transforms Zod validation errors into user-friendly API error format
 */

import { ZodError, ZodIssue } from 'zod';
import type { ApiError } from './types.js';

export interface ValidationErrorDetail {
  field: string;
  message: string;
  code: string;
  value?: unknown;
}

/**
 * Transform Zod error into ValidationError
 */
export function transformZodError(error: ZodError, context?: 'request' | 'response'): ApiError {
  const errors = error.issues.map(transformZodIssue);

  return {
    name: 'ApiError',
    code: 'VALIDATION_ERROR',
    status: 400,
    message: context === 'request'
      ? 'Request validation failed'
      : 'Response validation failed',
    metadata: {
      errors,
      context: context || 'unknown',
      timestamp: new Date().toISOString(),
    },
  };
}

/**
 * Transform individual Zod issue into validation error detail
 */
function transformZodIssue(issue: ZodIssue): ValidationErrorDetail {
  const field = issue.path.map(String).join('.');

  return {
    field: field || 'root',
    message: formatIssueMessage(issue),
    code: issue.code,
    value: 'input' in issue ? issue.input : undefined,
  };
}

/**
 * Format Zod issue message for better UX
 */
function formatIssueMessage(issue: ZodIssue): string {
  const field = issue.path.map(String).join('.');

  switch (issue.code) {
    case 'invalid_type':
      return `${field || 'Value'} must be ${issue.expected}, received ${JSON.stringify(issue.input)}`;

    case 'invalid_format':
      if (issue.format === 'email') {
        return `${field || 'Value'} must be a valid email address`;
      }
      if (issue.format === 'url') {
        /* v8 ignore start */
        return `${field || 'Value'} must be a valid URL`;
        /* v8 ignore stop */
      }
      if (issue.format === 'uuid') {
        /* v8 ignore start */
        return `${field || 'Value'} must be a valid UUID`;
        /* v8 ignore stop */
      }
      if (issue.format === 'datetime') {
        /* v8 ignore start */
        return `${field || 'Value'} must be a valid ISO datetime`;
        /* v8 ignore stop */
      }
      return issue.message;

    case 'too_small':
      if (issue.origin === 'string') {
        return `${field || 'Value'} must be at least ${issue.minimum} characters`;
      }
      if (issue.origin === 'number') {
        /* v8 ignore start */
        return `${field || 'Value'} must be at least ${issue.minimum}`;
        /* v8 ignore stop */
      }
      if (issue.origin === 'array') {
        return `${field || 'Array'} must contain at least ${issue.minimum} items`;
      }
      return issue.message;

    case 'too_big':
      if (issue.origin === 'string') {
        /* v8 ignore start */
        return `${field || 'Value'} must be at most ${issue.maximum} characters`;
        /* v8 ignore stop */
      }
      if (issue.origin === 'number') {
        return `${field || 'Value'} must be at most ${issue.maximum}`;
      }
      if (issue.origin === 'array') {
        /* v8 ignore start */
        return `${field || 'Array'} must contain at most ${issue.maximum} items`;
        /* v8 ignore stop */
      }
      return issue.message;

    case 'invalid_value':
      return `${field || 'Value'} must be one of: ${issue.values.map(String).join(', ')}`;

    case 'invalid_union':
      return `${field || 'Value'} does not match any expected type`;

    case 'custom':
      /* v8 ignore start */
      return issue.message || `${field || 'Value'} is invalid`;
      /* v8 ignore stop */

    default:
      return issue.message;
  }
}

/**
 * Check if error is a Zod validation error
 */
export function isZodError(error: unknown): error is ZodError {
  return error instanceof ZodError;
}

/**
 * Safely parse with error transformation
 */
export function safeParse<T>(
  schema: { parse: (data: unknown) => T },
  data: unknown,
  context?: 'request' | 'response'
): T {
  try {
    return schema.parse(data);
  } catch (error) {
    if (isZodError(error)) {
      throw transformZodError(error, context);
    }
    throw error;
  }
}

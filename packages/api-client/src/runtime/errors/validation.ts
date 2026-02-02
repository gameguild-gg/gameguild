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
  const errors = error.errors.map(transformZodIssue);
  
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
  const field = issue.path.join('.');
  
  return {
    field: field || 'root',
    message: formatIssueMessage(issue),
    code: issue.code,
    value: 'received' in issue ? issue.received : undefined,
  };
}

/**
 * Format Zod issue message for better UX
 */
function formatIssueMessage(issue: ZodIssue): string {
  const field = issue.path.join('.');
  
  switch (issue.code) {
    case 'invalid_type':
      return `${field || 'Value'} must be ${issue.expected}, received ${issue.received}`;
    
    case 'invalid_string':
      if (issue.validation === 'email') {
        return `${field || 'Value'} must be a valid email address`;
      }
      if (issue.validation === 'url') {
        return `${field || 'Value'} must be a valid URL`;
      }
      if (issue.validation === 'uuid') {
        return `${field || 'Value'} must be a valid UUID`;
      }
      if (issue.validation === 'datetime') {
        return `${field || 'Value'} must be a valid ISO datetime`;
      }
      return issue.message;
    
    case 'too_small':
      if (issue.type === 'string') {
        return `${field || 'Value'} must be at least ${issue.minimum} characters`;
      }
      if (issue.type === 'number') {
        return `${field || 'Value'} must be at least ${issue.minimum}`;
      }
      if (issue.type === 'array') {
        return `${field || 'Array'} must contain at least ${issue.minimum} items`;
      }
      return issue.message;
    
    case 'too_big':
      if (issue.type === 'string') {
        return `${field || 'Value'} must be at most ${issue.maximum} characters`;
      }
      if (issue.type === 'number') {
        return `${field || 'Value'} must be at most ${issue.maximum}`;
      }
      if (issue.type === 'array') {
        return `${field || 'Array'} must contain at most ${issue.maximum} items`;
      }
      return issue.message;
    
    case 'invalid_enum_value':
      return `${field || 'Value'} must be one of: ${issue.options.join(', ')}`;
    
    case 'invalid_union':
      return `${field || 'Value'} does not match any expected type`;
    
    case 'custom':
      return issue.message || `${field || 'Value'} is invalid`;
    
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

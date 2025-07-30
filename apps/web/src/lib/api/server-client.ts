'use server';

import { auth } from '@/auth';
import { environment } from '@/configs/environment';

/**
 * Get authenticated API client configuration for server actions
 */
export async function getServerApiConfig() {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  return {
    baseUrl: environment.apiBaseUrl,
    headers: {
      Authorization: `Bearer ${session.accessToken}`,
      'Content-Type': 'application/json',
      'User-Agent': 'GameGuild-Web/1.0',
      'X-Request-Source': 'server-action',
    },
  };
}

/**
 * Get public API client configuration (for non-authenticated endpoints)
 */
export function getPublicApiConfig() {
  return {
    baseUrl: environment.apiBaseUrl,
    headers: {
      'Content-Type': 'application/json',
      'User-Agent': 'GameGuild-Web/1.0',
      'X-Request-Source': 'server-action',
    },
  };
}

/**
 * Enhanced error handling for API responses
 */
export function handleApiError(error: unknown, context: string) {
  console.error(`API Error in ${context}:`, error);

  // Type guard for error with response
  const hasResponse = (err: unknown): err is { response: { status: number; data?: { message?: string } } } => {
    return typeof err === 'object' && err !== null && 'response' in err;
  };

  if (hasResponse(error)) {
    if (error.response.status === 401) {
      throw new Error('Authentication expired. Please sign in again.');
    }

    if (error.response.status === 403) {
      throw new Error('You do not have permission to perform this action.');
    }

    if (error.response.status >= 500) {
      throw new Error('Server error. Please try again later.');
    }

    if (error.response.data?.message) {
      throw new Error(error.response.data.message);
    }
  }

  // Type guard for error with message
  const hasMessage = (err: unknown): err is { message: string } => {
    return typeof err === 'object' && err !== null && 'message' in err;
  };

  if (hasMessage(error)) {
    throw new Error(error.message);
  }

  throw new Error('An unexpected error occurred');
}

/**
 * Standardized success response wrapper
 */
export function createSuccessResponse<T>(data: T, message?: string) {
  return {
    success: true,
    data,
    message,
  };
}

/**
 * Standardized error response wrapper
 */
export function createErrorResponse(error: string | Error) {
  return {
    success: false,
    error: error instanceof Error ? error.message : error,
  };
}

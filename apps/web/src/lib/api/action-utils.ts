'use server';

import { revalidateTag, revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';
import { handleApiError, createSuccessResponse, createErrorResponse } from './server-client';

export interface ActionOptions {
  /** Tags to revalidate after successful action */
  revalidateTags?: string[];
  /** Paths to revalidate after successful action */
  revalidatePaths?: string[];
  /** Redirect URL after successful action */
  redirectTo?: string;
  /** Skip authentication check */
  skipAuth?: boolean;
  /** Custom error handler */
  onError?: (error: Error) => void;
}

export interface ActionResult<T = unknown> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

/**
 * Enhanced server action wrapper with standardized patterns
 */
export function createServerAction<TInput, TOutput>(actionFn: (input: TInput) => Promise<TOutput>, options: ActionOptions = {}) {
  return async (input: TInput): Promise<ActionResult<TOutput>> => {
    try {
      const result = await actionFn(input);

      // Handle revalidation
      if (options.revalidateTags) {
        options.revalidateTags.forEach((tag) => revalidateTag(tag));
      }

      if (options.revalidatePaths) {
        options.revalidatePaths.forEach((path) => revalidatePath(path));
      }

      // Handle redirect
      if (options.redirectTo) {
        redirect(options.redirectTo);
      }

      return createSuccessResponse(result);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'An unexpected error occurred';

      if (options.onError) {
        options.onError(error instanceof Error ? error : new Error(errorMessage));
      }

      return createErrorResponse(errorMessage);
    }
  };
}

/**
 * Enhanced server action wrapper that handles API calls
 */
export function createApiAction<TInput, TOutput>(apiCall: (input: TInput) => Promise<TOutput>, context: string, options: ActionOptions = {}) {
  return createServerAction<TInput, TOutput>(async (input: TInput) => {
    try {
      return await apiCall(input);
    } catch (error) {
      handleApiError(error, context);
      throw error; // This won't be reached due to handleApiError throwing
    }
  }, options);
}

/**
 * Form action wrapper for handling form submissions
 */
export function createFormAction<TOutput>(actionFn: (formData: FormData) => Promise<TOutput>, options: ActionOptions = {}) {
  return createServerAction<FormData, TOutput>(actionFn, options);
}

/**
 * Optimistic action wrapper for client-side optimistic updates
 */
export function createOptimisticAction<TInput, TOutput>(actionFn: (input: TInput) => Promise<TOutput>, options: ActionOptions = {}) {
  return createServerAction<TInput, TOutput>(actionFn, {
    ...options,
    revalidatePaths: options.revalidatePaths || ['/'],
  });
}

/**
 * Batch action wrapper for handling multiple operations
 */
export function createBatchAction<TInput, TOutput>(actionFn: (inputs: TInput[]) => Promise<TOutput[]>, options: ActionOptions = {}) {
  return createServerAction<TInput[], TOutput[]>(actionFn, options);
}

/**
 * Cache tags for different data types
 */
export const CACHE_TAGS = {
  USERS: 'users',
  USER_DETAIL: 'user-detail',
  PROJECTS: 'projects',
  PROJECT_DETAIL: 'project-detail',
  ACHIEVEMENTS: 'achievements',
  PROGRAMS: 'programs',
  COURSES: 'courses',
  COURSE_DETAIL: 'course-detail',
  POSTS: 'posts',
  FEED: 'feed',
  NOTIFICATIONS: 'notifications',
  TESTING_REQUESTS: 'testing-requests',
  TESTING_SESSIONS: 'testing-sessions',
} as const;

/**
 * Common revalidation patterns
 */
export const REVALIDATION_PATTERNS = {
  USER_PROFILE: {
    revalidateTags: [CACHE_TAGS.USERS, CACHE_TAGS.USER_DETAIL],
    revalidatePaths: ['/profile', '/dashboard'],
  },
  PROJECT_MANAGEMENT: {
    revalidateTags: [CACHE_TAGS.PROJECTS, CACHE_TAGS.PROJECT_DETAIL],
    revalidatePaths: ['/projects', '/dashboard'],
  },
  COURSE_ENROLLMENT: {
    revalidateTags: [CACHE_TAGS.COURSES, CACHE_TAGS.COURSE_DETAIL],
    revalidatePaths: ['/courses', '/learn'],
  },
  FEED_UPDATE: {
    revalidateTags: [CACHE_TAGS.FEED, CACHE_TAGS.POSTS],
    revalidatePaths: ['/feed', '/'],
  },
} as const;

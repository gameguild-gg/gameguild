/**
 * Authentication API client with automatic token refresh
 */

'use client';

import { useSession } from 'next-auth/react';
import { useCallback } from 'react';
import { apiClient } from '@/lib/api/api-client';

export interface AuthenticatedRequestOptions {
  skipRefresh?: boolean;
  retryOnAuth?: boolean;
}

/**
 * Hook for making authenticated API requests with automatic token refresh
 */
export function useAuthenticatedRequest() {
  const { data: session, update } = useSession();

  const makeRequest = useCallback(
    async <T>(
      endpoint: string,
      options: RequestInit & AuthenticatedRequestOptions = {}
    ): Promise<T> => {
      const { skipRefresh = false, retryOnAuth = true, ...requestOptions } = options;

      // Check if we have a session
      if (!session?.accessToken) {
        throw new Error('No authentication session available');
      }

      // Check for refresh token errors
      if ((session as any)?.error === 'RefreshTokenError') {
        throw new Error('Authentication session expired. Please sign in again.');
      }

      try {
        // Make the authenticated request
        return await apiClient.authenticatedRequest<T>(
          endpoint,
          session.accessToken,
          requestOptions
        );
      } catch (error) {
        // If it's an authentication error and we haven't already tried refreshing
        if (
          retryOnAuth &&
          !skipRefresh &&
          error instanceof Error &&
          (error.message.includes('401') || error.message.includes('Unauthorized'))
        ) {
          console.log('🔄 [AUTH REQUEST] Received 401, triggering session update...');
          
          try {
            // Trigger session update which should refresh the token
            const updatedSession = await update();
            
            if (updatedSession?.accessToken && !(updatedSession as any)?.error) {
              console.log('✅ [AUTH REQUEST] Session updated, retrying request...');
              
              // Retry the request with the new token
              return await apiClient.authenticatedRequest<T>(
                endpoint,
                updatedSession.accessToken,
                { ...requestOptions }
              );
            } else {
              console.error('❌ [AUTH REQUEST] Session update failed or returned error');
              throw new Error('Authentication session expired. Please sign in again.');
            }
          } catch (refreshError) {
            console.error('❌ [AUTH REQUEST] Failed to refresh session:', refreshError);
            throw new Error('Authentication session expired. Please sign in again.');
          }
        }

        // Re-throw the original error if we can't handle it
        throw error;
      }
    },
    [session, update]
  );

  return {
    makeRequest,
    isAuthenticated: !!session?.accessToken && !(session as any)?.error,
    hasError: !!(session as any)?.error,
    session,
  };
}

/**
 * Hook for getting the current access token with automatic refresh
 */
export function useAccessToken() {
  const { data: session, update } = useSession();

  const getToken = useCallback(async (): Promise<string | null> => {
    if (!session) {
      return null;
    }

    // Check for errors
    if ((session as any)?.error === 'RefreshTokenError') {
      return null;
    }

    // If we have a valid token, return it
    if (session.accessToken) {
      return session.accessToken;
    }

    // Try to refresh the session
    try {
      const updatedSession = await update();
      return updatedSession?.accessToken || null;
    } catch (error) {
      console.error('Failed to refresh token:', error);
      return null;
    }
  }, [session, update]);

  return {
    getToken,
    currentToken: session?.accessToken,
    isAuthenticated: !!session?.accessToken && !(session as any)?.error,
    hasError: !!(session as any)?.error,
  };
}

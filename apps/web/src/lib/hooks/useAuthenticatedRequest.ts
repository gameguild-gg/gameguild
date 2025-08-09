'use client';

import { useSession } from 'next-auth/react';
import { useCallback } from 'react';
import { forceSignOut } from './auth.actions';

interface AuthenticatedRequestOptions extends RequestInit {
  skipAuth?: boolean;
}

interface UseAuthenticatedRequestReturn {
  makeRequest: (url: string, options?: AuthenticatedRequestOptions) => Promise<Response>;
  isAuthenticated: boolean;
  hasValidSession: boolean;
}

/**
 * Hook for making authenticated HTTP requests with automatic token refresh handling
 */
export function useAuthenticatedRequest(): UseAuthenticatedRequestReturn {
  const { data: session, status, update } = useSession();

  const isAuthenticated = status === 'authenticated' && !!session?.api?.accessToken;
  const hasValidSession = isAuthenticated && !session?.error;

  const makeRequest = useCallback(async (
    url: string, 
    options: AuthenticatedRequestOptions = {}
  ): Promise<Response> => {
    const { skipAuth = false, headers = {}, ...restOptions } = options;

    // If auth is skipped, make request without authentication
    if (skipAuth) {
      return fetch(url, { ...restOptions, headers });
    }

    // Check if we have a valid session
    if (!session?.api?.accessToken) {
      console.error('❌ No access token available for authenticated request');
      throw new Error('Authentication required');
    }

    // Check for session errors (like RefreshTokenError)
    if (session.error) {
      console.error('❌ Session error detected:', session.error);
      
      // Force sign out for auth errors
      if (session.error === 'RefreshTokenError' || session.error === 'MissingTokensError') {
        await forceSignOut();
        throw new Error('Authentication expired. Please sign in again.');
      }
      
      throw new Error('Session error occurred');
    }

    // Prepare headers with authentication
    const authHeaders = {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${session.api.accessToken}`,
      ...headers,
    };

    console.log('🔒 Making authenticated request to:', url);

    try {
      // Make the initial request
      const response = await fetch(url, {
        ...restOptions,
        headers: authHeaders,
      });

      // If we get a 401, try to refresh the session and retry once
      if (response.status === 401) {
        console.warn('⚠️ Received 401, attempting session refresh...');
        
        try {
          // Trigger session update which should refresh tokens if needed
          await update();
          
          // Get the updated session
          const updatedSession = await new Promise((resolve) => {
            // We need to wait a bit for the session to update
            setTimeout(() => {
              resolve(session);
            }, 100);
          });
          
          // Check if we have valid tokens after refresh
          if (!session?.api?.accessToken || session.error) {
            console.error('❌ Session refresh failed, signing out');
            await forceSignOut();
            throw new Error('Authentication expired. Please sign in again.');
          }

          console.log('✅ Session refreshed, retrying request');
          
          // Retry the request with the new token
          const retryResponse = await fetch(url, {
            ...restOptions,
            headers: {
              ...authHeaders,
              'Authorization': `Bearer ${session.api.accessToken}`,
            },
          });

          return retryResponse;
        } catch (refreshError) {
          console.error('❌ Failed to refresh session:', refreshError);
          await forceSignOut();
          throw new Error('Authentication expired. Please sign in again.');
        }
      }

      return response;
    } catch (error) {
      console.error('❌ Authenticated request failed:', error);
      throw error;
    }
  }, [session, update]);

  return {
    makeRequest,
    isAuthenticated,
    hasValidSession,
  };
}

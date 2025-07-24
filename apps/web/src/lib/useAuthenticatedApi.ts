'use client';

import { useSession } from 'next-auth/react';
import { apiClient } from '@/lib/api/api-client';
import { useCallback } from 'react';

/**
 * Hook that provides an authenticated API client
 * Automatically includes the user's access token in requests
 */
export function useAuthenticatedApi() {
  const { data: session } = useSession();

  const makeAuthenticatedRequest = useCallback(
    async <T>(endpoint: string, options: RequestInit = {}): Promise<T> => {
      if (!session?.accessToken) {
        throw new Error('No access token available - user may not be authenticated');
      }

      return apiClient.authenticatedRequest<T>(endpoint, session.accessToken as string, options);
    },
    [session?.accessToken],
  );

  return {
    authenticatedRequest: makeAuthenticatedRequest,
    hasToken: !!session?.accessToken,
    isAuthenticated: !!session?.user,
  };
}

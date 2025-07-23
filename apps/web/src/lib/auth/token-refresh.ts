'use server';

/**
 * Token refresh utilities for NextAuth.js
 */

import { apiClient } from '@/lib/api/api-client';
import { getJwtExpiryDate } from '@/lib/utils/jwt-utils';
import { RefreshTokenResponse } from '@/components/legacy/types/auth';

export interface TokenRefreshResult {
  success: boolean;
  error?: string;
  data?: RefreshTokenResponse;
}

/**
 * Check if a token needs to be refreshed
 * @param accessToken - The access token to check
 * @param refreshBuffer - Buffer time in milliseconds before expiry (default: 30 seconds)
 * @returns true if token should be refreshed
 */
export async function shouldRefreshToken(accessToken: string | undefined, refreshBuffer = 30 * 1000): Promise<boolean> {
  if (!accessToken) {
    return false;
  }

  const expiresAt = getJwtExpiryDate(accessToken);
  if (!expiresAt) {
    return false;
  }

  const now = new Date();
  return now.getTime() + refreshBuffer >= expiresAt.getTime();
}

/**
 * Attempt to refresh an access token
 * @param refreshToken - The refresh token to use
 * @returns Promise<TokenRefreshResult>
 */
export async function refreshAccessToken(refreshToken: string): Promise<TokenRefreshResult> {
  try {
    if (!refreshToken) {
      return {
        success: false,
        error: 'No refresh token provided',
      };
    }

    const tokenPrefix = refreshToken?.length > 20 ? refreshToken.substring(0, 20) : (refreshToken ?? 'null');
    console.log('🔄 [TOKEN REFRESH] Attempting token refresh with token:', `${tokenPrefix}...`);
    console.log('🔄 [TOKEN REFRESH] Full token length:', refreshToken.length);
    console.log('🔄 [TOKEN REFRESH] API Base URL:', process.env.NEXT_PUBLIC_API_URL);

    const response = await apiClient.refreshToken({
      refreshToken,
    });

    console.log('✅ [TOKEN REFRESH] Token refresh successful:', {
      hasAccessToken: !!response.accessToken,
      hasRefreshToken: !!response.refreshToken,
      expiresAt: response.expires,
    });

    return {
      success: true,
      data: response,
    };
  } catch (error) {
    // Check if it's a 401 error (invalid refresh token)
    const isUnauthorized = error instanceof Error && error.message.includes('401');

    console.error('❌ [TOKEN REFRESH] Failed to refresh token:', {
      error: error instanceof Error ? error.message : 'Unknown error',
      isUnauthorized,
      stack: error instanceof Error ? error.stack : undefined,
    });

    // If unauthorized, the refresh token is invalid/expired
    if (isUnauthorized) {
      console.warn('🔒 [TOKEN REFRESH] Refresh token is invalid or expired. User needs to sign in again.');
    }

    return {
      success: false,
      error: error instanceof Error ? error.message : 'Token refresh failed',
    };
  }
}

/**
 * Validate that a token response has all required fields
 */
export async function validateTokenResponse(response: RefreshTokenResponse): Promise<boolean> {
  return response && typeof response.accessToken === 'string' && typeof response.refreshToken === 'string' && typeof response.expires === 'string';
}

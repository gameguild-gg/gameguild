/**
 * Token refresh utilities for NextAuth.js
 */

import { getJwtExpiryDate } from '@/lib/utils/jwt-utils';
import { RefreshTokenResponse } from '@/components/legacy/types/auth';
import { serverRefreshToken } from './server-actions';

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
export function shouldRefreshToken(accessToken: string | undefined, refreshBuffer = 30 * 1000): boolean {
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

    console.log('🔄 [TOKEN REFRESH] Attempting token refresh...');

    const response = await serverRefreshToken(refreshToken);

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
    console.error('❌ [TOKEN REFRESH] Failed to refresh token:', {
      error: error instanceof Error ? error.message : 'Unknown error',
      stack: error instanceof Error ? error.stack : undefined,
    });

    return {
      success: false,
      error: error instanceof Error ? error.message : 'Token refresh failed',
    };
  }
}

/**
 * Validate that a token response has all required fields
 */
export function validateTokenResponse(response: any): response is RefreshTokenResponse {
  return response && typeof response.accessToken === 'string' && typeof response.refreshToken === 'string' && typeof response.expires === 'string';
}

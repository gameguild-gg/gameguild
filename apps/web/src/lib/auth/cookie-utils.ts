import { cookies } from 'next/headers';

/**
 * Cookie management utilities for NextAuth callbacks
 * These are NOT server actions - they work in NextAuth callback context
 */
const BACKEND_TOKENS_COOKIE = 'backend-tokens';
const COOKIE_MAX_AGE = 7 * 24 * 60 * 60; // 7 days

export interface BackendTokenData {
  accessToken: string;
  refreshToken: string;
  expires: string;
  user: {
    id: string;
    username: string;
    email: string;
  };
  tenantId?: string;
  availableTenants?: Array<{
    id: string;
    name: string;
    isActive: boolean;
  }>;
  storedAt: string;
}

/**
 * Store backend tokens in HTTP-only cookie (for NextAuth callbacks)
 */
export function storeBackendTokensSync(tokens: {
  accessToken: string;
  refreshToken: string;
  expires: string;
  user: {
    id: string;
    username: string;
    email: string;
  };
  tenantId?: string;
  availableTenants?: Array<{
    id: string;
    name: string;
    isActive: boolean;
  }>;
}): boolean {
  try {
    const cookieStore = cookies();

    const tokenData = JSON.stringify({
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
      expires: tokens.expires,
      user: tokens.user,
      tenantId: tokens.tenantId,
      availableTenants: tokens.availableTenants,
      storedAt: new Date().toISOString(),
    });

    // Handle both sync and async cookies
    if (typeof cookieStore.set === 'function') {
      cookieStore.set(BACKEND_TOKENS_COOKIE, tokenData, {
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'lax',
        maxAge: COOKIE_MAX_AGE,
        path: '/',
      });
    } else {
      // Fallback for when cookies() returns a promise
      console.warn('[Auth Cookie] Cannot set cookie synchronously - cookies() returned a promise');
      return false;
    }

    console.log('[Auth Cookie] Backend tokens stored in cookie');
    return true;
  } catch (error) {
    console.error('[Auth Cookie] Failed to store backend tokens:', error);
    return false;
  }
}

/**
 * Retrieve backend tokens from HTTP-only cookie (for NextAuth callbacks)
 */
export function getBackendTokensSync(): BackendTokenData | null {
  try {
    const cookieStore = cookies();
    const tokenCookie = cookieStore.get(BACKEND_TOKENS_COOKIE);

    if (!tokenCookie?.value) {
      console.log('[Auth Cookie] No backend tokens found in cookie');
      return null;
    }

    const tokenData = JSON.parse(tokenCookie.value);

    // Check if tokens are still valid (not expired)
    const expiresAt = new Date(tokenData.expires);
    const now = new Date();

    if (now >= expiresAt) {
      console.log('[Auth Cookie] Backend tokens in cookie are expired');
      // Clear expired cookie
      clearBackendTokensSync();
      return null;
    }

    console.log('[Auth Cookie] Backend tokens retrieved from cookie');
    return tokenData;
  } catch (error) {
    console.error('[Auth Cookie] Failed to retrieve backend tokens:', error);
    // Clear corrupted cookie
    clearBackendTokensSync();
    return null;
  }
}

/**
 * Update backend tokens in HTTP-only cookie (for NextAuth callbacks)
 */
export function updateBackendTokensSync(tokens: { accessToken: string; refreshToken: string; expires: string }): boolean {
  try {
    // Get existing token data
    const existingTokens = getBackendTokensSync();

    if (!existingTokens) {
      console.error('[Auth Cookie] Cannot update tokens - no existing tokens found');
      return false;
    }

    // Update with new tokens while preserving user data
    const updatedTokenData = {
      ...existingTokens,
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
      expires: tokens.expires,
      storedAt: new Date().toISOString(),
    };

    return storeBackendTokensSync(updatedTokenData);
  } catch (error) {
    console.error('[Auth Cookie] Failed to update backend tokens:', error);
    return false;
  }
}

/**
 * Clear backend tokens from HTTP-only cookie (for NextAuth callbacks)
 */
export function clearBackendTokensSync(): boolean {
  try {
    const cookieStore = cookies();
    cookieStore.delete(BACKEND_TOKENS_COOKIE);
    console.log('[Auth Cookie] Backend tokens cleared from cookie');
    return true;
  } catch (error) {
    console.error('[Auth Cookie] Failed to clear backend tokens:', error);
    return false;
  }
}

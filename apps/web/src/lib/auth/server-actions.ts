'use server';

import { environment } from '@/configs/environment';

/**
 * Server action for Google ID token validation - used in NextAuth signIn callback
 */
export async function serverGoogleIdTokenSignIn(idToken: string, tenantId?: string) {
  try {
    const response = await fetch(`${environment.apiBaseUrl}/api/auth/google/id-token`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        idToken,
        tenantId,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  } catch (error) {
    console.error('[Server Action] Google ID token validation failed:', error);
    throw error;
  }
}

/**
 * Server action for token refresh - used in NextAuth JWT callback
 */
export async function serverRefreshToken(refreshToken: string) {
  try {
    const response = await fetch(`${environment.apiBaseUrl}/api/auth/refresh-token`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        refreshToken,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  } catch (error) {
    console.error('[Server Action] Token refresh failed:', error);
    throw error;
  }
}

/**
 * Store backend tokens in HTTP-only cookie
 */
export async function storeBackendTokens() {
  // Implementation would go here for use outside NextAuth callbacks
  // For now, just return success since we're not using this
  console.log('[Server Action] storeBackendTokens called but not implemented');
  return true;
}

/**
 * Retrieve backend tokens from HTTP-only cookie
 */
export async function getBackendTokens() {
  // Implementation would go here for use outside NextAuth callbacks
  // For now, just return null since we're not using this
  console.log('[Server Action] getBackendTokens called but not implemented');
  return null;
}

/**
 * Update backend tokens in HTTP-only cookie
 */
export async function updateBackendTokens() {
  // Implementation would go here for use outside NextAuth callbacks
  // For now, just return success since we're not using this
  console.log('[Server Action] updateBackendTokens called but not implemented');
  return true;
}

/**
 * Clear backend tokens from HTTP-only cookie
 */
export async function clearBackendTokens() {
  // Implementation would go here for use outside NextAuth callbacks
  // For now, just return success since we're not using this
  console.log('[Server Action] clearBackendTokens called but not implemented');
  return true;
}

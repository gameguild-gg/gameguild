'use server';

import { environment } from '@/configs/environment';

/**
 * Server action for admin login - used in NextAuth authorize callback
 */
export async function serverAdminLogin(email: string, password: string) {
  try {
    const response = await fetch(`${environment.apiBaseUrl}/api/auth/sign-in`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        email,
        password,
        tenantId: null, // Let backend choose the tenant
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  } catch (error) {
    console.error('[Server Action] Admin login failed:', error);
    throw error;
  }
}

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

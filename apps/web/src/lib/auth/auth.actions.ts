'use server';

import { environment } from '@/configs/environment';
import { AuthError } from 'next-auth';
import { signIn, signOut } from '@/auth';
import { RefreshTokenResponse, SignInResponse } from './auth.types';

interface GoogleSignInRequest {
  idToken: string;
  tenantId?: string;
}

export async function signInWithEmailAndPassword(email: string, password: string): Promise<void> {
  try {
    await signIn('local', { email, password });
  } catch (error) {
    // Handle known NextAuth errors
    if (error instanceof AuthError) {
      switch (error.type) {
        case 'OAuthSignInError':
          throw new Error('OAuth sign-in failed');
        case 'OAuthCallbackError':
          throw new Error('OAuth callback error');
        case 'AccessDenied':
          throw new Error('Access denied');
        case 'OAuthAccountNotLinked':
          throw new Error('Email already in use with different provider');
        default:
          throw new Error('Authentication error occurred');
      }
    }
    // Re-throw other errors
    throw error;
  }
}

export async function signInWithGoogle() {
  try {
    await signIn('google');
  } catch (error) {
    // Handle known NextAuth errors
    if (error instanceof AuthError) {
      switch (error.type) {
        case 'OAuthSignInError':
          throw new Error('OAuth sign-in failed');
        case 'OAuthCallbackError':
          throw new Error('OAuth callback error');
        case 'AccessDenied':
          throw new Error('Access denied');
        case 'OAuthAccountNotLinked':
          throw new Error('Email already in use with different provider');
        default:
          throw new Error('Authentication error occurred');
      }
    }
    // Re-throw other errors
    throw error;
  }
}

export async function localSign(payload: { email: string; password: string }): Promise<SignInResponse> {
  try {
    const response = await fetch(`${environment.apiBaseUrl}/api/auth/sign-in`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) throw new Error(`Authentication failed: ${response.status} ${response.statusText}`);

    return await response.json();
  } catch (error) {
    console.error('local sign-in failed:', error);
    throw new Error('Failed to authenticate with local credentials');
  }
}

export async function googleIdTokenSignIn(request: GoogleSignInRequest): Promise<SignInResponse> {
  try {
    const response = await fetch(`${environment.apiBaseUrl}/api/auth/google`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) throw new Error(`Authentication failed: ${response.status} ${response.statusText}`);

    return await response.json();
  } catch (error) {
    console.error('Google ID token sign-in failed:', error);
    throw new Error('Failed to authenticate with Google ID token');
  }
}

export async function refreshAccessToken(refreshToken: string): Promise<RefreshTokenResponse> {
  try {
    const response = await fetch(`${environment.apiBaseUrl}/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      throw new Error(`Refresh failed: ${response.status} ${response.statusText}`);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error('Token refresh failed:', error);
    throw new Error('Failed to refresh token');
  }
}

export async function fetchTenantDetails(tenantId: string, accessToken: string): Promise<{ id: string; name: string; isActive: boolean }> {
  try {
    const response = await fetch(`${environment.apiBaseUrl}/api/tenants/${tenantId}`, {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch tenant details: ${response.status} ${response.statusText}`);
    }

    const tenant = await response.json();
    return {
      id: tenant.id,
      name: tenant.name || `Tenant ${tenant.id}`,
      isActive: tenant.isActive ?? true,
    };
  } catch (error) {
    console.error('Failed to fetch tenant details:', error);
    // Return fallback data if fetch fails
    return {
      id: tenantId,
      name: `Tenant ${tenantId}`,
      isActive: true,
    };
  }
}

export async function forceSignOut(): Promise<void> {
  try {
    await signOut({ redirect: true, redirectTo: '/sign-in' });
  } catch (error) {
    console.error('Failed to sign out:', error);
    // Force redirect even if sign out fails
    if (typeof window !== 'undefined') {
      window.location.href = '/sign-in';
    }
  }
}

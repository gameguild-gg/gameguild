'use server';

import { signIn, signOut } from '@/auth';
import { environment } from '@/configs/environment';
import { AuthError } from 'next-auth';
import { RefreshTokenResponse, SignInResponse } from './auth.types';

interface GoogleSignInRequest {
  idToken: string;

  tenantId?: string;
}

export async function signInWithEmailAndPassword(email: string, password: string): Promise<void> {
  try {
    await signIn('local', { email, password });
    // redirect('/'); // Redirect to home page after successful sign-in
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
      body: JSON.stringify({
        Email: payload.email,
        Password: payload.password,
      }),
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
      body: JSON.stringify({
        IdToken: request.idToken,
        TenantId: request.tenantId,
      }),
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
    console.log('Attempting to refresh token with:', {
      url: `${environment.apiBaseUrl}/api/auth/refresh`,
      refreshToken: refreshToken ? `${refreshToken.substring(0, 10)}...` : 'null'
    });

    const response = await fetch(`${environment.apiBaseUrl}/api/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ RefreshToken: refreshToken }),
    });

    console.log('Refresh token response status:', response.status, response.statusText);

    if (!response.ok) {
      let errorDetails = '';
      try {
        const errorData = await response.json();
        errorDetails = JSON.stringify(errorData);
      } catch {
        errorDetails = await response.text();
      }

      console.error('Refresh token API error:', {
        status: response.status,
        statusText: response.statusText,
        details: errorDetails
      });

      throw new Error(`Refresh failed: ${response.status} ${response.statusText} - ${errorDetails}`);
    }

    const data = await response.json();
    console.log('Refresh token response data:', {
      hasAccessToken: !!data.accessToken,
      hasRefreshToken: !!data.refreshToken,
      expiresAt: data.expiresAt
    });

    return data;
  } catch (error) {
    console.error('Token refresh failed:', error);
    throw error; // Re-throw the original error instead of wrapping it
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

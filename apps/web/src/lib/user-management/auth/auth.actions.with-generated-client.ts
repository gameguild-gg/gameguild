'use server';

import { environment } from '@/configs/environment';
import { AuthError } from 'next-auth';
import { signIn, signOut } from '@/auth';
import { RefreshTokenResponse, SignInResponse } from './auth.types';
// Import from your auto-generated client
import { postApiAuthSignin, postApiAuthGoogleIdToken, postApiAuthRefresh } from '@/lib/api/generated';

interface GoogleSignInRequest {
  idToken: string;
  tenantId?: string;
}

// Your existing signIn functions remain the same
export async function signInWithEmailAndPassword(email: string, password: string): Promise<void> {
  try {
    await signIn('local', { email, password });
  } catch (error) {
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
    throw error;
  }
}

export async function signInWithGoogle() {
  try {
    await signIn('google');
  } catch (error) {
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
    throw error;
  }
}

// Using auto-generated client instead of manual fetch
export async function localSign(payload: { email: string; password: string }): Promise<SignInResponse> {
  try {
    const response = await postApiAuthSignin({
      baseUrl: environment.apiBaseUrl,
      body: payload,
    });

    if (!response.data) {
      throw new Error('Authentication failed: No data received');
    }

    return response.data;
  } catch (error) {
    console.error('local sign-in failed:', error);
    throw new Error('Failed to authenticate with local credentials');
  }
}

// Using auto-generated client for Google sign-in
export async function googleIdTokenSignIn(request: GoogleSignInRequest): Promise<SignInResponse> {
  try {
    const response = await postApiAuthGoogleIdToken({
      baseUrl: environment.apiBaseUrl,
      body: request,
    });

    if (!response.data) {
      throw new Error('Authentication failed: No data received');
    }

    return response.data;
  } catch (error) {
    console.error('Google ID token sign-in failed:', error);
    throw new Error('Failed to authenticate with Google ID token');
  }
}

// Using auto-generated client for token refresh
export async function refreshAccessToken(refreshToken: string): Promise<RefreshTokenResponse> {
  try {
    const response = await postApiAuthRefresh({
      baseUrl: environment.apiBaseUrl,
      body: { refreshToken },
    });

    if (!response.data) {
      throw new Error('Refresh failed: No data received');
    }

    return response.data;
  } catch (error) {
    console.error('Token refresh failed:', error);
    throw new Error('Failed to refresh token');
  }
}

export async function forceSignOut(): Promise<void> {
  try {
    await signOut({ redirect: true, redirectTo: '/sign-in' });
  } catch (error) {
    console.error('Failed to sign out:', error);
    if (typeof window !== 'undefined') {
      window.location.href = '/sign-in';
    }
  }
}

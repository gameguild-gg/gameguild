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
    if (!refreshToken || refreshToken.trim() === '') {
      throw new Error('Refresh token is empty or null');
    }

    console.log('🔄 Attempting token refresh:', {
      url: `${environment.apiBaseUrl}/api/auth/refresh`,
      refreshTokenLength: refreshToken.length,
      refreshTokenPreview: `${refreshToken.substring(0, 10)}...`
    });

    const requestBody = { RefreshToken: refreshToken.trim() };
    console.log('🔄 Request body prepared:', { RefreshToken: `${refreshToken.substring(0, 10)}...` });

    const response = await fetch(`${environment.apiBaseUrl}/api/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
      body: JSON.stringify(requestBody),
    });

    console.log('🔄 Refresh response received:', {
      status: response.status,
      statusText: response.statusText,
      ok: response.ok,
      contentType: response.headers.get('content-type')
    });

    if (!response.ok) {
      let errorDetails = '';
      let errorData: any = null;
      
      try {
        const contentType = response.headers.get('content-type');
        if (contentType?.includes('application/json')) {
          errorData = await response.json();
          errorDetails = JSON.stringify(errorData, null, 2);
        } else {
          errorDetails = await response.text();
        }
      } catch (parseError) {
        errorDetails = `Failed to parse error response: ${parseError}`;
      }

      console.error('❌ Refresh token API error:', {
        status: response.status,
        statusText: response.statusText,
        details: errorDetails,
        errorData
      });

      // Provide specific error messages based on status codes
      if (response.status === 401) {
        throw new Error(`Refresh token is invalid or expired`);
      } else if (response.status === 400) {
        throw new Error(`Invalid refresh request: ${errorData?.detail || errorDetails}`);
      } else if (response.status >= 500) {
        throw new Error(`Server error during token refresh: ${response.status}`);
      } else {
        throw new Error(`Token refresh failed: ${response.status} ${response.statusText}`);
      }
    }

    let data: RefreshTokenResponse;
    try {
      data = await response.json();
    } catch (parseError) {
      console.error('❌ Failed to parse refresh response JSON:', parseError);
      throw new Error('Invalid JSON response from refresh endpoint');
    }

    // Validate the response structure
    if (!data.accessToken || !data.refreshToken) {
      console.error('❌ Invalid refresh response structure:', {
        hasAccessToken: !!data.accessToken,
        hasRefreshToken: !!data.refreshToken,
        hasExpiresAt: !!data.expiresAt,
        data: { ...data, accessToken: data.accessToken ? 'present' : 'missing', refreshToken: data.refreshToken ? 'present' : 'missing' }
      });
      throw new Error('Invalid refresh response: missing required tokens');
    }

    console.log('✅ Token refresh successful:', {
      hasAccessToken: !!data.accessToken,
      hasRefreshToken: !!data.refreshToken,
      expiresAt: data.expiresAt,
      accessTokenExpiresAt: data.accessTokenExpiresAt,
      refreshTokenExpiresAt: data.refreshTokenExpiresAt,
      tenantId: data.tenantId
    });

    return data;
  } catch (error) {
    console.error('❌ Token refresh failed with error:', {
      error: error instanceof Error ? error.message : error,
      stack: error instanceof Error ? error.stack : undefined,
      refreshTokenPreview: refreshToken ? `${refreshToken.substring(0, 10)}...` : 'null'
    });
    
    // Re-throw the error to be handled by the caller
    throw error;
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

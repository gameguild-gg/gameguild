import Google from 'next-auth/providers/google';
import { environment } from '@/configs/environment';
import { NextAuthConfig } from 'next-auth';
import { apiClient } from '@/lib/api/api-client';
import { SignInResponse } from '@/components/legacy/types/auth';
import { getJwtExpiryDate } from '@/lib/utils/jwt-utils';
import { refreshAccessToken } from '@/lib/auth/token-refresh';

export const authConfig: NextAuthConfig = {
  pages: {
    signIn: '/sign-in',
    error: '/auth/error', // Error code passed in query string as ?error=
  },
  providers: [
    Google({
      clientId: environment.googleClientId,
      clientSecret: environment.googleClientSecret,
      authorization: {
        params: {
          request_uri: environment.signInGoogleCallbackUrl,
          // scope: 'openid email profile',
        },
      },
    }),
  ],
  session: {
    strategy: 'jwt',
    // Extend session duration to 7 days
    maxAge: 7 * 24 * 60 * 60, // 7 days in seconds
    // Update session every hour to keep it fresh
    updateAge: 60 * 60, // 1 hour in seconds
  },
  callbacks: {
    async signIn({ user, account, profile }) {
      console.log('🔑 [AUTH DEBUG] SignIn callback triggered:', {
        provider: account?.provider,
        hasIdToken: !!account?.id_token,
        userEmail: user?.email,
        accountType: account?.type,
        profileId: profile?.sub,
        timestamp: new Date().toISOString(),
      });

      if (account?.provider === 'google') {
        console.log('🔍 [AUTH DEBUG] Processing Google authentication...');
        if (!account?.id_token) {
          console.error('❌ [AUTH DEBUG] No id_token received from Google');
          return false;
        }

        console.log('📨 [AUTH DEBUG] Google ID token received, length:', account.id_token.length);

        try {
          console.log('🚀 [AUTH DEBUG] Attempting Google ID token validation with CMS backend:', environment.apiBaseUrl);
          console.log('🔑 [AUTH DEBUG] Making request to: POST /api/auth/google/id-token');

          // Try the new Google ID token endpoint
          const response = await apiClient.googleIdTokenSignIn({
            idToken: account.id_token,
            tenantId: undefined, // Can be set later via tenant switching
          });

          console.log('✅ [AUTH DEBUG] CMS backend authentication successful:', {
            userId: response.user?.id,
            userEmail: response.user?.email,
            tenantId: response.tenantId,
            availableTenants: response.availableTenants?.length,
            hasAccessToken: !!response.accessToken,
            hasRefreshToken: !!response.refreshToken,
          });

          // Store the backend response in the user object
          (user as any).cmsData = response;
          (user as any).accessToken = response.accessToken;
          (user as any).refreshToken = response.refreshToken;
          (user as any).tenantId = response.tenantId;
          (user as any).availableTenants = response.availableTenants;

          return true;
        } catch (error) {
          console.error('❌ [AUTH DEBUG] CMS backend authentication failed:', {
            error: error instanceof Error ? error.message : error,
            apiBaseUrl: environment.apiBaseUrl,
            endpoint: '/auth/google/id-token',
            cause: (error as any)?.cause?.code || 'Unknown',
            stack: error instanceof Error ? error.stack : 'No stack trace',
            fullError: error,
          });

          // Check if it's a connection error
          if ((error as any)?.cause?.code === 'ECONNREFUSED') {
            console.error('🚨 [AUTH DEBUG] CMS Backend is not running on:', environment.apiBaseUrl);
            console.error('💡 [AUTH DEBUG] Please start the CMS backend with: cd apps/api && dotnet run');
          } else {
            console.error('🔍 [AUTH DEBUG] CMS Backend is running but authentication failed. Check CMS logs for details.');
            console.error('🐛 [AUTH DEBUG] This might be a Google ID token validation issue in the CMS backend.');
          }

          return false;
        }
      }

      console.log('⚠️ [AUTH DEBUG] SignIn: Provider not supported:', account?.provider);
      return false;
    },
    async jwt({ token, user, account, trigger, session }) {
      console.log('🔐 [AUTH DEBUG] JWT callback triggered:', {
        trigger,
        hasUser: !!user,
        hasAccount: !!account,
        hasToken: !!token,
        hasSession: !!session,
        userId: user?.id,
        provider: account?.provider,
        hasStoredRefreshToken: !!token.refreshToken,
        timestamp: new Date().toISOString(),
      });

      // If this is a new sign-in, store the CMS data
      if (user && (user as any).cmsData) {
        console.log('📦 [AUTH DEBUG] Storing CMS data in JWT token...');
        const cmsData = (user as any).cmsData as SignInResponse;

        token.id = cmsData.user.id;
        token.accessToken = cmsData.accessToken;
        token.refreshToken = cmsData.refreshToken;
        token.expires = new Date(cmsData.expires);
        token.user = cmsData.user;
        token.tenantId = cmsData.tenantId;
        token.availableTenants = cmsData.availableTenants;

        console.log('✅ [AUTH DEBUG] CMS data stored in token:', {
          userId: token.id,
          hasAccessToken: !!token.accessToken,
          hasRefreshToken: !!token.refreshToken,
          expires: token.expires,
          tenantId: token.tenantId,
        });
      }

      // Handle session updates (like tenant switching)
      if (trigger === 'update' && session) {
        console.log('🔄 [AUTH DEBUG] Session update triggered:', {
          hasCurrentTenant: !!session.currentTenant,
          currentTenant: session.currentTenant,
        });
        if (session.currentTenant) {
          token.currentTenant = session.currentTenant;
        }
      }

      // Check if we have essential data - if not, the session is corrupted
      if (!token.id || !token.user) {
        console.error('❌ [AUTH DEBUG] Session corrupted - missing essential data:', {
          hasId: !!token.id,
          hasUser: !!token.user,
          hasAccessToken: !!token.accessToken,
          hasRefreshToken: !!token.refreshToken,
        });

        // Mark session as corrupted to force re-authentication
        token.error = 'SessionCorrupted';
        return token;
      }

      // If we don't have a refresh token, we can't refresh - mark for re-auth
      if (!token.refreshToken) {
        console.warn('⚠️ [AUTH DEBUG] No refresh token available - session will need re-authentication');
        console.warn('⚠️ [AUTH DEBUG] Current token state:', {
          hasId: !!token.id,
          hasUser: !!token.user,
          hasAccessToken: !!token.accessToken,
          hasRefreshToken: !!token.refreshToken,
          expires: token.expires,
          error: token.error,
        });
        token.error = 'RefreshTokenError';
        return token;
      }

      // Check if token is expired and refresh if needed
      const now = new Date();
      let expiresAt: Date | null = null;

      // Try to get expiry from JWT token itself (most reliable)
      if (token.accessToken) {
        expiresAt = getJwtExpiryDate(token.accessToken as string);
      }

      // Fallback to stored expires value if JWT decode fails
      if (!expiresAt && token.expires) {
        expiresAt = new Date(token.expires as unknown as string);
      }

      // Add 30-second buffer to avoid race conditions
      const refreshBuffer = 30 * 1000; // 30 seconds
      const shouldRefresh = expiresAt && now.getTime() + refreshBuffer >= expiresAt.getTime();

      console.log('⏰ [AUTH DEBUG] Token expiry check:', {
        now: now.toISOString(),
        expiresAt: expiresAt?.toISOString(),
        isExpired: expiresAt ? now > expiresAt : false,
        shouldRefresh,
        hasRefreshToken: !!token.refreshToken,
        hasError: !!token.error,
        tokenSource: expiresAt ? (token.accessToken ? 'JWT_DECODE' : 'STORED_VALUE') : 'NONE',
        willAttemptRefresh: shouldRefresh && !!token.refreshToken && !token.error,
        refreshBuffer: `${refreshBuffer / 1000}s`,
      });

      // Only attempt refresh if:
      // 1. Token is about to expire or expired
      // 2. We have a refresh token
      // 3. We don't already have a refresh error
      // 4. This is not a session update trigger (to avoid infinite loops)
      if (shouldRefresh && token.refreshToken && !token.error && trigger !== 'update') {
        console.log('🔄 [AUTH DEBUG] Token needs refresh, attempting...');

        const refreshResult = await refreshAccessToken(token.refreshToken as string);

        if (refreshResult.success && refreshResult.data) {
          console.log('✅ [AUTH DEBUG] Token refresh successful:', {
            newExpiresAt: refreshResult.data.expires,
            hasNewAccessToken: !!refreshResult.data.accessToken,
            hasNewRefreshToken: !!refreshResult.data.refreshToken,
          });

          token.accessToken = refreshResult.data.accessToken;
          token.refreshToken = refreshResult.data.refreshToken;
          token.expires = new Date(refreshResult.data.expires);

          // Clear any previous errors
          delete token.error;
        } else {
          console.error('❌ [AUTH DEBUG] Failed to refresh token:', {
            error: refreshResult.error,
            refreshToken: token.refreshToken ? 'present' : 'missing',
            refreshTokenPrefix: token.refreshToken ? (token.refreshToken as string).substring(0, 20) + '...' : 'none',
            tokenLength: token.refreshToken ? (token.refreshToken as string).length : 0,
          });
          // Token refresh failed, user needs to sign in again
          token.error = 'RefreshTokenError';
          // Don't immediately clear tokens to allow graceful degradation
          // They will be cleared in the session callback
        }
      }

      return token;
    },
    async session({ session, token }) {
      console.log('📋 [AUTH DEBUG] Session callback triggered:', {
        hasSession: !!session,
        hasToken: !!token,
        tokenId: token.id,
        hasAccessToken: !!token.accessToken,
        hasError: !!token.error,
        sessionUserEmail: session?.user?.email,
        timestamp: new Date().toISOString(),
      });

      // If there's any token error, clear the session data and return minimal session
      if (token.error === 'RefreshTokenError' || token.error === 'SessionCorrupted') {
        console.log('🚨 [AUTH DEBUG] Token error detected, clearing session data:', token.error);

        // Return null to force NextAuth to clear the session completely
        // This will automatically redirect to sign-in page
        return {
          ...session,
          accessToken: undefined,
          error: token.error,
          user: {
            ...session.user,
            id: token.id as string,
          },
          // Add expires in the past to force session expiry
          expires: new Date(0).toISOString(),
        };
      }

      // Ensure we have essential data
      if (!token.id || !token.user) {
        console.error('❌ [AUTH DEBUG] Session missing essential data');
        return {
          ...session,
          accessToken: undefined,
          error: 'SessionCorrupted',
          user: {
            ...session.user,
            id: (token.id as string) || 'unknown',
          },
        };
      }

      // Pass token data to the session only if we don't have errors
      if (token.accessToken) {
        session.accessToken = token.accessToken as string;
      }

      // DEBUG ONLY: Expose refresh token for debugging (remove in production)
      if (token.refreshToken) {
        (session as any).refreshToken = token.refreshToken as string;
      }

      session.user.id = token.id as string;

      if (token.user) {
        session.user = {
          ...session.user,
          ...(token.user as any),
        };
      }

      if (token.tenantId) {
        (session as any).tenantId = token.tenantId;
      }

      if (token.availableTenants) {
        (session as any).availableTenants = token.availableTenants;
      }

      if (token.currentTenant) {
        (session as any).currentTenant = token.currentTenant;
      }

      return session;
    },
  },
};

declare module 'next-auth' {
  interface Session {
    accessToken?: string;
    tenantId?: string;
    availableTenants?: Array<{
      id: string;
      name: string;
      isActive: boolean;
    }>;
    currentTenant?: {
      id: string;
      name: string;
      isActive: boolean;
    };
    error?: 'RefreshTokenError' | 'SessionCorrupted';
  }

  interface User {
    id: string;
    username?: string;
  }

  interface JWT {
    id: string;
    accessToken?: string;
    refreshToken?: string;
    expires?: Date;
    user?: {
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
    currentTenant?: {
      id: string;
      name: string;
      isActive: boolean;
    };
    error?: 'RefreshTokenError' | 'SessionCorrupted';
  }
}

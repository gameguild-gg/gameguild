import Google from 'next-auth/providers/google';
import { environment } from '@/configs/environment';
import { NextAuthConfig } from 'next-auth';
import { apiClient } from '@/lib/api-client';
import { SignInResponse } from '@/types/auth';
import { getJwtExpiryDate } from '@/lib/jwt-utils';

export const authConfig: NextAuthConfig = {
  providers: [
    Google({
      clientId: environment.googleClientId,
      clientSecret: environment.googleClientSecret,
      authorization: {
        params: {
          request_uri: environment.signInGoogleCallbackUrl,
        },
      },
    }),
  ],
  session: { strategy: 'jwt' },
  callbacks: {
    async signIn({ user, account, profile }) {
      console.log('SignIn callback triggered:', {
        provider: account?.provider,
        hasIdToken: !!account?.id_token,
        userEmail: user?.email,
      });

      if (account?.provider === 'google') {
        if (!account?.id_token) {
          console.error('No id_token received from Google');
          return false;
        }
        try {
          console.log('Attempting Google ID token validation with CMS backend:', environment.apiBaseUrl);

          // Try the new Google ID token endpoint
          const response = await apiClient.googleIdTokenSignIn({
            idToken: account.id_token,
            tenantId: undefined, // Can be set later via tenant switching
          });

          console.log('CMS backend authentication successful:', {
            userId: response.user?.id,
            tenantId: response.tenantId,
            availableTenants: response.availableTenants?.length,
          });

          // Store the backend response in the user object
          (user as any).cmsData = response;
          (user as any).accessToken = response.accessToken;
          (user as any).refreshToken = response.refreshToken;
          (user as any).tenantId = response.tenantId;
          (user as any).availableTenants = response.availableTenants;

          return true;
        } catch (error) {
          console.error('❌ CMS backend authentication failed:', {
            error: error instanceof Error ? error.message : error,
            apiBaseUrl: environment.apiBaseUrl,
            endpoint: '/auth/google/id-token',
            cause: (error as any)?.cause?.code || 'Unknown',
            fullError: error,
          });

          // Check if it's a connection error
          if ((error as any)?.cause?.code === 'ECONNREFUSED') {
            console.error('🚨 CMS Backend is not running on:', environment.apiBaseUrl);
            console.error('💡 Please start the CMS backend with: cd apps/cms && dotnet run');
          } else {
            console.error('🔍 CMS Backend is running but authentication failed. Check CMS logs for details.');
            console.error('🐛 This might be a Google ID token validation issue in the CMS backend.');
          }

          return false;
        }
      }

      console.log('SignIn: Provider not supported or not Google:', account?.provider);
      // For other providers, return false or implement accordingly
      return false;
    },
    async jwt({ token, user, account, trigger, session }) {
      // If this is a new sign-in, store the CMS data
      if (user && (user as any).cmsData) {
        const cmsData = (user as any).cmsData as SignInResponse;

        token.id = cmsData.user.id;
        token.accessToken = cmsData.accessToken;
        token.refreshToken = cmsData.refreshToken;
        token.expires = new Date(cmsData.expires);
        token.user = cmsData.user;
        token.tenantId = cmsData.tenantId;
        token.availableTenants = cmsData.availableTenants;
      }

      // Handle session updates (like tenant switching)
      if (trigger === 'update' && session) {
        if (session.currentTenant) {
          token.currentTenant = session.currentTenant;
        }
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

      console.log('Token expiry check:', {
        now: now.toISOString(),
        expiresAt: expiresAt?.toISOString(),
        isExpired: expiresAt ? now > expiresAt : false,
        hasRefreshToken: !!token.refreshToken,
        tokenSource: expiresAt ? (token.accessToken ? 'JWT_DECODE' : 'STORED_VALUE') : 'NONE',
      });

      if (expiresAt && now > expiresAt && token.refreshToken) {
        console.log('🔄 Token expired, attempting refresh...');
        try {
          const refreshResponse = await apiClient.refreshToken({
            refreshToken: token.refreshToken as string,
          });

          console.log('✅ Token refresh successful');
          token.accessToken = refreshResponse.accessToken;
          token.refreshToken = refreshResponse.refreshToken;
          token.expires = new Date(refreshResponse.expires);

          // Clear any previous errors
          delete token.error;
          delete token.error;
        } catch (error) {
          console.error('❌ Failed to refresh token:', error);
          // Token refresh failed, user needs to sign in again
          token.error = 'RefreshTokenError';
          token.accessToken = undefined;
          token.refreshToken = undefined;
        }
      }

      return token;
    },
    async session({ session, token }) {
      // Pass token data to the session
      session.accessToken = token.accessToken as string;
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

      // If there's a token error, include it in the session
      if (token.error) {
        (session as any).error = token.error;
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
    error?: string;
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
    error?: string;
  }
}

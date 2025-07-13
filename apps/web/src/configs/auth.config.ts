import Google from 'next-auth/providers/google';
import Credentials from 'next-auth/providers/credentials';
import { environment } from '@/configs/environment';
import { NextAuthConfig } from 'next-auth';
import { apiClient } from '@/lib/api/api-client';
import { SignInResponse } from '@/types/auth';
import { getJwtExpiryDate } from '@/lib/utils/jwt-utils';

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
    // Admin credentials provider for development
    Credentials({
      id: 'admin-bypass',
      name: 'Admin Login',
      credentials: {
        email: { label: 'Email', type: 'email' },
        password: { label: 'Password', type: 'password' },
      },
      async authorize(credentials) {
        if (!credentials?.email || !credentials?.password) {
          console.error('[NextAuth][admin-bypass] Missing credentials:', credentials);
          return null;
        }

        try {
          // Call the backend API for admin authentication
          console.log('[NextAuth][admin-bypass] Attempting admin login via backend API', credentials);
          const response = await apiClient.adminLogin({
            email: credentials.email,
            password: credentials.password,
          });

          console.log('[NextAuth][admin-bypass] Raw backend response:', response);

          if (!response || !response.user || !response.user.id || !response.accessToken) {
            console.error('[NextAuth][admin-bypass] Invalid backend response:', response);
            return null;
          }

          // Return the authenticated user with backend response data
          return {
            id: response.user.id,
            email: response.user.email,
            name: response.user.name || 'Admin User',
            image: null,
            // Store the backend response for later use in callbacks
            cmsData: response,
          };
        } catch (error) {
          console.error('[NextAuth][admin-bypass] Admin login failed:', error);
          if (error instanceof Error) {
            console.error('[NextAuth][admin-bypass] Error message:', error.message);
            if ((error as any).response) {
              console.error('[NextAuth][admin-bypass] Error response:', (error as any).response);
            }
          }
          return null;
        }
      },
    }),
  ],
  session: { strategy: 'jwt' },
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

      // Handle admin bypass provider
      if (account?.provider === 'admin-bypass') {
        console.log('🔧 [AUTH DEBUG] Admin credentials sign in successful');
        // For admin login, the user object already contains the backend response
        if ((user as any).cmsData) {
          console.log('✅ [AUTH DEBUG] Admin login backend response found');
          return true;
        } else {
          console.error('❌ [AUTH DEBUG] Admin login missing backend response');
          return false;
        }
      }

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
            console.error('💡 [AUTH DEBUG] Please start the CMS backend with: cd apps/cms && dotnet run');
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

      console.log('⏰ [AUTH DEBUG] Token expiry check:', {
        now: now.toISOString(),
        expiresAt: expiresAt?.toISOString(),
        isExpired: expiresAt ? now > expiresAt : false,
        hasRefreshToken: !!token.refreshToken,
        tokenSource: expiresAt ? (token.accessToken ? 'JWT_DECODE' : 'STORED_VALUE') : 'NONE',
        willAttemptRefresh: expiresAt && now > expiresAt && !!token.refreshToken,
      });

      if (expiresAt && now > expiresAt && token.refreshToken) {
        console.log('🔄 [AUTH DEBUG] Token expired, attempting refresh...');
        try {
          const refreshResponse = await apiClient.refreshToken({
            refreshToken: token.refreshToken as string,
          });

          console.log('✅ [AUTH DEBUG] Token refresh successful');
          token.accessToken = refreshResponse.accessToken;
          token.refreshToken = refreshResponse.refreshToken;
          token.expires = new Date(refreshResponse.expires);

          // Clear any previous errors
          delete token.error;
          delete token.error;
        } catch (error) {
          console.error('❌ [AUTH DEBUG] Failed to refresh token:', {
            error: error instanceof Error ? error.message : error,
            stack: error instanceof Error ? error.stack : 'No stack trace',
          });
          // Token refresh failed, user needs to sign in again
          token.error = 'RefreshTokenError';
          token.accessToken = undefined;
          token.refreshToken = undefined;
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

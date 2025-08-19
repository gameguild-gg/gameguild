import { environment } from '@/configs/environment';
import { googleIdTokenSignIn, localSign, refreshAccessToken } from '@/lib/auth/auth.actions';
import { SignInResponse } from '@/lib/auth/auth.types';
import { isUserWithAuthData } from '@/lib/auth/auth.utils';
import { logTokenDebugInfo, shouldRefreshToken } from '@/lib/utils/jwt-debug';
import { Account, DefaultSession, NextAuthConfig, Profile, Session, User } from 'next-auth';
import type { JWT } from 'next-auth/jwt';
import type { CredentialInput, Provider } from 'next-auth/providers';
import Credentials from 'next-auth/providers/credentials';
import GitHub from 'next-auth/providers/github';
import Google from 'next-auth/providers/google';



const providers: Provider[] = [
  Credentials({
    id: 'local',
    credentials: {
      email: { label: 'Email', type: 'text' },
      password: { label: 'Password', type: 'password' },
    },
    authorize: async (credentials): Promise<User | null> => {
      if (!credentials?.email || !credentials?.password) throw new Error('Email and password are required');

      try {
        const response = await localSign({
          email: credentials.email as string,
          password: credentials.password as string,
        });

        if (!response?.user) throw new Error('Invalid credentials');

        // Return user with additional auth data
        return {
          id: response.user.id,
          email: response.user.email,
          name: response.user.username,
          username: response.user.username,
          // Store auth tokens in the user object for JWT callback
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
          expiresAt: response.expiresAt,
          tenantId: response.tenantId,
          availableTenants: response.availableTenants,
        } as User & SignInResponse;
      } catch (error) {
        console.error('Local authentication failed:', error);
        throw new Error('Invalid credentials');
      }
    },
  }),
  GitHub,
  Google({
    clientId: environment.googleClientId,
    clientSecret: environment.googleClientSecret,
    profile(profile) {
      return {
        id: profile.sub,
        name: profile.name,
        email: profile.email,
        image: profile.picture,
      };
    },
  }),
];

export const authConfig: NextAuthConfig = {
  debug: true, // Enable debug logging to see what's happening
  pages: {
    signIn: '/sign-in',
    error: '/auth/error',
  },
  providers: providers,
  session: {
    strategy: 'jwt',
  },
  // Disable automatic URL verification to prevent DNS resolution loops
  trustHost: true,
  // Disable URL verification to prevent DNS resolution loops
  secret: process.env.NEXTAUTH_SECRET,
  // Override the base URL to use internal URL for all operations
  basePath: '',
  // Custom callback to prevent URL verification
  callbacks: {
    async redirect({ url, baseUrl }) {
      // Get the configured URL from environment, fallback to localhost
      const configuredUrl = process.env.NEXTAUTH_URL || 'http://localhost:3000';

      // If the URL is relative, make it absolute using the configured URL
      if (url.startsWith('/')) {
        return `${configuredUrl}${url}`;
      }

      // If the URL already starts with our configured URL, allow it
      if (url.startsWith(configuredUrl)) {
        return url;
      }

      // For any other case, redirect to the configured base URL
      return configuredUrl;
    },
    signIn: async ({ user, account }: { user: User; account?: Account | null; profile?: Profile; email?: { verificationRequest?: boolean }; credentials?: Record<string, CredentialInput> }): Promise<boolean> => {
      if (account?.provider === 'google') {
        if (!account?.id_token) return false;
        try {
          const response = await googleIdTokenSignIn({ idToken: account.id_token });

          // Safely extend the user object with auth data.
          Object.assign(user, {
            tenantId: response?.tenantId,
            accessToken: response?.accessToken,
            refreshToken: response?.refreshToken,
            expiresAt: response?.expiresAt,
            availableTenants: response?.availableTenants,
          });

          return true;
        } catch (error) {
          console.error('Google sign-in failed:', error);
          return false;
        }
      }
      // Allow local authentication.
      return account?.provider === 'local';
    },
    jwt: async ({ token, user, trigger, session, profile }: { token: JWT; user: User; account?: Account | null; profile?: Profile; trigger?: 'update' | 'signIn' | 'signUp'; isNewUser?: boolean; session?: Session }): Promise<JWT | null> => {
      // This is called when a user signing-in or signing-up.
      if (trigger === 'signIn' || trigger === 'signUp') {
        // We must have auth data from our API, otherwise the session is corrupted.

        if (!isUserWithAuthData(user)) {
          // If we don't have auth data, the authentication flow failed
          console.error('Authentication failed: No auth data from API');
          // Return null to prevent creating a broken JWT token
          return null;
        }

        token.id = user.id;
        token.username = user.username || user.name || '';
        token.email = user.email || '';

        // Set profile picture URL with priority: Google OAuth picture > DiceBear generated
        if (user.image) {
          // Use Google OAuth profile picture if available
          token.profilePictureUrl = user.image;
        } else {
          // Fall back to DiceBear generated avatar
          token.profilePictureUrl = `https://api.dicebear.com/7.x/avataaars/svg?seed=${user.name || user.email || 'default'}`;
        }

        token.api = {
          accessToken: user.accessToken || '',
          refreshToken: user.refreshToken || '',
        };
        token.availableTenants = user.availableTenants;
        // Set token expiration from the initial sign-in response
        if ((user as User & SignInResponse).expiresAt) {
          token.expiresAt = (user as User & SignInResponse).expiresAt;
        }

        // Set the current tenant from availableTenants (it must exist in the list)
        if (user.tenantId) {
          // Find a tenant in availableTenants
          const tenantFromList = user.availableTenants?.find((tenant) => tenant.id === user.tenantId);
          // Tenant not in an available list means that the session is corrupted.
          if (!tenantFromList) {
            console.error('Session corrupted: currentTenant not found in availableTenants');
            return null;
          }

          // Set the current tenant in the token
          token.currentTenant = tenantFromList;
        }

        return token;
      }

      // Check token expiration and refresh if needed
      if (token?.api?.refreshToken && token?.api?.accessToken) {
        // Debug the current access token
        if (process.env.NODE_ENV === 'development') {
          logTokenDebugInfo(token.api.accessToken, 'Current Access Token');
        }

        // Check if token needs refreshing using the debug utility
        const needsRefresh = shouldRefreshToken(token.api.accessToken, 30000); // 30s buffer

        if (needsRefresh) {
          console.log('🔄 Token needs refresh, attempting...');

          try {
            const response = await refreshAccessToken(token.api.refreshToken);

            if (response && response.accessToken && response.refreshToken) {
              // Update tokens
              token.api.accessToken = response.accessToken;
              token.api.refreshToken = response.refreshToken;
              
              // Debug the new access token
              if (process.env.NODE_ENV === 'development') {
                logTokenDebugInfo(response.accessToken, 'New Access Token');
              }
              
              // Update expiration times based on JWT payload
              try {
                const parts = response.accessToken.split('.');
                if (parts.length === 3) {
                  let payloadB64 = parts[1] || '';
                  while (payloadB64.length % 4) {
                    payloadB64 += '=';
                  }
                  
                  const payloadJson = Buffer.from(payloadB64, 'base64').toString('utf-8');
                  const payload = JSON.parse(payloadJson);
                  
                  if (payload.exp) {
                    token.accessTokenExpiresAt = new Date(payload.exp * 1000).toISOString();
                    console.log('✅ Access token exp (parsed):', token.accessTokenExpiresAt);
                  }
                }
              } catch (e) {
                console.warn('⚠️ Failed to parse new access token exp:', e);
              }
              
              // Update refresh token expiry from server response
              if (response.refreshTokenExpiresAt) {
                token.expiresAt = response.refreshTokenExpiresAt;
                console.log('✅ Refresh token exp (server):', token.expiresAt);
              } else if (response.expiresAt) {
                token.expiresAt = response.expiresAt;
                console.log('✅ Refresh token exp (legacy):', response.expiresAt);
              }
              
              console.log('✅ Token refresh completed successfully');
            } else {
              console.error('❌ Invalid refresh response structure');
              return null;
            }
          } catch (error) {
            console.error('❌ Token refresh failed:', error);
            // Set error flag to handle in session callback
            token.error = 'RefreshTokenError';
            return null;
          }
        } else {
          console.log('🔍 Token is still valid, no refresh needed');
        }
      } else if (!token?.api?.accessToken || !token?.api?.refreshToken) {
        console.error('❌ Missing required tokens in JWT callback');
        return null;
      }

      if (trigger === 'update') {
        // This is called when the user updates their profile or switches tenants.
        // Handle session updates with new user data
        if (session) {
          // Handle tenant switching
          if (session.currentTenant?.id && session.currentTenant.id !== token.currentTenant?.id) {
            // Validate that the new tenant exists in availableTenants
            const tenantFromList = token.availableTenants?.find((tenant) => tenant.id === session.currentTenant?.id);
            if (!tenantFromList) {
              // Tenant must exist in the available tenants list. Otherwise, the session is corrupted
              console.error('Session corrupted: refreshed tenantId not found in availableTenants');
              return null;
            }
            token.currentTenant = tenantFromList;
            console.log(`Switched to tenant: ${tenantFromList.name}`);
          }

          // Handle profile updates (username, email, etc.)
          if (session.user) {
            // Update user fields if they've changed
            if (session.user.username && session.user.username !== token.username) {
              token.username = session.user.username;
            }
            if (session.user.email && session.user.email !== token.email) {
              token.email = session.user.email;
            }
          }

          // Handle availableTenants updates (if a user gained/lost access to tenants)
          // TODO: Fetch new available tenants from the API if needed.
          if (session.availableTenants) {
            token.availableTenants = session.availableTenants;

            // Validate currentTenant is still in the updated availableTenants list
            if (token.currentTenant?.id) {
              // Tenant must exist in the available tenants list. Otherwise, the user is no longer in that tenant.
              const currentTenantStillAvailable = session.availableTenants.find((tenant) => tenant.id === token.currentTenant?.id);
              if (!currentTenantStillAvailable) {
                // If the current tenant is no longer available, clear it.
                token.currentTenant = undefined;
                console.log('No tenants available, cleared currentTenant');
              }
            }
          }
        }
      }

      return token;
    },
    session: async ({ session, token, trigger }: { session: Session; token: JWT | null; newSession?: Session; trigger?: 'update' }): Promise<Session | DefaultSession> => {
      // Check if the token is null (JWT callback returned null due to corruption or refresh failure)
      if (!token) {
        console.error('❌ Session callback: Token is null, returning error session');
        session.error = 'RefreshTokenError';
        return session;
      }

      // Check for refresh token error flag
      if (token.error === 'RefreshTokenError') {
        console.error('❌ Session callback: Refresh token error detected');
        session.error = 'RefreshTokenError';
        return session;
      }

      // Check if we have API tokens first - we need these for any API operations
      if (!token.api?.accessToken || !token.api?.refreshToken) {
        console.error('❌ Session callback: Missing API tokens');
        session.error = 'MissingTokensError';
        return session;
      }

      // Always set the session with the current valid token (refreshed in JWT callback if needed)
      session.api = {
        accessToken: token.api.accessToken,
      };

      if (trigger === 'update') {
        console.log('🔄 Session update triggered - data already processed in JWT callback');
      }

      // Add user data to the session
      if (token.id) session.user.id = token.id;
      if (token.username) session.user.username = token.username;
      if (token.email) session.user.email = token.email;
      if (token.profilePictureUrl) (session.user as any).profilePictureUrl = token.profilePictureUrl;
      if (token.availableTenants) session.availableTenants = token.availableTenants;
      if (token.currentTenant) session.currentTenant = token.currentTenant;

      return session;
    },
  },
};

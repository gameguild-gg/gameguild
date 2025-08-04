import { googleIdTokenSignIn, localSign, refreshAccessToken } from '@/lib/auth/auth.actions';
import { SignInResponse } from '@/lib/auth/auth.types';
import { isUserWithAuthData } from '@/lib/auth/auth.utils';
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
  Google,
];

export const authConfig: NextAuthConfig = {
  debug: !!process.env.AUTH_DEBUG,
  pages: {
    signIn: '/sign-in',
  },
  providers: providers,
  session: {
    strategy: 'jwt',
  },
  // Disable automatic URL verification to prevent DNS resolution loops
  trustHost: true,
  // Disable URL verification to prevent DNS resolution loops
  secret: process.env.NEXTAUTH_SECRET,
  // Use internal URL for callbacks
  basePath: '',
  // Disable URL verification entirely
  useSecureCookies: false,
  // Completely disable URL verification
  skipCSRFCheck: true,
  // Custom callback to prevent URL verification
  callbacks: {
    async redirect({ url, baseUrl }) {
      // Always return the original URL to prevent verification
      return url;
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
    jwt: async ({ token, user, trigger, session }: { token: JWT; user: User; account?: Account | null; profile?: Profile; trigger?: 'update' | 'signIn' | 'signUp'; isNewUser?: boolean; session?: Session }): Promise<JWT | null> => {
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
        token.username = user.name || '';
        token.email = user.email || '';
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

      // Check token expiration and refresh if needed (for all requests, not just updates)
      if (token?.api?.refreshToken && (!token?.expiresAt || Date.now() >= new Date(token.expiresAt).getTime())) {
        // Token expired or no expiration set, try to refresh it
        console.log('Token expired or missing expiration, attempting refresh...');
        try {
          const response = await refreshAccessToken(token.api.refreshToken);

          // Update token with new values
          if (response?.accessToken) {
            token.api.accessToken = response.accessToken;
            console.log('Access token refreshed successfully');
          }
          if (response?.refreshToken) {
            token.api.refreshToken = response.refreshToken;
          }
          if (response?.expiresAt) {
            token.expiresAt = response.expiresAt;
          }

          // Update tenant ID if present in refresh response
          if (response?.tenantId) {
            // Tenant must exist in the available tenants list. Otherwise, the session is corrupted
            const tenantFromList = token.availableTenants?.find((tenant) => tenant.id === response?.tenantId);
            if (!tenantFromList) {
              // Tenant not in an available list means that the session is corrupted.
              console.error('Session corrupted: refreshed tenantId not found in availableTenants');
              return null;
            }

            token.currentTenant = tenantFromList;
          }
        } catch (error) {
          console.error('Error refreshing api access token', error);
          // If we fail to refresh the token, return null to force the sign-out.
          return null;
        }
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
            if (session.user.displayName && session.user.displayName !== token.displayName) {
              token.displayName = session.user.displayName;
            }
            if (session.user.profilePictureUrl && session.user.profilePictureUrl !== token.profilePictureUrl) {
              token.profilePictureUrl = session.user.profilePictureUrl;
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
      // Check if the token is null (JWT callback returned null due to corruption)
      if (!token) {
        session.error = 'CorruptedSessionError';
        return session;
      }

      // Check if we have API tokens first - we need these for any API operations
      if (!token.api?.accessToken || !token.api?.refreshToken) {
        session.error = 'CorruptedSessionError';
        return session;
      }

      // Always set the session with the current valid token (refreshed in JWT callback if needed)
      session.api = {
        accessToken: token.api.accessToken,
      };

      if (trigger === 'update') {
        // Handle session updates when the user updates their profile or switches tenants.
        // The JWT callback has already processed the updates, so we just need to copy the data
        console.log('Session update triggered - data already processed in JWT callback');
      }

      // Add user ID, available tenants, and current tenant to the session
      if (token.id) session.user.id = token.id;
      if (token.username) session.user.username = token.username;
      if (token.email) session.user.email = token.email;
      if (token.availableTenants) session.availableTenants = token.availableTenants;
      if (token.currentTenant) session.currentTenant = token.currentTenant;

      return session;
    },
  },
};

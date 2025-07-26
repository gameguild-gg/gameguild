import { Account, DefaultSession, NextAuthConfig, Profile, Session, User } from 'next-auth';
import type { JWT } from 'next-auth/jwt';
import type { Provider } from 'next-auth/providers';
import Credentials from 'next-auth/providers/credentials';
import GitHub from 'next-auth/providers/github';
import Google from 'next-auth/providers/google';
import { fetchTenantDetails, googleIdTokenSignIn, localSign, refreshAccessToken } from '@/lib/auth/auth.actions';
import { SignInResponse } from '@/lib/auth/auth.types';
import { isUserWithAuthData } from '@/lib/auth/auth.utils';

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

        if (!response.user) throw new Error('Invalid credentials');

        // Return user with additional auth data
        return {
          id: response.user.id,
          email: response.user.email,
          name: response.user.username,
          // Store auth tokens in the user object for JWT callback
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
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
  callbacks: {
    signIn: async ({ user, account }) => {
      if (account?.provider === 'google') {
        if (!account?.id_token) return false;
        try {
          const response = await googleIdTokenSignIn({ idToken: account.id_token });

          // Safely extend the user object with auth data.
          Object.assign(user, {
            tenantId: response.tenantId,
            accessToken: response.accessToken,
            refreshToken: response.refreshToken,
            availableTenants: response.availableTenants,
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
    jwt: async ({
      token,
      user,
      // account,
      trigger,
      // session,
    }: {
      token: JWT;
      user: User;
      account?: Account | null | undefined;
      profile?: Profile | undefined;
      trigger?: 'update' | 'signIn' | 'signUp' | undefined;
      isNewUser?: boolean | undefined;
      session?: Session;
    }): Promise<JWT | null> => {
      if (trigger === 'signIn' || trigger === 'signUp') {
        // This is the first time the user is signing in or signing up.
        // We must have auth data from our API, otherwise the session is corrupted.

        if (!isUserWithAuthData(user)) {
          // If we don't have auth data, the authentication flow failed
          console.error('Authentication failed: No auth data from API');
          // Return null to prevent creating a broken JWT token
          return null;
        }

        token.id = user.id;
        token.api = {
          accessToken: user.accessToken || '',
          refreshToken: user.refreshToken || '',
        };
        token.availableTenants = user.availableTenants;

        // If we have a tenant ID, fetch the tenant details
        if (user.tenantId && user.accessToken) {
          try {
            const tenantDetails = await fetchTenantDetails(user.tenantId, user.accessToken);
            token.currentTenant = tenantDetails;
          } catch (error) {
            console.error('Failed to fetch tenant details:', error);
            // Fallback to basic tenant info
            token.currentTenant = { id: user.tenantId, name: `Tenant ${user.tenantId}`, isActive: true };
          }
        }
      } else if (trigger === 'update') {
        // This is called when the user updates their profile or switches tenants.
        // You can update the token with new user data here if needed.
        // TODO: Handle updates to their profile or switches tenants here.
      }
      return token;
    },
    session: async ({
      session,
      token,
      trigger,
    }: {
      session: Session;
      token: JWT | null;
      newSession?: Session;
      trigger?: 'update' | undefined;
    }): Promise<Session | DefaultSession> => {
      // Check if the token is null (JWT callback returned null due to corruption)
      if (!token) {
        session.error = 'CorruptedSessionError';
        return session;
      }

      // Check if we have API tokens first - we need these for any API operations including updates
      if (token && (!token.api?.accessToken || !token.api?.refreshToken)) {
        session.error = 'CorruptedSessionError';
        return session;
      }

      // Check token expiration and refresh if needed (handle expired tokens first)
      if (!token?.expiresAt || Date.now() >= new Date(token.expiresAt).getTime()) {
        // Token expired or no expiration set, try to refresh
        try {
          const response = await refreshAccessToken(token.api.refreshToken);

          // Update token with new values
          token.api.accessToken = response.accessToken;
          token.api.refreshToken = response.refreshToken;
          token.expiresAt = response.expiresAt;

          // Add refreshed token to session
          session.api = {
            accessToken: response.accessToken,
          };

          // Handle tenantId if present
          if (response.tenantId) {
            try {
              const tenantDetails = await fetchTenantDetails(response.tenantId, response.accessToken);
              token.currentTenant = tenantDetails;
              session.currentTenant = tenantDetails;
            } catch (error) {
              console.error('Failed to fetch tenant details after refresh:', error);
              // Fallback to basic tenant info
              token.currentTenant = { id: response.tenantId, name: `Tenant ${response.tenantId}`, isActive: true };
              session.currentTenant = token.currentTenant;
            }
          }
        } catch (error) {
          console.error('Error refreshing api access token', error);
          // If we fail to refresh the token, return an error so we can handle it on the page.
          session.error = 'RefreshTokenError';
          return session;
        }
      }

      // Token is still valid, add it to the session
      session.api = {
        accessToken: token.api.accessToken,
      };

      if (trigger === 'update') {
        // TODO: Handle session updates when the user updates their profile or switches tenants.
        // This is where you can update the session with new user data.
      }

      // Add user ID and current tenant to the session
      if (token.id) session.user.id = token.id;
      if (token.currentTenant) session.currentTenant = token.currentTenant;
      if (token.availableTenants) session.availableTenants = token.availableTenants;

      return session;
    },
  },
};

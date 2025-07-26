import { Account, DefaultSession, NextAuthConfig, Profile, Session, User } from 'next-auth';
import type { JWT } from 'next-auth/jwt';
import type { Provider } from 'next-auth/providers';
import GitHub from 'next-auth/providers/github';
import Google from 'next-auth/providers/google';
import { googleIdTokenSignIn, refreshAccessToken } from '@/lib/auth/auth.actions';
import { SignInResponse } from '@/lib/auth/types';

const providers: Provider[] = [
  Credentials({
    id: 'local',
    credentials: {
      email: { label: 'Email', type: 'text' },
      password: { label: 'Password', type: 'password' },
    },
    authorize: async (credentials: Partial<Record<string, unknown>>, request: Request): Promise<User | null> => {
      const user = null;

      // TODO: Implement local authentication logic here.

      if (!user) throw new Error('Invalid credentials');

      return user;
    },
  }),
  GitHub,
  Google,
  // Google({
  //   clientId: environment.googleClientId,
  //   clientSecret: environment.googleClientSecret,
  //   authorization: {
  //     params: {
  //       request_uri: environment.signInGoogleCallbackUrl,
  //     },
  //   },
  // }),
];

export const authConfig: NextAuthConfig = {
  debug: !!process.env.AUTH_DEBUG,
  pages: {
    signIn: '/sign-in',
  },
  providers: providers,
  session: {
    strategy: 'jwt',
    // maxAge: 7 * 24 * 60 * 60, // 7 days in seconds
    // updateAge: 60 * 60, // 1 hour in seconds
  },
  callbacks: {
    signIn: async ({ user, account }) => {
      if (account?.provider === 'google') {
        if (!account?.id_token) return false;
        try {
          const response = await googleIdTokenSignIn({ idToken: account.id_token });

          // TODO: Check if this casting is safe.
          const payload = user as unknown as SignInResponse;
          // TODO: Check if the response is valid and contains the necessary data.

          payload.tenantId = response.tenantId;
          payload.accessToken = response.accessToken;
          payload.refreshToken = response.refreshToken;
          payload.availableTenants = response.availableTenants;

          // TODO: Check if this casting is safe.
          (user as unknown as SignInResponse) = payload;

          return true;
        } catch (error) {
          console.error('Google sign-in failed:', error);
          return false;
        }
      }

      return false;
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
        // We can initialize the token with user data.

        // TODO: Check if this casting is safe.
        const payload = user as unknown as SignInResponse;
        token.id = user.id;
        token.api = {
          accessToken: payload.accessToken,
          refreshToken: payload.refreshToken,
        };
        token.availableTenants = payload.availableTenants;

        // TODO: Fetch tenant info here.
        token.currentTenant = payload.tenantId ? { id: payload.tenantId, name: '', isActive: true } : undefined;
      } else if (trigger === 'update') {
        // This is called when the user updates their profile or switches tenants.
        // You can update the token with new user data here if needed.
        // TODO: Handle updates to their profile or switches tenants here.
      }
      return null;
    },
    session: async ({
      session,
      token,
      trigger,
    }: {
      session: Session;
      token: JWT;
      newSession?: Session;
      trigger?: 'update' | undefined;
    }): Promise<Session | DefaultSession> => {
      if (trigger === 'update') {
        // TODO: Handle session updates when the user updates their profile or switches tenants.
        // This is where you can update the session with new user data.
      }

      // TODO: Check the token expiration checks and refresh logic.
      // If the token is still valid, return the session as is.
      if (token?.expiresAt && Date.now() < new Date(token.expiresAt).getTime()) return session;

      // Otherwise, refresh the session.
      try {
        //
        const response = await refreshAccessToken(token.api.refreshToken);

        const { accessToken, refreshToken, expiresAt } = response;

        // TODO: Handle tenantId.

        token.api.accessToken = accessToken;
        token.api.refreshToken = refreshToken;
        token.expiresAt = expiresAt;

        session.api.accessToken = accessToken;
      } catch (error) {
        console.error('Error refreshing api access token', error);
        // If we fail to refresh the token, return an error so we can handle it on the page.
        session.error = 'RefreshTokenError';
      }

      return session;
    },
  },
};

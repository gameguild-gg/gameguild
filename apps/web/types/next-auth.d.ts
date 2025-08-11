import type { Tenant } from '@/components/tenant';
// The `JWT` interface can be found in the `next-auth/jwt` submodule

declare module 'next-auth' {
  /**
   * The shape of the user object returned in the OAuth providers' `profile` callback,
   * or the second parameter of the `session` callback, when using a database.
   */
  interface User {
    id: string;
    // displayName: string;
    username?: string;
    email?: string;
    name?: string;
    image?: string;
    profilePictureUrl?: string;
  }

  /**
   * The shape of the account object returned in the OAuth providers' `account` callback,
   * Usually contains information about the provider being used, like OAuth tokens (`access_token`, etc).
   */
  // eslint-disable-next-line @typescript-eslint/no-empty-object-type
  interface Account { }

  /**
   * Returned by `useSession`, `auth`, contains information about the active session.
   */
  interface Session {
    user: {
      id: string;
      displayName?: string;
      username: string;
      email?: string;
      profilePictureUrl?: string;
    };
    currentTenant?: Tenant;
    availableTenants?: Tenant[];
    api: {
      accessToken?: string;
    };
    error?: 'RefreshTokenError' | 'CorruptedSessionError';
  }
}

declare module 'next-auth/jwt' {
  /** Returned by the `jwt` callback and `auth`, when using JWT sessions */
  interface JWT {
    id: string;
    username?: string;
    email?: string;
    displayName?: string;
    profilePictureUrl?: string;
    api: {
      accessToken: string;
      refreshToken: string;
    };
    availableTenants?: Array<Tenant>;
    currentTenant?: Tenant;
    expiresAt?: string;
  }
}

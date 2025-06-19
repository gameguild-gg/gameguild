import { DefaultSession, DefaultUser } from 'next-auth';
import { JWT, DefaultJWT } from 'next-auth/jwt';

declare module 'next-auth' {
  interface Session extends DefaultSession {
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
    user: {
      id: string;
      username?: string;
      email?: string;
      name?: string;
      image?: string;
    };
  }

  interface User extends DefaultUser {
    id: string;
    username?: string;
  }
}

declare module 'next-auth/jwt' {
  interface JWT extends DefaultJWT {
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


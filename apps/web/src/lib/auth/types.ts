/**
 * NextAuth.js type extensions and utilities
 */

import { User, DefaultSession, DefaultJWT } from 'next-auth';
import { SignInResponse, TenantInfo } from '@/components/legacy/types/auth';

// Extended user type for NextAuth.js
export interface ExtendedUser extends User {
  id: string;
  username?: string;
  cmsData?: SignInResponse;
  accessToken?: string;
  refreshToken?: string;
  tenantId?: string;
  availableTenants?: TenantInfo[];
}

// Extended session type
export interface ExtendedSession extends DefaultSession {
  accessToken?: string;
  tenantId?: string;
  availableTenants?: TenantInfo[];
  currentTenant?: TenantInfo;
  error?: string;
  user: {
    id: string;
    email: string;
    name?: string;
    image?: string;
    username?: string;
  };
}

// Extended JWT type
export interface ExtendedJWT extends DefaultJWT {
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
  availableTenants?: TenantInfo[];
  currentTenant?: TenantInfo;
  error?: string;
}

// Type guards
export function isExtendedUser(user: any): user is ExtendedUser {
  return user && typeof user === 'object' && typeof user.id === 'string';
}

export function hasRefreshTokenError(session: any): boolean {
  return session?.error === 'RefreshTokenError';
}

export function isValidCmsData(data: any): data is SignInResponse {
  return (
    data &&
    typeof data === 'object' &&
    typeof data.accessToken === 'string' &&
    typeof data.refreshToken === 'string' &&
    data.user &&
    typeof data.user.id === 'string'
  );
}

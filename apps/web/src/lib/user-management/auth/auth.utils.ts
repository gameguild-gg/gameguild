import { User, Session } from 'next-auth';
import { SignInResponse } from './auth.types';

/**
 * Type guard to check if user has auth data from our API
 * This applies to both local and OAuth authentication responses
 */
export const isUserWithAuthData = (user: User): user is User & Partial<SignInResponse> => {
  return 'tenantId' in user || 'accessToken' in user || 'refreshToken' in user;
};

/**
 * Check if a session has refresh token errors
 */
export const hasRefreshTokenError = (session: Session | null): boolean => {
  return session?.error === 'RefreshTokenError';
};

import { User } from 'next-auth';
import { SignInResponse } from './auth.types';

/**
 * Type guard to check if user has auth data from our API
 * This applies to both local and OAuth authentication responses
 */
export const isUserWithAuthData = (user: User): user is User & Partial<SignInResponse> => {
  return 'tenantId' in user || 'accessToken' in user || 'refreshToken' in user;
};

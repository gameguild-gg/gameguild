import { User, Session } from 'next-auth';
import { useEffect } from 'react';
import { SignInResponse } from './auth.types';

/**
 * Type guard to check if user has auth data from our API
 * This applies to both local and OAuth authentication responses
 */
export const isUserWithAuthData = (user: User): user is User & Partial<SignInResponse> => {
  return 'tenantId' in user || 'accessToken' in user || 'refreshToken' in user;
};

/**
 * Client-side utility to handle session corruption
 * Forces a sign-out and redirect to sign-in page
 */
export const handleSessionCorruption = (): void => {
  console.error('Session corrupted - forcing sign out');

  // Clear any local storage/session storage auth data
  if (typeof window !== 'undefined') {
    localStorage.removeItem('auth-token');
    sessionStorage.removeItem('auth-token');

    // Force redirect to sign-in page
    window.location.href = '/sign-in?error=CorruptedSessionError';
  }
};

/**
 * Check if a session has corruption errors
 */
export const isSessionCorrupted = (session: Session | null): boolean => {
  return session?.error === 'CorruptedSessionError';
};

/**
 * Check if a session has refresh token errors
 */
export const hasRefreshTokenError = (session: Session | null): boolean => {
  return session?.error === 'RefreshTokenError';
};

/**
 * React hook to automatically handle session corruption
 * Use this in your main app component or layout
 *
 * @example
 * ```tsx
 * function App() {
 *   const { data: session } = useSession();
 *   useSessionCorruptionHandler(session);
 *
 *   return <YourAppContent />;
 * }
 * ```
 */
export const useSessionCorruptionHandler = (session: Session | null): void => {
  useEffect(() => {
    if (isSessionCorrupted(session)) {
      handleSessionCorruption();
    }
  }, [session]);
};

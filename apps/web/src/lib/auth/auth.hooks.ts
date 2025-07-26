import { useEffect } from 'react';
import { Session } from 'next-auth';

/**
 * Check if a session has corruption errors
 */
export const isSessionCorrupted = (session: Session | null): boolean => {
  return session?.error === 'CorruptedSessionError';
};

/**
 * Client-side utility to handle session corruption
 * Forces a sign-out and redirect to the sign-in page
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
 * React hook to automatically handle session corruption
 * Use this in your main app part or layout
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
    if (isSessionCorrupted(session)) handleSessionCorruption();
  }, [session]);
};

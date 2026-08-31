'use client';

/**
 * useSession Hook
 *
 * Access the current authentication session from any React component.
 * Must be used within a <SessionProvider>.
 *
 * @example
 * ```tsx
 * import { useSession } from '@game-guild/client/react';
 *
 * function UserProfile() {
 *   const { data: session, status } = useSession();
 *
 *   if (status === 'loading') return <Spinner />;
 *   if (status === 'unauthenticated') return <SignInButton />;
 *
 *   return <p>Welcome, {session.user.name}!</p>;
 * }
 * ```
 *
 * With required auth (redirects to sign-in if not authenticated):
 * ```tsx
 * const { data: session } = useSession({ required: true });
 * ```
 */

import { useContext, useEffect } from 'react';
import { SessionContext } from './session-provider.js';
import type { UseSessionReturn } from '../../runtime/auth/types.js';

/**
 * Options for useSession
 */
export interface UseSessionOptions {
  /**
   * If true, redirects to the sign-in page when not authenticated.
   * @default false
   */
  required?: boolean;

  /**
   * Custom callback when authentication is required but user is not authenticated.
   * Default behavior: redirect to /sign-in
   */
  onUnauthenticated?: () => void;
}

/**
 * Hook to access the current session.
 *
 * @param options - Configuration options
 * @returns Session data, status, and update function
 * @throws Error if used outside of <SessionProvider>
 */
export function useSession(options?: UseSessionOptions): UseSessionReturn {
  const context = useContext(SessionContext);

  if (context === undefined) {
    throw new Error('useSession must be used within a <SessionProvider>. ' + 'Wrap your app with <SessionProvider> in your root layout.');
  }

  const { required = false, onUnauthenticated } = options ?? {};

  // Handle required auth
  useEffect(() => {
    if (!required) return;
    if (context.status === 'loading') return;
    /* v8 ignore start */
    if (context.status === 'unauthenticated') {
      /* v8 ignore stop */
      if (onUnauthenticated) {
        onUnauthenticated();
      } else {
        // Default: redirect to sign-in
        if (typeof window !== 'undefined') {
          const callbackUrl = encodeURIComponent(window.location.href);
          window.location.href = `/sign-in?callbackUrl=${callbackUrl}`;
        }
      }
    }
  }, [required, context.status, onUnauthenticated]);

  return context as UseSessionReturn;
}

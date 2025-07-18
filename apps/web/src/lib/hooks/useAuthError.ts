'use client';

import { signOut, useSession } from 'next-auth/react';
import { useCallback, useEffect } from 'react';
import { useRouter } from 'next/navigation';

export function useAuthError() {
  const { data: session } = useSession();
  const router = useRouter();

  const handleAuthError = useCallback(
    async (forceSignOut = false) => {
      if (session?.error === 'RefreshTokenError' || forceSignOut) {
        console.log('🚨 [AUTH ERROR] Token refresh failed, signing out user');

        try {
          // Sign out the user and redirect to sign-in page
          await signOut({
            callbackUrl: '/sign-in',
            redirect: true,
          });
        } catch (error) {
          console.error('❌ [AUTH ERROR] Failed to sign out:', error);
          // Fallback: force redirect to sign-in page
          router.push('/sign-in');
        }
      }
    },
    [session?.error, router],
  );

  useEffect(() => {
    handleAuthError();
  }, [handleAuthError]);

  return {
    hasError: !!session?.error,
    error: session?.error,
    handleAuthError,
    isRefreshTokenError: session?.error === 'RefreshTokenError',
  };
}

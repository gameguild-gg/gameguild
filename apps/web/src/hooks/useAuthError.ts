'use client';

import { signOut, useSession } from 'next-auth/react';
import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

export function useAuthError() {
  const { data: session } = useSession();
  const router = useRouter();

  useEffect(() => {
    if (session?.error === 'RefreshTokenError') {
      // Token refresh failed, redirect to sign-in
      signOut({
        callbackUrl: '/auth/signin',
        redirect: true,
      });
    }
  }, [session?.error, router]);

  return {
    hasError: !!session?.error,
    error: session?.error,
  };
}

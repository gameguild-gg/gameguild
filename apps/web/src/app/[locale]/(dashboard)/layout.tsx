import React, { PropsWithChildren } from 'react';
import { redirect } from 'next/navigation';
import { auth } from '@/auth';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  const session = await auth();

  // Redirect to sign-in if not authenticated
  if (!session) {
    redirect('/sign-in');
  }

  // Redirect to sign-in if there's a token refresh error
  if (session.error === 'RefreshTokenError') {
    redirect('/sign-in');
  }

  return <>{children}</>;
}

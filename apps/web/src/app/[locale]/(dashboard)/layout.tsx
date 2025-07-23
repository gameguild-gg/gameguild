import React, { PropsWithChildren } from 'react';
import { redirect } from 'next/navigation';
import { auth } from '@/auth';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  const session = await auth();

  // Check if user is authenticated (either regular user or admin)
  if (!session) redirect('/sign-in');

  // TODO: FIX IT later.
  if (session.error === 'RefreshTokenError') redirect('/');

  return <>{children}</>;
}

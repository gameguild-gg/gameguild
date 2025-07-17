import React, { PropsWithChildren } from 'react';
import { redirect } from 'next/navigation';
import { auth } from '@/auth';
import { PropsWithLocaleParams } from '@/types';

export default async function Layout({ children, params }: PropsWithChildren<PropsWithLocaleParams>): Promise<React.JSX.Element> {
  const session = await auth();

  // Check if user is authenticated (either regular user or admin)
  if (!session) redirect('/sign-in');

  if (session.error === 'RefreshTokenError') redirect('/sign-in');

  return <>{children}</>;
}

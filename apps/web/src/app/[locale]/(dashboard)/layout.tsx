import React, { PropsWithChildren } from 'react';
import { redirect } from 'next/navigation';
import { auth } from '@/auth';

interface LayoutProps extends PropsWithChildren {
  params?: { locale?: string };
}

export default async function Layout({ children, params }: LayoutProps): Promise<React.JSX.Element> {
  const session = await auth();

  // Check if user is authenticated (either regular user or admin)
  if (!session) redirect('/sign-in');

  if (session.error === 'RefreshTokenError') redirect('/sign-in');

  return <>{children}</>;
}

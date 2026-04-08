import React from 'react';
import { redirect } from 'next/navigation';
import { auth } from '@/auth';
import { DashboardShell } from '@/components/layout';

export default async function Layout({ children }: LayoutProps<'/[locale]/dashboard'>): Promise<React.JSX.Element> {
  // auth() internally handles token refresh and cookie persistence
  const session = await auth();

  if (!session) {
    redirect('/sign-in');
  }

  return <DashboardShell>{children}</DashboardShell>;
}

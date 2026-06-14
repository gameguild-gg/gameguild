import { auth } from '@/auth';
import { DashboardShell } from '@/components/layout';
import { redirect } from '@/i18n/navigation';
import { getDashboardNotificationSummary } from '@/lib/dashboard-notifications';
import React from 'react';

export default async function Layout({ children, params }: LayoutProps<'/[locale]/dashboard'>): Promise<React.JSX.Element> {
  const { locale } = await params;

  // auth() internally handles token refresh and cookie persistence
  const session = await auth();

  if (!session) {
    // Preserve locale via the i18n-aware redirect helper
    redirect({ href: { pathname: '/sign-in', query: { callbackUrl: '/dashboard' } }, locale });
    throw new Error('Unauthenticated dashboard access');
  }

  const notifications = await getDashboardNotificationSummary(session.user.id);

  return <DashboardShell notifications={notifications}>{children}</DashboardShell>;
}

import { auth } from '@/auth';
import { ConsoleShell } from '@/components/console/console-shell';
import { redirect } from '@/i18n/navigation';
import { getDashboardNotificationSummary } from '@/lib/dashboard-notifications';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { forbidden } from 'next/navigation';
import React from 'react';

export default async function Layout({ children, params }: LayoutProps<'/[locale]/dashboard'>): Promise<React.JSX.Element> {
  const { locale } = await params;

  // auth() internally handles token refresh and cookie persistence
  const session = await auth();

  if (!session || typeof session === 'function') {
    // Preserve locale via the i18n-aware redirect helper
    redirect({ href: { pathname: '/sign-in', query: { callbackUrl: '/dashboard' } }, locale });
    throw new Error('Unauthenticated dashboard access');
  }

  const [notifications, dashboardContexts] = await Promise.all([
    getDashboardNotificationSummary(session.user.id),
    getDashboardContexts(),
  ]);
  if (dashboardContexts.capabilities.length === 0) {
    forbidden();
  }
  const dashboardUser = {
    id: session.user.id,
    name: session.user.name?.trim() || session.user.email?.split('@')[0] || 'GameGuild user',
    email: session.user.email ?? 'No email available',
    image: session.user.image ?? null,
  };

  return (
    <ConsoleShell
      notifications={notifications}
      user={dashboardUser}
      capabilities={dashboardContexts.capabilities}
      contexts={dashboardContexts.contexts}
    >
      {children}
    </ConsoleShell>
  );
}

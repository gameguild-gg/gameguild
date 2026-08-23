import { auth } from '@/auth';
import { ConsoleShell } from '@/components/console/console-shell';
import { AccessibilitySyncInitializer } from '@/components/settings/accessibility-sync-initializer';
import { EditorPreferencesSyncInitializer } from '@/components/settings/editor-preferences-sync-initializer';
import { ThemeSyncInitializer } from '@/components/settings/theme-sync-initializer';
import { redirect } from '@/i18n/navigation';
import { getDashboardNotificationSummary } from '@/lib/dashboard-notifications';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import React from 'react';

export default async function Layout({ children, params }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  const { locale } = await params;

  const session = await auth();

  if (!session || typeof session === 'function') {
    redirect({ href: { pathname: '/sign-in', query: { callbackUrl: '/workspace' } }, locale });
    throw new Error('Unauthenticated dashboard access');
  }

  const [notifications, dashboardContexts] = await Promise.all([
    getDashboardNotificationSummary(session.user.id),
    getDashboardContexts(),
  ]);
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
      <ThemeSyncInitializer />
      <AccessibilitySyncInitializer />
      <EditorPreferencesSyncInitializer />
      {children}
    </ConsoleShell>
  );
}

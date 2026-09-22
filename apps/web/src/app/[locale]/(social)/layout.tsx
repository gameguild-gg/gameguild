import { auth } from '@/auth';
import { SocialAppShell } from '@/components/feed/social-app-shell';
import { AccessibilitySyncInitializer } from '@/components/settings/accessibility-sync-initializer';
import { EditorPreferencesSyncInitializer } from '@/components/settings/editor-preferences-sync-initializer';
import { ThemeSyncInitializer } from '@/components/settings/theme-sync-initializer';
import { redirect } from '@/i18n/navigation';
import { getDashboardNotificationSummary } from '@/lib/dashboard-notifications';
import React from 'react';

export default async function Layout({ children, params }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  const { locale } = await params;
  const session = await auth();

  if (!session || typeof session === 'function') {
    // Home is the default post-sign-in destination — no callbackUrl needed.
    redirect({ href: { pathname: '/sign-in' }, locale });
    throw new Error('Unauthenticated social access');
  }

  const notifications = await getDashboardNotificationSummary(session.user.id);
  const socialUser = {
    id: session.user.id,
    name: session.user.name?.trim() || session.user.email?.split('@')[0] || 'GameGuild member',
    email: session.user.email ?? 'No email available',
    image: session.user.image ?? null,
  };

  return (
    <SocialAppShell user={socialUser} notifications={notifications}>
      <ThemeSyncInitializer />
      <AccessibilitySyncInitializer />
      <EditorPreferencesSyncInitializer />
      {children}
    </SocialAppShell>
  );
}

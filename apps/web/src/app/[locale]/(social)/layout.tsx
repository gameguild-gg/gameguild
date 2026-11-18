import { auth } from '@/auth';
import { SocialAppShell } from '@/components/feed/social-app-shell';
import { AppShellHeaderMenu } from '@/components/app/app-shell-header-menu';
import { AccessibilitySyncInitializer } from '@/components/settings/accessibility-sync-initializer';
import { EditorPreferencesSyncInitializer } from '@/components/settings/editor-preferences-sync-initializer';
import { ThemeSyncInitializer } from '@/components/settings/theme-sync-initializer';
import { redirect } from '@/i18n/navigation';
import { headers } from 'next/headers';
import React, { Suspense } from 'react';
import { NotificationsSlot } from './notifications-slot';

/**
 * The browser-visible path the proxy forwarded (issue #864): layouts receive
 * no search params, so the fallback guard recovers the real target for the
 * sign-in callbackUrl instead of losing it. Same pattern as the
 * (dashboards) layout; non-slash-relative values are rejected.
 */
async function currentPublicPath(): Promise<string | null> {
  try {
    const forwarded = (await headers()).get('x-gameguild-path');
    return forwarded?.startsWith('/') && !forwarded.startsWith('//') ? forwarded : null;
  } catch {
    return null;
  }
}

export default async function Layout({ children, params }: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  const { locale } = await params;
  const session = await auth();

  if (!session || typeof session === 'function') {
    // Fallback guard for when the proxy-level authorized callback misses:
    // bounce to /sign-in with the original target as callbackUrl so the
    // post-sign-in flow returns where the visitor was.
    const callbackUrl = (await currentPublicPath()) ?? '/feed';
    redirect({ href: { pathname: '/sign-in', query: { callbackUrl } }, locale });
    throw new Error('Unauthenticated social access');
  }

  const socialUser = {
    id: session.user.id,
    name: session.user.name?.trim() || session.user.email?.split('@')[0] || 'GameGuild member',
    email: session.user.email ?? 'No email available',
    image: session.user.image ?? null,
  };

  // Streams the bell behind Suspense; the chrome-only fallback keeps the shell
  // paint off the notifications critical path and contains summary failures.
  const notificationsMenu = (
    <Suspense fallback={<AppShellHeaderMenu user={socialUser} />}>
      <NotificationsSlot userId={session.user.id} user={socialUser} />
    </Suspense>
  );

  return (
    <SocialAppShell user={socialUser} notificationsMenu={notificationsMenu}>
      <ThemeSyncInitializer />
      <AccessibilitySyncInitializer />
      <EditorPreferencesSyncInitializer />
      {children}
    </SocialAppShell>
  );
}

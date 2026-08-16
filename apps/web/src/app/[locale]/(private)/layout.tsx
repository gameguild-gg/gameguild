import { auth } from '@/auth';
import { redirect } from '@/i18n/navigation';
import { WorkspaceShell } from '@/components/workspace/workspace-shell';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { getDashboardNotificationSummary } from '@/lib/dashboard-notifications';
import { getWorkspaceTeams } from '@/lib/workspaces';
import React from 'react';

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return 'GG';
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
}

/**
 * Member-private surface (/workspace/*). The layout owns the auth context:
 * every child route renders for the signed-in user.
 */
export default async function PrivateLayout({
  children,
  params,
}: LayoutProps<'/[locale]'>): Promise<React.JSX.Element> {
  const [{ locale }, session] = await Promise.all([params, auth()]);
  if (!session || typeof session === 'function') {
    redirect({ href: '/sign-in', locale });
    throw new Error('Unauthenticated private area access');
  }

  const displayName = session.user.name?.trim() || session.user.email?.split('@')[0] || 'Member';
  const [contexts, teams, notifications] = await Promise.all([
    getDashboardContexts(),
    getWorkspaceTeams(),
    getDashboardNotificationSummary(session.user.id).catch(() => ({ items: [], unreadCount: 0 })),
  ]);

  return (
    <WorkspaceShell
      user={{
        name: displayName,
        email: session.user.email ?? '',
        initials: initials(displayName),
        canManage: contexts.capabilities.length > 0,
      }}
      teams={teams.map((team) => ({ id: team.id, slug: team.slug, name: team.name, isPersonal: team.isPersonal }))}
      notifications={notifications}
    >
      {children}
    </WorkspaceShell>
  );
}

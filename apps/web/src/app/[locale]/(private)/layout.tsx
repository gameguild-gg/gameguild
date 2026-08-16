import { auth } from '@/auth';
import { redirect } from '@/i18n/navigation';
import { WorkspaceShell } from '@/components/workspace/workspace-shell';
import React from 'react';

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

  return await WorkspaceShell({ children });
}

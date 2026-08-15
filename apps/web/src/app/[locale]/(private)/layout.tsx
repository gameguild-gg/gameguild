import { auth } from '@/auth';
import { redirect } from '@/i18n/navigation';
import { AppShell } from '@/components/app/app-shell';
import React from 'react';

/**
 * Member-private surface (/settings, /invitations, /teams, /projects, /work).
 * The layout owns the auth context: every child route renders for the
 * signed-in user without a per-page identity prefix.
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

  const shell = await AppShell({ children });
  return <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6 lg:px-8">{shell}</main>;
}

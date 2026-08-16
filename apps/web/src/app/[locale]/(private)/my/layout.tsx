import { auth } from '@/auth';
import { redirect } from '@/i18n/navigation';
import React from 'react';

/** Authenticated member workspace, deliberately separate from the admin dashboard. */
export default async function MyLayout({ children, params }: { children: React.ReactNode; params: Promise<{ locale: string }> }): Promise<React.JSX.Element> {
  const [{ locale }, session] = await Promise.all([params, auth()]);
  if (!session || typeof session === 'function') {
    redirect({ href: { pathname: '/sign-in', query: { callbackUrl: '/my' } }, locale });
    throw new Error('Unauthenticated personal workspace access');
  }

  return <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6 lg:px-8">{children}</main>;
}

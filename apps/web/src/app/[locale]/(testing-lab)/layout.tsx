import React, { PropsWithChildren } from 'react';
import { redirect } from 'next/navigation';
import { auth } from '@/auth';
import { PropsWithLocaleParams } from '@/types';

export default async function Layout({ children, params }: PropsWithChildren<PropsWithLocaleParams>): Promise<React.JSX.Element> {
  const session = await auth();

  // Check if user is authenticated (either regular user or admin)
  // if (!session) redirect('/sign-in');
  //
  // if (session.error === 'RefreshTokenError') redirect('/sign-in');

  return (
    <>
      <div className="flex flex-col flex-1 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white">
        {/* This is the main content area where children components will be rendered */}
        {children}
      </div>
    </>
  );
}

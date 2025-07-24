import React, { PropsWithChildren } from 'react';
import { redirect } from 'next/navigation';
import { auth } from '@/auth';
import { SyncProvider } from '@/lib/sync/sync-provider';
import { ErrorBoundaryProvider } from '@/components/errors/error-boundary-provider';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  const session = await auth();

  // Check if the user is authenticated (either regular users or administrators)
  if (!session) redirect('/sign-in');

  // TODO: FIX IT later.
  if (session.error === 'RefreshTokenError') redirect('/');

  return (
    <>
      <ErrorBoundaryProvider config={{ level: 'page', enableRetry: true, maxRetries: 3, reportToAnalytics: true, isolate: false }}>
        {/* <SyncProvider> */}
        {children}
        {/* </SyncProvider> */}
      </ErrorBoundaryProvider>
    </>
  );
}

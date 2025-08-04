import { auth } from '@/auth';
import { ErrorBoundaryProvider } from '@/components/common/errors/error-boundary-provider';
import { redirect } from 'next/navigation';
import React, { PropsWithChildren } from 'react';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  const session = await auth();

  // Check if the user is authenticated (either regular users or administrators)
  if (!session) redirect('/sign-in');

  return (
    <>
      <ErrorBoundaryProvider config={{ level: 'page', enableRetry: true, maxRetries: 3, reportToAnalytics: true, isolate: true }}>{children}</ErrorBoundaryProvider>
    </>
  );
}

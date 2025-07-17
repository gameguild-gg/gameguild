import React, { PropsWithChildren } from 'react';
import { Dashboard } from '@/components/dashboard/dashboard';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <>
      <Dashboard>{children}</Dashboard>
    </>
  );
}

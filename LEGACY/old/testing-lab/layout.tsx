import type { Metadata } from 'next';
import React, { PropsWithChildren } from 'react';

export const metadata: Metadata = {
  title: 'Testing Lab | Game Guild',
  description: 'Submit, test, and manage game projects for Capstone teams',
};

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return <>{children}</>;
}

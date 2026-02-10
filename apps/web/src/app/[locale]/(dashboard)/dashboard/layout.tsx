import React from 'react';
import { DashboardShell } from '@/components/layout';

export default async function Layout({ children }: LayoutProps<'/[locale]/dashboard'>): Promise<React.JSX.Element> {
  return <DashboardShell>{children}</DashboardShell>;
}

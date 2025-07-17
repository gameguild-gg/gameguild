import React, { PropsWithChildren } from 'react';
import { cookies } from 'next/headers';
import { DashboardProvider } from './layout/dashboard-provider';

export const Dashboard = async ({ children }: PropsWithChildren) => {
  const cookieStore = await cookies();
  const defaultOpen = cookieStore.get('sidebar_state')?.value === 'true';

  return <DashboardProvider defaultOpen={defaultOpen}>{children}</DashboardProvider>;
};

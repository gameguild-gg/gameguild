import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Reports</DashboardPageTitle>
        <DashboardPageDescription></DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <div className="text-center text-muted-foreground">Reports functionality coming soon...</div>
      </DashboardPageContent>
    </DashboardPage>
  );
}

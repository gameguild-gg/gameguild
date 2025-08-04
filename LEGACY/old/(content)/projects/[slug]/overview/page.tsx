import React from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Course Overview</DashboardPageTitle>
        <DashboardPageDescription></DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent></DashboardPageContent>
    </DashboardPage>
  );
}

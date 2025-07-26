import React from 'react';
// import { TestingLabOverview } from '@/components/testing-lab/overview/testing-lab-overview';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Lab</DashboardPageTitle>
        <DashboardPageDescription></DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>{/*<TestingLabOverview />*/}</DashboardPageContent>
    </DashboardPage>
  );
}

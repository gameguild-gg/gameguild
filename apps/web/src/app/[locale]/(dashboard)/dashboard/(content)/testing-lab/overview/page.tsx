// import { TestingLabOverview } from '@/components/testing-lab/overview/testing-lab-overview';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingLabOverview }                                                                                     from '@/components/testing-lab/overview/testing-lab-overview';
import React from 'react';

export default function TestingLabPage() {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Lab</DashboardPageTitle>
        <DashboardPageDescription></DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingLabOverview />
      </DashboardPageContent>
    </DashboardPage>
  );
}

import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingRequestList } from '@/components/testing-lab/management/requests/testing-request-list';
import { getAllTestingRequests } from '@/lib/admin';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  const testingRequests = await getAllTestingRequests();

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Requests</DashboardPageTitle>
        <DashboardPageDescription>Manage request submitted to testing sessions you coordinate</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingRequestList data={testingRequests} mode="admin" />
      </DashboardPageContent>
    </DashboardPage>
  );
}

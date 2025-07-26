import React from 'react';
import { TestingRequestList } from '@/components/testing-lab/requests/testing-request-list';
import { getTestingRequests } from '@/lib/testing-lab/testing-lab.actions';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';

export default async function Page(): Promise<React.JSX.Element> {
  const testingRequests = await getTestingRequests();

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Requests</DashboardPageTitle>
        <DashboardPageDescription>Manage request submitted to testing sessions you coordinate</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingRequestList data={testingRequests} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

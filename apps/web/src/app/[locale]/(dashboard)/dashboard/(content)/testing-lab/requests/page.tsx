import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingRequestManagementContent } from '@/components/testing-lab/requests/testing-request-management-content';
import { getTestingRequestsAction } from '@/lib/admin/testing-lab/requests/testing-requests.actions';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  const result = await getTestingRequestsAction();
  const testingRequests = result.data || [];

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Requests</DashboardPageTitle>
        <DashboardPageDescription>Manage request submitted to testing sessions you coordinate</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingRequestManagementContent testingRequests={testingRequests} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

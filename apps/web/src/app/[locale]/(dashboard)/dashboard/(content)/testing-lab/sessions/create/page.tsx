import React from 'react';
import { CreateTestingSessionForm } from '@/components/testing-lab/management/sessions/create-testing-session-form';
import { getAllTestingRequests } from '@/lib/admin/testing-lab/requests/requests.actions';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Create Testing Session | Game Guild Dashboard',
  description: 'Create a new testing session to coordinate game testing activities.',
};

export default async function CreateSessionPage(): Promise<React.JSX.Element> {
  // Get testing requests to populate the dropdown
  const testingRequests = await getAllTestingRequests();

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Create Testing Session</DashboardPageTitle>
        <DashboardPageDescription>Schedule a new testing session and coordinate game testing activities</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <CreateTestingSessionForm testingRequests={testingRequests.testingRequests} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

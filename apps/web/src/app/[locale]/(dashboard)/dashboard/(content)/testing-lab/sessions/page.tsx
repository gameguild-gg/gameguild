import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingSessionManagementContent } from '@/components/testing-lab/sessions/testing-session-management-content';
import { getTestingSessionsData } from '@/lib/admin/testing-lab';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'Testing Sessions | Game Guild Dashboard',
  description: 'Manage testing sessions, review submissions, and coordinate game testing activities in the Game Guild platform.',
};

export default async function Page(): Promise<React.JSX.Element> {
  const result = await getTestingSessionsData();
  const testingSessions = result.testingSessions || [];

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Sessions</DashboardPageTitle>
        <DashboardPageDescription>Manage testing sessions and coordinate game testing activities</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingSessionManagementContent testingSessions={testingSessions} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

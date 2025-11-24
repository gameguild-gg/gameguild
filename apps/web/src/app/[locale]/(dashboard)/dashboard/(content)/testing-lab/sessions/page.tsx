import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingSessionsManagement } from '@/components/testing-lab/sessions/testing-sessions-management';
import { getTestingLocationsAction, getTestingSessionsAction } from '@/lib/admin/testing-lab/sessions/sessions.actions';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'Testing Sessions | Game Guild Dashboard',
  description: 'Manage testing sessions, review submissions, and coordinate game testing activities in the Game Guild platform.',
};

export default async function Page(): Promise<React.JSX.Element> {
  // Load all required data for session management
  const [testingSessions, testingLocations] = await Promise.all([
    getTestingSessionsAction(),
    getTestingLocationsAction(),
  ]);

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Sessions</DashboardPageTitle>
        <DashboardPageDescription>
          Create, edit, and manage testing sessions. Sessions group multiple testing requests together
          in the same location and time period, with a shared tester capacity.
        </DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingSessionsManagement
          initialSessions={testingSessions}
          availableLocations={testingLocations}
        />
      </DashboardPageContent>
    </DashboardPage>
  );
}

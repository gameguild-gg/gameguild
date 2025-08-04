import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingSessionList } from '@/components/testing-lab/management/sessions/testing-session-list';
import { getAllTestingSessions } from '@/lib/admin';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'Testing Sessions | Game Guild Dashboard',
  description: 'Manage testing sessions, review submissions, and coordinate game testing activities in the Game Guild platform.',
};

export default async function Page(): Promise<React.JSX.Element> {
  const testingSessions = await getAllTestingSessions();

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Sessions</DashboardPageTitle>
        <DashboardPageDescription>Manage testing sessions and coordinate game testing activities</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingSessionList data={testingSessions} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

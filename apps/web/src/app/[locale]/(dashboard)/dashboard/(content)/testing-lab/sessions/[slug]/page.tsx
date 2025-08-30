import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingSessionDetails } from '@/components/testing-lab/sessions/testing-session-details';
import { getTestingSessionByIdAction } from '@/lib/admin/testing-lab/sessions/sessions.actions';
import { PropsWithSlugParams } from '@/types';
import { notFound } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;
  const testingSession = await getTestingSessionByIdAction(slug);

  if (!testingSession) notFound();

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>{testingSession.sessionName}</DashboardPageTitle>
        <DashboardPageDescription>View and manage this testing session</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingSessionDetails data={testingSession} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

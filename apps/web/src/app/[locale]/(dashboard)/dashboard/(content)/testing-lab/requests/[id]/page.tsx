import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingRequestDetails } from '@/components/testing-lab';
import { getTestingRequestById } from '@/lib/admin';
import { getTestingSessionsByRequest } from '@/lib/admin/testing-lab/sessions/testing-sessions.actions';
import { PropsWithIdParams } from '@/types';
import { notFound } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PropsWithIdParams): Promise<React.JSX.Element> {
  const { id } = await params;

  const testingRequest = await getTestingRequestById(id);
  let linkedSessions: any[] = [];
  try {
    linkedSessions = await getTestingSessionsByRequest(id);
  } catch (e) {
    // swallow so page still renders
    console.error('Failed to load sessions for request', id, e);
  }

  if (!testingRequest) notFound();

  return (
    <>
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Reports</DashboardPageTitle>
          <DashboardPageDescription></DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <TestingRequestDetails data={testingRequest} sessions={linkedSessions} />
        </DashboardPageContent>
      </DashboardPage>
    </>
  );
}

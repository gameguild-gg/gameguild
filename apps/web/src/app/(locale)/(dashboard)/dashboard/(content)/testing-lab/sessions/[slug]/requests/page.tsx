import React from 'react';
import { notFound } from 'next/navigation';
import { TestingRequestList } from '@/components/testing-lab';
import { getTestingRequestsBySession, getTestingSessionBySlug } from '@/lib/testing-lab/testing-lab.actions';
import { PropsWithSlugParams } from '@/types';
import {
  DashboardPage,
  DashboardPageContent,
  DashboardPageDescription,
  DashboardPageHeader,
  DashboardPageTitle,
} from '@/components/dashboard/common/ui/dashboard-page';

export default async function Page({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;

  const session = await getTestingSessionBySlug(slug);

  if (!session) notFound();

  const testingRequests = await getTestingRequestsBySession(slug);

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

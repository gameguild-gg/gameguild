import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingSessionDetails } from '@/components/testing-lab';
import { getTestingSessionBySlug } from '@/lib/admin';
import { PropsWithSlugParams } from '@/types';
import { notFound } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;
  const testingSession = await getTestingSessionBySlug(slug);

  if (!testingSession) notFound();

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Requests</DashboardPageTitle>
        <DashboardPageDescription>Manage request submitted to testing sessions you coordinate</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingSessionDetails data={testingSession} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

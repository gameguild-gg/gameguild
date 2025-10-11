import React from 'react';
import { notFound } from 'next/navigation';
import { TestingFeedbackList } from '@/components/testing-lab';
import { getTestingFeedbacksBySession, getTestingSessionBySlug } from '@/lib/testing-lab/testing-lab.actions';
import { PropsWithSlugParams } from '@/types';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';

export default async function Page({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;

  const session = await getTestingSessionBySlug(slug);

  if (!session) notFound();

  const testingFeedbacks = await getTestingFeedbacksBySession(slug);

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Feedbacks</DashboardPageTitle>
        <DashboardPageDescription></DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingFeedbackList data={testingFeedbacks} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

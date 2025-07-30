import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingFeedbackList } from '@/components/testing-lab';
import { getTestingFeedbacksBySession, getTestingSessionBySlug } from '@/lib/admin';
import { PropsWithSlugParams } from '@/types';
import { notFound } from 'next/navigation';
import React from 'react';

export default async function FeedbacksPage({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
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

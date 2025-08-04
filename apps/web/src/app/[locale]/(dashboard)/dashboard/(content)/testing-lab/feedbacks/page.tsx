import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingFeedbackList } from '@/components/testing-lab';
import { getTestingFeedbacks } from '@/lib/admin';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  const testingFeedbacks = await getTestingFeedbacks();

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Feedbacks</DashboardPageTitle>
        <DashboardPageDescription>Manage feedbacks submitted to testing sessions you coordinate</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingFeedbackList data={testingFeedbacks} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

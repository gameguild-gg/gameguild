import React from 'react';
import { TestingFeedbackList } from '@/components/testing-lab';
import { getTestingFeedbacks } from '@/lib/testing-lab/testing-lab.actions';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';

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

import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingFeedbackManagementContent } from '@/components/testing-lab/feedbacks/testing-feedback-management-content';
import { getTestingFeedbacksAction } from '@/lib/admin/testing-lab/feedbacks/testing-feedbacks.actions';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  const result = await getTestingFeedbacksAction();
  const testingFeedbacks = result.data || [];

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Feedbacks</DashboardPageTitle>
        <DashboardPageDescription>Manage feedbacks submitted to testing sessions you coordinate</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingFeedbackManagementContent testingFeedbacks={testingFeedbacks} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingFeedbackManagementContent } from '@/components/testing-lab/feedbacks/testing-feedback-management-content';
import { getTestingFeedbacksAction } from '@/lib/admin/testing-lab/feedbacks/testing-feedbacks.actions';
import { PropsWithSlugParams } from '@/types';
import React from 'react';

export default async function FeedbacksPage({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;

  // For now, use the general testing feedbacks action
  // This can be updated to filter by session when the API is properly configured
  const result = await getTestingFeedbacksAction();
  const testingFeedbacks = result.data || [];

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Testing Feedbacks</DashboardPageTitle>
        <DashboardPageDescription>Feedbacks from testing session participants</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <TestingFeedbackManagementContent testingFeedbacks={testingFeedbacks} />
      </DashboardPageContent>
    </DashboardPage>
  );
}

import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TestingFeedbackDetails } from '@/components/testing-lab';
import { getTestingFeedbackById } from '@/lib/admin';
import { PropsWithIdParams } from '@/types';
import { notFound } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PropsWithIdParams): Promise<React.JSX.Element> {
  const { id } = await params;
  const testingFeedback = await getTestingFeedbackById(id);

  if (!testingFeedback) notFound();

  return (
    <>
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Feedbacks</DashboardPageTitle>
          <DashboardPageDescription>Feedbacks from testing session you coordinate</DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <TestingFeedbackDetails data={testingFeedback} />
        </DashboardPageContent>
      </DashboardPage>
    </>
  );
}

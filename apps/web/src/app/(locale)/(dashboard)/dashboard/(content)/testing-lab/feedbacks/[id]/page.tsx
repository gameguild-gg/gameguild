import React from 'react';
import { notFound } from 'next/navigation';
import { TestingFeedbackDetails } from '@/components/testing-lab';
import { getTestingFeedbackById } from '@/lib/testing-lab/testing-lab.actions';
import { PropsWithIdParams } from '@/types';
import {
  DashboardPage,
  DashboardPageContent,
  DashboardPageDescription,
  DashboardPageHeader,
  DashboardPageTitle,
} from '@/components/dashboard/common/ui/dashboard-page';

export default async function Page({ params }: PropsWithIdParams): Promise<React.JSX.Element> {
  const { id } = await params;
  const testingFeedback = await getTestingFeedbackById(id);

  if (!testingFeedback) notFound();

  return (
    <>
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>Your Testing Lab Settings</DashboardPageTitle>
          <DashboardPageDescription></DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <TestingFeedbackDetails data={testingFeedback} />
        </DashboardPageContent>
      </DashboardPage>
    </>
  );
}

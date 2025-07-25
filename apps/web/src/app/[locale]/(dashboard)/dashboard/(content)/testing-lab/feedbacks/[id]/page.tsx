import React from 'react';
import { notFound } from 'next/navigation';
import { TestingFeedbackDetails } from '@/components/testing-lab/testing-feedback-details';
import { PropsWithIdParams } from '@/types';

export default async function Page({ params }: PropsWithIdParams): Promise<React.JSX.Element> {
  const { id } = await params;
  // Todo: Replace with actual data fetching logic.
  const testingFeedback = await getTestingRequestById(id);

  if (!testingFeedback) notFound();

  return (
    <>
      <TestingFeedbackDetails data={testingFeedback} />
    </>
  );
}

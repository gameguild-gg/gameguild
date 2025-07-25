import React from 'react';
import { TestingFeedbackList } from '@/components/testing-lab';
import { getTestingFeedbacks } from '@/lib/testing-lab/testing-lab.actions';

export default async function Page(): Promise<React.JSX.Element> {
  const testingFeedbacks = await getTestingFeedbacks();

  return (
    <div className="container">
      <TestingFeedbackList data={testingFeedbacks} />
    </div>
  );
}

import React from 'react';
import { TestingFeedbackList } from '@/components/testing-lab/testing-feedback-list';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <div className="container">
      <TestingFeedbackList />
    </div>
  );
}

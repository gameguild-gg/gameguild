'use client';

import type { TestingFeedback } from '@/lib/api/testing-types';
import { TestingFeedbacksList } from '../testing-feedbacks-list';

interface TestingFeedbackManagementContentProps {
  testingFeedbacks: TestingFeedback[]
}

export function TestingFeedbackManagementContent({ testingFeedbacks }: TestingFeedbackManagementContentProps) {
  console.log('TestingFeedbackManagementContent received testing feedbacks:', testingFeedbacks.length);

  return (
    <TestingFeedbacksList testingFeedbacks={testingFeedbacks} />
  );
}

'use client';

import type { TestingRequest } from '@/lib/api/testing-types';
import { TestingRequestsList } from '../testing-requests-list';

interface TestingRequestManagementContentProps {
  testingRequests: TestingRequest[]
}

export function TestingRequestManagementContent({ testingRequests }: TestingRequestManagementContentProps) {
  console.log('TestingRequestManagementContent received testing requests:', testingRequests.length);

  return (
    <TestingRequestsList testingRequests={testingRequests} />
  );
}

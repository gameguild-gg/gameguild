'use client';

import type { EnhancedTestingRequest } from '@/lib/admin/testing-lab/requests/testing-requests.actions';
import { TestingRequestsList } from '../testing-requests-list';

interface TestingRequestManagementContentProps {
  testingRequests: EnhancedTestingRequest[]
}

export function TestingRequestManagementContent({ testingRequests }: TestingRequestManagementContentProps) {
  console.log('TestingRequestManagementContent received testing requests:', testingRequests.length);

  return (
    <TestingRequestsList testingRequests={testingRequests} />
  );
}

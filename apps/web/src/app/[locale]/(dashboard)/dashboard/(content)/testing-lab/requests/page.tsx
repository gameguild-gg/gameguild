import React from 'react';
import { TestingRequestList } from '@/components/testing-lab/requests/testing-request-list';
import { getTestingRequests } from '@/lib/testing-lab/testing-lab.actions';

export default async function Page(): Promise<React.JSX.Element> {
  const testingRequests = await getTestingRequests();

  return (
    <div className="container">
      <TestingRequestList data={testingRequests} />
    </div>
  );
}

import React from 'react';
import { TestingRequestList } from '@/components/testing-lab/testing-request-list';

export default async function Page(): Promise<React.JSX.Element> {
  const testingRequests = await getTestingRequests();

  return (
    <div className="container">
      <TestingRequestList data={testingRequests} />
    </div>
  );
}

import React from 'react';
import { TestingRequestList } from '@/components/testing-lab/testing-request-list';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <div className="container">
      <TestingRequestList />
    </div>
  );
}

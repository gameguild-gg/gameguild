import React from 'react';
import { notFound } from 'next/navigation';
import { TestingRequestList } from '@/components/testing-lab';
import { getTestingRequestsBySession, getTestingSessionBySlug } from '@/lib/testing-lab/testing-lab.actions';
import { PropsWithSlugParams } from '@/types';

export default async function Page({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;

  const session = await getTestingSessionBySlug(slug);

  if (!session) notFound();

  const testingRequests = await getTestingRequestsBySession(slug);

  return (
    <div className="container">
      <TestingRequestList data={testingRequests} />
    </div>
  );
}

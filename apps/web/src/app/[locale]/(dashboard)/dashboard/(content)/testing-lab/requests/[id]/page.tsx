import React from 'react';
import { notFound } from 'next/navigation';
import { TestingRequestDetails } from '@/components/testing-lab';
import { getTestingRequestById } from '@/lib/testing-lab/testing-lab.actions';
import { PropsWithIdParams } from '@/types';

export default async function Page({ params }: PropsWithIdParams): Promise<React.JSX.Element> {
  const { id } = await params;
  const testingRequest = await getTestingRequestById(id);

  if (!testingRequest) notFound();

  return (
    <>
      <TestingRequestDetails data={testingRequest} />
    </>
  );
}

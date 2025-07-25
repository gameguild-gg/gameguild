import React from 'react';
import { notFound } from 'next/navigation';
import { TestingSessionDetails } from '@/components/testing-lab/testing-session-details';
import { PropsWithSlugParams } from '@/types';

export default async function Page({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;
  // Todo: Replace with actual data fetching logic.
  const testingSession = await getTestingSessionBySlug(slug);

  if (!testingSession) notFound();

  return (
    <>
      <TestingSessionDetails data={testingSession} />
    </>
  );
}

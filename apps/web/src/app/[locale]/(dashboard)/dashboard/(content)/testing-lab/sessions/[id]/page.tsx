import React from 'react';
import { notFound } from 'next/navigation';
import { TestingSessionDetails } from '@/components/testing-lab/testing-session-details';
import { getTestingSessionBySlug } from '@/lib/testing-lab/testing-lab.actions';
import { PropsWithSlugParams } from '@/types';

export default async function Page({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;
  const testingSession = await getTestingSessionBySlug(slug);

  if (!testingSession) notFound();

  return (
    <>
      <TestingSessionDetails data={testingSession} />
    </>
  );
}

import { notFound } from 'next/navigation';
import { getTestSessionBySlug } from '@/lib/api/testing-lab/test-sessions';
import { SessionDetail } from '@/components/testing-lab/session-detail';

interface SessionPageProps {
  params: {
    slug: string;
  };
}

export default async function SessionPage({ params }: SessionPageProps) {
  const session = await getTestSessionBySlug(params.slug);

  if (!session) {
    notFound();
  }

  return <SessionDetail session={session} />;
}

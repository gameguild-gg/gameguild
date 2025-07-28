import { notFound } from 'next/navigation';
import { getTestSessionBySlug } from '@/lib/admin';
import { SessionDetail } from '@/components/testing-lab/management/sessions/session-detail';

interface SessionPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export default async function SessionPage({ params }: SessionPageProps) {
  const { slug } = await params;
  const session = await getTestSessionBySlug(slug);

  if (!session) {
    notFound();
  }

  return <SessionDetail session={session} />;
}

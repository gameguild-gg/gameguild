import { SessionDetail } from '@/components/testing-lab/management/sessions/session-detail';
import { getTestSessionBySlug } from '@/lib/admin';
import { notFound } from 'next/navigation';

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

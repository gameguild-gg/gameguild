import { JoinProcess } from '@/components/testing-lab/join-process';

interface JoinPageProps {
  params: {
    slug: string;
  };
}

export default function JoinPage({ params }: JoinPageProps) {
  return <JoinProcess sessionSlug={params.slug} />;
}

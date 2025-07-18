import { JoinProcess } from '@/components/testing-lab/join-process';

interface JoinPageProps {
  params: {
    slug: string;
  };
}

export default function JoinPage({ params }: JoinPageProps) {
  return (
    <div className="flex flex-col flex-1 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white">
      <JoinProcess sessionSlug={params.slug} />
    </div>
  );
}

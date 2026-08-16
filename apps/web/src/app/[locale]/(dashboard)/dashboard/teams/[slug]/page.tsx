import { redirect } from 'next/navigation';

export default async function LegacyTeamPage({ params }: { params: Promise<{ slug: string }> }): Promise<never> {
  const { slug } = await params;
  redirect(`/workspace/teams/${encodeURIComponent(slug)}`);
}

import { redirect } from 'next/navigation';

export default async function LegacyTeamPage({ params }: { params: Promise<{ slug: string; section?: string[] }> }): Promise<never> {
  const { slug, section = [] } = await params;
  redirect(`/teams/${encodeURIComponent(slug)}${section.length ? `/${section.map(encodeURIComponent).join('/')}` : ''}`);
}

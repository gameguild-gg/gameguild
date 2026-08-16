import { redirect } from 'next/navigation';

export default async function LegacyTeamSectionPage({ params }: { params: Promise<{ slug: string; section: string[] }> }): Promise<never> {
  const { slug, section } = await params;
  redirect(`/workspace/teams/${encodeURIComponent(slug)}/${section.map(encodeURIComponent).join('/')}`);
}

import { redirect } from 'next/navigation';

export default async function LegacyProjectSectionPage({ params }: { params: Promise<{ slug: string; section: string[] }> }): Promise<never> {
  const { slug, section } = await params;
  redirect(`/workspace/projects/${encodeURIComponent(slug)}/${section.map(encodeURIComponent).join('/')}`);
}

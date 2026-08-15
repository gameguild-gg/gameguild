import { redirect } from 'next/navigation';

export default async function LegacyProjectPage({ params }: { params: Promise<{ slug: string; section?: string[] }> }): Promise<never> {
  const { slug, section = [] } = await params;
  redirect(`/projects/${encodeURIComponent(slug)}${section.length ? `/${section.map(encodeURIComponent).join('/')}` : ''}`);
}

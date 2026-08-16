import { redirect } from 'next/navigation';

export default async function LegacyProjectPage({ params }: { params: Promise<{ slug: string }> }): Promise<never> {
  const { slug } = await params;
  redirect(`/workspace/projects/${encodeURIComponent(slug)}`);
}

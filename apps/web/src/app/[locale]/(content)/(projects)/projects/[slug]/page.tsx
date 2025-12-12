import { redirect } from 'next/navigation';

// STUB FIX: Next.js 15 requires async page with Promise-based params
export default async function ProjectsSlugRedirect({ params }: { params: Promise<{ slug: string; locale: string }> }) {
  const awaited = await params
  // Preserve locale via next-intl middleware by omitting explicit locale in the target
  return redirect(`/dashboard/projects/${awaited.slug}`)
}

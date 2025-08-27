import { redirect } from 'next/navigation'

export default function ProjectsSlugRedirect({ params }: { params: { slug: string; locale: string } }) {
  // Preserve locale via next-intl middleware by omitting explicit locale in the target
  return redirect(`/dashboard/projects/${params.slug}`)
}

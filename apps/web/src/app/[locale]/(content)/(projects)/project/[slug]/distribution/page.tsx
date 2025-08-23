import { redirect } from "next/navigation"

export default function LegacyDistributionRedirect({ params }: { params: { slug: string } }) {
  redirect(`/dashboard/projects/${params.slug}/distribution`)
}

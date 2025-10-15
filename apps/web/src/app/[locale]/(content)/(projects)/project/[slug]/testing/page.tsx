import { redirect } from "next/navigation"

export default function LegacyTestingRedirect({ params }: { params: { slug: string } }) {
  redirect(`/dashboard/projects/${params.slug}/testing`)
}

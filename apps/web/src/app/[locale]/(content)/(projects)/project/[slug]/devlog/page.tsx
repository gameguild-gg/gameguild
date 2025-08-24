import { redirect } from "next/navigation"

export default function LegacyDevlogRedirect({ params }: { params: { slug: string } }) {
  redirect(`/dashboard/projects/${params.slug}/devlog`)
}

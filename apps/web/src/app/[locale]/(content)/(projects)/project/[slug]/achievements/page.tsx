import { redirect } from "next/navigation"

export default function LegacyAchievementsRedirect({ params }: { params: { slug: string } }) {
  redirect(`/dashboard/projects/${params.slug}/achievements`)
}

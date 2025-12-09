import { redirect } from "next/navigation"

export default async function LegacyAchievementsRedirect({
  params
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = await params
  redirect(`/dashboard/projects/${slug}/achievements`)
}

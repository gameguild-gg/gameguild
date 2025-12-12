import { redirect } from "next/navigation"

export default async function LegacyGameJamsRedirect({
  params
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = await params
  redirect(`/dashboard/projects/${slug}/game-jams`)
}

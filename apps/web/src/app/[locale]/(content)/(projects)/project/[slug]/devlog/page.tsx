import { redirect } from "next/navigation"

export default async function LegacyDevlogRedirect({
  params
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = await params
  redirect(`/dashboard/projects/${slug}/devlog`)
}

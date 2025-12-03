import { redirect } from "next/navigation"

// STUB FIX: Make Page async and await Promise-based params for Next.js 15.
export default async function LegacyTestingRedirect({ params }: { params: Promise<{ slug: string }> }) {
  const awaited = await params
  redirect(`/dashboard/projects/${awaited.slug}/testing`)
}

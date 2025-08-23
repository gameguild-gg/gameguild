import { redirect } from "next/navigation"

export default function LegacyGameJamsRedirect({ params }: { params: { slug: string } }) {
  redirect(`/dashboard/projects/${params.slug}/game-jams`)
}

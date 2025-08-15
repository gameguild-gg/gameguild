import { redirect } from "next/navigation"

export default function LegacyFeedbackRedirect({ params }: { params: { slug: string } }) {
  redirect(`/dashboard/projects/${params.slug}/feedbacks`)
}

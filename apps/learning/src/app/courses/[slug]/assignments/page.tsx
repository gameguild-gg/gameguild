import { permanentRedirect } from "next/navigation";

export default async function CourseAssignmentsPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  permanentRedirect(`/courses/${slug}/activities`);
}

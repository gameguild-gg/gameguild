import { redirect } from "next/navigation";

export default async function CourseGradesPage({
  params,
}: PageProps<"/[locale]/courses/[course]/grades">): Promise<React.JSX.Element> {
  const { course, locale } = await params;

  // Keep legacy public course-grades URLs on the same application. An external
  // learning origin can point back here during a staged host migration.
  redirect(`/${locale}/learn/courses/${encodeURIComponent(course)}/grades`);
}

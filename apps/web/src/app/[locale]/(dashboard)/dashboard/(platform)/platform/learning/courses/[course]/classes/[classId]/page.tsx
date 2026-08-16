import { redirect } from 'next/navigation';

export default async function ClassWorkspacePage({
  params,
}: {
  params: Promise<{ locale: string; course: string; classId: string }>;
}) {
  const { course, classId } = await params;
  redirect(`/dashboard/platform/learning/courses/${course}/classes/${classId}/schedule`);
}

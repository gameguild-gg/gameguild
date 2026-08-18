import { getCohort } from '@/lib/learning';
import { notFound } from 'next/navigation';

import { CohortSettingsForm } from '@/components/learning/console/courses/[course]/classes/[classId]/settings/cohort-settings-form';

export default async function CohortSettingsPage({
  params,
}: {
  params: Promise<{ course: string; classId: string }>;
}) {
  const { course, classId } = await params;
  const cohort = await getCohort(classId);
  if (!cohort) notFound();

  return <CohortSettingsForm courseId={course} cohort={cohort} />;
}

import { getCohort, getCohortSchedule } from '@/lib/learning';
import { notFound } from 'next/navigation';

import { CohortScheduleWorkspace } from './cohort-schedule-workspace';

export default async function CohortSchedulePage({
  params,
}: {
  params: Promise<{ course: string; classId: string }>;
}) {
  const { course, classId } = await params;
  const [cohort, schedule] = await Promise.all([getCohort(classId), getCohortSchedule(course, classId)]);
  if (!cohort || cohort.courseId === '') notFound();

  return <CohortScheduleWorkspace courseId={course} cohort={cohort} initialSchedule={schedule} />;
}

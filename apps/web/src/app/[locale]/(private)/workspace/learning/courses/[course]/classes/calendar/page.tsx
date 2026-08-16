import { getCourseCohortCalendar, getCourseCohorts } from '@/lib/learning';

import { GeneralCohortCalendar } from './general-cohort-calendar';

export default async function ClassesCalendarPage({
  params,
}: {
  params: Promise<{ locale: string; course: string }>;
}) {
  const { course } = await params;
  const [collection, calendar] = await Promise.all([
    getCourseCohorts(course),
    getCourseCohortCalendar(course),
  ]);

  return (
    <GeneralCohortCalendar
      courseId={course}
      cohorts={collection.cohorts}
      entries={calendar?.entries ?? []}
    />
  );
}

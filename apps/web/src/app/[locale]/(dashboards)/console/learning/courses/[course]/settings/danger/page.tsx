import React from 'react';
import { getCourse } from '@/lib/learning';
import { CourseDangerZoneForm } from '@/components/learning/course-management/course-danger-zone-form';

/**
 * Course danger zone page (issue #106 phase 1): the course is fetched on the
 * server (cached per request via React `cache` in the query layer) and handed
 * to the client form as a prop — no useEffect fetch waterfall.
 */
export default async function DangerPage({
  params,
}: PageProps<'/[locale]/console/learning/courses/[course]/settings/danger'>): Promise<React.JSX.Element> {
  const { course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);

  if (!course) {
    return <div className="p-6 text-muted-foreground">Course not found.</div>;
  }

  return <CourseDangerZoneForm course={course} surface="console" />;
}

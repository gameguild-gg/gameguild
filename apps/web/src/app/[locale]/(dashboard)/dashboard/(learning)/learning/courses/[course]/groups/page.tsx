import React from 'react';
import { getCourse, getCourseGroupSetViews } from '@/lib/learning';
import { GroupsClient } from './groups-client';

/**
 * Course Groups page: group sets, groups, and manual membership management.
 *
 * Route: /dashboard/learning/courses/[course]/groups
 */
export default async function GroupsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/groups'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const [course, sets] = await Promise.all([
    getCourse(courseId),
    getCourseGroupSetViews(courseId),
  ]);

  return <GroupsClient courseId={course?.id ?? courseId} sets={sets} />;
}

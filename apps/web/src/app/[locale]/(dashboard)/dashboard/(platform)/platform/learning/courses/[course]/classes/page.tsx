import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseCohorts } from '@/lib/learning';
import { ClassControlCenter } from './class-control-center';

/**
 * L6: Course Classes/Schedule Page
 *
 * Route: /learning/courses/[course]/classes
 *
 * Lists independent cohorts and their operational schedule state.
 *
 * Data Pattern:
 * - Layout preloads getCourseCohorts() when classes are enabled
 * - This page awaits the same cached query
 * - Also validates course exists AND has classes feature enabled
 *
 * UI Responsibility:
 * - Cohort periods, schedules, capacity, next meeting, and conflicts
 * - Navigate to the cohort schedule workspace
 */
export default async function ClassesPage({
  params,
}: PageProps<'/[locale]/dashboard/platform/learning/courses/[course]/classes'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  // Parallel fetch - both hit warm cache from layout preload
  const [course, collection] = await Promise.all([
    getCourse(courseId),
    getCourseCohorts(courseId),
  ]);

  if (!course) {
    notFound();
  }

  return (
    <ClassControlCenter courseId={courseId} cohorts={collection.cohorts} />
  );
}

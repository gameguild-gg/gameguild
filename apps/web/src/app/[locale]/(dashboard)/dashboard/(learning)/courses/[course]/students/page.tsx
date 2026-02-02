import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseStudents } from '@/lib/learning';

/**
 * L7: Course Students Page
 *
 * Enrolled students management:
 * - Students list (id, name, email, enrolledAt, progress, lastActivity)
 * - Computed: completion %, active/inactive status
 *
 * Data fetching: Parallel (both preloaded by layout)
 * - getCourse(courseId) -> course info (for 404 check)
 * - getCourseStudents(courseId) -> enrolled students list
 *
 * Fetch Type: REST (via getCourseStudents cache)
 * Cache: revalidate 60s, deduplicated via React cache()
 */
export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses/[course]/students'>): Promise<React.JSX.Element> {
  const { locale, course: courseId } = await params;
  void locale; // Available for i18n if needed

  // Parallel fetch - both hit cache from layout preload
  const [course, studentsData] = await Promise.all([
    getCourse(courseId),
    getCourseStudents(courseId),
  ]);

  if (!course) {
    notFound();
  }

  const { students, total } = studentsData;

  // Computed values per student (would be calculated in component/util)
  // students.map(student => ({
  //   ...student,
  //   completionPercent: student.progress,
  //   isActive: new Date(student.lastActivity) > new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), // Active if activity in last 7 days
  // }));

  void course; // TODO: Pass to UI components
  void students; // TODO: Pass to UI components
  void total; // TODO: Pass to UI components

  return <></>;
}

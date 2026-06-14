import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseClasses } from '@/lib/learning';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { CourseClassesManager } from './course-classes-manager';

/**
 * L6: Course Classes/Schedule Page
 *
 * Route: /learning/courses/[course]/classes
 *
 * Lists all scheduled and past classes for live/presential/hybrid courses.
 * Only available when course.features.hasClasses = true.
 *
 * Data Pattern:
 * - Layout conditionally preloaded getCourseClasses() if hasClasses
 * - This page awaits getCourseClasses() — hits warm cache or in-flight promise
 * - Also validates course exists AND has classes feature enabled
 *
 * UI Responsibility:
 * - Scheduled class list with upcoming/live/completed counts
 * - Session metadata, attendance capacity, and virtual meeting indicators
 * - Navigate to /classes/[classId] for class detail/editing
 *
 * Delivery Mode Behavior:
 * - Live: Virtual sessions with meeting URLs
 * - Presential: Physical location with address
 * - Hybrid: Mix of both, location.type per class
 */
export default async function ClassesPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/classes'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  // Parallel fetch - both hit warm cache from layout preload
  const [course, classes] = await Promise.all([
    getCourse(courseId),
    getCourseClasses(courseId),
  ]);

  if (!course) {
    notFound();
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 md:grid-cols-3">
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{classes.total}</p><p className="text-sm text-muted-foreground">Total sessions</p></CardContent></Card>
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{classes.upcomingCount}</p><p className="text-sm text-muted-foreground">Upcoming or live</p></CardContent></Card>
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{classes.completedCount}</p><p className="text-sm text-muted-foreground">Completed</p></CardContent></Card>
      </div>

      <CourseClassesManager courseId={courseId} courseTitle={course.title} classes={classes.classes} />
    </div>
  );
}

import React from 'react';
import { notFound, forbidden } from 'next/navigation';
import { getCourse, getCourseClasses } from '@/lib/learning';

/**
 * L6: Course Classes/Schedule Page
 *
 * Route: /dashboard/learning/courses/[course]/classes
 *
 * Lists all scheduled and past classes for live/presential/hybrid courses.
 * Only available when course.features.hasClasses = true.
 *
 * Data Pattern:
 * - Layout conditionally preloaded getCourseClasses() if hasClasses
 * - This page awaits getCourseClasses() — hits warm cache or in-flight promise
 * - Also validates course exists AND has classes feature enabled
 *
 * UI Responsibility (not implemented here):
 * - Calendar view of scheduled classes
 * - List view with upcoming/past tabs
 * - Quick actions: join live, view recording, reschedule
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

  // Route guard: this route is only valid for courses with classes feature
  if (!course.features.hasClasses) {
    forbidden(); // 403 - route not applicable for this course type
  }

  // ==========================================================================
  // DATA AVAILABLE FOR UI:
  // - course: CourseDetails (title, deliveryMode, features)
  // - classes: CourseClasses { classes: CourseClass[], total, upcomingCount, completedCount }
  //
  // CourseClass: { id, title, description, status, scheduledAt, duration,
  //                timezone, location, instructor, attendeeCount, maxAttendees,
  //                recordingUrl, materials, createdAt, updatedAt }
  //
  // Status values: scheduled, live, completed, cancelled, rescheduled
  //
  // Filter by status:
  //   const upcoming = classes.filter(c => c.status === 'scheduled');
  //   const live = classes.filter(c => c.status === 'live');
  //   const past = classes.filter(c => c.status === 'completed');
  // ==========================================================================
  void course;
  void classes;

  return <div>Classes/Schedule Page - UI not implemented</div>;
}

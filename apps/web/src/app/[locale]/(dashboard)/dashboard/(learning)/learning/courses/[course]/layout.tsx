import { getCourse, getCourseAnalytics, getCourseClasses, getCourseContent, getCourseStudents } from '@/lib/learning';
import { notFound } from 'next/navigation';
import React from 'react';
import { CourseNav } from './course-nav';

/**
 * Course Detail Layout
 *
 * Shared layout with sidebar navigation for all course subroutes.
 * Uses Parallel Data Preload Pattern for optimal performance.
 */
export default async function Layout({ children, params }: LayoutProps<'/[locale]/dashboard/learning/courses/[course]'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  // Parallel preload: fire core fetches immediately
  const coursePromise = getCourse(courseId);
  getCourseAnalytics(courseId);
  getCourseContent(courseId);
  getCourseStudents(courseId);

  const course = await coursePromise;

  if (!course) {
    notFound();
  }

  // Conditional preload based on features
  if (course.features.hasClasses) {
    getCourseClasses(courseId);
  }

  return (
    <CourseNav
      courseTitle={course.title}
      courseDescription={course.description}
      courseStatus={course.status}
      courseSlug={course.slug}
      features={course.features}
    >
      {children}
    </CourseNav>
  );
}

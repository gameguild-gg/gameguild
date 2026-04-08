import React from 'react';
import { getCourse, getCourseAnalytics, getCourseContent, getCourseStudents, getCourseClasses } from '@/lib/learning';
import { CourseNav } from './course-nav';

/**
 * Course Detail Layout
 *
 * Shared layout with sidebar navigation for all course subroutes.
 * Uses Parallel Data Preload Pattern for optimal performance.
 */
export default async function Layout({ children, params }: LayoutProps<'/[locale]/dashboard/learning/courses/[course]'>): Promise<React.JSX.Element> {
  const { locale, course: courseId } = await params;
  void locale;

  // Parallel preload: fire core fetches immediately
  const coursePromise = getCourse(courseId);
  getCourseAnalytics(courseId);
  getCourseContent(courseId);
  getCourseStudents(courseId);

  const course = await coursePromise;

  // Conditional preload based on features
  if (course?.features.hasClasses) {
    getCourseClasses(courseId);
  }

  if (!course) {
    return <>{children}</>;
  }

  return (
    <CourseNav courseTitle={course.title} courseDescription={course.description} courseStatus={course.status}>
      {children}
    </CourseNav>
  );
}

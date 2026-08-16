import { getCourseRouteParam } from '@/lib/learning/course-route';
import { getCourse, getCourseAnalytics, getCourseCohorts, getCourseContent, getCourseStudents } from '@/lib/learning';
import { notFound } from 'next/navigation';
import React from 'react';
import { CourseNav } from '@/components/learning/console/courses/[course]/course-nav';

/**
 * Course Detail Layout
 *
 * Shared layout with sidebar navigation for all course subroutes.
 * Uses Parallel Data Preload Pattern for optimal performance.
 */
export default async function Layout({ children, params }: LayoutProps<'/[locale]/workspace/learning/courses/[course]'>): Promise<React.JSX.Element> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);

  if (!course) {
    notFound();
  }

  const courseId = course.id;
  const courseRouteParam = getCourseRouteParam(course);

  // Parallel preload after resolving the route param to the canonical API ID.
  getCourseAnalytics(courseId);
  getCourseContent(courseId);
  getCourseStudents(courseId);

  // Conditional preload based on features
  if (course.features.hasClasses) {
    getCourseCohorts(courseId);
  }

  return (
    <CourseNav
      courseTitle={course.title}
      courseDescription={course.description}
      courseStatus={course.status}
      courseSlug={course.slug}
      courseRouteParam={courseRouteParam}
      locale={locale}
      features={course.features}
    >
      {children}
    </CourseNav>
  );
}

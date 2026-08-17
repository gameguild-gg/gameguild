import React from 'react';
import { getCourse, getCourseDiscussions } from '@/lib/learning';
import { CourseDiscussionsManager } from '@/components/learning/console/courses/[course]/support/discussions/course-discussions-manager';

/**
 * Discussions Page
 *
 * Route: /courses/[course]/support/discussions
 * Condition: course.features.hasDiscussions = true
 */
export default async function DiscussionsPage({
  params,
}: PageProps<'/[locale]/console/learning/courses/[course]/support/discussions'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  if (!course) return <div className="text-muted-foreground p-6">Course not found.</div>;

  const discussions = await getCourseDiscussions(courseId);

  return <CourseDiscussionsManager courseId={courseId} courseTitle={course.title} threads={discussions.threads} />;
}

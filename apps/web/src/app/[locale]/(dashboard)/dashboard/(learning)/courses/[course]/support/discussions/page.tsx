import React from 'react';
import { forbidden } from 'next/navigation';
import { getCourse, getCourseDiscussions } from '@/lib/learning';

/**
 * Discussions Page
 *
 * Route: /courses/[course]/support/discussions
 * Condition: course.features.hasDiscussions = true
 */
export default async function DiscussionsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/support/discussions'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  
  if (!course?.features.hasDiscussions) {
    forbidden();
  }

  const discussions = await getCourseDiscussions(courseId);

  // ==========================================================================
  // DATA: CourseDiscussions
  // threads: [{ id, title, authorName, replyCount, viewCount, pinned, locked, lastReplyAt }]
  // ==========================================================================
  void discussions;

  return <div>Discussions Page - UI not implemented</div>;
}

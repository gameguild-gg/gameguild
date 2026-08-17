import { getCourse, getCourseDiscussions, getCourseSupportTickets } from '@/lib/learning';
import { notFound } from 'next/navigation';
import React from 'react';

/**
 * Support Group Layout
 *
 * Shared layout for all support subroutes.
 * Preloads support-related data for child pages.
 *
 * Routes:
 * - /support (redirect → /support/tickets)
 * - /support/tickets - Support ticket list
 * - /support/tickets/[ticketId] - Ticket detail
 * - /support/discussions - Forum threads (conditional: hasDiscussions)
 * - /support/discussions/[threadId] - Thread detail
 */
export default async function SupportLayout({
  children,
  params,
}: LayoutProps<'/[locale]/console/learning/courses/[course]/support'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  if (!course) {
    notFound();
  }

  // Preload support data
  getCourseSupportTickets(courseId);

  if (course.features.hasDiscussions) {
    getCourseDiscussions(courseId);
  }

  return <>{children}</>;
}

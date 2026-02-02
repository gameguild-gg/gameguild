import React from 'react';
import { getCourseSupportTickets } from '@/lib/learning';

/**
 * Support Tickets Page
 *
 * Route: /courses/[course]/support/tickets
 */
export default async function SupportTicketsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/support/tickets'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const tickets = await getCourseSupportTickets(courseId);

  // ==========================================================================
  // DATA: CourseSupportTickets
  // tickets: [{ id, studentName, subject, status, priority, category, lastMessageAt }]
  // openCount, inProgressCount, resolvedCount
  // ==========================================================================
  void tickets;

  return <div>Support Tickets Page - UI not implemented</div>;
}

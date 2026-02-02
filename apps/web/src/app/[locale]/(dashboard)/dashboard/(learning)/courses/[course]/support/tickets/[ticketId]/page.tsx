import React from 'react';
import { notFound } from 'next/navigation';
import { getSupportTicket } from '@/lib/learning';

/**
 * Support Ticket Detail Page
 *
 * Route: /courses/[course]/support/tickets/[ticketId]
 */
export default async function SupportTicketDetailPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/support/tickets/[ticketId]'>): Promise<React.JSX.Element> {
  const { ticketId } = await params;

  const ticket = await getSupportTicket(ticketId);

  if (!ticket) {
    notFound();
  }

  // ==========================================================================
  // DATA: SupportTicketDetail
  // subject, status, priority, category, studentName, assignedTo
  // messages: [{ authorName, authorRole, content, attachments, createdAt }]
  // ==========================================================================
  void ticket;

  return <div>Support Ticket Detail Page - UI not implemented</div>;
}

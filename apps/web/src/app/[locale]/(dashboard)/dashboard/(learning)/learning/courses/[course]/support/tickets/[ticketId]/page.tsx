import React from 'react';
import { notFound } from 'next/navigation';
import { getSupportTicket } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { MessageSquare } from 'lucide-react';
import { ThreadActionPanel } from '../../thread-action-panel';

/**
 * Support Ticket Detail Page
 *
 * Route: /courses/[course]/support/tickets/[ticketId]
 */
export default async function SupportTicketDetailPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/support/tickets/[ticketId]'>): Promise<React.JSX.Element> {
  const { course: courseId, ticketId } = await params;
  const ticket = await getSupportTicket(ticketId);

  if (!ticket) {
    notFound();
  }

  const replies = ticket.messages.slice(1).map((message) => ({
    id: message.id,
    threadId: ticket.id,
    authorId: message.authorId,
    authorName: message.authorName,
    authorRole: message.authorRole === 'support' ? 'ta' as const : message.authorRole,
    content: message.content,
    upvotes: 0,
    isAnswer: false,
    createdAt: message.createdAt,
    updatedAt: message.createdAt,
  }));

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <Card className="lg:col-span-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2"><MessageSquare className="size-5" />{ticket.subject}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-2">
            <Badge>{ticket.status}</Badge>
            <Badge variant="outline">{ticket.priority}</Badge>
            <Badge variant="outline">{ticket.category}</Badge>
          </div>
          {ticket.messages.map((message) => (
            <div key={message.id} className="rounded-lg border p-4">
              <p className="mb-2 text-sm font-medium">{message.authorName} <span className="text-muted-foreground">· {message.authorRole}</span></p>
              <p className="whitespace-pre-wrap text-sm text-muted-foreground">{message.content}</p>
            </div>
          ))}
        </CardContent>
      </Card>
      <ThreadActionPanel
        courseId={courseId}
        threadId={ticket.id}
        pinned={ticket.priority === 'high' || ticket.priority === 'urgent'}
        resolved={ticket.status === 'resolved' || ticket.status === 'closed'}
        replies={replies}
      />
    </div>
  );
}

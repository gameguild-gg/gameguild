import React from 'react';
import { notFound } from 'next/navigation';
import { getSupportTicket } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { MessageSquare } from 'lucide-react';

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

  return (
    <Card>
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
  );
}

import React from 'react';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { MessageSquare } from 'lucide-react';

/**
 * Support Ticket Detail Page
 *
 * Route: /courses/[course]/support/tickets/[ticketId]
 */
export default async function SupportTicketDetailPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/support/tickets/[ticketId]'>): Promise<React.JSX.Element> {
  void (await params);

  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center py-16 text-center">
        <MessageSquare className="text-muted-foreground mb-4 size-12" />
        <h3 className="text-lg font-medium">Ticket Details</h3>
        <p className="text-muted-foreground mt-1 max-w-sm text-sm">
          View ticket conversation, update status, and respond to student inquiries. Coming soon.
        </p>
      </CardContent>
    </Card>
  );
}

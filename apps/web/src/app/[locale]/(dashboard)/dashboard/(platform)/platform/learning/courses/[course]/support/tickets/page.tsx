import React from 'react';
import { getCourseSupportTickets } from '@/lib/learning';
import { Link } from '@/i18n/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { LifeBuoy } from 'lucide-react';

/**
 * Support Tickets Page
 *
 * Route: /courses/[course]/support/tickets
 */
export default async function SupportTicketsPage({
  params,
}: PageProps<'/[locale]/dashboard/platform/learning/courses/[course]/support/tickets'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;
  const tickets = await getCourseSupportTickets(courseId);

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 md:grid-cols-3">
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{tickets.openCount}</p><p className="text-sm text-muted-foreground">Open</p></CardContent></Card>
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{tickets.inProgressCount}</p><p className="text-sm text-muted-foreground">In progress</p></CardContent></Card>
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{tickets.resolvedCount}</p><p className="text-sm text-muted-foreground">Resolved</p></CardContent></Card>
      </div>
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2"><LifeBuoy className="size-5" />Support Queue</CardTitle>
          <CardDescription>Persisted learner support requests assigned to this course.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          {tickets.tickets.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No support items are open for this course.</div>
          ) : (
            tickets.tickets.map((ticket) => (
              <Link key={ticket.id} href={`/dashboard/platform/learning/courses/${courseId}/support/tickets/${ticket.id}`} className="flex items-center justify-between rounded-lg border p-4 transition-colors hover:bg-muted/50">
                <div>
                  <p className="font-medium">{ticket.subject}</p>
                  <p className="text-sm text-muted-foreground">{ticket.studentName} · {ticket.messageCount} messages</p>
                </div>
                <Badge variant={ticket.status === 'open' ? 'default' : 'secondary'}>{ticket.status}</Badge>
              </Link>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}

import React from 'react';
import { getSupportTickets } from '@/lib/community';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { HeadphonesIcon } from 'lucide-react';

const statusVariant: Record<string, 'default' | 'secondary' | 'outline' | 'destructive'> = {
  open: 'destructive',
  'in-progress': 'default',
  resolved: 'secondary',
  closed: 'outline',
};

const priorityVariant: Record<string, 'default' | 'secondary' | 'outline' | 'destructive'> = {
  critical: 'destructive',
  high: 'destructive',
  medium: 'default',
  low: 'outline',
};

export default async function Page(): Promise<React.JSX.Element> {
  const { tickets, total } = await getSupportTickets({ limit: 50 });

  return (
    <div className="flex flex-col gap-6 p-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Member Support</h1>
        <p className="text-muted-foreground">Manage support tickets from community members.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Support Tickets</CardTitle>
          <CardDescription>{total > 0 ? `${total} tickets` : 'No tickets submitted yet'}</CardDescription>
        </CardHeader>
        <CardContent>
          {tickets.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <HeadphonesIcon className="mb-4 size-12 text-muted-foreground" />
              <h3 className="text-lg font-semibold">No support tickets</h3>
              <p className="text-sm text-muted-foreground">When members submit support requests, they will appear here.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Subject</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Priority</TableHead>
                  <TableHead>Created By</TableHead>
                  <TableHead>Assigned To</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead>Updated</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {tickets.map((ticket) => (
                  <TableRow key={ticket.id}>
                    <TableCell className="font-medium">{ticket.subject}</TableCell>
                    <TableCell>
                      <Badge variant={statusVariant[ticket.status] ?? 'outline'}>{ticket.status}</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={priorityVariant[ticket.priority] ?? 'outline'}>{ticket.priority}</Badge>
                    </TableCell>
                    <TableCell className="text-sm">@{ticket.createdBy.username}</TableCell>
                    <TableCell className="text-sm">{ticket.assignedTo ? `@${ticket.assignedTo.username}` : '—'}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(ticket.createdAt).toLocaleDateString()}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(ticket.updatedAt).toLocaleDateString()}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

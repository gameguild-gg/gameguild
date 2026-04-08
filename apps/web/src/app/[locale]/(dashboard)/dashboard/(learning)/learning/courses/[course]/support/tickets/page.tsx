import React from 'react';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { LifeBuoy } from 'lucide-react';

/**
 * Support Tickets Page
 *
 * Route: /courses/[course]/support/tickets
 */
export default async function SupportTicketsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/support/tickets'>): Promise<React.JSX.Element> {
  void (await params);

  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center py-16 text-center">
        <LifeBuoy className="text-muted-foreground mb-4 size-12" />
        <h3 className="text-lg font-medium">Support Tickets</h3>
        <p className="text-muted-foreground mt-1 max-w-sm text-sm">
          Manage student support requests, track ticket status, and respond to questions. Coming soon.
        </p>
      </CardContent>
    </Card>
  );
}

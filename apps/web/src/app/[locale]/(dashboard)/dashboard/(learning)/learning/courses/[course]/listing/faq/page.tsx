import React from 'react';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { HelpCircle } from 'lucide-react';

/**
 * Listing FAQ Page
 *
 * Route: /courses/[course]/listing/faq
 *
 * Manage frequently asked questions for the course listing.
 */
export default async function ListingFaqPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/listing/faq'>): Promise<React.JSX.Element> {
  void (await params);

  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center py-16 text-center">
        <HelpCircle className="text-muted-foreground mb-4 size-12" />
        <h3 className="text-lg font-medium">Frequently Asked Questions</h3>
        <p className="text-muted-foreground mt-1 max-w-sm text-sm">
          Add and manage FAQ entries that will be displayed on your course listing page. Coming soon.
        </p>
      </CardContent>
    </Card>
  );
}

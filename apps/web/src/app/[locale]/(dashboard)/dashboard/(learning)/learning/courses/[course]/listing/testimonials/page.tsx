import React from 'react';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Star } from 'lucide-react';

/**
 * Listing Testimonials Page
 *
 * Route: /courses/[course]/listing/testimonials
 *
 * Manage student reviews and testimonials.
 * Feature/unfeature testimonials for the public listing.
 */
export default async function ListingTestimonialsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/listing/testimonials'>): Promise<React.JSX.Element> {
  void (await params);

  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center py-16 text-center">
        <Star className="text-muted-foreground mb-4 size-12" />
        <h3 className="text-lg font-medium">Testimonials &amp; Reviews</h3>
        <p className="text-muted-foreground mt-1 max-w-sm text-sm">
          Feature student reviews and testimonials on your course listing to build social proof. Coming soon.
        </p>
      </CardContent>
    </Card>
  );
}

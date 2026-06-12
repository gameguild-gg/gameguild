import React from 'react';
import { getCourseFaq } from '@/lib/learning';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
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
  const { course: courseId } = await params;
  const faq = await getCourseFaq(courseId);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2"><HelpCircle className="size-5" />Frequently Asked Questions</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {faq.items.map((item) => (
          <div key={item.id} className="rounded-lg border p-4">
            <p className="font-medium">{item.question}</p>
            <p className="mt-1 text-sm text-muted-foreground">{item.answer}</p>
          </div>
        ))}
        {faq.items.length === 0 && <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No FAQ entries are available.</div>}
      </CardContent>
    </Card>
  );
}

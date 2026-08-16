import React from 'react';
import { getCourseFaq } from '@/lib/learning';
import { FaqEditorForm } from '@/components/learning/console/courses/[course]/listing/faq/faq-editor-form';
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
}: PageProps<'/[locale]/workspace/learning/courses/[course]/listing/faq'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;
  const faq = await getCourseFaq(courseId);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2"><HelpCircle className="size-5" />Frequently Asked Questions</CardTitle>
      </CardHeader>
      <CardContent>
        <FaqEditorForm courseId={courseId} items={faq.items} />
      </CardContent>
    </Card>
  );
}

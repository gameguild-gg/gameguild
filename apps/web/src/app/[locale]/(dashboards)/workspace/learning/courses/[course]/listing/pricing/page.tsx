import React from 'react';
import { getCourse, getCoursePricing } from '@/lib/learning';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CreditCard, Receipt } from 'lucide-react';
import { PricingEditorForm } from '@/components/learning/console/courses/[course]/listing/pricing/pricing-editor-form';

/**
 * Listing Pricing Page
 *
 * Route: /courses/[course]/listing/pricing
 * Condition: course.features.hasPricing = true
 *
 * Manage pricing tiers, discounts, and refund policy.
 * Only available for paid/subscription/freemium courses.
 */
export default async function ListingPricingPage({
  params,
}: PageProps<'/[locale]/workspace/learning/courses/[course]/listing/pricing'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  if (!course) return <div className="text-muted-foreground p-6">Course not found.</div>;

  const pricing = await getCoursePricing(courseId);

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2"><CreditCard className="size-5" />Pricing</CardTitle>
          <CardDescription>{course.title} storefront monetization and public price display.</CardDescription>
        </CardHeader>
        <CardContent>
          <PricingEditorForm courseId={courseId} pricing={pricing} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Receipt className="size-5" />Policy</CardTitle></CardHeader>
        <CardContent className="text-sm text-muted-foreground">{pricing.refundPolicy}</CardContent>
      </Card>
    </div>
  );
}

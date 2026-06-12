import React from 'react';
import { getCourse, getCoursePricing } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CreditCard, Receipt } from 'lucide-react';

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
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/listing/pricing'>): Promise<React.JSX.Element> {
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
        <CardContent className="space-y-4">
          {pricing.tiers.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">This course is currently free or monetization is disabled.</div>
          ) : (
            pricing.tiers.map((tier) => (
              <div key={tier.id} className="rounded-lg border p-4">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="font-medium">{tier.name}</p>
                    <p className="text-sm text-muted-foreground">{tier.description}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-2xl font-semibold">{tier.currency} {tier.price}</p>
                    <Badge variant="outline">{tier.interval ?? 'one-time'}</Badge>
                  </div>
                </div>
                <div className="mt-4 flex flex-wrap gap-2">
                  {tier.features.map((feature) => <Badge key={feature} variant="secondary">{feature}</Badge>)}
                </div>
              </div>
            ))
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Receipt className="size-5" />Policy</CardTitle></CardHeader>
        <CardContent className="text-sm text-muted-foreground">{pricing.refundPolicy}</CardContent>
      </Card>
    </div>
  );
}

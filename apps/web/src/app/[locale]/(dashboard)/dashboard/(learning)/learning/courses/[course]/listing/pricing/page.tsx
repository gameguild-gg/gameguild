import React from 'react';
import { forbidden } from 'next/navigation';
import { getCourse, getCoursePricing } from '@/lib/learning';

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
  
  if (!course?.features.hasPricing) {
    forbidden();
  }

  const pricing = await getCoursePricing(courseId);

  // ==========================================================================
  // DATA: CoursePricing
  // tiers: [{ id, name, price, currency, interval, features[], highlighted }]
  // discounts: [{ code, type, value, validFrom, validUntil, maxUses, usedCount }]
  // refundPolicy, hasFreeTrial, trialDays
  // ==========================================================================
  void pricing;

  return <div>Listing Pricing Page - UI not implemented</div>;
}

import { getCourse } from '@/lib/learning';
import { notFound } from 'next/navigation';
import React from 'react';

/**
 * Listing Group Layout
 *
 * Shared layout for all listing subroutes (store/catalog configuration).
 *
 * Routes:
 * - /listing (redirect → /listing/info)
 * - /listing/info - Basic course info, objectives, requirements
 * - /listing/media - Cover image, promo video, gallery
 * - /listing/projects - Public project carousel
 * - /listing/testimonials - Student reviews & testimonials
 * - /listing/faq - Frequently asked questions
 * - /listing/pricing - Pricing tiers (conditional: hasPricing)
 * - /listing/access - Visibility and enrollment controls
 */
export default async function ListingLayout({
  children,
  params,
}: LayoutProps<'/[locale]/workspace/learning/courses/[course]/listing'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  if (!course) {
    notFound();
  }

  return <>{children}</>;
}

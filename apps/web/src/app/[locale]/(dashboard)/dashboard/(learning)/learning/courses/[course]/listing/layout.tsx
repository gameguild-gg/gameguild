import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse } from '@/lib/learning';

/**
 * Listing Group Layout
 *
 * Shared layout for all listing subroutes (store/catalog configuration).
 *
 * Routes:
 * - /listing (redirect → /listing/info)
 * - /listing/info - Basic course info, objectives, requirements
 * - /listing/media - Cover image, promo video, gallery
 * - /listing/testimonials - Student reviews & testimonials
 * - /listing/faq - Frequently asked questions
 * - /listing/pricing - Pricing tiers (conditional: hasPricing)
 */
export default async function ListingLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string; course: string }>;
}): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  if (!course) {
    notFound();
  }

  void course;

  return <>{children}</>;
}

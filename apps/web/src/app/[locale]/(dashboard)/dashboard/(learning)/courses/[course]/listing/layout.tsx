import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseListingInfo, getCourseListingMedia, getCourseTestimonials, getCourseFaq, getCoursePricing } from '@/lib/learning';

/**
 * Listing Group Layout
 *
 * Shared layout for all listing subroutes (store/catalog configuration).
 * Preloads all listing-related data for child pages.
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

  // Validate course exists
  const course = await getCourse(courseId);
  if (!course) {
    notFound();
  }

  // Preload all listing data (fire-and-forget)
  getCourseListingInfo(courseId);
  getCourseListingMedia(courseId);
  getCourseTestimonials(courseId);
  getCourseFaq(courseId);
  
  // Conditional preload
  if (course.features.hasPricing) {
    getCoursePricing(courseId);
  }

  // TODO: Listing sub-navigation UI
  void course;

  return <>{children}</>;
}

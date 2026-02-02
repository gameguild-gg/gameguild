import React from 'react';
import { getCourseTestimonials } from '@/lib/learning';

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
  const { course: courseId } = await params;

  const testimonials = await getCourseTestimonials(courseId);

  // ==========================================================================
  // DATA: CourseTestimonials
  // testimonials: [{ id, studentName, rating, title, content, featured, verified }]
  // averageRating, ratingDistribution: { 1: n, 2: n, 3: n, 4: n, 5: n }
  // ==========================================================================
  void testimonials;

  return <div>Listing Testimonials Page - UI not implemented</div>;
}

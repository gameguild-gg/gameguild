import React from 'react';
import { getCourseTestimonials } from '@/lib/learning';
import { TestimonialsManager } from './testimonials-manager';

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

  return <TestimonialsManager courseId={courseId} testimonials={testimonials} />;
}

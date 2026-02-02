import React from 'react';
import { getCourseFaq } from '@/lib/learning';

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

  // ==========================================================================
  // DATA: CourseFaq
  // items: [{ id, question, answer, order, category }]
  // ==========================================================================
  void faq;

  return <div>Listing FAQ Page - UI not implemented</div>;
}

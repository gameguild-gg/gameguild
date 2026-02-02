import React from 'react';
import { getCourseListingMedia } from '@/lib/learning';

/**
 * Listing Media Page
 *
 * Route: /courses/[course]/listing/media
 *
 * Manage cover image, promotional video, and image gallery.
 */
export default async function ListingMediaPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/listing/media'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const media = await getCourseListingMedia(courseId);

  // ==========================================================================
  // DATA: CourseListingMedia
  // coverImage: { url, alt, width, height }
  // promoVideo: { url, duration, thumbnailUrl }
  // gallery: [{ id, type, url, thumbnailUrl, caption, order }]
  // ==========================================================================
  void media;

  return <div>Listing Media Page - UI not implemented</div>;
}

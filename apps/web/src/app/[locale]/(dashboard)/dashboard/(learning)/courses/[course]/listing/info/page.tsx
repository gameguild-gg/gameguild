import React from 'react';
import { getCourseListingInfo } from '@/lib/learning';

/**
 * Listing Info Page
 *
 * Route: /courses/[course]/listing/info
 *
 * Edit course headline, description, objectives, requirements, etc.
 * This is the primary listing page shown to potential students.
 */
export default async function ListingInfoPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/listing/info'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const info = await getCourseListingInfo(courseId);

  // ==========================================================================
  // DATA: CourseListingInfo
  // headline, description, objectives[], requirements[], targetAudience[],
  // language, subtitles[], level, estimatedDuration, lastUpdated
  // ==========================================================================
  void info;

  return <div>Listing Info Page - UI not implemented</div>;
}

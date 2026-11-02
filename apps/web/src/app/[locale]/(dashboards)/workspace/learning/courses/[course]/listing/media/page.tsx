import React from 'react';
import { getCourse } from '@/lib/learning';
import { ListingMediaForm } from '@/components/learning/course-management/listing-media-form';

/**
 * Listing media page (issue #106 phase 1): the course is fetched on the
 * server (cached per request via React `cache` in the query layer) and handed
 * to the client form as a prop — no useEffect fetch waterfall.
 */
export default async function ListingMediaPage({
  params,
}: PageProps<'/[locale]/workspace/learning/courses/[course]/listing/media'>): Promise<React.JSX.Element> {
  const { course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);

  if (!course) {
    return <div className="text-muted-foreground p-6">Course not found.</div>;
  }

  return <ListingMediaForm course={course} />;
}

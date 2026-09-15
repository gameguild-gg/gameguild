import { redirect } from '@/i18n/navigation';
import { getCourse } from '@/lib/learning';
import { getCourseRouteParam } from '@/lib/learning/course-route';

/**
 * Settings Index Redirect
 * /settings → /settings/danger
 *
 * Course identity and access fields live under Listing.
 * Settings focuses on operational controls:
 * - /settings/notifications - Email templates, alerts
 * - /settings/integrations - Third-party integrations
 * - /settings/danger - Ownership transfer, archive, delete course
 */
export default async function Page({ params }: PageProps<'/[locale]/console/learning/courses/[course]/settings'>): Promise<void> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);
  const courseRouteParam = course ? getCourseRouteParam(course) : courseIdentifier;

  return redirect({
    href: `/console/learning/courses/${encodeURIComponent(courseRouteParam)}/settings/danger`,
    locale,
  });
}

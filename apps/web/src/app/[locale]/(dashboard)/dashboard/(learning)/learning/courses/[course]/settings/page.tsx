import { getCourse } from '@/lib/learning';
import { getCourseRouteParam } from '@/lib/learning/course-route';
import { redirect } from 'next/navigation';

/**
 * Settings Index Redirect
 * /settings → /settings/general
 *
 * Course identity fields (title, description, category, etc.) have moved to Course Info.
 * Settings now focuses on operational controls:
 * - /settings/notifications - Email templates, alerts
 * - /settings/integrations - Third-party integrations
 * - /settings/danger - Archive, delete course
 */
export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings'>): Promise<never> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);
  const courseRouteParam = course ? getCourseRouteParam(course) : courseIdentifier;

  redirect(`/${locale}/dashboard/learning/courses/${encodeURIComponent(courseRouteParam)}/settings/general`);
}

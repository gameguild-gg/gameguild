import { redirect } from '@/i18n/navigation';
import { getCourse } from '@/lib/learning';
import { getCourseRouteParam } from '@/lib/learning/course-route';

/**
 * Analytics is surfaced inside Overview.
 */
export default async function AnalyticsPage({
  params,
}: PageProps<'/[locale]/console/learning/courses/[course]/analytics'>): Promise<void> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);
  const courseRouteParam = course ? getCourseRouteParam(course) : courseIdentifier;

  return redirect({
    href: `/console/learning/courses/${encodeURIComponent(courseRouteParam)}/overview`,
    locale,
  });
}

import { getCourse } from '@/lib/learning';
import { getCourseRouteParam } from '@/lib/learning/course-route';
import { redirect } from 'next/navigation';

/**
 * Analytics is surfaced inside Overview.
 */
export default async function AnalyticsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/analytics'>): Promise<never> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);
  const courseRouteParam = course ? getCourseRouteParam(course) : courseIdentifier;

  redirect(`/${locale}/dashboard/learning/courses/${encodeURIComponent(courseRouteParam)}/overview`);
}

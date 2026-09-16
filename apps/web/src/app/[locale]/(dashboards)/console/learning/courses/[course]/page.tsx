import { redirect } from '@/i18n/navigation';
import { getCourseRouteParam } from '@/lib/learning/course-route';
import { getCourse } from '@/lib/learning';

/**
 * L4: Course Detail Redirect
 *
 * Redirects to the overview page which contains the course dashboard
 * with analytics, metrics, and course summary.
 */
export default async function Page({ params }: PageProps<'/[locale]/console/learning/courses/[course]'>): Promise<void> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);
  const courseRouteParam = course ? getCourseRouteParam(course) : courseIdentifier;

  return redirect({
    href: `/console/learning/courses/${encodeURIComponent(courseRouteParam)}/overview`,
    locale,
  });
}

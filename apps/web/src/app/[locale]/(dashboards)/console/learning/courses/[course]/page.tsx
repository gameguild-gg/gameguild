import { getCourseRouteParam } from '@/lib/learning/course-route';
import { getCourse } from '@/lib/learning';
import { redirect } from 'next/navigation';

/**
 * L4: Course Detail Redirect
 *
 * Redirects to the overview page which contains the course dashboard
 * with analytics, metrics, and course summary.
 */
export default async function Page({ params }: PageProps<'/[locale]/console/learning/courses/[course]'>): Promise<never> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);
  const courseRouteParam = course ? getCourseRouteParam(course) : courseIdentifier;

  redirect(`/${locale}/console/learning/courses/${encodeURIComponent(courseRouteParam)}/overview`);
}

import { redirect } from '@/i18n/navigation';
import { getCourse } from '@/lib/learning';
import { getCourseRouteParam } from '@/lib/learning/course-route';

/**
 * Support Index Redirect
 * /support → /support/tickets
 */
export default async function SupportPage({
  params,
}: PageProps<'/[locale]/workspace/learning/courses/[course]/support'>): Promise<void> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);
  const courseRouteParam = course ? getCourseRouteParam(course) : courseIdentifier;

  return redirect({
    href: `/workspace/learning/courses/${encodeURIComponent(courseRouteParam)}/support/tickets`,
    locale,
  });
}

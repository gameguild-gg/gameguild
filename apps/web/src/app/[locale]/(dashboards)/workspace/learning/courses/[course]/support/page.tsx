import { getCourse } from '@/lib/learning';
import { getCourseRouteParam } from '@/lib/learning/course-route';
import { redirect } from 'next/navigation';

/**
 * Support Index Redirect
 * /support → /support/tickets
 */
export default async function SupportPage({
  params,
}: PageProps<'/[locale]/workspace/learning/courses/[course]/support'>): Promise<never> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);
  const courseRouteParam = course ? getCourseRouteParam(course) : courseIdentifier;

  redirect(`/${locale}/workspace/learning/courses/${encodeURIComponent(courseRouteParam)}/support/tickets`);
}

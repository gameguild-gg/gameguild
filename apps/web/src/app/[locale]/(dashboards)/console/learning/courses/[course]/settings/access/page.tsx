import { redirect } from '@/i18n/navigation';
import { getCourse } from '@/lib/learning';
import { getCourseRouteParam } from '@/lib/learning/course-route';

export default async function Page({ params }: PageProps<'/[locale]/console/learning/courses/[course]/settings/access'>): Promise<never> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);
  const courseRouteParam = course ? getCourseRouteParam(course) : courseIdentifier;

  redirect({
    href: `/console/learning/courses/${encodeURIComponent(courseRouteParam)}/listing/access`,
    locale,
  });
}

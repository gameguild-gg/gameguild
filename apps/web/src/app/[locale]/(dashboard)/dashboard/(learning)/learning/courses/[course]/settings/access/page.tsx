import { getCourse } from '@/lib/learning';
import { getCourseRouteParam } from '@/lib/learning/course-route';
import { redirect } from 'next/navigation';

export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings/access'>): Promise<never> {
  const { locale, course: courseIdentifier } = await params;
  const course = await getCourse(courseIdentifier);
  const courseRouteParam = course ? getCourseRouteParam(course) : courseIdentifier;

  redirect(`/${locale}/dashboard/learning/courses/${encodeURIComponent(courseRouteParam)}/listing/access`);
}

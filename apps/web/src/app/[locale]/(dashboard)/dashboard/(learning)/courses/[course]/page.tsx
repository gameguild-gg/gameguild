import { redirect } from 'next/navigation';

/**
 * L4: Course Detail Redirect
 *
 * Redirects to the overview page which contains the course dashboard
 * with analytics, metrics, and course summary.
 */
export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses/[course]'>): Promise<never> {
  const { locale, course: courseId } = await params;
  redirect(`/${locale}/dashboard/learning/courses/${courseId}/overview`);
}

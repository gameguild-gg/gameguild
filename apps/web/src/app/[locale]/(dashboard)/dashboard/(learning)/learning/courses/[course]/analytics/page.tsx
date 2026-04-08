import { redirect } from 'next/navigation';

/**
 * Analytics Index Redirect
 * /analytics → /analytics/engagement
 */
export default async function AnalyticsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/analytics'>): Promise<never> {
  const { locale, course: courseId } = await params;
  redirect(`/${locale}/dashboard/learning/courses/${courseId}/analytics/engagement`);
}

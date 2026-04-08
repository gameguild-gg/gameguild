import { redirect } from 'next/navigation';

/**
 * Support Index Redirect
 * /support → /support/tickets
 */
export default async function SupportPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/support'>): Promise<never> {
  const { locale, course: courseId } = await params;
  redirect(`/${locale}/dashboard/learning/courses/${courseId}/support/tickets`);
}

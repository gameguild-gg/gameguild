import { redirect } from 'next/navigation';

/**
 * Listing Index Redirect
 * /listing → /listing/info
 */
export default async function ListingPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/listing'>): Promise<never> {
  const { locale, course: courseId } = await params;
  redirect(`/${locale}/dashboard/learning/courses/${courseId}/listing/info`);
}

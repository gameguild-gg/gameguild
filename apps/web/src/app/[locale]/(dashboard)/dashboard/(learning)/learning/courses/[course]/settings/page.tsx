import { redirect } from 'next/navigation';

/**
 * Settings Index Redirect
 * /settings → /settings/access
 *
 * Course identity fields (title, description, category, etc.) have moved to Course Info.
 * Settings now focuses on operational controls:
 * - /settings/access - Visibility, enrollment rules
 * - /settings/notifications - Email templates, alerts
 * - /settings/integrations - Third-party integrations
 * - /settings/danger - Archive, delete course
 */
export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings'>): Promise<never> {
  const { locale, course: courseId } = await params;
  redirect(`/${locale}/dashboard/learning/courses/${courseId}/settings/access`);
}
